using System.Globalization;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Payments;
using Microsoft.Extensions.Logging;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Service for payment operations
/// </summary>
public class PaymentService : BaseService, IPaymentService
{
    private const string NewEndpoint = "/api/payments/v1/new";

    private const string RecurrentEndpoint = "/api/payments/v1/recurrent";

    private const string ConfirmEndpoint = "/api/payments/v1/confirm";

    private const string CancelEndpoint = "/api/payments/v1/cancel";

    private const string RefundEndpoint = "/api/payments/v1/refund";

    private const string RetryRefundEndpoint = "/api/payments/v1/refund/retry";

    private const string CancelRefundEndpoint = "/api/payments/v1/refund/cancel";

    private const string LookupEndpoint = "/api/payments/v1/lookup";

    private const string ResendCallbackEndpoint = "/api/payments/v1/callback/resend";


    /// <summary>
    /// Route of the payment-info operation. Also the log label: the real request target carries the
    /// caller's external ID in the query, which must not be logged.
    /// </summary>
    private const string InfoEndpoint = "/api/payments/v1/info";


    /// <summary>
    /// Route of the receipt operation, and its log label. The real target carries the caller's external ID.
    /// </summary>
    private const string ReceiptEndpoint = "/api/payments/v1/receipt";

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentService"/> class.
    /// </summary>
    /// <param name="configuration">SDK configuration.</param>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="logger">Optional logger.</param>
    public PaymentService(RozetkaPayConfiguration configuration, HttpClient httpClient, ILogger? logger = null)
        : base(configuration, httpClient, logger)
    {
    }

