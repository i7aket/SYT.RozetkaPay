using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Extensions;
using SYT.RozetkaPay.Models.Merchants;
using SYT.RozetkaPay.Models.PaymentInstructions;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Proves what the SDK actually puts on the wire, by running it against a real ASP.NET Core/Kestrel server
/// over a real socket on loopback.
/// </summary>
/// <remarks>
/// <para>
/// The deterministic contract suite stops at the <see cref="HttpMessageHandler"/> boundary: it sees the
/// <see cref="HttpRequestMessage"/> the SDK built. These tests go one layer further out. The SDK is resolved
/// through the supported DI route, so the request travels through <see cref="IHttpClientFactory"/>, a real
/// <c>HttpClientHandler</c>, a TCP connection, and Kestrel's HTTP/1.1 parser before anything is asserted.
/// What the endpoint observes is what a provider would observe.
/// </para>
/// <para>
/// Every host binds <c>127.0.0.1</c> on an ephemeral port, so these tests are parallel-safe and reachable
/// only from this machine. No test sleeps, and no test contacts RozetkaPay.
/// </para>
/// <para>
/// The credential placeholders below carry non-ASCII text on purpose: a Basic header built with anything but
/// UTF-8 would decode to different bytes and fail. They are placeholders, not credentials, and are never sent
/// anywhere but to the loopback listener this test started.
/// </para>
/// </remarks>
public class HttpBoundaryIntegrationTests
{
    /// <summary>Placeholder API login carrying non-ASCII text, to pin the Basic header to UTF-8.</summary>
    private const string LoginPlaceholder = "sandbox-login-Привіт";

    /// <summary>Placeholder API password carrying non-ASCII text. Not a credential.</summary>
    private const string PasswordPlaceholder = "sandbox-password-Привіт";

    /// <summary>Placeholder <c>X-ON-BEHALF-OF</c> value.</summary>
    private const string OnBehalfOfPlaceholder = "partner-id";

    /// <summary>Placeholder <c>X-CUSTOMER-AUTH</c> value.</summary>
    private const string CustomerAuthPlaceholder = "customer-token";

    private const string UserAgentPlaceholder = "SYT.RozetkaPay.Tests-EXP-337";

    private const string MerchantRoute = "/api/merchants/v1/me";

    private const string DeclineRoute = "/api/payment-instructions/v1/decline";

    private const string RedirectTargetRoute = "/decline-redirect-target";

    /// <summary>
    /// Headers that must never appear on the unauthenticated decline request, and whose values must never be
    /// rendered into an assertion message for the authenticated one.
    /// </summary>
    private static readonly string[] CredentialHeaderNames =
    [
        "Authorization",
        "Proxy-Authorization",
        "X-ON-BEHALF-OF",
        "X-CUSTOMER-AUTH"
    ];

    /// <summary>Bound on a single loopback exchange. Generous: nothing here leaves the machine.</summary>
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    // ===================== Outbound authenticated request =====================

    /// <summary>
    /// A canonical authenticated call, observed at the far end of a real socket: Basic credentials that
    /// decode to exactly the configured placeholders as UTF-8, both optional headers exactly as configured,
    /// a user agent, no credential anywhere in the request target, and a typed response back.
    /// </summary>
    [Fact]
    public async Task AuthenticatedRequest_ShouldArriveWithBasicCredentialsAndBothOptionalHeaders()
    {
        RequestCapture capture = new();

        await using LoopbackWebApplication server = await LoopbackWebApplication.StartAsync(endpoints =>
            endpoints.MapGet(MerchantRoute, async context =>
            {
                capture.Record(context.Request);
                await WriteJsonAsync(context, """{"status":"activated"}""");
            }));

        await using ServiceProvider provider = BuildProvider(server.BaseUrl);
        using IServiceScope scope = provider.CreateScope();
        IMerchantService merchants = scope.ServiceProvider.GetRequiredService<IMerchantService>();

        using CancellationTokenSource timeout = new(RequestTimeout);
        MerchantValidationResponse response = await merchants.GetInfoAsync(timeout.Token);

        // The SDK deserialized what Kestrel really wrote back.
        Assert.Equal("activated", response.Status);

        Assert.Equal(1, capture.Count);
        Assert.Equal("GET", capture.Method);
        Assert.Equal(MerchantRoute, capture.Path);
        Assert.Equal(string.Empty, capture.QueryString);

        // Exactly the Basic scheme, spelled that way and nothing else.
        Assert.Equal("Basic", capture.AuthorizationScheme);

        // Decoded inside the capture and compared there, so no credential reaches an assertion message.
        // This is what pins the header to UTF-8: the placeholders are non-ASCII, so a header built with any
        // other encoding decodes to different bytes.
        Assert.True(
            capture.AuthorizationDecodesToPlaceholders,
            "Authorization must decode, as UTF-8, to exactly the configured placeholder login and password.");

        // Exactly one colon, so the login and password boundary is unambiguous.
        Assert.Equal(1, capture.AuthorizationColonCount);

        Assert.Equal([OnBehalfOfPlaceholder], capture.HeaderValues("X-ON-BEHALF-OF"));
        Assert.Equal([CustomerAuthPlaceholder], capture.HeaderValues("X-CUSTOMER-AUTH"));
        Assert.Equal([UserAgentPlaceholder], capture.HeaderValues("User-Agent"));

        // A credential in the request target would be recorded by every intermediary and by Kestrel itself.
        AssertNoCredentialInRequestTarget(capture);

        // A GET carries no body, so there is nothing for the provider to read.
        Assert.Equal(0, capture.BodyLength);
    }

