using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Models.PaymentInstructions;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// EXP-356: <see cref="RetryPolicy.RetriableStatusCodes"/> is a public setting, so the runtime must honour
/// exactly the set it declares — no more and no fewer statuses.
///
/// Every case here drives the real retry loop in <c>BaseService</c> through the real transport helpers over a
/// scripted handler that never opens a socket, so the matrix measures production behaviour rather than a
/// re-implementation of it, and no test can reach RozetkaPay.
/// </summary>
public class RetryStatusCodeContractTests
{
    /// <summary>
    /// The default set, written out literally so a silent change to the production default fails here.
    /// </summary>
    public static TheoryData<HttpStatusCode> DefaultRetriableStatuses =>
    [
        HttpStatusCode.InternalServerError,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout,
        HttpStatusCode.TooManyRequests,
        HttpStatusCode.RequestTimeout
    ];

    [Fact]
    public void DefaultPolicy_ShouldDeclareExactlyTheSixDocumentedStatuses()
    {
        HashSet<HttpStatusCode> expected =
        [
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout,
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.RequestTimeout
        ];

        Assert.Equal(expected, RetryPolicy.Default.RetriableStatusCodes);
        Assert.Equal(expected, RetryPolicy.Standard.RetriableStatusCodes);
    }

    // ===================== default status matrix =====================

    /// <summary>
    /// Every configured status repeats the same verb, target, body, and content type through the configured
    /// budget, and the result comes from the attempt that finally succeeded.
    /// </summary>
    [Theory]
    [MemberData(nameof(DefaultRetriableStatuses))]
    public async Task EveryDefaultRetriableStatus_ShouldRepeatTheIdenticalRequestUntilItSucceeds(HttpStatusCode status)
    {
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(status, RetryContractContext.ErrorBody),
            RetryOutcomes.Failure(status, RetryContractContext.ErrorBody),
            RetryOutcomes.Success());
        RetryProbeService service = RetryContractContext.Service(
            handler,
            RetryContractContext.Immediate(maxRetryAttempts: 2));

        RetryProbeResult result = await service.PostJsonAsync<RetryProbeResult>(
            RetryContractContext.Endpoint,
            RetryContractContext.Payload());

