using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Extensions;
using SYT.RozetkaPay.Services;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// The transport boundary that keeps merchant secrets on the merchant's gateway: redirects are not
/// followed, and clear text is not spoken.
/// </summary>
/// <remarks>
/// <para>
/// A <c>302</c> is not a safe instruction to repeat a request elsewhere. <see cref="HttpClientHandler"/>
/// drops <c>Authorization</c> when a redirect crosses an origin, but it forwards every other header
/// verbatim - including <c>X-ON-BEHALF-OF</c> and <c>X-CUSTOMER-AUTH</c>, which are merchant secrets in
/// their own right. A compromised host, or merely a misconfigured one, could harvest both by answering
/// with a <c>Location</c> it controls. Refusing the redirect is the only guarantee that does not depend
/// on which headers a particular runtime version decides to strip.
/// </para>
/// <para>
/// The clear-text tests are the other half of the same boundary: over <c>http</c> the Basic credential
/// and both secret headers are readable by anything on the path, so no redirect is needed to lose them.
/// </para>
/// </remarks>
public class RedirectSecurityTests
{
    private const string Login = "test-login";
    private const string Secret = "test-password";

    /// <summary>
    /// Name of the authenticated named client every SDK service resolves.
    /// </summary>
    private const string AuthenticatedHttpClientName = "RozetkaPay";

    /// <summary>
    /// Name of the dedicated client used by the unauthenticated decline operation.
    /// </summary>
    private const string NonRedirectHttpClientName = "RozetkaPay.PaymentInstructions.Decline";

    [Theory]
    [InlineData(AuthenticatedHttpClientName)]
    [InlineData(NonRedirectHttpClientName)]
    public void SdkClients_ShouldNotFollowRedirects(string clientName)
    {
        using ServiceProvider provider = BuildProvider(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.OnBehalfOf = "on-behalf-secret";
            options.CustomerAuth = "customer-auth-secret";
        });

        HttpMessageHandler handler = ResolvePrimaryHandler(
            provider.GetRequiredService<IHttpClientFactory>(),
            clientName);

