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
    public async Task<PaymentOperationResult> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        // The one mutation the provider makes an at-most-once promise about: "At most one success
        // payment is allowed with same external_id within single login." A repeat after a timeout or a
        // 5xx therefore cannot produce a second successful payment, so a retry is safe here and only here.
        return await PostAsync<CreatePaymentRequest, PaymentOperationResult>(
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
    public async Task<PaymentOperationResult> ConfirmAsync(ConfirmPaymentRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<ConfirmPaymentRequest, PaymentOperationResult>(ConfirmEndpoint, ConfirmEndpoint, request, cancellationToken);
    }

    /// <summary>
    /// Cancel a payment
    /// POST /api/payments/v1/cancel
    /// </summary>
    /// <param name="request">Payment cancellation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment response</returns>
    public async Task<PaymentOperationResult> CancelAsync(CancelPaymentRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<CancelPaymentRequest, PaymentOperationResult>(CancelEndpoint, CancelEndpoint, request, cancellationToken);
    }

    /// <summary>
    /// Refund a payment
    /// POST /api/payments/v1/refund
    /// </summary>
    /// <param name="request">Payment refund request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment response</returns>
    public async Task<PaymentOperationResult> RefundAsync(RefundPaymentRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<RefundPaymentRequest, PaymentOperationResult>(RefundEndpoint, RefundEndpoint, request, cancellationToken);
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
    public async Task<PaymentStatusResult> GetInfoAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<PaymentStatusResult>(
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
    public async Task<PaymentOperationResult> CardLookupAsync(CreateLookupRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<CreateLookupRequest, PaymentOperationResult>(LookupEndpoint, LookupEndpoint, request, cancellationToken);
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
    public async Task<PaymentOperationResult> CreateP2PAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Recipient == null)
            throw new ArgumentException("Recipient information is required for P2P payments", nameof(request));

        return await CreateAsync(request, cancellationToken);
    }


    /// <summary>
    /// Builds the body of a card-to-card transfer for <c>POST /api/payments/v1/new</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The recipient half of this request is spelled exactly as the document declares it:
    /// <c>type</c> is <c>cc_number</c>, one of the four values the schema's inline enum permits
    /// (<c>iban</c>, <c>cc_token</c>, <c>wallet</c>, <c>cc_number</c>), and the card sits under
    /// <c>cc_number</c> carrying <c>number</c> alone.
    /// </para>
    /// <para>
    /// Expiry is not a parameter because <c>RecipientCCNumberRequestPaymentMethod</c> declares only
    /// <c>number</c>. The earlier signature took a month and a year, wrote them to properties no
    /// schema declares, and sent <c>type: "card_number"</c>, which is not among the four — so every
    /// request it built was invalid on two counts.
    /// </para>
    /// <para>
    /// <paramref name="customerEmail"/> is required rather than defaulted. The document makes
    /// <c>customer</c> required when <c>mode</c> is <c>direct</c>, and the earlier version satisfied
    /// that by hardcoding <c>customer@example.com</c> — sending a fabricated address to the provider
    /// on a real transfer, attached to a real payment, with no sign to the caller that it had
    /// happened.
    /// </para>
    /// </remarks>
    /// <param name="amount">Payment amount.</param>
    /// <param name="currency">Payment currency.</param>
    /// <param name="externalId">Caller's own identifier for the payment.</param>
    /// <param name="customerEmail">The paying customer's address. Required in <c>direct</c> mode.</param>
    /// <param name="recipientCardNumber">Recipient card number.</param>
    /// <param name="description">Payment description.</param>
    /// <returns>A request body ready for <see cref="CreateAsync"/>.</returns>
    public static CreatePaymentRequest BuildP2PRequest(
        decimal amount,
        string currency,
        string externalId,
        string customerEmail,
        string recipientCardNumber,
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
                Email = customerEmail
            },
            Recipient = new RecipientRequestUserDetails
            {
                PaymentMethod = new RecipientRequestPaymentMethod
                {
                    Type = "cc_number",
                    CcNumber = new RecipientCCNumberRequestPaymentMethod
                    {
                        Number = recipientCardNumber
                    }
                }
            }
        };
    }
}
