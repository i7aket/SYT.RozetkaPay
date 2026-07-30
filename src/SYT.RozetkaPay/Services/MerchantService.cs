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



}
