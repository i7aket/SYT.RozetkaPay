using System.Net;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SYT.RozetkaPay;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Extensions;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

public class CriticalFixesTests
{
    [Fact]
    public async Task BaseService_DeleteAsync_ShouldHandleNoContentResponse()
    {
        StubHttpMessageHandler handler = new(async (_, _) => new HttpResponseMessage(HttpStatusCode.NoContent));
        SubscriptionService service = new(CreateConfiguration(), CreateHttpClient(handler));

        await service.DeactivatePlanAsync("plan-1");

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Equal("/api/subscriptions/v1/plans/plan-1", handler.LastRequest.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task BaseService_BadRequest_ShouldNotExposeRawErrorPayload()
    {
        StubHttpMessageHandler handler = new(async (_, _) => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""
                {"message":"Invalid card number","details":{"card":"4111111111111111"}}
                """)
        });
        PaymentService service = new(CreateConfiguration(), CreateHttpClient(handler));

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.RetryRefundAsync(new RetryRefundRequest { ExternalId = "payment-1" }));

        Assert.Contains("Invalid card number", exception.Message);
        Assert.DoesNotContain("4111111111111111", exception.Message);
        Assert.DoesNotContain("Full response", exception.Message);
    }

    [Fact]
    public async Task BaseService_Logger_ShouldNotLogRequestBody()
    {
        StubHttpMessageHandler handler = new(async (_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {"external_id":"secret-value-123","is_success":true,"action_required":false}
                """)
        });

        TestLogger<PaymentService> logger = new();
        PaymentService service = new(CreateConfiguration(), CreateHttpClient(handler), logger);

        await service.RetryRefundAsync(new RetryRefundRequest { ExternalId = "secret-value-123" });

        Assert.DoesNotContain(logger.Messages, message => message.Contains("secret-value-123", StringComparison.Ordinal));
    }

    [Fact]
    public void ServiceCollectionExtensions_ConfigObject_ShouldPreserveAllConfigurationFields()
    {
        RetryPolicy retryPolicy = new()
        {
            Enabled = true,
            MaxRetryAttempts = 5,
            BaseDelay = TimeSpan.FromSeconds(2),
            MaxDelay = TimeSpan.FromSeconds(15),
            BackoffStrategy = BackoffStrategy.Linear,
            RetriableStatusCodes = new HashSet<HttpStatusCode> { HttpStatusCode.TooManyRequests, HttpStatusCode.BadGateway }
        };

        RozetkaPayConfiguration source = new()
        {
            BaseUrl = "https://api-epdev.rozetkapay.com",
            Login = "login",
            Password = "password",
            OnBehalfOf = "merchant-child",
            CustomerAuth = "auth-token",
            Timeout = TimeSpan.FromSeconds(77),
            UserAgent = "RozetkaPaySDK.Tests",
            ValidateSslCertificate = false,
            RetryPolicy = retryPolicy
        };

        ServiceCollection services = new();
        services.AddRozetkaPay(source);
        ServiceProvider provider = services.BuildServiceProvider();

        RozetkaPayConfiguration resolved = provider.GetRequiredService<RozetkaPayConfiguration>();

        Assert.Equal(source.BaseUrl, resolved.BaseUrl);
        Assert.Equal(source.Login, resolved.Login);
        Assert.Equal(source.Password, resolved.Password);
        Assert.Equal(source.OnBehalfOf, resolved.OnBehalfOf);
        Assert.Equal(source.CustomerAuth, resolved.CustomerAuth);
        Assert.Equal(source.Timeout, resolved.Timeout);
        Assert.Equal(source.UserAgent, resolved.UserAgent);
        Assert.Equal(source.ValidateSslCertificate, resolved.ValidateSslCertificate);
        Assert.Equal(source.RetryPolicy.Enabled, resolved.RetryPolicy.Enabled);
        Assert.Equal(source.RetryPolicy.MaxRetryAttempts, resolved.RetryPolicy.MaxRetryAttempts);
        Assert.Equal(source.RetryPolicy.BaseDelay, resolved.RetryPolicy.BaseDelay);
        Assert.Equal(source.RetryPolicy.MaxDelay, resolved.RetryPolicy.MaxDelay);
        Assert.Equal(source.RetryPolicy.BackoffStrategy, resolved.RetryPolicy.BackoffStrategy);
        Assert.Equal(source.RetryPolicy.RetriableStatusCodes, resolved.RetryPolicy.RetriableStatusCodes);
    }

