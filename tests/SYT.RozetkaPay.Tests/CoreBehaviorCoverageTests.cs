using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Converters;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Extensions;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

public class FlexibleConvertersTests
{
    [Theory]
    [InlineData("{\"value\":123}", 123)]
    [InlineData("{\"value\":\"123\"}", 123)]
    [InlineData("{\"value\":\"123.0\"}", 123)]
    [InlineData("{\"value\":null}", 0)]
    [InlineData("{\"value\":\"\"}", 0)]
    public void FlexibleInt32Converter_ShouldReadCompatibleValues(string json, int expected)
    {
        Int32Payload payload = Deserialize<Int32Payload>(json);
        Assert.Equal(expected, payload.Value);
    }

    [Fact]
    public void FlexibleInt32Converter_ShouldThrowOnPrecisionLoss()
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            Deserialize<Int32Payload>("{\"value\":\"123.5\"}"));

        Assert.Contains("precision loss", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FlexibleInt32Converter_ShouldThrowOnUnexpectedToken()
    {
        Assert.Throws<JsonException>(() => Deserialize<Int32Payload>("{\"value\":true}"));
    }

    [Theory]
    [InlineData("{\"value\":123}", 123)]
    [InlineData("{\"value\":\"123\"}", 123)]
    [InlineData("{\"value\":\"123.0\"}", 123)]
    [InlineData("{\"value\":null}", null)]
    [InlineData("{\"value\":\"\"}", null)]
    [InlineData("{\"value\":\"   \"}", null)]
    public void FlexibleNullableInt32Converter_ShouldReadCompatibleValues(string json, int? expected)
    {
        NullableInt32Payload payload = Deserialize<NullableInt32Payload>(json);
        Assert.Equal(expected, payload.Value);
    }

    [Fact]
    public void FlexibleNullableInt32Converter_ShouldThrowOnPrecisionLoss()
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            Deserialize<NullableInt32Payload>("{\"value\":\"123.5\"}"));

        Assert.Contains("precision loss", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("{\"value\":922337203685477580}", 922337203685477580L)]
    [InlineData("{\"value\":\"922337203685477580\"}", 922337203685477580L)]
    [InlineData("{\"value\":\"922337203685477580.0\"}", 922337203685477580L)]
    [InlineData("{\"value\":null}", 0L)]
    [InlineData("{\"value\":\"\"}", 0L)]
    public void FlexibleInt64Converter_ShouldReadCompatibleValues(string json, long expected)
    {
        Int64Payload payload = Deserialize<Int64Payload>(json);
        Assert.Equal(expected, payload.Value);
    }

    [Fact]
    public void FlexibleInt64Converter_ShouldThrowOnPrecisionLoss()
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            Deserialize<Int64Payload>("{\"value\":\"42.5\"}"));

        Assert.Contains("precision loss", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("{\"value\":922337203685477580}", 922337203685477580L)]
    [InlineData("{\"value\":\"922337203685477580\"}", 922337203685477580L)]
    [InlineData("{\"value\":\"922337203685477580.0\"}", 922337203685477580L)]
    [InlineData("{\"value\":null}", null)]
    [InlineData("{\"value\":\"\"}", null)]
    [InlineData("{\"value\":\"  \"}", null)]
    public void FlexibleNullableInt64Converter_ShouldReadCompatibleValues(string json, long? expected)
    {
        NullableInt64Payload payload = Deserialize<NullableInt64Payload>(json);
        Assert.Equal(expected, payload.Value);
    }

    [Fact]
    public void FlexibleNullableInt64Converter_ShouldThrowOnPrecisionLoss()
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            Deserialize<NullableInt64Payload>("{\"value\":\"42.5\"}"));

        Assert.Contains("precision loss", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("{\"value\":12.34}", "12.34")]
    [InlineData("{\"value\":\"12.34\"}", "12.34")]
    [InlineData("{\"value\":null}", null)]
    [InlineData("{\"value\":\"\"}", null)]
    public void FlexibleDecimalConverter_ShouldReadCompatibleValues(string json, string? expected)
    {
        DecimalNullablePayload payload = Deserialize<DecimalNullablePayload>(json);
        decimal? expectedDecimal = expected is null
            ? null
            : decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(expectedDecimal, payload.Value);
    }

    [Theory]
    [InlineData("{\"value\":12.34}", "12.34")]
    [InlineData("{\"value\":\"12.34\"}", "12.34")]
    [InlineData("{\"value\":null}", "0")]
    [InlineData("{\"value\":\"\"}", "0")]
    public void FlexibleDecimalNonNullableConverter_ShouldReadCompatibleValues(string json, string expected)
    {
        DecimalPayload payload = Deserialize<DecimalPayload>(json);
        decimal expectedDecimal = decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(expectedDecimal, payload.Value);
    }

    [Fact]
    public void FlexibleDecimalConverters_ShouldThrowOnInvalidValues()
    {
        Assert.Throws<JsonException>(() => Deserialize<DecimalNullablePayload>("{\"value\":\"abc\"}"));
        Assert.Throws<JsonException>(() => Deserialize<DecimalPayload>("{\"value\":\"abc\"}"));
        Assert.Throws<JsonException>(() => Deserialize<DecimalNullablePayload>("{\"value\":false}"));
    }

    [Fact]
    public void FlexibleDecimalConverters_ShouldWriteNumbersAndNulls()
    {
        string withValue = JsonSerializer.Serialize(new DecimalNullablePayload { Value = 12.34m });
        string withNull = JsonSerializer.Serialize(new DecimalNullablePayload { Value = null });
        string nonNullable = JsonSerializer.Serialize(new DecimalPayload { Value = 99.01m });

        Assert.Contains("\"value\":12.34", withValue, StringComparison.Ordinal);
        Assert.Contains("\"value\":null", withNull, StringComparison.Ordinal);
        Assert.Contains("\"value\":99.01", nonNullable, StringComparison.Ordinal);
    }

    [Fact]
    public void FlexibleDateTimeConverter_ShouldReadIsoAndCustomFormatsAndUnixTimestamp()
    {
        DateTimePayload iso = Deserialize<DateTimePayload>("{\"value\":\"2026-02-28T10:20:30Z\"}");
        DateTimePayload custom = Deserialize<DateTimePayload>("{\"value\":\"28.02.2026 10:20:30\"}");
        DateTimePayload timestamp = Deserialize<DateTimePayload>("{\"value\":1700000000}");

        Assert.Equal(new DateTime(2026, 2, 28, 10, 20, 30, DateTimeKind.Utc), iso.Value);
        Assert.Equal(new DateTime(2026, 2, 28, 10, 20, 30, DateTimeKind.Utc), custom.Value);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1700000000).DateTime, timestamp.Value);
    }

    [Fact]
    public void FlexibleDateTimeConverter_ShouldHandleEmptyAndInvalidValues()
    {
        DateTimePayload empty = Deserialize<DateTimePayload>("{\"value\":\"\"}");
        Assert.Equal(DateTime.MinValue, empty.Value);

        Assert.Throws<JsonException>(() => Deserialize<DateTimePayload>("{\"value\":\"not-a-date\"}"));
        Assert.Throws<JsonException>(() => Deserialize<DateTimePayload>("{\"value\":true}"));
    }

    [Fact]
    public void NullableFlexibleDateTimeConverter_ShouldHandleNullAndWriteValues()
    {
        NullableDateTimePayload withNull = Deserialize<NullableDateTimePayload>("{\"value\":null}");
        NullableDateTimePayload withCustomFormat = Deserialize<NullableDateTimePayload>("{\"value\":\"2026-02-28\"}");

        Assert.Null(withNull.Value);
        Assert.Equal(new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc), withCustomFormat.Value);

        string serializedValue = JsonSerializer.Serialize(new NullableDateTimePayload
        {
            Value = new DateTime(2026, 2, 28, 1, 2, 3, 456, DateTimeKind.Utc)
        });
        string serializedNull = JsonSerializer.Serialize(new NullableDateTimePayload { Value = null });

        Assert.Contains("\"value\":\"2026-02-28T01:02:03.456Z\"", serializedValue, StringComparison.Ordinal);
        Assert.Contains("\"value\":null", serializedNull, StringComparison.Ordinal);
    }

    private static T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json)!;
    }

    private sealed class Int32Payload
    {
        [JsonPropertyName("value")]
        [JsonConverter(typeof(FlexibleInt32Converter))]
        public int Value { get; set; }
    }

    private sealed class NullableInt32Payload
    {
        [JsonPropertyName("value")]
        [JsonConverter(typeof(FlexibleNullableInt32Converter))]
        public int? Value { get; set; }
    }

    private sealed class Int64Payload
    {
        [JsonPropertyName("value")]
        [JsonConverter(typeof(FlexibleInt64Converter))]
        public long Value { get; set; }
    }

    private sealed class NullableInt64Payload
    {
        [JsonPropertyName("value")]
        [JsonConverter(typeof(FlexibleNullableInt64Converter))]
        public long? Value { get; set; }
    }

    private sealed class DecimalNullablePayload
    {
        [JsonPropertyName("value")]
        [JsonConverter(typeof(FlexibleDecimalConverter))]
        public decimal? Value { get; set; }
    }

    private sealed class DecimalPayload
    {
        [JsonPropertyName("value")]
        [JsonConverter(typeof(FlexibleDecimalConverterNonNullable))]
        public decimal Value { get; set; }
    }

    private sealed class DateTimePayload
    {
        [JsonPropertyName("value")]
        [JsonConverter(typeof(FlexibleDateTimeConverter))]
        public DateTime Value { get; set; }
    }

    private sealed class NullableDateTimePayload
    {
        [JsonPropertyName("value")]
        [JsonConverter(typeof(NullableFlexibleDateTimeConverter))]
        public DateTime? Value { get; set; }
    }
}

