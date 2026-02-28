using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Payments;

namespace SYT.RozetkaPay.Models.Batch;

/// <summary>
/// Request to create batch acquiring payment
/// </summary>
public class CreateBatchPaymentRequest
{
    /// <summary>
    /// URL to receive callback after finalization of batch operation
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Batch payment currency
    /// </summary>
    [Required]
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Payer customer data
    /// </summary>
    [Required]
    [JsonPropertyName("customer")]
    public BatchCustomer Customer { get; set; } = new();

    /// <summary>
    /// Describes the way of the integration: direct - requires customer's payment details in request
    /// </summary>
    [Required]
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "direct";

    /// <summary>
    /// Additional data transmitted with the batch payment request
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    /// <summary>
    /// URL to receive payment result
    /// </summary>
    [JsonPropertyName("result_url")]
    public string? ResultUrl { get; set; }

    /// <summary>
    /// If true - funds are transferred to recipient account. If false - you have to call confirm for additional confirmation
    /// </summary>
    [JsonPropertyName("confirm")]
    public bool Confirm { get; set; } = true;

    /// <summary>
    /// List of orders of batch (max 10 items)
    /// </summary>
    [Required]
    [JsonPropertyName("orders")]
    public List<BatchOrder> Orders { get; set; } = new();

    /// <summary>
    /// External identifier from the merchant, one per batch. Unique within the project
    /// </summary>
    [Required]
    [JsonPropertyName("batch_external_id")]
    public string BatchExternalId { get; set; } = string.Empty;
}

/// <summary>
/// Customer data for batch payment
/// </summary>
public class BatchCustomer
{
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
    /// Customer country
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>
    /// Customer email
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// External customer ID
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

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
    /// Customer postal code
    /// </summary>
    [JsonPropertyName("postal_code")]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Payment method details
    /// </summary>
    [JsonPropertyName("payment_method")]
    public PaymentMethod? PaymentMethod { get; set; }

    /// <summary>
    /// Customer locale
    /// </summary>
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    /// <summary>
    /// Browser fingerprint
    /// </summary>
    [JsonPropertyName("fingerprint")]
    public BrowserFingerprint? Fingerprint { get; set; }
}

/// <summary>
/// Batch order information for create request
/// Based on official RozetkaPay CDN documentation: https://cdn.rozetkapay.com/public-docs/index.html
/// </summary>
public class BatchOrder
{
    /// <summary>
    /// API key for the order
    /// </summary>
    [JsonPropertyName("api_key")]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Order amount in UAH. Standard JSON number format as per CDN documentation.
    /// Use decimal values like 123.45 for 123.45 UAH.
    /// </summary>
    [Required]
    [JsonPropertyName("amount")]
    public required decimal Amount { get; set; }

    /// <summary>
    /// Order description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// External order ID
    /// </summary>
    [Required]
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; set; }

    /// <summary>
    /// Unified external ID
    /// </summary>
    [JsonPropertyName("unified_external_id")]
    public string? UnifiedExternalId { get; set; }

    /// <summary>
    /// List of products in the order
    /// </summary>
    [JsonPropertyName("products")]
    public List<Product>? Products { get; set; }
}

/// <summary>
/// Request to confirm batch payment
/// </summary>
public class ConfirmBatchPaymentRequest
{
    /// <summary>
    /// URL to receive callback after finalization of batch operation
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// External identifier from the merchant, one per batch. Unique within the project
    /// </summary>
    [Required]
    [JsonPropertyName("batch_external_id")]
    public string BatchExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Additional data transmitted with the batch payment request
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    /// <summary>
    /// List of orders to confirm
    /// </summary>
    [JsonPropertyName("orders")]
    public List<BatchConfirmOrder>? Orders { get; set; }

    /// <summary>
    /// External identifier from the merchant, one per batch. Unique within the project
    /// </summary>
    [Required]
    [JsonPropertyName("external_id")]
    public string ExternalId { get; set; } = string.Empty;
}

/// <summary>
/// Batch order for confirmation
/// Based on official RozetkaPay CDN documentation: https://cdn.rozetkapay.com/public-docs/index.html
/// </summary>
public class BatchConfirmOrder
{
    /// <summary>
    /// API key for the order
    /// </summary>
    [JsonPropertyName("api_key")]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Confirmation amount (JSON number as per CDN documentation)
    /// If the amount is not set, full confirmation will be processed for this order
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// External order ID
    /// </summary>
    [Required]
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; set; }
}

/// <summary>
/// Request to cancel batch payment
/// </summary>
public class CancelBatchPaymentRequest
{
    /// <summary>
    /// URL to receive callback after finalization of batch operation
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Additional data transmitted with the batch payment request
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    /// <summary>
    /// External identifier from the merchant, one per batch. Unique within the project
    /// </summary>
    [Required]
    [JsonPropertyName("external_id")]
    public string ExternalId { get; set; } = string.Empty;
}

/// <summary>
/// Batch payment response
/// </summary>
public class BatchPaymentResponse
{
    /// <summary>
    /// Batch ID
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Whether action is required from user
    /// </summary>
    [JsonPropertyName("action_required")]
    public bool? ActionRequired { get; set; }

    /// <summary>
    /// User action details
    /// </summary>
    [JsonPropertyName("action")]
    public UserAction? Action { get; set; }

    /// <summary>
    /// Batch details
    /// </summary>
    [JsonPropertyName("batch_details")]
    public BatchDetails? BatchDetails { get; set; }

