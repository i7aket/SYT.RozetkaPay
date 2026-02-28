using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Converters;
using SYT.RozetkaPay.Models.Common;

namespace SYT.RozetkaPay.Models.Payments;

// ===================== RECURRENT PAYMENT MODELS =====================

/// <summary>
/// Request to create a recurrent payment
/// </summary>
public class CreateRecurrentPaymentRequest
{
    /// <summary>
    /// Payment amount in UAH
    /// </summary>
    [Required]
    [JsonPropertyName("amount")]
    public required decimal Amount { get; set; }

    /// <summary>
    /// Payment currency
    /// </summary>
    [Required]
    [JsonPropertyName("currency")]
    public required string Currency { get; set; }

    /// <summary>
    /// Unique external ID for the payment
    /// </summary>
    [Required]
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; set; }

    /// <summary>
    /// Recurrent ID from previous payment
    /// </summary>
    [Required]
    [JsonPropertyName("recurrent_id")]
    public required string RecurrentId { get; set; }

    /// <summary>
    /// Payment description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Callback URL for payment notifications
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Customer information
    /// </summary>
    [JsonPropertyName("customer")]
    public CustomerInfo? Customer { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

// ===================== CONFIRM PAYMENT MODELS =====================

/// <summary>
/// Request to confirm a payment
/// </summary>
public class ConfirmPaymentRequest
{
    /// <summary>
    /// External ID of the payment to confirm
    /// </summary>
    [Required]
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; set; }

    /// <summary>
    /// Amount to confirm in UAH (optional, confirms full amount if not specified)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }
}

// ===================== CANCEL PAYMENT MODELS =====================

/// <summary>
/// Request to cancel a payment
/// </summary>
public class CancelPaymentRequest
{
    /// <summary>
    /// External ID of the payment to cancel
    /// </summary>
    [Required]
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; set; }

    /// <summary>
    /// Cancellation reason
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

// ===================== REFUND PAYMENT MODELS =====================

/// <summary>
/// Request to refund a payment
/// </summary>
public class RefundPaymentRequest
{
    /// <summary>
    /// External ID of the original payment
    /// </summary>
    [Required]
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; set; }

    /// <summary>
    /// Amount to refund in UAH (optional, refunds full amount if not specified)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Refund reason
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// External refund ID
    /// </summary>
    [JsonPropertyName("external_refund_id")]
    public string? ExternalRefundId { get; set; }

    /// <summary>
    /// Callback URL for refund notifications
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }
}

/// <summary>
/// Request to retry a pending refund operation
/// </summary>
public class RetryRefundRequest
{
    /// <summary>
    /// External ID of the original payment
    /// </summary>
    [Required]
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; set; }
}

/// <summary>
/// Request to cancel a pending refund operation
/// </summary>
public class CancelRefundRequest
{
    /// <summary>
    /// External ID of the original payment
    /// </summary>
    [Required]
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; set; }
}

// ===================== PAYMENT LIST MODELS =====================

/// <summary>
/// Request parameters for payment list
/// </summary>
public class PaymentListRequest
{
    /// <summary>
    /// Start date filter
    /// </summary>
    [JsonPropertyName("date_from")]
    public DateTime? DateFrom { get; set; }

    /// <summary>
    /// End date filter
    /// </summary>
    [JsonPropertyName("date_to")]
    public DateTime? DateTo { get; set; }

    /// <summary>
    /// Payment status filter
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Number of payments to return
    /// </summary>
    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    /// <summary>
    /// Number of payments to skip
    /// </summary>
    [JsonPropertyName("offset")]
    public int? Offset { get; set; }
}

/// <summary>
/// Response containing list of payments
/// </summary>
public class PaymentListResponse
{
    /// <summary>
    /// List of payments
    /// </summary>
    [JsonPropertyName("payments")]
    public List<PaymentResponse>? Payments { get; set; }

    /// <summary>
    /// Total number of payments
    /// </summary>
    [JsonPropertyName("count")]
    public int? Count { get; set; }

    /// <summary>
    /// Offset for pagination
    /// </summary>
    [JsonPropertyName("offset")]
    public int? Offset { get; set; }
}

// ===================== PAYMENT RECEIPT MODELS =====================

