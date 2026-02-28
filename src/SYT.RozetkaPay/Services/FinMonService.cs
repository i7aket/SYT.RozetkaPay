using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Models.FinMon;
using Microsoft.Extensions.Logging;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Service for financial monitoring operations
/// </summary>
public class FinMonService : BaseService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FinMonService"/> class.
    /// </summary>
    /// <param name="configuration">SDK configuration.</param>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="logger">Optional logger.</param>
    public FinMonService(RozetkaPayConfiguration configuration, HttpClient httpClient, ILogger? logger = null)
        : base(configuration, httpClient, logger)
    {
    }

    /// <summary>
    /// Get finmon P2P limits
    /// Fetches finmon P2P limits
    /// GET /api/finmon/v1/p2p-payment/pre-limits
    /// </summary>
    /// <param name="recipientIpn">IPN of recipient</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>P2P limits response</returns>
    public async Task<FinMonP2PPaymentPreLimitsResponse> GetRulesAsync(int recipientIpn, CancellationToken cancellationToken = default)
    {
        return await GetAsync<FinMonP2PPaymentPreLimitsResponse>($"/api/finmon/v1/p2p-payment/pre-limits?recipient_ipn={recipientIpn}", cancellationToken);
    }
} 