    /// <summary>
    /// Payment method response
    /// </summary>
    [JsonPropertyName("payment_method")]
    public PaymentMethodResponse? PaymentMethod { get; set; }

    /// <summary>
    /// Order details
    /// </summary>
    [JsonPropertyName("order_details")]
    public List<BatchOrderDetails>? OrderDetails { get; set; }

    /// <summary>
    /// Batch external ID
    /// </summary>
    [JsonPropertyName("batch_external_id")]
    public string? BatchExternalId { get; set; }

    /// <summary>
    /// Receipt URL
    /// </summary>
    [JsonPropertyName("receipt_url")]
    public string? ReceiptUrl { get; set; }
}

/// <summary>
/// Batch details information
/// </summary>
public class BatchDetails
{
    /// <summary>
    /// Batch total amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Authorization code
    /// </summary>
    [JsonPropertyName("auth_code")]
    public string? AuthCode { get; set; }

    /// <summary>
    /// Batch comment
    /// </summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// Batch creation date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Batch currency
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Merchant ID
    /// </summary>
    [JsonPropertyName("mid")]
    public string? Mid { get; set; }

    /// <summary>
    /// Additional payload data
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    /// <summary>
    /// Batch processing date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Reference Retrieval Number
    /// </summary>
    [JsonPropertyName("rrn")]
    public string? Rrn { get; set; }

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
    /// Terminal ID
    /// </summary>
    [JsonPropertyName("tid")]
    public string? Tid { get; set; }
}

/// <summary>
/// Batch order details information
/// </summary>
public class BatchOrderDetails
{
    /// <summary>
    /// External order ID
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Unified external ID
    /// </summary>
    [JsonPropertyName("unified_external_id")]
    public string? UnifiedExternalId { get; set; }

    /// <summary>
    /// Order description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

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
    /// Order amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Order status
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
    /// Payment method
    /// </summary>
    [JsonPropertyName("method")]
    public string? Method { get; set; }

    /// <summary>
    /// Fee details
    /// </summary>
    [JsonPropertyName("fee")]
    public BatchFee? Fee { get; set; }
}

/// <summary>
/// Payment method response information
/// </summary>
public class PaymentMethodResponse
{
    /// <summary>
    /// Payment method type (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Masked card number (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("masked_card")]
    public string? MaskedCard { get; set; }

    /// <summary>
    /// Card brand (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("brand")]
    public string? Brand { get; set; }
}

/// <summary>
/// Batch fee information
/// </summary>
public class BatchFee
{
    /// <summary>
    /// Fee amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Fee currency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Fee rate (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("rate")]
    public decimal? Rate { get; set; }
}

/// <summary>
/// Batch payment mode enumeration (JSON string as per CDN documentation)
/// </summary>
public enum BatchPaymentMode
{
    /// <summary>
    /// Direct mode - requires customer's payment details in request
    /// </summary>
    [JsonPropertyName("direct")]
    Direct
}

/// <summary>
/// Batch customer request user details (JSON object as per CDN documentation)
/// </summary>
public class BatchCustomerRequestUserDetails : BaseRequestUserDetails
{
    /// <summary>
    /// Customer payment method (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_method")]
    [Required]
    public CustomerRequestPaymentMethod PaymentMethod { get; set; } = new();

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
/// Batch order detail from OpenAPI schema (JSON object as per CDN documentation)
/// </summary>
public class BatchOrderDetail
{
    /// <summary>
    /// External transaction identifier (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Unified external identifier (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("unified_external_id")]
    public string? UnifiedExternalId { get; set; }

    /// <summary>
    /// Transaction description (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Operation identifier (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("operation_id")]
    public string? OperationId { get; set; }

    /// <summary>
    /// Transaction identifier (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }

    /// <summary>
    /// Order amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Operation status (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public OperationStatus? Status { get; set; }

    /// <summary>
    /// Status code (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status_code")]
    public ResponseCode? StatusCode { get; set; }

    /// <summary>
    /// Description of the operation status (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status_description")]
    public string? StatusDescription { get; set; }

    /// <summary>
    /// Operation method (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("method")]
    public BatchMethodType? Method { get; set; }

    /// <summary>
    /// Fee details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("fee")]
    public FeeDetails? Fee { get; set; }
}

/// <summary>
/// Batch payment operation result (OpenAPI response schema)
/// </summary>
public class BatchPaymentOperationResult
{
    /// <summary>
    /// Unique batch identifier
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// A flag indicating that user action is required
    /// </summary>
    [JsonPropertyName("action_required")]
    public bool ActionRequired { get; set; }

    /// <summary>
    /// User action details if required
    /// </summary>
    [JsonPropertyName("action")]
    public UserAction? Action { get; set; }

    /// <summary>
    /// Batch details
    /// </summary>
    [JsonPropertyName("batch_details")]
    public BatchDetails? BatchDetails { get; set; }

    /// <summary>
    /// Payment method information
    /// </summary>
    [JsonPropertyName("payment_method")]
    public ResultPaymentMethod? PaymentMethod { get; set; }

    /// <summary>
    /// List of orders in batch
    /// </summary>
    [JsonPropertyName("order_details")]
    public List<BatchOrderDetail>? OrderDetails { get; set; }

    /// <summary>
    /// External identifier from the merchant, one per batch
    /// </summary>
    [JsonPropertyName("batch_external_id")]
    public string? BatchExternalId { get; set; }

    /// <summary>
    /// URL to the received receipt or transaction confirmation
    /// </summary>
    [JsonPropertyName("receipt_url")]
    public string? ReceiptUrl { get; set; }
} 