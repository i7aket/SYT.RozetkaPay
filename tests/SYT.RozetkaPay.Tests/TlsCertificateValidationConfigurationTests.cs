using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Extensions;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Guards the TLS contract of the SDK: there is no certificate-validation switch on the public surface, no
/// renamed replacement for the one that was removed, a stale <c>RozetkaPay:ValidateSslCertificate</c>
/// configuration key fails loudly instead of being silently ignored, and the handler the DI registration
/// builds keeps the platform's default certificate validation.
/// </summary>
/// <remarks>
/// The removed switch never reached an <see cref="HttpMessageHandler"/>, so setting it to <c>false</c> only
/// ever promised a behaviour the SDK did not have. Nothing here opens a network connection, needs a
/// certificate, or uses a real credential.
/// </remarks>
public class TlsCertificateValidationConfigurationTests
{
    private const string Login = "test-login";
    private const string Secret = "test-password";

    private const string RemovedPropertyName = "ValidateSslCertificate";
    private const string RemovedConfigurationKey = "RozetkaPay:ValidateSslCertificate";

    // Literal fragments of the migration error, spelled out here rather than read back from the production
    // constant, so a reworded message has to be re-approved by a human instead of silently agreeing with
    // itself.
    private const string RemovedKeyFragment = "RozetkaPay:ValidateSslCertificate";
    private const string RemovedReasonFragment = "was removed because it never controlled the HTTP handler";
    private const string PlatformPolicyFragment =
        "TLS certificate validation always follows the platform or caller-supplied HttpMessageHandler policy";
    private const string RemoveKeyFragment = "Remove this configuration key";

    // Substrings that mark a name as a certificate/TLS validation switch. Applied only to the two SDK
    // configuration types, so an unrelated type keeps its own booleans.
    private static readonly string[] TlsSwitchNameMarkers =
        ["valid", "cert", "tls", "ssl", "trust", "insecure", "ignore", "skip"];

    // ---------------------------------------------------------------------------------------------
    // Public surface: the dead switch is gone, and nothing was renamed in its place
    // ---------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(typeof(RozetkaPayConfiguration))]
    [InlineData(typeof(RozetkaPayOptions))]
    public void ConfigurationTypes_ShouldNotExposeTheRemovedSslSwitch(Type configurationType)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

        Assert.Null(configurationType.GetProperty(RemovedPropertyName, flags));