/// <summary>
/// Payment receipt response
/// </summary>
public class PaymentReceiptResponse
{
    /// <summary>
    /// Receipt URL
    /// </summary>
    [JsonPropertyName("receipt_url")]
    public string? ReceiptUrl { get; set; }

    /// <summary>
    /// Receipt PDF data
    /// </summary>
    [JsonPropertyName("receipt_pdf")]
    public string? ReceiptPdf { get; set; }

    /// <summary>
    /// Receipt HTML data
    /// </summary>
    [JsonPropertyName("receipt_html")]
    public string? ReceiptHtml { get; set; }
}

// ===================== CARD LOOKUP MODELS =====================

/// <summary>
/// Card lookup request
/// </summary>
public class CardLookupRequest
{
    /// <summary>
    /// Card number to lookup
    /// </summary>
    [Required]
    [JsonPropertyName("card_number")]
    public required string CardNumber { get; set; }
}

/// <summary>
/// Card lookup response
/// </summary>
public class CardLookupResponse
{
    /// <summary>
    /// Bank Identification Number
    /// </summary>
    [JsonPropertyName("bin")]
    public string? Bin { get; set; }

    /// <summary>
    /// Available payment methods
    /// </summary>
    [JsonPropertyName("payment_methods")]
    public List<string>? PaymentMethods { get; set; }
}

/// <summary>
/// BIN information
/// </summary>
public class BinInfo
{
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
    /// Country code
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>
    /// Country name
    /// </summary>
    [JsonPropertyName("country_name")]
    public string? CountryName { get; set; }
}

// ===================== CALLBACK RESEND MODELS =====================

/// <summary>
/// Request to resend callback
/// </summary>
public class ResendCallbackRequest
{
    /// <summary>
    /// External payment ID
    /// </summary>
    [Required]
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; set; }

    /// <summary>
    /// New callback URL (optional)
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }
}

/// <summary>
/// Callback resend response
/// </summary>
public class CallbackResendResponse
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

    /// <summary>
    /// Default constructor for 204 No Content responses
    /// </summary>
    public CallbackResendResponse()
    {
        Status = "success";
        Message = "Callback sent successfully";
    }
}

/// <summary>
/// P2P confirmation request
/// </summary>
public class P2PConfirmationRequest
{
    /// <summary>
    /// External ID of the P2P payment to confirm
    /// </summary>
    [Required]
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; set; }

    /// <summary>
    /// P2P confirmation amount in UAH
    /// </summary>
    [Required]
    [JsonPropertyName("amount")]
    public required decimal Amount { get; set; }

    /// <summary>
    /// Confirmation description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Callback URL for confirmation notifications
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }
}

/// <summary>
/// Request for creating a lookup payment
/// </summary>
public class CreateLookupRequest
{
    /// <summary>
    /// External ID for lookup (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    [Required]
    public string ExternalId { get; set; } = string.Empty;
}

/// <summary>
/// Customer request payment method details
/// </summary>
public class CustomerRequestPaymentMethod
{
    /// <summary>
    /// Payment method type (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Card details for card payments (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("cc")]
    public CustomerCCRequestPaymentMethod? Card { get; set; }

    /// <summary>
    /// Card token details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("cc_token")]
    public CustomerCCTokenRequestPaymentMethod? CCToken { get; set; }

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
    /// Wallet token (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("wallet_token")]
    public string? WalletToken { get; set; }
}

/// <summary>
/// Customer credit card request payment method
/// </summary>
public class CustomerCCRequestPaymentMethod
{
    /// <summary>
    /// Card CVV (JSON string as per API validation)
    /// </summary>
    [JsonPropertyName("cvv")]
    [Required]
    public string CVV { get; set; } = string.Empty;

    /// <summary>
    /// Card expiration month (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("exp_month")]
    [Required]
    [Range(1, 12)]
    public int ExpirationMonth { get; set; }

    /// <summary>
    /// Card expiration year (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("exp_year")]
    [Required]
    public int ExpirationYear { get; set; }

    /// <summary>
    /// Card number (JSON string as per API validation)
    /// </summary>
    [JsonPropertyName("number")]
    [Required]
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use 3DS flow (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("use_3ds_flow")]
    public bool? Use3DSFlow { get; set; }
}

/// <summary>
/// Customer credit card token request payment method
/// </summary>
public class CustomerCCTokenRequestPaymentMethod
{
    /// <summary>
    /// Card token (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("token")]
    [Required]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use 3DS flow (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("use_3ds_flow")]
    public bool? Use3DSFlow { get; set; }