    /// <summary>
    /// The optional headers are absent from the wire when they are not configured. Basic auth is not
    /// optional and is still present.
    /// </summary>
    [Fact]
    public async Task AuthenticatedRequest_ShouldOmitTheOptionalHeaders_WhenTheyAreNotConfigured()
    {
        RequestCapture capture = new();

        await using LoopbackWebApplication server = await LoopbackWebApplication.StartAsync(endpoints =>
            endpoints.MapGet(MerchantRoute, async context =>
            {
                capture.Record(context.Request);
                await WriteJsonAsync(context, """{"status":"activated"}""");
            }));

        await using ServiceProvider provider = BuildProvider(server.BaseUrl, withOptionalHeaders: false);
        using IServiceScope scope = provider.CreateScope();

        using CancellationTokenSource timeout = new(RequestTimeout);
        await scope.ServiceProvider.GetRequiredService<IMerchantService>().GetInfoAsync(timeout.Token);

        Assert.Equal(1, capture.Count);
        Assert.Equal("Basic", capture.AuthorizationScheme);
        Assert.Empty(capture.HeaderValues("X-ON-BEHALF-OF"));
        Assert.Empty(capture.HeaderValues("X-CUSTOMER-AUTH"));
    }

    // ===================== Anonymous decline and redirect =====================

    /// <summary>
    /// The one anonymous operation, over a real socket: no credential-bearing header of any kind, correctly
    /// escaped query values, the <c>Location</c> of the <c>302</c> returned to the caller, and the redirect
    /// target never requested.
    /// </summary>
    [Fact]
    public async Task DeclineRequest_ShouldBeAnonymous_AndMustNotFollowTheProviderRedirect()
    {
        RequestCapture declineCapture = new();
        RequestCapture redirectTargetCapture = new();

        // Raw caller input and the single-pass escaping it must arrive as, written as independent literals.
        const string rawProjectId = "boundary project +/&=?#% Привіт";
        const string rawInstructionId = "boundary instruction +/&=?#% Привіт";
        const string encodedProjectId =
            "boundary%20project%20%2B%2F%26%3D%3F%23%25%20%D0%9F%D1%80%D0%B8%D0%B2%D1%96%D1%82";
        const string encodedInstructionId =
            "boundary%20instruction%20%2B%2F%26%3D%3F%23%25%20%D0%9F%D1%80%D0%B8%D0%B2%D1%96%D1%82";

        await using LoopbackWebApplication server = await LoopbackWebApplication.StartAsync(endpoints =>
        {
            endpoints.MapGet(DeclineRoute, context =>
            {
                declineCapture.Record(context.Request);

                // A provider-shaped 302 whose target is a real, reachable loopback endpoint. If the SDK ever
                // followed it, the counter below would move.
                WriteRedirect(context);
                return Task.CompletedTask;
            });

            endpoints.MapGet(RedirectTargetRoute, context =>
            {
                redirectTargetCapture.Record(context.Request);
                context.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            });
        });

        await using ServiceProvider provider = BuildProvider(server.BaseUrl);
        using IServiceScope scope = provider.CreateScope();
        IPaymentInstructionService instructions =
            scope.ServiceProvider.GetRequiredService<IPaymentInstructionService>();

        using CancellationTokenSource timeout = new(RequestTimeout);
        PaymentInstructionDeclineResult result =
            await instructions.DeclineAsync(rawProjectId, rawInstructionId, timeout.Token);

        // The Location header is the whole documented result.
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        Assert.Equal(new Uri(server.BaseAddress, RedirectTargetRoute), result.Location);

        // The redirect was reported, never followed.
        Assert.Equal(0, redirectTargetCapture.Count);

        Assert.Equal(1, declineCapture.Count);
        Assert.Equal("GET", declineCapture.Method);
        Assert.Equal(DeclineRoute, declineCapture.Path);

        // Each value escaped exactly once, at its own insertion point, in a deterministic order.
        Assert.Equal(
            $"?project_id={encodedProjectId}&payment_instruction_id={encodedInstructionId}",
            declineCapture.QueryString);

        // Kestrel's own decoding agrees with the escaping, so the provider would read the caller's values.
        Assert.Equal(rawProjectId, declineCapture.QueryValue("project_id"));
        Assert.Equal(rawInstructionId, declineCapture.QueryValue("payment_instruction_id"));

        // Not one of the four credential-bearing headers arrived, and no body was sent.
        Assert.Null(declineCapture.AuthorizationScheme);
        foreach (string headerName in CredentialHeaderNames)
        {
            Assert.Empty(declineCapture.HeaderValues(headerName));
        }

        Assert.Equal(0, declineCapture.BodyLength);
        AssertNoCredentialInRequestTarget(declineCapture);
    }

