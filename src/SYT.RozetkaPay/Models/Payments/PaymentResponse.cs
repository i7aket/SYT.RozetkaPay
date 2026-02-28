using System.Text.Json.Serialization;
using SYT.RozetkaPay.Models.Common;

namespace SYT.RozetkaPay.Models.Payments;

/// <summary>
/// Response from payment operations
/// </summary>
public class PaymentResponse
{
    /// <summary>
    /// Payment ID assigned by RozetkaPay
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// External ID provided in the request
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Current payment status
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Status code
    /// </summary>
    [JsonPropertyName("status_code")]
    public int? StatusCode { get; set; }

    /// <summary>
    /// Status description
    /// </summary>
    [JsonPropertyName("status_description")]
    public string? StatusDescription { get; set; }

    /// <summary>
    /// Payment amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Amount of canceled funds (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount_canceled")]
    public decimal? AmountCanceled { get; set; }

    /// <summary>
    /// Amount of confirmed funds in the 2-step acquiring flow (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount_confirmed")]
    public decimal? AmountConfirmed { get; set; }

    /// <summary>
    /// Amount of refunded funds (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount_refunded")]
    public decimal? AmountRefunded { get; set; }

    /// <summary>
    /// Payment currency code
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Payment description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Payment mode
    /// </summary>
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    /// <summary>
    /// Whether payment was confirmed immediately
    /// </summary>
    [JsonPropertyName("confirm")]
    public bool? Confirm { get; set; }

    /// <summary>
    /// A boolean flag which indicates if payment was successful
    /// </summary>
    [JsonPropertyName("purchased")]
    public bool? Purchased { get; set; }

    /// <summary>
    /// A boolean flag which indicates if payment was canceled
    /// </summary>
    [JsonPropertyName("canceled")]
    public bool? Canceled { get; set; }

    /// <summary>
    /// A boolean flag for the 2-step acquiring flow which indicates if payment was confirmed
    /// </summary>
    [JsonPropertyName("confirmed")]
    public bool? Confirmed { get; set; }

    /// <summary>
    /// A boolean flag which indicates if payment was refunded
    /// </summary>
    [JsonPropertyName("refunded")]
    public bool? Refunded { get; set; }

    /// <summary>
    /// A boolean flag which indicates if action from the customer is required
    /// </summary>
    [JsonPropertyName("action_required")]
    public bool? ActionRequired { get; set; }

    /// <summary>
    /// Date when transaction was created (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Last update date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Checkout URL for customer (if mode is 'hosted')
    /// </summary>
    [JsonPropertyName("checkout_url")]
    public string? CheckoutUrl { get; set; }

    /// <summary>
    /// Required user action (if any)
    /// </summary>
    [JsonPropertyName("action")]
    public UserAction? Action { get; set; }

    /// <summary>
    /// QR code URL for QR payments
    /// </summary>
    [JsonPropertyName("qr_code_url")]
    public string? QrCodeUrl { get; set; }

    /// <summary>
    /// Link to the receipt for user
    /// </summary>
    [JsonPropertyName("receipt_url")]
    public string? ReceiptUrl { get; set; }

    /// <summary>
    /// Customer information
    /// </summary>
    [JsonPropertyName("customer")]
    public PaymentCustomer? Customer { get; set; }

    /// <summary>
    /// Payment method information
    /// </summary>
    [JsonPropertyName("payment_method")]
    public PaymentMethodInfo? PaymentMethod { get; set; }

    /// <summary>
    /// Card information (if applicable)
    /// </summary>
    [JsonPropertyName("card")]
    public CardInfo? Card { get; set; }

    /// <summary>
    /// 3DS information (if applicable)
    /// </summary>
    [JsonPropertyName("three_ds")]
    public ThreeDsInfo? ThreeDs { get; set; }

    /// <summary>
    /// Authorization code
    /// </summary>
    [JsonPropertyName("auth_code")]
    public string? AuthCode { get; set; }

    /// <summary>
    /// Reference Retrieval Number
    /// </summary>
    [JsonPropertyName("rrn")]
    public string? Rrn { get; set; }

