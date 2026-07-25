using System.Net;
using Microsoft.Extensions.Logging;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Models.PaymentInstructions;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// EXP-357: every SDK transport helper rejects an already-cancelled caller token as one contract owned by the
/// SDK, before logging, JSON serialization, retry bookkeeping, request creation, or any
/// <see cref="HttpClient"/> or handler invocation — on every target framework and for every verb.
///
/// The pre-dispatch check inside <see cref="HttpClient"/> is a runtime implementation detail that differs
/// between <c>net9.0</c> and <c>net10.0</c>, so a handler-invocation counter alone cannot prove the contract:
/// a framework that rejects the token itself would hide a helper that had already logged the request and
/// serialized the caller's body. Every pre-cancelled case here therefore asserts independent tripwires —
/// handler invocations, log entries, serializer accesses, and for the shared executor the attempt delegate
/// itself — and all of them must read zero.
///
/// Nothing here opens a socket: the base address is in the reserved <c>.invalid</c> TLD and the transport
/// never forwards. There is no sleep and no timing-threshold assertion — every mid-flight case is settled by
/// cancelling from a handler or log callback.
/// </summary>
public class PreDispatchCancellationContractTests
{
    // Row keys of the helper matrix. Strings rather than delegates so the theory data stays serializable and
    // a failing case names the helper it drove.
    private const string GetRow = "get";
    private const string PostJsonRow = "post-json";
    private const string PostJsonNoContentRow = "post-json-accepting-no-content";
    private const string PatchRow = "patch";
    private const string PostBodylessRow = "post-bodyless";
    private const string DeleteRow = "delete-bodiless";
    private const string DeleteJsonRow = "delete-json-body";
    private const string GetFallbackRow = "get-fallback";
    private const string PostFallbackRow = "post-fallback";
    private const string PostNoContentFallbackRow = "post-accepting-no-content-fallback";

    private const string NotFoundBody = """{"code":"not_found","message":"Resource not found"}""";

    /// <summary>
    /// Every shared transport helper, each run with the retry policy disabled and enabled with a positive
    /// budget: the two must produce identical cancellation semantics.
    /// </summary>
    public static TheoryData<string, bool> HelperRows
    {
        get
        {
            TheoryData<string, bool> rows = [];
            foreach (string row in AllHelperRows)
            {
                rows.Add(row, false);
                rows.Add(row, true);
            }

            return rows;
        }
    }

    /// <summary>
    /// The helpers a service calls directly, without a fallback wrapper around them.
    /// </summary>
    public static TheoryData<string, bool> DirectHelperRows
    {
        get
        {
            TheoryData<string, bool> rows = [];
            foreach (string row in AllHelperRows.Where(static row => !IsFallbackRow(row)))
            {
                rows.Add(row, false);
                rows.Add(row, true);
            }

            return rows;
        }
    }

    /// <summary>
    /// The three fallback wrappers, which must not dispatch a fallback request after cancellation.
    /// </summary>
    public static TheoryData<string, bool> FallbackRows
    {
        get
        {
            TheoryData<string, bool> rows = [];
            foreach (string row in AllHelperRows.Where(static row => IsFallbackRow(row)))
            {
                rows.Add(row, false);
                rows.Add(row, true);
            }

            return rows;
        }
    }

    public static TheoryData<bool> RetryPolicyStates => [false, true];

    private static IEnumerable<string> AllHelperRows =>
    [
        GetRow,
        PostJsonRow,
        PostJsonNoContentRow,
        PatchRow,
        PostBodylessRow,
        DeleteRow,
        DeleteJsonRow,
        GetFallbackRow,
        PostFallbackRow,
        PostNoContentFallbackRow
    ];

    // ===================== already cancelled: nothing happens at all =====================

