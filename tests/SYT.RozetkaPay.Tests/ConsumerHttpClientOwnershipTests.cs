using System.Net;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// The SDK accepts a caller-supplied <see cref="HttpClient"/>, and writing to it is not the SDK's to
/// do.
/// </summary>
/// <remarks>
/// <para>
/// That client may be pooled by <c>IHttpClientFactory</c> and shared with the rest of the application.
/// Setting <see cref="HttpClient.BaseAddress"/> and <see cref="HttpClient.Timeout"/> on it changed
/// configuration the caller never asked to change — and once the client has served a request the
/// runtime forbids the write outright, so the SDK simply threw.
/// </para>
/// <para>
/// EXP-341 already moved header state off the shared client for exactly this reason. These two
/// properties were the part it missed.
/// </para>
/// </remarks>
public class ConsumerHttpClientOwnershipTests
{
    [Fact]
    public void Construction_ShouldNotRewriteTheCallersBaseAddressOrTimeout()
    {
        using HttpClient consumerClient = new(RespondWithEmptyJson())
        {
            BaseAddress = new Uri("https://consumer.example/"),
            Timeout = TimeSpan.FromSeconds(5),
        };

        using RozetkaPayClient client = new(Configuration(), consumerClient);

        Assert.Equal(new Uri("https://consumer.example/"), consumerClient.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(5), consumerClient.Timeout);
    }

    [Fact]
    public async Task Construction_ShouldSucceedOnAClientThatHasAlreadySentARequest()
    {
        using HttpClient consumerClient = new(RespondWithEmptyJson());
        await consumerClient.GetAsync("https://consumer.example/warmup");

        // Before this change: InvalidOperationException, "This instance has already started one or
        // more requests. Properties can only be modified before sending the first request."
        using RozetkaPayClient client = new(Configuration(), consumerClient);

        Assert.NotNull(client);
    }

    [Fact]
    public async Task Requests_ShouldGoToTheConfiguredEndpoint_NotTheClientsBaseAddress()
    {
        StubHttpMessageHandler handler = RespondWithEmptyJson();

        // A base address pointing somewhere else entirely: if the SDK still leaned on the client to
        // resolve targets, the request would land there.
        using HttpClient consumerClient = new(handler)
        {
            BaseAddress = new Uri("https://consumer.example/"),
        };

        PaymentService service = new(Configuration(), consumerClient);
        await service.GetInfoAsync("order-1");

        Assert.StartsWith(
            "https://api.rozetkapay.com/",
            handler.LastRequest!.RequestUri!.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ATimeout_ShouldStillEndTheCall_WithoutTheClientCarryingIt()
    {
        StubHttpMessageHandler handler = new(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using HttpClient consumerClient = new(handler);

        RozetkaPayConfiguration configuration = Configuration();
        configuration.Timeout = TimeSpan.FromMilliseconds(50);

        PaymentService service = new(configuration, consumerClient);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetInfoAsync("order-1"));
    }

    [Fact]
    public async Task CallerCancellation_ShouldStillCarryTheCallersOwnToken()
    {
        // The timeout is applied through a token linked to the caller's, so the two must stay
        // distinguishable: a caller who cancels gets their own token back, not the SDK's.
        StubHttpMessageHandler handler = new(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using HttpClient consumerClient = new(handler);
        PaymentService service = new(Configuration(), consumerClient);

        using CancellationTokenSource callerTokenSource = new();
        Task call = service.GetInfoAsync("order-1", callerTokenSource.Token);
        await callerTokenSource.CancelAsync();

        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => call);

        Assert.Equal(callerTokenSource.Token, failure.CancellationToken);
    }

    private static StubHttpMessageHandler RespondWithEmptyJson()
    {
        return new StubHttpMessageHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}"),
            }));
    }

    private static RozetkaPayConfiguration Configuration() => new()
    {
        BaseUrl = RozetkaPayOptions.ProductionBaseUrl,
        Login = "test-login",
        Password = "test-password",
        Timeout = TimeSpan.FromSeconds(30),
        RetryPolicy = RetryPolicy.None,
    };
}
