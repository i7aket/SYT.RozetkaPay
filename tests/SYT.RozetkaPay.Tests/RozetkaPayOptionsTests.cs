using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Extensions;
using SYT.RozetkaPay.Security;
using SYT.RozetkaPay.Services;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Verifies the typed options surface: defaults, the sandbox/production endpoint switch, configuration
/// binding, the validation pipeline (DataAnnotations plus <see cref="IValidateOptions{TOptions}"/> checked
/// at startup), and that none of it leaks credentials or changes the pre-existing configuration API.
/// </summary>
public class RozetkaPayOptionsTests
{
    private const string ProductionUrl = "https://api.rozetkapay.com";
    private const string SandboxUrl = "https://api-epdev.rozetkapay.com";
    private const string Login = "test-login";
    private const string Secret = "test-password";

    // A deliberately low-entropy, obviously fake marker. It exists only so a test can search the
    // validation output for it; it must never look like, or be mistaken for, a real credential.
    private const string CanaryValue = "canary-value-not-a-secret";

    [Fact]
    public void SectionName_ShouldBeTheDocumentedConfigurationSection()
    {
        Assert.Equal("RozetkaPay", RozetkaPayOptions.SectionName);
    }

    [Fact]
    public void EndpointConstants_ShouldMatchTheOfficialOpenApiServers()
    {
        Assert.Equal(ProductionUrl, RozetkaPayOptions.ProductionBaseUrl);
        Assert.Equal(SandboxUrl, RozetkaPayOptions.SandboxBaseUrl);
    }

    [Fact]
    public void Defaults_ShouldMatchTheLegacyConfigurationDefaults()
    {
        RozetkaPayOptions options = new();
        RozetkaPayConfiguration legacy = new() { Login = Login, Password = Secret };

        Assert.Equal(RozetkaPayEnvironment.Production, options.Environment);
        Assert.Null(options.BaseUrl);
        Assert.Equal(string.Empty, options.Login);
        Assert.Equal(string.Empty, options.Password);
        Assert.Null(options.OnBehalfOf);
        Assert.Null(options.CustomerAuth);

        Assert.Equal(legacy.Timeout, options.Timeout);
        Assert.Equal(legacy.UserAgent, options.UserAgent);
        Assert.NotNull(options.RetryPolicy);
        Assert.Equal(legacy.RetryPolicy.Enabled, options.RetryPolicy.Enabled);
        Assert.Equal(legacy.RetryPolicy.MaxRetryAttempts, options.RetryPolicy.MaxRetryAttempts);
        Assert.Equal(legacy.RetryPolicy.BaseDelay, options.RetryPolicy.BaseDelay);
        Assert.Equal(legacy.RetryPolicy.MaxDelay, options.RetryPolicy.MaxDelay);
        Assert.Equal(legacy.RetryPolicy.BackoffStrategy, options.RetryPolicy.BackoffStrategy);
        Assert.Equal(legacy.RetryPolicy.RetriableStatusCodes, options.RetryPolicy.RetriableStatusCodes);
    }

    [Fact]
    public void Defaults_ShouldGiveEachInstanceItsOwnRetryPolicy()
    {
        RozetkaPayOptions first = new();
        RozetkaPayOptions second = new();

        Assert.NotSame(first.RetryPolicy, second.RetryPolicy);
        Assert.NotSame(first.RetryPolicy.RetriableStatusCodes, second.RetryPolicy.RetriableStatusCodes);
    }

    [Fact]
    public void Environment_ShouldBeAnEnumWithStableValues()
    {
        Assert.Equal(0, (int)RozetkaPayEnvironment.Production);
        Assert.Equal(1, (int)RozetkaPayEnvironment.Sandbox);
        Assert.Equal(2, Enum.GetValues<RozetkaPayEnvironment>().Length);
    }

    [Fact]
    public void Options_ShouldNotExposeABooleanEnvironmentFlag()
    {
        PropertyInfo[] properties = typeof(RozetkaPayOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.DoesNotContain(properties, property => property.Name is "UseSandbox" or "IsSandbox" or "IsProduction");

        // The options carry no public boolean at all: the environment is an enum, and the SSL switch that
        // used to be the one exception was removed because it never controlled the HTTP handler.
        Assert.DoesNotContain(properties, property => property.PropertyType == typeof(bool));
    }

    // ---------------------------------------------------------------------------------------------
    // Environment resolution and BaseUrl precedence
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void DefaultEnvironment_ShouldResolveToTheProductionEndpoint()
    {
        RozetkaPayConfiguration snapshot = ResolveSnapshot(options =>
        {
            options.Login = Login;
            options.Password = Secret;
        });

        Assert.Equal(ProductionUrl, snapshot.BaseUrl);
    }

    [Theory]
    [InlineData(RozetkaPayEnvironment.Production, ProductionUrl)]
    [InlineData(RozetkaPayEnvironment.Sandbox, SandboxUrl)]
    public void Environment_ShouldResolveToItsOfficialEndpoint(RozetkaPayEnvironment environment, string expectedUrl)
    {
        RozetkaPayConfiguration snapshot = ResolveSnapshot(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.Environment = environment;
        });

        Assert.Equal(expectedUrl, snapshot.BaseUrl);
    }

