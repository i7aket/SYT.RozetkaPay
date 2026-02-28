using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Models.Batch;
using Microsoft.Extensions.Logging;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Service for batch payment operations
/// </summary>
public class BatchPaymentService : BaseService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BatchPaymentService"/> class.
    /// </summary>
    /// <param name="configuration">SDK configuration.</param>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="logger">Optional logger.</param>
    public BatchPaymentService(RozetkaPayConfiguration configuration, HttpClient httpClient, ILogger? logger = null)
        : base(configuration, httpClient, logger)
    {
    }

    /// <summary>
    /// Create batch acquiring payment
    /// POST /api/payments/batch/v1/new
    /// </summary>
    /// <param name="request">Batch payment creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batch payment response</returns>
    public async Task<BatchPaymentResponse> CreateBatchPaymentAsync(CreateBatchPaymentRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<CreateBatchPaymentRequest, BatchPaymentResponse>("/api/payments/batch/v1/new", request, cancellationToken);
    }

    /// <summary>
    /// Confirm batch acquiring payment
    /// POST /api/payments/batch/v1/confirm
    /// </summary>
    /// <param name="request">Batch payment confirmation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batch payment response</returns>
    public async Task<BatchPaymentResponse> ConfirmBatchPaymentAsync(ConfirmBatchPaymentRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<ConfirmBatchPaymentRequest, BatchPaymentResponse>("/api/payments/batch/v1/confirm", request, cancellationToken);
    }

    /// <summary>
    /// Cancel batch acquiring payment
    /// POST /api/payments/batch/v1/cancel
    /// </summary>
    /// <param name="request">Batch payment cancellation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batch payment response</returns>
    public async Task<BatchPaymentResponse> CancelBatchPaymentAsync(CancelBatchPaymentRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<CancelBatchPaymentRequest, BatchPaymentResponse>("/api/payments/batch/v1/cancel", request, cancellationToken);
    }
} 
