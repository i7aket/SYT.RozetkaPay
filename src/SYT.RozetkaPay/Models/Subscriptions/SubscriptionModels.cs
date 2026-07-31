using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Converters;

using SYT.RozetkaPay.Models.Common;

namespace SYT.RozetkaPay.Models.Subscriptions;

/// <summary>
/// Subscription state enumeration
/// </summary>
public enum SubscriptionState
{
    /// <summary>
    /// Subscription is initializing
    /// </summary>
    [JsonStringEnumMemberName("init")]
    Init,

    /// <summary>
    /// Subscription is processing
    /// </summary>
    [JsonStringEnumMemberName("processing")]
    Processing,

    /// <summary>
    /// Subscription is pending
    /// </summary>
    [JsonStringEnumMemberName("pending")]
    Pending,

    /// <summary>
    /// Subscription is active
    /// </summary>
    [JsonStringEnumMemberName("active")]
    Active,


    /// <summary>
    /// Subscription is inactive
    /// </summary>
    [JsonStringEnumMemberName("inactive")]
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
    [JsonStringEnumMemberName("unprocessed")]
    Unprocessed,

    /// <summary>
    /// Payment is processing
    /// </summary>
    [JsonStringEnumMemberName("processing")]
    Processing,

    /// <summary>
    /// Payment is processed
    /// </summary>
    [JsonStringEnumMemberName("processed")]
    Processed,

    /// <summary>
    /// Payment failed
    /// </summary>
    [JsonStringEnumMemberName("failed")]
    Failed
}

/// <summary>
/// Subscription callback type enumeration
/// </summary>
public enum SubscriptionCallbackType
{
    /// <summary>
    /// A subscription payment was processed.
    /// </summary>
    [JsonStringEnumMemberName("payment.processed")]
    PaymentProcessed,

    /// <summary>
    /// A subscription payment failed.
    /// </summary>
    [JsonStringEnumMemberName("payment.failed")]
    PaymentFailed,

    /// <summary>
    /// The subscription was deactivated.
    /// </summary>
    [JsonStringEnumMemberName("subscription.deactivated")]
    SubscriptionDeactivated,

    /// <summary>
    /// The subscription was renewed for another period.
    /// </summary>
    [JsonStringEnumMemberName("subscription.renewed")]
    SubscriptionRenewed,

    /// <summary>
    /// The subscription was cancelled.
    /// </summary>
    [JsonStringEnumMemberName("subscription.cancelled")]
    SubscriptionCancelled,

    /// <summary>
    /// A subscription payment was refunded.
    /// </summary>
    [JsonStringEnumMemberName("subscription.refunded")]
    SubscriptionRefunded,

    /// <summary>
    /// A subscription refund failed.
    /// </summary>
    [JsonStringEnumMemberName("subscription.refund_failed")]
    SubscriptionRefundFailed,

    /// <summary>
    /// The subscription was updated.
    /// </summary>
    [JsonStringEnumMemberName("subscription.updated")]
    SubscriptionUpdated,

    /// <summary>
    /// The subscription payment method was changed.
    /// </summary>
    [JsonStringEnumMemberName("subscription.payment_method_changed")]
    SubscriptionPaymentMethodChanged,

    /// <summary>
    /// The customer identifier on the subscription was updated.
    /// </summary>
    [JsonStringEnumMemberName("subscription.customer_id_updated")]
    SubscriptionCustomerIdUpdated
}

/// <summary>
/// Plan state enumeration
/// </summary>
public enum PlanState
{
    /// <summary>
    /// Plan is active
    /// </summary>
    [JsonStringEnumMemberName("active")]
    Active,

    /// <summary>
    /// Plan is inactive
    /// </summary>
    [JsonStringEnumMemberName("inactive")]
    Inactive
}

/// <summary>
/// Request for creating a subscription
/// </summary>
public class CreateSubscriptionRequest
{
    /// <summary>
    /// Customer the subscription is created for.
    /// </summary>
    [Required]
    [JsonPropertyName("customer")]
    public required CustomerRequestUserDetails Customer { get; set; }

    /// <summary>
    /// Plan the subscription follows.
    /// </summary>
    [Required]
    [JsonPropertyName("plan_id")]
    public required string PlanId { get; set; }

    /// <summary>
    /// URL the payer is returned to once the subscription is set up.
    /// </summary>
    [Required]
    [JsonPropertyName("result_url")]
    public required string ResultUrl { get; set; }

    /// <summary>
    /// Date the subscription starts.
    /// </summary>
    [Required]
    [JsonPropertyName("start_date")]
    public required string StartDate { get; set; }

