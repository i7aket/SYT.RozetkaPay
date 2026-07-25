using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Extensions;
using SYT.RozetkaPay.Models.InStorePayments;
using SYT.RozetkaPay.Models.PaymentInstructions;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Models.Subscriptions;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// The SDK's logging contract at the DI level.
///
/// Every service writes a static route template instead of the real request target, so a caller identifier
/// never reaches a log sink. That contract is not enforced by the service logs alone:
/// <c>AddHttpClient</c> installs its own handler logging under
/// <c>System.Net.Http.HttpClient.&lt;name&gt;.LogicalHandler</c> and
/// <c>...ClientHandler</c>, which logs the request URI and which the SDK's log labels cannot influence.
///
/// <see cref="PlatformBehaviour_FactoryLogging_RedactsTheQueryButNotThePath"/> pins what that built-in
/// logging actually does in Microsoft.Extensions.Http 9.0.5, because it is the reason the fix is scoped the
/// way it is: query values are already redacted to <c>?*</c>, but <b>path segments are logged verbatim</b>.
/// Of the ten EXP-354 operations exactly one carries a caller identifier in the path —
/// <c>UpdateSubscriptionPaymentMethod</c> — and that is where the leak was real.
///
/// The SDK therefore calls <c>RemoveAllLoggers()</c> on both named clients, so the only HTTP logging left is
/// the SDK's own static-label logging. The tests below prove the leak is gone, that the static logs survive,
/// and that the suppression is actually configured rather than incidental.
/// </summary>
public class Exp354FactoryLoggingTests
{
    private const string AuthenticatedClientName = "RozetkaPay";

    private const string DeclineClientName = "RozetkaPay.PaymentInstructions.Decline";

    private const string SubscriptionUpdateLogLabel =
        "/api/subscriptions/v1/subscriptions/{subscription_id}/payment-method";

    private const string DeclineLogLabel = "/api/payment-instructions/v1/decline";

    private const string InfoLogLabel = "/api/in-store-payments/v1/info";