        // A field with the same name would keep the identical false promise on the public surface.
        Assert.Null(configurationType.GetField(RemovedPropertyName, flags));
    }

    [Theory]
    [InlineData(typeof(RozetkaPayConfiguration))]
    [InlineData(typeof(RozetkaPayOptions))]
    public void ConfigurationTypes_ShouldNotExposeAnyCertificateValidationSwitch(Type configurationType)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

        IEnumerable<string> booleanMemberNames = configurationType
            .GetProperties(flags)
            .Where(property => property.PropertyType == typeof(bool))
            .Select(property => property.Name)
            .Concat(configurationType
                .GetFields(flags)
                .Where(field => field.FieldType == typeof(bool))
                .Select(field => field.Name));

        List<string> offenders = booleanMemberNames
            .Where(name => TlsSwitchNameMarkers.Any(
                marker => name.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.Empty(offenders);
    }

    // ---------------------------------------------------------------------------------------------
    // Migration: a stale configuration key is rejected instead of quietly ignored
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void IConfigurationOverload_ShouldRejectTheRemovedSslKey()
    {
        IConfigurationRoot configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["RozetkaPay:Login"] = "cfg-login",
            ["RozetkaPay:Password"] = "cfg-password",
            [RemovedConfigurationKey] = "false"
        });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddRozetkaPay(configuration));

        Assert.Contains(RemovedKeyFragment, exception.Message, StringComparison.Ordinal);
        Assert.Contains(RemovedReasonFragment, exception.Message, StringComparison.Ordinal);
        Assert.Contains(PlatformPolicyFragment, exception.Message, StringComparison.Ordinal);
        Assert.Contains(RemoveKeyFragment, exception.Message, StringComparison.Ordinal);

        // The message names the key, never what it was set to, and never a neighbouring setting.
        Assert.DoesNotContain("false", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cfg-login", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("cfg-password", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    // Every presence of the key is obsolete: the SDK never read the value, so there is no value that could
    // still mean anything.
    [InlineData("RozetkaPay:ValidateSslCertificate", "false")]
    [InlineData("RozetkaPay:ValidateSslCertificate", "true")]
    [InlineData("RozetkaPay:ValidateSslCertificate", "")]
    // A null value is still the key being present. It is reachable by name through GetChildren() even though
    // IConfigurationSection.Exists() reports false for it, so it must not be a hole in the check.
    [InlineData("RozetkaPay:ValidateSslCertificate", null)]
    // Configuration keys are matched case-insensitively, so a differently cased stale key is the same key.
    [InlineData("RozetkaPay:validatesslcertificate", "false")]
    [InlineData("RozetkaPay:VALIDATESSLCERTIFICATE", "true")]
    public void IConfigurationOverload_ShouldRejectTheRemovedSslKeyWhateverItsValue(string key, string? value)
    {
        IConfigurationRoot configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["RozetkaPay:Login"] = "cfg-login",
            ["RozetkaPay:Password"] = "cfg-password",
            [key] = value
        });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddRozetkaPay(configuration));

        Assert.Contains(RemovedKeyFragment, exception.Message, StringComparison.Ordinal);
        Assert.Contains(RemovedReasonFragment, exception.Message, StringComparison.Ordinal);
        Assert.Contains(PlatformPolicyFragment, exception.Message, StringComparison.Ordinal);
        Assert.Contains(RemoveKeyFragment, exception.Message, StringComparison.Ordinal);

        // The message names the key, never what it was set to, and never a neighbouring setting.
        if (!string.IsNullOrEmpty(value))
        {
            Assert.DoesNotContain(value, exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("cfg-login", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("cfg-password", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void IConfigurationOverload_WithoutTheRemovedSslKey_ShouldBindNormally()
    {
        IConfigurationRoot configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["RozetkaPay:Login"] = "cfg-login",
            ["RozetkaPay:Password"] = "cfg-password",
            ["RozetkaPay:Environment"] = "Sandbox",
            ["RozetkaPay:UserAgent"] = "CfgAgent/1.0",
            ["RozetkaPay:Timeout"] = "00:00:45"
        });

        ServiceCollection services = new();
        services.AddRozetkaPay(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        RozetkaPayOptions options = provider.GetRequiredService<IOptions<RozetkaPayOptions>>().Value;
        RozetkaPayConfiguration snapshot = provider.GetRequiredService<RozetkaPayConfiguration>();

        Assert.Equal("cfg-login", options.Login);
        Assert.Equal("cfg-password", options.Password);
        Assert.Equal(RozetkaPayEnvironment.Sandbox, options.Environment);
        Assert.Equal("CfgAgent/1.0", options.UserAgent);
        Assert.Equal(TimeSpan.FromSeconds(45), options.Timeout);

        // Endpoint selection still follows the environment, and the snapshot still agrees with the options.
        Assert.Equal(RozetkaPayOptions.SandboxBaseUrl, snapshot.BaseUrl);
        Assert.Equal(options.UserAgent, snapshot.UserAgent);
        Assert.Equal(options.Timeout, snapshot.Timeout);
    }

    // ---------------------------------------------------------------------------------------------
    // Transport: the registered handler keeps the platform's certificate validation
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void RegisteredHttpClient_ShouldKeepThePlatformCertificateValidation()
    {
        PrimaryHandlerCapturingFilter capturingFilter = new();

        ServiceCollection services = new();
        services.AddRozetkaPay(options =>
        {
            options.Login = Login;
            options.Password = Secret;
        });
        services.AddSingleton<IHttpMessageHandlerBuilderFilter>(capturingFilter);

        using ServiceProvider provider = services.BuildServiceProvider();

        // Creating the client is what runs the builder actions; no request is ever sent.
        using HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("RozetkaPay");

        HttpMessageHandler primaryHandler = Assert.Contains("RozetkaPay", capturingFilter.PrimaryHandlers);

        switch (primaryHandler)
        {
            case SocketsHttpHandler socketsHandler:
                Assert.Null(socketsHandler.SslOptions.RemoteCertificateValidationCallback);
                break;
            case HttpClientHandler httpClientHandler:
                Assert.Null(httpClientHandler.ServerCertificateCustomValidationCallback);
                break;
            default:
                Assert.Fail(
                    $"The named RozetkaPay client uses an unexpected primary handler " +
                    $"'{primaryHandler.GetType().FullName}'. Its certificate validation policy cannot be " +
                    "verified, so this test must be taught about the handler rather than skipped.");
                break;
        }
    }

    private static IConfigurationRoot BuildConfiguration(Dictionary<string, string?> settings)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    /// <summary>
    /// Records the primary handler each named client ends up with, after every standard builder action has
    /// run. Observes the handler chain; it never modifies it.
    /// </summary>
    private sealed class PrimaryHandlerCapturingFilter : IHttpMessageHandlerBuilderFilter
    {
        private readonly Dictionary<string, HttpMessageHandler> _primaryHandlers = new(StringComparer.Ordinal);

        internal IReadOnlyDictionary<string, HttpMessageHandler> PrimaryHandlers => _primaryHandlers;

        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
        {
            return builder =>
            {
                next(builder);

                // Read after the chain is fully configured, so a handler swapped in by a builder action
                // would be what gets captured.
                _primaryHandlers[builder.Name ?? string.Empty] = builder.PrimaryHandler;
            };
        }
    }
}