public class RetryPolicyBehaviorTests
{
    [Fact]
    public void RetryPolicy_StaticFactories_ShouldExposeExpectedDefaults()
    {
        RetryPolicy @default = RetryPolicy.Default;
        RetryPolicy none = RetryPolicy.None;
        RetryPolicy standard = RetryPolicy.Standard;

        Assert.False(@default.Enabled);
        Assert.Equal(0, @default.MaxRetryAttempts);
        Assert.False(none.Enabled);
        Assert.Equal(0, none.MaxRetryAttempts);

        Assert.True(standard.Enabled);
        Assert.Equal(3, standard.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), standard.BaseDelay);
        Assert.Equal(TimeSpan.FromSeconds(30), standard.MaxDelay);
        Assert.Equal(BackoffStrategy.ExponentialWithJitter, standard.BackoffStrategy);
    }

    [Fact]
    public void RetryPolicy_CalculateDelay_ShouldSupportAllStrategiesAndFallback()
    {
        RetryPolicy fixedPolicy = new() { BaseDelay = TimeSpan.FromMilliseconds(100), BackoffStrategy = BackoffStrategy.Fixed };
        RetryPolicy linearPolicy = new() { BaseDelay = TimeSpan.FromMilliseconds(100), BackoffStrategy = BackoffStrategy.Linear };
        RetryPolicy exponentialPolicy = new()
        {
            BaseDelay = TimeSpan.FromMilliseconds(100),
            MaxDelay = TimeSpan.FromMilliseconds(250),
            BackoffStrategy = BackoffStrategy.Exponential
        };
        RetryPolicy invalidPolicy = new() { BaseDelay = TimeSpan.FromMilliseconds(123), BackoffStrategy = (BackoffStrategy)999 };

        Assert.Equal(TimeSpan.FromMilliseconds(100), fixedPolicy.CalculateDelay(5));
        Assert.Equal(TimeSpan.FromMilliseconds(300), linearPolicy.CalculateDelay(3));
        Assert.Equal(TimeSpan.FromMilliseconds(250), exponentialPolicy.CalculateDelay(3));
        Assert.Equal(TimeSpan.FromMilliseconds(123), invalidPolicy.CalculateDelay(4));
    }

    [Fact]
    public void RetryPolicy_ExponentialWithJitter_ShouldReturnNonNegativeDelay()
    {
        RetryPolicy policy = new()
        {
            Enabled = true,
            BaseDelay = TimeSpan.FromMilliseconds(1000),
            MaxDelay = TimeSpan.FromMilliseconds(2000),
            BackoffStrategy = BackoffStrategy.ExponentialWithJitter
        };

        TimeSpan delay = policy.CalculateDelay(6);

        Assert.True(delay >= TimeSpan.Zero);
        Assert.True(delay <= TimeSpan.FromMilliseconds(2500));
    }

    [Fact]
    public void RetryPolicy_ShouldRetryStatusCode_OnlyWhenEnabled()
    {
        RetryPolicy policy = new();

        Assert.False(policy.ShouldRetry(HttpStatusCode.InternalServerError));

        policy.Enabled = true;
        Assert.True(policy.ShouldRetry(HttpStatusCode.InternalServerError));
        Assert.False(policy.ShouldRetry(HttpStatusCode.OK));
    }

    [Fact]
    public void RetryPolicy_ShouldRetryException_OnlyForSupportedTypesWhenEnabled()
    {
        RetryPolicy policy = new();

        Assert.False(policy.ShouldRetry(new HttpRequestException("net")));
        Assert.False(policy.ShouldRetry(new TaskCanceledException("timeout")));
        Assert.False(policy.ShouldRetry(new SocketException()));

        policy.Enabled = true;
        Assert.True(policy.ShouldRetry(new HttpRequestException("net")));
        Assert.True(policy.ShouldRetry(new TaskCanceledException("timeout")));
        Assert.True(policy.ShouldRetry(new SocketException()));
        Assert.False(policy.ShouldRetry(new InvalidOperationException("no-retry")));
    }
}

