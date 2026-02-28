using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Converters;

namespace SYT.RozetkaPay.Models.Subscriptions;

/// <summary>
/// Subscription state enumeration
/// </summary>
public enum SubscriptionState
{
    /// <summary>
    /// Subscription is initializing
    /// </summary>
    [JsonPropertyName("init")]
    Init,
    
    /// <summary>
    /// Subscription is processing
    /// </summary>
    [JsonPropertyName("processing")]
    Processing,
    
    /// <summary>
    /// Subscription is pending
    /// </summary>
    [JsonPropertyName("pending")]
    Pending,
    
    /// <summary>
    /// Subscription is active
    /// </summary>
    [JsonPropertyName("active")]
    Active,
    
    /// <summary>
    /// Subscription is paused
    /// </summary>
    [JsonPropertyName("paused")]
    Paused,
    
    /// <summary>
    /// Subscription is inactive
    /// </summary>
    [JsonPropertyName("inactive")]
    Inactive
}

/// <summary>
/// Subscription payment state enumeration
/// </summary>
public enum SubscriptionPaymentState
{
    /// <summary>
    /// Payment is unprocessed
    /// </summary>
    [JsonPropertyName("unprocessed")]
    Unprocessed,
    
    /// <summary>
    /// Payment is processing
    /// </summary>
    [JsonPropertyName("processing")]
    Processing,
    
    /// <summary>
    /// Payment is processed
    /// </summary>
    [JsonPropertyName("processed")]
    Processed,
    
    /// <summary>
    /// Payment failed
    /// </summary>
    [JsonPropertyName("failed")]
    Failed
}

/// <summary>
/// Subscription callback type enumeration
/// </summary>
public enum SubscriptionCallbackType
{
    /// <summary>
    /// Payment callback
    /// </summary>
    [JsonPropertyName("payment")]
    Payment,
    
    /// <summary>
    /// Subscription status change
    /// </summary>
    [JsonPropertyName("status_change")]
    StatusChange,
    
    /// <summary>
    /// Subscription expiry
    /// </summary>
    [JsonPropertyName("expiry")]
    Expiry
}

/// <summary>
/// Plan state enumeration
/// </summary>
public enum PlanState
{
    /// <summary>
    /// Plan is active
    /// </summary>
    [JsonPropertyName("active")]
    Active,
    
    /// <summary>
    /// Plan is inactive
    /// </summary>
    [JsonPropertyName("inactive")]
    Inactive
}

/// <summary>
/// Request for creating a subscription
/// </summary>
public class CreateSubscriptionRequest
{
    /// <summary>
    /// External subscription ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    [Required]
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Subscription amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    [Required]
    public decimal Amount { get; set; }

    /// <summary>
    /// Subscription currency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("currency")]
    [Required]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Subscription description (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Payment frequency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("frequency")]
    [Required]
    public string Frequency { get; set; } = string.Empty;

    /// <summary>
    /// Number of periods (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("period_count")]
    public int? PeriodCount { get; set; }

    /// <summary>
    /// Customer information (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("customer")]
    public SubscriptionCustomer? Customer { get; set; }

    /// <summary>
    /// Initial payment information (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("initial_payment")]
    public SubscriptionInitialPayment? InitialPayment { get; set; }

    /// <summary>
    /// Callback URL (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Subscription start date (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("start_date")]
    public string? StartDate { get; set; }
}

/// <summary>
/// Request for creating a gifted subscription
/// </summary>
public class GiftSubscriptionRequest
{
    /// <summary>
    /// Controls whether the subscription should automatically renew
    /// </summary>
    [JsonPropertyName("auto_renew")]
    public bool? AutoRenew { get; set; }

    /// <summary>
    /// URL where asynchronous subscription updates will be sent
    /// </summary>
    [Required]
    [JsonPropertyName("callback_url")]
    public required string CallbackUrl { get; set; }

    /// <summary>
    /// Customer information
    /// </summary>
    [Required]
    [JsonPropertyName("customer")]
    public required SubscriptionCustomer Customer { get; set; }

    /// <summary>
    /// Project API key to delegate recurrent payments to
    /// </summary>
    [JsonPropertyName("delegate_api_key")]
    public string? DelegateApiKey { get; set; }

    /// <summary>
    /// Description for the subscription
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Identifier to link the subscription within partner system
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Identifier to link the subscription premium within partner system
    /// </summary>
    [JsonPropertyName("external_premium_id")]
    public string? ExternalPremiumId { get; set; }

    /// <summary>
    /// The corresponding plan identifier
    /// </summary>
    [Required]
    [JsonPropertyName("plan_id")]
    public required string PlanId { get; set; }

