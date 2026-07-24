using SYT.RozetkaPay.Models.Customers;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Contract for customer and wallet operations. Implemented by <see cref="CustomerService"/> and
/// intended as the injection/mocking seam for consumer code.
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// Get customer information and wallet
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Customer wallet response</returns>
    Task<CustomerWalletResponse> GetCustomerWalletAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add customer payment to wallet
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="request">Add card request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Card addition response</returns>
    Task<AddCardToWalletResponse> AddCardToWalletAsync(string customerId, AddCardToWalletRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete customer payment from wallet
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cardId">Card ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Card deletion response</returns>
    Task<DeleteCardFromWalletResponse> DeletePaymentFromWalletAsync(string customerId, string cardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find customer wallet item
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cardId">Card ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Wallet item response</returns>
    Task<WalletItemResponse> GetWalletItemAsync(string customerId, string cardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get confirmation status of the card in wallet
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cardId">Card ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Card confirmation status</returns>
    Task<CardConfirmationStatusResponse> GetCardConfirmationStatusAsync(string customerId, string cardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set default card
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="request">Set default card request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Set default card response</returns>
    Task<SetDefaultCardResponse> SetDefaultCardAsync(string customerId, SetDefaultCardRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all customer cards
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Customer cards response</returns>
    Task<CustomerCardsResponse> GetCustomerCardsAsync(string customerId, CancellationToken cancellationToken = default);
}
