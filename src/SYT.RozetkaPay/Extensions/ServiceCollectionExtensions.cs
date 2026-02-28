using System.Net;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace SYT.RozetkaPay.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register RozetkaPay SDK services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add RozetkaPay SDK services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="login">RozetkaPay API login</param>
    /// <param name="password">RozetkaPay API password</param>
    /// <param name="baseUrl">Optional API base URL (defaults to production API)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRozetkaPay(
        this IServiceCollection services,
        string login,
        string password,
        string? baseUrl = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        RozetkaPayConfiguration configuration = new RozetkaPayConfiguration
        {
            Login = login,
            Password = password,
            BaseUrl = baseUrl ?? "https://api.rozetkapay.com"
        };

        return services.AddRozetkaPay(configuration);
    }

    /// <summary>
    /// Add RozetkaPay SDK services to the service collection using configuration object
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">RozetkaPay configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRozetkaPay(
        this IServiceCollection services,
        RozetkaPayConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (!configuration.IsValid())
        {
            throw new InvalidOperationException("RozetkaPay configuration is invalid. BaseUrl, Login and Password are required.");
        }

        RozetkaPayConfiguration registeredConfiguration = CloneConfiguration(configuration);
        services.TryAddSingleton(registeredConfiguration);

        services.AddHttpClient("RozetkaPay", (provider, client) =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            client.BaseAddress = new Uri(config.BaseUrl);
            client.Timeout = config.Timeout;
            client.DefaultRequestHeaders.UserAgent.Clear();
            if (!string.IsNullOrWhiteSpace(config.UserAgent))
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(config.UserAgent);
            }
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("RozetkaPay");
            return new PaymentService(config, httpClient, provider.GetService<ILogger<PaymentService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("RozetkaPay");
            return new BatchPaymentService(config, httpClient, provider.GetService<ILogger<BatchPaymentService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("RozetkaPay");
            return new PayPartsService(config, httpClient, provider.GetService<ILogger<PayPartsService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("RozetkaPay");
            return new PayoutService(config, httpClient, provider.GetService<ILogger<PayoutService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("RozetkaPay");
            return new CustomerService(config, httpClient, provider.GetService<ILogger<CustomerService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("RozetkaPay");
            return new SubscriptionService(config, httpClient, provider.GetService<ILogger<SubscriptionService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("RozetkaPay");
            return new ReportService(config, httpClient, provider.GetService<ILogger<ReportService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("RozetkaPay");
            return new AlternativePaymentService(config, httpClient, provider.GetService<ILogger<AlternativePaymentService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("RozetkaPay");
            return new MerchantService(config, httpClient, provider.GetService<ILogger<MerchantService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("RozetkaPay");
            return new FinMonService(config, httpClient, provider.GetService<ILogger<FinMonService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("RozetkaPay");
            return new RozetkaPayClient(config, httpClient, provider.GetService<ILogger<RozetkaPayClient>>());
        });

        return services;
    }

    /// <summary>
    /// Add RozetkaPay SDK services to the service collection using IConfiguration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration containing RozetkaPay section</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRozetkaPay(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        IConfigurationSection rozetkaPaySection = configuration.GetSection("RozetkaPay");
        RozetkaPayConfiguration rozetkaPayConfiguration = rozetkaPaySection.Get<RozetkaPayConfiguration>() ??
                                                          throw new InvalidOperationException("RozetkaPay section is not configured in appsettings");

        if (string.IsNullOrWhiteSpace(rozetkaPayConfiguration.Login))
        {
            throw new InvalidOperationException("RozetkaPay:Login is not configured in appsettings");
        }

        if (string.IsNullOrWhiteSpace(rozetkaPayConfiguration.Password))
        {
            throw new InvalidOperationException("RozetkaPay:Password is not configured in appsettings");
        }

        return services.AddRozetkaPay(rozetkaPayConfiguration);
    }

    private static RozetkaPayConfiguration CloneConfiguration(RozetkaPayConfiguration source)
    {
        return new RozetkaPayConfiguration
        {
            BaseUrl = source.BaseUrl,
            Login = source.Login,
            Password = source.Password,
            OnBehalfOf = source.OnBehalfOf,
            CustomerAuth = source.CustomerAuth,
            Timeout = source.Timeout,
            UserAgent = source.UserAgent,
            ValidateSslCertificate = source.ValidateSslCertificate,
            RetryPolicy = CloneRetryPolicy(source.RetryPolicy)
        };
    }

    private static RetryPolicy CloneRetryPolicy(RetryPolicy source)
    {
        return new RetryPolicy
        {
            Enabled = source.Enabled,
            MaxRetryAttempts = source.MaxRetryAttempts,
            BaseDelay = source.BaseDelay,
            MaxDelay = source.MaxDelay,
            BackoffStrategy = source.BackoffStrategy,
            RetriableStatusCodes = new HashSet<HttpStatusCode>(source.RetriableStatusCodes)
        };
    }
}
