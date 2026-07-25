using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Services;
using Microsoft.Extensions.Logging;

namespace SYT.RozetkaPay;

/// <summary>
/// Main client for RozetkaPay API that provides access to all services
/// </summary>
public class RozetkaPayClient : IRozetkaPayClient
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
    /// In-store (POS) payment service for terminal payment operations
    /// </summary>
    public InStorePaymentService InStorePayments { get; }

    /// <summary>
    /// Partner service for partner fee, status and transaction reporting
    /// </summary>
    public PartnerService Partners { get; }

    /// <summary>
    /// Payment instruction service for instruction batches and the unauthenticated decline operation
    /// </summary>
    public PaymentInstructionService PaymentInstructions { get; }

    // Explicit IRozetkaPayClient members expose the same service instances as the concrete
    // properties above. The concrete property types are kept as-is for source compatibility.

    /// <inheritdoc />
    IPaymentService IRozetkaPayClient.Payments => Payments;

    /// <inheritdoc />
    IBatchPaymentService IRozetkaPayClient.BatchPayments => BatchPayments;

    /// <inheritdoc />
    IPayPartsService IRozetkaPayClient.PayParts => PayParts;

    /// <inheritdoc />
    IPayoutService IRozetkaPayClient.Payouts => Payouts;

    /// <inheritdoc />
    ICustomerService IRozetkaPayClient.Customers => Customers;

    /// <inheritdoc />
    ISubscriptionService IRozetkaPayClient.Subscriptions => Subscriptions;

    /// <inheritdoc />
    IReportService IRozetkaPayClient.Reports => Reports;

    /// <inheritdoc />
    IAlternativePaymentService IRozetkaPayClient.AlternativePayments => AlternativePayments;

    /// <inheritdoc />
    IMerchantService IRozetkaPayClient.Merchants => Merchants;

    /// <inheritdoc />
    IFinMonService IRozetkaPayClient.FinMon => FinMon;

    /// <inheritdoc />
    IInStorePaymentService IRozetkaPayClient.InStorePayments => InStorePayments;

    /// <inheritdoc />
    IPartnerService IRozetkaPayClient.Partners => Partners;

    /// <inheritdoc />
    IPaymentInstructionService IRozetkaPayClient.PaymentInstructions => PaymentInstructions;

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
        InStorePayments = new InStorePaymentService(configuration1, HttpClient, logger);
        Partners = new PartnerService(configuration1, HttpClient, logger);

        // The payment-instruction service creates its own unauthenticated, non-redirecting client for
        // the decline operation. That client is owned by the service and released when this client is
        // disposed; the authenticated HttpClient above is untouched by it.
        PaymentInstructions = new PaymentInstructionService(configuration1, HttpClient, logger);
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
    /// <remarks>
    /// Only resources this client owns are released. An externally supplied
    /// <see cref="System.Net.Http.HttpClient"/> stays usable, while the decline client that the
    /// payment-instruction service created internally is always released.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Always owned: the payment-instruction service created this client itself.
        ((IDisposable)PaymentInstructions).Dispose();

        if (_ownsHttpClient)
        {
            HttpClient.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