    /// <summary>
    /// Whether to save card to wallet (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("save_to_wallet")]
    public bool SaveToWallet { get; set; } = false;
}

/// <summary>
/// Customer Apple/Google Pay request payment method
/// </summary>
public class CustomerAppleGooglePayRequestPaymentMethod
{
    /// <summary>
    /// Payment token (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("token")]
    [Required]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use 3DS flow (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("use_3ds_flow")]
    public bool? Use3DSFlow { get; set; }
}

/// <summary>
/// Customer wallet request payment method (JSON object as per CDN documentation)
/// </summary>
public class CustomerWalletRequestPaymentMethod
{
    /// <summary>
    /// Wallet option ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("option_id")]
    [Required]
    public string OptionId { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use 3DS flow (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("use_3ds_flow")]
    public bool? Use3DSFlow { get; set; }
}

/// <summary>
/// Customer request user details
/// </summary>
public class CustomerRequestUserDetails : BaseRequestUserDetails
{
    /// <summary>
    /// Customer payment method (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_method")]
    [Required]
    public CustomerRequestPaymentMethod PaymentMethod { get; set; } = new();

    /// <summary>
    /// Customer IP address (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Customer account number (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("account_number")]
    public string? AccountNumber { get; set; }

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
    /// Browser fingerprint (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("fingerprint")]
    public BrowserFingerprint? Fingerprint { get; set; }
}

/// <summary>
/// Customer address information
/// </summary>
public class CustomerAddress
{
    /// <summary>
    /// Country (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>
    /// City (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>
    /// Street address (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("street")]
    public string? Street { get; set; }

    /// <summary>
    /// Postal code (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("postal_code")]
    public string? PostalCode { get; set; }
}

/// <summary>
/// Recipient request user details
/// </summary>
public class RecipientRequestUserDetails : BaseRequestUserDetails
{
    /// <summary>
    /// Recipient payment method (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_method")]
    [Required]
    public RecipientRequestPaymentMethod PaymentMethod { get; set; } = new();

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
/// Recipient request payment method
/// </summary>
public class RecipientRequestPaymentMethod
{
    /// <summary>
    /// Payment method type (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("type")]
    [Required]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Card token details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("card_token")]
    public RecipientCCTokenRequestPaymentMethod? CardToken { get; set; }

    /// <summary>
    /// Card number details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("card_number")]
    public RecipientCCNumberRequestPaymentMethod? CardNumber { get; set; }

    /// <summary>
    /// Wallet details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("wallet")]
    public RecipientWalletRequestPaymentMethod? Wallet { get; set; }
}

/// <summary>
/// Recipient credit card token request payment method
/// </summary>
public class RecipientCCTokenRequestPaymentMethod
{
    /// <summary>
    /// Card token (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("token")]
    [Required]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Card CVV (JSON string as per API validation)
    /// </summary>
    [JsonPropertyName("cvv")]
    public string? CVV { get; set; }
}

/// <summary>
/// Recipient credit card number request payment method
/// </summary>
public class RecipientCCNumberRequestPaymentMethod
{
    /// <summary>
    /// Card number (JSON string as per API validation)
    /// </summary>
    [JsonPropertyName("number")]
    [Required]
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Card expiration month (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("exp_month")]
    [Required]
    [Range(1, 12)]
    public int ExpirationMonth { get; set; }

    /// <summary>
    /// Card expiration year (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("exp_year")]
    [Required]
    public int ExpirationYear { get; set; }
}

/// <summary>
/// Recipient wallet request payment method
/// </summary>
public class RecipientWalletRequestPaymentMethod
{
    /// <summary>
    /// Wallet option ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("option_id")]
    [Required]
    public string OptionId { get; set; } = string.Empty;
}

/// <summary>
/// Result payment method information
/// </summary>
public class ResultPaymentMethod
{
    /// <summary>
    /// Payment method type (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Payment method title (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Payment system (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_system")]
    public string? PaymentSystem { get; set; }
}

/// <summary>
/// Result user details
/// </summary>
public class ResultUserDetails : UserInfo
{
    /// <summary>
    /// Payment method used (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_method")]
    public ResultPaymentMethod? PaymentMethod { get; set; }
}

/// <summary>
/// Payment operation result
/// </summary>
public class PaymentOperationResult
{
    /// <summary>
    /// Internal payment ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// External payment ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Unified external ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("unified_external_id")]
    public string? UnifiedExternalId { get; set; }

