using SYT.RozetkaPay.Services;

namespace SYT.RozetkaPay;

/// <summary>
/// Aggregate contract for the RozetkaPay SDK that exposes every service through abstractions.
/// Implemented by <see cref="RozetkaPayClient"/> and intended as the injection/mocking seam for
/// consumer code that needs the whole SDK surface behind a single dependency.
/// </summary>
/// <remarks>
/// The contract derives from <see cref="IDisposable"/> because the concrete client may own an
/// internally created <see cref="HttpClient"/>. When an instance is constructed directly rather
/// than resolved from a container, dispose it (for example with a <c>using</c> statement) so that
/// an owned <see cref="HttpClient"/> is released. When the SDK is registered through
/// <c>AddRozetkaPay</c>, the container owns the lifetime and the HTTP client comes from
/// <c>IHttpClientFactory</c>.
/// </remarks>
public interface IRozetkaPayClient : IDisposable
{
    /// <summary>
    /// Payment service for all payment operations
    /// </summary>
    IPaymentService Payments { get; }

    /// <summary>
    /// Batch payment service for batch payment operations
    /// </summary>
    IBatchPaymentService BatchPayments { get; }

    /// <summary>
    /// PayParts service for installment payment operations
    /// </summary>
    IPayPartsService PayParts { get; }

    /// <summary>
    /// Payout service for payout operations
    /// </summary>
    IPayoutService Payouts { get; }

    /// <summary>
    /// Customer service for wallet and customer operations
    /// </summary>
    ICustomerService Customers { get; }

    /// <summary>
    /// Subscription service for subscription management
    /// </summary>
    ISubscriptionService Subscriptions { get; }

    /// <summary>
    /// Report service for generating reports
    /// </summary>
    IReportService Reports { get; }

    /// <summary>
    /// Alternative payment service for alternative payment methods
    /// </summary>
    IAlternativePaymentService AlternativePayments { get; }

    /// <summary>
    /// Merchant service for merchant operations
    /// </summary>
    IMerchantService Merchants { get; }

    /// <summary>
    /// FinMon service for financial monitoring
    /// </summary>
    IFinMonService FinMon { get; }

    /// <summary>
    /// In-store (POS) payment service for terminal payment operations
    /// </summary>
    IInStorePaymentService InStorePayments { get; }

    /// <summary>
    /// Partner service for partner fee, status and transaction reporting
    /// </summary>
    IPartnerService Partners { get; }

    /// <summary>
    /// Payment instruction service for instruction batches and the unauthenticated decline operation
    /// </summary>
    IPaymentInstructionService PaymentInstructions { get; }
}