        Assert.Equal("succeeded", result.Outcome);
        AssertIdenticalAttempts(handler, expectedCount: 3, HttpMethod.Post, RetryContractContext.ExpectedPayloadJson);
    }

    /// <summary>
    /// The budget is the number of retries after the first attempt, so the total is exactly
    /// <c>1 + MaxRetryAttempts</c> — not one fewer, and not one more.
    /// </summary>
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(3, 4)]
    [InlineData(5, 6)]
    public async Task MaxRetryAttempts_ShouldMeanRetriesAfterTheFirstAttempt(int budget, int expectedAttempts)
    {
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(HttpStatusCode.ServiceUnavailable, RetryContractContext.ErrorBody));
        RetryProbeService service = RetryContractContext.Service(
            handler,
            RetryContractContext.Immediate(maxRetryAttempts: budget));

        await Assert.ThrowsAsync<RozetkaPayException>(
            () => service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint));

        Assert.Equal(expectedAttempts, handler.AttemptCount);
    }

    // ===================== transport helper matrix =====================

    [Fact]
    public async Task GetHelper_ShouldRepeatTheSameBodilessTarget()
    {
        ScriptedRetryHandler handler = FailThenSucceed();
        RetryProbeService service = RetryContractContext.Service(handler, RetryContractContext.Immediate());

        RetryProbeResult result = await service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint);

        Assert.Equal("succeeded", result.Outcome);
        AssertIdenticalAttempts(handler, expectedCount: 2, HttpMethod.Get, expectedBody: null);
    }

    [Fact]
    public async Task PostJsonHelper_ShouldRepeatTheSameBody()
    {
        ScriptedRetryHandler handler = FailThenSucceed();
        RetryProbeService service = RetryContractContext.Service(handler, RetryContractContext.Immediate());

        RetryProbeResult result = await service.PostJsonAsync<RetryProbeResult>(
            RetryContractContext.Endpoint,
            RetryContractContext.Payload());

        Assert.Equal("succeeded", result.Outcome);
        AssertIdenticalAttempts(handler, expectedCount: 2, HttpMethod.Post, RetryContractContext.ExpectedPayloadJson);
    }

    [Fact]
    public async Task PostHelperAcceptingNoContent_ShouldRepeatTheSameBodyAndAcceptA204()
    {
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(HttpStatusCode.ServiceUnavailable, RetryContractContext.ErrorBody),
            RetryOutcomes.NoContent());
        RetryProbeService service = RetryContractContext.Service(handler, RetryContractContext.Immediate());

        RetryProbeResult result = await service.PostJsonAllowingNoContentAsync(
            RetryContractContext.Endpoint,
            RetryContractContext.Payload());

        Assert.Null(result.Outcome);
        AssertIdenticalAttempts(handler, expectedCount: 2, HttpMethod.Post, RetryContractContext.ExpectedPayloadJson);
    }

    [Fact]
    public async Task PatchHelper_ShouldRepeatTheSameBody()
    {
        ScriptedRetryHandler handler = FailThenSucceed();
        RetryProbeService service = RetryContractContext.Service(handler, RetryContractContext.Immediate());

        RetryProbeResult result = await service.PatchJsonAsync<RetryProbeResult>(
            RetryContractContext.Endpoint,
            RetryContractContext.Payload());

        Assert.Equal("succeeded", result.Outcome);
        AssertIdenticalAttempts(handler, expectedCount: 2, HttpMethod.Patch, RetryContractContext.ExpectedPayloadJson);
    }

    /// <summary>
    /// An official POST that declares no request body must not gain an invented <c>{}</c> on the repeat, and
    /// must not be downgraded to a GET.
    /// </summary>
    [Fact]
    public async Task PostWithoutBodyHelper_ShouldRepeatWithoutInventingABody()
    {
        ScriptedRetryHandler handler = FailThenSucceed();
        RetryProbeService service = RetryContractContext.Service(handler, RetryContractContext.Immediate());

        RetryProbeResult result = await service.PostWithoutBodyJsonAsync<RetryProbeResult>(
            RetryContractContext.Endpoint);

        Assert.Equal("succeeded", result.Outcome);
        AssertIdenticalAttempts(handler, expectedCount: 2, HttpMethod.Post, expectedBody: null);
    }

    [Fact]
    public async Task DeleteHelper_ShouldRepeatTheSameBodilessDelete()
    {
        ScriptedRetryHandler handler = FailThenSucceed();
        RetryProbeService service = RetryContractContext.Service(handler, RetryContractContext.Immediate());

        RetryProbeResult result = await service.DeleteJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint);

        Assert.Equal("succeeded", result.Outcome);
        AssertIdenticalAttempts(handler, expectedCount: 2, HttpMethod.Delete, expectedBody: null);
    }

    [Fact]
    public async Task DeleteWithBodyHelper_ShouldRepeatTheSameBodyAndKeepTheVerb()
    {
        ScriptedRetryHandler handler = FailThenSucceed();
        RetryProbeService service = RetryContractContext.Service(handler, RetryContractContext.Immediate());

        RetryProbeResult result = await service.DeleteWithBodyAsync<RetryProbeResult>(
            RetryContractContext.Endpoint,
            RetryContractContext.Payload());

        Assert.Equal("succeeded", result.Outcome);
        AssertIdenticalAttempts(handler, expectedCount: 2, HttpMethod.Delete, RetryContractContext.ExpectedPayloadJson);
    }

    /// <summary>
    /// The decline operation has its own transport, and its own guarantees: unauthenticated, and a
    /// provider <c>302</c> is the documented success. A retry must preserve both.
    /// </summary>
    [Fact]
    public async Task Decline_ShouldRepeatTheAnonymousGetAndStillNotFollowTheRedirect()
    {
        const string Location = "https://provider.example/declined";

        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(HttpStatusCode.ServiceUnavailable, RetryContractContext.ErrorBody),
            RetryOutcomes.Redirect(Location));
        using HttpClient declineClient = RetryContractContext.Client(handler);
        PaymentInstructionService service = new(
            RetryContractContext.Configuration(RetryContractContext.Immediate()),
            RetryContractContext.Client(new ScriptedRetryHandler(RetryOutcomes.Success())),
            declineClient);

        PaymentInstructionDeclineResult result;
        using (service as IDisposable)
        {
            result = await service.DeclineAsync("project-1", "instruction-1");
        }

        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        Assert.Equal(new Uri(Location), result.Location);

        // Two attempts and no third: the redirect was returned, never followed.
        Assert.Equal(2, handler.AttemptCount);
        Assert.All(handler.Attempts, attempt =>
        {
            Assert.Equal(HttpMethod.Get, attempt.Method);
            Assert.Equal(
                $"{RetryContractContext.DeclineEndpoint}?project_id=project-1&payment_instruction_id=instruction-1",
                attempt.PathAndQuery);
            Assert.False(attempt.HasContent);
            Assert.False(attempt.HasAuthorization);
        });
    }

    // ===================== disabled and custom policies =====================

    [Fact]
    public async Task RetryDisabled_ShouldSendExactlyOneRequestEvenWithABudget()
    {
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(HttpStatusCode.ServiceUnavailable, RetryContractContext.ErrorBody));
        RetryPolicy policy = RetryContractContext.Immediate(maxRetryAttempts: 3);
        policy.Enabled = false;
        RetryProbeService service = RetryContractContext.Service(handler, policy);

        await Assert.ThrowsAsync<RozetkaPayException>(
            () => service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint));

        Assert.Equal(1, handler.AttemptCount);
    }

    [Fact]
    public async Task RetryEnabledWithZeroBudget_ShouldSendExactlyOneRequest()
    {
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(HttpStatusCode.ServiceUnavailable, RetryContractContext.ErrorBody));
        RetryProbeService service = RetryContractContext.Service(
            handler,
            RetryContractContext.Immediate(maxRetryAttempts: 0));

        await Assert.ThrowsAsync<RozetkaPayException>(
            () => service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint));

        Assert.Equal(1, handler.AttemptCount);
    }

    /// <summary>
    /// Removing a default status from the set must actually stop the repeat.
    /// </summary>
    [Fact]
    public async Task StatusRemovedFromTheCustomSet_ShouldNotBeRetried()
    {
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(HttpStatusCode.ServiceUnavailable, RetryContractContext.ErrorBody));
        RetryProbeService service = RetryContractContext.Service(
            handler,
            RetryContractContext.Immediate(
                maxRetryAttempts: 3,
                statusCodes: [HttpStatusCode.InternalServerError, HttpStatusCode.TooManyRequests]));

        await Assert.ThrowsAsync<RozetkaPayException>(
            () => service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint));

        Assert.Equal(1, handler.AttemptCount);
    }

    /// <summary>
    /// A status outside the default set must be honoured when the caller adds it, including one whose SDK
    /// exception type is not a "server error" type at all.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    public async Task StatusAddedToTheCustomSet_ShouldBeRetried(HttpStatusCode status)
    {
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(status, RetryContractContext.ErrorBody),
            RetryOutcomes.Success());
        RetryProbeService service = RetryContractContext.Service(
            handler,
            RetryContractContext.Immediate(maxRetryAttempts: 1, statusCodes: [status]));

        RetryProbeResult result = await service.PostJsonAsync<RetryProbeResult>(
            RetryContractContext.Endpoint,
            RetryContractContext.Payload());

        Assert.Equal("succeeded", result.Outcome);
        AssertIdenticalAttempts(handler, expectedCount: 2, HttpMethod.Post, RetryContractContext.ExpectedPayloadJson);
    }

    [Theory]
    [MemberData(nameof(DefaultRetriableStatuses))]
    public async Task EmptyCustomSet_ShouldRetryNoStatusAtAll(HttpStatusCode status)
    {
        ScriptedRetryHandler handler = new(RetryOutcomes.Failure(status, RetryContractContext.ErrorBody));
        RetryProbeService service = RetryContractContext.Service(
            handler,
            RetryContractContext.Immediate(maxRetryAttempts: 3, statusCodes: []));

        // ThrowsAny: each status keeps its own mapped subtype, and this case is about the attempt count.
        await Assert.ThrowsAnyAsync<RozetkaPayException>(
            () => service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint));

        Assert.Equal(1, handler.AttemptCount);
    }

    /// <summary>
    /// The decision reads the HTTP evidence, not the exception class. An SDK exception constructed by hand
    /// never came from a response, so its type must not make it retriable.
    /// </summary>
    [Fact]
    public async Task ManuallyConstructedRateLimitException_ShouldNotBeRetried()
    {
        int calls = 0;
        RetryProbeService service = RetryContractContext.Service(
            new ScriptedRetryHandler(RetryOutcomes.Success()),
            RetryContractContext.Immediate(maxRetryAttempts: 3));

        RozetkaPayRateLimitException exception = await Assert.ThrowsAsync<RozetkaPayRateLimitException>(
            () => service.ExecuteAsync<RetryProbeResult>(() =>
            {
                calls++;
                throw new RozetkaPayRateLimitException("carries no HTTP response evidence");
            }));

        Assert.Equal(1, calls);
        Assert.Null(exception.ApiError);
    }

    [Fact]
    public async Task ManuallyConstructedStatusExceptions_ShouldNotBeRetried()
    {
        RetryProbeService service = RetryContractContext.Service(
            new ScriptedRetryHandler(RetryOutcomes.Success()),
            RetryContractContext.Immediate(maxRetryAttempts: 3));

        foreach (Func<Exception> factory in new Func<Exception>[]
        {
            static () => new RozetkaPayException("no evidence"),
            static () => new RozetkaPayValidationException("no evidence"),
            static () => new RozetkaPayAuthorizationException("no evidence"),
            static () => new RozetkaPayNotFoundException("no evidence")
        })
        {
            int calls = 0;
            await Assert.ThrowsAnyAsync<RozetkaPayException>(
                () => service.ExecuteAsync<RetryProbeResult>(() =>
                {
                    calls++;
                    throw factory();
                }));

            Assert.Equal(1, calls);
        }
    }

    // ===================== exhaustion preserves the original failure =====================

    /// <summary>
    /// Exhaustion must surrender the exact status-mapped exception of the last response, with only the last
    /// response's evidence on it — never an opaque wrapper that erases both.
    /// </summary>
    [Fact]
    public async Task Exhausted429_ShouldThrowTheFinalRateLimitExceptionWithTheFinalEvidence()
    {
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(HttpStatusCode.TooManyRequests, RetryContractContext.ErrorBody));
        RetryProbeService service = RetryContractContext.Service(
            handler,
            RetryContractContext.Immediate(maxRetryAttempts: 2));

        RozetkaPayRateLimitException exception = await Assert.ThrowsAsync<RozetkaPayRateLimitException>(
            () => service.PostJsonAsync<RetryProbeResult>(
                RetryContractContext.Endpoint,
                RetryContractContext.Payload()));

        Assert.Equal(3, handler.AttemptCount);
        AssertIdenticalAttempts(handler, expectedCount: 3, HttpMethod.Post, RetryContractContext.ExpectedPayloadJson);
        AssertFinalEvidence(exception, HttpStatusCode.TooManyRequests, attempt: 3);
        Assert.Equal("Rate limit exceeded. Retry after 60 seconds", exception.Message);
    }

    [Fact]
    public async Task Exhausted400InACustomSet_ShouldThrowTheFinalValidationException()
    {
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(HttpStatusCode.BadRequest, RetryContractContext.ErrorBody));
        RetryProbeService service = RetryContractContext.Service(
            handler,
            RetryContractContext.Immediate(maxRetryAttempts: 2, statusCodes: [HttpStatusCode.BadRequest]));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(
            () => service.PostJsonAsync<RetryProbeResult>(
                RetryContractContext.Endpoint,
                RetryContractContext.Payload()));

        Assert.Equal(3, handler.AttemptCount);
        AssertFinalEvidence(exception, HttpStatusCode.BadRequest, attempt: 3);
    }

    [Fact]
    public async Task Exhausted503_ShouldThrowTheFinalServerException()
    {
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(HttpStatusCode.ServiceUnavailable, RetryContractContext.ErrorBody));
        RetryProbeService service = RetryContractContext.Service(
            handler,
            RetryContractContext.Immediate(maxRetryAttempts: 2));

        RozetkaPayException exception = await Assert.ThrowsAsync<RozetkaPayException>(
            () => service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint));

        Assert.Equal(3, handler.AttemptCount);
        AssertFinalEvidence(exception, HttpStatusCode.ServiceUnavailable, attempt: 3);
    }

    [Fact]
    public async Task Exhausted500_ShouldKeepItsFixedMessageAndFinalEvidence()
    {
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(HttpStatusCode.InternalServerError, RetryContractContext.ErrorBody));
        RetryProbeService service = RetryContractContext.Service(
            handler,
            RetryContractContext.Immediate(maxRetryAttempts: 1));

        RozetkaPayException exception = await Assert.ThrowsAsync<RozetkaPayException>(
            () => service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint));

        Assert.Equal(2, handler.AttemptCount);
        Assert.Equal("Internal server error", exception.Message);
        AssertFinalEvidence(exception, HttpStatusCode.InternalServerError, attempt: 2);
    }

    // ===================== transport failures stay compatible =====================

    [Fact]
    public async Task TransportException_ShouldStillBeRetried()
    {
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Throws(_ => new HttpRequestException(RetryContractContext.TransportMessageMarker)),
            RetryOutcomes.Success());
        RetryProbeService service = RetryContractContext.Service(handler, RetryContractContext.Immediate());

        RetryProbeResult result = await service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint);

        Assert.Equal("succeeded", result.Outcome);
        Assert.Equal(2, handler.AttemptCount);
    }

    /// <summary>
    /// A timeout surfaces as <see cref="TaskCanceledException"/> while the caller token is still live. That
    /// stays retriable — only caller cancellation does not.
    /// </summary>
    [Fact]
    public async Task TimeoutLikeCancellation_WithALiveCallerToken_ShouldStillBeRetried()
    {
        using CancellationTokenSource callerTokenSource = new();
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Throws(_ => new TaskCanceledException("the request timed out")),
            RetryOutcomes.Success());
        RetryProbeService service = RetryContractContext.Service(handler, RetryContractContext.Immediate());

        RetryProbeResult result = await service.GetJsonAsync<RetryProbeResult>(
            RetryContractContext.Endpoint,
            callerTokenSource.Token);

        Assert.Equal("succeeded", result.Outcome);
        Assert.Equal(2, handler.AttemptCount);
        Assert.False(callerTokenSource.IsCancellationRequested);
    }

    /// <summary>
    /// <see cref="RetryPolicy.ShouldRetry(Exception)"/> has always published <see cref="SocketException"/> as
    /// retriable. The runtime now matches that published policy.
    /// </summary>
    [Fact]
    public async Task SocketException_ShouldBeRetried()
    {
        int calls = 0;
        RetryProbeService service = RetryContractContext.Service(
            new ScriptedRetryHandler(RetryOutcomes.Success()),
            RetryContractContext.Immediate());

        RetryProbeResult result = await service.ExecuteAsync(() =>
        {
            calls++;
            return calls == 1
                ? throw new SocketException((int)SocketError.ConnectionReset)
                : Task.FromResult(new RetryProbeResult { Outcome = "succeeded" });
        });

        Assert.Equal("succeeded", result.Outcome);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task WrappedTransportException_ShouldStillBeRetried()
    {
        int calls = 0;
        RetryProbeService service = RetryContractContext.Service(
            new ScriptedRetryHandler(RetryOutcomes.Success()),
            RetryContractContext.Immediate());

        RetryProbeResult result = await service.ExecuteAsync(() =>
        {
            calls++;
            return calls == 1
                ? throw new RozetkaPayException(
                    "wrapped transport failure",
                    new HttpRequestException(RetryContractContext.TransportMessageMarker))
                : Task.FromResult(new RetryProbeResult { Outcome = "succeeded" });
        });

        Assert.Equal("succeeded", result.Outcome);
        Assert.Equal(2, calls);
    }

    /// <summary>
    /// An exhausted transport failure keeps its own type too: it is not re-wrapped into a generic SDK error.
    /// </summary>
    [Fact]
    public async Task ExhaustedTransportException_ShouldKeepItsOwnTypeAndMessage()
    {
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Throws(attempt => new HttpRequestException($"transport attempt {attempt}")));
        RetryProbeService service = RetryContractContext.Service(
            handler,
            RetryContractContext.Immediate(maxRetryAttempts: 2));

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint));

        Assert.Equal(3, handler.AttemptCount);
        Assert.Equal("transport attempt 3", exception.Message);
    }

    /// <summary>
    /// Only the SDK's own wrapper is unwrapped. A retryable transport exception buried inside an arbitrary
    /// outer exception is not evidence of a transport failure the SDK produced, and must not be repeated on
    /// the strength of its inner exception alone.
    /// </summary>
    [Fact]
    public async Task ArbitraryExceptionWrappingATransportFailure_ShouldNotBeRetried()
    {
        int calls = 0;
        RetryProbeService service = RetryContractContext.Service(
            new ScriptedRetryHandler(RetryOutcomes.Success()),
            RetryContractContext.Immediate(maxRetryAttempts: 3));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExecuteAsync<RetryProbeResult>(() =>
            {
                calls++;
                throw new InvalidOperationException(
                    "not an SDK wrapper",
                    new HttpRequestException(RetryContractContext.TransportMessageMarker));
            }));

        Assert.Equal(1, calls);
        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Fact]
    public async Task NonRetriableException_ShouldNotBeRetried()
    {
        int calls = 0;
        RetryProbeService service = RetryContractContext.Service(
            new ScriptedRetryHandler(RetryOutcomes.Success()),
            RetryContractContext.Immediate(maxRetryAttempts: 3));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExecuteAsync<RetryProbeResult>(() =>
            {
                calls++;
                throw new InvalidOperationException("not a transport failure");
            }));

        Assert.Equal(1, calls);
    }

    // ===================== caller cancellation is never a retry =====================

    /// <summary>
    /// The caller cancelled, and the transport reported it as a cancellation. That is the caller's decision,
    /// so it must end the operation instead of buying another attempt.
    /// </summary>
    [Fact]
    public async Task CallerCancellation_ShouldNotBuyAnotherAttempt()
    {
        using CancellationTokenSource callerTokenSource = new();
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Throws(_ => new TaskCanceledException("the caller cancelled")));
        handler.OnAttempt = (_, _) => callerTokenSource.Cancel();
        RetryProbeService service = RetryContractContext.Service(
            handler,
            RetryContractContext.Immediate(maxRetryAttempts: 3));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint, callerTokenSource.Token));

        Assert.Equal(1, handler.AttemptCount);
    }

    /// <summary>
    /// Cancelling while the retry delay is being awaited must abort the wait and never start the next request.
    /// </summary>
    /// <remarks>
    /// Deterministic without a sleep: the retry warning is written immediately before the delay is awaited,
    /// so cancelling from the log callback lands inside the wait every time.
    /// </remarks>
    [Fact]
    public async Task CancellationDuringTheRetryDelay_ShouldAbortBeforeTheNextRequest()
    {
        using CancellationTokenSource callerTokenSource = new();
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(HttpStatusCode.ServiceUnavailable, RetryContractContext.ErrorBody));
        RetryLogRecorder logger = new()
        {
            OnEntry = entry =>
            {
                if (entry.Level == LogLevel.Warning && entry.Message.StartsWith("Retry ", StringComparison.Ordinal))
                {
                    callerTokenSource.Cancel();
                }
            }
        };

        RetryPolicy policy = new()
        {
            Enabled = true,
            MaxRetryAttempts = 3,
            BaseDelay = TimeSpan.FromSeconds(30),
            MaxDelay = TimeSpan.FromSeconds(30),
            BackoffStrategy = BackoffStrategy.Fixed
        };
        RetryProbeService service = RetryContractContext.Service(handler, policy, logger);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint, callerTokenSource.Token));

        Assert.Equal(1, handler.AttemptCount);
        Assert.Single(logger.RetryWarnings);
    }

    // ===================== Retry-After =====================

    /// <summary>
    /// A <c>429</c> carrying delta-seconds waits exactly what the provider asked for, instead of the
    /// configured backoff.
    /// </summary>
    /// <remarks>
    /// The decision is asserted without paying for it. The retry warning is written immediately before the
    /// wait, so cancelling from the log callback lands between the two: the logged figure proves the hint took
    /// precedence over the configured backoff, the wait is abandoned instead of slept through, and the next
    /// request is never sent. Nothing here depends on elapsed time.
    /// </remarks>
    [Fact]
    public async Task RetryAfterDeltaSeconds_ShouldReplaceTheConfiguredBackoff()
    {
        using CancellationTokenSource callerTokenSource = new();
        RetryLogRecorder logger = new()
        {
            OnEntry = entry =>
            {
                if (entry.Level == LogLevel.Warning && entry.Message.StartsWith("Retry ", StringComparison.Ordinal))
                {
                    callerTokenSource.Cancel();
                }
            }
        };
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(
                HttpStatusCode.TooManyRequests,
                RetryContractContext.ErrorBody,
                static (response, _) => response.Headers.RetryAfter =
                    new RetryConditionHeaderValue(TimeSpan.FromSeconds(1))),
            RetryOutcomes.Success());
        RetryProbeService service = RetryContractContext.Service(handler, HintPolicy(TimeSpan.FromSeconds(30)), logger);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint, callerTokenSource.Token));

        // 1000 ms, not the 15 ms backoff, and not the 30 s cap.
        Assert.Equal([1000d], logger.RetryDelaysMilliseconds);
        Assert.Equal(1, handler.AttemptCount);
    }

    /// <summary>
    /// A provider that answers <c>Retry-After: 0</c> is asking for an immediate repeat, so the wait is zero
    /// rather than the configured backoff.
    /// </summary>
    [Fact]
    public async Task RetryAfterDeltaZero_ShouldRetryImmediately()
    {
        RetryLogRecorder logger = new();
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(
                HttpStatusCode.TooManyRequests,
                RetryContractContext.ErrorBody,
                static (response, _) => response.Headers.RetryAfter =
                    new RetryConditionHeaderValue(TimeSpan.Zero)),
            RetryOutcomes.Success());
        RetryProbeService service = RetryContractContext.Service(handler, HintPolicy(), logger);

        RetryProbeResult result = await service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint);

        Assert.Equal("succeeded", result.Outcome);
        Assert.Equal(2, handler.AttemptCount);
        Assert.Equal([0d], logger.RetryDelaysMilliseconds);
    }

    /// <summary>
    /// A future HTTP-date is honoured as a relative delay, and the existing public maximum still bounds it.
    /// </summary>
    [Fact]
    public async Task RetryAfterFutureHttpDate_ShouldBeHonouredAndCappedByMaxDelay()
    {
        RetryLogRecorder logger = new();
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(
                HttpStatusCode.TooManyRequests,
                RetryContractContext.ErrorBody,
                static (response, _) => response.Headers.RetryAfter =
                    new RetryConditionHeaderValue(DateTimeOffset.UtcNow.AddHours(1))),
            RetryOutcomes.Success());
        RetryProbeService service = RetryContractContext.Service(handler, HintPolicy(), logger);

        await service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint);

        Assert.Equal(2, handler.AttemptCount);
        Assert.Equal([CappedDelayMilliseconds], logger.RetryDelaysMilliseconds);
    }

    [Fact]
    public async Task RetryAfterPastHttpDate_ShouldRetryImmediately()
    {
        RetryLogRecorder logger = new();
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(
                HttpStatusCode.TooManyRequests,
                RetryContractContext.ErrorBody,
                static (response, _) => response.Headers.RetryAfter =
                    new RetryConditionHeaderValue(DateTimeOffset.UtcNow.AddHours(-1))),
            RetryOutcomes.Success());
        RetryProbeService service = RetryContractContext.Service(handler, HintPolicy(), logger);

        await service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint);

        Assert.Equal(2, handler.AttemptCount);
        Assert.Equal([0d], logger.RetryDelaysMilliseconds);
    }

    [Fact]
    public async Task RetryAfterAbsent_ShouldUseTheConfiguredBackoff()
    {
        RetryLogRecorder logger = new();
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(HttpStatusCode.TooManyRequests, RetryContractContext.ErrorBody),
            RetryOutcomes.Success());
        RetryProbeService service = RetryContractContext.Service(handler, HintPolicy(), logger);

        await service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint);

        Assert.Equal(2, handler.AttemptCount);
        Assert.Equal([BackoffDelayMilliseconds], logger.RetryDelaysMilliseconds);
    }

    /// <summary>
    /// A header the typed accessor cannot parse is treated as absent. It must not become a
    /// <see cref="FormatException"/> in place of the proper rate-limit exception, and it must not change the
    /// delay.
    /// </summary>
    [Fact]
    public async Task RetryAfterMalformed_ShouldFallBackToTheConfiguredBackoff()
    {
        RetryLogRecorder logger = new();
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(
                HttpStatusCode.TooManyRequests,
                RetryContractContext.ErrorBody,
                static (response, _) => response.Headers.TryAddWithoutValidation("Retry-After", "not-a-delay")));
        RetryProbeService service = RetryContractContext.Service(handler, HintPolicy(), logger);

        await Assert.ThrowsAsync<RozetkaPayRateLimitException>(
            () => service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint));

        Assert.Equal(2, handler.AttemptCount);
        Assert.Equal([BackoffDelayMilliseconds], logger.RetryDelaysMilliseconds);
        Assert.DoesNotContain("not-a-delay", string.Join('|', logger.AllText));
    }

    [Fact]
    public async Task RetryAfterLargerThanMaxDelay_ShouldBeCapped()
    {
        RetryLogRecorder logger = new();
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(
                HttpStatusCode.TooManyRequests,
                RetryContractContext.ErrorBody,
                static (response, _) => response.Headers.RetryAfter =
                    new RetryConditionHeaderValue(TimeSpan.FromHours(1))),
            RetryOutcomes.Success());
        RetryProbeService service = RetryContractContext.Service(handler, HintPolicy(), logger);

        await service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint);

        Assert.Equal([CappedDelayMilliseconds], logger.RetryDelaysMilliseconds);
    }

    /// <summary>
    /// Only a real <c>429</c> carries a server hint. A <c>Retry-After</c> on any other status is ignored.
    /// </summary>
    [Fact]
    public async Task RetryAfterOnANon429_ShouldNotInfluenceTheDelay()
    {
        RetryLogRecorder logger = new();
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(
                HttpStatusCode.ServiceUnavailable,
                RetryContractContext.ErrorBody,
                static (response, _) => response.Headers.RetryAfter =
                    new RetryConditionHeaderValue(TimeSpan.FromHours(1))),
            RetryOutcomes.Success());
        RetryProbeService service = RetryContractContext.Service(handler, HintPolicy(), logger);

        await service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint);

        Assert.Equal(2, handler.AttemptCount);
        Assert.Equal([BackoffDelayMilliseconds], logger.RetryDelaysMilliseconds);
    }

    // ===================== safe retry logging =====================

    /// <summary>
    /// The retry warning has to be actionable without becoming a leak: attempt, failure category, status, and
    /// delay only.
    /// </summary>
    [Fact]
    public async Task RetryLogging_ShouldCarryNoProviderTextCredentialOrTarget()
    {
        RetryLogRecorder logger = new();
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(HttpStatusCode.ServiceUnavailable, RetryContractContext.ErrorBody));
        RetryProbeService service = RetryContractContext.Service(
            handler,
            RetryContractContext.Immediate(maxRetryAttempts: 2),
            logger);

        await Assert.ThrowsAsync<RozetkaPayException>(
            () => service.PostJsonAsync<RetryProbeResult>(
                RetryContractContext.Endpoint,
                RetryContractContext.Payload()));

        string everything = string.Join('|', logger.AllText);
        Assert.DoesNotContain(RetryContractContext.ProviderMessageMarker, everything, StringComparison.Ordinal);
        Assert.DoesNotContain(RetryContractContext.RawBodyMarker, everything, StringComparison.Ordinal);
        Assert.DoesNotContain(RetryContractContext.RequestBodyMarker, everything, StringComparison.Ordinal);
        Assert.DoesNotContain(RetryContractContext.PasswordPlaceholder, everything, StringComparison.Ordinal);
        Assert.DoesNotContain("Basic ", everything, StringComparison.Ordinal);

        // The two retry warnings say what an operator needs and nothing else.
        Assert.Equal(2, logger.RetryWarnings.Count());
        foreach (RetryLogEntry warning in logger.RetryWarnings)
        {
            Assert.DoesNotContain(RetryContractContext.Endpoint, string.Join('|', warning.AllText), StringComparison.Ordinal);
            Assert.Contains("503", string.Join('|', warning.AllText), StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task RetryLogging_ShouldNotRenderATransportExceptionMessage()
    {
        RetryLogRecorder logger = new();
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Throws(_ => new HttpRequestException(RetryContractContext.TransportMessageMarker)),
            RetryOutcomes.Success());
        RetryProbeService service = RetryContractContext.Service(
            handler,
            RetryContractContext.Immediate(),
            logger);

        await service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint);

        string everything = string.Join('|', logger.AllText);
        Assert.DoesNotContain(RetryContractContext.TransportMessageMarker, everything, StringComparison.Ordinal);

        // The category is still reported, so the warning remains actionable.
        RetryLogEntry warning = Assert.Single(logger.RetryWarnings);
        Assert.Contains(nameof(HttpRequestException), string.Join('|', warning.AllText), StringComparison.Ordinal);
    }

    // ===================== per-attempt resource ownership =====================

    [Fact]
    public async Task Success_ShouldDisposeTheResponseAndItsContent()
    {
        ScriptedRetryHandler handler = new(RetryOutcomes.Success());
        RetryProbeService service = RetryContractContext.Service(handler);

        await service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint);

        AssertEveryResponseDisposed(handler, expectedCount: 1);
    }

    [Fact]
    public async Task RetryThenSuccess_ShouldDisposeEveryResponse()
    {
        ScriptedRetryHandler handler = FailThenSucceed();
        RetryProbeService service = RetryContractContext.Service(handler, RetryContractContext.Immediate());

        await service.PostJsonAsync<RetryProbeResult>(
            RetryContractContext.Endpoint,
            RetryContractContext.Payload());

        AssertEveryResponseDisposed(handler, expectedCount: 2);
    }

    [Fact]
    public async Task Exhaustion_ShouldDisposeEveryResponse()
    {
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(HttpStatusCode.ServiceUnavailable, RetryContractContext.ErrorBody));
        RetryProbeService service = RetryContractContext.Service(
            handler,
            RetryContractContext.Immediate(maxRetryAttempts: 2));

        await Assert.ThrowsAsync<RozetkaPayException>(
            () => service.PostJsonAsync<RetryProbeResult>(
                RetryContractContext.Endpoint,
                RetryContractContext.Payload()));

        AssertEveryResponseDisposed(handler, expectedCount: 3);
    }

    /// <summary>
    /// Every helper releases the response on the success path and on the throwing error path alike.
    /// </summary>
    [Theory]
    [InlineData("get")]
    [InlineData("post")]
    [InlineData("post-no-content")]
    [InlineData("patch")]
    [InlineData("post-without-body")]
    [InlineData("delete")]
    [InlineData("delete-with-body")]
    public async Task EveryHelper_ShouldDisposeTheResponseOnBothPaths(string helper)
    {
        foreach (bool failing in new[] { false, true })
        {
            ScriptedRetryHandler handler = failing
                ? new ScriptedRetryHandler(
                    RetryOutcomes.Failure(HttpStatusCode.ServiceUnavailable, RetryContractContext.ErrorBody))
                : new ScriptedRetryHandler(RetryOutcomes.Success());
            RetryProbeService service = RetryContractContext.Service(handler);

            if (failing)
            {
                await Assert.ThrowsAsync<RozetkaPayException>(() => Invoke(service, helper));
            }
            else
            {
                await Invoke(service, helper);
            }

            AssertEveryResponseDisposed(handler, expectedCount: 1);
        }
    }

    /// <summary>
    /// The body the SDK built for an attempt is owned by that attempt's request, so it must be disposed with
    /// it — including on the attempts that failed.
    /// </summary>
    [Fact]
    public async Task EveryAttempt_ShouldDisposeTheRequestBodyItBuilt()
    {
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(HttpStatusCode.ServiceUnavailable, RetryContractContext.ErrorBody));
        RetryProbeService service = RetryContractContext.Service(
            handler,
            RetryContractContext.Immediate(maxRetryAttempts: 2));

        await Assert.ThrowsAsync<RozetkaPayException>(
            () => service.PostJsonAsync<RetryProbeResult>(
                RetryContractContext.Endpoint,
                RetryContractContext.Payload()));

        Assert.Equal(3, handler.RequestContents.Count);
        foreach (HttpContent content in handler.RequestContents)
        {
            await Assert.ThrowsAsync<ObjectDisposedException>(() => content.ReadAsByteArrayAsync());
        }
    }

    /// <summary>
    /// Each attempt owns its own <see cref="HttpRequestMessage"/> and releases it. A single-use request object
    /// is never carried across a retry.
    /// </summary>
    [Theory]
    [InlineData("get")]
    [InlineData("post")]
    [InlineData("post-no-content")]
    [InlineData("patch")]
    [InlineData("post-without-body")]
    [InlineData("delete")]
    [InlineData("delete-with-body")]
    public async Task EveryAttempt_ShouldDisposeItsOwnRequest(string helper)
    {
        ScriptedRetryHandler handler = new(
            RetryOutcomes.Failure(HttpStatusCode.ServiceUnavailable, RetryContractContext.ErrorBody),
            RetryOutcomes.Success())
        {
            AttachRequestDisposalProbe = true
        };
        RetryProbeService service = RetryContractContext.Service(handler, RetryContractContext.Immediate());

        await Invoke(service, helper);

        Assert.Equal(2, handler.RequestProbes.Count);
        Assert.All(handler.RequestProbes, probe =>
            Assert.True(probe.Disposed, "the SDK must dispose the request message it built for the attempt."));
    }

    /// <summary>
    /// Cancellation that lands after the response has been handed to the SDK, but before its body has been
    /// read, must still release everything the attempt owns — and must not buy another attempt.
    /// </summary>
    /// <remarks>
    /// Deterministic: the handler cancels once it has recorded the request, so the response is produced and
    /// handed over and the cancellation falls on the body read rather than on the send.
    /// </remarks>
    [Fact]
    public async Task CancellationWhileReadingTheBody_ShouldReleaseTheAttemptAndNotRetry()
    {
        using CancellationTokenSource callerTokenSource = new();
        ScriptedRetryHandler handler = new(RetryOutcomes.CancellableSuccess())
        {
            AttachRequestDisposalProbe = true,
            OnAttempt = (_, _) => callerTokenSource.Cancel()
        };
        RetryProbeService service = RetryContractContext.Service(
            handler,
            RetryContractContext.Immediate(maxRetryAttempts: 3));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint, callerTokenSource.Token));

        // One attempt: a cancelled caller never buys another, whatever the budget says.
        Assert.Equal(1, handler.AttemptCount);

        CancellableTrackedResponse response = Assert.Single(handler.CancellableResponses);
        Assert.True(response.Disposed, "the response of a cancelled attempt must still be disposed.");
        Assert.True(response.TrackedContent.Disposed, "the content of a cancelled attempt must still be disposed.");

        DisposalTrackingContent probe = Assert.Single(handler.RequestProbes);
        Assert.True(probe.Disposed, "the request of a cancelled attempt must still be disposed.");
    }

    // ===================== helpers =====================

    /// <summary>
    /// Backoff for the <c>Retry-After</c> cases: a fixed value that is unmistakably not the server hint and
    /// not the cap, so the assertion can tell all three apart while the test still runs in milliseconds.
    /// </summary>
    private const double BackoffDelayMilliseconds = 15d;

    private const double CappedDelayMilliseconds = 40d;

    private static RetryPolicy HintPolicy(TimeSpan? maxDelay = null)
    {
        return new RetryPolicy
        {
            Enabled = true,
            MaxRetryAttempts = 1,
            BaseDelay = TimeSpan.FromMilliseconds(BackoffDelayMilliseconds),
            MaxDelay = maxDelay ?? TimeSpan.FromMilliseconds(CappedDelayMilliseconds),
            BackoffStrategy = BackoffStrategy.Fixed
        };
    }

    private static ScriptedRetryHandler FailThenSucceed(
        HttpStatusCode status = HttpStatusCode.ServiceUnavailable)
    {
        return new ScriptedRetryHandler(
            RetryOutcomes.Failure(status, RetryContractContext.ErrorBody),
            RetryOutcomes.Success());
    }

    private static Task Invoke(RetryProbeService service, string helper)
    {
        return helper switch
        {
            "get" => service.GetJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint),
            "post" => service.PostJsonAsync<RetryProbeResult>(
                RetryContractContext.Endpoint,
                RetryContractContext.Payload()),
            "post-no-content" => service.PostJsonAllowingNoContentAsync(
                RetryContractContext.Endpoint,
                RetryContractContext.Payload()),
            "patch" => service.PatchJsonAsync<RetryProbeResult>(
                RetryContractContext.Endpoint,
                RetryContractContext.Payload()),
            "post-without-body" => service.PostWithoutBodyJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint),
            "delete" => service.DeleteJsonAsync<RetryProbeResult>(RetryContractContext.Endpoint),
            "delete-with-body" => service.DeleteWithBodyAsync<RetryProbeResult>(
                RetryContractContext.Endpoint,
                RetryContractContext.Payload()),
            _ => throw new ArgumentOutOfRangeException(nameof(helper), helper, "Unknown transport helper.")
        };
    }

    /// <summary>
    /// Every attempt was the same request: same verb, same concrete target, same body bytes, same content
    /// type.
    /// </summary>
    private static void AssertIdenticalAttempts(
        ScriptedRetryHandler handler,
        int expectedCount,
        HttpMethod expectedMethod,
        string? expectedBody)
    {
        Assert.Equal(expectedCount, handler.Attempts.Count);

        foreach (RetryAttempt attempt in handler.Attempts)
        {
            Assert.Equal(expectedMethod, attempt.Method);
            Assert.Equal(RetryContractContext.Endpoint, attempt.PathAndQuery);

            if (expectedBody is null)
            {
                Assert.False(attempt.HasContent);
                Assert.Null(attempt.ContentType);
            }
            else
            {
                Assert.True(attempt.HasContent);
                Assert.Equal(expectedBody, attempt.Body);
                Assert.Equal(RetryContractContext.JsonContentType, attempt.ContentType);
            }
        }

        // Byte-for-byte, not merely equal as text.
        foreach (RetryAttempt attempt in handler.Attempts.Skip(1))
        {
            Assert.Equal(handler.Attempts[0].BodyBytes, attempt.BodyBytes);
        }
    }

    /// <summary>
    /// The exception carries the evidence of the last response only.
    /// </summary>
    private static void AssertFinalEvidence(RozetkaPayException exception, HttpStatusCode status, int attempt)
    {
        Assert.NotNull(exception.ApiError);
        Assert.Equal(status, exception.ApiError!.StatusCode);
        Assert.Equal($"attempt_{attempt}_code", exception.ApiError.Code);
        Assert.Equal($"attempt-{attempt}-request-id", exception.ApiError.RequestId);
        Assert.Equal(RetryContractContext.ErrorBody(attempt), exception.ApiError.RawBody);

        // No opaque wrapper: neither the message nor an inner exception replaced the mapped failure.
        Assert.DoesNotContain("Request failed after", exception.Message, StringComparison.Ordinal);
        Assert.Null(exception.InnerException);
    }

    private static void AssertEveryResponseDisposed(ScriptedRetryHandler handler, int expectedCount)
    {
        Assert.Equal(expectedCount, handler.Responses.Count);
        Assert.All(handler.Responses, response =>
        {
            Assert.True(response.Disposed, "the SDK must dispose the HttpResponseMessage of every attempt.");
            Assert.True(response.TrackedContent.Disposed, "the SDK must dispose the response content.");
        });
    }
}
