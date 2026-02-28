using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Models.Common;

namespace SYT.RozetkaPay.Models.Payments;

/// <summary>
/// Request for creating a payment
/// </summary>
public class CreatePaymentRequest
{
    /// <summary>
    /// Payment amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    [Required]
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment currency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("currency")]
    [Required]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// External ID to link payment in your system (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    [Required]
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Payment mode (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("mode")]
    [Required]
    public PaymentMode Mode { get; set; }

    /// <summary>
    /// Callback URL for payment notifications (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Result URL for user redirection (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("result_url")]
    public string? ResultUrl { get; set; }

    /// <summary>
    /// Whether to confirm payment automatically (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("confirm")]
    public bool Confirm { get; set; } = true;

    /// <summary>
    /// Payment description (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Additional payload data (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    /// <summary>
    /// Customer information (JSON object as per CDN documentation)
    /// For hosted mode, payment_method is not required
    /// For direct mode, payment_method is required
    /// </summary>
    [JsonPropertyName("customer")]
    public CustomerInfo? Customer { get; set; }

    /// <summary>
    /// Product list for express checkout (JSON array as per CDN documentation)
    /// </summary>
    [JsonPropertyName("products")]
    public List<Product>? Products { get; set; }

    /// <summary>
    /// Recipient information for P2P payments (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("recipient")]
    public RecipientRequestUserDetails? Recipient { get; set; }

    /// <summary>
    /// Whether to initialize recurrent payment (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("init_recurrent_payment")]
    public bool? InitRecurrentPayment { get; set; }

    /// <summary>
    /// Unified external ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("unified_external_id")]
    public string? UnifiedExternalId { get; set; }
}

/// <summary>
/// Customer information for payment request - OpenAPI compliant
/// Payment method is optional for hosted mode, required for direct mode
/// </summary>
public class CustomerInfo : BaseRequestUserDetails
{
    /// <summary>
    /// Customer payment method (JSON object as per CDN documentation)
    /// Required only for direct mode, optional for hosted mode
    /// </summary>
    [JsonPropertyName("payment_method")]
    public CustomerRequestPaymentMethod? PaymentMethod { get; set; }

    /// <summary>
    /// Checkout color mode (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("color_mode")]
    public CheckoutColorMode? ColorMode { get; set; }

    /// <summary>
    /// Customer locale (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("locale")]
    public CustomerCheckoutLocale? Locale { get; set; }

    /// <summary>
    /// Customer account number (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("account_number")]
    public string? AccountNumber { get; set; }

    /// <summary>
    /// Customer IP address (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Browser fingerprint (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("fingerprint")]
    public BrowserFingerprint? Fingerprint { get; set; }

    /// <summary>
    /// User information with additional customer details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("user_info")]
    public CustomerUserInfo? UserInfo { get; set; }
}

/// <summary>
/// Customer user information for payment request
/// </summary>
public class CustomerUserInfo
{
    /// <summary>
    /// Customer locale (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("locale")]
    public CustomerCheckoutLocale? Locale { get; set; }
}

/// <summary>
/// Payment method information
/// </summary>
public class PaymentMethod
{
    /// <summary>
    /// Payment method type (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("type")]
    public PaymentMethodType? Type { get; set; }

    /// <summary>
    /// Card details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("card")]
    public CardDetails? Card { get; set; }

    /// <summary>
    /// Apple Pay token (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("apple_pay_token")]
    public string? ApplePayToken { get; set; }

    /// <summary>
    /// Google Pay token (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("google_pay_token")]
    public string? GooglePayToken { get; set; }

    /// <summary>
    /// Card token (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("cc_token")]
    public string? CCToken { get; set; }

    /// <summary>
    /// Wallet token (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("wallet_token")]
    public string? WalletToken { get; set; }
}

/// <summary>
/// Card details for payment
/// </summary>
public class CardDetails
{
    /// <summary>
    /// Card number (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("number")]
    public string? Number { get; set; }

    /// <summary>
    /// Card expiration month (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("exp_month")]
    [Range(1, 12)]
    public int? ExpirationMonth { get; set; }

    /// <summary>
    /// Card expiration year (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("exp_year")]
    public int? ExpirationYear { get; set; }

    /// <summary>
    /// Card CVV (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("cvv")]
    public string? CVV { get; set; }

    /// <summary>
    /// Card holder name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("holder_name")]
    public string? HolderName { get; set; }
}

/// <summary>
/// Apple Pay token
/// </summary>
public class ApplePayToken
{
    /// <summary>
    /// Apple Pay token data (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }
}

/// <summary>
/// Google Pay token
/// </summary>
public class GooglePayToken
{
    /// <summary>
    /// Google Pay token data (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }
}

/// <summary>
/// Recipient information for P2P payments
/// </summary>
public class RecipientInfo
{
    /// <summary>
    /// Recipient payment method (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_method")]
    public RecipientPaymentMethod? PaymentMethod { get; set; }

    /// <summary>
    /// Recipient amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Recipient currency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
}

/// <summary>
/// Recipient payment method
/// </summary>
public class RecipientPaymentMethod
{
    /// <summary>
    /// Payment method type (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Card token (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("card_token")]
    public RecipientCardToken? CardToken { get; set; }

    /// <summary>
    /// Card number (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("card_number")]
    public RecipientCardNumber? CardNumber { get; set; }

    /// <summary>
    /// Wallet (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("wallet")]
    public RecipientWallet? Wallet { get; set; }
}

/// <summary>
/// Recipient card token
/// </summary>
public class RecipientCardToken
{
    /// <summary>
    /// Card token (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>
    /// Card CVV (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("cvv")]
    public string? CVV { get; set; }
}

/// <summary>
/// Recipient card number
/// </summary>
public class RecipientCardNumber
{
    /// <summary>
    /// Card number (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("number")]
    public string? Number { get; set; }

    /// <summary>
    /// Card expiration month (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("exp_month")]
    public int? ExpirationMonth { get; set; }

    /// <summary>
    /// Card expiration year (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("exp_year")]
    public int? ExpirationYear { get; set; }
}

/// <summary>
/// Recipient wallet
/// </summary>
public class RecipientWallet
{
    /// <summary>
    /// Wallet ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Wallet provider (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("provider")]
    public string? Provider { get; set; }
}

/// <summary>
/// PayParts configuration
/// </summary>
public class PayPartsConfig
{
    /// <summary>
    /// Number of payment parts (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("parts_count")]
    public int? PartsCount { get; set; }

    /// <summary>
    /// Bank for PayParts (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("bank")]
    public string? Bank { get; set; }

    /// <summary>
    /// Merchant ID for PayParts (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("merchant_id")]
    public string? MerchantId { get; set; }
} 