public class ServiceCollectionExtensionsBehaviorTests
{
    [Fact]
    public void AddRozetkaPay_ShouldGuardAgainstNullArguments()
    {
        ServiceCollection? services = null;
        IConfiguration? configuration = null;

        Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddRozetkaPay(services!, "login", "password"));
        Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddRozetkaPay(services!, new RozetkaPayConfiguration
        {
            BaseUrl = "https://api.rozetkapay.com",
            Login = "login",
            Password = "password"
        }));
        Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddRozetkaPay(new ServiceCollection(), configuration!));
    }

    [Fact]
    public void AddRozetkaPay_StringOverload_ShouldApplyDefaultsAndRegisterServices()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay("login", "password");
        ServiceProvider provider = services.BuildServiceProvider();

        RozetkaPayConfiguration configuration = provider.GetRequiredService<RozetkaPayConfiguration>();
        RozetkaPayClient client = provider.GetRequiredService<RozetkaPayClient>();

        Assert.Equal("https://api.rozetkapay.com", configuration.BaseUrl);
        Assert.Equal("login", configuration.Login);
        Assert.Equal("password", configuration.Password);

        Assert.NotNull(client.Payments);
        Assert.NotNull(client.AlternativePayments);
        Assert.NotNull(client.PayParts);
        Assert.NotNull(client.Payouts);
        Assert.NotNull(client.Customers);
        Assert.NotNull(client.Subscriptions);
        Assert.NotNull(client.Reports);
        Assert.NotNull(client.Merchants);
        Assert.NotNull(client.FinMon);
    }

    [Fact]
    public void AddRozetkaPay_ConfigOverload_ShouldThrowOnInvalidConfiguration()
    {
        ServiceCollection services = new();
        RozetkaPayConfiguration invalid = new()
        {
            BaseUrl = "not-a-valid-url",
            Login = "login",
            Password = "password"
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => services.AddRozetkaPay(invalid));

        Assert.Contains("configuration is invalid", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddRozetkaPay_IConfigurationOverload_ShouldValidateRequiredFields()
    {
        ServiceCollection services = new();
        IConfigurationRoot missingSectionConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        InvalidOperationException missingSectionException = Assert.Throws<InvalidOperationException>(() =>
            services.AddRozetkaPay(missingSectionConfiguration));
        Assert.Contains("RozetkaPay section is not configured", missingSectionException.Message, StringComparison.Ordinal);

        IConfigurationRoot missingLoginConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RozetkaPay:BaseUrl"] = "https://api.rozetkapay.com",
                ["RozetkaPay:Password"] = "password"
            })
            .Build();
        InvalidOperationException missingLoginException = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddRozetkaPay(missingLoginConfiguration));
        Assert.Contains("RozetkaPay:Login", missingLoginException.Message, StringComparison.Ordinal);

        IConfigurationRoot missingPasswordConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RozetkaPay:BaseUrl"] = "https://api.rozetkapay.com",
                ["RozetkaPay:Login"] = "login"
            })
            .Build();
        InvalidOperationException missingPasswordException = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddRozetkaPay(missingPasswordConfiguration));
        Assert.Contains("RozetkaPay:Password", missingPasswordException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddRozetkaPay_ShouldConfigureNamedHttpClient()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(new RozetkaPayConfiguration
        {
            BaseUrl = "https://api-epdev.rozetkapay.com",
            Login = "login",
            Password = "password",
            Timeout = TimeSpan.FromSeconds(42),
            UserAgent = "SDK.Tests/1.0"
        });

        ServiceProvider provider = services.BuildServiceProvider();
        IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();
        HttpClient client = factory.CreateClient("RozetkaPay");

        Assert.Equal(new Uri("https://api-epdev.rozetkapay.com"), client.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(42), client.Timeout);
        Assert.Equal("SDK.Tests/1.0", client.DefaultRequestHeaders.UserAgent.ToString());
    }

    [Fact]
    public void AddRozetkaPay_ShouldNotDuplicateDescriptorsOnRepeatedRegistration()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(new RozetkaPayConfiguration
        {
            BaseUrl = "https://api.rozetkapay.com",
            Login = "first-login",
            Password = "first-password"
        });
        services.AddRozetkaPay(new RozetkaPayConfiguration
        {
            BaseUrl = "https://api-epdev.rozetkapay.com",
            Login = "second-login",
            Password = "second-password"
        });

        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(RozetkaPayConfiguration)));
        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(PaymentService)));
        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(BatchPaymentService)));
        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(PayPartsService)));
        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(PayoutService)));
        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(CustomerService)));
        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(SubscriptionService)));
        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(ReportService)));
        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(AlternativePaymentService)));
        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(MerchantService)));
        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(FinMonService)));
        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(RozetkaPayClient)));

        using ServiceProvider provider = services.BuildServiceProvider();
        RozetkaPayConfiguration resolved = provider.GetRequiredService<RozetkaPayConfiguration>();
        Assert.Equal("first-login", resolved.Login);
        Assert.Equal("first-password", resolved.Password);
        Assert.Equal("https://api.rozetkapay.com", resolved.BaseUrl);
    }
}

