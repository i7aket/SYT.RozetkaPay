using System.Text.Json.Serialization;
using SYT.RozetkaPay.Converters;
using SYT.RozetkaPay.Models.Common;

namespace SYT.RozetkaPay.Models.Payments;

/// <summary>
/// RozetkaPay webhook callback payload
/// </summary>
public class PaymentWebhook
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
    /// Unified external ID
    /// </summary>
    [JsonPropertyName("unified_external_id")]
    public string? UnifiedExternalId { get; set; }

    /// <summary>
    /// Project ID
    /// </summary>
    [JsonPropertyName("project_id")]
    public string? ProjectId { get; set; }

    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    [JsonPropertyName("is_success")]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Payment details
    /// </summary>
    [JsonPropertyName("details")]
    public PaymentWebhookDetails? Details { get; set; }

    /// <summary>
    /// Receipt URL
    /// </summary>
    [JsonPropertyName("receipt_url")]
    public string? ReceiptUrl { get; set; }

    /// <summary>
    /// Whether action is required
    /// </summary>
    [JsonPropertyName("action_required")]
    public bool ActionRequired { get; set; }

    /// <summary>
    /// Required action (if any)
    /// </summary>
    [JsonPropertyName("action")]
    public UserAction? Action { get; set; }

    /// <summary>
    /// Payment method information
    /// </summary>
    [JsonPropertyName("payment_method")]
    public WebhookPaymentMethod? PaymentMethod { get; set; }

    /// <summary>
    /// Customer information
    /// </summary>
    [JsonPropertyName("customer")]
    public WebhookCustomer? Customer { get; set; }

    /// <summary>
    /// Operation type
    /// </summary>
    [JsonPropertyName("operation")]
    public string? Operation { get; set; }
}

/// <summary>
/// Payment details in webhook
/// </summary>
public class PaymentWebhookDetails
{
    /// <summary>
    /// Payment ID
    /// </summary>
    [JsonPropertyName("payment_id")]
    public string? PaymentId { get; set; }

    /// <summary>
    /// Operation ID
    /// </summary>
    [JsonPropertyName("operation_id")]
    public string? OperationId { get; set; }

    /// <summary>
    /// Transaction ID
    /// </summary>
    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }

    /// <summary>
    /// Billing order ID
    /// </summary>
    [JsonPropertyName("billing_order_id")]
    public string? BillingOrderId { get; set; }

    /// <summary>
    /// Gateway order ID
    /// </summary>
    [JsonPropertyName("gateway_order_id")]
    public string? GatewayOrderId { get; set; }

    /// <summary>
    /// Reference Retrieval Number
    /// </summary>
    [JsonPropertyName("rrn")]
    public string? Rrn { get; set; }

    /// <summary>
    /// Payment amount as string (from webhook)
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonConverter(typeof(FlexibleDecimalConverter))]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Payment currency
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Payment status
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Status code
    /// </summary>
    [JsonPropertyName("status_code")]
    public string? StatusCode { get; set; }

    /// <summary>
    /// Status description
    /// </summary>
    [JsonPropertyName("status_description")]
    public string? StatusDescription { get; set; }

    /// <summary>
    /// Date when transaction was created
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Date when transaction was processed
    /// </summary>
    [JsonPropertyName("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Payment description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Additional payload
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    /// <summary>
    /// Authorization code
    /// </summary>
    [JsonPropertyName("auth_code")]
    public string? AuthCode { get; set; }

    /// <summary>
    /// Terminal name
    /// </summary>
    [JsonPropertyName("terminal_name")]
    public string? TerminalName { get; set; }

    /// <summary>
    /// Bank name
    /// </summary>
    [JsonPropertyName("bank_name")]
    public string? BankName { get; set; }

    /// <summary>
    /// Fee information
    /// </summary>
    [JsonPropertyName("fee")]
    public WebhookFee? Fee { get; set; }

    /// <summary>
    /// Comment
    /// </summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// Payment method
    /// </summary>
    [JsonPropertyName("method")]
    public string? Method { get; set; }

    /// <summary>
    /// Recipient card mask
    /// </summary>
    [JsonPropertyName("recipient_cc_mask")]
    public string? RecipientCardMask { get; set; }
}

/// <summary>
/// Fee information in webhook
/// </summary>
public class WebhookFee
{
    /// <summary>
    /// Fee amount as string (from webhook)
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonConverter(typeof(FlexibleDecimalConverter))]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Fee currency
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
}

/// <summary>
/// Payment method information in webhook
/// </summary>
public class WebhookPaymentMethod
{
    /// <summary>
    /// Payment method type
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Credit card token information
    /// </summary>
    [JsonPropertyName("cc_token")]
    public WebhookCardToken? CardToken { get; set; }
}

/// <summary>
/// Card token information in webhook
/// </summary>
public class WebhookCardToken
{
    /// <summary>
    /// Card token
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>
    /// Masked card number
    /// </summary>
    [JsonPropertyName("mask")]
    public string? Mask { get; set; }

    /// <summary>
    /// Token expiration date
    /// </summary>
    [JsonPropertyName("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Bank short name
    /// </summary>
    [JsonPropertyName("bank_short_name")]
    public string? BankShortName { get; set; }

    /// <summary>
    /// Payment system
    /// </summary>
    [JsonPropertyName("payment_system")]
    public string? PaymentSystem { get; set; }
}

/// <summary>
/// Customer information in webhook
/// </summary>
public class WebhookCustomer
{
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
    /// Customer email
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Customer phone
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Customer country
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>
    /// Customer address
    /// </summary>
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    /// <summary>
    /// Customer city
    /// </summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>
    /// Customer postal code
    /// </summary>
    [JsonPropertyName("postal_code")]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Customer IP address
    /// </summary>
    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Browser user agent
    /// </summary>
    [JsonPropertyName("browser_user_agent")]
    public string? BrowserUserAgent { get; set; }

    /// <summary>
    /// Device fingerprint
    /// </summary>
    [JsonPropertyName("fingerprint")]
    public string? Fingerprint { get; set; }
} 