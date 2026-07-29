using System.Globalization;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Models.AlternativePayments;
using Microsoft.Extensions.Logging;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Service for alternative payment methods
/// </summary>
public class AlternativePaymentService : BaseService, IAlternativePaymentService
{
    private const string CreateEndpoint = "/api/alternative-payments/v1/create";

    private const string NewEndpoint = "/api/alternative-payments/v1/new";

    private const string RefundEndpoint = "/api/alternative-payments/v1/refund";

    private const string ResendCallbackEndpoint = "/api/alternative-payments/v1/callback/resend";

    private const string MethodsEndpoint = "/api/alternative-payments/v1/methods";

    /// <summary>
    /// Static route template of the operation-by-external-ID lookup, used as the log label only. The real
    /// request target carries the escaped external identifier, which must not be logged.
    /// </summary>
    private const string OperationByExternalIdLogLabel =
        "/api/alternative-payments/v1/operation/{external_id}";

    /// <summary>
    /// Route of the info/operation lookup. Also the log label: the real request target carries the caller's
    /// external and operation identifiers in the query.
    /// </summary>
    private const string InfoOperationEndpoint = "/api/alternative-payments/v1/info/operation";

    /// <summary>
    /// Route of the info lookup, and its log label. The real target carries the caller's external ID.
    /// </summary>
    private const string InfoEndpoint = "/api/alternative-payments/v1/info";

    /// <summary>
    /// Route of the operations list, and its log label. The real target carries the caller's filter and
    /// pagination values.
    /// </summary>
    private const string OperationsEndpoint = "/api/alternative-payments/v1/operations";

    /// <summary>
    /// Static route template of the status lookup, used as the log label only. The real request target
    /// carries the escaped payment identifier.
    /// </summary>
    private const string StatusLogLabel = "/api/alternative-payments/v1/{payment_id}/status";

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
        return await PostAsync<CreateAlternativePaymentRequest, AlternativePaymentResponse>(
            CreateEndpoint,
            CreateEndpoint,
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
        return await PostAsync<CreateAlternativePaymentRequest, AlternativePaymentOperationResult>(
            CreateEndpoint,
            CreateEndpoint,
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
        return await PostAsync<RefundAlternativePaymentRequest, AlternativePaymentResponse>(
            RefundEndpoint,
            RefundEndpoint,
            request,
            cancellationToken);
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
            ResendCallbackEndpoint,
            ResendCallbackEndpoint,
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
        string encodedExternalId = RequestTargetEncoding.EscapePathSegment(externalId, nameof(externalId));
        return await GetAsync<AlternativePaymentOperationResponse>(
            $"/api/alternative-payments/v1/operation/{encodedExternalId}",
            OperationByExternalIdLogLabel,
            cancellationToken);
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
            $"{InfoOperationEndpoint}?external_id={Uri.EscapeDataString(externalId)}&operation_id={Uri.EscapeDataString(operationId)}";
        return await GetAsync<AlternativePaymentOperationResult>(
            primaryEndpoint,
            InfoOperationEndpoint,
            cancellationToken);
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
        {
            queryParams.Add($"date_from={Uri.EscapeDataString(request.DateFrom)}");
        }

        if (!string.IsNullOrEmpty(request.DateTo))
        {
            queryParams.Add($"date_to={Uri.EscapeDataString(request.DateTo)}");
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            queryParams.Add($"status={Uri.EscapeDataString(request.Status)}");
        }

        if (request.Limit.HasValue)
        {
            queryParams.Add($"limit={Uri.EscapeDataString(request.Limit.Value.ToString(CultureInfo.InvariantCulture))}");
        }

        if (request.Offset.HasValue)
        {
            queryParams.Add($"offset={Uri.EscapeDataString(request.Offset.Value.ToString(CultureInfo.InvariantCulture))}");
        }

        string query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        return await GetAsync<AlternativePaymentOperationsResponse>(
            $"{OperationsEndpoint}{query}",
            OperationsEndpoint,
            cancellationToken);
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
        string endpoint = $"{InfoEndpoint}?external_id={Uri.EscapeDataString(externalId)}";
        return await GetAsync<AlternativePaymentOperationsResult>(endpoint, InfoEndpoint, cancellationToken);
    }

    /// <summary>
    /// Get available payment methods
    /// GET /api/alternative-payments/v1/methods
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Available payment methods</returns>
    public async Task<AlternativePaymentMethodsResponse> GetAvailableMethodsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<AlternativePaymentMethodsResponse>(MethodsEndpoint, MethodsEndpoint, cancellationToken);
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
        string encodedPaymentId = RequestTargetEncoding.EscapePathSegment(paymentId, nameof(paymentId));
        return await GetAsync<AlternativePaymentStatusResponse>(
            $"/api/alternative-payments/v1/{encodedPaymentId}/status",
            StatusLogLabel,
            cancellationToken);
    }
}
