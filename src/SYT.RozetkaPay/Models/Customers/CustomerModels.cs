using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Converters;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Payments;

namespace SYT.RozetkaPay.Models.Customers;

/// <summary>
/// Request to add customer payment method (JSON object as per CDN documentation)
/// </summary>
public class AddCustomerPaymentRequest
{
    /// <summary>
    /// Callback URL for payment confirmation notifications (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Payment method details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_method")]
    [Required]
    public AddCustomerPaymentMethod PaymentMethod { get; set; } = new();

    /// <summary>
    /// Result URL for user redirection (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("result_url")]
    public string? ResultUrl { get; set; }

    /// <summary>
    /// Whether to make this payment method default (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("make_default")]
    public bool MakeDefault { get; set; } = false;

    /// <summary>
    /// Payment mode (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("mode")]
    [Required]
    public PaymentMode Mode { get; set; }

    /// <summary>
    /// Browser fingerprint for fraud detection (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("fingerprint")]
    public BrowserFingerprint? Fingerprint { get; set; }
}

/// <summary>
/// Add customer payment method details (JSON object as per CDN documentation)
/// </summary>
public class AddCustomerPaymentMethod
{
    /// <summary>
    /// Payment method type (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("type")]
    [Required]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Credit card token details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("cc_token")]
    public AddCustomerCCToken? CCToken { get; set; }

    /// <summary>
    /// Encrypted credit card token details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("encrypted_cc_token")]
    public AddCustomerEncryptedCCToken? EncryptedCCToken { get; set; }
}

/// <summary>
/// Credit card token for adding customer payment method (JSON object as per CDN documentation)
/// </summary>
public class AddCustomerCCToken
{
    /// <summary>
    /// Card token (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("token")]
    [Required]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Card mask (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("mask")]
    [Required]
    public string Mask { get; set; } = string.Empty;

    /// <summary>
    /// Card expiration date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("expires_at")]
    [JsonConverter(typeof(FlexibleDateTimeConverter))]
    [Required]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether to use 3DS flow (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("use_3ds_flow")]
    public bool? Use3DSFlow { get; set; }
}

/// <summary>
/// Encrypted credit card token for adding customer payment method (JSON object as per CDN documentation)
/// </summary>
public class AddCustomerEncryptedCCToken
{
    /// <summary>
    /// Encrypted data from card widget (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("data")]
    [Required]
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use 3DS flow (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("use_3ds_flow")]
    public bool? Use3DSFlow { get; set; }
}

/// <summary>
/// Request to delete customer payment method (JSON object as per CDN documentation)
/// </summary>
public class DeleteCustomerPaymentRequest
{
    /// <summary>
    /// Card identifier in wallet (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("option_id")]
    [Required]
    public string OptionId { get; set; } = string.Empty;

    /// <summary>
    /// Payment method type (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("type")]
    [Required]
    public string Type { get; set; } = "card";
}

// ===================== CUSTOMER WALLET MODELS =====================

/// <summary>
/// Customer wallet response
/// </summary>
public class CustomerWalletResponse
{
    /// <summary>
    /// Customer ID
    /// </summary>
    [JsonPropertyName("customer_id")]
    public string? CustomerId { get; set; }

    /// <summary>
    /// Wallet cards
    /// </summary>
    [JsonPropertyName("cards")]
    public List<WalletCard>? Cards { get; set; }

    /// <summary>
    /// Default card ID
    /// </summary>
    [JsonPropertyName("default_card_id")]
    public string? DefaultCardId { get; set; }
}

/// <summary>
/// Wallet card information
/// </summary>
public class WalletCard
{
    /// <summary>
    /// Card ID
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Masked card number
    /// </summary>
    [JsonPropertyName("mask")]
    public string? Mask { get; set; }

    /// <summary>
    /// Card payment system
    /// </summary>
    [JsonPropertyName("payment_system")]
    public string? PaymentSystem { get; set; }

    /// <summary>
    /// Card type
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Bank name
    /// </summary>
    [JsonPropertyName("bank_name")]
    public string? BankName { get; set; }

    /// <summary>
    /// Whether this is the default card
    /// </summary>
    [JsonPropertyName("is_default")]
    public bool? IsDefault { get; set; }

    /// <summary>
    /// Card status
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Card creation timestamp
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }
}

// ===================== ADD CARD TO WALLET MODELS =====================

/// <summary>
/// Request to add card to wallet
/// </summary>
public class AddCardToWalletRequest
{
    /// <summary>
    /// Card details
    /// </summary>
    [Required]
    [JsonPropertyName("card")]
    public required WalletCardDetails Card { get; set; }

    /// <summary>
    /// Whether to set as default card
    /// </summary>
    [JsonPropertyName("set_as_default")]
    public bool SetAsDefault { get; set; } = false;

    /// <summary>
    /// Verification amount for card confirmation
    /// </summary>
    [JsonPropertyName("verification_amount")]
    public int? VerificationAmount { get; set; }
}

/// <summary>
/// Card details for wallet
/// </summary>
public class WalletCardDetails
{
    /// <summary>
    /// Card number
    /// </summary>
    [Required]
    [JsonPropertyName("number")]
    public required string Number { get; set; }

    /// <summary>
    /// Expiry month
    /// </summary>
    [Required]
    [JsonPropertyName("exp_month")]
    public required string ExpMonth { get; set; }