    /// <summary>
    /// Documents the platform behaviour the SDK contract has to work around, using a client the SDK does not
    /// configure. If a future Microsoft.Extensions.Http release starts redacting path segments too, this test
    /// is the one that will say so.
    /// </summary>
    [Fact]
    public async Task PlatformBehaviour_FactoryLogging_RedactsTheQueryButNotThePath()
    {
        const string pathSecret = "path-secret-marker-EXP354";
        const string querySecret = "query-secret-marker-EXP354";

        CapturingLoggerProvider logs = new();
        ServiceCollection services = new();
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddProvider(logs);
        });
        services
            .AddHttpClient("Control")
            .ConfigurePrimaryHttpMessageHandler(static () => RecordingHandler.Json("{}"));

        using ServiceProvider provider = services.BuildServiceProvider();
        HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("Control");

        using HttpResponseMessage response = await client.GetAsync(
            new Uri($"https://control.unit.test/api/v1/things/{pathSecret}/child?external_id={querySecret}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // The built-in logging is present for a client nobody suppressed it on.
        Assert.NotEmpty(logs.FactoryHttpCategories);

        // A path segment reaches the log verbatim: this is the leak the SDK has to prevent.
        Assert.Contains(logs.AllText, text => text.Contains(pathSecret, StringComparison.Ordinal));

        // A query value does not: 9.0.5 already replaces the whole query with "?*".
        Assert.DoesNotContain(logs.AllText, text => text.Contains(querySecret, StringComparison.Ordinal));
        Assert.Contains(logs.AllText, text => text.Contains("?*", StringComparison.Ordinal));
    }

    /// <summary>
    /// The real leak, and the reason for the fix: the subscription identifier travels in the path of the
    /// authenticated PATCH, so the factory's handler logging used to write it out in full.
    /// </summary>
    [Fact]
    public async Task AuthenticatedClient_ShouldNotLogASubscriptionIdFromThePath()
    {
        CapturingLoggerProvider logs = new();
        RecordingHandler authenticated = RecordingHandler.Json("""{"message":"updated"}""");

        using ServiceProvider provider = BuildProvider(logs, authenticated, RecordingHandler.Json("{}"));
        using IServiceScope scope = provider.CreateScope();

        UpdateSubscriptionPaymentMethodResponse response = await scope.ServiceProvider
            .GetRequiredService<ISubscriptionService>()
            .UpdatePaymentMethodAsync(
                Exp354TestContext.SecretSubscriptionId,
                new UpdateSubscriptionPaymentMethodRequest
                {
                    PaymentMethod = new SubscriptionPaymentMethodUpdate
                    {
                        Type = SubscriptionPaymentMethodUpdateType.Wallet,
                        Wallet = new CustomerWalletRequestPaymentMethod { OptionId = "option-1" }
                    }
                });

        Assert.Equal("updated", response.Message);

        // The request really was sent, and really did carry the identifier in its path.
        Exp354Request recorded = Assert.Single(authenticated.Requests);
        Assert.Contains(
            Uri.EscapeDataString(Exp354TestContext.SecretSubscriptionId),
            recorded.RequestUri.AbsolutePath,
            StringComparison.Ordinal);

        // No sink saw it, in any category, message, structured value, or scope. Both the raw and the
        // percent-encoded spelling are checked: the URI the factory logs is the encoded one.
        AssertNotLogged(logs, Exp354TestContext.SecretSubscriptionId);
        AssertNotLogged(logs, Uri.EscapeDataString(Exp354TestContext.SecretSubscriptionId));

        // The SDK's own static-label log is still there.
        Assert.Contains(logs.AllText, text => text.Contains(SubscriptionUpdateLogLabel, StringComparison.Ordinal));
    }

    /// <summary>
    /// The decline operation's identifiers travel in the query, which 9.0.5 already redacted, so this was
    /// never a leak. What is asserted is the strict contract the SDK now enforces regardless: no factory HTTP
    /// logging at all on the decline client, and the static service log still present.
    /// </summary>
    [Fact]
    public async Task DeclineClient_ShouldEmitNoFactoryHttpLoggingAndKeepTheStaticServiceLog()
    {
        CapturingLoggerProvider logs = new();
        RecordingHandler decline = RecordingHandler.Redirect("https://provider.example/declined?marker=1");

        using ServiceProvider provider = BuildProvider(logs, RecordingHandler.Json("{}"), decline);
        using IServiceScope scope = provider.CreateScope();

        PaymentInstructionDeclineResult result = await scope.ServiceProvider
            .GetRequiredService<IPaymentInstructionService>()
            .DeclineAsync(Exp354TestContext.SecretProjectId, Exp354TestContext.SecretInstructionId);

        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);

        // No built-in HTTP logging category appeared for either client.
        Assert.Empty(logs.FactoryHttpCategories);

        AssertNotLogged(logs, Exp354TestContext.SecretProjectId);
        AssertNotLogged(logs, Exp354TestContext.SecretInstructionId);

        // The returned redirect target is provider output and must not be logged either.
        AssertNotLogged(logs, "https://provider.example/declined");

        // The SDK's own static-label log survives.
        Assert.Contains(logs.AllText, text => text.Contains(DeclineLogLabel, StringComparison.Ordinal));
    }

    /// <summary>
    /// The in-store info operation carries its identifier in the query. It was already redacted by the
    /// factory, and after the fix there is no factory logging at all — while the static service log remains.
    /// </summary>
    [Fact]
    public async Task InStoreInfo_ShouldEmitNoFactoryHttpLoggingAndKeepTheStaticServiceLog()
    {
        CapturingLoggerProvider logs = new();
        RecordingHandler authenticated = RecordingHandler.Json("""{"fc_id":"fc-1"}""");

        using ServiceProvider provider = BuildProvider(logs, authenticated, RecordingHandler.Json("{}"));
        using IServiceScope scope = provider.CreateScope();

        InStorePaymentInfoResponse response = await scope.ServiceProvider
            .GetRequiredService<IInStorePaymentService>()
            .GetInfoAsync(Exp354TestContext.SecretExternalId);

        Assert.Equal("fc-1", response.FcId);

        Assert.Empty(logs.FactoryHttpCategories);
        AssertNotLogged(logs, Exp354TestContext.SecretExternalId);
        AssertNotLogged(logs, Uri.EscapeDataString(Exp354TestContext.SecretExternalId));
        Assert.Contains(logs.AllText, text => text.Contains(InfoLogLabel, StringComparison.Ordinal));
    }

    /// <summary>
    /// Both named clients must be free of the built-in logging, asserted at the client itself rather than
    /// through a particular SDK operation. The probe deliberately puts a secret in the <b>path</b>, which is
    /// the spelling the factory does not redact.
    /// </summary>
    /// <remarks>
    /// This is a behavioural check on purpose. The flag <c>RemoveAllLoggers()</c> sets
    /// (<c>SuppressDefaultLogging</c>) is internal to Microsoft.Extensions.Http, so asserting it would mean
    /// reflecting over a private implementation detail that a future release may rename. What the SDK
    /// actually promises is that no HTTP logging happens on these clients, and that is what is measured.
    /// </remarks>
    [Theory]
    [InlineData(AuthenticatedClientName)]
    [InlineData(DeclineClientName)]
    public async Task BothNamedClients_ShouldEmitNoBuiltInFactoryLogging(string clientName)
    {
        const string pathSecret = "named-client-path-secret-EXP354";

        CapturingLoggerProvider logs = new();
        using ServiceProvider provider = BuildProvider(
            logs,
            RecordingHandler.Json("{}"),
            RecordingHandler.Json("{}"));

        HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(clientName);

        using HttpResponseMessage response = await client.GetAsync(
            new Uri($"{Exp354TestContext.BaseUrl}/api/probe/{pathSecret}?external_id={pathSecret}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.True(
            !logs.FactoryHttpCategories.Any(),
            $"'{clientName}' must emit no built-in IHttpClientFactory logging - it writes the request URI, " +
            $"and path segments are not redacted. Categories seen: " +
            string.Join(", ", logs.FactoryHttpCategories));
        AssertNotLogged(logs, pathSecret);
    }

    /// <summary>
    /// Suppressing the factory logging must not touch the SDK's own logging. An operation with no caller
    /// identifier in its target still logs its route and its response status.
    /// </summary>
    [Fact]
    public async Task ServiceLogging_ShouldSurviveTheSuppression()
    {
        CapturingLoggerProvider logs = new();
        RecordingHandler authenticated = RecordingHandler.Json("{}");

        using ServiceProvider provider = BuildProvider(logs, authenticated, RecordingHandler.Json("{}"));
        using IServiceScope scope = provider.CreateScope();

        await scope.ServiceProvider.GetRequiredService<IPartnerService>().GetFeeDetailsAsync();

        Assert.Contains(
            logs.AllText,
            text => text.Contains("/api/partners/v1/fee-details", StringComparison.Ordinal));
        Assert.Contains(logs.AllText, text => text.Contains("Response status", StringComparison.Ordinal));

        // The SDK logs come from the service categories, never from the factory ones.
        Assert.Contains(
            logs.Entries,
            entry => entry.Category.StartsWith("SYT.RozetkaPay.Services.", StringComparison.Ordinal));
        Assert.Empty(logs.FactoryHttpCategories);
    }

    /// <summary>
    /// Credentials must never reach a sink either, whichever pipeline is doing the logging.
    /// </summary>
    [Fact]
    public async Task NoCredentialValue_ShouldEverReachALogSink()
    {
        CapturingLoggerProvider logs = new();
        RecordingHandler authenticated = RecordingHandler.Json("""{"message":"updated"}""");

        RozetkaPayConfiguration configuration = CreateConfiguration();
        configuration.CustomerAuth = Exp354TestContext.CustomerAuthPlaceholder;
        configuration.OnBehalfOf = "on-behalf-placeholder-not-a-real-value-EXP354";

        using ServiceProvider provider = BuildProvider(
            logs,
            authenticated,
            RecordingHandler.Json("{}"),
            configuration);
        using IServiceScope scope = provider.CreateScope();

        await scope.ServiceProvider.GetRequiredService<ISubscriptionService>().UpdatePaymentMethodAsync(
            Exp354TestContext.SecretSubscriptionId,
            new UpdateSubscriptionPaymentMethodRequest
            {
                PaymentMethod = new SubscriptionPaymentMethodUpdate
                {
                    Type = SubscriptionPaymentMethodUpdateType.CcToken,
                    CcToken = new CustomerCCTokenRequestPaymentMethod { Token = "card-token-secret-EXP354" }
                }
            });

        AssertNotLogged(logs, Exp354TestContext.CustomerAuthPlaceholder);
        AssertNotLogged(logs, "on-behalf-placeholder-not-a-real-value-EXP354");
        AssertNotLogged(logs, "card-token-secret-EXP354");
        AssertNotLogged(logs, configuration.Password);
        AssertNotLogged(logs, "Basic ");
    }

    private static void AssertNotLogged(CapturingLoggerProvider logs, string marker)
    {
        List<CapturedLogEntry> offenders = logs.Entries
            .Where(entry => entry.AllText.Any(text => text.Contains(marker, StringComparison.Ordinal)))
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"'{marker}' reached a log sink through: " +
            string.Join(
                " | ",
                offenders.Select(entry => $"[{entry.Category}] {entry.Message}")));
    }

    /// <summary>
    /// A provider with the SDK registered exactly as a consumer would, plus a controlled primary handler for
    /// each named client so nothing leaves the process.
    /// </summary>
    private static ServiceProvider BuildProvider(
        CapturingLoggerProvider logs,
        HttpMessageHandler authenticatedHandler,
        HttpMessageHandler declineHandler,
        RozetkaPayConfiguration? configuration = null)
    {
        ServiceCollection services = new();
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddProvider(logs);
        });

        services.AddRozetkaPay(configuration ?? CreateConfiguration());

        // Replace only the transport. Everything else - including the logging configuration under test -
        // stays exactly as AddRozetkaPay left it.
        services.AddHttpClient(AuthenticatedClientName)
            .ConfigurePrimaryHttpMessageHandler(() => authenticatedHandler);
        services.AddHttpClient(DeclineClientName)
            .ConfigurePrimaryHttpMessageHandler(() => declineHandler);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static RozetkaPayConfiguration CreateConfiguration()
    {
        return new RozetkaPayConfiguration
        {
            BaseUrl = Exp354TestContext.BaseUrl,
            Login = "unit-test-login",
            Password = "unit-test-placeholder",
            RetryPolicy = RetryPolicy.None
        };
    }
}
