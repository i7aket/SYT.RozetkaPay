using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Converters;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Payments;

namespace SYT.RozetkaPay.Models.PayParts;

// ===================== OPENAPI SCHEMA: CreatePayPartsOrder =====================

/// <summary>
/// OpenAPI schema for creating PayParts order (different from CreatePayPartsOrderRequest)
/// </summary>
public class CreatePayPartsOrder
{
    /// <summary>
    /// If set to true, order confirmation will be done automatically after creation becomes successful
    /// </summary>
    [JsonPropertyName("auto_confirm_after_success")]
    public bool AutoConfirmAfterSuccess { get; set; } = false;

    /// <summary>
    /// Bank name
    /// </summary>
    [Required]
    [JsonPropertyName("bank_name")]
    public required string BankName { get; set; }

    /// <summary>
    /// Payment mode
    /// </summary>
    [Required]
    [JsonPropertyName("mode")]
    public PayPartsPaymentMode Mode { get; set; }

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
    /// Order amount (minimum 1)
    /// </summary>
    [Required]
    [JsonPropertyName("amount")]
    public required decimal Amount { get; set; }

    /// <summary>
    /// Order currency
    /// </summary>
    [Required]
    [JsonPropertyName("currency")]
    public required string Currency { get; set; }

    /// <summary>
    /// Number of parts (installments)
    /// </summary>
    [Required]
    [JsonPropertyName("parts_count")]
    public required int PartsCount { get; set; }

    /// <summary>
    /// Order description (1-256 characters)
    /// </summary>
    [Required]
    [JsonPropertyName("description")]
    public required string Description { get; set; }

    /// <summary>
    /// Payload string
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    /// <summary>
    /// Products in the order
    /// </summary>
    [Required]
    [JsonPropertyName("products")]
    public required List<PayPartsProduct> Products { get; set; }

    /// <summary>
    /// Result URL (max 2048 characters)
    /// </summary>
    [JsonPropertyName("result_url")]
    public string? ResultUrl { get; set; }

    /// <summary>
    /// Callback URL
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Customer information
    /// </summary>
    [Required]
    [JsonPropertyName("customer")]
    public required PayPartsCustomer Customer { get; set; }
}

/// <summary>
/// PayParts payment mode enum
/// </summary>
public enum PayPartsPaymentMode
{
    /// <summary>
    /// Single payment mode
    /// </summary>
    [JsonPropertyName("single")]
    Single,

    /// <summary>
    /// Installment payment mode
    /// </summary>
    [JsonPropertyName("installment")]
    Installment
}

// ===================== CREATE PAYPARTS ORDER MODELS =====================

/// <summary>
/// Request to create PayParts order
/// </summary>
public class CreatePayPartsOrderRequest
{
    /// <summary>
    /// External order ID (must be unique per merchant)
    /// </summary>
    [Required]
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; set; }

    /// <summary>
    /// Order amount (JSON number as per CDN documentation)
    /// </summary>
    [Required]
    [JsonPropertyName("amount")]
    public required decimal Amount { get; set; }

    /// <summary>
    /// Order currency
    /// </summary>
    [Required]
    [JsonPropertyName("currency")]
    public required string Currency { get; set; }

    /// <summary>
    /// Number of installments
    /// </summary>
    [Required]
    [JsonPropertyName("parts_count")]
    public required int PartsCount { get; set; }

    /// <summary>
    /// Order description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Selected bank for PayParts
    /// </summary>
    [JsonPropertyName("bank")]
    public string? Bank { get; set; }

    /// <summary>
    /// Merchant ID for PayParts
    /// </summary>
    [JsonPropertyName("merchant_id")]
    public string? MerchantId { get; set; }

    /// <summary>
    /// Callback URL for order notifications
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Success URL to redirect customer after successful order
    /// </summary>
    [JsonPropertyName("success_url")]
    public string? SuccessUrl { get; set; }

    /// <summary>
    /// Failure URL to redirect customer after failed order
    /// </summary>
    [JsonPropertyName("failure_url")]
    public string? FailureUrl { get; set; }

    /// <summary>
    /// Customer information
    /// </summary>
    [JsonPropertyName("customer")]
    public PayPartsCustomer? Customer { get; set; }

    /// <summary>
    /// Products in the order
    /// </summary>
    [JsonPropertyName("products")]
    public List<PayPartsProduct>? Products { get; set; }

    /// <summary>
    /// Additional order metadata
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Customer information for PayParts
/// </summary>
public class PayPartsCustomer
{
    /// <summary>
    /// Customer first name
    /// </summary>
    [Required]
    [JsonPropertyName("first_name")]
    public required string FirstName { get; set; }

