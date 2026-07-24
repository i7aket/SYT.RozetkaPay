using SYT.RozetkaPay.Models.PayParts;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Contract for PayParts (installment payment) operations. Implemented by
/// <see cref="PayPartsService"/> and intended as the injection/mocking seam for consumer code.
/// </summary>
public interface IPayPartsService
{
    /// <summary>
    /// Create PayParts order
    /// </summary>
    /// <param name="request">PayParts order creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts order response</returns>
    Task<PayPartsOrderResponse> CreateOrderAsync(CreatePayPartsOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirm PayParts order
    /// </summary>
    /// <param name="request">PayParts confirm request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts order response</returns>
    Task<PayPartsOrderResponse> ConfirmOrderAsync(ConfirmPayPartsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel PayParts order
    /// </summary>
    /// <param name="request">PayParts cancel request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts order response</returns>
    Task<PayPartsOrderResponse> CancelOrderAsync(CancelPayPartsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refund PayParts order
    /// </summary>
    /// <param name="request">PayParts refund request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts refund response</returns>
    Task<PayPartsRefundResponse> RefundOrderAsync(RefundPayPartsOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retry pending PayParts refund operation
    /// </summary>
    /// <param name="request">Retry refund request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts operation result</returns>
    Task<PayPartsOperationResult> RetryRefundAsync(RetryRefundPPayRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel pending PayParts refund operation
    /// </summary>
    /// <param name="request">Cancel refund request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts operation result</returns>
    Task<PayPartsOperationResult> CancelRefundAsync(CancelRefundPPayRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get operation info
    /// </summary>
    /// <param name="operationId">Operation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts operation info</returns>
    Task<PayPartsOperationResponse> GetOperationInfoAsync(string operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get operation info by external ID and operation ID
    /// </summary>
    /// <param name="externalId">External ID</param>
    /// <param name="operationId">Operation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts operation info</returns>
    Task<PayPartsOperationResult> GetOperationInfoAsync(string externalId, string operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get operations info by external ID
    /// </summary>
    /// <param name="externalId">External ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts operations result</returns>
    Task<PayPartsOperationsResult> GetInfoAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get operations info
    /// </summary>
    /// <param name="request">Operations list request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts operations list</returns>
    Task<PayPartsOperationsListResponse> GetOperationsAsync(PayPartsOperationsListRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get banks info for PayParts
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts banks information</returns>
    Task<PayPartsBanksResponse> GetBanksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resend PayParts callback
    /// </summary>
    /// <param name="request">Resend callback request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Callback resend response</returns>
    Task<PayPartsResendCallbackResponse> ResendCallbackAsync(PayPartsResendCallbackRequest request, CancellationToken cancellationToken = default);
}