    [Theory]
    [InlineData(RozetkaPayEnvironment.Production)]
    [InlineData(RozetkaPayEnvironment.Sandbox)]
    public void ExplicitBaseUrl_ShouldWinOverTheEnvironmentEndpoint(RozetkaPayEnvironment environment)
    {
        RozetkaPayConfiguration snapshot = ResolveSnapshot(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.Environment = environment;
            options.BaseUrl = "https://gateway.example.com";
        });

        Assert.Equal("https://gateway.example.com", snapshot.BaseUrl);
    }

    [Fact]
    public void ExplicitBaseUrl_ShouldBeUsedVerbatim()
    {
        RozetkaPayConfiguration snapshot = ResolveSnapshot(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.BaseUrl = "http://localhost:5005/rozetkapay";
        });

        Assert.Equal("http://localhost:5005/rozetkapay", snapshot.BaseUrl);
    }

    // ---------------------------------------------------------------------------------------------
    // Configuration binding
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void IConfigurationOverload_ShouldBindEveryOptionIncludingNestedRetryPolicy()
    {
        IConfigurationRoot configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["RozetkaPay:Login"] = "cfg-login",
            ["RozetkaPay:Password"] = "cfg-password",
            ["RozetkaPay:Environment"] = "Sandbox",
            ["RozetkaPay:OnBehalfOf"] = "cfg-child",
            ["RozetkaPay:CustomerAuth"] = "cfg-auth",
            ["RozetkaPay:Timeout"] = "00:00:45",
            ["RozetkaPay:UserAgent"] = "CfgAgent/1.0",
            ["RozetkaPay:RetryPolicy:Enabled"] = "true",
            ["RozetkaPay:RetryPolicy:MaxRetryAttempts"] = "4",
            ["RozetkaPay:RetryPolicy:BaseDelay"] = "00:00:02",
            ["RozetkaPay:RetryPolicy:MaxDelay"] = "00:00:12",
            ["RozetkaPay:RetryPolicy:BackoffStrategy"] = "Linear"
        });

        ServiceCollection services = new();
        services.AddRozetkaPay(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        RozetkaPayOptions options = provider.GetRequiredService<IOptions<RozetkaPayOptions>>().Value;

        Assert.Equal("cfg-login", options.Login);
        Assert.Equal("cfg-password", options.Password);
        Assert.Equal(RozetkaPayEnvironment.Sandbox, options.Environment);
        Assert.Null(options.BaseUrl);
        Assert.Equal("cfg-child", options.OnBehalfOf);
        Assert.Equal("cfg-auth", options.CustomerAuth);
        Assert.Equal(TimeSpan.FromSeconds(45), options.Timeout);
        Assert.Equal("CfgAgent/1.0", options.UserAgent);
        Assert.True(options.RetryPolicy.Enabled);
        Assert.Equal(4, options.RetryPolicy.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(2), options.RetryPolicy.BaseDelay);
        Assert.Equal(TimeSpan.FromSeconds(12), options.RetryPolicy.MaxDelay);
        Assert.Equal(BackoffStrategy.Linear, options.RetryPolicy.BackoffStrategy);

        // No explicit BaseUrl in configuration, so the sandbox endpoint is resolved for the SDK.
        Assert.Equal(SandboxUrl, provider.GetRequiredService<RozetkaPayConfiguration>().BaseUrl);
    }

    [Fact]
    public void IConfigurationOverload_ShouldBindTheRetriableStatusCodeCollection()
    {
        IConfigurationRoot configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["RozetkaPay:Login"] = "cfg-login",
            ["RozetkaPay:Password"] = "cfg-password",
            ["RozetkaPay:RetryPolicy:RetriableStatusCodes:0"] = "TooManyRequests",
            ["RozetkaPay:RetryPolicy:RetriableStatusCodes:1"] = "InternalServerError"
        });

        ServiceCollection services = new();
        services.AddRozetkaPay(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        HashSet<HttpStatusCode> codes = provider.GetRequiredService<IOptions<RozetkaPayOptions>>()
            .Value.RetryPolicy.RetriableStatusCodes;

        Assert.Contains(HttpStatusCode.TooManyRequests, codes);
        Assert.Contains(HttpStatusCode.InternalServerError, codes);
    }

    [Fact]
    public void IConfigurationOverload_ShouldHonourAnExplicitBaseUrlOverride()
    {
        IConfigurationRoot configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["RozetkaPay:Login"] = "cfg-login",
            ["RozetkaPay:Password"] = "cfg-password",
            ["RozetkaPay:Environment"] = "Sandbox",
            ["RozetkaPay:BaseUrl"] = "https://gateway.example.com"
        });

        ServiceCollection services = new();
        services.AddRozetkaPay(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Equal("https://gateway.example.com", provider.GetRequiredService<RozetkaPayConfiguration>().BaseUrl);
    }

    [Fact]
    public void IConfigurationOverload_ShouldUseTheCanonicalSectionName()
    {
        IConfigurationRoot configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            [$"{RozetkaPayOptions.SectionName}:Login"] = "cfg-login",
            [$"{RozetkaPayOptions.SectionName}:Password"] = "cfg-password"
        });

        ServiceCollection services = new();
        services.AddRozetkaPay(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Equal("cfg-login", provider.GetRequiredService<IOptions<RozetkaPayOptions>>().Value.Login);
    }

    // ---------------------------------------------------------------------------------------------
    // Action overload
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void ActionOverload_ShouldGuardAgainstNullArguments()
    {
        ServiceCollection? nullServices = null;
        Action<RozetkaPayOptions>? nullConfigure = null;

        Assert.Throws<ArgumentNullException>(() =>
            ServiceCollectionExtensions.AddRozetkaPay(nullServices!, static _ => { }));
        Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddRozetkaPay(nullConfigure!));
    }

    [Fact]
    public void ActionOverload_ShouldRegisterEverySdkServiceAgainstTheSandboxEndpoint()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.Environment = RozetkaPayEnvironment.Sandbox;
            options.Timeout = TimeSpan.FromSeconds(21);
            options.UserAgent = "Options.Tests/1.0";
        });

        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });

        RozetkaPayOptions options = provider.GetRequiredService<IOptions<RozetkaPayOptions>>().Value;
        Assert.Equal(RozetkaPayEnvironment.Sandbox, options.Environment);

        HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("RozetkaPay");
        Assert.Equal(new Uri(SandboxUrl), httpClient.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(21), httpClient.Timeout);
        Assert.Equal("Options.Tests/1.0", httpClient.DefaultRequestHeaders.UserAgent.ToString());

        using IServiceScope scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IRozetkaPayClient>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IPaymentService>());
        Assert.NotNull(provider.GetRequiredService<IRozetkaPayWebhookSignatureVerifier>());
    }

    [Fact]
    public void ActionOverload_ShouldConfigureTheWebhookVerifierWithTheConfiguredPassword()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(options =>
        {
            options.Login = Login;
            options.Password = "verifier-password";
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        IRozetkaPayWebhookSignatureVerifier verifier =
            provider.GetRequiredService<IRozetkaPayWebhookSignatureVerifier>();

        const string body = "{\"id\":\"order-1\"}";

        Assert.True(verifier.Verify(body, ComputeSignature("verifier-password", body)));
        Assert.False(verifier.Verify(body, ComputeSignature("another-password", body)));
    }

    [Fact]
    public void ActionOverload_ShouldSendBasicAuthenticationForTheConfiguredCredentials()
    {
        RozetkaPayConfiguration snapshot = ResolveSnapshot(options =>
        {
            options.Login = "basic-login";
            options.Password = "basic-password";
        });

        string expected = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("basic-login:basic-password"));

        Assert.Equal(expected, snapshot.GetBasicAuthenticationHeader());
    }

    // ---------------------------------------------------------------------------------------------
    // Validation matrix
    // ---------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validation_ShouldRejectAMissingLogin(string? login)
    {
        OptionsValidationException exception = AssertInvalid(options =>
        {
            options.Login = login!;
            options.Password = Secret;
        });

        Assert.Contains("Login", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validation_ShouldRejectAMissingPassword(string? password)
    {
        OptionsValidationException exception = AssertInvalid(options =>
        {
            options.Login = Login;
            options.Password = password!;
        });

        Assert.Contains("Password", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validation_ShouldRejectAnUndefinedEnvironment()
    {
        OptionsValidationException exception = AssertInvalid(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.Environment = (RozetkaPayEnvironment)99;
        });

        Assert.Contains("Environment", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validation_ShouldRejectAnUndefinedEnvironmentBoundFromConfiguration()
    {
        IConfigurationRoot configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["RozetkaPay:Login"] = "cfg-login",
            ["RozetkaPay:Password"] = "cfg-password",
            ["RozetkaPay:Environment"] = "99"
        });

        ServiceCollection services = new();
        services.AddRozetkaPay(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Contains("Environment", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("api.rozetkapay.com")]
    [InlineData("/api/payments")]
    [InlineData("ftp://api.rozetkapay.com")]
    [InlineData("not a url")]
    [InlineData("://broken")]
    public void Validation_ShouldRejectAnInvalidExplicitBaseUrl(string baseUrl)
    {
        OptionsValidationException exception = AssertInvalid(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.BaseUrl = baseUrl;
        });

        Assert.Contains("BaseUrl", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validation_ShouldRejectANonPositiveTimeout(int seconds)
    {
        OptionsValidationException exception = AssertInvalid(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.Timeout = TimeSpan.FromSeconds(seconds);
        });

        Assert.Contains("Timeout", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validation_ShouldRejectANullRetryPolicy()
    {
        OptionsValidationException exception = AssertInvalid(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.RetryPolicy = null!;
        });

        Assert.Contains("RetryPolicy", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validation_ShouldRejectNegativeRetryAttempts()
    {
        OptionsValidationException exception = AssertInvalid(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.RetryPolicy = new RetryPolicy { MaxRetryAttempts = -1 };
        });

        Assert.Contains("MaxRetryAttempts", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validation_ShouldRejectEnabledRetriesWithoutAttempts()
    {
        OptionsValidationException exception = AssertInvalid(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.RetryPolicy = new RetryPolicy { Enabled = true, MaxRetryAttempts = 0 };
        });

        Assert.Contains("MaxRetryAttempts", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validation_ShouldRejectANegativeBaseDelay()
    {
        OptionsValidationException exception = AssertInvalid(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.RetryPolicy = new RetryPolicy { BaseDelay = TimeSpan.FromSeconds(-1) };
        });

        Assert.Contains("BaseDelay", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validation_ShouldRejectANegativeMaxDelay()
    {
        OptionsValidationException exception = AssertInvalid(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.RetryPolicy = new RetryPolicy { MaxDelay = TimeSpan.FromSeconds(-1) };
        });

        Assert.Contains("MaxDelay", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validation_ShouldRejectAMaxDelayBelowTheBaseDelayWhenRetriesAreEnabled()
    {
        OptionsValidationException exception = AssertInvalid(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.RetryPolicy = new RetryPolicy
            {
                Enabled = true,
                MaxRetryAttempts = 3,
                BaseDelay = TimeSpan.FromSeconds(10),
                MaxDelay = TimeSpan.FromSeconds(5)
            };
        });

        Assert.Contains("MaxDelay", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validation_ShouldRejectAnUndefinedBackoffStrategy()
    {
        OptionsValidationException exception = AssertInvalid(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.RetryPolicy = new RetryPolicy { BackoffStrategy = (BackoffStrategy)999 };
        });

        Assert.Contains("BackoffStrategy", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validation_ShouldRejectANullRetriableStatusCodeCollection()
    {
        OptionsValidationException exception = AssertInvalid(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.RetryPolicy = new RetryPolicy { RetriableStatusCodes = null! };
        });

        Assert.Contains("RetriableStatusCodes", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validation_ShouldReportEveryBrokenRuleAtOnce()
    {
        OptionsValidationException exception = AssertInvalid(options =>
        {
            options.Login = "   ";
            options.Password = "   ";
            options.Timeout = TimeSpan.Zero;
        });

        Assert.Contains("Login", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Password", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Timeout", exception.Message, StringComparison.Ordinal);
        Assert.True(exception.Failures.Count() > 1);
    }

    [Fact]
    public void Validation_ShouldAcceptTheProductionDefaults()
    {
        AssertValid(options =>
        {
            options.Login = Login;
            options.Password = Secret;
        });
    }

    [Fact]
    public void Validation_ShouldAcceptTheSandboxEnvironment()
    {
        AssertValid(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.Environment = RozetkaPayEnvironment.Sandbox;
        });
    }

    [Theory]
    [InlineData("https://gateway.example.com")]
    [InlineData("http://localhost:5005")]
    [InlineData("http://127.0.0.1:5005/rozetkapay/")]
    public void Validation_ShouldAcceptAnAbsoluteHttpOrHttpsBaseUrl(string baseUrl)
    {
        AssertValid(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.BaseUrl = baseUrl;
        });
    }

    [Fact]
    public void Validation_ShouldAcceptDisabledRetriesWithZeroAttempts()
    {
        AssertValid(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.RetryPolicy = RetryPolicy.None;
        });
    }

    [Fact]
    public void Validation_ShouldAcceptEnabledRetriesWithPositiveAttempts()
    {
        AssertValid(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.RetryPolicy = RetryPolicy.Standard;
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Startup validation
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void AddRozetkaPay_ShouldRegisterTheStartupValidator()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(options =>
        {
            options.Login = Login;
            options.Password = Secret;
        });

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IStartupValidator));
        Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IValidateOptions<RozetkaPayOptions>) &&
                          descriptor.ImplementationType?.Name == "RozetkaPayOptionsValidator");
    }

    [Fact]
    public void StartupValidation_ShouldFailBeforeAnySdkServiceIsResolved()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.Timeout = TimeSpan.Zero;
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Equal(string.Empty, exception.OptionsName);
        Assert.Equal(typeof(RozetkaPayOptions), exception.OptionsType);
        Assert.NotEmpty(exception.Failures);
    }

    [Fact]
    public void StartupValidation_ShouldPassForAValidConfiguration()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.Environment = RozetkaPayEnvironment.Sandbox;
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IStartupValidator>().Validate();
    }

    [Fact]
    public void InvalidOptions_ShouldAlsoFailWhenTheOptionsValueIsRead()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.BaseUrl = "not a url";
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<RozetkaPayOptions>>().Value);
        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<RozetkaPayConfiguration>());
    }

    // ---------------------------------------------------------------------------------------------
    // Secret safety
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void ValidationFailures_ShouldNotEchoTheConfiguredCredentials()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(options =>
        {
            options.Login = CanaryValue + "-login";
            options.Password = CanaryValue + "-pw";
            options.BaseUrl = "ftp://" + CanaryValue + ".example.com";
            options.Timeout = TimeSpan.Zero;
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.DoesNotContain(CanaryValue, exception.Message, StringComparison.Ordinal);
        Assert.All(
            exception.Failures,
            failure => Assert.DoesNotContain(CanaryValue, failure, StringComparison.Ordinal));
        Assert.DoesNotContain(CanaryValue, exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ServiceDescriptors_ShouldNotExposeTheConfiguredCredentials()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(options =>
        {
            options.Login = CanaryValue + "-login";
            options.Password = CanaryValue + "-pw";
        });

        foreach (ServiceDescriptor descriptor in services)
        {
            Assert.DoesNotContain(CanaryValue, descriptor.ToString() ?? string.Empty, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Validator_ShouldNotDependOnLogging()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(options =>
        {
            options.Login = Login;
            options.Password = Secret;
        });

        ServiceDescriptor descriptor = Assert.Single(
            services,
            entry => entry.ServiceType == typeof(IValidateOptions<RozetkaPayOptions>) &&
                     entry.ImplementationType?.Name == "RozetkaPayOptionsValidator");

        Type? validatorType = descriptor.ImplementationType;
        Assert.NotNull(validatorType);
        ConstructorInfo constructor = Assert.Single(validatorType.GetConstructors());

        // No logger, no configuration, nothing that could write a credential anywhere.
        Assert.Empty(constructor.GetParameters());
    }

    // ---------------------------------------------------------------------------------------------
    // Compatibility with the pre-existing configuration API
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void LegacyConfigurationOverload_ShouldStillProduceADeepClonedSnapshot()
    {
        RetryPolicy policy = new()
        {
            Enabled = true,
            MaxRetryAttempts = 5,
            BaseDelay = TimeSpan.FromSeconds(2),
            MaxDelay = TimeSpan.FromSeconds(15),
            BackoffStrategy = BackoffStrategy.Linear,
            RetriableStatusCodes = [HttpStatusCode.BadGateway]
        };
        RozetkaPayConfiguration source = new()
        {
            BaseUrl = SandboxUrl,
            Login = Login,
            Password = Secret,
            OnBehalfOf = "child",
            CustomerAuth = "auth",
            Timeout = TimeSpan.FromSeconds(77),
            UserAgent = "Legacy/1.0",
            RetryPolicy = policy
        };

        ServiceCollection services = new();
        services.AddRozetkaPay(source);
        using ServiceProvider provider = services.BuildServiceProvider();

        // Mutating the caller-owned instance after registration must not reach the SDK.
        source.Login = "mutated-login";
        source.Password = "mutated-password";
        source.BaseUrl = "https://mutated.example.com";
        source.Timeout = TimeSpan.FromSeconds(1);
        policy.MaxRetryAttempts = 99;
        policy.RetriableStatusCodes.Add(HttpStatusCode.NotFound);

        RozetkaPayConfiguration resolved = provider.GetRequiredService<RozetkaPayConfiguration>();

        Assert.NotSame(source, resolved);
        Assert.NotSame(policy, resolved.RetryPolicy);
        Assert.NotSame(policy.RetriableStatusCodes, resolved.RetryPolicy.RetriableStatusCodes);

        Assert.Equal(SandboxUrl, resolved.BaseUrl);
        Assert.Equal(Login, resolved.Login);
        Assert.Equal(Secret, resolved.Password);
        Assert.Equal("child", resolved.OnBehalfOf);
        Assert.Equal("auth", resolved.CustomerAuth);
        Assert.Equal(TimeSpan.FromSeconds(77), resolved.Timeout);
        Assert.Equal("Legacy/1.0", resolved.UserAgent);
        Assert.True(resolved.RetryPolicy.Enabled);
        Assert.Equal(5, resolved.RetryPolicy.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(2), resolved.RetryPolicy.BaseDelay);
        Assert.Equal(TimeSpan.FromSeconds(15), resolved.RetryPolicy.MaxDelay);
        Assert.Equal(BackoffStrategy.Linear, resolved.RetryPolicy.BackoffStrategy);
        Assert.Equal<HttpStatusCode>([HttpStatusCode.BadGateway], resolved.RetryPolicy.RetriableStatusCodes);
    }

    [Fact]
    public void LegacyConfigurationOverload_ShouldSurfaceThroughTheTypedOptions()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(new RozetkaPayConfiguration
        {
            BaseUrl = SandboxUrl,
            Login = Login,
            Password = Secret
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        RozetkaPayOptions options = provider.GetRequiredService<IOptions<RozetkaPayOptions>>().Value;

        // The legacy BaseUrl is an explicit override, so the environment stays at its default.
        Assert.Equal(SandboxUrl, options.BaseUrl);
        Assert.Equal(RozetkaPayEnvironment.Production, options.Environment);
        Assert.Equal(SandboxUrl, provider.GetRequiredService<RozetkaPayConfiguration>().BaseUrl);
    }

    [Fact]
    public void StringOverload_ShouldDefaultToProductionWithoutAnExplicitBaseUrl()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(Login, Secret);

        using ServiceProvider provider = services.BuildServiceProvider();
        RozetkaPayOptions options = provider.GetRequiredService<IOptions<RozetkaPayOptions>>().Value;

        Assert.Equal(RozetkaPayEnvironment.Production, options.Environment);
        Assert.Null(options.BaseUrl);
        Assert.Equal(ProductionUrl, provider.GetRequiredService<RozetkaPayConfiguration>().BaseUrl);
    }

    [Fact]
    public void StringOverload_ShouldKeepAnExplicitBaseUrl()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(Login, Secret, SandboxUrl);

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Equal(SandboxUrl, provider.GetRequiredService<IOptions<RozetkaPayOptions>>().Value.BaseUrl);
        Assert.Equal(SandboxUrl, provider.GetRequiredService<RozetkaPayConfiguration>().BaseUrl);
    }

    [Fact]
    public void LegacyOverloads_ShouldKeepTheirRegistrationTimeFailFast()
    {
        InvalidOperationException invalidConfiguration = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddRozetkaPay(new RozetkaPayConfiguration
            {
                BaseUrl = "not-a-valid-url",
                Login = Login,
                Password = Secret
            }));
        Assert.Contains("configuration is invalid", invalidConfiguration.Message, StringComparison.OrdinalIgnoreCase);

        InvalidOperationException missingSection = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddRozetkaPay(BuildConfiguration(new Dictionary<string, string?>())));
        Assert.Contains("RozetkaPay section is not configured", missingSection.Message, StringComparison.Ordinal);

        InvalidOperationException missingLogin = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddRozetkaPay(BuildConfiguration(new Dictionary<string, string?>
            {
                ["RozetkaPay:Password"] = "cfg-password"
            })));
        Assert.Contains("RozetkaPay:Login", missingLogin.Message, StringComparison.Ordinal);

        InvalidOperationException missingPassword = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddRozetkaPay(BuildConfiguration(new Dictionary<string, string?>
            {
                ["RozetkaPay:Login"] = "cfg-login"
            })));
        Assert.Contains("RozetkaPay:Password", missingPassword.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LegacyConfigurationOverload_ShouldStillBeValidatedAtStartup()
    {
        // Well formed enough for the legacy registration-time check, but a scheme the SDK cannot speak.
        // Before the options pipeline this only failed once a request was attempted.
        ServiceCollection services = new();
        services.AddRozetkaPay(new RozetkaPayConfiguration
        {
            BaseUrl = "ftp://api.rozetkapay.com",
            Login = Login,
            Password = Secret
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Contains("BaseUrl", exception.Message, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------------------------
    // Registration semantics and lifetimes
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void CompatibilitySnapshot_ShouldBeASingletonSharedByEveryConsumer()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(options =>
        {
            options.Login = Login;
            options.Password = Secret;
        });

        ServiceDescriptor descriptor = Assert.Single(
            services, entry => entry.ServiceType == typeof(RozetkaPayConfiguration));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);

        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope firstScope = provider.CreateScope();
        using IServiceScope secondScope = provider.CreateScope();

        RozetkaPayConfiguration fromRoot = provider.GetRequiredService<RozetkaPayConfiguration>();

        Assert.Same(fromRoot, firstScope.ServiceProvider.GetRequiredService<RozetkaPayConfiguration>());
        Assert.Same(fromRoot, secondScope.ServiceProvider.GetRequiredService<RozetkaPayConfiguration>());

        Assert.NotSame(
            firstScope.ServiceProvider.GetRequiredService<IPaymentService>(),
            secondScope.ServiceProvider.GetRequiredService<IPaymentService>());
        Assert.Same(
            firstScope.ServiceProvider.GetRequiredService<IRozetkaPayWebhookSignatureVerifier>(),
            secondScope.ServiceProvider.GetRequiredService<IRozetkaPayWebhookSignatureVerifier>());
    }

    [Fact]
    public void CompatibilitySnapshot_ShouldAgreeWithTheOptionsValue()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.Environment = RozetkaPayEnvironment.Sandbox;
            options.OnBehalfOf = "child";
            options.CustomerAuth = "auth";
            options.Timeout = TimeSpan.FromSeconds(33);
            options.UserAgent = "Agreement/1.0";
            options.RetryPolicy = RetryPolicy.Standard;
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        RozetkaPayOptions options = provider.GetRequiredService<IOptions<RozetkaPayOptions>>().Value;
        RozetkaPayConfiguration snapshot = provider.GetRequiredService<RozetkaPayConfiguration>();

        Assert.Equal(SandboxUrl, snapshot.BaseUrl);
        Assert.Equal(options.Login, snapshot.Login);
        Assert.Equal(options.Password, snapshot.Password);
        Assert.Equal(options.OnBehalfOf, snapshot.OnBehalfOf);
        Assert.Equal(options.CustomerAuth, snapshot.CustomerAuth);
        Assert.Equal(options.Timeout, snapshot.Timeout);
        Assert.Equal(options.UserAgent, snapshot.UserAgent);
        Assert.Equal(options.RetryPolicy.Enabled, snapshot.RetryPolicy.Enabled);
        Assert.Equal(options.RetryPolicy.MaxRetryAttempts, snapshot.RetryPolicy.MaxRetryAttempts);
        Assert.Equal(options.RetryPolicy.RetriableStatusCodes, snapshot.RetryPolicy.RetriableStatusCodes);

        // The snapshot is a copy, so mutating it cannot corrupt the options value shared with consumers.
        Assert.NotSame(options.RetryPolicy, snapshot.RetryPolicy);
        Assert.NotSame(options.RetryPolicy.RetriableStatusCodes, snapshot.RetryPolicy.RetriableStatusCodes);
    }

    [Theory]
    [InlineData("action-action")]
    [InlineData("action-configuration")]
    [InlineData("configuration-action")]
    [InlineData("configuration-configuration")]
    [InlineData("legacy-action")]
    public void RepeatedRegistration_ShouldKeepTheFirstConfiguration(string sequence)
    {
        ServiceCollection services = new();

        switch (sequence)
        {
            case "action-action":
                AddFirstViaAction(services);
                AddSecondViaAction(services);
                break;
            case "action-configuration":
                AddFirstViaAction(services);
                AddSecondViaConfiguration(services);
                break;
            case "configuration-action":
                AddFirstViaConfiguration(services);
                AddSecondViaAction(services);
                break;
            case "configuration-configuration":
                AddFirstViaConfiguration(services);
                AddSecondViaConfiguration(services);
                break;
            default:
                AddFirstViaLegacyConfiguration(services);
                AddSecondViaAction(services);
                break;
        }

        using ServiceProvider provider = services.BuildServiceProvider();
        RozetkaPayOptions options = provider.GetRequiredService<IOptions<RozetkaPayOptions>>().Value;

        Assert.Equal("first-login", options.Login);
        Assert.Equal(RozetkaPayEnvironment.Production, options.Environment);
        Assert.Equal(ProductionUrl, provider.GetRequiredService<RozetkaPayConfiguration>().BaseUrl);
    }

    [Fact]
    public void RepeatedRegistration_ShouldNotDuplicateOptionsInfrastructure()
    {
        ServiceCollection services = new();
        AddFirstViaAction(services);
        AddSecondViaAction(services);
        AddSecondViaConfiguration(services);

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IStartupValidator));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(RozetkaPayConfiguration));
        Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IConfigureOptions<RozetkaPayOptions>));
        Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IValidateOptions<RozetkaPayOptions>) &&
                          descriptor.ImplementationType?.Name == "RozetkaPayOptionsValidator");
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(PaymentService));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IPaymentService));
    }

    [Fact]
    public void RepeatedRegistration_ShouldNotDuplicateNamedHttpClientConfiguration()
    {
        ServiceCollection services = new();
        AddFirstViaAction(services);
        AddSecondViaAction(services);

        using ServiceProvider provider = services.BuildServiceProvider();
        HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("RozetkaPay");

        Assert.Equal(new Uri(ProductionUrl), httpClient.BaseAddress);
        Assert.Equal("First/1.0", httpClient.DefaultRequestHeaders.UserAgent.ToString());
    }

    [Fact]
    public void ConsumerRegisteredInterface_ShouldStillWinOverTheOptionsRegistration()
    {
        ServiceCollection services = new();
        services.AddScoped<IPaymentService, TestInfrastructure.FakePaymentService>();
        services.AddRozetkaPay(options =>
        {
            options.Login = Login;
            options.Password = Secret;
        });

        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope scope = provider.CreateScope();

        Assert.IsType<TestInfrastructure.FakePaymentService>(
            scope.ServiceProvider.GetRequiredService<IPaymentService>());
        Assert.IsType<PaymentService>(scope.ServiceProvider.GetRequiredService<PaymentService>());
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    private static void AddFirstViaAction(IServiceCollection services)
    {
        services.AddRozetkaPay(options =>
        {
            options.Login = "first-login";
            options.Password = "first-password";
            options.UserAgent = "First/1.0";
        });
    }

    private static void AddSecondViaAction(IServiceCollection services)
    {
        services.AddRozetkaPay(options =>
        {
            options.Login = "second-login";
            options.Password = "second-password";
            options.Environment = RozetkaPayEnvironment.Sandbox;
            options.UserAgent = "Second/1.0";
        });
    }

    private static void AddFirstViaConfiguration(IServiceCollection services)
    {
        services.AddRozetkaPay(BuildConfiguration(new Dictionary<string, string?>
        {
            ["RozetkaPay:Login"] = "first-login",
            ["RozetkaPay:Password"] = "first-password",
            ["RozetkaPay:UserAgent"] = "First/1.0"
        }));
    }

    private static void AddSecondViaConfiguration(IServiceCollection services)
    {
        services.AddRozetkaPay(BuildConfiguration(new Dictionary<string, string?>
        {
            ["RozetkaPay:Login"] = "second-login",
            ["RozetkaPay:Password"] = "second-password",
            ["RozetkaPay:Environment"] = "Sandbox"
        }));
    }

    private static void AddFirstViaLegacyConfiguration(IServiceCollection services)
    {
        services.AddRozetkaPay(new RozetkaPayConfiguration
        {
            BaseUrl = ProductionUrl,
            Login = "first-login",
            Password = "first-password",
            UserAgent = "First/1.0"
        });
    }

    private static IConfigurationRoot BuildConfiguration(Dictionary<string, string?> settings)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    private static RozetkaPayConfiguration ResolveSnapshot(Action<RozetkaPayOptions> configure)
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(configure);
        using ServiceProvider provider = services.BuildServiceProvider();

        return provider.GetRequiredService<RozetkaPayConfiguration>();
    }

    private static OptionsValidationException AssertInvalid(Action<RozetkaPayOptions> configure)
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(configure);
        using ServiceProvider provider = services.BuildServiceProvider();

        return Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());
    }

    private static void AssertValid(Action<RozetkaPayOptions> configure)
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(configure);
        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IStartupValidator>().Validate();
        Assert.NotNull(provider.GetRequiredService<RozetkaPayConfiguration>());
    }

    /// <summary>
    /// Independent reimplementation of the documented callback signature, used to prove the verifier was
    /// wired with the password that came from the options.
    /// </summary>
    private static string ComputeSignature(string password, string body)
    {
        string inner = Convert.ToBase64String(Encoding.UTF8.GetBytes(body))
            .Replace('+', '-')
            .Replace('/', '_');
        byte[] digest = SHA1.HashData(Encoding.UTF8.GetBytes(password + inner + password));

        return Convert.ToBase64String(digest).Replace('+', '-').Replace('/', '_');
    }
}
