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
    /// Delete a customer payment method from the wallet, identifying the customer through the
    /// configured <c>X-CUSTOMER-AUTH</c> header. Official operation <c>deleteCustomerPayment</c>:
    /// <c>DELETE /api/customers/v1/wallet</c>.
    /// </summary>
    /// <param name="request">Payment method to delete. Sent as the JSON request body.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion result reported by the provider</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    Task<DeleteCustomerPaymentResult> DeleteCustomerPaymentAsync(DeleteCustomerPaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a customer payment method from the wallet, identifying the customer by external ID.
    /// Official operation <c>deleteCustomerPayment</c>: <c>DELETE /api/customers/v1/wallet</c>.
    /// </summary>
    /// <param name="externalId">
    /// Customer ID in the caller's system. Pass the raw value: it is percent-encoded exactly once as
    /// the <c>external_id</c> query value.
    /// </param>
    /// <param name="request">Payment method to delete. Sent as the JSON request body.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion result reported by the provider</returns>
    /// <exception cref="ArgumentNullException"><paramref name="externalId"/> or <paramref name="request"/> is null.</exception>
    Task<DeleteCustomerPaymentResult> DeleteCustomerPaymentAsync(string externalId, DeleteCustomerPaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete customer payment from wallet
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cardId">Card ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Card deletion response</returns>
    [Obsolete("Use DeleteCustomerPaymentAsync(...). This member calls the legacy /api/customers/v1/{customerId}/cards/{cardId} route.")]
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
