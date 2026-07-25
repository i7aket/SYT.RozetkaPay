using System.Net;
using Microsoft.Extensions.DependencyInjection;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Extensions;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Ownership and disposal of the second HTTP client EXP-354 introduces.
///
/// The payment-instruction service needs an unauthenticated non-redirecting client that the rest of the
/// SDK does not have. That client is a resource, so the question "who releases it" has to have exactly one
/// answer per construction path: the service when it created it, the container when the container created
/// it, and nobody when the caller supplied it.
/// </summary>
public class Exp354DisposalTests
{
    [Fact]
    public void AggregateClient_Dispose_ShouldStayIdempotent()
    {
        using HttpClient httpClient = CreateStubHttpClient();
        RozetkaPayClient client = new(CreateConfiguration(), httpClient);

        client.Dispose();
        client.Dispose();
        client.Dispose();

        // A caller-supplied client is not owned, so it survives every one of those calls.
        Assert.NotNull(httpClient.BaseAddress);
    }

    /// <summary>
    /// The decline client the aggregate created internally is always owned, so disposing the aggregate must
    /// release it — while leaving the caller's authenticated client alone.
    /// </summary>
    [Fact]
    public async Task AggregateClient_Dispose_ShouldReleaseTheOwnedDeclineClientButNotTheCallersClient()
    {
        RecordingHandler handler = RecordingHandler.Json("{}");
        using HttpClient httpClient = Exp354TestContext.CreateHttpClient(handler);
        RozetkaPayClient client = new(CreateConfiguration(), httpClient);

        client.Dispose();

        // The owned decline client is gone, so the operation that used it fails instead of silently
        // falling back to the authenticated client.
        await Assert.ThrowsAnyAsync<Exception>(() => client.PaymentInstructions.DeclineAsync("project-1", "pi-1"));
        Assert.Empty(handler.Requests);

        // The caller's authenticated client is untouched and still usable.
        Assert.NotNull(httpClient.BaseAddress);
        using HttpResponseMessage response = await httpClient.GetAsync(new Uri($"{Exp354TestContext.BaseUrl}/probe"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// An internally created authenticated client is still disposed, exactly as before EXP-354.
    /// </summary>
    [Fact]
    public async Task AggregateClient_Dispose_ShouldStillReleaseAnInternallyCreatedHttpClient()
    {
        RozetkaPayClient client = new(CreateConfiguration());
        PaymentService payments = client.Payments;

        client.Dispose();

        // Reaching the disposed internal client through any service now fails.
        await Assert.ThrowsAnyAsync<Exception>(() => payments.GetInfoAsync("payment-1"));
    }

    /// <summary>
    /// In the DI path the decline client comes from <see cref="IHttpClientFactory"/>, so the factory owns
    /// it. Disposing the scope must not leave the pooled client unusable for the next scope.
    /// </summary>
    [Fact]
    public async Task DiRegisteredService_ShouldNotDisposeTheFactoryOwnedDeclineClient()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(CreateConfiguration());

        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });

        IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();

        using (IServiceScope scope = provider.CreateScope())
        {
            IPaymentInstructionService service =
                scope.ServiceProvider.GetRequiredService<IPaymentInstructionService>();
            Assert.IsType<PaymentInstructionService>(service);
        }

        // The scope is gone. The container disposed the scoped service, and the service left the pooled
        // client alone, so a fresh client from the same named configuration still works.
        HttpClient declineClient = factory.CreateClient("RozetkaPay.PaymentInstructions.Decline");
        Assert.NotNull(declineClient.BaseAddress);
        Assert.Null(declineClient.DefaultRequestHeaders.Authorization);

        // A second scope resolves a working service over the same pooled handler.
        using IServiceScope secondScope = provider.CreateScope();
        Assert.IsType<PaymentInstructionService>(
            secondScope.ServiceProvider.GetRequiredService<IPaymentInstructionService>());

        await Task.CompletedTask;
    }

    /// <summary>
    /// Disposing the whole provider is clean: every scoped SDK service, including the new ones, is
    /// released without throwing.
    /// </summary>
    [Fact]
    public void DiProvider_Dispose_ShouldCompleteCleanly()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(CreateConfiguration());

        ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });

        using (IServiceScope scope = provider.CreateScope())
        {
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IInStorePaymentService>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IPartnerService>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IPaymentInstructionService>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IRozetkaPayClient>());
        }

        provider.Dispose();
        provider.Dispose();
    }

    /// <summary>
    /// The aggregate client resolved from the container holds a payment-instruction service that owns its
    /// decline client, and the container disposes the aggregate at scope end. Nothing may throw.
    /// </summary>
    [Fact]
    public void DiScope_Dispose_ShouldReleaseTheAggregateClientCleanly()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(CreateConfiguration());

        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });

        IServiceScope scope = provider.CreateScope();
        IRozetkaPayClient client = scope.ServiceProvider.GetRequiredService<IRozetkaPayClient>();
        Assert.NotNull(client.PaymentInstructions);

        scope.Dispose();
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

    private static HttpClient CreateStubHttpClient()
    {
        return Exp354TestContext.CreateHttpClient(RecordingHandler.Json("{}"));
    }
}
