using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Services;

namespace SYT.RozetkaPay.Tests.TestInfrastructure;

/// <summary>
/// Builds every canonical SDK service over one <see cref="ContractRecordingHandler"/>, so a manifest row
/// can invoke a real service and the transport can capture the request it produced.
/// </summary>
/// <remarks>
/// <para>
/// The base address is in the reserved <c>.invalid</c> TLD (RFC 2606), so a regression that bypassed the
/// recording handler could not resolve a host, let alone reach RozetkaPay. Every row additionally asserts
/// the observed host.
/// </para>
/// <para>
/// Retries are disabled and each service gets its own <see cref="HttpClient"/> over the shared handler,
/// because <c>BaseService</c> configures default headers on the client it is handed. The handler is owned
/// by the caller that created it; the clients created here are disposed with this host.
/// </para>
/// </remarks>
internal sealed class ContractServiceHost : IDisposable
{
    /// <summary>Unroutable base address every contract request is sent to.</summary>
    internal const string BaseUrl = "https://openapi-contract.invalid";

    /// <summary>Host component of <see cref="BaseUrl"/>, asserted per row.</summary>
    internal const string Host = "openapi-contract.invalid";

    /// <summary>Placeholder API login. Not a credential and never used against a live endpoint.</summary>
    internal const string LoginPlaceholder = "sandbox-login";

    /// <summary>Placeholder API password. Not a credential and never used against a live endpoint.</summary>
    internal const string PasswordPlaceholder = "sandbox-password";

    /// <summary>
    /// The exact plain text a correct Basic header must decode to. The transport decodes what the SDK
    /// sent and compares against this, rather than re-encoding the expectation.
    /// </summary>
    internal const string ExpectedBasicCredentials = $"{LoginPlaceholder}:{PasswordPlaceholder}";

    /// <summary>Placeholder <c>X-ON-BEHALF-OF</c> value.</summary>
    internal const string OnBehalfOfPlaceholder = "partner-id";

    /// <summary>Placeholder <c>X-CUSTOMER-AUTH</c> value.</summary>
    internal const string CustomerAuthPlaceholder = "customer-token";

    /// <summary>User agent the contract configuration sends.</summary>
    internal const string UserAgentPlaceholder = "SYT.RozetkaPay.Tests-EXP-337";

    private readonly ContractRecordingHandler _handler;

    private readonly RozetkaPayConfiguration _configuration;

    private readonly List<HttpClient> _clients = [];

    private readonly List<IDisposable> _disposables = [];

    private bool _disposed;

    /// <summary>
    /// Create a host over a recording transport.
    /// </summary>
    /// <param name="handler">Controlled transport. Not disposed by this host.</param>
    /// <param name="configuration">
    /// Configuration to build the services with. Defaults to <see cref="CreateConfiguration"/>, which
    /// sets both optional headers so that the anonymous-operation assertions are meaningful.
    /// </param>
    internal ContractServiceHost(ContractRecordingHandler handler, RozetkaPayConfiguration? configuration = null)
    {
        _handler = handler;
        _configuration = configuration ?? CreateConfiguration();
    }

    /// <summary>Contract configuration: placeholder credentials plus both optional headers.</summary>
    internal static RozetkaPayConfiguration CreateConfiguration()
    {
        return new RozetkaPayConfiguration
        {
            BaseUrl = BaseUrl,
            Login = LoginPlaceholder,
            Password = PasswordPlaceholder,
            OnBehalfOf = OnBehalfOfPlaceholder,
            CustomerAuth = CustomerAuthPlaceholder,
            UserAgent = UserAgentPlaceholder,
            RetryPolicy = RetryPolicy.None
        };
    }

    /// <summary>
    /// Contract configuration with neither optional header set, used to prove that the SDK sends them
    /// only when they are configured.
    /// </summary>
    internal static RozetkaPayConfiguration CreateConfigurationWithoutOptionalHeaders()
    {
        RozetkaPayConfiguration configuration = CreateConfiguration();
        configuration.OnBehalfOf = null;
        configuration.CustomerAuth = null;
        return configuration;
    }

    internal IPaymentService Payments =>
        Register(new PaymentService(_configuration, CreateClient()));

    internal IBatchPaymentService BatchPayments =>
        Register(new BatchPaymentService(_configuration, CreateClient()));

    internal IPayoutService Payouts =>
        Register(new PayoutService(_configuration, CreateClient()));

    internal IPayPartsService PayParts =>
        Register(new PayPartsService(_configuration, CreateClient()));

    internal IAlternativePaymentService AlternativePayments =>
        Register(new AlternativePaymentService(_configuration, CreateClient()));

    internal ICustomerService Customers =>
        Register(new CustomerService(_configuration, CreateClient()));

    internal ISubscriptionService Subscriptions =>
        Register(new SubscriptionService(_configuration, CreateClient()));

    internal IReportService Reports =>
        Register(new ReportService(_configuration, CreateClient()));

    internal IInStorePaymentService InStorePayments =>
        Register(new InStorePaymentService(_configuration, CreateClient()));

    internal IPartnerService Partners =>
        Register(new PartnerService(_configuration, CreateClient()));

    internal IMerchantService Merchants =>
        Register(new MerchantService(_configuration, CreateClient()));

    internal IFinMonService FinMon =>
        Register(new FinMonService(_configuration, CreateClient()));

    /// <summary>
    /// Payment instructions, wired the way the DI registration wires them: the create operation over the
    /// authenticated client, the decline operation over a second client that carries the user agent and
    /// nothing else. The service validates the absence of credential headers on the decline client.
    /// </summary>
    internal IPaymentInstructionService PaymentInstructions
    {
        get
        {
            PaymentInstructionService service = new(
                _configuration,
                CreateClient(),
                CreateDeclineClient());
            _disposables.Add(service);
            return service;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (IDisposable disposable in _disposables)
        {
            disposable.Dispose();
        }

        foreach (HttpClient client in _clients)
        {
            client.Dispose();
        }
    }

    private TService Register<TService>(TService service)
    {
        if (service is IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        return service;
    }

    /// <summary>
    /// A client over the shared transport. <c>disposeHandler: false</c> because several clients share one
    /// handler and the handler's lifetime belongs to the test that created it.
    /// </summary>
    private HttpClient CreateClient()
    {
        HttpClient client = new(_handler, disposeHandler: false) { BaseAddress = new Uri(BaseUrl) };
        _clients.Add(client);
        return client;
    }

    /// <summary>
    /// A client for the anonymous decline operation, configured the way the DI registration configures
    /// its named non-redirecting client: base address, timeout and user agent, and no credential header of
    /// any kind. The user agent is set here so that every one of the 67 rows - the anonymous one
    /// included - can assert the same non-credential header.
    /// </summary>
    private HttpClient CreateDeclineClient()
    {
        HttpClient client = CreateClient();
        client.Timeout = _configuration.Timeout;
        if (!string.IsNullOrWhiteSpace(_configuration.UserAgent))
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(_configuration.UserAgent);
        }

        return client;
    }
}