    /// <summary>
    /// Create a new payment
    /// POST /api/payments/v1/new
    /// </summary>
    /// <param name="request">Payment creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment response</returns>
    public async Task<PaymentResponse> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        // The one mutation the provider makes an at-most-once promise about: "At most one success
        // payment is allowed with same external_id within single login." A repeat after a timeout or a
        // 5xx therefore cannot produce a second successful payment, so a retry is safe here and only here.
        return await PostAsync<CreatePaymentRequest, PaymentResponse>(
            NewEndpoint,
            NewEndpoint,
            request,
            cancellationToken,
            isIdempotent: true);
    }

    /// <summary>
    /// Create a recurrent payment using existing recurrent ID
    /// POST /api/payments/v1/recurrent
    /// </summary>
    /// <param name="request">Recurrent payment request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment response</returns>
    public async Task<PaymentResponse> CreateRecurrentAsync(CreateRecurrentPaymentRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<CreateRecurrentPaymentRequest, PaymentResponse>(RecurrentEndpoint, RecurrentEndpoint, request, cancellationToken);
    }

    /// <summary>
    /// Confirm a payment (for two-step payments)
    /// POST /api/payments/v1/confirm
    /// </summary>
    /// <param name="request">Payment confirmation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment response</returns>
    public async Task<PaymentResponse> ConfirmAsync(ConfirmPaymentRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<ConfirmPaymentRequest, PaymentResponse>(ConfirmEndpoint, ConfirmEndpoint, request, cancellationToken);
    }

    /// <summary>
    /// Cancel a payment
    /// POST /api/payments/v1/cancel
    /// </summary>
    /// <param name="request">Payment cancellation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment response</returns>
    public async Task<PaymentResponse> CancelAsync(CancelPaymentRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<CancelPaymentRequest, PaymentResponse>(CancelEndpoint, CancelEndpoint, request, cancellationToken);
    }

    /// <summary>
    /// Refund a payment
    /// POST /api/payments/v1/refund
    /// </summary>
    /// <param name="request">Payment refund request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment response</returns>
    public async Task<PaymentResponse> RefundAsync(RefundPaymentRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<RefundPaymentRequest, PaymentResponse>(RefundEndpoint, RefundEndpoint, request, cancellationToken);
    }

    /// <summary>
    /// Retry pending refund operation
    /// POST /api/payments/v1/refund/retry
    /// </summary>
    /// <param name="request">Retry refund request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment operation result</returns>
    public async Task<PaymentOperationResult> RetryRefundAsync(RetryRefundRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<RetryRefundRequest, PaymentOperationResult>(RetryRefundEndpoint, RetryRefundEndpoint, request, cancellationToken);
    }

    /// <summary>
    /// Cancel pending refund operation
    /// POST /api/payments/v1/refund/cancel
    /// </summary>
    /// <param name="request">Cancel refund request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment operation result</returns>
    public async Task<PaymentOperationResult> CancelRefundAsync(CancelRefundRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<CancelRefundRequest, PaymentOperationResult>(CancelRefundEndpoint, CancelRefundEndpoint, request, cancellationToken);
    }

    /// <summary>
    /// Get payment information
    /// GET /api/payments/v1/info
    /// </summary>
    /// <param name="externalId">External payment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment response</returns>
    public async Task<PaymentResponse> GetInfoAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<PaymentResponse>(
            $"{InfoEndpoint}?external_id={Uri.EscapeDataString(externalId)}",
            InfoEndpoint,
            cancellationToken);
    }


    /// <summary>
    /// Get payment receipt
    /// GET /api/payments/v1/receipt
    /// </summary>
    /// <param name="externalId">External payment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment receipt response</returns>
    public async Task<PaymentReceiptResponse> GetReceiptAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<PaymentReceiptResponse>(
            $"{ReceiptEndpoint}?external_id={Uri.EscapeDataString(externalId)}",
            ReceiptEndpoint,
            cancellationToken);
    }

    /// <summary>
    /// Perform card lookup
    /// POST /api/payments/v1/lookup
    /// </summary>
    /// <param name="request">Card lookup request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Card lookup response</returns>
    public async Task<CardLookupResponse> CardLookupAsync(CardLookupRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<CardLookupRequest, CardLookupResponse>(LookupEndpoint, LookupEndpoint, request, cancellationToken);
    }

    /// <summary>
    /// Resend payment callback
    /// POST /api/payments/v1/callback/resend
    /// </summary>
    /// <param name="request">Resend callback request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Callback resend response</returns>
    public async Task<CallbackResendResponse> ResendCallbackAsync(ResendCallbackRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsyncWithNoContent<ResendCallbackRequest, CallbackResendResponse>(
            ResendCallbackEndpoint,
            ResendCallbackEndpoint,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Create P2P payment (card-to-card transfer)
    /// </summary>
    /// <param name="request">P2P payment request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment response</returns>
    public async Task<PaymentResponse> CreateP2PAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Recipient == null)
            throw new ArgumentException("Recipient information is required for P2P payments", nameof(request));

        return await CreateAsync(request, cancellationToken);
    }


    /// <summary>
    /// Build P2P payment request helper
    /// </summary>
    /// <param name="amount">Payment amount in UAH</param>
    /// <param name="currency">Payment currency</param>
    /// <param name="externalId">External payment ID</param>
    /// <param name="recipientCardNumber">Recipient card number</param>
    /// <param name="recipientExpMonth">Recipient card expiry month</param>
    /// <param name="recipientExpYear">Recipient card expiry year</param>
    /// <param name="description">Payment description</param>
    /// <returns>P2P payment request</returns>
    public static CreatePaymentRequest BuildP2PRequest(
        decimal amount,
        string currency,
        string externalId,
        string recipientCardNumber,
        string recipientExpMonth,
        string recipientExpYear,
        string? description = null)
    {
        return new CreatePaymentRequest
        {
            Amount = amount,
            Currency = currency,
            ExternalId = externalId,
            Mode = PaymentMode.Direct,
            Description = description ?? "P2P Transfer",
            Customer = new CustomerInfo
            {
                Email = "customer@example.com" // Default customer for P2P
            },
            Recipient = new RecipientRequestUserDetails
            {
                PaymentMethod = new RecipientRequestPaymentMethod
                {
                    Type = "card_number",
                    CardNumber = new RecipientCCNumberRequestPaymentMethod
                    {
                        Number = recipientCardNumber,
                        ExpirationMonth = int.Parse(recipientExpMonth),
                        ExpirationYear = int.Parse(recipientExpYear)
                    }
                }
            }
        };
    }
}