        HttpClientHandler clientHandler = Assert.IsType<HttpClientHandler>(handler);
        Assert.False(clientHandler.AllowAutoRedirect);
    }

    [Theory]
    [InlineData("http://gateway.example.com")]
    [InlineData("http://203.0.113.10:8080/rozetkapay/")]
    public void Validation_ShouldRejectPlainHttpOnANonLoopbackHost(string baseUrl)
    {
        OptionsValidationException failure = AssertInvalid(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.BaseUrl = baseUrl;
        });

        Assert.Contains(
            "https",
            string.Join(" ", failure.Failures),
            StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("http://localhost:5005")]
    [InlineData("http://127.0.0.1:5005/rozetkapay/")]
    public void Validation_ShouldRejectLoopbackHttpUnlessExplicitlyAllowed(string baseUrl)
    {
        AssertInvalid(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.BaseUrl = baseUrl;
        });
    }

    [Theory]
    [InlineData("http://localhost:5005")]
    [InlineData("http://127.0.0.1:5005/rozetkapay/")]
    public void Validation_ShouldAcceptLoopbackHttpWhenExplicitlyAllowed(string baseUrl)
    {
        using ServiceProvider provider = BuildProvider(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.BaseUrl = baseUrl;
            options.TransportSecurity = RozetkaPayTransportSecurity.AllowClearTextLoopback;
        });

        provider.GetRequiredService<IStartupValidator>().Validate();

        Assert.NotNull(provider.GetRequiredService<RozetkaPayConfiguration>());
    }

    [Fact]
    public void Validation_ShouldRejectNonLoopbackHttpEvenWhenLoopbackIsAllowed()
    {
        // The switch names loopback and means it. A single test-oriented setting must not be able to
        // downgrade a production gateway to clear text.
        AssertInvalid(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.BaseUrl = "http://gateway.example.com";
            options.TransportSecurity = RozetkaPayTransportSecurity.AllowClearTextLoopback;
        });
    }

    [Fact]
    public void Validation_ShouldStillAcceptHttpsWhenLoopbackIsAllowed()
    {
        using ServiceProvider provider = BuildProvider(options =>
        {
            options.Login = Login;
            options.Password = Secret;
            options.BaseUrl = "https://gateway.example.com";
            options.TransportSecurity = RozetkaPayTransportSecurity.AllowClearTextLoopback;
        });

        provider.GetRequiredService<IStartupValidator>().Validate();

        Assert.NotNull(provider.GetRequiredService<RozetkaPayConfiguration>());
    }

    // ---------------------------------------------------------------------------------------------
    // The non-DI surface. A service or client built directly never touches the options pipeline, so
    // every guarantee above has to hold here independently - an earlier revision enforced both only on
    // the DI path, and a directly constructed service sent credentials over clear text.
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void ClientOwnedTransport_ShouldNotFollowRedirects()
    {
        using RozetkaPayClient client = new(HttpsConfiguration());

        HttpClient owned = OwnedHttpClientOf(client);
        HttpClientHandler handler = Assert.IsType<HttpClientHandler>(PrimaryHandlerOf(owned));

        Assert.False(handler.AllowAutoRedirect);
    }

    [Theory]
    [InlineData("http://payments.example.test")]
    [InlineData("http://203.0.113.10:8080/rozetkapay/")]
    public void DirectlyConstructedService_ShouldRefuseAClearTextEndpoint(string baseUrl)
    {
        RozetkaPayConfiguration configuration = HttpsConfiguration();
        configuration.BaseUrl = baseUrl;

        using HttpClient httpClient = new();

        ArgumentException failure = Assert.Throws<ArgumentException>(
            () => new PaymentService(configuration, httpClient));

        Assert.Contains("https", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DirectlyConstructedClient_ShouldRefuseAClearTextEndpoint()
    {
        RozetkaPayConfiguration configuration = HttpsConfiguration();
        configuration.BaseUrl = "http://payments.example.test";

        Assert.Throws<ArgumentException>(() => new RozetkaPayClient(configuration));
    }

    [Fact]
    public void DirectlyConstructedService_ShouldAcceptLoopbackHttpWhenExplicitlyAllowed()
    {
        RozetkaPayConfiguration configuration = HttpsConfiguration();
        configuration.BaseUrl = "http://127.0.0.1:5005";
        configuration.TransportSecurity = RozetkaPayTransportSecurity.AllowClearTextLoopback;

        using HttpClient httpClient = new();

        Assert.NotNull(new PaymentService(configuration, httpClient));
    }

    [Fact]
    public void DirectlyConstructedService_ShouldRefuseNonLoopbackHttpEvenWhenLoopbackIsAllowed()
    {
        RozetkaPayConfiguration configuration = HttpsConfiguration();
        configuration.BaseUrl = "http://payments.example.test";
        configuration.TransportSecurity = RozetkaPayTransportSecurity.AllowClearTextLoopback;

        using HttpClient httpClient = new();

        Assert.Throws<ArgumentException>(() => new PaymentService(configuration, httpClient));
    }

    private static RozetkaPayConfiguration HttpsConfiguration() => new()
    {
        BaseUrl = RozetkaPayOptions.ProductionBaseUrl,
        Login = Login,
        Password = Secret,
        OnBehalfOf = "on-behalf-secret",
        CustomerAuth = "customer-auth-secret"
    };

    /// <summary>
    /// The <see cref="HttpClient"/> a <see cref="RozetkaPayClient"/> created for itself.
    /// </summary>
    /// <remarks>
    /// Not exposed on the public surface, and deliberately so - but whether the SDK disables redirects
    /// on the transport it owns is a security property, and asserting it needs the instance.
    /// </remarks>
    private static HttpClient OwnedHttpClientOf(RozetkaPayClient client)
    {
        FieldInfo field = typeof(RozetkaPayClient).GetField(
            "HttpClient",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "RozetkaPayClient no longer holds its transport in a field named 'HttpClient'. Update " +
                "this walk rather than dropping the assertion.");

        return (HttpClient)field.GetValue(client)!;
    }

    private static HttpMessageHandler PrimaryHandlerOf(HttpClient client)
    {
        FieldInfo field = typeof(HttpMessageInvoker).GetField(
            "_handler",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "HttpMessageInvoker no longer exposes its handler through the '_handler' field.");

        object? current = field.GetValue(client);

        while (current is DelegatingHandler delegating)
        {
            current = delegating.InnerHandler;
        }

        return current as HttpMessageHandler
            ?? throw new InvalidOperationException("The handler chain did not end in an HttpMessageHandler.");
    }

    private static ServiceProvider BuildProvider(Action<RozetkaPayOptions> configure)
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(configure);
        return services.BuildServiceProvider();
    }

    private static OptionsValidationException AssertInvalid(Action<RozetkaPayOptions> configure)
    {
        using ServiceProvider provider = BuildProvider(configure);

        return Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());
    }

    /// <summary>
    /// Walk the handler chain of a named client down to the primary handler.
    /// </summary>
    /// <remarks>
    /// <see cref="IHttpClientFactory"/> exposes no way to inspect the chain it built, and the redirect
    /// policy lives on the primary handler rather than on anything the factory surfaces. Reading the
    /// private field is the only way to assert the policy without making a real network request, and a
    /// real request would prove nothing here: the assertion is about what the client would do if a
    /// gateway answered with a <c>Location</c>, not about any particular gateway.
    /// </remarks>
    private static HttpMessageHandler ResolvePrimaryHandler(IHttpClientFactory factory, string name)
    {
        HttpClient client = factory.CreateClient(name);

        FieldInfo field = typeof(HttpMessageInvoker).GetField(
            "_handler",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "HttpMessageInvoker no longer exposes its handler through the '_handler' field. This " +
                "test reads it to assert the redirect policy; update the walk rather than deleting the " +
                "assertion.");

        object? current = field.GetValue(client);

        while (current is DelegatingHandler delegating)
        {
            current = delegating.InnerHandler;
        }

        return current as HttpMessageHandler
            ?? throw new InvalidOperationException("The handler chain did not end in an HttpMessageHandler.");
    }
}