    /// <summary>
    /// Customer last name
    /// </summary>
    [Required]
    [JsonPropertyName("last_name")]
    public required string LastName { get; set; }

    /// <summary>
    /// Customer patronym (middle name)
    /// </summary>
    [JsonPropertyName("patronym")]
    public string? Patronym { get; set; }

    /// <summary>
    /// Customer phone number
    /// </summary>
    [Required]
    [JsonPropertyName("phone")]
    public required string Phone { get; set; }

    /// <summary>
    /// Customer email address
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Customer date of birth
    /// </summary>
    [JsonPropertyName("birth_date")]
    public DateTime? BirthDate { get; set; }
}

/// <summary>
/// Product information for PayParts
/// </summary>
public class PayPartsProduct
{
    /// <summary>
    /// Product name
    /// </summary>
    [Required]
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Product price (JSON number as per CDN documentation)
    /// </summary>
    [Required]
    [JsonPropertyName("price")]
    public required decimal Price { get; set; }

    /// <summary>
    /// Product quantity
    /// </summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Product category
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>
    /// Product URL
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

// ===================== PAYPARTS ORDER RESPONSE =====================

/// <summary>
/// PayParts order response
/// </summary>
public class PayPartsOrderResponse
{
    /// <summary>
    /// Order ID
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// External order ID
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Order status
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Order amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Order currency
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Number of installments
    /// </summary>
    [JsonPropertyName("parts_count")]
    public int? PartsCount { get; set; }

    /// <summary>
    /// Selected bank
    /// </summary>
    [JsonPropertyName("bank")]
    public string? Bank { get; set; }

    /// <summary>
    /// Checkout URL for customer
    /// </summary>
    [JsonPropertyName("checkout_url")]
    public string? CheckoutUrl { get; set; }

    /// <summary>
    /// QR code for payment
    /// </summary>
    [JsonPropertyName("qr_code")]
    public string? QrCode { get; set; }

    /// <summary>
    /// Order creation date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Order processing date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Customer information
    /// </summary>
    [JsonPropertyName("customer")]
    public PayPartsCustomer? Customer { get; set; }

    /// <summary>
    /// Products information
    /// </summary>
    [JsonPropertyName("products")]
    public List<PayPartsProduct>? Products { get; set; }

    /// <summary>
    /// Error information
    /// </summary>
    [JsonPropertyName("error")]
    public PayPartsError? Error { get; set; }
}

/// <summary>
/// PayParts error information
/// </summary>
public class PayPartsError
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
}

// ===================== CONFIRM PAYPARTS ORDER MODELS =====================

// ===================== CANCEL PAYPARTS ORDER MODELS =====================

/// <summary>
/// Request to refund PayParts order
/// </summary>
public class RefundPayPartsOrderRequest
{
    /// <summary>
    /// External order ID
    /// </summary>
    [Required]
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; set; }

    /// <summary>
    /// Refund amount (JSON number as per CDN documentation)
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
}

/// <summary>
/// Request to retry pending PayParts refund operation
/// </summary>
public class RetryRefundPPayRequest
{
    /// <summary>
    /// External order ID
    /// </summary>
    [Required]
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; set; }
}

/// <summary>
/// Request to cancel pending PayParts refund operation
/// </summary>
public class CancelRefundPPayRequest
{
    /// <summary>
    /// External order ID
    /// </summary>
    [Required]
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; set; }
}

