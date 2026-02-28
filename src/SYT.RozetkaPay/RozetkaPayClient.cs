using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Services;
using Microsoft.Extensions.Logging;

namespace SYT.RozetkaPay;

/// <summary>
/// Main client for RozetkaPay API that provides access to all services
/// </summary>
public class RozetkaPayClient : IDisposable
{
    private readonly HttpClient HttpClient;
    private readonly bool _ownsHttpClient;
    private bool _disposed;

    /// <summary>
    /// Payment service for all payment operations
    /// </summary>
    public PaymentService Payments { get; }

    /// <summary>
    /// Batch payment service for batch payment operations
    /// </summary>
    public BatchPaymentService BatchPayments { get; }

    /// <summary>
    /// PayParts service for installment payment operations
    /// </summary>
    public PayPartsService PayParts { get; }

    /// <summary>
    /// Payout service for payout operations
    /// </summary>
    public PayoutService Payouts { get; }

    /// <summary>
    /// Customer service for wallet and customer operations
    /// </summary>
    public CustomerService Customers { get; }

    /// <summary>
    /// Subscription service for subscription management
    /// </summary>
    public SubscriptionService Subscriptions { get; }

    /// <summary>
    /// Report service for generating reports
    /// </summary>
    public ReportService Reports { get; }

    /// <summary>
    /// Alternative payment service for alternative payment methods
    /// </summary>
    public AlternativePaymentService AlternativePayments { get; }

    /// <summary>
    /// Merchant service for merchant operations
    /// </summary>
    public MerchantService Merchants { get; }

    /// <summary>
    /// FinMon service for financial monitoring
    /// </summary>
    public FinMonService FinMon { get; }

    /// <summary>
    /// Initialize RozetkaPayClient with configuration
    /// </summary>
    /// <param name="configuration">RozetkaPay configuration</param>
    /// <param name="httpClient">Optional HTTP client</param>
    /// <param name="logger">Optional logger</param>
    public RozetkaPayClient(RozetkaPayConfiguration configuration, HttpClient? httpClient = null, ILogger<RozetkaPayClient>? logger = null)
    {
        RozetkaPayConfiguration configuration1 = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _ownsHttpClient = httpClient is null;
        HttpClient = httpClient ?? new HttpClient();

        // Initialize all services
        Payments = new PaymentService(configuration1, HttpClient, logger);
        BatchPayments = new BatchPaymentService(configuration1, HttpClient, logger);
        PayParts = new PayPartsService(configuration1, HttpClient, logger);
        Payouts = new PayoutService(configuration1, HttpClient, logger);
        Customers = new CustomerService(configuration1, HttpClient, logger);
        Subscriptions = new SubscriptionService(configuration1, HttpClient, logger);
        Reports = new ReportService(configuration1, HttpClient, logger);
        AlternativePayments = new AlternativePaymentService(configuration1, HttpClient, logger);
        Merchants = new MerchantService(configuration1, HttpClient, logger);
        FinMon = new FinMonService(configuration1, HttpClient, logger);
    }

    /// <summary>
    /// Create RozetkaPayClient with basic configuration
    /// </summary>
    /// <param name="baseUrl">Base URL for RozetkaPay API</param>
    /// <param name="login">API login</param>
    /// <param name="password">API password</param>
    /// <param name="httpClient">Optional HTTP client</param>
    /// <param name="logger">Optional logger</param>
    public static RozetkaPayClient Create(string baseUrl, string login, string password, HttpClient? httpClient = null, ILogger<RozetkaPayClient>? logger = null)
    {
        RozetkaPayConfiguration configuration = new RozetkaPayConfiguration
        {
            BaseUrl = baseUrl,
            Login = login,
            Password = password
        };

        return new RozetkaPayClient(configuration, httpClient, logger);
    }

    /// <summary>
    /// Dispose the HTTP client if it was created internally
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_ownsHttpClient)
        {
            HttpClient.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
} 
