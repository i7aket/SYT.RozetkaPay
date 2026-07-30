using SYT.RozetkaPay.Models.Batch;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Contract for batch payment operations. Implemented by <see cref="BatchPaymentService"/> and
/// intended as the injection/mocking seam for consumer code.
/// </summary>
public interface IBatchPaymentService
{
    /// <summary>
    /// Create batch acquiring payment
    /// </summary>
    /// <param name="request">Batch payment creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batch payment response</returns>
    Task<BatchPaymentOperationResult> CreateBatchPaymentAsync(CreateBatchPaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirm batch acquiring payment
    /// </summary>
    /// <param name="request">Batch payment confirmation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batch payment response</returns>
    Task<BatchPaymentOperationResult> ConfirmBatchPaymentAsync(ConfirmBatchPaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel batch acquiring payment
    /// </summary>
    /// <param name="request">Batch payment cancellation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batch payment response</returns>
    Task<BatchPaymentOperationResult> CancelBatchPaymentAsync(CancelBatchPaymentRequest request, CancellationToken cancellationToken = default);
}
