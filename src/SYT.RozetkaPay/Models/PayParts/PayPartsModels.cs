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
    [JsonPropertyName("products")]
    public List<PayPartsProduct>? Products { get; set; }

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

    /// <summary>
    /// Additional merchant-defined data associated with the payment.
    /// </summary>
    /// <remarks>
    /// At most ten entries; each key up to 30 characters and each value up to 200. The limits are the
    /// provider's, and <see cref="MetadataLimitsAttribute"/> enforces them before a
    /// request is sent rather than leaving the gateway to reject it.
    /// </remarks>
    [MetadataLimits]
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Optional delivery information for the order.
    /// </summary>
    [JsonPropertyName("deliveries")]
    public List<PayPartsDelivery>? Deliveries { get; set; }
}

/// <summary>
/// PayParts payment mode enum
/// </summary>
public enum PayPartsPaymentMode
{
    /// <summary>
    /// Hosted checkout: RozetkaPay collects the payment details.
    /// </summary>
    [JsonStringEnumMemberName("hosted")]
    Hosted,

    /// <summary>
    /// Direct: the merchant supplies the payment details.
    /// </summary>
    [JsonStringEnumMemberName("direct")]
    Direct
}

// ===================== CREATE PAYPARTS ORDER MODELS =====================

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
    /// Customer phone number
    /// </summary>
    [JsonPropertyName("phone")]
    public required string Phone { get; set; }

    /// <summary>
    /// Customer email address
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }


    /// <summary>
    /// Provider field &lt;c&gt;account_number&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("account_number")]
    public string? AccountNumber { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;fingerprint&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("fingerprint")]
    public BrowserFingerprint? Fingerprint { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;payment_method&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("payment_method")]
    public PayPartsPaymentMethod? PaymentMethod { get; set; }
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
    [Required]
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = 1;



    /// <summary>
    /// First-level product category (top level in hierarchy, most general)
    /// </summary>
    [JsonPropertyName("category_name")]
    public string? CategoryName { get; set; }
    /// <summary>
    /// Id of the product on the merchant side.
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }
    /// <summary>
    /// Second-level product category
    /// </summary>
    [JsonPropertyName("second_category_name")]
    public string? SecondCategoryName { get; set; }
    /// <summary>
    /// Tax group is required for projects with enabled fiscalization.
    /// </summary>
    [JsonPropertyName("tax_group")]
    public int? TaxGroup { get; set; }
}

// ===================== PAYPARTS ORDER RESPONSE =====================


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

    // reason and external_refund_id are gone with EXP-436. The document declares neither, so the
    // gateway discarded both: a caller who recorded a refund reason or their own refund id believed
    // it was stored, and it never left the process in any form the provider kept.

    /// <summary>
    /// Refund currency. Declared by the document alongside the amount.
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Opaque value echoed back on the callback.
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    /// <summary>
    /// Line items covered by the refund, for fiscalization.
    /// </summary>
    [JsonPropertyName("products")]
    // same shape as the order it refunds: quantity is an integer and price is a
    // required number, where Product writes both as strings for payments/new.
    public List<PayPartsProduct>? Products { get; set; }

    /// <summary>
    /// Where the provider posts the result of this refund.
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }
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


// ===================== PAYPARTS OPERATION MODELS =====================


// ===================== PAYPARTS OPERATIONS LIST MODELS =====================



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

    // callback_url is gone with EXP-436: the document does not declare it for this operation, so
    // the provider discarded it. Resending a callback goes to the URL recorded on the original
    // payment, and offering an override that silently does nothing is worse than not offering one.

    /// <summary>
    /// Which operation's callback to resend. Declared by the document; the SDK could not express it.
    /// </summary>
    [JsonPropertyName("operation")]
    public OperationType? Operation { get; set; }
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
    [JsonStringEnumMemberName("create")]
    Create,
    
    /// <summary>
    /// Confirm operation
    /// </summary>
    [JsonStringEnumMemberName("confirm")]
    Confirm,
    
    /// <summary>
    /// Cancel operation
    /// </summary>
    [JsonStringEnumMemberName("cancel")]
    Cancel,
    
    /// <summary>
    /// Refund operation
    /// </summary>
    [JsonStringEnumMemberName("refund")]
    Refund
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
    public ResponseCode? StatusCode { get; set; }

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

    /// <summary>
    /// Provider field &lt;c&gt;bank_key&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("bank_key")]
    public string? BankKey { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;fiscalization&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("fiscalization")]
    public Fiscalization? Fiscalization { get; set; }
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

    /// <summary>
    /// Additional merchant-defined data associated with the payment.
    /// </summary>
    /// <remarks>
    /// At most ten entries; each key up to 30 characters and each value up to 200. The limits are the
    /// provider's, and <see cref="MetadataLimitsAttribute"/> enforces them before a
    /// request is sent rather than leaving the gateway to reject it.
    /// </remarks>
    [MetadataLimits]
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
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

    /// <summary>
    /// Products covered by the refund, for a partial refund that names the items.
    /// </summary>
    [JsonPropertyName("products")]
    public List<PayPartsProduct>? Products { get; set; }
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

/// <summary>
/// Delivery details attached to a PayParts order.
/// </summary>
public class PayPartsDelivery
{
    /// <summary>Delivery method identifier.</summary>
    [JsonPropertyName("method_id")]
    public string? MethodId { get; set; }

    /// <summary>Delivery service identifier.</summary>
    [JsonPropertyName("service_id")]
    public string? ServiceId { get; set; }

    /// <summary>Requested delivery time.</summary>
    [JsonPropertyName("time")]
    public string? Time { get; set; }

    /// <summary>Destination city.</summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>Destination street.</summary>
    [JsonPropertyName("street")]
    public string? Street { get; set; }

    /// <summary>Destination house.</summary>
    [JsonPropertyName("house")]
    public string? House { get; set; }

    /// <summary>Destination flat.</summary>
    [JsonPropertyName("flat")]
    public string? Flat { get; set; }

    /// <summary>Whether someone other than the payer receives the delivery.</summary>
    [JsonPropertyName("is_other_person")]
    public bool? IsOtherPerson { get; set; }

    /// <summary>Recipient, when it is not the payer.</summary>
    [JsonPropertyName("other_person")]
    public PayPartsDeliveryOtherPerson? OtherPerson { get; set; }
}

/// <summary>
/// Recipient of a PayParts delivery when it is not the payer.
/// </summary>
public class PayPartsDeliveryOtherPerson
{
    /// <summary>Recipient last name.</summary>
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    /// <summary>Recipient first name.</summary>
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    /// <summary>Recipient middle name.</summary>
    [JsonPropertyName("middle_name")]
    public string? MiddleName { get; set; }

    /// <summary>Recipient phone number.</summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
}
