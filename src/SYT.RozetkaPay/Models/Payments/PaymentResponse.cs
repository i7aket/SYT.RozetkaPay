using System.Text.Json.Serialization;
using SYT.RozetkaPay.Models.Common;

namespace SYT.RozetkaPay.Models.Payments;


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

 