    /// <summary>
    /// The anonymous decline client and the authenticated client are separate and stay that way: an
    /// authenticated call made after a decline is still fully credentialed, and the decline that follows it
    /// is still anonymous.
    /// </summary>
    [Fact]
    public async Task DeclineRequest_ShouldNotDisturbTheAuthenticatedClientInEitherDirection()
    {
        RequestCapture merchantCapture = new();
        RequestCapture declineCapture = new();

        await using LoopbackWebApplication server = await LoopbackWebApplication.StartAsync(endpoints =>
        {
            endpoints.MapGet(MerchantRoute, async context =>
            {
                merchantCapture.Record(context.Request);
                await WriteJsonAsync(context, """{"status":"activated"}""");
            });

            endpoints.MapGet(DeclineRoute, context =>
            {
                declineCapture.Record(context.Request);
                WriteRedirect(context);
                return Task.CompletedTask;
            });
        });

        await using ServiceProvider provider = BuildProvider(server.BaseUrl);
        using IServiceScope scope = provider.CreateScope();
        IMerchantService merchants = scope.ServiceProvider.GetRequiredService<IMerchantService>();
        IPaymentInstructionService instructions =
            scope.ServiceProvider.GetRequiredService<IPaymentInstructionService>();

        using CancellationTokenSource timeout = new(RequestTimeout);

        await merchants.GetInfoAsync(timeout.Token);
        await instructions.DeclineAsync("project-before", "instruction-before", timeout.Token);
        await merchants.GetInfoAsync(timeout.Token);
        await instructions.DeclineAsync("project-after", "instruction-after", timeout.Token);

        // Both authenticated calls carried credentials; neither decline did.
        Assert.Equal(2, merchantCapture.Count);
        Assert.Equal("Basic", merchantCapture.AuthorizationScheme);
        Assert.True(merchantCapture.AuthorizationDecodesToPlaceholders);
        Assert.Equal([OnBehalfOfPlaceholder], merchantCapture.HeaderValues("X-ON-BEHALF-OF"));

        Assert.Equal(2, declineCapture.Count);
        Assert.Null(declineCapture.AuthorizationScheme);
        foreach (string headerName in CredentialHeaderNames)
        {
            Assert.Empty(declineCapture.HeaderValues(headerName));
        }
    }

