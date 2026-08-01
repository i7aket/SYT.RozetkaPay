using SYT.RozetkaPay.Models.Payments;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Contract for payment operations. Implemented by <see cref="PaymentService"/> and
/// intended as the injection/mocking seam for consumer code.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// A view of this service that acts for a child merchant in partnership mode (EXP-459).
    /// </summary>
    /// <remarks>
    /// <para>
    /// RozetkaPay routes a payment to a child merchant with the <c>X-ON-BEHALF-OF</c> header — "partnership
    /// mode, when one core account operates with several children". Authentication stays the platform's; the
    /// child is named by identifier, so a platform never handles a merchant's own credentials.
    /// </para>
    /// <para>
    /// The header is configurable per client, which is enough for a single merchant and not enough for a
    /// platform: every payment goes to a different child. This returns a service bound to one child, sharing
    /// the caller's HTTP client, so a platform needs neither a client per expert nor a mutable field that a
    /// concurrent call could read mid-change.
    /// </para>
    /// <para>
    /// The returned service is independent: the original keeps whatever scope it had. Calling this on an
    /// already-scoped service re-scopes the copy rather than nesting.
    /// </para>
    /// <para>
    /// An unusable identifier fails here rather than at send time: a payment that cannot name its merchant
    /// must not reach the provider, because the provider would accept it for the platform's own account and
    /// the money would go to the wrong party.
    /// </para>
    /// </remarks>
    /// <param name="onBehalfOf">Identifier of the child merchant. Must not be blank.</param>
    /// <returns>A service that sends every request on behalf of <paramref name="onBehalfOf"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="onBehalfOf"/> is blank.</exception>
    /// <exception cref="FormatException">
    /// <paramref name="onBehalfOf"/> is not a valid HTTP header value. This is the same error a badly formed
    /// configured value raises, so one catch covers both routes to the header.
    /// </exception>
    IPaymentService ActingFor(string onBehalfOf);

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
    Task<PaymentOperationResult> CreateRecurrentAsync(CreateRecurrentPaymentRequest request, CancellationToken cancellationToken = default);

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
    Task<PaymentReceiptResult> GetReceiptAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Perform card lookup
    /// </summary>
    /// <param name="request">Card lookup request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Card lookup response</returns>
    Task<PaymentOperationResult> CardLookupAsync(CreateLookupRequest request, CancellationToken cancellationToken = default);

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
