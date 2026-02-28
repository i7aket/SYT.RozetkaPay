using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Converters;
using SYT.RozetkaPay.Models.Common;

namespace SYT.RozetkaPay.Models.AlternativePayments;

/// <summary>
/// Alternative payment method types (JSON string as per CDN documentation)
/// </summary>
public enum AlternativePaymentMethodType
{
    /// <summary>
    /// BLIK payment method
    /// </summary>
    [JsonPropertyName("blik")]
    Blik,
    
    /// <summary>
    /// BLIK paylater payment method
    /// </summary>
    [JsonPropertyName("blik_paylater")]
    BlikPaylater,
    
    /// <summary>
    /// Leasing payment method
    /// </summary>
    [JsonPropertyName("leasing")]
    Leasing
}

/// <summary>
/// Alternative payment operation types (JSON string as per CDN documentation)
/// </summary>
public enum AlternativePaymentOperationType
{
    /// <summary>
    /// Create operation
    /// </summary>
    [JsonPropertyName("create")]
    Create
}

/// <summary>
/// Alternative payment response codes (JSON string as per CDN documentation)
/// </summary>
public enum AlternativePaymentResponseCode
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
/// Alternative payment providers (JSON string as per CDN documentation)
/// </summary>
public enum AlternativePaymentProvider
{
    /// <summary>
    /// iMoje provider
    /// </summary>
    [JsonPropertyName("imoje")]
    Imoje,
    
    /// <summary>
    /// LeaseLink provider
    /// </summary>
    [JsonPropertyName("leaselink")]
    LeaseLink
}

/// <summary>
/// Request for creating alternative payment
/// </summary>
public class CreateAlternativePaymentRequest
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
    /// External payment ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    [Required]
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Payment description (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Payment method (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_method")]
    public AlternativePaymentMethod? PaymentMethod { get; set; }

    /// <summary>
    /// Callback URL (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Return URL (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("return_url")]
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Customer details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("customer")]
    public AlternativePaymentCustomerDetails? Customer { get; set; }

    /// <summary>
    /// Payment provider (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("provider")]
    [Required]
    public AlternativePaymentProvider Provider { get; set; }

    /// <summary>
    /// Payment method data (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_method_data")]
    public object? PaymentMethodData { get; set; }
}

/// <summary>
/// Create alternative payment request (JSON object as per CDN documentation)
/// </summary>
public class CreateAlternativePayment
{
    /// <summary>
    /// Payment provider (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("provider")]
    [Required]
    public AlternativePaymentProvider Provider { get; set; }

    /// <summary>
    /// External payment ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    [Required]
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Unified external ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("unified_external_id")]
    public string? UnifiedExternalId { get; set; }

    /// <summary>
    /// Payment amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    [Required]
    [Range(1, double.MaxValue)]
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment currency (JSON string as per CDN documentation)
    /// PLN value is required if customer.payment_method.type is blik, blik_paylater, leasing
    /// </summary>
    [JsonPropertyName("currency")]
    [Required]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Order description (JSON string as per CDN documentation)
    /// Can have from 1 to 256 characters
    /// </summary>
    [JsonPropertyName("description")]
    [Required]
    [MaxLength(256)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Additional payload (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    /// <summary>
    /// Products list (JSON array as per CDN documentation)
    /// </summary>
    [JsonPropertyName("products")]
    [Required]
    public List<AlternativePaymentProduct> Products { get; set; } = new();

    /// <summary>
    /// Result URL (JSON string as per CDN documentation)
    /// Maximum length is about 2048 characters
    /// </summary>
    [JsonPropertyName("result_url")]
    [MaxLength(2048)]
    public string? ResultUrl { get; set; }

    /// <summary>
    /// Callback URL (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Customer details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("customer")]
    [Required]
    public AlternativePaymentCustomerDetails Customer { get; set; } = new();
}

/// <summary>
/// Customer details for alternative payments
/// </summary>
public class AlternativePaymentCustomerDetails : UserInfo
{
    /// <summary>
    /// Customer country (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>
    /// Customer city (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>
    /// Customer postal code (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("postal_code")]
    public string? PostalCode { get; set; }
}

/// <summary>
/// Alternative payment method information
/// </summary>
public class AlternativePaymentMethod
{
    /// <summary>
    /// Payment method code (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Payment method name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Payment method logo URL (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("logo_url")]
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Whether payment method is active (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
}

/// <summary>
/// Alternative payment product information
/// </summary>
public class AlternativePaymentProduct : Product
{
    /// <summary>
    /// Product URL (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Product image URL (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }
}

/// <summary>
/// BLIK request alternative payment method
/// </summary>
public class BlikRequestAlternativePaymentMethod
{
    /// <summary>
    /// BLIK code (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("blik_code")]
    [Required]
    public string BlikCode { get; set; } = string.Empty;

    /// <summary>
    /// Payment type (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

/// <summary>
/// Alternative payment operation details
/// </summary>
public class AlternativePaymentOperationDetails
{
    /// <summary>
    /// Operation method (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("method")]
    public AlternativePaymentOperationType? Method { get; set; }

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
    public AlternativePaymentResponseCode? StatusCode { get; set; }

    /// <summary>
    /// Operation status description (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status_description")]
    public string? StatusDescription { get; set; }

    /// <summary>
    /// Operation creation date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("created_at")]
    [JsonConverter(typeof(FlexibleDateTimeConverter))]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Operation processing date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("processed_at")]
    [JsonConverter(typeof(NullableFlexibleDateTimeConverter))]
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Additional payload (JSON string as per CDN documentation)
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
    /// Required user action (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("action")]
    public UserAction? Action { get; set; }

    /// <summary>
    /// Whether action is required (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("action_required")]
    public bool ActionRequired { get; set; }

    /// <summary>
    /// Receipt URL (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("receipt_url")]
    public string? ReceiptUrl { get; set; }

    /// <summary>
    /// Payment method type (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_method_type")]
    public string? PaymentMethodType { get; set; }

    /// <summary>
    /// Whether it's one stage payment (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("one_stage")]
    public bool OneStage { get; set; }
}

/// <summary>
/// Alternative payment operation result
/// </summary>
public class AlternativePaymentOperationResult
{
    /// <summary>
    /// Operation ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// External operation ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Operation status (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

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
    /// Payment method (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_method")]
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Operation creation date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("created_at")]
    [JsonConverter(typeof(FlexibleDateTimeConverter))]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Operation processing date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("processed_at")]
    [JsonConverter(typeof(NullableFlexibleDateTimeConverter))]
    public DateTime? ProcessedAt { get; set; }
}

