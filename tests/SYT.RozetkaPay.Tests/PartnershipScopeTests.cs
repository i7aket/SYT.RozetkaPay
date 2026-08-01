using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;
using Xunit;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Partnership mode: one core account acting for many child merchants (EXP-459).
/// </summary>
/// <remarks>
/// The header these tests watch decides <b>who gets the money</b>. That is why the blank case is here and
/// why it asserts on the absence of an HTTP call rather than on an exception alone: a child identifier that
/// quietly degraded to "no header" would not fail — the provider would accept the payment for the platform's
/// own account, and the expert would simply never be paid.
/// </remarks>
public class PartnershipScopeTests
{
    private const string HeaderName = "X-ON-BEHALF-OF";
    private const string ChildMerchant = "expert-merchant-1";

    private static CreatePaymentRequest Body() => new()
    {
        Amount = 100m,
        Currency = "UAH",
        ExternalId = "order-1",
        Mode = PaymentMode.Hosted,
    };

    private static RozetkaPayConfiguration Configuration(string? onBehalfOf = null) => new()
    {
        BaseUrl = RozetkaPayOptions.ProductionBaseUrl,
        Login = "probe-login",
        Password = "probe-password",
        OnBehalfOf = onBehalfOf,
        RetryPolicy = RetryPolicy.None,
    };

    /// <summary>Records every dispatched request; the SDK's own stub keeps only the last one.</summary>
    private sealed class HeaderRecorder
    {
        public List<string?> Dispatched { get; } = [];

