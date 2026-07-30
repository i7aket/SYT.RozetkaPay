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



}