/// <summary>
/// PayParts refund response
/// </summary>
public class PayPartsRefundResponse
{
    /// <summary>
    /// Refund ID
    /// </summary>
    [JsonPropertyName("refund_id")]
    public string? RefundId { get; set; }

    /// <summary>
    /// External refund ID
    /// </summary>
    [JsonPropertyName("external_refund_id")]
    public string? ExternalRefundId { get; set; }

    /// <summary>
    /// Refund status
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Refund amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Refund currency
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Refund reason
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// Refund creation date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Refund processing date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("processed_at")]
    public DateTime? ProcessedAt { get; set; }
}

// ===================== PAYPARTS OPERATION MODELS =====================

/// <summary>
/// PayParts operation response
/// </summary>
public class PayPartsOperationResponse
{
    /// <summary>
    /// Operation ID
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// External ID
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Operation type
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Operation status
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Operation amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Operation currency
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Operation description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Number of installments
    /// </summary>
    [JsonPropertyName("parts_count")]
    public int? PartsCount { get; set; }

    /// <summary>
    /// Bank name
    /// </summary>
    [JsonPropertyName("bank")]
    public string? Bank { get; set; }

    /// <summary>
    /// Operation creation date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Operation processing date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Customer information
    /// </summary>
    [JsonPropertyName("customer")]
    public PayPartsCustomer? Customer { get; set; }
}

// ===================== PAYPARTS OPERATIONS LIST MODELS =====================

/// <summary>
/// Request for PayParts operations list
/// </summary>
public class PayPartsOperationsListRequest
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
    /// Operation status filter
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Number of operations to return
    /// </summary>
    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    /// <summary>
    /// Number of operations to skip
    /// </summary>
    [JsonPropertyName("offset")]
    public int? Offset { get; set; }
}

/// <summary>
/// PayParts operations list response
/// </summary>
public class PayPartsOperationsListResponse
{
    /// <summary>
    /// List of operations
    /// </summary>
    [JsonPropertyName("operations")]
    public List<PayPartsOperationResponse>? Operations { get; set; }

    /// <summary>
    /// Total number of operations
    /// </summary>
    [JsonPropertyName("total")]
    public int? Total { get; set; }

    /// <summary>
    /// Number of operations returned
    /// </summary>
    [JsonPropertyName("count")]
    public int? Count { get; set; }

    /// <summary>
    /// Pagination offset
    /// </summary>
    [JsonPropertyName("offset")]
    public int? Offset { get; set; }
}

// ===================== PAYPARTS CALLBACK MODELS =====================

/// <summary>
/// Request to resend PayParts callback
/// </summary>
public class PayPartsResendCallbackRequest
{
    /// <summary>
    /// External order ID
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
/// PayParts resend callback response
/// </summary>
public class PayPartsResendCallbackResponse
{
    /// <summary>
    /// Operation status
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// PayParts operation types (JSON string as per CDN documentation)
/// </summary>
public enum PayPartsOperationType
{
    /// <summary>
    /// Create operation
    /// </summary>
    [JsonPropertyName("create")]
    Create,
    
    /// <summary>
    /// Confirm operation
    /// </summary>
    [JsonPropertyName("confirm")]
    Confirm,
    
    /// <summary>
    /// Cancel operation
    /// </summary>
    [JsonPropertyName("cancel")]
    Cancel,
    
    /// <summary>
    /// Refund operation
    /// </summary>
    [JsonPropertyName("refund")]
    Refund
}

/// <summary>
/// PayParts response codes (JSON string as per CDN documentation)
/// </summary>
public enum PayPartsResponseCode
{
    /// <summary>
    /// Success
    /// </summary>
    [JsonPropertyName("00")]
    Success,
    
    /// <summary>
    /// Pending
    /// </summary>
    [JsonPropertyName("pending")]
    Pending,
    
