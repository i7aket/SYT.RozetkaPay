using SYT.RozetkaPay.Models.Merchants;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Contract for merchant operations. Implemented by <see cref="MerchantService"/> and
/// intended as the injection/mocking seam for consumer code.
/// </summary>
public interface IMerchantService
{
    /// <summary>
    /// Validate merchant keys
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Merchant validation response</returns>
    Task<MerchantValidationResponse> GetInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get merchant settings
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Merchant settings</returns>
    Task<MerchantSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Update merchant settings
    /// </summary>
    /// <param name="request">Update settings request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated merchant settings</returns>
    Task<MerchantSettingsResponse> UpdateSettingsAsync(UpdateMerchantSettingsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get commission rates
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Commission rates information</returns>
    Task<CommissionRatesResponse> GetCommissionRatesAsync(CancellationToken cancellationToken = default);
}