    /// <summary>
    /// Transaction ID
    /// </summary>
    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }

    /// <summary>
    /// Unified external id
    /// </summary>
    [JsonPropertyName("unified_external_id")]
    public string? UnifiedExternalId { get; set; }

    /// <summary>
    /// External identifier from the merchant, one per batch.
    /// </summary>
    [JsonPropertyName("batch_external_id")]
    public string? BatchExternalId { get; set; }

    /// <summary>
    /// Recurrent token (if recurrent payment was initialized)
    /// </summary>
    [JsonPropertyName("recurrent_token")]
    public string? RecurrentToken { get; set; }

    /// <summary>
    /// Recurrent ID
    /// </summary>
    [JsonPropertyName("recurrent_id")]
    public string? RecurrentId { get; set; }

    /// <summary>
    /// Payment processing timestamp (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Error information (if payment failed)
    /// </summary>
    [JsonPropertyName("error")]
    public PaymentError? Error { get; set; }

    /// <summary>
    /// Actions available for this payment
    /// </summary>
    [JsonPropertyName("actions")]
    public List<string>? Actions { get; set; }

    /// <summary>
    /// Callback information
    /// </summary>
    [JsonPropertyName("callback")]
    public CallbackInfo? Callback { get; set; }

    /// <summary>
    /// Payment method details
    /// </summary>
    [JsonPropertyName("details")]
    public PaymentDetails? Details { get; set; }

    /// <summary>
    /// List of primary transactions
    /// </summary>
    [JsonPropertyName("purchase_details")]
    public List<TransactionDetails>? PurchaseDetails { get; set; }

    /// <summary>
    /// List of confirmation transactions in the 2-step acquiring flow
    /// </summary>
    [JsonPropertyName("confirmation_details")]
    public List<TransactionDetails>? ConfirmationDetails { get; set; }

    /// <summary>
    /// List of cancellation transactions
    /// </summary>
    [JsonPropertyName("cancellation_details")]
    public List<TransactionDetails>? CancellationDetails { get; set; }

    /// <summary>
    /// List of refund transactions
    /// </summary>
    [JsonPropertyName("refund_details")]
    public List<TransactionDetails>? RefundDetails { get; set; }
}

/// <summary>
/// Customer information in payment response
/// </summary>
public class PaymentCustomer
{
    /// <summary>
    /// Customer email
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Customer first name
    /// </summary>
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    /// <summary>
    /// Customer last name
    /// </summary>
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    /// <summary>
    /// Customer patronym
    /// </summary>
    [JsonPropertyName("patronym")]
    public string? Patronym { get; set; }

    /// <summary>
    /// Customer phone
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Customer IP address
    /// </summary>
    [JsonPropertyName("ip")]
    public string? Ip { get; set; }
}

/// <summary>
/// Payment method information in response
/// </summary>
public class PaymentMethodInfo
{
    /// <summary>
    /// Payment method type
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Payment method title
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Payment system
    /// </summary>
    [JsonPropertyName("payment_system")]
    public string? PaymentSystem { get; set; }
}

/// <summary>
/// Card information in payment response
/// </summary>
public class CardInfo
{
    /// <summary>
    /// Masked card number
    /// </summary>
    [JsonPropertyName("mask")]
    public string? Mask { get; set; }

    /// <summary>
    /// Card BIN (first 6 digits)
    /// </summary>
    [JsonPropertyName("bin")]
    public string? Bin { get; set; }

    /// <summary>
    /// Card payment system (Visa, MasterCard, etc.)
    /// </summary>
    [JsonPropertyName("payment_system")]
    public string? PaymentSystem { get; set; }

    /// <summary>
    /// Card type (debit, credit)
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Issuing bank name
    /// </summary>
    [JsonPropertyName("bank_name")]
    public string? BankName { get; set; }

    /// <summary>
    /// Card country code
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>
    /// Card token (if tokenization was requested)
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}

/// <summary>
/// 3DS authentication information
/// </summary>
public class ThreeDsInfo
{
    /// <summary>
    /// 3DS version
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// ACS URL for 3DS authentication
    /// </summary>
    [JsonPropertyName("acs_url")]
    public string? AcsUrl { get; set; }

    /// <summary>
    /// PAReq for 3DS v1
    /// </summary>
    [JsonPropertyName("pareq")]
    public string? PaReq { get; set; }

    /// <summary>
    /// TermUrl for 3DS v1
    /// </summary>
    [JsonPropertyName("term_url")]
    public string? TermUrl { get; set; }

    /// <summary>
    /// Challenge request for 3DS v2
    /// </summary>
    [JsonPropertyName("creq")]
    public string? CReq { get; set; }

    /// <summary>
    /// Authentication status
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

/// <summary>
/// Payment error information
/// </summary>
public class PaymentError
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

    /// <summary>
    /// Detailed error description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Error source
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }
}

/// <summary>
/// Callback information
/// </summary>
public class CallbackInfo
{
    /// <summary>
    /// Callback URL
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Callback status
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Number of callback attempts
    /// </summary>
    [JsonPropertyName("attempts")]
    public int? Attempts { get; set; }

    /// <summary>
    /// Last callback attempt timestamp
    /// </summary>
    [JsonPropertyName("last_attempt_at")]
    public DateTime? LastAttemptAt { get; set; }

    /// <summary>
    /// Next callback attempt timestamp
    /// </summary>
    [JsonPropertyName("next_attempt_at")]
    public DateTime? NextAttemptAt { get; set; }
}

/// <summary>
/// Transaction details information
/// </summary>
public class TransactionDetails
{
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
    /// Transaction ID
    /// </summary>
    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }

    /// <summary>
    /// Date when transaction was created (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Date when transaction was processed (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Authorization code
    /// </summary>
    [JsonPropertyName("auth_code")]
    public string? AuthCode { get; set; }

    /// <summary>
    /// Reference Retrieval Number
    /// </summary>
    [JsonPropertyName("rrn")]
    public string? Rrn { get; set; }
}

/// <summary>
/// Payment details
/// </summary>
public class PaymentDetails
{
    /// <summary>
    /// Payment amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Payment currency
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Payment method details
    /// </summary>
    [JsonPropertyName("payment_method")]
    public string? PaymentMethod { get; set; }
} 