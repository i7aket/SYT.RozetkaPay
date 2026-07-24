using SYT.RozetkaPay.Models.FinMon;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Contract for financial monitoring operations. Implemented by <see cref="FinMonService"/> and
/// intended as the injection/mocking seam for consumer code.
/// </summary>
public interface IFinMonService
{
    /// <summary>
    /// Get finmon P2P limits
    /// </summary>
    /// <param name="recipientIpn">IPN of recipient</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>P2P limits response</returns>
    Task<FinMonP2PPaymentPreLimitsResponse> GetRulesAsync(int recipientIpn, CancellationToken cancellationToken = default);
}