    /// <summary>
    /// Subscription price in main currency units
    /// </summary>
    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    /// <summary>
    /// Recurrent ID for subscription payments
    /// </summary>
    [Required]
    [JsonPropertyName("recurrent_id")]
    public required string RecurrentId { get; set; }

    /// <summary>
    /// URL where user will be redirected after successful payment
    /// </summary>
    [Required]
    [JsonPropertyName("result_url")]
    public required string ResultUrl { get; set; }

    /// <summary>
    /// Start date of the subscription in RFC3339 format, UTC timezone
    /// </summary>
    [Required]
    [JsonPropertyName("start_date")]
    public required string StartDate { get; set; }

    /// <summary>
    /// Amount of trial periods assigned to the subscription
    /// </summary>
    [JsonPropertyName("trial_periods")]
    [Obsolete("trial_periods is deprecated by API and may be removed in the future.")]
    public int? TrialPeriods { get; set; }

    /// <summary>
    /// Identifier to link the subscription within partner system
    /// </summary>
    [JsonPropertyName("unified_external_id")]
    public string? UnifiedExternalId { get; set; }

    /// <summary>
    /// Whether plan price should be used for auto renew
    /// </summary>
    [JsonPropertyName("use_plan_price_on_auto_renew")]
    public bool? UsePlanPriceOnAutoRenew { get; set; }

    /// <summary>
    /// Amount of gifted periods assigned to the subscription
    /// </summary>
    [JsonPropertyName("gifted_periods")]
    public int? GiftedPeriods { get; set; }

    /// <summary>
    /// Identifier to link the gifted subscription within partner system
    /// </summary>
    [JsonPropertyName("gifted_unified_external_id")]
    public string? GiftedUnifiedExternalId { get; set; }
}

/// <summary>
/// Customer information for subscription
/// </summary>
public class SubscriptionCustomer
{
    /// <summary>
    /// Customer email (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Customer first name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    /// <summary>
    /// Customer last name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    /// <summary>
    /// Customer phone (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
}

/// <summary>
/// Initial payment information for subscription
/// </summary>
public class SubscriptionInitialPayment
{
    /// <summary>
    /// Initial payment amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Payment method information (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payment_method")]
    public SubscriptionPaymentMethod? PaymentMethod { get; set; }
}

/// <summary>
/// Payment method for subscription
/// </summary>
public class SubscriptionPaymentMethod
{
    /// <summary>
    /// Payment method type (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("type")]
    [Required]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Card details (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("card")]
    public SubscriptionCard? Card { get; set; }

    /// <summary>
    /// Recurrent token (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("recurrent_token")]
    public string? RecurrentToken { get; set; }
}

/// <summary>
/// Card information for subscription
/// </summary>
public class SubscriptionCard
{
    /// <summary>
    /// Card number (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("number")]
    [Required]
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Card expiration month (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("exp_month")]
    [Required]
    public int ExpirationMonth { get; set; }

    /// <summary>
    /// Card expiration year (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("exp_year")]
    [Required]
    public int ExpirationYear { get; set; }

    /// <summary>
    /// Card CVV (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("cvv")]
    [Required]
    public string CVV { get; set; } = string.Empty;

    /// <summary>
    /// Card holder name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("holder_name")]
    public string? HolderName { get; set; }
}

/// <summary>
/// Subscription information
/// </summary>
public class Subscription
{
    /// <summary>
    /// Whether subscription auto-renews (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("auto_renew")]
    public bool AutoRenew { get; set; }

    /// <summary>
    /// Callback URL (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Subscription creation date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("created_at")]
    [JsonConverter(typeof(FlexibleDateTimeConverter))]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Subscription currency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Subscription description (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Subscription due date (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("due_date")]
    public string? DueDate { get; set; }

    /// <summary>
    /// Subscription ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Next notification date (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("next_notification_date")]
    public string? NextNotificationDate { get; set; }

    /// <summary>
    /// Next payment date (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("next_payment_date")]
    public string? NextPaymentDate { get; set; }

    /// <summary>
    /// Plan ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("plan_id")]
    public string? PlanId { get; set; }

    /// <summary>
    /// Subscription price (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    /// <summary>
    /// Result URL (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("result_url")]
    public string? ResultUrl { get; set; }

    /// <summary>
    /// Subscription start date (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("start_date")]
    public string? StartDate { get; set; }

    /// <summary>
    /// Subscription state (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("state")]
    public SubscriptionState? State { get; set; }

    /// <summary>
    /// Subscription update date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("updated_at")]
    [JsonConverter(typeof(FlexibleDateTimeConverter))]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Whether to use plan price on auto-renew (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("use_plan_price_on_auto_renew")]
    public bool UsePlanPriceOnAutoRenew { get; set; }
}

/// <summary>
/// Subscription response
/// </summary>
public class SubscriptionResponse
{
    /// <summary>
    /// Subscription ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// External subscription ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Subscription status (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Subscription amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Subscription currency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Subscription description (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Payment frequency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("frequency")]
    public string? Frequency { get; set; }

