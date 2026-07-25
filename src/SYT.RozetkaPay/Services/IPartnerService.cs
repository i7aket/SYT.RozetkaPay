using SYT.RozetkaPay.Models.Merchants;
using SYT.RozetkaPay.Models.Partners;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Contract for partner reporting operations. Implemented by <see cref="PartnerService"/> and intended
/// as the injection/mocking seam for consumer code.
/// </summary>
/// <remarks>
/// Every identifier is a caller-supplied query value. Pass raw values: each is percent-encoded exactly
/// once, and none of them is ever logged.
/// </remarks>
public interface IPartnerService
{
    /// <summary>
    /// Read partner fee details for the authenticated partner, sending no query. Official operation
    /// <c>feeDetails</c>: <c>GET /api/partners/v1/fee-details</c>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inner and outer fees per channel</returns>
    Task<PartnerFeeDetailsResponse> GetFeeDetailsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Read partner fee details for one merchant project. Official operation <c>feeDetails</c>:
    /// <c>GET /api/partners/v1/fee-details?merchant_project_id={merchantProjectId}</c>.
    /// </summary>
    /// <param name="merchantProjectId">
    /// Merchant project ID. Pass the raw value: it is percent-encoded exactly once. An empty string is
    /// not null and is sent as an empty value.
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inner and outer fees per channel</returns>
    /// <exception cref="ArgumentNullException"><paramref name="merchantProjectId"/> is null.</exception>
    Task<PartnerFeeDetailsResponse> GetFeeDetailsAsync(
        string merchantProjectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Read merchant status for the authenticated partner, sending no query. Official operation
    /// <c>merchantStatus</c>: <c>GET /api/partners/v1/merchant-status</c>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entity, project and overall status</returns>
    Task<MerchantStatusResponse> GetMerchantStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Read merchant status with explicit query options. Official operation <c>merchantStatus</c>:
    /// <c>GET /api/partners/v1/merchant-status</c>.
    /// </summary>
    /// <param name="options">
    /// Optional <c>merchant_project_id</c> and <c>merchant_entity_id</c> query parameters, rendered in
    /// that order. A null property is omitted.
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entity, project and overall status</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    Task<MerchantStatusResponse> GetMerchantStatusAsync(
        PartnerMerchantStatusOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Read partner transaction details for one merchant entity. Official operation
    /// <c>transactionDetails</c>:
    /// <c>GET /api/partners/v1/transaction-details?merchant_entity_id={merchantEntityId}</c>.
    /// </summary>
    /// <param name="merchantEntityId">
    /// Merchant entity ID, required by the official operation. Pass the raw value: it is
    /// percent-encoded exactly once.
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching transactions</returns>
    /// <exception cref="ArgumentNullException"><paramref name="merchantEntityId"/> is null.</exception>
    Task<PartnerTransactionDetailsListResponse> GetTransactionDetailsAsync(
        string merchantEntityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Read partner transaction details with explicit query options. Official operation
    /// <c>transactionDetails</c>: <c>GET /api/partners/v1/transaction-details</c>.
    /// </summary>
    /// <param name="merchantEntityId">
    /// Merchant entity ID, required by the official operation. Pass the raw value: it is
    /// percent-encoded exactly once.
    /// </param>
    /// <param name="options">
    /// Optional <c>merchant_order_id</c> and <c>unified_external_id</c> query parameters, rendered
    /// after <c>merchant_entity_id</c> in that order. A null property is omitted.
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching transactions</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="merchantEntityId"/> or <paramref name="options"/> is null.
    /// </exception>
    Task<PartnerTransactionDetailsListResponse> GetTransactionDetailsAsync(
        string merchantEntityId,
        PartnerTransactionDetailsOptions options,
        CancellationToken cancellationToken = default);
}
