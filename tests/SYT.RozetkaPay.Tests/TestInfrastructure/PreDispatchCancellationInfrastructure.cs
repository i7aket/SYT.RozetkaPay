using Microsoft.Extensions.Logging;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Services;

namespace SYT.RozetkaPay.Tests.TestInfrastructure;

/// <summary>
/// Counts how many times JSON serialization touched a request body.
/// </summary>
/// <remarks>
/// A handler-invocation counter alone cannot prove the pre-dispatch contract: one target framework may
/// reject an already-cancelled token inside <see cref="HttpClient"/> before the handler is called, while the
/// transport helper has already serialized the caller's body. This tripwire is the independent evidence that
/// serialization never happened at all.
/// </remarks>
internal sealed class SerializationTripwire
{
    private int _accesses;

    internal int Accesses => Volatile.Read(ref _accesses);

    internal void Record()
    {
        Interlocked.Increment(ref _accesses);
    }
}

/// <summary>
/// Request body whose only property records every serializer access to it.
/// </summary>
/// <remarks>
/// The SDK serializer reads public properties, so one access equals one serialization of this body. The
/// marker value is a synthetic placeholder: it appears nowhere else, so a log-leak assertion over it cannot
/// pass by accident.
/// </remarks>
internal sealed class TripwirePayload
{
    private readonly SerializationTripwire _tripwire;

    internal TripwirePayload(SerializationTripwire tripwire)
    {
        _tripwire = tripwire;
    }

    public string Marker
    {
        get
        {
            _tripwire.Record();
            return PreDispatchCancellationContext.RequestBodyMarker;
        }
    }
}

/// <summary>
/// Response body for the cancellation matrix. Parameterless-constructible, so it also fits the helper that
/// accepts a <c>204</c>.
/// </summary>
internal sealed class TripwireResult
{
    public string? Outcome { get; set; }
}

/// <summary>
/// Test-only service that exposes the protected transport helpers of <see cref="BaseService"/> unchanged,
/// including the three fallback wrappers and the shared retry executor itself.
/// </summary>
/// <remarks>
/// The pre-dispatch contract belongs to the real helpers, so the matrix drives them directly instead of
/// re-implementing a predicate the production code does not have. <see cref="RetryProbeService"/> covers the
/// seven direct helpers for EXP-356; this type adds the fallback wrappers and a body that can be watched, and
/// leaves the EXP-356 probe to its own subject.
/// </remarks>
internal sealed class CancellationProbeService : BaseService
{
    internal CancellationProbeService(
        RozetkaPayConfiguration configuration,
        HttpClient httpClient,
        ILogger? logger = null)
        : base(configuration, httpClient, logger)
    {
    }

    internal Task<TripwireResult> GetJsonAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        return GetAsync<TripwireResult>(endpoint, cancellationToken);
    }

    internal Task<TripwireResult> PostJsonAsync(
        string endpoint,
        TripwirePayload request,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<TripwirePayload, TripwireResult>(endpoint, request, cancellationToken);
    }

    internal Task<TripwireResult> PostJsonAllowingNoContentAsync(
        string endpoint,
        TripwirePayload request,
        CancellationToken cancellationToken = default)
    {
        return PostAsyncWithNoContent<TripwirePayload, TripwireResult>(endpoint, request, cancellationToken);
    }

    internal Task<TripwireResult> PatchJsonAsync(
        string endpoint,
        TripwirePayload request,
        CancellationToken cancellationToken = default)
    {
        return PatchAsync<TripwirePayload, TripwireResult>(endpoint, request, cancellationToken);
    }

    internal Task<TripwireResult> PostWithoutBodyJsonAsync(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        return PostWithoutBodyAsync<TripwireResult>(endpoint, endpoint, cancellationToken);
    }

    internal Task<TripwireResult> DeleteJsonAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        return DeleteAsync<TripwireResult>(endpoint, cancellationToken);
    }

    internal Task<TripwireResult> DeleteWithBodyAsync(
        string endpoint,
        TripwirePayload request,
        CancellationToken cancellationToken = default)
    {
        return DeleteAsync<TripwirePayload, TripwireResult>(endpoint, endpoint, request, cancellationToken);
    }




    /// <summary>
    /// The shared retry executor itself, so that "the attempt delegate was never invoked" is a direct
    /// assertion rather than an inference from what the transport saw.
    /// </summary>
    internal Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        return ExecuteWithRetryAsync(operation, isIdempotent: true, cancellationToken);
    }
}