    /// <summary>
    /// Whether the subscription renews automatically at the end of each period.
    /// </summary>
    [JsonPropertyName("auto_renew")]
    public bool? AutoRenew { get; set; }

    /// <summary>
    /// Callback URL for subscription notifications.
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// API key of the merchant the subscription is delegated to.
    /// </summary>
    [JsonPropertyName("delegate_api_key")]
    public string? DelegateApiKey { get; set; }

    /// <summary>
    /// Human-readable description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Identifier linking the subscription within the merchant's system.
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Identifier of an external premium entitlement.
    /// </summary>
    [JsonPropertyName("external_premium_id")]
    public string? ExternalPremiumId { get; set; }

    /// <summary>
    /// Price overriding the plan's, when the subscription is not billed at plan price.
    /// </summary>
    [JsonPropertyName("price")]
    public int? Price { get; set; }

    /// <summary>
    /// Recurrent identifier of an existing card mandate to bill.
    /// </summary>
    [JsonPropertyName("recurrent_id")]
    public string? RecurrentId { get; set; }

    /// <summary>
    /// Number of trial periods before billing begins.
    /// </summary>
    [JsonPropertyName("trial_periods")]
    public decimal? TrialPeriods { get; set; }

    /// <summary>
    /// Whether trial periods are themselves charged.
    /// </summary>
    [JsonPropertyName("trial_periodic_payments")]
    public bool? TrialPeriodicPayments { get; set; }

    /// <summary>
    /// Identifier linking this subscription to others across systems.
    /// </summary>
    [JsonPropertyName("unified_external_id")]
    public string? UnifiedExternalId { get; set; }

    /// <summary>
    /// Whether an automatic renewal is billed at the plan's price rather than this subscription's.
    /// </summary>
    [JsonPropertyName("use_plan_price_on_auto_renew")]
    public bool? UsePlanPriceOnAutoRenew { get; set; }

    /// <summary>
    /// Number of periods granted as a gift.
    /// </summary>
    [JsonPropertyName("gifted_periods")]
    public decimal? GiftedPeriods { get; set; }
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
    /// Provider field &lt;c&gt;apple_pay&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("apple_pay")]
    public CustomerAppleGooglePayRequestPaymentMethod? ApplePay { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;cc_token&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("cc_token")]
    public CustomerCCTokenRequestPaymentMethod? CcToken { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;google_pay&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("google_pay")]
    public CustomerAppleGooglePayRequestPaymentMethod? GooglePay { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;wallet&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("wallet")]
    public CustomerWalletRequestPaymentMethod? Wallet { get; set; }

    /// <summary>
    /// Recurrent payment identifier, required when the type is a recurrent mandate.
    /// </summary>
    [JsonPropertyName("recurrent_id")]
    public string? RecurrentId { get; set; }
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

    /// <summary>
    /// The next date-time when the subscription auto_renew param can be changed.
    /// </summary>
    [JsonPropertyName("auto_renew_locked_until")]
    public DateTime? AutoRenewLockedUntil { get; set; }
    /// <summary>
    /// Customer identifier. May hold RID value.
    /// </summary>
    [JsonPropertyName("customer_id")]
    public string? CustomerId { get; set; }
    /// <summary>
    /// Project API key where recurrent payments are delegated.
    /// </summary>
    [JsonPropertyName("delegate_api_key")]
    public Guid? DelegateApiKey { get; set; }
    /// <summary>
    /// Identifier to link the subscription within partner system. Currently used by Rozetka only.
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }
    /// <summary>
    /// Identifier to link the subscription within partner system. Currently used by Rozetka only.
    /// </summary>
    [JsonPropertyName("external_premium_id")]
    public string? ExternalPremiumId { get; set; }
    /// <summary>
    /// The date-time when the subscription will finally expire
    /// </summary>
    [JsonPropertyName("final_expiration_date")]
    public DateTime? FinalExpirationDate { get; set; }
    /// <summary>
    /// A marker that indicates whether the subscription is currently a gift subscription.
    /// </summary>
    [JsonPropertyName("gifted")]
    public bool? Gifted { get; set; }
    /// <summary>
    /// The start date of the gift subscription.
    /// </summary>
    [JsonPropertyName("gifted_from")]
    public DateTime? GiftedFrom { get; set; }
    /// <summary>
    /// Identifier to link the gifted subscription within partner system.
    /// </summary>
    [JsonPropertyName("gifted_unified_external_id")]
    public string? GiftedUnifiedExternalId { get; set; }
    /// <summary>
    /// The end date of the gift subscription.
    /// </summary>
    [JsonPropertyName("gifted_until")]
    public DateTime? GiftedUntil { get; set; }
    /// <summary>
    /// Shows whether the subscription in currently retrying its renew payment.
    /// </summary>
    [JsonPropertyName("is_retrying")]
    public bool? IsRetrying { get; set; }
    /// <summary>
    /// Source project for the current subscription.
    /// </summary>
    [JsonPropertyName("project_id")]
    public Guid? ProjectId { get; set; }
    /// <summary>
    /// The recurrent id for the subscription payments.
    /// </summary>
    [JsonPropertyName("recurrent_id")]
    public string? RecurrentId { get; set; }
    /// <summary>
    /// The time of day component for the params that have date specified only.
    /// </summary>
    [JsonPropertyName("time_of_day")]
    public DateTime? TimeOfDay { get; set; }
    /// <summary>
    /// Specifies, whether to apply periodic payments for trial subscription
    /// </summary>
    [JsonPropertyName("trial_periodic_payments")]
    public bool? TrialPeriodicPayments { get; set; }
    /// <summary>
    /// The amount of trial periods assigned to the subscription.
    /// </summary>
    [JsonPropertyName("trial_periods")]
    public decimal? TrialPeriods { get; set; }
    /// <summary>
    /// The end date-time of the subscription trial.
    /// </summary>
    [JsonPropertyName("trial_until")]
    public DateTime? TrialUntil { get; set; }
    /// <summary>
    /// Identifier to link the subscription within partner system. Currently used by Rozetka only.
    /// </summary>
    [JsonPropertyName("unified_external_id")]
    public string? UnifiedExternalId { get; set; }
}