    [Fact]
    public void ServiceCollectionExtensions_IConfiguration_ShouldBindNestedConfiguration()
    {
        Dictionary<string, string?> settings = new()
        {
            ["RozetkaPay:BaseUrl"] = "https://api-epdev.rozetkapay.com",
            ["RozetkaPay:Login"] = "cfg-login",
            ["RozetkaPay:Password"] = "cfg-password",
            ["RozetkaPay:OnBehalfOf"] = "cfg-child",
            ["RozetkaPay:CustomerAuth"] = "cfg-auth",
            ["RozetkaPay:UserAgent"] = "CfgAgent/1.0",
            ["RozetkaPay:ValidateSslCertificate"] = "false",
            ["RozetkaPay:Timeout"] = "00:00:45",
            ["RozetkaPay:RetryPolicy:Enabled"] = "true",
            ["RozetkaPay:RetryPolicy:MaxRetryAttempts"] = "3",
            ["RozetkaPay:RetryPolicy:BaseDelay"] = "00:00:02",
            ["RozetkaPay:RetryPolicy:MaxDelay"] = "00:00:11",
            ["RozetkaPay:RetryPolicy:BackoffStrategy"] = "Linear",
            ["RozetkaPay:RetryPolicy:RetriableStatusCodes:0"] = "TooManyRequests",
            ["RozetkaPay:RetryPolicy:RetriableStatusCodes:1"] = "InternalServerError"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        ServiceCollection services = new();
        services.AddRozetkaPay(configuration);
        ServiceProvider provider = services.BuildServiceProvider();

        RozetkaPayConfiguration resolved = provider.GetRequiredService<RozetkaPayConfiguration>();

        Assert.Equal("https://api-epdev.rozetkapay.com", resolved.BaseUrl);
        Assert.Equal("cfg-login", resolved.Login);
        Assert.Equal("cfg-password", resolved.Password);
        Assert.Equal("cfg-child", resolved.OnBehalfOf);
        Assert.Equal("cfg-auth", resolved.CustomerAuth);
        Assert.Equal("CfgAgent/1.0", resolved.UserAgent);
        Assert.False(resolved.ValidateSslCertificate);
        Assert.Equal(TimeSpan.FromSeconds(45), resolved.Timeout);
        Assert.True(resolved.RetryPolicy.Enabled);
        Assert.Equal(3, resolved.RetryPolicy.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(2), resolved.RetryPolicy.BaseDelay);
        Assert.Equal(TimeSpan.FromSeconds(11), resolved.RetryPolicy.MaxDelay);
        Assert.Equal(BackoffStrategy.Linear, resolved.RetryPolicy.BackoffStrategy);
        Assert.Contains(HttpStatusCode.TooManyRequests, resolved.RetryPolicy.RetriableStatusCodes);
        Assert.Contains(HttpStatusCode.InternalServerError, resolved.RetryPolicy.RetriableStatusCodes);
    }

    [Fact]
    public async Task RozetkaPayClient_Dispose_ShouldNotDisposeExternalHttpClient()
    {
        StubHttpMessageHandler handler = new(async (_, _) => new HttpResponseMessage(HttpStatusCode.OK));
        using HttpClient externalHttpClient = CreateHttpClient(handler);
        using RozetkaPayClient client = new(CreateConfiguration(), externalHttpClient);

        client.Dispose();

        HttpResponseMessage response = await externalHttpClient.GetAsync("https://example.org/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RozetkaPayClient_Dispose_ShouldDisposeOwnedHttpClient()
    {
        RozetkaPayClient client = new(CreateConfiguration());
        FieldInfo? field = typeof(RozetkaPayClient).GetField("HttpClient", BindingFlags.Instance | BindingFlags.NonPublic);
        HttpClient internalHttpClient = Assert.IsType<HttpClient>(field?.GetValue(client));

        client.Dispose();
        client.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => internalHttpClient.GetAsync("https://example.org/health"));
    }

    private static RozetkaPayConfiguration CreateConfiguration()
    {
        return new RozetkaPayConfiguration
        {
            BaseUrl = "https://api.rozetkapay.com",
            Login = "login",
            Password = "password",
            RetryPolicy = RetryPolicy.None
        };
    }

    private static HttpClient CreateHttpClient(StubHttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.rozetkapay.com")
        };
    }
}
