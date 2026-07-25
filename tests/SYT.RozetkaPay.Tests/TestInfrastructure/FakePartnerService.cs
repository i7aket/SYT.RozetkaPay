using SYT.RozetkaPay.Models.Merchants;
using SYT.RozetkaPay.Models.Partners;
using SYT.RozetkaPay.Services;

namespace SYT.RozetkaPay.Tests.TestInfrastructure;

/// <summary>
/// Hand-written stand-in for <see cref="IPartnerService"/>. Its only job is to prove that a consumer can
/// substitute the contract without a mocking framework, and that a registration made before
/// <c>AddRozetkaPay</c> survives.
/// </summary>
internal sealed class FakePartnerService : IPartnerService
{
    public Task<PartnerFeeDetailsResponse> GetFeeDetailsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PartnerFeeDetailsResponse());
    }

    public Task<PartnerFeeDetailsResponse> GetFeeDetailsAsync(
        string merchantProjectId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PartnerFeeDetailsResponse());
    }

    public Task<MerchantStatusResponse> GetMerchantStatusAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new MerchantStatusResponse());
    }

    public Task<MerchantStatusResponse> GetMerchantStatusAsync(
        PartnerMerchantStatusOptions options,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new MerchantStatusResponse());
    }

    public Task<PartnerTransactionDetailsListResponse> GetTransactionDetailsAsync(
        string merchantEntityId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PartnerTransactionDetailsListResponse());
    }

    public Task<PartnerTransactionDetailsListResponse> GetTransactionDetailsAsync(
        string merchantEntityId,
        PartnerTransactionDetailsOptions options,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PartnerTransactionDetailsListResponse());
    }
}
