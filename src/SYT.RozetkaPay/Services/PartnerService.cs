using Microsoft.Extensions.Logging;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Models.Merchants;
using SYT.RozetkaPay.Models.Partners;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Service for partner reporting operations
/// </summary>
/// <remarks>
/// Query values are escaped at their own insertion point, exactly once, and the log label is always the
/// static route with no query at all. Parameter order is fixed by the SDK so that two identical calls
/// always produce the same request target.
/// </remarks>
public class PartnerService : BaseService, IPartnerService
{
    /// <summary>
    /// Route of the official fee-details operation. Also the log label: the real request target may
    /// carry a merchant project ID, which must not be logged.
    /// </summary>
    private const string FeeDetailsEndpoint = "/api/partners/v1/fee-details";

    /// <summary>
    /// Route of the official merchant-status operation, and its log label.
    /// </summary>
    private const string MerchantStatusEndpoint = "/api/partners/v1/merchant-status";

    /// <summary>
    /// Route of the official transaction-details operation, and its log label.
    /// </summary>
    private const string TransactionDetailsEndpoint = "/api/partners/v1/transaction-details";

    /// <summary>
    /// Initializes a new instance of the <see cref="PartnerService"/> class.
    /// </summary>
    /// <param name="configuration">SDK configuration.</param>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="logger">Optional logger.</param>
    public PartnerService(RozetkaPayConfiguration configuration, HttpClient httpClient, ILogger? logger = null)
        : base(configuration, httpClient, logger)
    {
    }

    /// <summary>
    /// Read partner fee details for the authenticated partner
    /// GET /api/partners/v1/fee-details
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inner and outer fees per channel</returns>
    public async Task<PartnerFeeDetailsResponse> GetFeeDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<PartnerFeeDetailsResponse>(FeeDetailsEndpoint, FeeDetailsEndpoint, cancellationToken);
    }

    /// <summary>
    /// Read partner fee details for one merchant project
    /// GET /api/partners/v1/fee-details?merchant_project_id={merchantProjectId}
    /// </summary>
    /// <param name="merchantProjectId">Merchant project ID. Passed raw and escaped once.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inner and outer fees per channel</returns>
    /// <exception cref="ArgumentNullException"><paramref name="merchantProjectId"/> is null.</exception>
    public async Task<PartnerFeeDetailsResponse> GetFeeDetailsAsync(
        string merchantProjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(merchantProjectId);

        return await GetAsync<PartnerFeeDetailsResponse>(
            $"{FeeDetailsEndpoint}?merchant_project_id={Uri.EscapeDataString(merchantProjectId)}",
            FeeDetailsEndpoint,
            cancellationToken);
    }

    /// <summary>
    /// Read merchant status for the authenticated partner
    /// GET /api/partners/v1/merchant-status
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entity, project and overall status</returns>
    public async Task<MerchantStatusResponse> GetMerchantStatusAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<MerchantStatusResponse>(
            MerchantStatusEndpoint,
            MerchantStatusEndpoint,
            cancellationToken);
    }

    /// <summary>
    /// Read merchant status with explicit query options
    /// GET /api/partners/v1/merchant-status?merchant_project_id={...}&amp;merchant_entity_id={...}
    /// </summary>
    /// <param name="options">Optional query parameters. A null property is omitted.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entity, project and overall status</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    public async Task<MerchantStatusResponse> GetMerchantStatusAsync(
        PartnerMerchantStatusOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Deterministic order, and null means "omit". An empty value is not null: it is sent as an
        // empty query value so the provider - which owns non-empty validation - can reject it.
        List<string> query = new(2);
        if (options.MerchantProjectId is not null)
        {
            query.Add($"merchant_project_id={Uri.EscapeDataString(options.MerchantProjectId)}");
        }

        if (options.MerchantEntityId is not null)
        {
            query.Add($"merchant_entity_id={Uri.EscapeDataString(options.MerchantEntityId)}");
        }

        string endpoint = query.Count > 0
            ? $"{MerchantStatusEndpoint}?{string.Join('&', query)}"
            : MerchantStatusEndpoint;

        return await GetAsync<MerchantStatusResponse>(endpoint, MerchantStatusEndpoint, cancellationToken);
    }

    /// <summary>
    /// Read partner transaction details for one merchant entity
    /// GET /api/partners/v1/transaction-details?merchant_entity_id={merchantEntityId}
    /// </summary>
    /// <param name="merchantEntityId">Merchant entity ID. Passed raw and escaped once.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching transactions</returns>
    /// <exception cref="ArgumentNullException"><paramref name="merchantEntityId"/> is null.</exception>
    public async Task<PartnerTransactionDetailsListResponse> GetTransactionDetailsAsync(
        string merchantEntityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(merchantEntityId);

        return await GetAsync<PartnerTransactionDetailsListResponse>(
            $"{TransactionDetailsEndpoint}?merchant_entity_id={Uri.EscapeDataString(merchantEntityId)}",
            TransactionDetailsEndpoint,
            cancellationToken);
    }

    /// <summary>
    /// Read partner transaction details with explicit query options
    /// GET /api/partners/v1/transaction-details?merchant_entity_id={...}&amp;merchant_order_id={...}&amp;unified_external_id={...}
    /// </summary>
    /// <param name="merchantEntityId">Merchant entity ID. Passed raw and escaped once.</param>
    /// <param name="options">Optional query parameters. A null property is omitted.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching transactions</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="merchantEntityId"/> or <paramref name="options"/> is null.
    /// </exception>
    public async Task<PartnerTransactionDetailsListResponse> GetTransactionDetailsAsync(
        string merchantEntityId,
        PartnerTransactionDetailsOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(merchantEntityId);
        ArgumentNullException.ThrowIfNull(options);

        // The required parameter is always first, then the optional ones in schema order.
        List<string> query = new(3)
        {
            $"merchant_entity_id={Uri.EscapeDataString(merchantEntityId)}"
        };

        if (options.MerchantOrderId is not null)
        {
            query.Add($"merchant_order_id={Uri.EscapeDataString(options.MerchantOrderId)}");
        }

        if (options.UnifiedExternalId is not null)
        {
            query.Add($"unified_external_id={Uri.EscapeDataString(options.UnifiedExternalId)}");
        }

        return await GetAsync<PartnerTransactionDetailsListResponse>(
            $"{TransactionDetailsEndpoint}?{string.Join('&', query)}",
            TransactionDetailsEndpoint,
            cancellationToken);
    }
}
