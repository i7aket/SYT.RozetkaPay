using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Models.Common;

namespace SYT.RozetkaPay.Models.Payments;

/// <summary>
/// Request for creating a payment
/// </summary>
public class CreatePaymentRequest
{
    /// <summary>
    /// Payment amount (JSON number as per CDN documentation)
    /// </summary>
    [JsonPropertyName("amount")]
    [Required]

    // [Required] on a value type can never fail - it was decorative, and 0 or -5 reached the
    // gateway. A price calculation that returns zero is a bug worth catching before it is a payment.
    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment currency (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("currency")]
    [Required]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// External ID to link payment in your system (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("external_id")]
    [Required]
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Payment mode (JSON string as per CDN documentation)
    /// </summary>
    /// <remarks>
    /// Nullable, and for the same reason <c>CustomerRequestPaymentMethod.Type</c> is: a non-nullable
    /// enum property defaults to its zero value, which here is <see cref="PaymentMode.Direct"/> — raw
    /// card acceptance, the PCI-scope flow. A caller who simply forgot the field was requesting it,
    /// silently, and <c>[Required]</c> on a value type cannot fail. Nullable plus <c>[Required]</c>
    /// turns the omission into the validation error it always should have been.
    /// </remarks>
    [JsonPropertyName("mode")]
    [Required]
    public PaymentMode? Mode { get; set; }

    /// <summary>
    /// Callback URL for payment notifications (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("callback_url")]
    public string? CallbackUrl { get; set; }

    /// <summary>
    /// Result URL for user redirection (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("result_url")]
    public string? ResultUrl { get; set; }

    /// <summary>
    /// Whether to confirm payment automatically (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("confirm")]
    public bool Confirm { get; set; } = true;

    /// <summary>
    /// Payment description (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Additional payload data (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    /// <summary>
    /// Customer information (JSON object as per CDN documentation)
    /// For hosted mode, payment_method is not required
    /// For direct mode, payment_method is required
    /// </summary>
    [JsonPropertyName("customer")]
    public CustomerRequestUserDetails? Customer { get; set; }

    /// <summary>
    /// Product list for express checkout (JSON array as per CDN documentation)
    /// </summary>
    [JsonPropertyName("products")]
    public List<Product>? Products { get; set; }

    /// <summary>
    /// Recipient information for P2P payments (JSON object as per CDN documentation)
    /// </summary>
    [JsonPropertyName("recipient")]
    public RecipientRequestUserDetails? Recipient { get; set; }

    /// <summary>
    /// Whether to initialize recurrent payment (JSON boolean as per CDN documentation)
    /// </summary>
    [JsonPropertyName("init_recurrent_payment")]
    public bool? InitRecurrentPayment { get; set; }

    /// <summary>
    /// Unified external ID (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("unified_external_id")]
    public string? UnifiedExternalId { get; set; }

    /// <summary>
    /// URL the payer is returned to after a successful checkout.
    /// </summary>
    [JsonPropertyName("result_url_success")]
    public string? ResultUrlSuccess { get; set; }

    /// <summary>
    /// URL the payer is returned to after a failed checkout.
    /// </summary>
    [JsonPropertyName("result_url_fail")]
    public string? ResultUrlFail { get; set; }

    /// <summary>
    /// How long the hosted checkout stays open, in seconds.
    /// </summary>
    [JsonPropertyName("checkout_ttl")]
    public decimal? CheckoutTtl { get; set; }

    /// <summary>
    /// Subscription this payment belongs to.
    /// </summary>
    [JsonPropertyName("subscription_id")]
    public string? SubscriptionId { get; set; }

    /// <summary>
    /// Whether the payer may choose the amount at checkout.
    /// </summary>
    [JsonPropertyName("use_custom_free_amount")]
    public bool? UseCustomFreeAmount { get; set; }

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
/// Customer block of a <b>response</b> body (document schema <c>CustomerInfo</c>): a flat six-field shape.
/// </summary>
/// <remarks>
/// Not to be confused with <see cref="CustomerRequestUserDetails"/>, the customer block of a <b>request</b>
/// (EXP-452). Until now one C# type served both, and that is exactly why the contract gates needed an alias
/// for it: the union of two schemas declares everything, so nothing looked wrong — while a caller setting
/// <c>Address</c> on an operation whose schema is this one had the field silently dropped, with no signature
/// to warn them.
/// <para>
/// This is a response shape, so there is nothing here for a caller to fill in.
/// </para>
/// </remarks>
public class CustomerInfo
{
    /// <summary>
    /// Browser user agent (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("browser_user_agent")]
    public string? BrowserUserAgent { get; set; }

    /// <summary>
    /// Email address (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// First name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    /// <summary>
    /// IP address (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Last name (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    /// <summary>
    /// Phone number (JSON string as per CDN documentation)
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
}

 