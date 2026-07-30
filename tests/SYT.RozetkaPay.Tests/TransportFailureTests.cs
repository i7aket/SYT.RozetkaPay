using System.Net;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// A request that never got an answer says so, in the hierarchy the caller is told to catch.
/// </summary>
/// <remarks>
/// <para>
/// This is the failure that can leave money in an unknown state, and it was reported worst of all. A
/// timeout escaped as a bare <c>TaskCanceledException</c>, outside <c>RozetkaPayException</c>
/// entirely, so a caller following the documented error handling did not catch it — it left their
/// handler as an unhandled 500 while a payment may have existed.
/// </para>
/// <para>
/// Worse, the retry policy treated it as retriable. <c>TaskCanceledException</c> is exactly what the
/// SDK's own timeout raises, so one ambiguous payment creation became four real POSTs, protected only
/// by the provider's per-<c>external_id</c> promise, with nothing said to the caller.
/// </para>
/// </remarks>
public class TransportFailureTests
{
    [Fact]
    public async Task ATimeout_ShouldSurfaceInsideTheDocumentedHierarchy()
    {
        using HttpClient httpClient = new(Hangs());
        PaymentService service = new(Configuration(TimeSpan.FromMilliseconds(80)), httpClient);

        RozetkaPayTransportException failure =
            await Assert.ThrowsAsync<RozetkaPayTransportException>(() => service.CreateAsync(Body()));

        Assert.True(failure.IsTimeout);
        Assert.True(failure.MayHaveReachedProvider);

        // The whole point: the documented catch clause sees it.
        Assert.IsAssignableFrom<RozetkaPayException>(failure);
    }

    /// <summary>
    /// A dispatched request is not repeated because it timed out.
    /// </summary>
    /// <remarks>
    /// A connect failure and a timeout after dispatch are different risks. The first cost nothing to
    /// repeat; the second may already have taken the money.
    /// </remarks>
    [Fact]
    public async Task ATimeout_ShouldNotBeRetried()
    {
        int dispatched = 0;
        using HttpClient httpClient = new(Hangs(() => dispatched++));

        PaymentService service = new(
            Configuration(TimeSpan.FromMilliseconds(80), RetryPolicy.Standard), httpClient);

        RozetkaPayTransportException failure =
            await Assert.ThrowsAsync<RozetkaPayTransportException>(() => service.CreateAsync(Body()));

        Assert.Equal(1, dispatched);
        Assert.Equal(1, failure.AttemptsDispatched);
    }

    /// <summary>
    /// The caller's own cancellation stays the caller's, carrying their token.
    /// </summary>
    /// <remarks>
    /// This is EXP-357's contract and it must survive the change. A caller has to be able to tell "I
    /// stopped this" from "this did not finish" — the first needs no reconciliation and the second
    /// does.
    /// </remarks>
    [Fact]
    public async Task CallerCancellation_ShouldNotBecomeATransportFailure()
    {
        using CancellationTokenSource caller = new();
        using HttpClient httpClient = new(Hangs(() => caller.Cancel()));

        PaymentService service = new(Configuration(TimeSpan.FromSeconds(30)), httpClient);

        OperationCanceledException cancelled = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.CreateAsync(Body(), caller.Token));

        Assert.IsNotType<RozetkaPayTransportException>(cancelled);
        Assert.Equal(caller.Token, cancelled.CancellationToken);
    }

    /// <summary>
    /// A transport failure that is not a timeout reports itself as one — and still says how many
    /// times it went out.
    /// </summary>
    [Fact]
    public async Task ATransportFailure_ShouldReportHowManyTimesItWasDispatched()
    {
        int dispatched = 0;
        StubHttpMessageHandler handler = new((_, _) =>
        {
            dispatched++;

            throw new HttpRequestException("socket closed");
        });

        using HttpClient httpClient = new(handler);
        PaymentService service = new(Configuration(TimeSpan.FromSeconds(5), RetryPolicy.Standard), httpClient);

        RozetkaPayTransportException failure =
            await Assert.ThrowsAsync<RozetkaPayTransportException>(() => service.CreateAsync(Body()));

        Assert.False(failure.IsTimeout);
        Assert.Equal(dispatched, failure.AttemptsDispatched);
        Assert.True(failure.AttemptsDispatched > 1, "a connect failure is still worth repeating");
    }

    private static StubHttpMessageHandler Hangs(Action? onDispatch = null)
    {
        return new StubHttpMessageHandler(async (_, token) =>
        {
            onDispatch?.Invoke();
            await Task.Delay(Timeout.Infinite, token);

            return new HttpResponseMessage(HttpStatusCode.OK);
        });
    }

    private static CreatePaymentRequest Body() => new()
    {
        Amount = 1m,
        Currency = "UAH",
        ExternalId = "order-1",
        Mode = PaymentMode.Hosted,
    };

    private static RozetkaPayConfiguration Configuration(TimeSpan timeout, RetryPolicy? retryPolicy = null) => new()
    {
        BaseUrl = RozetkaPayOptions.ProductionBaseUrl,
        Login = "probe-login",
        Password = "probe-password",
        Timeout = timeout,
        RetryPolicy = retryPolicy ?? RetryPolicy.None,
    };
}