/// <summary>
/// Optional query parameters of the official <c>CancelCustomerSubscription</c> operation
/// (<c>DELETE /api/subscriptions/v1/subscriptions/{subscription_id}/cancel</c>).
/// </summary>
/// <remarks>
/// <para>
/// This object is never serialized. Both members are rendered as URL query parameters, which is why
/// they carry no <c>JsonPropertyName</c> annotation: the operation sends no request body at all.
/// </para>
/// <para>
/// A <see langword="null"/> member is omitted from the request target; a non-null member is always
/// sent. An empty <see cref="ExternalId"/> is therefore not the same as an absent one - it is sent
/// as <c>external_id=</c> and validated by the provider.
/// </para>
/// </remarks>
public class CancelCustomerSubscriptionOptions
{
    /// <summary>
    /// Customer identifier in the caller's system, sent as the <c>external_id</c> query parameter.
    /// Pass the raw value: the SDK percent-encodes it exactly once. Leave <see langword="null"/> to
    /// omit the parameter and identify the customer through the <c>X-CUSTOMER-AUTH</c> header instead.
    /// </summary>
    public string? ExternalId { get; set; }

    /// <summary>
    /// Refund option of the cancellation command, sent as the <c>refund</c> query parameter in the
    /// lowercase spelling the provider documents. This is a transient instruction carried by this one
    /// request, not a stored fact about the subscription. Leave <see langword="null"/> to omit the
    /// parameter and let the provider apply its default.
    /// </summary>
    public bool? Refund { get; set; }
}





/// <summary>
/// Request to update subscription
/// </summary>
public class UpdateSubscriptionRequest
{
    /// <summary>Whether the subscription renews automatically at the end of each period.</summary>
    [JsonPropertyName("auto_renew")]
    public bool? AutoRenew { get; set; }

    /// <summary>Date up to which the subscription is granted free of charge.</summary>
    [JsonPropertyName("gifted_until")]
    public string? GiftedUntil { get; set; }
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

    /// <summary>
    /// The number of frequency_types in a plan duration period.
    /// </summary>
    [JsonPropertyName("duration_periods")]
    public int? DurationPeriods { get; set; }
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

    /// <summary>
    /// Provider field &lt;c&gt;auth_code&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("auth_code")]
    public string? AuthCode { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;bank_name&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("bank_name")]
    public string? BankName { get; set; }
    /// <summary>
    /// Description for the plan.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;meta&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("meta")]
    public SubscriptionPaymentMeta? Meta { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;mid&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("mid")]
    public string? Mid { get; set; }
    /// <summary>
    /// Date-time of next payment processing attempt in RFC3339 format.
    /// </summary>
    [JsonPropertyName("next_processing_date")]
    public DateTime? NextProcessingDate { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;recipient_iban&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("recipient_iban")]
    public string? RecipientIban { get; set; }
    /// <summary>
    /// True when this payment is a verify-only payment-method-change charge.
    /// </summary>
    [JsonPropertyName("refund_on_processed")]
    public bool? RefundOnProcessed { get; set; }
    /// <summary>
    /// The count of payment retries performed after failed first attempt.
    /// </summary>
    [JsonPropertyName("retry_count")]
    public int? RetryCount { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;rrn&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("rrn")]
    public string? Rrn { get; set; }
    /// <summary>
    /// Provider field &lt;c&gt;status_code&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("status_code")]
    public ResponseCode? StatusCode { get; set; }
    /// <summary>
    /// Description of status_code value.
    /// </summary>
    [JsonPropertyName("status_description")]
    public string? StatusDescription { get; set; }
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
    /// <summary>
    /// Unique transaction id for payment (null for a gifted subscription or failed payment operation)
    /// </summary>
    [JsonPropertyName("transaction_id")]
    public Guid? TransactionId { get; set; }
    /// <summary>
    /// Last date-time of payment data update in RFC3339 format.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
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

