using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Models.Customers;
using Microsoft.Extensions.Logging;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Service for customer and wallet operations
/// </summary>
public class CustomerService : BaseService
{
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
        string primaryEndpoint = $"/api/customers/v1/wallet?external_id={Uri.EscapeDataString(customerId)}";
        string fallbackEndpoint = $"/api/customers/v1/{Uri.EscapeDataString(customerId)}/wallet";
        return await GetAsyncWithFallback<CustomerWalletResponse>(primaryEndpoint, fallbackEndpoint, cancellationToken);
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
        string primaryEndpoint = $"/api/customers/v1/wallet?external_id={Uri.EscapeDataString(customerId)}";
        string fallbackEndpoint = $"/api/customers/v1/{Uri.EscapeDataString(customerId)}/cards";
        return await PostAsyncWithFallback<AddCardToWalletRequest, AddCardToWalletResponse>(primaryEndpoint, fallbackEndpoint, request, cancellationToken);
    }

    /// <summary>
    /// Delete customer payment from wallet
    /// DELETE /api/customers/v1/{customerId}/cards/{cardId}
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cardId">Card ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Card deletion response</returns>
    public async Task<DeleteCardFromWalletResponse> DeletePaymentFromWalletAsync(string customerId, string cardId, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<DeleteCardFromWalletResponse>($"/api/customers/v1/{customerId}/cards/{cardId}", cancellationToken);
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
            $"/api/customers/v1/wallet/find?external_id={Uri.EscapeDataString(customerId)}&option_id={Uri.EscapeDataString(cardId)}";
        string fallbackEndpoint = $"/api/customers/v1/{Uri.EscapeDataString(customerId)}/cards/{Uri.EscapeDataString(cardId)}";
        return await GetAsyncWithFallback<WalletItemResponse>(primaryEndpoint, fallbackEndpoint, cancellationToken);
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
            $"/api/customers/v1/wallet/confirmation/status?external_id={Uri.EscapeDataString(customerId)}&option_id={Uri.EscapeDataString(cardId)}";
        string fallbackEndpoint = $"/api/customers/v1/{Uri.EscapeDataString(customerId)}/cards/{Uri.EscapeDataString(cardId)}/confirmation";
        return await GetAsyncWithFallback<CardConfirmationStatusResponse>(primaryEndpoint, fallbackEndpoint, cancellationToken);
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
        string primaryEndpoint = $"/api/customers/v1/wallet/settings/set?external_id={Uri.EscapeDataString(customerId)}";
        string fallbackEndpoint = $"/api/customers/v1/{Uri.EscapeDataString(customerId)}/cards/default";
        return await PostAsyncWithFallback<SetDefaultCardRequest, SetDefaultCardResponse>(primaryEndpoint, fallbackEndpoint, request, cancellationToken);
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
        return await GetAsync<CustomerCardsResponse>($"/api/customers/v1/{customerId}/cards", cancellationToken);
    }
} 
