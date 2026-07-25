using System.Globalization;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Models.Payouts;
using Microsoft.Extensions.Logging;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Service for payout operations
/// </summary>
public class PayoutService : BaseService, IPayoutService
{
    private const string NewEndpoint = "/api/payouts/v1/new";

    private const string RequestPayoutEndpoint = "/api/payouts/v1/request-payout";

    /// <summary>
    /// Route of the payout-info operation. Also the log label: the real request target carries the
    /// caller's external ID in the query, which must not be logged.
    /// </summary>
    private const string InfoEndpoint = "/api/payouts/v1/info";

    /// <summary>
    /// Route of the payout-list operation, and its log label. The real target carries the caller's filter
    /// and pagination values.
    /// </summary>
    private const string ListEndpoint = "/api/payouts/v1/list";

    private const string BalanceEndpoint = "/api/payouts/v1/balance";

    /// <summary>
    /// Route of the account-balance operation, and its log label. The real target carries the merchant
    /// entity ID.
    /// </summary>
    private const string AccountBalanceEndpoint = "/api/payouts/v1/account-balance";

    private const string ResendCallbackEndpoint = "/api/payouts/v1/resend-callback";

    private const string CancelPayoutEndpoint = "/api/payouts/v1/cancel-payout";

    /// <summary>
    /// Initializes a new instance of the <see cref="PayoutService"/> class.
    /// </summary>
    /// <param name="configuration">SDK configuration.</param>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="logger">Optional logger.</param>
    public PayoutService(RozetkaPayConfiguration configuration, HttpClient httpClient, ILogger? logger = null)
        : base(configuration, httpClient, logger)
    {
    }

    /// <summary>
    /// Create a new payout
    /// POST /api/payouts/v1/new
    /// </summary>
    /// <param name="request">Payout creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payout response</returns>
    public async Task<PayoutResponse> CreateAsync(CreatePayoutRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<CreatePayoutRequest, PayoutResponse>(NewEndpoint, NewEndpoint, request, cancellationToken);
    }

    /// <summary>
    /// Create payout request using OpenAPI contract endpoint
    /// POST /api/payouts/v1/request-payout
    /// </summary>
    /// <param name="request">Request payout request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payout transaction result</returns>
    public async Task<PayoutTransactionResult> RequestPayoutAsync(RequestPayoutRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<RequestPayoutRequest, PayoutTransactionResult>(
            RequestPayoutEndpoint,
            RequestPayoutEndpoint,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Get payout information
    /// GET /api/payouts/v1/info
    /// </summary>
    /// <param name="externalId">External payout ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payout response</returns>
    public async Task<PayoutResponse> GetInfoAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<PayoutResponse>(
            $"{InfoEndpoint}?external_id={Uri.EscapeDataString(externalId)}",
            InfoEndpoint,
            cancellationToken);
    }

    /// <summary>
    /// Get payouts list
    /// GET /api/payouts/v1/list
    /// </summary>
    /// <param name="request">Payouts list request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payouts list response</returns>
    public async Task<PayoutListResponse> GetListAsync(PayoutListRequest request, CancellationToken cancellationToken = default)
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
        return await GetAsync<PayoutListResponse>($"{ListEndpoint}{query}", ListEndpoint, cancellationToken);
    }

    /// <summary>
    /// Get balance information
    /// GET /api/payouts/v1/balance
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Balance information</returns>
    public async Task<BalanceResponse> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<BalanceResponse>(BalanceEndpoint, BalanceEndpoint, cancellationToken);
    }

    /// <summary>
    /// Get merchant account balance using OpenAPI contract endpoint
    /// GET /api/payouts/v1/account-balance
    /// </summary>
    /// <param name="merchantEntityId">Merchant entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Balance information</returns>
    public async Task<BalanceResponse> GetAccountBalanceAsync(string merchantEntityId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<BalanceResponse>(
            $"{AccountBalanceEndpoint}?merchant_entity_id={Uri.EscapeDataString(merchantEntityId)}",
            AccountBalanceEndpoint,
            cancellationToken);
    }

    /// <summary>
    /// Resend payout callback
    /// POST /api/payouts/v1/resend-callback
    /// </summary>
    /// <param name="request">Resend callback request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Callback resend response</returns>
    public async Task<PayoutCallbackResendResponse> ResendCallbackAsync(ResendPayoutCallbackRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsyncWithNoContent<ResendPayoutCallbackRequest, PayoutCallbackResendResponse>(
            ResendCallbackEndpoint,
            ResendCallbackEndpoint,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Cancel cash payout
    /// POST /api/payouts/v1/cancel-payout
    /// </summary>
    /// <param name="request">Cancel payout request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payout transaction result</returns>
    public async Task<PayoutTransactionResult> CancelCashPayoutAsync(CancelCashPayoutRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<CancelCashPayoutRequest, PayoutTransactionResult>(
            CancelPayoutEndpoint,
            CancelPayoutEndpoint,
            request,
            cancellationToken);
    }
}
