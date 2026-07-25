using System.Text.Json.Serialization;
using SYT.RozetkaPay.Models.Common;

namespace SYT.RozetkaPay.Models.Partners;

/// <summary>
/// Optional query parameters of the official <c>merchantStatus</c> operation:
/// <c>GET /api/partners/v1/merchant-status</c>.
/// </summary>
/// <remarks>
/// Both properties are transient query switches, not persisted state. <see langword="null"/> means
/// "omit the parameter"; an empty string is not null and is sent as an empty value, so the provider —
/// which owns non-empty validation — is the one that rejects it.
/// </remarks>
public class PartnerMerchantStatusOptions
{
    /// <summary>
    /// Optional <c>merchant_project_id</c> query value. Pass the raw value: it is percent-encoded
    /// exactly once.
    /// </summary>
    public string? MerchantProjectId { get; set; }

    /// <summary>
    /// Optional <c>merchant_entity_id</c> query value. Pass the raw value: it is percent-encoded
    /// exactly once.
    /// </summary>
    public string? MerchantEntityId { get; set; }
}

/// <summary>
/// Optional query parameters of the official <c>transactionDetails</c> operation:
/// <c>GET /api/partners/v1/transaction-details</c>.
/// </summary>
/// <remarks>
/// The required <c>merchant_entity_id</c> is a method parameter, not an option. Both properties here
/// are transient query switches: <see langword="null"/> means "omit", and an empty string is sent as an
/// empty value.
/// </remarks>
public class PartnerTransactionDetailsOptions
{
    /// <summary>
    /// Optional <c>merchant_order_id</c> query value. Pass the raw value: it is percent-encoded
    /// exactly once.
    /// </summary>
    public string? MerchantOrderId { get; set; }

    /// <summary>
    /// Optional <c>unified_external_id</c> query value. Pass the raw value: it is percent-encoded
    /// exactly once.
    /// </summary>
    public string? UnifiedExternalId { get; set; }
}

/// <summary>
/// Inner and outer fee of one partner payment channel.
/// </summary>
/// <remarks>
/// This is the shape the official <c>feeDetails</c> operation returns. The historical
/// <see cref="SYT.RozetkaPay.Models.Merchants.PartnersFeeDetails"/> and
/// <see cref="SYT.RozetkaPay.Models.Common.PartnersFeeDetails"/> types describe an older layout and are
/// left untouched for consumers that already compiled against them.
/// </remarks>
public class PartnerFeeDetails
{
    /// <summary>
    /// Fee charged by RozetkaPay (JSON object).
    /// </summary>
    [JsonPropertyName("inner_fee")]
    public FeeItem? InnerFee { get; set; }

    /// <summary>
    /// Fee charged by the external participant (JSON object).
    /// </summary>
    [JsonPropertyName("outer_fee")]
    public FeeItem? OuterFee { get; set; }
}

/// <summary>
/// Response of the official <c>feeDetails</c> operation:
/// <c>GET /api/partners/v1/fee-details</c>.
/// </summary>
public class PartnerFeeDetailsResponse
{
    /// <summary>
    /// Fees of the online channel (JSON object).
    /// </summary>
    [JsonPropertyName("online")]
    public PartnerFeeDetails? Online { get; set; }

    /// <summary>
    /// Fees of the pay-now-fund-provider channel (JSON object).
    /// </summary>
    [JsonPropertyName("pnfp")]
    public PartnerFeeDetails? Pnfp { get; set; }
}

/// <summary>
/// One transaction returned by the official <c>transactionDetails</c> operation.
/// </summary>
/// <remarks>
/// Every field is an optional string because the official schema declares nothing else. In particular
/// <see cref="ProcessedAt"/> is not parsed into a date: the schema carries no format, so parsing it
/// would invent a contract the provider never published. The historical
/// <see cref="SYT.RozetkaPay.Models.Merchants.PartnersTransactionDetails"/> and
/// <see cref="SYT.RozetkaPay.Models.Common.PartnersTransactionDetails"/> types describe an older layout
/// and are left untouched.
/// </remarks>
public class PartnerTransactionDetails
{
    /// <summary>
    /// Masked payer card number (JSON string).
    /// </summary>
    [JsonPropertyName("card_mask")]
    public string? CardMask { get; set; }

    /// <summary>
    /// Merchant entity identifier (JSON string).
    /// </summary>
    [JsonPropertyName("merchant_entity_id")]
    public string? MerchantEntityId { get; set; }

    /// <summary>
    /// Merchant fee amount (JSON string).
    /// </summary>
    [JsonPropertyName("merchant_fee_amount")]
    public string? MerchantFeeAmount { get; set; }

    /// <summary>
    /// Merchant order identifier (JSON string).
    /// </summary>
    [JsonPropertyName("merchant_order_id")]
    public string? MerchantOrderId { get; set; }

    /// <summary>
    /// Unified external identifier (JSON string).
    /// </summary>
    [JsonPropertyName("unified_external_id")]
    public string? UnifiedExternalId { get; set; }

    /// <summary>
    /// Payment method (JSON string).
    /// </summary>
    [JsonPropertyName("method")]
    public string? Method { get; set; }

    /// <summary>
    /// Order description (JSON string).
    /// </summary>
    [JsonPropertyName("order_description")]
    public string? OrderDescription { get; set; }

    /// <summary>
    /// Order identifier (JSON string).
    /// </summary>
    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    /// <summary>
    /// Payment way (JSON string).
    /// </summary>
    [JsonPropertyName("pay_way")]
    public string? PayWay { get; set; }

    /// <summary>
    /// Payment amount (JSON string).
    /// </summary>
    [JsonPropertyName("payment_amount")]
    public string? PaymentAmount { get; set; }

    /// <summary>
    /// Original payment amount (JSON string).
    /// </summary>
    [JsonPropertyName("payment_original_amount")]
    public string? PaymentOriginalAmount { get; set; }

    /// <summary>
    /// Amount credited to the recipient (JSON string).
    /// </summary>
    [JsonPropertyName("payment_recipient_amount")]
    public string? PaymentRecipientAmount { get; set; }

    /// <summary>
    /// Processing timestamp (JSON string). Carried verbatim; the official schema declares no format.
    /// </summary>
    [JsonPropertyName("processed_at")]
    public string? ProcessedAt { get; set; }

    /// <summary>
    /// Masked recipient card number (JSON string).
    /// </summary>
    [JsonPropertyName("recipient_card_mask")]
    public string? RecipientCardMask { get; set; }

    /// <summary>
    /// Transaction status (JSON string).
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

/// <summary>
/// Response of the official <c>transactionDetails</c> operation:
/// <c>GET /api/partners/v1/transaction-details</c>.
/// </summary>
public class PartnerTransactionDetailsListResponse
{
    /// <summary>
    /// Matching transactions (JSON array).
    /// </summary>
    [JsonPropertyName("transactions")]
    public List<PartnerTransactionDetails>? Transactions { get; set; }
}