        public StubHttpMessageHandler Handler => new((request, _) =>
        {
            Dispatched.Add(
                request.Headers.TryGetValues(HeaderName, out IEnumerable<string>? values)
                    ? string.Join(',', values)
                    : null);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            });
        });
    }

    [Fact]
    public async Task ActingFor_sends_the_child_merchant_in_the_header()
    {
        HeaderRecorder recorder = new();
        using HttpClient httpClient = new(recorder.Handler);
        IPaymentService platform = new PaymentService(Configuration(), httpClient);

        await platform.ActingFor(ChildMerchant).CreateAsync(Body());

        Assert.Equal([ChildMerchant], recorder.Dispatched);
    }

    [Fact]
    public async Task Without_ActingFor_the_configured_value_is_used()
    {
        HeaderRecorder recorder = new();
        using HttpClient httpClient = new(recorder.Handler);
        IPaymentService service = new PaymentService(Configuration("configured-partner"), httpClient);

        await service.CreateAsync(Body());

        Assert.Equal(["configured-partner"], recorder.Dispatched);
    }

    [Fact]
    public async Task Without_a_scope_or_configuration_no_header_is_sent()
    {
        HeaderRecorder recorder = new();
        using HttpClient httpClient = new(recorder.Handler);
        IPaymentService service = new PaymentService(Configuration(), httpClient);

        await service.CreateAsync(Body());

        Assert.Equal([null], recorder.Dispatched);
    }

    /// <summary>A per-call scope replaces the configured merchant; it does not append to it.</summary>
    [Fact]
    public async Task ActingFor_overrides_the_configured_value()
    {
        HeaderRecorder recorder = new();
        using HttpClient httpClient = new(recorder.Handler);
        IPaymentService service = new PaymentService(Configuration("configured-partner"), httpClient);

        await service.ActingFor(ChildMerchant).CreateAsync(Body());

        Assert.Equal([ChildMerchant], recorder.Dispatched);
    }

    /// <summary>
    /// The platform case: many experts served from one service instance, without a client per expert.
    /// </summary>
    [Fact]
    public async Task One_service_serves_several_children()
    {
        HeaderRecorder recorder = new();
        using HttpClient httpClient = new(recorder.Handler);
        IPaymentService platform = new PaymentService(Configuration(), httpClient);

        await platform.ActingFor("expert-a").CreateAsync(Body());
        await platform.ActingFor("expert-b").CreateAsync(Body());

        Assert.Equal(["expert-a", "expert-b"], recorder.Dispatched);
    }

    /// <summary>Scoping produces a new service; the original is not mutated by it.</summary>
    [Fact]
    public async Task The_original_service_keeps_its_own_scope()
    {
        HeaderRecorder recorder = new();
        using HttpClient httpClient = new(recorder.Handler);
        IPaymentService platform = new PaymentService(Configuration(), httpClient);

        IPaymentService scoped = platform.ActingFor(ChildMerchant);
        await scoped.CreateAsync(Body());
        await platform.CreateAsync(Body());

        Assert.Equal([ChildMerchant, null], recorder.Dispatched);
    }

    /// <summary>
    /// The dangerous case. A blank identifier must not be read as "act as the platform": the request would
    /// succeed, the provider would book the payment to the core account, and the expert would never be paid.
    /// Nothing about that failure is visible in a log or a status code.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task A_blank_child_merchant_is_refused_before_any_request(string blank)
    {
        HeaderRecorder recorder = new();
        using HttpClient httpClient = new(recorder.Handler);
        IPaymentService platform = new PaymentService(Configuration(), httpClient);

        Assert.Throws<ArgumentException>(() => platform.ActingFor(blank));

        // The absence of a dispatch is the point: an exception thrown after the money moved would be no help.
        Assert.Empty(recorder.Dispatched);
        await Task.CompletedTask;
    }

    [Fact]
    public void A_null_child_merchant_is_refused()
    {
        using HttpClient httpClient = new(new HeaderRecorder().Handler);
        IPaymentService platform = new PaymentService(Configuration(), httpClient);

        Assert.Throws<ArgumentException>(() => platform.ActingFor(null!));
    }

    /// <summary>
    /// An identifier that cannot be a header value fails while scoping, not on the first payment.
    /// </summary>
    /// <remarks>
    /// The exception type is <see cref="FormatException"/> rather than <see cref="ArgumentException"/>
    /// because scoping goes through the same header validation as the configured value does, and that has
    /// reported bad header syntax as a format error since EXP-341. Two exception types for one mistake,
    /// depending only on whether the merchant arrived from configuration or from a call, would be a contract
    /// a caller cannot write a single catch for.
    /// </remarks>
    [Theory]
    [InlineData("bad\nvalue")]
    [InlineData("bad\rvalue")]
    public void An_unusable_header_value_is_refused_while_scoping(string invalid)
    {
        using HttpClient httpClient = new(new HeaderRecorder().Handler);
        IPaymentService platform = new PaymentService(Configuration(), httpClient);

        Assert.Throws<FormatException>(() => platform.ActingFor(invalid));
    }

    /// <summary>
    /// A per-call merchant identifier does not reach a log sink.
    /// </summary>
    /// <remarks>
    /// <c>LegacyLoggingRedactionTests</c> already proves this for the value that comes from configuration.
    /// It does not cover this one: the scoped service is constructed at call time and carries a value the
    /// configuration never held, so the two travel different routes into the logger. A merchant identifier
    /// is a business relationship — who a platform pays, and how many of them there are — and once it is in
    /// a sink it is in whatever ships that sink onward.
    /// </remarks>
    [Fact]
    public async Task A_scoped_merchant_identifier_does_not_reach_a_log()
    {
        const string marker = "child-merchant-must-never-be-logged-EXP459";

        HeaderRecorder recorder = new();
        CapturingLoggerProvider logs = new();
        using ILoggerFactory factory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddProvider(logs);
        });

        using HttpClient httpClient = new(recorder.Handler);
        IPaymentService platform = new PaymentService(
            Configuration(),
            httpClient,
            factory.CreateLogger<PaymentService>());

        await platform.ActingFor(marker).CreateAsync(Body());

        // Two guards against "not logged" being true for the wrong reason: the identifier really did
        // travel, and the scoped service really did write to this sink. A service that logged nothing at
        // all would satisfy the assertion below while proving nothing.
        Assert.Equal([marker], recorder.Dispatched);
        Assert.NotEmpty(logs.Entries);

        LoggingRedactionAssert.NotLogged(logs, marker);
        LoggingRedactionAssert.NotLogged(logs, HeaderName);
    }

    /// <summary>
    /// Scoping copies the whole configuration. A hand-written copy would keep compiling while dropping a
    /// property added later — most likely a credential or a timeout, discovered in production.
    /// </summary>
    [Fact]
    public void Scoping_carries_every_other_setting_across()
    {
        RozetkaPayConfiguration original = new()
        {
            BaseUrl = RozetkaPayOptions.ProductionBaseUrl,
            Login = "probe-login",
            Password = "probe-password",
            CustomerAuth = "customer-token",
            UserAgent = "SYT.RozetkaPay.Tests-EXP-459",
            Timeout = TimeSpan.FromSeconds(17),
            TransportSecurity = RozetkaPayTransportSecurity.HttpsOnly,
            RetryPolicy = RetryPolicy.None,
            OnBehalfOf = "configured-partner",
        };

        RozetkaPayConfiguration scoped = original.WithOnBehalfOf(ChildMerchant);

        Assert.Equal(ChildMerchant, scoped.OnBehalfOf);
        Assert.Equal("configured-partner", original.OnBehalfOf);

        Assert.Equal(original.BaseUrl, scoped.BaseUrl);
        Assert.Equal(original.Login, scoped.Login);
        Assert.Equal(original.Password, scoped.Password);
        Assert.Equal(original.CustomerAuth, scoped.CustomerAuth);
        Assert.Equal(original.UserAgent, scoped.UserAgent);
        Assert.Equal(original.Timeout, scoped.Timeout);
        Assert.Equal(original.TransportSecurity, scoped.TransportSecurity);
        Assert.Same(original.RetryPolicy, scoped.RetryPolicy);
    }

    /// <summary>
    /// A gate against the copy silently going stale: every settable property must be carried over. If a
    /// property is added to the configuration and the copy stops covering it, this fails rather than a
    /// payment quietly losing a timeout or a credential.
    /// </summary>
    [Fact]
    public void Every_configuration_property_is_carried_across()
    {
        RozetkaPayConfiguration original = new()
        {
            BaseUrl = RozetkaPayOptions.ProductionBaseUrl,
            Login = "probe-login",
            Password = "probe-password",
            CustomerAuth = "customer-token",
            UserAgent = "probe-agent",
            Timeout = TimeSpan.FromSeconds(23),
            RetryPolicy = RetryPolicy.None,
        };

        RozetkaPayConfiguration scoped = original.WithOnBehalfOf(ChildMerchant);

        List<string> divergent = [.. typeof(RozetkaPayConfiguration)
            .GetProperties()
            .Where(property => property.CanRead && property.CanWrite)
            .Where(property => property.Name != nameof(RozetkaPayConfiguration.OnBehalfOf))
            .Where(property => !Equals(property.GetValue(original), property.GetValue(scoped)))
            .Select(property => property.Name)];

        Assert.True(
            divergent.Count == 0,
            "these settings did not survive scoping: " + string.Join(", ", divergent));
    }
}
