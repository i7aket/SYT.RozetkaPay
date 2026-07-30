using System.Text.Json.Serialization;
using SYT.RozetkaPay.Models.Common;

namespace SYT.RozetkaPay.Models.Payments;


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

    /// <summary>
    /// Provider field &lt;c&gt;bank_name&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("bank_name")]
    public string? BankName { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;billing_order_id&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("billing_order_id")]
    public string? BillingOrderId { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;description&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;fee&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("fee")]
    public FeeDetails? Fee { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;fiscalization&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("fiscalization")]
    public Fiscalization? Fiscalization { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;gateway_order_id&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("gateway_order_id")]
    public string? GatewayOrderId { get; set; }
    /// <summary>
    /// Method name called on payment flow provider side.
    /// </summary>
    [JsonPropertyName("method")]
    public string? Method { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;mid&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("mid")]
    public string? Mid { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;operation_id&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("operation_id")]
    public string? OperationId { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;payload&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;payment_id&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("payment_id")]
    public string? PaymentId { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;recipient_iban&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("recipient_iban")]
    public string? RecipientIban { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;recurrent_id&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("recurrent_id")]
    public string? RecurrentId { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;status_code&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("status_code")]
    public ResponseCode? StatusCode { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;status_description&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("status_description")]
    public string? StatusDescription { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;subscription_id&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("subscription_id")]
    public string? SubscriptionId { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;terminal_name&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("terminal_name")]
    public string? TerminalName { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;tid&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("tid")]
    public string? Tid { get; set; }
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