    /// <summary>
    /// Number of periods (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("period_count")]
    public int? PeriodCount { get; set; }

    /// <summary>
    /// Number of completed payments (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("completed_payments")]
    public int? CompletedPayments { get; set; }

    /// <summary>
    /// Customer information (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("customer")]
    public SubscriptionCustomer? Customer { get; set; }

    /// <summary>
    /// Subscription creation date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("created_at")]
    [JsonConverter(typeof(FlexibleDateTimeConverter))]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Next payment date (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("next_payment_date")]
    public string? NextPaymentDate { get; set; }

    /// <summary>
    /// Error information (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("error")]
    public SubscriptionError? Error { get; set; }
}

/// <summary>
/// Subscription error information
/// </summary>
public class SubscriptionError
{
    /// <summary>
    /// Error code (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Error message (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Request to cancel subscription
/// </summary>
public class CancelSubscriptionRequest
{
    /// <summary>
    /// External subscription ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    [Required]
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Cancellation reason (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// Whether to cancel immediately (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("immediate")]
    public bool Immediate { get; set; } = false;
}

/// <summary>
/// Request to list subscriptions
/// </summary>
public class SubscriptionListRequest
{
    /// <summary>
    /// Customer ID filter (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("customer_id")]
    public string? CustomerId { get; set; }

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
    /// Email filter (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

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
/// Subscription list response
/// </summary>
public class SubscriptionListResponse
{
    /// <summary>
    /// List of subscriptions (JSON array as per CDN documentation)
    /// </summary>
    [JsonPropertyName("subscriptions")]
    public List<SubscriptionResponse>? Subscriptions { get; set; }

    /// <summary>
    /// Total count of subscriptions (JSON number as per CDN documentation)
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
/// Request to create subscription plan
/// </summary>
public class CreateSubscriptionPlanRequest
{
    /// <summary>
    /// Plan name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("name")]
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Plan description (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Plan price (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    [Required]
    public decimal Amount { get; set; }

    /// <summary>
    /// Plan currency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("currency")]
    [Required]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Payment frequency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("frequency")]
    [Required]
    public string Frequency { get; set; } = string.Empty;

    /// <summary>
    /// Trial days (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("trial_days")]
    public int? TrialDays { get; set; }
}

/// <summary>
/// Request to update subscription plan
/// </summary>
public class UpdateSubscriptionPlanRequest
{
    /// <summary>
    /// Plan name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Plan description (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Plan price (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Plan currency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Payment frequency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("frequency")]
    public string? Frequency { get; set; }

    /// <summary>
    /// Trial days (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("trial_days")]
    public int? TrialDays { get; set; }
}

/// <summary>
/// Request to update subscription
/// </summary>
public class UpdateSubscriptionRequest
{
    /// <summary>
    /// Subscription amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Payment frequency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("frequency")]
    public string? Frequency { get; set; }

    /// <summary>
    /// Number of periods (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("period_count")]
    public int? PeriodCount { get; set; }

    /// <summary>
    /// Subscription description (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Subscription plan response
/// </summary>
public class SubscriptionPlanResponse
{
    /// <summary>
    /// Plan ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Plan name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Plan description (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Plan price (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Plan currency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Payment frequency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("frequency")]
    public string? Frequency { get; set; }

    /// <summary>
    /// Trial days (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("trial_days")]
    public int? TrialDays { get; set; }

    /// <summary>
    /// Plan status (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Plan creation date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("created_at")]
    [JsonConverter(typeof(FlexibleDateTimeConverter))]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Plan update date (ISO 8601 format)
    /// </summary>
    [JsonPropertyName("updated_at")]
    [JsonConverter(typeof(FlexibleDateTimeConverter))]
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Subscription plans response
/// </summary>
public class SubscriptionPlansResponse
{
    /// <summary>
    /// List of plans (JSON array as per CDN documentation)
    /// </summary>
    [JsonPropertyName("plans")]
    public List<SubscriptionPlanResponse>? Plans { get; set; }

    /// <summary>
    /// Total count of plans (JSON number as per CDN documentation)
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
/// Customer subscriptions response
/// </summary>
public class CustomerSubscriptionsResponse
{
    /// <summary>
    /// List of subscriptions (JSON array as per CDN documentation)
    /// </summary>
    [JsonPropertyName("subscriptions")]
    public List<SubscriptionResponse>? Subscriptions { get; set; }