    /// <summary>
    /// The self-owning constructor really owns the decline client it created: after disposal the operation
    /// fails with <see cref="ObjectDisposedException"/> rather than opening a new connection.
    /// </summary>
    /// <remarks>
    /// The DI route hands the service a factory-owned client instead, which the service must not dispose;
    /// that path is covered by the registration suites. What is proven here is the ownership branch, over a
    /// real socket, so "disposal released the transport" is an observation rather than an assumption.
    /// </remarks>
    [Fact]
    public async Task DeclineService_ShouldReleaseTheDeclineClientItOwns_OnDisposal()
    {
        RequestCapture declineCapture = new();

        await using LoopbackWebApplication server = await LoopbackWebApplication.StartAsync(endpoints =>
            endpoints.MapGet(DeclineRoute, context =>
            {
                declineCapture.Record(context.Request);
                WriteRedirect(context);
                return Task.CompletedTask;
            }));

        RozetkaPayConfiguration configuration = CreateConfiguration(server.BaseUrl);
        using HttpClient authenticatedClient = new() { BaseAddress = new Uri(server.BaseUrl) };

        // The two-argument constructor: the service builds and owns the non-redirecting decline client.
        PaymentInstructionService service = new(configuration, authenticatedClient);
        using CancellationTokenSource timeout = new(RequestTimeout);

        PaymentInstructionDeclineResult result =
            await service.DeclineAsync("owned-project", "owned-instruction", timeout.Token);
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        Assert.Equal(1, declineCapture.Count);

        ((IDisposable)service).Dispose();

        // The owned transport is gone, so the next call cannot reach the still-running listener.
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => service.DeclineAsync("owned-project", "owned-instruction", CancellationToken.None));
        Assert.Equal(1, declineCapture.Count);

