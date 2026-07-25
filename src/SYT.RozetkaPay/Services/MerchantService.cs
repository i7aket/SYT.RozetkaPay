using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Models.Merchants;
using Microsoft.Extensions.Logging;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Service for merchant operations
/// </summary>
public class MerchantService : BaseService, IMerchantService
{
    private const string MeEndpoint = "/api/merchants/v1/me";

    /// <summary>
    /// Route of both merchant-settings operations - the GET that reads them and the POST that replaces
    /// them - and their log label.
    /// </summary>
    private const string SettingsEndpoint = "/api/merchant/v1/settings";

    private const string CommissionRatesEndpoint = "/api/merchant/v1/commission-rates";

    /// <summary>
    /// Initializes a new instance of the <see cref="MerchantService"/> class.
    /// </summary>
    /// <param name="configuration">SDK configuration.</param>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="logger">Optional logger.</param>
    public MerchantService(RozetkaPayConfiguration configuration, HttpClient httpClient, ILogger? logger = null)
        : base(configuration, httpClient, logger)
    {
    }

    /// <summary>
    /// Validate merchant keys
    /// Validates merchant keys
    /// GET /api/merchants/v1/me
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Merchant validation response</returns>
    public async Task<MerchantValidationResponse> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<MerchantValidationResponse>(MeEndpoint, MeEndpoint, cancellationToken);
    }

    /// <summary>
    /// Get merchant settings
    /// GET /api/merchant/v1/settings
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Merchant settings</returns>
    public async Task<MerchantSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<MerchantSettingsResponse>(SettingsEndpoint, SettingsEndpoint, cancellationToken);
    }

    /// <summary>
    /// Update merchant settings
    /// POST /api/merchant/v1/settings
    /// </summary>
    /// <param name="request">Update settings request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated merchant settings</returns>
    public async Task<MerchantSettingsResponse> UpdateSettingsAsync(UpdateMerchantSettingsRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<UpdateMerchantSettingsRequest, MerchantSettingsResponse>(
            SettingsEndpoint,
            SettingsEndpoint,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Get commission rates
    /// GET /api/merchant/v1/commission-rates
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Commission rates information</returns>
    public async Task<CommissionRatesResponse> GetCommissionRatesAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<CommissionRatesResponse>(CommissionRatesEndpoint, CommissionRatesEndpoint, cancellationToken);
    }
}