    /// <summary>
    /// Whether operation was successful (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("is_success")]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Transaction details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("details")]
    public TransactionDetails? Details { get; set; }

    /// <summary>
    /// Whether action is required from customer (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("action_required")]
    public bool ActionRequired { get; set; }

    /// <summary>
    /// Required user action (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("action")]
    public UserAction? Action { get; set; }

    /// <summary>
    /// Receipt URL (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("receipt_url")]
    public string? ReceiptUrl { get; set; }

    /// <summary>
    /// Payment method information (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_method")]
    public ResultPaymentMethod? PaymentMethod { get; set; }

    /// <summary>
    /// Customer details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("customer")]
    public ResultUserDetails? Customer { get; set; }
}

/// <summary>
/// Payment status result
/// </summary>
public class PaymentStatusResult
{
    /// <summary>
    /// Payment ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// External payment ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Payment status (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Payment amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Payment currency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Payment creation date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("created_at")]
    [JsonConverter(typeof(FlexibleDateTimeConverter))]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Payment processing date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("processed_at")]
    [JsonConverter(typeof(NullableFlexibleDateTimeConverter))]
    public DateTime? ProcessedAt { get; set; }
}

/// <summary>
/// Payment receipt result
/// </summary>
public class PaymentReceiptResult
{
    /// <summary>
    /// Receipt data (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("receipt")]
    public string? Receipt { get; set; }

    /// <summary>
    /// Receipt URL (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("receipt_url")]
    public string? ReceiptUrl { get; set; }
}

/// <summary>
/// Payment search result item
/// </summary>
public class PaymentSearchResult
{
    /// <summary>
    /// Payment ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// External payment ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Payment status (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Payment amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Payment currency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Payment creation date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("created_at")]
    [JsonConverter(typeof(FlexibleDateTimeConverter))]
    public DateTime? CreatedAt { get; set; }
}

/// <summary>
/// Payment search list result
/// </summary>
public class PaymentSearchList
{
    /// <summary>
    /// List of payments (JSON array as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payments")]
    public List<PaymentSearchResult>? Payments { get; set; }

    /// <summary>
    /// Total count of payments (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("total")]
    public int? Total { get; set; }

    /// <summary>
    /// Current count in response (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("count")]
    public int? Count { get; set; }
}

/// <summary>
/// Development environment payment request (JSON object as per CDN documentation)
/// This is similar to CreatePaymentRequest but with additional development-specific fields
/// </summary>
public class CreatePaymentRequestDev
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
    /// Unified external ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("unified_external_id")]
    public string? UnifiedExternalId { get; set; }

    /// <summary>
    /// Additional payload data (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    /// <summary>
    /// Whether to initialize recurrent payment (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("init_recurrent_payment")]
    public bool? InitRecurrentPayment { get; set; }

    /// <summary>
    /// Customer information (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("customer")]
    public CustomerRequestUserDetails? Customer { get; set; }

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
    /// Checkout TTL in minutes for express checkout (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("checkout_ttl")]
    public decimal? CheckoutTtl { get; set; }
}

/// <summary>
/// Apple Pay response payment method (JSON object as per CDN documentation)
/// </summary>
public class ApplePayResponsePaymentMethod
{
    /// <summary>
    /// Bank short name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("bank_short_name")]
    public string? BankShortName { get; set; }

    /// <summary>
    /// Card expiration date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("expires_at")]
    [JsonConverter(typeof(NullableFlexibleDateTimeConverter))]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Card mask (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("mask")]
    public string? Mask { get; set; }

    /// <summary>
    /// Payment system (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_system")]
    public string? PaymentSystem { get; set; }

    /// <summary>
    /// Payment token (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}

/// <summary>
/// Google Pay response payment method (JSON object as per CDN documentation)
/// </summary>
public class GooglePayResponsePaymentMethod
{
    /// <summary>
    /// Bank short name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("bank_short_name")]
    public string? BankShortName { get; set; }

    /// <summary>
    /// Card expiration date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("expires_at")]
    [JsonConverter(typeof(NullableFlexibleDateTimeConverter))]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Card mask (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("mask")]
    public string? Mask { get; set; }