    /// <summary>
    /// An already-cancelled token ends the call before the helper does anything observable: no log line, no
    /// serialization of the caller's body, and no request at the transport.
    /// </summary>
    [Theory]
    [MemberData(nameof(HelperRows))]
    public async Task AnAlreadyCancelledToken_ShouldStopEveryHelperBeforeItObservesAnything(
        string row,
        bool retryEnabled)
    {
        ScriptedRetryHandler handler = new(RetryOutcomes.Success());
        RetryLogRecorder logger = new();
        SerializationTripwire tripwire = new();
        CancellationProbeService service = PreDispatchCancellationContext.Service(
            handler,
            PreDispatchCancellationContext.Policy(retryEnabled),
            logger);

        using CancellationTokenSource callerTokenSource = new();
        await callerTokenSource.CancelAsync();

        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => InvokeAsync(row, service, tripwire, callerTokenSource.Token));

        // The caller's own token, not a token the SDK invented on the way out.
        Assert.Equal(callerTokenSource.Token, failure.CancellationToken);

        Assert.Equal(0, handler.AttemptCount);
        Assert.Empty(logger.Entries);
        Assert.Equal(0, tripwire.Accesses);
        Assert.Empty(logger.RetryWarnings);
    }

    /// <summary>
    /// The shared retry executor is the one place every helper's attempt passes through, so the contract is
    /// asserted on it directly: the attempt delegate is never invoked at all.
    /// </summary>
    [Theory]
    [MemberData(nameof(RetryPolicyStates))]
    public async Task AnAlreadyCancelledToken_ShouldStopTheSharedExecutorBeforeTheAttemptDelegate(bool retryEnabled)
    {
        int attempts = 0;
        RetryLogRecorder logger = new();
        CancellationProbeService service = PreDispatchCancellationContext.Service(
            new ScriptedRetryHandler(RetryOutcomes.Success()),
            PreDispatchCancellationContext.Policy(retryEnabled),
            logger);

        using CancellationTokenSource callerTokenSource = new();
        await callerTokenSource.CancelAsync();

        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ExecuteAsync(
                () =>
                {
                    attempts++;
                    return Task.FromResult(new TripwireResult());
                },
                callerTokenSource.Token));

        Assert.Equal(callerTokenSource.Token, failure.CancellationToken);
        Assert.Equal(0, attempts);
        Assert.Empty(logger.Entries);
    }

    /// <summary>
    /// The decline operation builds its own request target before it reaches the executor, so it carries its
    /// own guard. Neither client is touched, and nothing is logged.
    /// </summary>
    [Theory]
    [MemberData(nameof(RetryPolicyStates))]
    public async Task AnAlreadyCancelledToken_ShouldStopTheDeclineOperationBeforeEitherClient(bool retryEnabled)
    {
        ScriptedRetryHandler authenticated = new(RetryOutcomes.Success());
        ScriptedRetryHandler decline = new(RetryOutcomes.Redirect("https://provider.example/declined"));
        RetryLogRecorder logger = new();
        PaymentInstructionService service = PreDispatchCancellationContext.PaymentInstructions(
            authenticated,
            decline,
            PreDispatchCancellationContext.Policy(retryEnabled),
            logger);

        using CancellationTokenSource callerTokenSource = new();
        await callerTokenSource.CancelAsync();

        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.DeclineAsync("project-1", "pi-1", callerTokenSource.Token));

        Assert.Equal(callerTokenSource.Token, failure.CancellationToken);
        Assert.Equal(0, decline.AttemptCount);
        Assert.Equal(0, authenticated.AttemptCount);
        Assert.Empty(logger.Entries);
    }

    // ===================== cancelled after the primary 404, before the fallback =====================

    /// <summary>
    /// Cancellation that becomes observable after the primary request answered <c>404</c> must stop the
    /// fallback path at the catch boundary: no fallback request, and not even the "falling back" log that
    /// used to be written before the fallback helper could inspect the token.
    /// </summary>
    /// <remarks>
    /// Deterministic without a sleep: <c>HandleErrorResponse</c> writes its error log while the primary
    /// response is still open, so cancelling from that callback lands after the <c>404</c> was mapped and
    /// before the fallback catch runs, every time.
    /// </remarks>
    [Theory]
    [MemberData(nameof(FallbackRows))]
    public async Task CancellationAfterThePrimary404_ShouldStopBeforeTheFallbackLogAndRequest(
        string row,
        bool retryEnabled)
    {
        using CancellationTokenSource callerTokenSource = new();
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(HttpStatusCode.NotFound, static _ => NotFoundBody),
            RetryOutcomes.Success());
        RetryLogRecorder logger = new()
        {
            OnEntry = entry =>
            {
                if (entry.Level == LogLevel.Error
                    && entry.Message.StartsWith(
                        PreDispatchCancellationContext.ApiErrorLogPrefix,
                        StringComparison.Ordinal))
                {
                    callerTokenSource.Cancel();
                }
            }
        };
        SerializationTripwire tripwire = new();
        CancellationProbeService service = PreDispatchCancellationContext.Service(
            handler,
            PreDispatchCancellationContext.Policy(retryEnabled),
            logger);

        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => InvokeAsync(row, service, tripwire, callerTokenSource.Token));

        Assert.Equal(callerTokenSource.Token, failure.CancellationToken);

        // The primary only. The fallback target is never requested.
        Assert.Equal(1, handler.AttemptCount);
        Assert.All(
            handler.Attempts,
            attempt => Assert.Equal(PreDispatchCancellationContext.Endpoint, attempt.PathAndQuery));

        Assert.DoesNotContain(
            logger.AllText,
            text => text.Contains(PreDispatchCancellationContext.FallbackLogFragment, StringComparison.Ordinal));
        Assert.Empty(logger.RetryWarnings);

        // The primary serialized the body once; the abandoned fallback never serialized it again.
        Assert.Equal(CarriesBody(row) ? 1 : 0, tripwire.Accesses);
    }

    // ===================== cancelled mid-flight: one attempt, never a retry =====================

    /// <summary>
    /// Cancellation that arrives while a real attempt is in flight is not the same contract: the attempt is
    /// already at the transport and may be observed there. What must still hold is that the caller's
    /// cancellation ends the operation — one request, no retry — and is never converted into a transport
    /// failure.
    /// </summary>
    /// <remarks>
    /// Deterministic: the handler cancels once it has recorded the request, and the response body observes
    /// the token while it is read, so the cancellation lands on a live attempt rather than racing whichever
    /// await the runtime happens to check first.
    /// </remarks>
    [Theory]
    [MemberData(nameof(DirectHelperRows))]
    public async Task MidFlightCancellation_ShouldEndTheOperationAfterExactlyOneAttempt(string row, bool retryEnabled)
    {
        using CancellationTokenSource callerTokenSource = new();
        ScriptedRetryHandler handler = new(RetryOutcomes.CancellableSuccess())
        {
            OnAttempt = (_, _) => callerTokenSource.Cancel()
        };
        RetryLogRecorder logger = new();
        SerializationTripwire tripwire = new();
        CancellationProbeService service = PreDispatchCancellationContext.Service(
            handler,
            PreDispatchCancellationContext.Policy(retryEnabled),
            logger);

        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => InvokeAsync(row, service, tripwire, callerTokenSource.Token));

        // The caller's token survives the transport, so a caller can still tell its own cancellation from a
        // timeout.
        Assert.Equal(callerTokenSource.Token, failure.CancellationToken);

        Assert.Equal(1, handler.AttemptCount);
        Assert.Empty(logger.RetryWarnings);

        // The attempt that was cancelled still released everything it owned.
        CancellableTrackedResponse response = Assert.Single(handler.CancellableResponses);
        Assert.True(response.Disposed, "the response of a cancelled attempt must still be disposed.");
        Assert.True(response.TrackedContent.Disposed, "the content of a cancelled attempt must still be disposed.");
    }

    /// <summary>
    /// The decline path answers on its own client, so mid-flight cancellation is asserted there too: one
    /// decline request, no retry, no authenticated call, and the caller's own token on the way out.
    /// </summary>
    [Theory]
    [MemberData(nameof(RetryPolicyStates))]
    public async Task MidFlightCancellation_ShouldEndTheDeclineOperationAfterExactlyOneRequest(bool retryEnabled)
    {
        using CancellationTokenSource callerTokenSource = new();
        ScriptedRetryHandler authenticated = new(RetryOutcomes.Success());
        ScriptedRetryHandler decline = new(RetryOutcomes.Throws(_ => new TaskCanceledException("the caller cancelled")))
        {
            OnAttempt = (_, _) => callerTokenSource.Cancel()
        };
        RetryLogRecorder logger = new();
        PaymentInstructionService service = PreDispatchCancellationContext.PaymentInstructions(
            authenticated,
            decline,
            PreDispatchCancellationContext.Policy(retryEnabled),
            logger);

        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.DeclineAsync("project-1", "pi-1", callerTokenSource.Token));

        Assert.Equal(callerTokenSource.Token, failure.CancellationToken);
        Assert.Equal(1, decline.AttemptCount);
        Assert.Equal(0, authenticated.AttemptCount);
        Assert.Empty(logger.RetryWarnings);
    }

    /// <summary>
    /// A timeout surfaces as <see cref="TaskCanceledException"/> while the caller's token is still live. The
    /// pre-dispatch guards must not turn that into caller cancellation: it stays a retriable transport
    /// failure.
    /// </summary>
    [Fact]
    public async Task ATimeoutWithALiveCallerToken_ShouldStillBeRetried()
    {
        using CancellationTokenSource callerTokenSource = new();
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Throws(_ => new TaskCanceledException("the request timed out")),
            RetryOutcomes.Success());
        CancellationProbeService service = PreDispatchCancellationContext.Service(
            handler,
            PreDispatchCancellationContext.Immediate(maxRetryAttempts: 1));

        TripwireResult result = await service.GetJsonAsync(
            PreDispatchCancellationContext.Endpoint,
            callerTokenSource.Token);

        Assert.Equal("succeeded", result.Outcome);
        Assert.Equal(2, handler.AttemptCount);
        Assert.False(callerTokenSource.IsCancellationRequested);
    }

    // ===================== the guards are inert for a live token =====================

    /// <summary>
    /// Every tripwire the pre-cancelled matrix asserts to be zero must be capable of firing, or that matrix
    /// would pass against a broken probe. With a live token the same setup reaches the transport, logs the
    /// request, and serializes the body exactly once.
    /// </summary>
    [Theory]
    [MemberData(nameof(RetryPolicyStates))]
    public async Task ALiveToken_ShouldReachTheTransportAndTripEveryTripwire(bool retryEnabled)
    {
        ScriptedRetryHandler handler = new(RetryOutcomes.Success());
        RetryLogRecorder logger = new();
        SerializationTripwire tripwire = new();
        CancellationProbeService service = PreDispatchCancellationContext.Service(
            handler,
            PreDispatchCancellationContext.Policy(retryEnabled),
            logger);

        TripwireResult result = await service.PostJsonAsync(
            PreDispatchCancellationContext.Endpoint,
            new TripwirePayload(tripwire));

        Assert.Equal("succeeded", result.Outcome);
        Assert.Equal(1, handler.AttemptCount);
        Assert.Equal(1, tripwire.Accesses);
        Assert.Contains(
            logger.Entries,
            entry => entry.Message.StartsWith(
                PreDispatchCancellationContext.RequestLogPrefix,
                StringComparison.Ordinal));

        // The body reached the wire exactly as serialized, and the guards changed nothing about it.
        RetryAttempt attempt = Assert.Single(handler.Attempts);
        Assert.Equal($"{{\"marker\":\"{PreDispatchCancellationContext.RequestBodyMarker}\"}}", attempt.Body);
    }

    /// <summary>
    /// The fallback catch still falls back when the caller has not cancelled, so the guard added there is a
    /// cancellation check and not a change to the <c>404</c> fallback contract.
    /// </summary>
    [Theory]
    [MemberData(nameof(FallbackRows))]
    public async Task ALiveToken_ShouldStillFallBackAfterThePrimary404(string row, bool retryEnabled)
    {
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(HttpStatusCode.NotFound, static _ => NotFoundBody),
            RetryOutcomes.Success());
        RetryLogRecorder logger = new();
        SerializationTripwire tripwire = new();
        CancellationProbeService service = PreDispatchCancellationContext.Service(
            handler,
            PreDispatchCancellationContext.Policy(retryEnabled),
            logger);

        await InvokeAsync(row, service, tripwire, CancellationToken.None);

        Assert.Equal(2, handler.AttemptCount);
        Assert.Equal(PreDispatchCancellationContext.Endpoint, handler.Attempts[0].PathAndQuery);
        Assert.Equal(PreDispatchCancellationContext.FallbackEndpoint, handler.Attempts[1].PathAndQuery);
        Assert.Contains(
            logger.AllText,
            text => text.Contains(PreDispatchCancellationContext.FallbackLogFragment, StringComparison.Ordinal));
    }

    /// <summary>
    /// Cancelling is not an error report. The exception the caller catches carries no request target, no body,
    /// and no credential — and neither does the log capture, which for a pre-cancelled call is empty.
    /// </summary>
    /// <remarks>
    /// The whole rendered exception is inspected, not just its message, because that is what an operator sees
    /// when a cancelled call is written to a sink.
    /// </remarks>
    [Theory]
    [MemberData(nameof(HelperRows))]
    public async Task AnAlreadyCancelledToken_ShouldLeakNothingAtAll(string row, bool retryEnabled)
    {
        ScriptedRetryHandler handler = new(RetryOutcomes.Success());
        RetryLogRecorder logger = new();
        SerializationTripwire tripwire = new();
        CancellationProbeService service = PreDispatchCancellationContext.Service(
            handler,
            PreDispatchCancellationContext.Policy(retryEnabled),
            logger);

        using CancellationTokenSource callerTokenSource = new();
        await callerTokenSource.CancelAsync();

        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => InvokeAsync(row, service, tripwire, callerTokenSource.Token));

        string[] forbidden =
        [
            PreDispatchCancellationContext.RequestBodyMarker,
            PreDispatchCancellationContext.PasswordPlaceholder,
            PreDispatchCancellationContext.Endpoint,
            PreDispatchCancellationContext.FallbackEndpoint
        ];

        foreach (string text in logger.AllText.Append(failure.ToString()))
        {
            foreach (string marker in forbidden)
            {
                Assert.DoesNotContain(marker, text, StringComparison.Ordinal);
            }
        }
    }

    // ===================== row dispatch =====================

    /// <summary>
    /// Drives the real protected helper behind a row. Nothing here re-implements a production decision: each
    /// row is one call into <c>BaseService</c> exactly as a service makes it.
    /// </summary>
    private static Task InvokeAsync(
        string row,
        CancellationProbeService service,
        SerializationTripwire tripwire,
        CancellationToken cancellationToken)
    {
        string endpoint = PreDispatchCancellationContext.Endpoint;
        string fallback = PreDispatchCancellationContext.FallbackEndpoint;
        TripwirePayload payload = new(tripwire);

        return row switch
        {
            GetRow => service.GetJsonAsync(endpoint, cancellationToken),
            PostJsonRow => service.PostJsonAsync(endpoint, payload, cancellationToken),
            PostJsonNoContentRow => service.PostJsonAllowingNoContentAsync(endpoint, payload, cancellationToken),
            PatchRow => service.PatchJsonAsync(endpoint, payload, cancellationToken),
            PostBodylessRow => service.PostWithoutBodyJsonAsync(endpoint, cancellationToken),
            DeleteRow => service.DeleteJsonAsync(endpoint, cancellationToken),
            DeleteJsonRow => service.DeleteWithBodyAsync(endpoint, payload, cancellationToken),
            GetFallbackRow => service.GetJsonWithFallbackAsync(endpoint, fallback, cancellationToken),
            PostFallbackRow => service.PostJsonWithFallbackAsync(endpoint, fallback, payload, cancellationToken),
            PostNoContentFallbackRow => service.PostJsonAllowingNoContentWithFallbackAsync(
                endpoint,
                fallback,
                payload,
                cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(row), row, "Unknown transport helper row.")
        };
    }

    /// <summary>
    /// Whether a row sends a JSON request body, and therefore has a serialization step to observe.
    /// </summary>
    private static bool CarriesBody(string row)
    {
        return row is PostJsonRow
            or PostJsonNoContentRow
            or PatchRow
            or DeleteJsonRow
            or PostFallbackRow
            or PostNoContentFallbackRow;
    }

    private static bool IsFallbackRow(string row)
    {
        return row is GetFallbackRow or PostFallbackRow or PostNoContentFallbackRow;
    }
}
