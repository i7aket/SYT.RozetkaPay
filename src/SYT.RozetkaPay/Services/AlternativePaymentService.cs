using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Models.AlternativePayments;
using Microsoft.Extensions.Logging;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Service for alternative payment methods
/// </summary>
public class AlternativePaymentService : BaseService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AlternativePaymentService"/> class.
    /// </summary>
    /// <param name="configuration">SDK configuration.</param>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="logger">Optional logger.</param>
    public AlternativePaymentService(RozetkaPayConfiguration configuration, HttpClient httpClient, ILogger? logger = null)
        : base(configuration, httpClient, logger)
    {
    }

    /// <summary>
    /// Create alternative payment
    /// POST /api/alternative-payments/v1/new
    /// </summary>
    /// <param name="request">Alternative payment request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Alternative payment response</returns>
    public async Task<AlternativePaymentResponse> CreateAsync(CreateAlternativePaymentRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsyncWithFallback<CreateAlternativePaymentRequest, AlternativePaymentResponse>(
            "/api/alternative-payments/v1/create",
            "/api/alternative-payments/v1/new",
            request,
            cancellationToken);
    }

    /// <summary>
    /// Create alternative payment using OpenAPI contract response schema
    /// POST /api/alternative-payments/v1/create
    /// </summary>
    /// <param name="request">Alternative payment request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Alternative payment operation result</returns>
    public async Task<AlternativePaymentOperationResult> CreateOperationAsync(CreateAlternativePaymentRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsyncWithFallback<CreateAlternativePaymentRequest, AlternativePaymentOperationResult>(
            "/api/alternative-payments/v1/create",
            "/api/alternative-payments/v1/new",
            request,
            cancellationToken);
    }

    /// <summary>
    /// Refund alternative payment
    /// POST /api/alternative-payments/v1/refund
    /// </summary>
    /// <param name="request">Alternative payment refund request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Alternative payment refund response</returns>
    public async Task<AlternativePaymentResponse> RefundAsync(RefundAlternativePaymentRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<RefundAlternativePaymentRequest, AlternativePaymentResponse>("/api/alternative-payments/v1/refund", request, cancellationToken);
    }

    /// <summary>
    /// Resend alternative payment callback
    /// POST /api/alternative-payments/v1/callback/resend
    /// </summary>
    /// <param name="request">Resend callback request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Callback resend response</returns>
    public async Task<AlternativePaymentCallbackResendResponse> ResendCallbackAsync(ResendAlternativePaymentCallbackRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsyncWithNoContent<ResendAlternativePaymentCallbackRequest, AlternativePaymentCallbackResendResponse>(
            "/api/alternative-payments/v1/callback/resend",
            request,
            cancellationToken);
    }

    /// <summary>
    /// Get operation info
    /// GET /api/alternative-payments/v1/operation/{externalId}
    /// </summary>
    /// <param name="externalId">External ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation info response</returns>
    public async Task<AlternativePaymentOperationResponse> GetOperationInfoAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<AlternativePaymentOperationResponse>($"/api/alternative-payments/v1/operation/{externalId}", cancellationToken);
    }

    /// <summary>
    /// Get operation info by external ID and operation ID
    /// GET /api/alternative-payments/v1/info/operation
    /// </summary>
    /// <param name="externalId">External ID</param>
    /// <param name="operationId">Operation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Alternative payment operation result</returns>
    public async Task<AlternativePaymentOperationResult> GetOperationInfoAsync(
        string externalId,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        string primaryEndpoint =
            $"/api/alternative-payments/v1/info/operation?external_id={Uri.EscapeDataString(externalId)}&operation_id={Uri.EscapeDataString(operationId)}";
        string fallbackEndpoint = $"/api/alternative-payments/v1/operation/{Uri.EscapeDataString(externalId)}";
        return await GetAsyncWithFallback<AlternativePaymentOperationResult>(primaryEndpoint, fallbackEndpoint, cancellationToken);
    }

    /// <summary>
    /// Get operations info
    /// GET /api/alternative-payments/v1/operations
    /// </summary>
    /// <param name="request">Operations list request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operations list response</returns>
    public async Task<AlternativePaymentOperationsResponse> GetOperationsAsync(GetAlternativePaymentOperationsRequest request, CancellationToken cancellationToken = default)
    {
        List<string> queryParams = new List<string>();
        
        if (!string.IsNullOrEmpty(request.DateFrom))
            queryParams.Add($"date_from={request.DateFrom}");
        if (!string.IsNullOrEmpty(request.DateTo))
            queryParams.Add($"date_to={request.DateTo}");
        if (!string.IsNullOrEmpty(request.Status))
            queryParams.Add($"status={request.Status}");
        if (request.Limit.HasValue)
            queryParams.Add($"limit={request.Limit}");
        if (request.Offset.HasValue)
            queryParams.Add($"offset={request.Offset}");

        string query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        return await GetAsync<AlternativePaymentOperationsResponse>($"/api/alternative-payments/v1/operations{query}", cancellationToken);
    }

    /// <summary>
    /// Get operations info by external ID
    /// GET /api/alternative-payments/v1/info
    /// </summary>
    /// <param name="externalId">External ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Alternative payment operations result</returns>
    public async Task<AlternativePaymentOperationsResult> GetInfoAsync(string externalId, CancellationToken cancellationToken = default)
    {
        string endpoint = $"/api/alternative-payments/v1/info?external_id={Uri.EscapeDataString(externalId)}";
        return await GetAsync<AlternativePaymentOperationsResult>(endpoint, cancellationToken);
    }

    /// <summary>
    /// Get available payment methods
    /// GET /api/alternative-payments/v1/methods
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Available payment methods</returns>
    public async Task<AlternativePaymentMethodsResponse> GetAvailableMethodsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<AlternativePaymentMethodsResponse>("/api/alternative-payments/v1/methods", cancellationToken);
    }

    /// <summary>
    /// Get payment status
    /// GET /api/alternative-payments/v1/{paymentId}/status
    /// </summary>
    /// <param name="paymentId">Payment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment status response</returns>
    public async Task<AlternativePaymentStatusResponse> GetStatusAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<AlternativePaymentStatusResponse>($"/api/alternative-payments/v1/{paymentId}/status", cancellationToken);
    }
} 
