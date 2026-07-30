using SYT.RozetkaPay.Models.Payouts;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Contract for payout operations. Implemented by <see cref="PayoutService"/> and
/// intended as the injection/mocking seam for consumer code.
/// </summary>
public interface IPayoutService
{

    /// <summary>
    /// Create payout request using OpenAPI contract endpoint
    /// </summary>
    /// <param name="request">Request payout request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payout transaction result</returns>
    Task<PayoutTransactionResult> RequestPayoutAsync(RequestPayoutRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get payout information
    /// </summary>
    /// <param name="externalId">External payout ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payout response</returns>
    Task<PayoutTransactionResult> GetInfoAsync(string externalId, CancellationToken cancellationToken = default);



    /// <summary>
    /// Get merchant account balance using OpenAPI contract endpoint
    /// </summary>
    /// <param name="merchantEntityId">Merchant entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Balance information</returns>
    Task<BalanceResponse> GetAccountBalanceAsync(string merchantEntityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resend payout callback
    /// </summary>
    /// <param name="request">Resend callback request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Callback resend response</returns>
    Task<PayoutCallbackResendResponse> ResendCallbackAsync(ResendPayoutCallbackRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel cash payout
    /// </summary>
    /// <param name="request">Cancel payout request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payout transaction result</returns>
    Task<PayoutTransactionResult> CancelCashPayoutAsync(CancelCashPayoutRequest request, CancellationToken cancellationToken = default);
}