    /// <summary>
    /// Expiry year
    /// </summary>
    [Required]
    [JsonPropertyName("exp_year")]
    public required string ExpYear { get; set; }

    /// <summary>
    /// CVV
    /// </summary>
    [Required]
    [JsonPropertyName("cvv")]
    public required string Cvv { get; set; }

    /// <summary>
    /// Cardholder name
    /// </summary>
    [JsonPropertyName("holder_name")]
    public string? HolderName { get; set; }
}

/// <summary>
/// Response for adding card to wallet
/// </summary>
public class AddCardToWalletResponse
{
    /// <summary>
    /// Card ID
    /// </summary>
    [JsonPropertyName("card_id")]
    public string? CardId { get; set; }

    /// <summary>
    /// Operation status
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Verification required
    /// </summary>
    [JsonPropertyName("verification_required")]
    public bool? VerificationRequired { get; set; }

    /// <summary>
    /// Card information
    /// </summary>
    [JsonPropertyName("card")]
    public WalletCard? Card { get; set; }
}

// ===================== DELETE CARD FROM WALLET MODELS =====================

/// <summary>
/// Response for deleting card from wallet
/// </summary>
public class DeleteCardFromWalletResponse
{
    /// <summary>
    /// Operation status
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Status message
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

// ===================== WALLET ITEM MODELS =====================

/// <summary>
/// Wallet item response
/// </summary>
public class WalletItemResponse
{
    /// <summary>
    /// Card information
    /// </summary>
    [JsonPropertyName("card")]
    public WalletCard? Card { get; set; }

    /// <summary>
    /// Card transactions history
    /// </summary>
    [JsonPropertyName("transactions")]
    public List<WalletTransaction>? Transactions { get; set; }
}

/// <summary>
/// Wallet transaction
/// </summary>
public class WalletTransaction
{
    /// <summary>
    /// Transaction ID
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Transaction type
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Transaction amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Transaction currency
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Transaction status
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Transaction timestamp (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }
}

// ===================== CARD CONFIRMATION MODELS =====================

/// <summary>
/// Card confirmation status response
/// </summary>
public class CardConfirmationStatusResponse
{
    /// <summary>
    /// Card ID
    /// </summary>
    [JsonPropertyName("card_id")]
    public string? CardId { get; set; }

    /// <summary>
    /// Confirmation status
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Confirmation required
    /// </summary>
    [JsonPropertyName("confirmation_required")]
    public bool? ConfirmationRequired { get; set; }

    /// <summary>
    /// Verification amount
    /// </summary>
    [JsonPropertyName("verification_amount")]
    public int? VerificationAmount { get; set; }
}

// ===================== SET DEFAULT CARD MODELS =====================

/// <summary>
/// Request to set default card
/// </summary>
public class SetDefaultCardRequest
{
    /// <summary>
    /// Card ID to set as default
    /// </summary>
    [Required]
    [JsonPropertyName("card_id")]
    public required string CardId { get; set; }
}

/// <summary>
/// Set default card response
/// </summary>
public class SetDefaultCardResponse
{
    /// <summary>
    /// Operation status
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Default card ID
    /// </summary>
    [JsonPropertyName("default_card_id")]
    public string? DefaultCardId { get; set; }

    /// <summary>
    /// Status message
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

// ===================== CUSTOMER CARDS MODELS =====================

/// <summary>
/// Customer cards response
/// </summary>
public class CustomerCardsResponse
{
    /// <summary>
    /// Customer ID
    /// </summary>
    [JsonPropertyName("customer_id")]
    public string? CustomerId { get; set; }

    /// <summary>
    /// List of cards
    /// </summary>
    [JsonPropertyName("cards")]
    public List<WalletCard>? Cards { get; set; }

    /// <summary>
    /// Total number of cards
    /// </summary>
    [JsonPropertyName("total_count")]
    public int? TotalCount { get; set; }
}

/// <summary>
/// Response when adding customer payment method
/// </summary>
public class AddCustomerPaymentResult
{
    /// <summary>
    /// Action details if required
    /// </summary>
    [JsonPropertyName("action")]
    public UserAction? Action { get; set; }

    /// <summary>
    /// Flag that indicates necessity of the post-request action (fill data on the checkout, 3ds confirmation, etc.)
    /// </summary>
    [JsonPropertyName("action_required")]
    public bool ActionRequired { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Payment method information
    /// </summary>
    [JsonPropertyName("payment_method")]
    public WalletItem? PaymentMethod { get; set; }

    /// <summary>
    /// Operation status
    /// </summary>
    [JsonPropertyName("status")]
    public OperationStatus Status { get; set; }

    /// <summary>
    /// Type of the performed operation
    /// </summary>
    [JsonPropertyName("operation")]
    public string Operation { get; set; } = "add_payment_method";

    /// <summary>
    /// Customer information
    /// </summary>
    [JsonPropertyName("customer")]
    public CustomerInfo? Customer { get; set; }
}

/// <summary>
/// Response when deleting customer payment method
/// </summary>
public class DeleteCustomerPaymentResult
{
    /// <summary>
    /// Whether deletion was successful
    /// </summary>
    [JsonPropertyName("delete")]
    public bool Delete { get; set; }

    /// <summary>
    /// Payment method option ID
    /// </summary>
    [JsonPropertyName("option_id")]
    public string? OptionId { get; set; }

    /// <summary>
    /// Payment method type
    /// </summary>
    [JsonPropertyName("type")]
    public PaymentMethodType Type { get; set; }
}