    /// <summary>
    /// Payment system (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_system")]
    public string? PaymentSystem { get; set; }

    /// <summary>
    /// Payment token (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}

/// <summary>
/// Credit card token response payment method (JSON object as per CDN documentation)
/// </summary>
public class CCTokenResponsePaymentMethod
{
    /// <summary>
    /// Bank short name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("bank_short_name")]
    public string? BankShortName { get; set; }

    /// <summary>
    /// Card expiration date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("expires_at")]
    [JsonConverter(typeof(NullableFlexibleDateTimeConverter))]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Card mask (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("mask")]
    public string? Mask { get; set; }

    /// <summary>
    /// Payment system (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_system")]
    public string? PaymentSystem { get; set; }

    /// <summary>
    /// Payment token (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}

/// <summary>
/// Partner details (JSON object as per CDN documentation)
/// </summary>
public class PartnerDetails
{
    /// <summary>
    /// Partner transaction ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }
}

/// <summary>
/// Express checkout delivery details (JSON object as per CDN documentation)
/// </summary>
public class ExpressCheckoutDeliveryDetails
{
    /// <summary>
    /// Apartment number (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("apartment")]
    public string? Apartment { get; set; }

    /// <summary>
    /// City name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>
    /// Delivery type (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("delivery_type")]
    public string? DeliveryType { get; set; }

    /// <summary>
    /// House number (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("house")]
    public string? House { get; set; }

    /// <summary>
    /// Delivery provider (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    /// <summary>
    /// Street name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("street")]
    public string? Street { get; set; }

    /// <summary>
    /// Warehouse number (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("warehouse_number")]
    public string? WarehouseNumber { get; set; }
}

/// <summary>
/// Express checkout recipient details (JSON object as per CDN documentation)
/// </summary>
public class ExpressCheckoutRecipient
{
    /// <summary>
    /// Recipient phone number (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Recipient first name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Recipient last name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    /// <summary>
    /// Recipient middle name/patronym (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("middle_name")]
    public string? MiddleName { get; set; }
}

/// <summary>
/// Express checkout order details (JSON object as per CDN documentation)
/// </summary>
public class ExpressCheckoutOrderDetails
{
    /// <summary>
    /// Delivery details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("delivery_details")]
    public ExpressCheckoutDeliveryDetails? DeliveryDetails { get; set; }

    /// <summary>
    /// Order recipient (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("order_recipient")]
    public ExpressCheckoutRecipient? OrderRecipient { get; set; }
}

/// <summary>
/// Wallet item details (JSON object as per CDN documentation)
/// </summary>
public class WalletItem
{
    /// <summary>
    /// Card details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("card")]
    public CardItemDetails? Card { get; set; }

    /// <summary>
    /// Wallet option ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("option_id")]
    public string? OptionId { get; set; }

    /// <summary>
    /// Whether this is the default payment method (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("is_default")]
    public bool? IsDefault { get; set; }

    /// <summary>
    /// Optional card name set by user (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Payment method type (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

/// <summary>
/// Card item details (JSON object as per CDN documentation)
/// </summary>
public class CardItemDetails
{
    /// <summary>
    /// Masked card number (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("mask")]
    public string? Mask { get; set; }

    /// <summary>
    /// Card brand/payment system (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    /// <summary>
    /// Card expiration date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("expires_at")]
    [JsonConverter(typeof(NullableFlexibleDateTimeConverter))]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Bank short name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("bank_short_name")]
    public string? BankShortName { get; set; }
}

/// <summary>
/// Get payment status response (OpenAPI schema)
/// </summary>
public class GetPaymentStatus
{
    /// <summary>
    /// Card details
    /// </summary>
    [JsonPropertyName("card")]
    public CardItemDetails? Card { get; set; }

    /// <summary>
    /// ID of added card. Can be used for payments with wallet payment type
    /// </summary>
    [JsonPropertyName("option_id")]
    public string? OptionId { get; set; }

    /// <summary>
    /// Whether payment is confirmed
    /// </summary>
    [JsonPropertyName("is_confirmed")]
    public bool IsConfirmed { get; set; }

    /// <summary>
    /// Payment type (always "card")
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "card";

    /// <summary>
    /// Operation status
    /// </summary>
    [JsonPropertyName("status")]
    public OperationStatus Status { get; set; }
}
