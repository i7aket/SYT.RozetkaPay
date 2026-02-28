using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Models.Common;

namespace SYT.RozetkaPay.Models.Payouts;

// ===================== RECIPIENT MODELS =====================

/// <summary>
/// Base recipient user information (JSON object as per CDN documentation)
/// </summary>
public class RecipientUser
{
    /// <summary>
    /// Recipient email address (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("email")]
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>
    /// Recipient first name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("first_name")]
    [Required]
    [MaxLength(500)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Recipient last name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("last_name")]
    [Required]
    [MaxLength(500)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Recipient middle name/patronym (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("middle_name")]
    [MaxLength(500)]
    public string? MiddleName { get; set; }

    /// <summary>
    /// Recipient phone number (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Recipient ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("rid")]
    public string? Rid { get; set; }

    /// <summary>
    /// Individual taxpayer number (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("ipn")]
    [MaxLength(10)]
    public string? Ipn { get; set; }

    /// <summary>
    /// External recipient ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }
}

/// <summary>
/// Card data for payouts (JSON object as per CDN documentation)
/// </summary>
public class CardData
{
    /// <summary>
    /// Full card number (JSON string as per CDN documentation)
    /// Required if token is empty
    /// </summary>
    [JsonPropertyName("number")]
    public string? Number { get; set; }

    /// <summary>
    /// Card token received from payment operation (JSON string as per CDN documentation)
    /// Required if number is empty
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>
    /// Card identifier in wallet (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("option_id")]
    public string? OptionId { get; set; }
}

/// <summary>
/// Card recipient for payouts (JSON object as per CDN documentation)
/// </summary>
public class CardRecipient : RecipientUser
{
    /// <summary>
    /// Card data for payout (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("card_data")]
    [Required]
    public CardData CardData { get; set; } = new();
}

/// <summary>
/// Cash recipient for payouts (JSON object as per CDN documentation)
/// </summary>
public class CashRecipient : RecipientUser
{
    /// <summary>
    /// Phone number is required for cash recipients (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("phone")]
    [Required]
    public new string Phone { get; set; } = string.Empty;
}

/// <summary>
/// Payout recipient with type discrimination (JSON object as per CDN documentation)
/// </summary>
public class PayoutRecipient
{
    /// <summary>
    /// Payout type (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payout_type")]
    [Required]
    public PayoutType PayoutType { get; set; }

    /// <summary>
    /// Card recipient details (JSON object as per CDN documentation)
    /// Required if payout_type is "card"
    /// </summary>
    [JsonPropertyName("card")]
    public CardRecipient? Card { get; set; }

    /// <summary>
    /// Cash recipient details (JSON object as per CDN documentation)
    /// Required if payout_type is "cash"
    /// </summary>
    [JsonPropertyName("cash")]
    public CashRecipient? Cash { get; set; }
}

/// <summary>
/// Payout type enumeration (JSON string as per CDN documentation)
/// </summary>
public enum PayoutType
{
    /// <summary>
    /// Card payout
    /// </summary>
    [JsonPropertyName("card")]
    Card,

    /// <summary>
    /// Cash payout
    /// </summary>
    [JsonPropertyName("cash")]
    Cash
}

// ===================== CREATE PAYOUT MODELS =====================

/// <summary>
/// Request to create a payout
/// Based on official RozetkaPay CDN documentation: https://cdn.rozetkapay.com/public-docs/index.html#tag/payouts/operation/createPayout
/// </summary>
public class CreatePayoutRequest
{
    /// <summary>
    /// Payout amount in UAH. Standard JSON number format as per CDN documentation.
    /// Use decimal values like 123.45 for 123.45 UAH.
    /// </summary>
    [Required]
    [JsonPropertyName("amount")]
    public required decimal Amount { get; set; }

    /// <summary>
    /// Payout currency
    /// </summary>
    [Required]
    [JsonPropertyName("currency")]
    public required string Currency { get; set; } = "UAH";

    /// <summary>
    /// External payout ID
    /// </summary>
    [Required]
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; set; }

    /// <summary>
    /// Payout description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Recipient information
    /// </summary>
    [Required]
    [JsonPropertyName("recipient")]
    public required PayoutRecipient Recipient { get; set; }

    /// <summary>
    /// Callback URL for payout notifications
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }
}

// ===================== PAYOUT RESPONSE MODELS =====================

/// <summary>
/// Payout response
/// Based on official RozetkaPay CDN documentation: https://cdn.rozetkapay.com/public-docs/index.html
/// </summary>
public class PayoutResponse
{
    /// <summary>
    /// Payout ID
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// External payout ID
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Payout status
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Payout amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Payout currency
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Payout description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Recipient information
    /// </summary>
    [JsonPropertyName("recipient")]
    public PayoutRecipient? Recipient { get; set; }