/// <summary>
/// Shared setup for the EXP-357 pre-dispatch cancellation contract.
/// </summary>
internal static class PreDispatchCancellationContext
{
    /// <summary>
    /// Reserved TLD: even a regression that bypassed the scripted handler could not resolve this host.
    /// </summary>
    internal const string BaseUrl = "https://cancellation.invalid";

    internal const string Endpoint = "/api/cancellation-probe/v1/resource";

    internal const string FallbackEndpoint = "/api/cancellation-probe/v1/legacy-resource";

    /// <summary>
    /// Unmistakably synthetic leak marker: long, unique, and obviously not a credential, so a log-leak
    /// assertion cannot pass by accident and a secret scanner has nothing to flag.
    /// </summary>
    internal const string RequestBodyMarker = "request-body-marker-must-never-be-logged-EXP357";

    internal const string PasswordPlaceholder = "unit-test-placeholder";

    /// <summary>
    /// The information log every transport helper writes before it builds a request. Pre-cancellation must
    /// produce no log at all, so this is asserted as absent rather than matched loosely.
    /// </summary>
    internal const string RequestLogPrefix = "Making ";

    /// <summary>
    /// The information log the fallback wrappers write inside their <c>404</c> catch, immediately before the
    /// fallback request.
    /// </summary>
    internal const string FallbackLogFragment = "Falling back";

    /// <summary>
    /// The error log <c>HandleErrorResponse</c> writes while the response is still open. Cancelling from it
    /// lands deterministically between "the primary answered 404" and "the fallback catch ran".
    /// </summary>
    internal const string ApiErrorLogPrefix = "RozetkaPay API error";

    internal static RozetkaPayConfiguration Configuration(RetryPolicy? retryPolicy = null)
    {
        return new RozetkaPayConfiguration
        {
            BaseUrl = BaseUrl,
            Login = "unit-test-login",
            Password = PasswordPlaceholder,
            RetryPolicy = retryPolicy ?? RetryPolicy.None,
            UserAgent = "SYT.RozetkaPay.Tests"
        };
    }

    /// <summary>
    /// Retry enabled with a positive budget and no waiting at all: the subject is the cancellation
    /// contract, never the delay.
    /// </summary>
    internal static RetryPolicy Immediate(int maxRetryAttempts = 3)
    {
        return new RetryPolicy
        {
            Enabled = true,
            MaxRetryAttempts = maxRetryAttempts,
            BaseDelay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            BackoffStrategy = BackoffStrategy.Fixed
        };
    }

    /// <summary>
    /// The policy for a row of the matrix: disabled, or enabled with a budget that would allow repeats.
    /// </summary>
    internal static RetryPolicy Policy(bool retryEnabled)
    {
        return retryEnabled ? Immediate() : RetryPolicy.None;
    }

    internal static CancellationProbeService Service(
        HttpMessageHandler handler,
        RetryPolicy? retryPolicy = null,
        ILogger? logger = null)
    {
        return new CancellationProbeService(Configuration(retryPolicy), Client(handler), logger);
    }

    /// <summary>
    /// The real decline path with a separate handler per client, so "the authenticated client was never
    /// touched" and "the decline client was never touched" are independent assertions.
    /// </summary>
    internal static PaymentInstructionService PaymentInstructions(
        HttpMessageHandler authenticatedHandler,
        HttpMessageHandler declineHandler,
        RetryPolicy? retryPolicy = null,
        ILogger? logger = null)
    {
        return new PaymentInstructionService(
            Configuration(retryPolicy),
            Client(authenticatedHandler),
            Client(declineHandler),
            logger);
    }

    internal static HttpClient Client(HttpMessageHandler handler)
    {
        return new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
    }
}
