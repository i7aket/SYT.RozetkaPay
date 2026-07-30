using System.Globalization;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Models.PayParts;
using Microsoft.Extensions.Logging;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Service for PayParts (installment payment) operations
/// </summary>
public class PayPartsService : BaseService, IPayPartsService
{
    private const string OrderCreateEndpoint = "/api/payparts/v1/order/create";


    private const string OrderConfirmEndpoint = "/api/payparts/v1/order/confirm";


    private const string OrderCancelEndpoint = "/api/payparts/v1/order/cancel";


    private const string RefundEndpoint = "/api/payparts/v1/refund";


    private const string RetryRefundEndpoint = "/api/payparts/v1/refund/retry";

    private const string CancelRefundEndpoint = "/api/payparts/v1/refund/cancel";

    /// <summary>
    /// Static route template of the operation-by-ID lookup, used as the log label only. The real request
    /// target carries the escaped operation identifier, which must not be logged.
    /// </summary>
    private const string OperationByIdLogLabel = "/api/payparts/v1/operation/{operation_id}";

    /// <summary>
    /// Route of the info/operation lookup. Also the log label: the real request target carries the caller's
    /// external and operation identifiers in the query.
    /// </summary>
    private const string InfoOperationEndpoint = "/api/payparts/v1/info/operation";

    /// <summary>
    /// Route of the info lookup, and its log label. The real target carries the caller's external ID.
    /// </summary>
    private const string InfoEndpoint = "/api/payparts/v1/info";


    private const string BanksInfoEndpoint = "/api/payparts/v1/banks/info";


    private const string ResendCallbackEndpoint = "/api/payparts/v1/callback/resend";

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
    public async Task<PayPartsOrderResponse> CreateOrderAsync(CreatePayPartsOrder request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<CreatePayPartsOrder, PayPartsOrderResponse>(
            OrderCreateEndpoint,
            OrderCreateEndpoint,
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
        return await PostAsync<ConfirmPayPartsRequest, PayPartsOrderResponse>(
            OrderConfirmEndpoint,
            OrderConfirmEndpoint,
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
        return await PostAsync<CancelPayPartsRequest, PayPartsOrderResponse>(
            OrderCancelEndpoint,
            OrderCancelEndpoint,
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
        return await PostAsync<RefundPayPartsOrderRequest, PayPartsRefundResponse>(
            RefundEndpoint,
            RefundEndpoint,
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
        return await PostAsync<RetryRefundPPayRequest, PayPartsOperationResult>(
            RetryRefundEndpoint,
            RetryRefundEndpoint,
            request,
            cancellationToken);
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
        return await PostAsync<CancelRefundPPayRequest, PayPartsOperationResult>(
            CancelRefundEndpoint,
            CancelRefundEndpoint,
            request,
            cancellationToken);
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
        string encodedOperationId = RequestTargetEncoding.EscapePathSegment(operationId, nameof(operationId));
        return await GetAsync<PayPartsOperationResponse>(
            $"/api/payparts/v1/operation/{encodedOperationId}",
            OperationByIdLogLabel,
            cancellationToken);
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
            $"{InfoOperationEndpoint}?external_id={Uri.EscapeDataString(externalId)}&operation_id={Uri.EscapeDataString(operationId)}";
        return await GetAsync<PayPartsOperationResult>(
            primaryEndpoint,
            InfoOperationEndpoint,
            cancellationToken);
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
        string endpoint = $"{InfoEndpoint}?external_id={Uri.EscapeDataString(externalId)}";
        return await GetAsync<PayPartsOperationsResult>(endpoint, InfoEndpoint, cancellationToken);
    }


    /// <summary>
    /// Get banks info for PayParts
    /// GET /api/payparts/v1/banks/info
    /// </summary>
    /// <remarks>
    /// The operation answers with a bare JSON array. It was being deserialized into an object with a
    /// <c>banks</c> property, which no response ever carries, so every call threw a
    /// <see cref="System.Text.Json.JsonException"/> - found by calling the live gateway, because the
    /// contract tests assert the request and say nothing about the shape of the reply.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The banks offering instalments, as the operation returns them.</returns>
    public async Task<List<PayPartsBankInfo>> GetBanksAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<List<PayPartsBankInfo>>(
            BanksInfoEndpoint,
            BanksInfoEndpoint,
            cancellationToken);
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
        return await PostAsync<PayPartsResendCallbackRequest, PayPartsResendCallbackResponse>(
            ResendCallbackEndpoint,
            ResendCallbackEndpoint,
            request,
            cancellationToken);
    }
}