    /// <summary>
    /// Failed
    /// </summary>
    [JsonPropertyName("failed")]
    Failed
}

/// <summary>
/// PayParts operation details (JSON object as per CDN documentation)
/// </summary>
public class PayPartsOperationDetails
{
    /// <summary>
    /// Operation method (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("method")]
    public PayPartsOperationType? Method { get; set; }

    /// <summary>
    /// Operation ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("operation_id")]
    public string? OperationId { get; set; }

    /// <summary>
    /// Transaction ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; set; }

    /// <summary>
    /// Billing order ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("billing_order_id")]
    public string? BillingOrderId { get; set; }

    /// <summary>
    /// Gateway order ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("gateway_order_id")]
    public string? GatewayOrderId { get; set; }

    /// <summary>
    /// Reference number (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("rrn")]
    public string? RRN { get; set; }

    /// <summary>
    /// Operation amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Operation currency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Operation status (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public OperationStatus? Status { get; set; }

    /// <summary>
    /// Operation status code (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status_code")]
    public PayPartsResponseCode? StatusCode { get; set; }

    /// <summary>
    /// Operation status description (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status_description")]
    public string? StatusDescription { get; set; }

    /// <summary>
    /// Operation creation date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("created_at")]
    [JsonConverter(typeof(NullableFlexibleDateTimeConverter))]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Operation processing date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("processed_at")]
    [JsonConverter(typeof(NullableFlexibleDateTimeConverter))]
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Additional payload data (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    /// <summary>
    /// Authorization code (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("auth_code")]
    public string? AuthCode { get; set; }

    /// <summary>
    /// Bank name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("bank_name")]
    public string? BankName { get; set; }
}

/// <summary>
/// PayParts operation result (OpenAPI response schema)
/// </summary>
public class PayPartsOperationResult
{
    /// <summary>
    /// Internal operation id
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// External id
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Unified external id
    /// </summary>
    [JsonPropertyName("unified_external_id")]
    public string? UnifiedExternalId { get; set; }

    /// <summary>
    /// Success flag
    /// </summary>
    [JsonPropertyName("is_success")]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Operation details
    /// </summary>
    [JsonPropertyName("details")]
    public PayPartsOperationDetails? Details { get; set; }

    /// <summary>
    /// A boolean flag which indicates if action from the customer is required
    /// </summary>
    [JsonPropertyName("action_required")]
    public bool ActionRequired { get; set; }

    /// <summary>
    /// User action details
    /// </summary>
    [JsonPropertyName("action")]
    public UserAction? Action { get; set; }

    /// <summary>
    /// Link to the receipt for user
    /// </summary>
    [JsonPropertyName("receipt_url")]
    public string? ReceiptUrl { get; set; }
}

/// <summary>
/// PayParts operations result (OpenAPI response schema)
/// </summary>
public class PayPartsOperationsResult
{
    /// <summary>
    /// Merchant's transaction id
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Unified external id
    /// </summary>
    [JsonPropertyName("unified_external_id")]
    public string? UnifiedExternalId { get; set; }

    /// <summary>
    /// Transaction amount
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Amount of confirmed funds (should be same as amount)
    /// </summary>
    [JsonPropertyName("amount_confirmed")]
    public decimal? AmountConfirmed { get; set; }

    /// <summary>
    /// Amount of canceled funds (should be same as amount)
    /// </summary>
    [JsonPropertyName("amount_canceled")]
    public decimal? AmountCanceled { get; set; }

    /// <summary>
    /// Amount of refunded funds
    /// </summary>
    [JsonPropertyName("amount_refunded")]
    public decimal? AmountRefunded { get; set; }

    /// <summary>
    /// Transaction currency
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// A boolean flag which indicates if payment was successful
    /// </summary>
    [JsonPropertyName("purchased")]
    public bool Purchased { get; set; }

    /// <summary>
    /// Details of primary operation
    /// </summary>
    [JsonPropertyName("purchase_details")]
    public PayPartsOperationDetails? PurchaseDetails { get; set; }

    /// <summary>
    /// A boolean flag which indicates if order was confirmed
    /// </summary>
    [JsonPropertyName("confirmed")]
    public bool Confirmed { get; set; }

