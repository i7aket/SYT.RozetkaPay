using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Models.Customers;
using Microsoft.Extensions.Logging;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Service for customer and wallet operations
/// </summary>
public class CustomerService : BaseService, ICustomerService
{
    /// <summary>
    /// Route of the official wallet operations. Used both as the request target and as the static log
    /// label, so a caller identifier carried in the query never reaches a log sink.
    /// </summary>
    private const string WalletEndpoint = "/api/customers/v1/wallet";

    /// <summary>
    /// Route of the official wallet-item lookup. Also the log label: the real request target carries the
    /// caller's customer and card identifiers in the query.
    /// </summary>
    private const string WalletFindEndpoint = "/api/customers/v1/wallet/find";

    /// <summary>
    /// Route of the official card-confirmation-status lookup, and its log label.
    /// </summary>
    private const string WalletConfirmationStatusEndpoint = "/api/customers/v1/wallet/confirmation/status";

    /// <summary>
    /// Route of the official default-card operation, and its log label.
    /// </summary>
    private const string WalletSettingsSetEndpoint = "/api/customers/v1/wallet/settings/set";

    /// <summary>
    /// Static route template of the legacy per-customer wallet route, used as the log label only. The real
    /// request target carries the escaped customer identifier, which must not be logged.
    /// </summary>
    private const string CustomerWalletLogLabel = "/api/customers/v1/{customer_id}/wallet";

    /// <summary>
    /// Static route template of the per-customer cards route, used as the log label only.
    /// </summary>
    private const string CustomerCardsLogLabel = "/api/customers/v1/{customer_id}/cards";

    /// <summary>
    /// Static route template of the per-card route, used as the log label only. The real request target
    /// carries the escaped customer and card identifiers.
    /// </summary>
    private const string CustomerCardLogLabel = "/api/customers/v1/{customer_id}/cards/{card_id}";

    /// <summary>
    /// Static route template of the per-card confirmation route, used as the log label only.
    /// </summary>
    private const string CustomerCardConfirmationLogLabel =
        "/api/customers/v1/{customer_id}/cards/{card_id}/confirmation";

    /// <summary>
    /// Static route template of the legacy default-card route, used as the log label only.
    /// </summary>
    private const string DefaultCardLogLabel = "/api/customers/v1/{customer_id}/cards/default";

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerService"/> class.
    /// </summary>
    /// <param name="configuration">SDK configuration.</param>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="logger">Optional logger.</param>
    public CustomerService(RozetkaPayConfiguration configuration, HttpClient httpClient, ILogger? logger = null)
        : base(configuration, httpClient, logger)
    {
    }

    /// <summary>
    /// Get customer information and wallet
    /// GET /api/customers/v1/{customerId}/wallet
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Customer wallet response</returns>
    public async Task<CustomerWalletResponse> GetCustomerWalletAsync(string customerId, CancellationToken cancellationToken = default)
    {
        string primaryEndpoint = $"{WalletEndpoint}?external_id={Uri.EscapeDataString(customerId)}";
        return await GetAsync<CustomerWalletResponse>(
            primaryEndpoint,
            WalletEndpoint,
            cancellationToken);
    }