/// <summary>
/// Alternative payment operations result
/// </summary>
public class AlternativePaymentOperationsResult
{
    /// <summary>
    /// List of operations (JSON array as per CDN documentation)
    /// </summary>
    [JsonPropertyName("operations")]
    public List<AlternativePaymentOperationResult>? Operations { get; set; }

    /// <summary>
    /// Total count of operations (JSON number as per CDN documentation)
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
/// Request for getting alternative payment operations
/// </summary>
public class GetAlternativePaymentOperationsRequest
{
    /// <summary>
    /// Date from filter (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("date_from")]
    public string? DateFrom { get; set; }

    /// <summary>
    /// Date to filter (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("date_to")]
    public string? DateTo { get; set; }

    /// <summary>
    /// Status filter (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Results limit (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    /// <summary>
    /// Results offset (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("offset")]
    public int? Offset { get; set; }
}

/// <summary>
/// Alternative payment response
/// </summary>
public class AlternativePaymentResponse
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
    /// Payment URL (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_url")]
    public string? PaymentUrl { get; set; }

    /// <summary>
    /// Payment method (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_method")]
    public string? PaymentMethod { get; set; }
}

/// <summary>
/// Alternative payment method info (JSON object as per CDN documentation)
/// </summary>
public class AlternativePaymentMethodInfo
{
    /// <summary>
    /// Payment method code (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Payment method name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Logo URL (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("logo_url")]
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Whether method is active (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
}

/// <summary>
/// Alternative payment methods response
/// </summary>
public class AlternativePaymentMethodsResponse
{
    /// <summary>
    /// Available payment methods (JSON array as per CDN documentation)
    /// </summary>
    [JsonPropertyName("methods")]
    public List<AlternativePaymentMethodInfo>? Methods { get; set; }
}

/// <summary>
/// Alternative payment status response
/// </summary>
public class AlternativePaymentStatusResponse
{
    /// <summary>
    /// Payment ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Payment status (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Status details (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status_details")]
    public string? StatusDetails { get; set; }
}

/// <summary>
/// Alternative payment info
/// </summary>
public class AlternativePaymentInfo
{
    /// <summary>
    /// Payment amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }
}

/// <summary>
/// Request for refunding alternative payment
/// </summary>
public class RefundAlternativePaymentRequest
{
    /// <summary>
    /// External payment ID to refund (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    [Required]
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Refund amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Refund reason (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// External refund ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_refund_id")]
    public string? ExternalRefundId { get; set; }
}

/// <summary>
/// Alternative payment callback operation type
/// </summary>
public enum AlternativePaymentCallbackOperation
{
    /// <summary>
    /// Callback for create operation
    /// </summary>
    Create,

    /// <summary>
    /// Callback for refund operation
    /// </summary>
    Refund
}

/// <summary>
/// Request to resend alternative payment callback
/// </summary>
public class ResendAlternativePaymentCallbackRequest
{
    /// <summary>
    /// External payment ID
    /// </summary>
    [Required]
    [JsonPropertyName("external_id")]
    public required string ExternalId { get; set; }

    /// <summary>
    /// Operation for which callback should be sent (create/refund)
    /// </summary>
    [JsonPropertyName("operation")]
    public AlternativePaymentCallbackOperation? Operation { get; set; }
}

/// <summary>
/// Response for alternative payment callback resend
/// </summary>
public class AlternativePaymentCallbackResendResponse
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
    public AlternativePaymentCallbackResendResponse()
    {
        Status = "success";
        Message = "Callback sent successfully";
    }
}

/// <summary>
/// Alternative payment operation response
/// </summary>
public class AlternativePaymentOperationResponse
{
    /// <summary>
    /// Operation ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// External operation ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Operation status (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

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
    /// Payment method (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_method")]
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Operation creation date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("created_at")]
    [JsonConverter(typeof(FlexibleDateTimeConverter))]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Operation processing date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("processed_at")]
    [JsonConverter(typeof(NullableFlexibleDateTimeConverter))]
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Operation details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("details")]
    public AlternativePaymentOperationDetails? Details { get; set; }
}

/// <summary>
/// Alternative payment operations response
/// </summary>
public class AlternativePaymentOperationsResponse
{
    /// <summary>
    /// List of operations (JSON array as per CDN documentation)
    /// </summary>
    [JsonPropertyName("operations")]
    public List<AlternativePaymentOperationResult>? Operations { get; set; }

    /// <summary>
    /// Total count of operations (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("total")]
    public int? Total { get; set; }

    /// <summary>
    /// Current count in response (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("count")]
    public int? Count { get; set; }
} 
