using System.Text.Json.Serialization;
using SYT.RozetkaPay.Converters;
using SYT.RozetkaPay.Models.Common;

namespace SYT.RozetkaPay.Models.Payments;

/// <summary>
/// RozetkaPay webhook callback payload
/// </summary>
/// <remarks>
/// The document's processing callback (<c>CPAYProcessingCallback</c>, aliased as
/// <c>CardPayProcessingCallback</c>) posts a <c>PaymentOperationResult</c>. This type is the SDK's historical,
/// lenient shape of that body — strings where the response model has enums, a few fields the document does not
/// declare — and carries every field the document declares for it, all nullable, so a field the provider omits
/// reads as <see langword="null"/>. <c>WebhookModelCoverageTests</c> keeps it that way.
/// </remarks>
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

    /// <summary>
    /// External identifier of the batch the payment belongs to (<c>batch_external_id</c>), for batch payments.
    /// </summary>
    [JsonPropertyName("batch_external_id")]
    public string? BatchExternalId { get; set; }

    /// <summary>
    /// External ID of the parent order (<c>child_of</c>). Empty when <see cref="HasChild"/> is
    /// <see langword="true"/>. Added to the published schema on 2026-09-30.
    /// </summary>
    [JsonPropertyName("child_of")]
    public string? ChildOf { get; set; }

    /// <summary>
    /// Whether this order has a child order (<c>has_child</c>); <see langword="false"/> when
    /// <see cref="ChildOf"/> carries a value, <see langword="null"/> when the provider omitted it. Added to the
    /// published schema on 2026-09-30.
    /// </summary>
    [JsonPropertyName("has_child")]
    public bool? HasChild { get; set; }

    /// <summary>
    /// Merchant-defined data sent with the payment (<c>metadata</c>).
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// A key that identifies this delivery, for deduplication.
    /// </summary>
    /// <remarks>
    /// <para>
    /// RozetkaPay retries any delivery it does not see answered with a <c>200</c>, so a handler will
    /// see the same event more than once and must be able to tell a repeat from a new event. Nothing
    /// in the payload said which field to use, and the obvious choice is wrong: <see cref="Id"/> is
    /// the <em>payment</em> identifier, identical across every event for that payment. Deduplicating
    /// on it drops the refund notification for a payment already seen.
    /// </para>
    /// <para>
    /// The tuple that does distinguish deliveries is the payment, the operation, and the operation's
    /// own identifier. <c>Details.OperationId</c> is nullable, so the key falls back to the pair
    /// without it — two events of different kinds for one payment stay distinct, while two deliveries
    /// of the same event collapse, which is the property deduplication needs.
    /// </para>
    /// <para>
    /// The operation's state is part of the key, and it has to be. One operation can be delivered
    /// more than once as it progresses — a pending or 3-D Secure callback, then the final one — and a
    /// key without the status collapses them, so a consumer following the advice below would reject
    /// the final delivery and never mark the payment paid. The dedup that was meant to protect the
    /// booking would lose it instead.
    /// </para>
    /// <para>
    /// Storing this key and rejecting a repeat has to happen in the same transaction as the state
    /// change it guards. The SDK can name the key; only the consumer can make the check atomic.
    /// </para>
    /// </remarks>
    [JsonIgnore]
    public string EventKey =>
        string.Join(
            '|',
            Id ?? string.Empty,
            Operation ?? string.Empty,
            Details?.OperationId ?? string.Empty,
            Details?.Status ?? string.Empty,
            IsSuccess ? "1" : "0");
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

    /// <summary>
    /// Customer-facing description of the operation status in English (<c>status_description_en</c>). Added
    /// to the published schema on 2026-09-30.
    /// </summary>
    [JsonPropertyName("status_description_en")]
    public string? StatusDescriptionEn { get; set; }

    /// <summary>
    /// Customer-facing description of the operation status in Ukrainian (<c>status_description_uk</c>).
    /// Added to the published schema on 2026-09-30.
    /// </summary>
    [JsonPropertyName("status_description_uk")]
    public string? StatusDescriptionUk { get; set; }

    /// <summary>
    /// Account of the transfer beneficiary (<c>recipient_iban</c>). For a non-contractual credit transfer this
    /// is the account named in <see cref="RecipientName"/>; otherwise it is the card2iban destination.
    /// </summary>
    [JsonPropertyName("recipient_iban")]
    public string? RecipientIban { get; set; }

    /// <summary>
    /// Beneficiary of a non-contractual credit transfer, for example a treasury or budget payment
    /// (<c>recipient_name</c>). Present only for such payments. Added to the published schema on 2026-09-30.
    /// </summary>
    [JsonPropertyName("recipient_name")]
    public string? RecipientName { get; set; }

    /// <summary>
    /// Business registration number (ЄДРПОУ) of the beneficiary (<c>recipient_tin</c>). Added to the
    /// published schema on 2026-09-30.
    /// </summary>
    [JsonPropertyName("recipient_tin")]
    public string? RecipientTin { get; set; }

    /// <summary>
    /// Bank holding the beneficiary's account (<c>recipient_bank_name</c>). Reported separately from
    /// <see cref="BankName"/>, which is always the acquiring bank of the terminal. Added to the published
    /// schema on 2026-09-30.
    /// </summary>
    [JsonPropertyName("recipient_bank_name")]
    public string? RecipientBankName { get; set; }

    /// <summary>
    /// Identifier of a successful recurrent-initiating payment (<c>recurrent_id</c>).
    /// </summary>
    [JsonPropertyName("recurrent_id")]
    public string? RecurrentId { get; set; }

    /// <summary>
    /// Merchant identifier (<c>mid</c>).
    /// </summary>
    [JsonPropertyName("mid")]
    public string? Mid { get; set; }

    /// <summary>
    /// Terminal identifier (<c>tid</c>).
    /// </summary>
    [JsonPropertyName("tid")]
    public string? Tid { get; set; }

    /// <summary>
    /// Subscription the payment belongs to (<c>subscription_id</c>).
    /// </summary>
    [JsonPropertyName("subscription_id")]
    public string? SubscriptionId { get; set; }

    /// <summary>
    /// Fiscalization details (<c>fiscalization</c>).
    /// </summary>
    [JsonPropertyName("fiscalization")]
    public Fiscalization? Fiscalization { get; set; }
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

    /// <summary>
    /// Apple Pay details (<c>apple_pay</c>), when the payer paid with Apple Pay.
    /// </summary>
    [JsonPropertyName("apple_pay")]
    public ApplePayResponsePaymentMethod? ApplePay { get; set; }

    /// <summary>
    /// Google Pay details (<c>google_pay</c>), when the payer paid with Google Pay.
    /// </summary>
    [JsonPropertyName("google_pay")]
    public GooglePayResponsePaymentMethod? GooglePay { get; set; }
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

    /// <summary>
    /// Whether the card was saved (<c>saved_card</c>).
    /// </summary>
    [JsonPropertyName("saved_card")]
    public bool? SavedCard { get; set; }

    /// <summary>
    /// Country of the card's BIN (<c>bin_country</c>).
    /// </summary>
    [JsonPropertyName("bin_country")]
    public string? BinCountry { get; set; }

    /// <summary>
    /// Alias name of the card BIN (<c>bin_alias_name</c>), for example <c>ROZETKA CARD</c>.
    /// </summary>
    [JsonPropertyName("bin_alias_name")]
    public string? BinAliasName { get; set; }
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

    /// <summary>
    /// User ID in the merchant's system (<c>external_id</c>).
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }
}