    /// <summary>
    /// Payout creation timestamp
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Payout processing timestamp
    /// </summary>
    [JsonPropertyName("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Payout error details (if any)
    /// </summary>
    [JsonPropertyName("error")]
    public PayoutError? Error { get; set; }
}

/// <summary>
/// Payout error information
/// </summary>
public class PayoutError
{
    /// <summary>
    /// Error code
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

// ===================== PAYOUT LIST MODELS =====================

/// <summary>
/// Request to get list of payouts
/// </summary>
public class PayoutListRequest
{
    /// <summary>
    /// Start date filter
    /// </summary>
    [JsonPropertyName("date_from")]
    public string? DateFrom { get; set; }

    /// <summary>
    /// End date filter
    /// </summary>
    [JsonPropertyName("date_to")]
    public string? DateTo { get; set; }

    /// <summary>
    /// Status filter
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Limit number of results
    /// </summary>
    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    /// <summary>
    /// Offset for pagination
    /// </summary>
    [JsonPropertyName("offset")]
    public int? Offset { get; set; }
}

/// <summary>
/// Response with list of payouts
/// </summary>
public class PayoutListResponse
{
    /// <summary>
    /// List of payouts
    /// </summary>
    [JsonPropertyName("payouts")]
    public List<PayoutResponse>? Payouts { get; set; }

    /// <summary>
    /// Total number of payouts
    /// </summary>
    [JsonPropertyName("total")]
    public int? Total { get; set; }

    /// <summary>
    /// Number of payouts in current response
    /// </summary>
    [JsonPropertyName("count")]
    public int? Count { get; set; }
}

// ===================== BALANCE MODELS =====================

/// <summary>
/// Balance response
/// </summary>
public class BalanceResponse
{
    /// <summary>
    /// List of currency balances
    /// </summary>
    [JsonPropertyName("balances")]
    public List<CurrencyBalance>? Balances { get; set; }

    /// <summary>
    /// Total balance amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("total_balance")]
    public decimal? TotalBalance { get; set; }

    /// <summary>
    /// Base currency for total balance
    /// </summary>
    [JsonPropertyName("base_currency")]
    public string? BaseCurrency { get; set; }
}

/// <summary>
/// Currency balance information
/// </summary>
public class CurrencyBalance
{
    /// <summary>
    /// Currency code
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Available balance amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("available")]
    public decimal? Available { get; set; }

    /// <summary>
    /// Pending balance amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("pending")]
    public decimal? Pending { get; set; }

    /// <summary>
    /// Reserved balance amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("reserved")]
    public decimal? Reserved { get; set; }
}

/// <summary>
/// Payout balance response
/// </summary>
public class PayoutBalanceResponse
{
    /// <summary>
    /// Available balance amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Balance currency
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
}

// ===================== ADDITIONAL PAYOUT MODELS =====================

/// <summary>
/// Request payout order details (JSON object as per CDN documentation)
/// </summary>
public class PayoutOrderDetails
{
    /// <summary>
    /// Callback URL for notifications (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Payment currency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("currency")]
    [Required]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Order description (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("description")]
    [Required]
    [MaxLength(500)]
    [MinLength(1)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// External order ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    [Required]
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Original amount (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("original_amount")]
    [Required]
    public string OriginalAmount { get; set; } = string.Empty;
}

/// <summary>
/// Payout payer details (JSON object as per CDN documentation)
/// </summary>
public class PayoutPayer
{
    /// <summary>
    /// Entity ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("entity_id")]
    [Required]
    public string EntityId { get; set; } = string.Empty;
}

/// <summary>
/// Request to create a payout with complete order structure (JSON object as per CDN documentation)
/// </summary>
public class RequestPayoutRequest
{
    /// <summary>
    /// Order details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("order")]
    [Required]
    public PayoutOrderDetails Order { get; set; } = new();

    /// <summary>
    /// Payer information (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payer")]
    [Required]
    public PayoutPayer Payer { get; set; } = new();

    /// <summary>
    /// Recipient information (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("recipient")]
    [Required]
    public PayoutRecipient Recipient { get; set; } = new();
}

/// <summary>
/// Request to resend payout callback (JSON object as per CDN documentation)
/// </summary>
public class ResendPayoutCallbackRequest
{
    /// <summary>
    /// External payout ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    [Required]
    public string ExternalId { get; set; } = string.Empty;
}

/// <summary>
/// Response for payout callback resend
/// </summary>
public class PayoutCallbackResendResponse
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
    public PayoutCallbackResendResponse()
    {
        Status = "success";
        Message = "Callback sent successfully";
    }
}

/// <summary>
/// Request to cancel cash payout (JSON object as per CDN documentation)
/// </summary>
public class CancelCashPayoutRequest
{
    /// <summary>
    /// External payout ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    [Required]
    public string ExternalId { get; set; } = string.Empty;
}

/// <summary>
/// Payer account details (JSON object as per CDN documentation)
/// </summary>
public class PayerAccount
{
    /// <summary>
    /// Entity ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("entity_id")]
    [Required]
    public string EntityId { get; set; } = string.Empty;
}

/// <summary>
/// Payout transaction result (JSON object as per CDN documentation)
/// </summary>
public class PayoutTransactionResult
{
    /// <summary>
    /// Payment currency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Transaction description (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// External transaction ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Financial channel ID (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("fc_id")]
    public int? FcId { get; set; }

    /// <summary>
    /// Original amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("original_amount")]
    public decimal? OriginalAmount { get; set; }

    /// <summary>
    /// Partner key ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("partner_key_id")]
    public string? PartnerKeyId { get; set; }

    /// <summary>
    /// Payer account information (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payer_account")]
    public PayerAccount? PayerAccount { get; set; }

    /// <summary>
    /// Payer amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payer_amount")]
    public decimal? PayerAmount { get; set; }

    /// <summary>
    /// Payer outer fee (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payer_outer_fee")]
    public decimal? PayerOuterFee { get; set; }

    /// <summary>
    /// Payment type (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_type")]
    public string? PaymentType { get; set; }

    /// <summary>
    /// Payout type (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payout_type")]
    public string? PayoutType { get; set; }

    /// <summary>
    /// Recipient user information (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("recipient_user")]
    public RecipientUser? RecipientUser { get; set; }

    /// <summary>
    /// Operation status (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public OperationStatus? Status { get; set; }

    /// <summary>
    /// Status code (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status_code")]
    public string? StatusCode { get; set; }

    /// <summary>
    /// Status code description (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status_code_description")]
    public string? StatusCodeDescription { get; set; }

    /// <summary>
    /// Transaction UUID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }
} 