    /// <summary>
    /// List of confirmation operations
    /// </summary>
    [JsonPropertyName("confirmation_details")]
    public List<PayPartsOperationDetails>? ConfirmationDetails { get; set; }

    /// <summary>
    /// A boolean flag which indicates if order was refunded
    /// </summary>
    [JsonPropertyName("refunded")]
    public bool Refunded { get; set; }

    /// <summary>
    /// List of refund operations
    /// </summary>
    [JsonPropertyName("refund_details")]
    public List<PayPartsOperationDetails>? RefundDetails { get; set; }

    /// <summary>
    /// A boolean flag which indicates if order was canceled
    /// </summary>
    [JsonPropertyName("canceled")]
    public bool Canceled { get; set; }

    /// <summary>
    /// List of cancel operations
    /// </summary>
    [JsonPropertyName("cancellation_details")]
    public List<PayPartsOperationDetails>? CancellationDetails { get; set; }

    /// <summary>
    /// Link to the receipt for user
    /// </summary>
    [JsonPropertyName("receipt_url")]
    public string? ReceiptUrl { get; set; }

    /// <summary>
    /// Date when transaction was created
    /// </summary>
    [JsonPropertyName("created_at")]
    [JsonConverter(typeof(NullableFlexibleDateTimeConverter))]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// A boolean flag which indicates if action from the customer is required
    /// </summary>
    [JsonPropertyName("action_required")]
    public bool ActionRequired { get; set; }

    /// <summary>
    /// User action details
    /// </summary>
    [JsonPropertyName("action")]
    public UserAction? Action { get; set; }

    /// <summary>
    /// Customer information
    /// </summary>
    [JsonPropertyName("customer")]
    public CustomerInfo? Customer { get; set; }

    /// <summary>
    /// Delivery details
    /// </summary>
    [JsonPropertyName("delivery_details")]
    public ExpressCheckoutDeliveryDetails? DeliveryDetails { get; set; }

    /// <summary>
    /// Order recipient
    /// </summary>
    [JsonPropertyName("order_recipient")]
    public ExpressCheckoutRecipient? OrderRecipient { get; set; }
}

// ===================== MISSING OPENAPI SCHEMAS =====================

/// <summary>
/// Request to cancel PayParts order (OpenAPI schema)
/// </summary>
public class CancelPayPartsRequest
{
    /// <summary>
    /// External order ID
    /// </summary>
    [Required]
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; set; }

    /// <summary>
    /// Callback URL for order processing notifications
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Optional payload data
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }
}

/// <summary>
/// Request to confirm PayParts order (OpenAPI schema)
/// </summary>
public class ConfirmPayPartsRequest
{
    /// <summary>
    /// External order ID
    /// </summary>
    [Required]
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; set; }

    /// <summary>
    /// Callback URL for order processing notifications
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Optional payload data
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }
}

/// <summary>
/// Request to refund PayParts order (OpenAPI schema)
/// </summary>
public class RefundPPayRequest
{
    /// <summary>
    /// External order ID
    /// </summary>
    [Required]
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; set; }

    /// <summary>
    /// Refund amount (minimum 1)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Callback URL for refund notifications
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Optional payload data
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }
}

/// <summary>
/// PayParts card payment method (OpenAPI schema)
/// </summary>
public class PayPartsCardPaymentMethod
{
    /// <summary>
    /// Card number
    /// </summary>
    [Required]
    [JsonPropertyName("number")]
    public required string Number { get; set; }

    /// <summary>
    /// Card expiration month
    /// </summary>
    [Required]
    [JsonPropertyName("exp_month")]
    public required int ExpirationMonth { get; set; }

    /// <summary>
    /// Card expiration year
    /// </summary>
    [Required]
    [JsonPropertyName("exp_year")]
    public required int ExpirationYear { get; set; }

    /// <summary>
    /// Card verification value (CVV)
    /// </summary>
    [JsonPropertyName("cvv")]
    public string? Cvv { get; set; }
} 