        // Disposal is idempotent, and the caller's authenticated client was never touched.
        ((IDisposable)service).Dispose();
        Assert.NotNull(authenticatedClient.BaseAddress);
    }

    // ===================== Helpers =====================

    /// <summary>
    /// No credential placeholder may appear in the request line - not in the path, not in the query, and not
    /// in the two together as Kestrel reconstructs them.
    /// </summary>
    private static void AssertNoCredentialInRequestTarget(RequestCapture capture)
    {
        string requestTarget = capture.Path + capture.QueryString;

        foreach (string placeholder in
            new[] { LoginPlaceholder, PasswordPlaceholder, OnBehalfOfPlaceholder, CustomerAuthPlaceholder })
        {
            Assert.DoesNotContain(placeholder, requestTarget, StringComparison.OrdinalIgnoreCase);
        }

        // Also in escaped form: a naive builder could percent-encode a credential into the query.
        Assert.DoesNotContain(
            Uri.EscapeDataString(PasswordPlaceholder),
            requestTarget,
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Answer with the provider-shaped <c>302</c> the decline operation documents: a <c>Location</c> naming a
    /// real, reachable endpoint on this same loopback host, and no body.
    /// </summary>
    /// <remarks>
    /// The target is built from the request the server is currently handling, so the host does not have to be
    /// captured into the endpoint before it is known. A reachable target matters: it is what makes "the
    /// redirect was not followed" an observation of a counter that could have moved rather than an assumption.
    /// </remarks>
    private static void WriteRedirect(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status302Found;
        context.Response.Headers.Location =
            $"{context.Request.Scheme}://{context.Request.Host}{RedirectTargetRoute}";
    }

    private static async Task WriteJsonAsync(HttpContext context, string json)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(json, Encoding.UTF8, context.RequestAborted);
    }

    /// <summary>
    /// The SDK as a consumer gets it: registered through the supported options route, so the request goes
    /// out over <see cref="IHttpClientFactory"/> and a real handler rather than a test double.
    /// </summary>
    private static ServiceProvider BuildProvider(string baseUrl, bool withOptionalHeaders = true)
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(options =>
        {
            options.BaseUrl = baseUrl;
            options.Login = LoginPlaceholder;
            options.Password = PasswordPlaceholder;
            options.UserAgent = UserAgentPlaceholder;
            options.Timeout = RequestTimeout;
            options.RetryPolicy = RetryPolicy.None;

            // The stub gateway is a loopback listener with no certificate to present, which is the one
            // case clear text is permitted for. Every other host stays https-only, and this setting
            // cannot change that - the validator checks the host, not just the switch.
            options.TransportSecurity = RozetkaPayTransportSecurity.AllowClearTextLoopback;

            if (withOptionalHeaders)
            {
                options.OnBehalfOf = OnBehalfOfPlaceholder;
                options.CustomerAuth = CustomerAuthPlaceholder;
            }
        });

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static RozetkaPayConfiguration CreateConfiguration(string baseUrl)
    {
        return new RozetkaPayConfiguration
        {
            BaseUrl = baseUrl,
            Login = LoginPlaceholder,
            Password = PasswordPlaceholder,
            OnBehalfOf = OnBehalfOfPlaceholder,
            CustomerAuth = CustomerAuthPlaceholder,
            UserAgent = UserAgentPlaceholder,
            Timeout = RequestTimeout,
            RetryPolicy = RetryPolicy.None
        };
    }

    /// <summary>
    /// What one ASP.NET Core endpoint observed, captured inside the endpoint itself.
    /// </summary>
    /// <remarks>
    /// The <c>Authorization</c> value is never stored. It is decoded here and reduced to a scheme, a colon
    /// count, and one boolean, so an assertion failure cannot render a credential even though the header did
    /// arrive. Counters use <see cref="Interlocked"/> so a stray concurrent request would still be counted.
    /// </remarks>
    private sealed class RequestCapture
    {
        private readonly Dictionary<string, string[]> _headers = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, string?> _query = new(StringComparer.Ordinal);

        private int _count;

        /// <summary>Number of requests this endpoint served.</summary>
        internal int Count => Volatile.Read(ref _count);

        internal string? Method { get; private set; }

        internal string? Path { get; private set; }

        /// <summary>Raw query string including the leading <c>?</c>, or empty when there is none.</summary>
        internal string QueryString { get; private set; } = string.Empty;

        internal long BodyLength { get; private set; }

        internal string? AuthorizationScheme { get; private set; }

        /// <summary>
        /// Whether <c>Authorization</c> is Basic and decodes, as UTF-8, to exactly the configured
        /// placeholder pair.
        /// </summary>
        internal bool AuthorizationDecodesToPlaceholders { get; private set; }

        /// <summary>Colons in the decoded Basic parameter, or <c>-1</c> when there was no usable header.</summary>
        internal int AuthorizationColonCount { get; private set; } = -1;

        internal void Record(HttpRequest request)
        {
            Interlocked.Increment(ref _count);

            Method = request.Method;
            Path = request.Path.Value;
            QueryString = request.QueryString.Value ?? string.Empty;
            BodyLength = request.ContentLength ?? 0;

            _headers.Clear();
            foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in request.Headers)
            {
                // The Authorization value is the one thing never stored: its presence is recorded as a marker,
                // so an assertion failure cannot render an encoded credential. The two partnership headers
                // carry placeholders the plan requires asserting exactly, so those values are kept.
                _headers[header.Key] = header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                    ? ["<redacted>"]
                    : header.Value.Select(static value => value ?? string.Empty).ToArray();
            }

            _query.Clear();
            foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> parameter in request.Query)
            {
                _query[parameter.Key] = parameter.Value.FirstOrDefault();
            }

            ReadAuthorization(request);
        }

        /// <summary>
        /// Values of one observed header, or an empty array when it did not arrive. <c>Authorization</c>
        /// reports a redaction marker rather than its value.
        /// </summary>
        internal string[] HeaderValues(string name)
        {
            return _headers.TryGetValue(name, out string[]? values) ? values : [];
        }

        /// <summary>The value Kestrel decoded for one query parameter, or <see langword="null"/>.</summary>
        internal string? QueryValue(string name)
        {
            return _query.TryGetValue(name, out string? value) ? value : null;
        }

        private void ReadAuthorization(HttpRequest request)
        {
            AuthorizationScheme = null;
            AuthorizationDecodesToPlaceholders = false;
            AuthorizationColonCount = -1;

            if (!request.Headers.TryGetValue("Authorization", out Microsoft.Extensions.Primitives.StringValues raw))
            {
                return;
            }

            string? header = raw.FirstOrDefault();
            if (header is null)
            {
                return;
            }

            string[] parts = header.Split(' ', 2, StringSplitOptions.TrimEntries);
            AuthorizationScheme = parts[0];

            if (parts.Length != 2 || !string.Equals(parts[0], "Basic", StringComparison.Ordinal))
            {
                return;
            }

            byte[] decoded;
            try
            {
                decoded = Convert.FromBase64String(parts[1]);
            }
            catch (FormatException)
            {
                return;
            }

            // Decoded as UTF-8 and compared here; the plain text does not leave this method.
            string credentials = Encoding.UTF8.GetString(decoded);
            AuthorizationColonCount = credentials.Count(static character => character == ':');
            AuthorizationDecodesToPlaceholders = string.Equals(
                credentials,
                $"{LoginPlaceholder}:{PasswordPlaceholder}",
                StringComparison.Ordinal);
        }
    }
}
