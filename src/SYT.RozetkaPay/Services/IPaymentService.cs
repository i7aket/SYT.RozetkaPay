using SYT.RozetkaPay.Models.Payments;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Contract for payment operations. Implemented by <see cref="PaymentService"/> and
/// intended as the injection/mocking seam for consumer code.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Create a new payment
    /// </summary>
    /// <param name="request">Payment creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment response</returns>
    Task<PaymentOperationResult> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a recurrent payment using existing recurrent ID
    /// </summary>
    /// <param name="request">Recurrent payment request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment response</returns>
    Task<PaymentResponse> CreateRecurrentAsync(CreateRecurrentPaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirm a payment (for two-step payments)
    /// </summary>
    /// <param name="request">Payment confirmation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment response</returns>
    Task<PaymentOperationResult> ConfirmAsync(ConfirmPaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a payment
    /// </summary>
    /// <param name="request">Payment cancellation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment response</returns>
    Task<PaymentOperationResult> CancelAsync(CancelPaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refund a payment
    /// </summary>
    /// <param name="request">Payment refund request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment response</returns>
    Task<PaymentOperationResult> RefundAsync(RefundPaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retry pending refund operation
    /// </summary>
    /// <param name="request">Retry refund request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment operation result</returns>
    Task<PaymentOperationResult> RetryRefundAsync(RetryRefundRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel pending refund operation
    /// </summary>
    /// <param name="request">Cancel refund request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment operation result</returns>
    Task<PaymentOperationResult> CancelRefundAsync(CancelRefundRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get payment information
    /// </summary>
    /// <param name="externalId">External payment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment response</returns>
    Task<PaymentStatusResult> GetInfoAsync(string externalId, CancellationToken cancellationToken = default);


    /// <summary>
    /// Get payment receipt
    /// </summary>
    /// <param name="externalId">External payment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment receipt response</returns>
    Task<PaymentReceiptResponse> GetReceiptAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Perform card lookup
    /// </summary>
    /// <param name="request">Card lookup request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Card lookup response</returns>
    Task<CardLookupResponse> CardLookupAsync(CardLookupRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resend payment callback
    /// </summary>
    /// <param name="request">Resend callback request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Callback resend response</returns>
    Task<CallbackResendResponse> ResendCallbackAsync(ResendCallbackRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create P2P payment (card-to-card transfer)
    /// </summary>
    /// <param name="request">P2P payment request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment response</returns>
    Task<PaymentOperationResult> CreateP2PAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);

}
