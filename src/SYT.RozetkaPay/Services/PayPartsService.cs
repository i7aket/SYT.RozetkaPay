using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Models.PayParts;
using Microsoft.Extensions.Logging;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Service for PayParts (installment payment) operations
/// </summary>
public class PayPartsService : BaseService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PayPartsService"/> class.
    /// </summary>
    /// <param name="configuration">SDK configuration.</param>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="logger">Optional logger.</param>
    public PayPartsService(RozetkaPayConfiguration configuration, HttpClient httpClient, ILogger? logger = null)
        : base(configuration, httpClient, logger)
    {
    }

    /// <summary>
    /// Create PayParts order
    /// POST /api/payparts/v1/new
    /// </summary>
    /// <param name="request">PayParts order creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts order response</returns>
    public async Task<PayPartsOrderResponse> CreateOrderAsync(CreatePayPartsOrderRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsyncWithFallback<CreatePayPartsOrderRequest, PayPartsOrderResponse>(
            "/api/payparts/v1/order/create",
            "/api/payparts/v1/new",
            request,
            cancellationToken);
    }

    /// <summary>
    /// Confirm PayParts order
    /// POST /api/payments/v1/payparts/confirm
    /// </summary>
    /// <param name="request">PayParts confirm request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts order response</returns>
    public async Task<PayPartsOrderResponse> ConfirmOrderAsync(ConfirmPayPartsRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsyncWithFallback<ConfirmPayPartsRequest, PayPartsOrderResponse>(
            "/api/payparts/v1/order/confirm",
            "/api/payments/v1/payparts/confirm",
            request,
            cancellationToken);
    }

    /// <summary>
    /// Cancel PayParts order
    /// POST /api/payments/v1/payparts/cancel
    /// </summary>
    /// <param name="request">PayParts cancel request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts order response</returns>
    public async Task<PayPartsOrderResponse> CancelOrderAsync(CancelPayPartsRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsyncWithFallback<CancelPayPartsRequest, PayPartsOrderResponse>(
            "/api/payparts/v1/order/cancel",
            "/api/payments/v1/payparts/cancel",
            request,
            cancellationToken);
    }

    /// <summary>
    /// Refund PayParts order
    /// POST /api/payments/v1/payparts/refund
    /// </summary>
    /// <param name="request">PayParts refund request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts refund response</returns>
    public async Task<PayPartsRefundResponse> RefundOrderAsync(RefundPayPartsOrderRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsyncWithFallback<RefundPayPartsOrderRequest, PayPartsRefundResponse>(
            "/api/payparts/v1/refund",
            "/api/payments/v1/payparts/refund",
            request,
            cancellationToken);
    }

    /// <summary>
    /// Retry pending PayParts refund operation
    /// POST /api/payparts/v1/refund/retry
    /// </summary>
    /// <param name="request">Retry refund request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts operation result</returns>
    public async Task<PayPartsOperationResult> RetryRefundAsync(RetryRefundPPayRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<RetryRefundPPayRequest, PayPartsOperationResult>("/api/payparts/v1/refund/retry", request, cancellationToken);
    }

    /// <summary>
    /// Cancel pending PayParts refund operation
    /// POST /api/payparts/v1/refund/cancel
    /// </summary>
    /// <param name="request">Cancel refund request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts operation result</returns>
    public async Task<PayPartsOperationResult> CancelRefundAsync(CancelRefundPPayRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<CancelRefundPPayRequest, PayPartsOperationResult>("/api/payparts/v1/refund/cancel", request, cancellationToken);
    }

    /// <summary>
    /// Get operation info
    /// GET /api/payparts/v1/operation/{id}
    /// </summary>
    /// <param name="operationId">Operation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts operation info</returns>
    public async Task<PayPartsOperationResponse> GetOperationInfoAsync(string operationId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<PayPartsOperationResponse>($"/api/payparts/v1/operation/{operationId}", cancellationToken);
    }

    /// <summary>
    /// Get operation info by external ID and operation ID
    /// GET /api/payparts/v1/info/operation
    /// </summary>
    /// <param name="externalId">External ID</param>
    /// <param name="operationId">Operation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts operation info</returns>
    public async Task<PayPartsOperationResult> GetOperationInfoAsync(string externalId, string operationId, CancellationToken cancellationToken = default)
    {
        string primaryEndpoint =
            $"/api/payparts/v1/info/operation?external_id={Uri.EscapeDataString(externalId)}&operation_id={Uri.EscapeDataString(operationId)}";
        string fallbackEndpoint = $"/api/payparts/v1/operation/{Uri.EscapeDataString(operationId)}";
        return await GetAsyncWithFallback<PayPartsOperationResult>(primaryEndpoint, fallbackEndpoint, cancellationToken);
    }

    /// <summary>
    /// Get operations info by external ID
    /// GET /api/payparts/v1/info
    /// </summary>
    /// <param name="externalId">External ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts operations result</returns>
    public async Task<PayPartsOperationsResult> GetInfoAsync(string externalId, CancellationToken cancellationToken = default)
    {
        string endpoint = $"/api/payparts/v1/info?external_id={Uri.EscapeDataString(externalId)}";
        return await GetAsync<PayPartsOperationsResult>(endpoint, cancellationToken);
    }

    /// <summary>
    /// Get operations info
    /// GET /api/payparts/v1/operations
    /// </summary>
    /// <param name="request">Operations list request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts operations list</returns>
    public async Task<PayPartsOperationsListResponse> GetOperationsAsync(PayPartsOperationsListRequest request, CancellationToken cancellationToken = default)
    {
        List<string> queryParams = new List<string>();
        
        if (request.DateFrom.HasValue)
            queryParams.Add($"date_from={request.DateFrom:yyyy-MM-dd}");
        if (request.DateTo.HasValue)
            queryParams.Add($"date_to={request.DateTo:yyyy-MM-dd}");
        if (!string.IsNullOrEmpty(request.Status))
            queryParams.Add($"status={request.Status}");
        if (request.Limit.HasValue)
            queryParams.Add($"limit={request.Limit}");
        if (request.Offset.HasValue)
            queryParams.Add($"offset={request.Offset}");

        string query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        return await GetAsync<PayPartsOperationsListResponse>($"/api/payparts/v1/operations{query}", cancellationToken);
    }

    /// <summary>
    /// Get banks info for PayParts
    /// GET /api/payparts/v1/banks
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PayParts banks information</returns>
    public async Task<PayPartsBanksResponse> GetBanksAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsyncWithFallback<PayPartsBanksResponse>("/api/payparts/v1/banks/info", "/api/payparts/v1/banks", cancellationToken);
    }

    /// <summary>
    /// Resend PayParts callback
    /// POST /api/payparts/v1/callback/resend
    /// </summary>
    /// <param name="request">Resend callback request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Callback resend response</returns>
    public async Task<PayPartsResendCallbackResponse> ResendCallbackAsync(PayPartsResendCallbackRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<PayPartsResendCallbackRequest, PayPartsResendCallbackResponse>("/api/payparts/v1/callback/resend", request, cancellationToken);
    }
} 
