using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Security;
using SYT.RozetkaPay.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SYT.RozetkaPay.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register RozetkaPay SDK services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configuration key of the SSL certificate validation switch that used to sit on the options. It never
    /// reached an <see cref="HttpMessageHandler"/>, so it is rejected rather than bound.
    /// </summary>
    private const string RemovedValidateSslCertificateKey = "ValidateSslCertificate";

    /// <summary>
    /// Name of the ordinary authenticated HTTP client used by every authenticated operation.
    /// </summary>
    private const string AuthenticatedHttpClientName = "RozetkaPay";

    /// <summary>
    /// Name of the dedicated client used by the unauthenticated <c>declinePaymentInstruction</c>
    /// operation. It is a separate named client because <see cref="HttpClient"/> has no per-request
    /// redirect switch: the guarantee that a <c>302</c> is never followed, and that no credential is
    /// replayed to a provider-named host, lives in this client's primary handler.
    /// </summary>
    private const string NonRedirectHttpClientName = "RozetkaPay.PaymentInstructions.Decline";

    /// <summary>
    /// Add RozetkaPay SDK services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="login">RozetkaPay API login</param>
    /// <param name="password">RozetkaPay API password</param>
    /// <param name="baseUrl">
    /// Optional API base URL. When omitted the production endpoint
    /// (<see cref="RozetkaPayOptions.ProductionBaseUrl"/>) is used; supplying a value overrides the endpoint
    /// of <see cref="RozetkaPayOptions.Environment"/>.
    /// </param>
    /// <returns>The service collection for chaining</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="login"/>, <paramref name="password"/>, or <paramref name="baseUrl"/> is missing or
    /// malformed.
    /// </exception>
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
            BaseUrl = baseUrl ?? RozetkaPayOptions.ProductionBaseUrl
        };

        RozetkaPayOptions options = SnapshotLegacyConfiguration(configuration);

        // No caller-supplied URL means "use the endpoint of the environment" rather than a pinned override.
        options.BaseUrl = baseUrl;

        return AddRozetkaPayCore(services, options);
    }

    /// <summary>
    /// Add RozetkaPay SDK services to the service collection using configuration object
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">
    /// RozetkaPay configuration. It is copied at registration time, so later changes to the instance do not
    /// affect the SDK, and its <see cref="RozetkaPayConfiguration.BaseUrl"/> becomes an explicit endpoint
    /// override.
    /// </param>
    /// <returns>The service collection for chaining</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> or <paramref name="configuration"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">The configuration is missing a required value.</exception>
    public static IServiceCollection AddRozetkaPay(
        this IServiceCollection services,
        RozetkaPayConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return AddRozetkaPayCore(services, SnapshotLegacyConfiguration(configuration));
    }

    /// <summary>
    /// Add RozetkaPay SDK services to the service collection using IConfiguration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">
    /// Application configuration containing the <see cref="RozetkaPayOptions.SectionName"/> section. The
    /// section is bound to <see cref="RozetkaPayOptions"/> and validated at startup.
    /// </param>
    /// <returns>The service collection for chaining</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> or <paramref name="configuration"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The section is absent, does not carry a login and a password, or still carries the removed
    /// <c>ValidateSslCertificate</c> key.
    /// </exception>
    public static IServiceCollection AddRozetkaPay(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        IConfigurationSection rozetkaPaySection = configuration.GetSection(RozetkaPayOptions.SectionName);

        // The removed switch is caught before anything is bound. The binder ignores a key with no matching
        // property, which would leave an operator believing a TLS policy they configured is still in force,
        // so the key's presence alone is the error.
        //
        // Presence is decided by comparing the direct children's key names, not by Exists(): Exists() is
        // false for a key whose value is null, yet such a key is still configured and GetChildren() still
        // lists it. Matching is case-insensitive, as configuration key matching is everywhere else. The
        // value is never read, parsed, interpolated, or logged — there is no value it could still have meant.
        if (rozetkaPaySection.GetChildren().Any(static child =>
                string.Equals(child.Key, RemovedValidateSslCertificateKey, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"{RozetkaPayOptions.SectionName}:{RemovedValidateSslCertificateKey} was removed because it " +
                "never controlled the HTTP handler. TLS certificate validation always follows the platform " +
                "or caller-supplied HttpMessageHandler policy. Remove this configuration key.");
        }

        // A section that is absent, or that carries no credentials at all, is reported here rather than at
        // startup validation: this is the failure the SDK has always thrown on, and the message points at the
        // configuration key that is missing. Everything else is left to the options pipeline below.
        RozetkaPayOptions bound = rozetkaPaySection.Get<RozetkaPayOptions>() ??
                                  throw new InvalidOperationException(
                                      $"{RozetkaPayOptions.SectionName} section is not configured in appsettings");

        if (string.IsNullOrWhiteSpace(bound.Login))
        {
            throw new InvalidOperationException(
                $"{RozetkaPayOptions.SectionName}:{nameof(RozetkaPayOptions.Login)} is not configured in appsettings");
        }

        if (string.IsNullOrWhiteSpace(bound.Password))
        {
            throw new InvalidOperationException(
                $"{RozetkaPayOptions.SectionName}:{nameof(RozetkaPayOptions.Password)} is not configured in appsettings");
        }

        return AddRozetkaPayCore(services, builder => builder.Bind(rozetkaPaySection));
    }

    /// <summary>
    /// Add RozetkaPay SDK services to the service collection, configuring the typed options in code. Use this
    /// overload to switch between <see cref="RozetkaPayEnvironment.Production"/> and
    /// <see cref="RozetkaPayEnvironment.Sandbox"/> without an <see cref="IConfiguration"/>.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">
    /// Callback that populates the options. It runs once, when the options are first resolved, and the result
    /// is validated before the application starts.
    /// </param>
    /// <returns>The service collection for chaining</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddRozetkaPay(
        this IServiceCollection services,
        Action<RozetkaPayOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        return AddRozetkaPayCore(services, builder => builder.Configure(configure));
    }

    /// <summary>
    /// Copy a caller-supplied configuration into options, failing immediately when it is unusable.
    /// </summary>
    /// <remarks>
    /// The copy happens now, not when the options are resolved, so mutating the caller's instance afterwards
    /// cannot change what the SDK runs with.
    /// </remarks>
    private static RozetkaPayOptions SnapshotLegacyConfiguration(RozetkaPayConfiguration configuration)
    {
        if (!configuration.IsValid())
        {
            throw new InvalidOperationException(
                "RozetkaPay configuration is invalid. BaseUrl, Login and Password are required.");
        }

        return RozetkaPayOptionsMapper.FromConfiguration(configuration);
    }

    private static IServiceCollection AddRozetkaPayCore(IServiceCollection services, RozetkaPayOptions options)
    {
        return AddRozetkaPayCore(
            services,
            builder => builder.Configure(target => RozetkaPayOptionsMapper.CopyInto(options, target)));
    }

    /// <summary>
    /// Register the options pipeline and every SDK service exactly once.
    /// </summary>
    private static IServiceCollection AddRozetkaPayCore(
        IServiceCollection services,
        Action<OptionsBuilder<RozetkaPayOptions>> configureOptions)
    {
        // First registration wins, as it always has. Returning early — instead of relying on TryAdd alone —
        // also keeps a second call from binding another configuration source over the first one.
        if (services.Any(static descriptor => descriptor.ServiceType == typeof(RozetkaPayRegistrationMarker)))
        {
            return services;
        }

        services.Add(ServiceDescriptor.Singleton(typeof(RozetkaPayRegistrationMarker), RozetkaPayRegistrationMarker.Instance));

        OptionsBuilder<RozetkaPayOptions> optionsBuilder = services.AddOptions<RozetkaPayOptions>();
        configureOptions(optionsBuilder);
        optionsBuilder
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // TryAddEnumerable keeps the validator single even if the SDK is registered again, and leaves any
        // validator a consumer added for the same options in place.
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<RozetkaPayOptions>, RozetkaPayOptionsValidator>());

        // One validated snapshot per provider, shared by the HTTP client, the services, and the verifier, so
        // they can never disagree about credentials or endpoint. Reading IOptions<>.Value here is what runs
        // validation, so an invalid configuration cannot reach a request.
        services.TryAddSingleton(static provider =>
            RozetkaPayOptionsMapper.ToConfiguration(provider.GetRequiredService<IOptions<RozetkaPayOptions>>().Value));

        // Every SDK named client removes the built-in IHttpClientFactory HTTP logging, and the SDK's own
        // static-label logging is the single source of HTTP log output.
        //
        // Services log a static route template, never the real request target, so no caller identifier
        // reaches a log sink through the SDK. AddHttpClient installs its own handler logging on top of that,
        // under System.Net.Http.HttpClient.<name>.LogicalHandler and ...ClientHandler, which the SDK's log
        // labels cannot influence - and that logging is unsafe here on two counts:
        //
        //  * it writes the request URI, and while Microsoft.Extensions.Http redacts the whole query to "?*",
        //    it does not redact path segments. An identifier the SDK deliberately keeps out of its own logs -
        //    the subscription_id of the payment-method update - was written out verbatim at Information level;
        //  * its header logging redacts values in the rendered message only. The structured state of those
        //    entries carries the real header values, so a sink that writes log state records Authorization
        //    and X-CUSTOMER-AUTH in clear at Trace level.
        //
        // Neither is configurable: RedactLoggedHeaders covers headers only, and there is no hook for the URI.
        // RemoveAllLoggers() is the supported way to turn the built-in logging off, and it does not touch the
        // ILogger instances the services themselves use. It is applied to both clients - the authenticated one
        // because of the path identifier, and the decline one so the guarantee does not depend on which
        // spelling a future operation happens to use.
        services
            .AddHttpClient(AuthenticatedHttpClientName, (provider, client) =>
            {
                RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
                client.BaseAddress = new Uri(config.BaseUrl);
                client.Timeout = config.Timeout;
                client.DefaultRequestHeaders.UserAgent.Clear();
                if (!string.IsNullOrWhiteSpace(config.UserAgent))
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(config.UserAgent);
                }
            })
            // A redirect is never followed on an authenticated transport. HttpClient drops
            // Authorization when a redirect crosses an origin, but forwards every other header
            // verbatim - and X-ON-BEHALF-OF and X-CUSTOMER-AUTH are merchant secrets in their own
            // right, so a host answering with a Location it controls would harvest both. Which
            // headers a given runtime strips is an implementation detail; refusing the redirect is
            // the only form of this guarantee that does not depend on it.
            //
            // The decline client below has carried this setting since it was introduced. The
            // authenticated client - the one that actually carries the secrets - did not, which is
            // the defect being closed here.
            .ConfigurePrimaryHttpMessageHandler(static () => new HttpClientHandler { AllowAutoRedirect = false })
            .RemoveAllLoggers();

        // The decline client shares the validated endpoint, the timeout snapshot and the user agent, and
        // nothing else. No credential-bearing default header is configured on it, and its primary handler
        // refuses to follow redirects. TLS validation is left to the platform: no certificate callback is
        // installed and no check is relaxed.
        services
            .AddHttpClient(NonRedirectHttpClientName, (provider, client) =>
            {
                RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
                client.BaseAddress = new Uri(config.BaseUrl);
                client.Timeout = config.Timeout;
                client.DefaultRequestHeaders.UserAgent.Clear();
                if (!string.IsNullOrWhiteSpace(config.UserAgent))
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(config.UserAgent);
                }
            })
            .ConfigurePrimaryHttpMessageHandler(static () => new HttpClientHandler { AllowAutoRedirect = false })
            .RemoveAllLoggers();

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(AuthenticatedHttpClientName);
            return new PaymentService(config, httpClient, provider.GetService<ILogger<PaymentService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(AuthenticatedHttpClientName);
            return new BatchPaymentService(config, httpClient, provider.GetService<ILogger<BatchPaymentService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(AuthenticatedHttpClientName);
            return new PayPartsService(config, httpClient, provider.GetService<ILogger<PayPartsService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(AuthenticatedHttpClientName);
            return new PayoutService(config, httpClient, provider.GetService<ILogger<PayoutService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(AuthenticatedHttpClientName);
            return new CustomerService(config, httpClient, provider.GetService<ILogger<CustomerService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(AuthenticatedHttpClientName);
            return new SubscriptionService(config, httpClient, provider.GetService<ILogger<SubscriptionService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(AuthenticatedHttpClientName);
            return new ReportService(config, httpClient, provider.GetService<ILogger<ReportService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(AuthenticatedHttpClientName);
            return new AlternativePaymentService(config, httpClient, provider.GetService<ILogger<AlternativePaymentService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(AuthenticatedHttpClientName);
            return new MerchantService(config, httpClient, provider.GetService<ILogger<MerchantService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(AuthenticatedHttpClientName);
            return new FinMonService(config, httpClient, provider.GetService<ILogger<FinMonService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(AuthenticatedHttpClientName);
            return new InStorePaymentService(config, httpClient, provider.GetService<ILogger<InStorePaymentService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(AuthenticatedHttpClientName);
            return new PartnerService(config, httpClient, provider.GetService<ILogger<PartnerService>>());
        });

        // The decline operation gets the dedicated non-redirect client from the factory. The factory owns
        // its lifetime, so the service is constructed through the overload that does not take ownership.
        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            IHttpClientFactory httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            return new PaymentInstructionService(
                config,
                httpClientFactory.CreateClient(AuthenticatedHttpClientName),
                httpClientFactory.CreateClient(NonRedirectHttpClientName),
                provider.GetService<ILogger<PaymentInstructionService>>());
        });

        services.TryAddScoped(provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(AuthenticatedHttpClientName);
            return new RozetkaPayClient(config, httpClient, provider.GetService<ILogger<RozetkaPayClient>>());
        });

        // The webhook signature verifier is a singleton: it holds nothing but the immutable merchant
        // password, creates its hash primitives per call, and keeps no request state.
        services.TryAddSingleton(static provider =>
        {
            RozetkaPayConfiguration config = provider.GetRequiredService<RozetkaPayConfiguration>();
            return new RozetkaPayWebhookSignatureVerifier(config.Password);
        });

        // Interface aliases resolve to the concrete registrations above, so a concrete type and its
        // interface share the same scoped instance. TryAdd is used so that an interface a consumer
        // registered before AddRozetkaPay (a fake in tests, a decorator in production) is preserved.
        services.TryAddScoped<IPaymentService>(static provider => provider.GetRequiredService<PaymentService>());
        services.TryAddScoped<IBatchPaymentService>(static provider => provider.GetRequiredService<BatchPaymentService>());
        services.TryAddScoped<IPayPartsService>(static provider => provider.GetRequiredService<PayPartsService>());
        services.TryAddScoped<IPayoutService>(static provider => provider.GetRequiredService<PayoutService>());
        services.TryAddScoped<ICustomerService>(static provider => provider.GetRequiredService<CustomerService>());
        services.TryAddScoped<ISubscriptionService>(static provider => provider.GetRequiredService<SubscriptionService>());
        services.TryAddScoped<IReportService>(static provider => provider.GetRequiredService<ReportService>());
        services.TryAddScoped<IAlternativePaymentService>(static provider => provider.GetRequiredService<AlternativePaymentService>());
        services.TryAddScoped<IMerchantService>(static provider => provider.GetRequiredService<MerchantService>());
        services.TryAddScoped<IFinMonService>(static provider => provider.GetRequiredService<FinMonService>());
        services.TryAddScoped<IInStorePaymentService>(static provider => provider.GetRequiredService<InStorePaymentService>());
        services.TryAddScoped<IPartnerService>(static provider => provider.GetRequiredService<PartnerService>());
        services.TryAddScoped<IPaymentInstructionService>(static provider => provider.GetRequiredService<PaymentInstructionService>());
        services.TryAddScoped<IRozetkaPayClient>(static provider => provider.GetRequiredService<RozetkaPayClient>());
        services.TryAddSingleton<IRozetkaPayWebhookSignatureVerifier>(
            static provider => provider.GetRequiredService<RozetkaPayWebhookSignatureVerifier>());

        return services;
    }

    /// <summary>
    /// Records that the SDK has already been registered on a service collection. Not resolved by anything;
    /// its presence is the whole signal.
    /// </summary>
    private sealed class RozetkaPayRegistrationMarker
    {
        internal static readonly RozetkaPayRegistrationMarker Instance = new();

        private RozetkaPayRegistrationMarker()
        {
        }
    }
}