    /// <summary>
    /// Total count of subscriptions (JSON number as per CDN documentation)
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
/// Subscription payments response
/// </summary>
public class SubscriptionPaymentsResponse
{
    /// <summary>
    /// List of payments (JSON array as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payments")]
    public List<SubscriptionPaymentInfo>? Payments { get; set; }

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
/// Subscription payment information
/// </summary>
public class SubscriptionPaymentInfo
{
    /// <summary>
    /// Payment ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

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
    /// Payment status (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

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
/// Plan information
/// </summary>
public class Plan
{
    /// <summary>
    /// Plan creation date (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>
    /// Plan currency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Plan description (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Plan end date (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("end_date")]
    public string? EndDate { get; set; }

    /// <summary>
    /// Payment frequency (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("frequency")]
    public int? Frequency { get; set; }

    /// <summary>
    /// Frequency type (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("frequency_type")]
    public string? FrequencyType { get; set; }

    /// <summary>
    /// Plan ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Plan state (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("state")]
    public PlanState? State { get; set; }

    /// <summary>
    /// Plan name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Supported platforms (JSON array as per CDN documentation)
    /// </summary>
    [JsonPropertyName("platforms")]
    public List<string>? Platforms { get; set; }

    /// <summary>
    /// Plan price (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    /// <summary>
    /// Plan start date (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("start_date")]
    public string? StartDate { get; set; }

    /// <summary>
    /// Plan update date (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
}

/// <summary>
/// Subscription payment details
/// </summary>
public class SubscriptionPaymentDetails
{
    /// <summary>
    /// Payment ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

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
    /// Payment status (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

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
/// Subscription payment
/// </summary>
public class SubscriptionPayment
{
    /// <summary>
    /// Payment ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

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
    /// Payment status (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

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
/// Response for creating subscription (OpenAPI schema)
/// </summary>
public class CreateSubscriptionResponse
{
    /// <summary>
    /// Payment information
    /// </summary>
    [JsonPropertyName("payment")]
    public SubscriptionPayment? Payment { get; set; }

    /// <summary>
    /// Subscription information
    /// </summary>
    [JsonPropertyName("subscription")]
    public Subscription? Subscription { get; set; }
}

/// <summary>
/// Plan list response (OpenAPI schema)
/// </summary>
public class PlanList
{
    /// <summary>
    /// List of plans
    /// </summary>
    [JsonPropertyName("plans")]
    public List<Plan>? Plans { get; set; }
}

/// <summary>
/// Subscription list response (OpenAPI schema)
/// </summary>
public class SubscriptionList
{
    /// <summary>
    /// List of subscriptions
    /// </summary>
    [JsonPropertyName("subscriptions")]
    public List<Subscription>? Subscriptions { get; set; }
}

/// <summary>
/// Subscription payment list response (OpenAPI schema)
/// </summary>
public class SubscriptionPaymentList
{
    /// <summary>
    /// List of subscription payments
    /// </summary>
    [JsonPropertyName("payments")]
    public List<SubscriptionPayment>? Payments { get; set; }
}

/// <summary>
/// Request to create a subscription plan (OpenAPI schema)
/// </summary>
public class CreatePlanRequest
{
    /// <summary>
    /// Plan name
    /// </summary>
    [Required]
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Plan description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Plan price
    /// </summary>
    [Required]
    [JsonPropertyName("price")]
    public required decimal Price { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    [Required]
    [JsonPropertyName("currency")]
    public required string Currency { get; set; }

    /// <summary>
    /// Platforms where plan is available
    /// </summary>
    [Required]
    [JsonPropertyName("platforms")]
    public required List<string> Platforms { get; set; }

    /// <summary>
    /// Frequency type
    /// </summary>
    [Required]
    [JsonPropertyName("frequency_type")]
    public required string FrequencyType { get; set; }

    /// <summary>
    /// Frequency value
    /// </summary>
    [Required]
    [JsonPropertyName("frequency")]
    public required decimal Frequency { get; set; }

    /// <summary>
    /// Plan start date
    /// </summary>
    [Required]
    [JsonPropertyName("start_date")]
    public required string StartDate { get; set; }

    /// <summary>
    /// Plan end date
    /// </summary>
    [JsonPropertyName("end_date")]
    public string? EndDate { get; set; }
}

/// <summary>
/// Request to update a subscription plan (OpenAPI schema)
/// </summary>
public class UpdatePlanRequest
{
    /// <summary>
    /// Plan description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Plan name
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
} 
