using SYT.RozetkaPay.Models.AlternativePayments;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Contract for alternative payment method operations. Implemented by
/// <see cref="AlternativePaymentService"/> and intended as the injection/mocking seam for
/// consumer code.
/// </summary>
public interface IAlternativePaymentService
{
    /// <summary>
    /// Create alternative payment
    /// </summary>
    /// <param name="request">Alternative payment request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Alternative payment response</returns>
    Task<AlternativePaymentResponse> CreateAsync(CreateAlternativePayment request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create alternative payment using OpenAPI contract response schema
    /// </summary>
    /// <param name="request">Alternative payment request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Alternative payment operation result</returns>
    Task<AlternativePaymentOperationResult> CreateOperationAsync(CreateAlternativePayment request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refund alternative payment
    /// </summary>
    /// <param name="request">Alternative payment refund request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Alternative payment refund response</returns>
    Task<AlternativePaymentResponse> RefundAsync(RefundAlternativePaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resend alternative payment callback
    /// </summary>
    /// <param name="request">Resend callback request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Callback resend response</returns>
    Task<AlternativePaymentCallbackResendResponse> ResendCallbackAsync(ResendAlternativePaymentCallbackRequest request, CancellationToken cancellationToken = default);


    /// <summary>
    /// Get operation info by external ID and operation ID
    /// </summary>
    /// <param name="externalId">External ID</param>
    /// <param name="operationId">Operation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Alternative payment operation result</returns>
    Task<AlternativePaymentOperationResult> GetOperationInfoAsync(string externalId, string operationId, CancellationToken cancellationToken = default);


    /// <summary>
    /// Get operations info by external ID
    /// </summary>
    /// <param name="externalId">External ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Alternative payment operations result</returns>
    Task<AlternativePaymentOperationsResult> GetInfoAsync(string externalId, CancellationToken cancellationToken = default);


}
