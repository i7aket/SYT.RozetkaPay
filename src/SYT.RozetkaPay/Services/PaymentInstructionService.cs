using System.Net;
using Microsoft.Extensions.Logging;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Models.PaymentInstructions;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Service for payment-instruction operations
/// </summary>
/// <remarks>
/// <para>
/// The two official operations differ in authentication, so this service holds two HTTP clients.
/// <see cref="CreateAsync"/> uses the ordinary authenticated transport of <see cref="BaseService"/>, which
/// attaches the configured credentials to each request it builds. <see cref="DeclineAsync"/> uses a second
/// client that carries no RozetkaPay credential and whose primary handler has
/// <c>AllowAutoRedirect = false</c>; its requests never go through the authenticated request factory.
/// </para>
/// <para>
/// The split is not a convenience. <see cref="HttpClient"/> has no per-request redirect switch, so the
/// only way to guarantee that a <c>302</c> is never followed - and that no credential is replayed to
/// whatever host the provider names in <c>Location</c> - is a separate client over a separate handler.
/// </para>
/// <para>
/// This type owns the decline client it creates itself and releases it through
/// <see cref="IDisposable"/>. A decline client supplied by the caller is never disposed here and is
/// never mutated: it is validated at construction and otherwise left exactly as the caller configured
/// it.
/// </para>
/// </remarks>
public class PaymentInstructionService : BaseService, IPaymentInstructionService, IDisposable
{
    private const string CreateEndpoint = "/api/payment-instructions/v1/new";

    /// <summary>
    /// Route of the official decline operation. Also the log label: the real request target carries the
    /// project and payment-instruction identifiers, which must not be logged.
    /// </summary>
    private const string DeclineEndpoint = "/api/payment-instructions/v1/decline";

    private const string LocationHeaderName = "Location";

    /// <summary>
    /// Default request headers that must never be present on a decline client. The decline operation is
    /// unauthenticated, and its redirect target is provider-controlled, so a credential attached here
    /// could be replayed to a host the SDK does not choose.
    /// </summary>
    private static readonly string[] CredentialHeaderNames =
    [
        "Authorization",
        "Proxy-Authorization",
        "X-ON-BEHALF-OF",
        "X-CUSTOMER-AUTH"
    ];

    private readonly HttpClient _declineHttpClient;