public class RozetkaPayClientFactoryTests
{
    [Fact]
    public void RozetkaPayClient_Constructor_ShouldGuardAgainstNullConfiguration()
    {
        Assert.Throws<ArgumentNullException>(() => new RozetkaPayClient(null!));
    }

    [Fact]
    public async Task RozetkaPayClient_Create_ShouldInitializeServicesAndUseCredentials()
    {
        StubHttpMessageHandler handler = new(async (_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });
        using HttpClient httpClient = new(handler);
        using RozetkaPayClient client = RozetkaPayClient.Create(
            baseUrl: "https://api.rozetkapay.com",
            login: "api-login",
            password: "api-password",
            httpClient: httpClient);

        Assert.NotNull(client.Payments);
        Assert.NotNull(client.BatchPayments);
        Assert.NotNull(client.PayParts);
        Assert.NotNull(client.Payouts);
        Assert.NotNull(client.Customers);
        Assert.NotNull(client.Subscriptions);
        Assert.NotNull(client.Reports);
        Assert.NotNull(client.AlternativePayments);
        Assert.NotNull(client.Merchants);
        Assert.NotNull(client.FinMon);

        await client.Payments.GetInfoAsync("payment-1");

        Assert.NotNull(handler.LastRequest);
        string expectedAuthorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("api-login:api-password"));
        Assert.Equal(expectedAuthorization, handler.LastRequest!.Headers.Authorization?.ToString());
        Assert.Equal("/api/payments/v1/info?external_id=payment-1", handler.LastRequest.RequestUri!.PathAndQuery);
    }
}