    /// <summary>
    /// Provider field &lt;c&gt;details&lt;/c&gt;.
    /// </summary>
    [JsonPropertyName("details")]
    public SubscriptionPaymentDetails? Details { get; set; }
    /// <summary>
    /// Subscription identifier.
    /// </summary>
    [JsonPropertyName("subscription_id")]
    public Guid? SubscriptionId { get; set; }
    /// <summary>
    /// Object which contains information about required post-request action. Will be null if action not required.
    /// </summary>
    [JsonPropertyName("user_action")]
    public UserAction? UserAction { get; set; }
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
/// Subscription list response (OpenAPI schema).
/// </summary>
/// <remarks>
/// The official <c>getSubscriptions</c> response is a root JSON array, not a wrapper object.
/// <see cref="SubscriptionListJsonConverter"/> maps that array onto <see cref="Subscriptions"/> while
/// still accepting the historical wrapper spelling, so this public shape stays source and binary
/// compatible.
/// </remarks>
[JsonConverter(typeof(SubscriptionListJsonConverter))]
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
    [JsonPropertyName("platforms")]
    public List<string>? Platforms { get; set; }

    /// <summary>
    /// Frequency type
    /// </summary>
    [Required]
    [JsonPropertyName("frequency_type")]
    public required PlanFrequencyType FrequencyType { get; set; }

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

    /// <summary>
    /// Number of periods the plan runs for.
    /// </summary>
    [Required]
    [JsonPropertyName("duration_periods")]
    public required int DurationPeriods { get; set; }

    /// <summary>
    /// Callback endpoints registered on the plan.
    /// </summary>
    [JsonPropertyName("callbacks")]
    public List<PlanCallbackRequest>? Callbacks { get; set; }
}

/// <summary>
/// Request to update a subscription plan (OpenAPI schema)
/// </summary>
public class UpdatePlanRequest
{
    /// <summary>Plan name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Plan description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Plan price.</summary>
    [JsonPropertyName("price")]
    public int? Price { get; set; }

    /// <summary>Platforms the plan is offered on.</summary>
    [JsonPropertyName("platforms")]
    public List<string>? Platforms { get; set; }

    /// <summary>Date the plan stops being offered.</summary>
    [JsonPropertyName("end_date")]
    public string? EndDate { get; set; }

    /// <summary>Number of periods the plan runs for.</summary>
    [JsonPropertyName("duration_periods")]
    public int? DurationPeriods { get; set; }

    /// <summary>How many intervals make one period.</summary>
    [JsonPropertyName("frequency")]
    public int? Frequency { get; set; }

    /// <summary>The interval a period is measured in.</summary>
    [JsonPropertyName("frequency_type")]
    public PlanFrequencyType? FrequencyType { get; set; }

    /// <summary>Callback endpoints registered on the plan.</summary>
    [JsonPropertyName("callbacks")]
    public List<PlanCallbackRequest>? Callbacks { get; set; }
}

/// <summary>
/// The time interval a plan period is measured in.
/// </summary>
/// <remarks>
/// Combined with the plan's frequency: four weeks is <c>frequency=4</c> with
/// <see cref="Weekly"/>.
/// </remarks>
public enum PlanFrequencyType
{
    /// <summary>Days.</summary>
    [JsonStringEnumMemberName("daily")]
    Daily,

    /// <summary>Weeks.</summary>
    [JsonStringEnumMemberName("weekly")]
    Weekly,

    /// <summary>Months.</summary>
    [JsonStringEnumMemberName("monthly")]
    Monthly,

    /// <summary>Years.</summary>
    [JsonStringEnumMemberName("yearly")]
    Yearly
}

/// <summary>
/// A callback endpoint registered on a subscription plan.
/// </summary>
public class PlanCallbackRequest
{
    /// <summary>API key the callback is authenticated with.</summary>
    [JsonPropertyName("api_key")]
    public string? ApiKey { get; set; }

    /// <summary>URL the callback is delivered to.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
