using System.Net;

namespace SYT.RozetkaPay.Configuration;

/// <summary>
/// Translates between <see cref="RozetkaPayOptions"/> and the <see cref="RozetkaPayConfiguration"/> snapshot
/// the services, the named <c>HttpClient</c>, and the webhook verifier are built from.
/// </summary>
/// <remarks>
/// Keeping one snapshot behind the whole SDK is what stops the HTTP client, the services, and the verifier
/// from drifting apart. Every copy here is deep: nothing that a caller still holds a reference to ends up
/// inside the registered configuration.
/// </remarks>
internal static class RozetkaPayOptionsMapper
{
    /// <summary>
    /// Resolve the endpoint the SDK will call: an explicit
    /// <see cref="RozetkaPayOptions.BaseUrl"/> wins, otherwise the endpoint of
    /// <see cref="RozetkaPayOptions.Environment"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The environment is not a defined <see cref="RozetkaPayEnvironment"/>. Unreachable through the DI
    /// registration, where <see cref="RozetkaPayOptionsValidator"/> rejects it first.
    /// </exception>
    internal static string ResolveBaseUrl(RozetkaPayOptions options)
    {
        // Only null means "not set". An empty or whitespace value is a broken override, and the validator
        // reports it rather than letting it silently fall back to an endpoint nobody asked for.
        if (options.BaseUrl is not null)
        {
            return options.BaseUrl;
        }

        return options.Environment switch
        {
            RozetkaPayEnvironment.Production => RozetkaPayOptions.ProductionBaseUrl,
            RozetkaPayEnvironment.Sandbox => RozetkaPayOptions.SandboxBaseUrl,
            _ => throw new InvalidOperationException(
                $"{RozetkaPayOptions.SectionName}:{nameof(RozetkaPayOptions.Environment)} is not a supported " +
                "RozetkaPay environment.")
        };
    }

    /// <summary>
    /// Build the compatibility snapshot consumed by the SDK services from validated options.
    /// </summary>
    internal static RozetkaPayConfiguration ToConfiguration(RozetkaPayOptions options)
    {
        return new RozetkaPayConfiguration
        {
            BaseUrl = ResolveBaseUrl(options),
            Login = options.Login,
            Password = options.Password,
            OnBehalfOf = options.OnBehalfOf,
            CustomerAuth = options.CustomerAuth,
            Timeout = options.Timeout,
            UserAgent = options.UserAgent,
            TransportSecurity = options.TransportSecurity,
            RetryPolicy = CloneRetryPolicy(options.RetryPolicy)!
        };
    }

    /// <summary>
    /// Snapshot a configuration instance supplied through one of the pre-existing <c>AddRozetkaPay</c>
    /// overloads. Its <see cref="RozetkaPayConfiguration.BaseUrl"/> becomes an explicit endpoint override.
    /// </summary>
    internal static RozetkaPayOptions FromConfiguration(RozetkaPayConfiguration configuration)
    {
        return new RozetkaPayOptions
        {
            BaseUrl = configuration.BaseUrl,
            Login = configuration.Login,
            Password = configuration.Password,
            OnBehalfOf = configuration.OnBehalfOf,
            CustomerAuth = configuration.CustomerAuth,
            Timeout = configuration.Timeout,
            UserAgent = configuration.UserAgent,
            TransportSecurity = configuration.TransportSecurity,
            RetryPolicy = CloneRetryPolicy(configuration.RetryPolicy)!
        };
    }

    /// <summary>
    /// Copy a registration-time snapshot onto the options instance the options factory created.
    /// </summary>
    internal static void CopyInto(RozetkaPayOptions source, RozetkaPayOptions destination)
    {
        destination.BaseUrl = source.BaseUrl;
        destination.Login = source.Login;
        destination.Password = source.Password;
        destination.Environment = source.Environment;
        destination.OnBehalfOf = source.OnBehalfOf;
        destination.CustomerAuth = source.CustomerAuth;
        destination.Timeout = source.Timeout;
        destination.UserAgent = source.UserAgent;
        destination.TransportSecurity = source.TransportSecurity;

        // Cloned again: the factory may build several options instances, and they must not share a policy.
        destination.RetryPolicy = CloneRetryPolicy(source.RetryPolicy)!;
    }

    /// <summary>
    /// Deep-copy a retry policy, including its status-code set.
    /// </summary>
    /// <remarks>
    /// A missing policy or status-code set is carried through as <see langword="null"/> instead of throwing,
    /// so <see cref="RozetkaPayOptionsValidator"/> can report it as the configuration error it is.
    /// </remarks>
    private static RetryPolicy? CloneRetryPolicy(RetryPolicy? source)
    {
        if (source is null)
        {
            return null;
        }

        return new RetryPolicy
        {
            Enabled = source.Enabled,
            MaxRetryAttempts = source.MaxRetryAttempts,
            BaseDelay = source.BaseDelay,
            MaxDelay = source.MaxDelay,
            BackoffStrategy = source.BackoffStrategy,
            RetriableStatusCodes = source.RetriableStatusCodes is null
                ? null!
                : new HashSet<HttpStatusCode>(source.RetriableStatusCodes)
        };
    }
}