public class ExceptionConstructorsTests
{
    [Fact]
    public void RozetkaPayExceptionHierarchy_Constructors_ShouldSetMessageAndInnerException()
    {
        Exception inner = new InvalidOperationException("inner");

        Assert.NotNull(new RozetkaPayException());
        Assert.Equal("base", new RozetkaPayException("base").Message);
        Assert.Same(inner, new RozetkaPayException("base", inner).InnerException);

        Assert.NotNull(new RozetkaPayAuthorizationException());
        Assert.Equal("auth", new RozetkaPayAuthorizationException("auth").Message);
        Assert.Same(inner, new RozetkaPayAuthorizationException("auth", inner).InnerException);

        Assert.NotNull(new RozetkaPayValidationException());
        Assert.Equal("validation", new RozetkaPayValidationException("validation").Message);
        Assert.Same(inner, new RozetkaPayValidationException("validation", inner).InnerException);

        Assert.NotNull(new RozetkaPayRateLimitException());
        Assert.Equal("rate", new RozetkaPayRateLimitException("rate").Message);
        Assert.Same(inner, new RozetkaPayRateLimitException("rate", inner).InnerException);

        Assert.NotNull(new RozetkaPayNotFoundException());
        Assert.Equal("not-found", new RozetkaPayNotFoundException("not-found").Message);
        Assert.Same(inner, new RozetkaPayNotFoundException("not-found", inner).InnerException);
    }
}