    private readonly bool _ownsDeclineHttpClient;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentInstructionService"/> class, creating the
    /// unauthenticated non-redirecting client that the decline operation requires.
    /// </summary>
    /// <remarks>
    /// This constructor is safe without any caller action: the decline client it creates shares the
    /// configured base URL, timeout and user agent, carries no credential, and never follows a redirect.
    /// The instance owns that client and disposes it.
    /// </remarks>
    /// <param name="configuration">SDK configuration.</param>
    /// <param name="httpClient">Authenticated HTTP client used by the create operation.</param>
    /// <param name="logger">Optional logger.</param>
    public PaymentInstructionService(
        RozetkaPayConfiguration configuration,
        HttpClient httpClient,
        ILogger? logger = null)
        : base(configuration, httpClient, logger)
    {
        _declineHttpClient = CreateDeclineHttpClient(Configuration);
        _ownsDeclineHttpClient = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentInstructionService"/> class with a decline
    /// client the caller has already prepared. Intended for dependency injection and for tests.
    /// </summary>
    /// <remarks>
    /// <paramref name="declineHttpClient"/> must not follow redirects: configure its primary handler
    /// with <c>AllowAutoRedirect = false</c>. That cannot be verified through the public
    /// <see cref="HttpClient"/> surface, so it is the caller's guarantee. What can be verified is
    /// checked: a client carrying any credential-bearing default header is rejected here rather than
    /// silently stripped, because stripping headers from a client the caller may share elsewhere would
    /// change behaviour the caller never asked to change. This instance does not own the client and
    /// never disposes it.
    /// </remarks>
    /// <param name="configuration">SDK configuration.</param>
    /// <param name="httpClient">Authenticated HTTP client used by the create operation.</param>
    /// <param name="declineHttpClient">
    /// Non-redirecting client without credentials, used by the decline operation.
    /// </param>
    /// <param name="logger">Optional logger.</param>
    /// <exception cref="ArgumentNullException"><paramref name="declineHttpClient"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="declineHttpClient"/> already carries a credential-bearing default header.
    /// </exception>
    public PaymentInstructionService(
        RozetkaPayConfiguration configuration,
        HttpClient httpClient,
        HttpClient declineHttpClient,
        ILogger? logger = null)
        : base(configuration, httpClient, logger)
    {
        ArgumentNullException.ThrowIfNull(declineHttpClient);
        EnsureNoCredentialHeaders(declineHttpClient);

        _declineHttpClient = declineHttpClient;
        _ownsDeclineHttpClient = false;
    }

    /// <summary>
    /// Create payment instructions for a batch of orders
    /// POST /api/payment-instructions/v1/new
    /// </summary>
    /// <param name="request">Batch request carrying at least one order</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batch URLs and the created instructions</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    public async Task<PaymentInstructionsResult> CreateAsync(
        CreatePaymentInstructionsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await PostAsync<CreatePaymentInstructionsRequest, PaymentInstructionsResult>(
            CreateEndpoint,
            CreateEndpoint,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Decline a payment instruction
    /// GET /api/payment-instructions/v1/decline?project_id={projectId}&amp;payment_instruction_id={paymentInstructionId}
    /// </summary>
    /// <remarks>
    /// Unauthenticated by the official document, and answered with a bare <c>302</c>. The redirect is
    /// not followed and the target is not read: the <c>Location</c> header is the whole result. Neither
    /// identifier nor the returned location is logged.
    /// </remarks>
    /// <param name="projectId">Project ID. Passed raw and escaped once as a query value.</param>
    /// <param name="paymentInstructionId">
    /// Payment instruction ID. Passed raw and escaped once as a query value.
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The <c>302</c> status and the parsed <c>Location</c> header</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="projectId"/> or <paramref name="paymentInstructionId"/> is null.
    /// </exception>
    /// <exception cref="RozetkaPayException">
    /// The provider answered <c>302</c> without a usable <c>Location</c> header, or answered a
    /// successful status other than <c>302</c>.
    /// </exception>
    public async Task<PaymentInstructionDeclineResult> DeclineAsync(
        string projectId,
        string paymentInstructionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projectId);
        ArgumentNullException.ThrowIfNull(paymentInstructionId);

        // Rejected before the retry loop and before the client is touched, so a cancelled caller can
        // never reach a handler.
        cancellationToken.ThrowIfCancellationRequested();

        // Deterministic parameter order, each value escaped exactly once at its own insertion point.
        // The target is absolute so that the request does not depend on the decline client carrying a
        // base address - a caller-supplied client may legitimately have none.
        Uri requestUri = new(
            new Uri(Configuration.BaseUrl),
            $"{DeclineEndpoint}?project_id={Uri.EscapeDataString(projectId)}" +
            $"&payment_instruction_id={Uri.EscapeDataString(paymentInstructionId)}");

        // The decline operation is a GET that only reads the provider's redirect target, so repeating it
        // creates nothing.
        return await ExecuteWithRetryAsync(
            () => SendDeclineAsync(requestUri, cancellationToken),
            isIdempotent: true,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Releases the decline client when this instance created it. Disposing is idempotent, and a
    /// caller-supplied decline client is left untouched.
    /// </summary>
    /// <remarks>
    /// Implemented explicitly so that disposal stays a lifetime concern of whoever owns the instance and
    /// does not appear as an API operation on <see cref="IPaymentInstructionService"/>. Use
    /// <c>using</c>, or let the DI container own the lifetime.
    /// </remarks>
    void IDisposable.Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_ownsDeclineHttpClient)
        {
            _declineHttpClient.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    /// One decline attempt. A <c>302</c> is the documented success and never enters error mapping or
    /// retry handling.
    /// </summary>
    private async Task<PaymentInstructionDeclineResult> SendDeclineAsync(
        Uri requestUri,
        CancellationToken cancellationToken)
    {
        Logger?.LogInformation("Making GET request to {Endpoint}", DeclineEndpoint);

        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);

        // ResponseHeadersRead: the result is a header, so the body - if the provider sends one on a
        // redirect - is never buffered, and the target is never fetched.
        using HttpResponseMessage response = await _declineHttpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        Logger?.LogDebug("Response status: {StatusCode}", response.StatusCode);

        if (response.StatusCode == HttpStatusCode.Redirect)
        {
            return new PaymentInstructionDeclineResult(response.StatusCode, ReadLocation(response));
        }

        string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            // Same status-specific exceptions and RozetkaPayApiError as every other operation.
            HandleErrorResponse(response, content);
        }

        throw new RozetkaPayException(
            "The decline operation must answer HTTP 302 with a Location header, but a different " +
            "successful status was returned.");
    }

    /// <summary>
    /// Read the <c>Location</c> response header of a <c>302</c>.
    /// </summary>
    /// <remarks>
    /// The raw header is parsed here instead of reading
    /// <see cref="System.Net.Http.Headers.HttpResponseHeaders.Location"/>, so that "absent" and "present
    /// but unparseable" are both handled explicitly rather than collapsing into the lenient behaviour of
    /// the typed accessor. The failure message is static: it repeats neither the header value nor either
    /// identifier.
    /// </remarks>
    private static Uri ReadLocation(HttpResponseMessage response)
    {
        string? rawLocation = response.Headers.TryGetValues(LocationHeaderName, out IEnumerable<string>? values)
            ? values.FirstOrDefault(NamesATarget)
            : null;

        if (rawLocation is null || !Uri.TryCreate(rawLocation, UriKind.RelativeOrAbsolute, out Uri? location))
        {
            throw new RozetkaPayException(
                "The decline operation answered HTTP 302 without a usable Location header.");
        }

        return location;
    }

    /// <summary>
    /// Whether a raw <c>Location</c> value names anything at all.
    /// </summary>
    /// <remarks>
    /// The blank check is applied to the unescaped value as well as the raw one. A whitespace-only header
    /// is normalized by the header store into its percent-encoded form (<c>"   "</c> becomes
    /// <c>"%20%20%20"</c>), which parses as a perfectly valid relative reference. Accepting it would hand
    /// the caller a redirect to nowhere instead of reporting that the provider sent no target.
    /// </remarks>
    private static bool NamesATarget(string headerValue)
    {
        return !string.IsNullOrWhiteSpace(headerValue)
            && !string.IsNullOrWhiteSpace(Uri.UnescapeDataString(headerValue));
    }

    /// <summary>
    /// Build the unauthenticated, non-redirecting client used by the decline operation.
    /// </summary>
    /// <remarks>
    /// Only the safe parts of the configuration are copied: base address, timeout and user agent. No
    /// credential header is set, and the handler keeps the platform TLS policy - no certificate callback
    /// is installed and no validation is relaxed.
    /// </remarks>
    private static HttpClient CreateDeclineHttpClient(RozetkaPayConfiguration configuration)
    {
        SocketsHttpHandler handler = new() { AllowAutoRedirect = false };

        HttpClient client = new(handler, disposeHandler: true)
        {
            BaseAddress = new Uri(configuration.BaseUrl),
            Timeout = configuration.Timeout
        };

        if (!string.IsNullOrWhiteSpace(configuration.UserAgent))
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(configuration.UserAgent);
        }

        return client;
    }

    /// <summary>
    /// Reject a caller-supplied decline client that already carries a credential-bearing default header.
    /// </summary>
    private static void EnsureNoCredentialHeaders(HttpClient declineHttpClient)
    {
        foreach (string headerName in CredentialHeaderNames)
        {
            if (declineHttpClient.DefaultRequestHeaders.Contains(headerName))
            {
                throw new ArgumentException(
                    $"The decline client must not carry the '{headerName}' default header: the decline " +
                    "operation is unauthenticated and its redirect target is provider-controlled. " +
                    "Supply a client without credential headers.",
                    nameof(declineHttpClient));
            }
        }
    }
}