    /// <summary>
    /// Add customer payment to wallet
    /// POST /api/customers/v1/{customerId}/cards
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="request">Add card request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Card addition response</returns>
    public async Task<AddCardToWalletResponse> AddCardToWalletAsync(string customerId, AddCardToWalletRequest request, CancellationToken cancellationToken = default)
    {
        string primaryEndpoint = $"{WalletEndpoint}?external_id={Uri.EscapeDataString(customerId)}";
        return await PostAsync<AddCardToWalletRequest, AddCardToWalletResponse>(
            primaryEndpoint,
            WalletEndpoint,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Delete a customer payment method from the wallet, identifying the customer through the
    /// configured <c>X-CUSTOMER-AUTH</c> header.
    /// DELETE /api/customers/v1/wallet
    /// </summary>
    /// <param name="request">Payment method to delete. Sent as the JSON request body.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion result reported by the provider</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    public async Task<DeleteCustomerPaymentResult> DeleteCustomerPaymentAsync(DeleteCustomerPaymentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await DeleteAsync<DeleteCustomerPaymentRequest, DeleteCustomerPaymentResult>(
            WalletEndpoint,
            WalletEndpoint,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Delete a customer payment method from the wallet, identifying the customer by external ID.
    /// DELETE /api/customers/v1/wallet?external_id={externalId}
    /// </summary>
    /// <param name="externalId">Customer ID in the caller's system. Passed raw and escaped once.</param>
    /// <param name="request">Payment method to delete. Sent as the JSON request body.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion result reported by the provider</returns>
    /// <exception cref="ArgumentNullException"><paramref name="externalId"/> or <paramref name="request"/> is null.</exception>
    public async Task<DeleteCustomerPaymentResult> DeleteCustomerPaymentAsync(string externalId, DeleteCustomerPaymentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(externalId);
        ArgumentNullException.ThrowIfNull(request);

        return await DeleteAsync<DeleteCustomerPaymentRequest, DeleteCustomerPaymentResult>(
            $"{WalletEndpoint}?external_id={Uri.EscapeDataString(externalId)}",
            WalletEndpoint,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Delete customer payment from wallet
    /// DELETE /api/customers/v1/{customerId}/cards/{cardId}
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cardId">Card ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Card deletion response</returns>
    [Obsolete("Use DeleteCustomerPaymentAsync(...). This member calls the legacy /api/customers/v1/{customerId}/cards/{cardId} route.")]
    public async Task<DeleteCardFromWalletResponse> DeletePaymentFromWalletAsync(string customerId, string cardId, CancellationToken cancellationToken = default)
    {
        string encodedCustomerId = RequestTargetEncoding.EscapePathSegment(customerId, nameof(customerId));
        string encodedCardId = RequestTargetEncoding.EscapePathSegment(cardId, nameof(cardId));
        return await DeleteAsync<DeleteCardFromWalletResponse>(
            $"/api/customers/v1/{encodedCustomerId}/cards/{encodedCardId}",
            CustomerCardLogLabel,
            cancellationToken);
    }

    /// <summary>
    /// Find customer wallet item
    /// GET /api/customers/v1/{customerId}/cards/{cardId}
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cardId">Card ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Wallet item response</returns>
    public async Task<WalletItemResponse> GetWalletItemAsync(string customerId, string cardId, CancellationToken cancellationToken = default)
    {
        string primaryEndpoint =
            $"{WalletFindEndpoint}?external_id={Uri.EscapeDataString(customerId)}&option_id={Uri.EscapeDataString(cardId)}";
        return await GetAsync<WalletItemResponse>(
            primaryEndpoint,
            WalletFindEndpoint,
            cancellationToken);
    }

    /// <summary>
    /// Get confirmation status of the card in wallet
    /// GET /api/customers/v1/{customerId}/cards/{cardId}/confirmation
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cardId">Card ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Card confirmation status</returns>
    public async Task<CardConfirmationStatusResponse> GetCardConfirmationStatusAsync(string customerId, string cardId, CancellationToken cancellationToken = default)
    {
        string primaryEndpoint =
            $"{WalletConfirmationStatusEndpoint}?external_id={Uri.EscapeDataString(customerId)}&option_id={Uri.EscapeDataString(cardId)}";
        return await GetAsync<CardConfirmationStatusResponse>(
            primaryEndpoint,
            WalletConfirmationStatusEndpoint,
            cancellationToken);
    }

    /// <summary>
    /// Set default card
    /// POST /api/customers/v1/{customerId}/cards/default
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="request">Set default card request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Set default card response</returns>
    public async Task<SetDefaultCardResponse> SetDefaultCardAsync(string customerId, SetDefaultCardRequest request, CancellationToken cancellationToken = default)
    {
        string primaryEndpoint = $"{WalletSettingsSetEndpoint}?external_id={Uri.EscapeDataString(customerId)}";
        return await PostAsync<SetDefaultCardRequest, SetDefaultCardResponse>(
            primaryEndpoint,
            WalletSettingsSetEndpoint,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Get all customer cards
    /// GET /api/customers/v1/{customerId}/cards
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Customer cards response</returns>
    public async Task<CustomerCardsResponse> GetCustomerCardsAsync(string customerId, CancellationToken cancellationToken = default)
    {
        string encodedCustomerId = RequestTargetEncoding.EscapePathSegment(customerId, nameof(customerId));
        return await GetAsync<CustomerCardsResponse>(
            $"/api/customers/v1/{encodedCustomerId}/cards",
            CustomerCardsLogLabel,
            cancellationToken);
    }
}
