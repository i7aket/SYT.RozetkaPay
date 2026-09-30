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
/// The <c>inner_fee</c> / <c>outer_fee</c> pair of one channel in a <c>feeDetails</c> response (document schema
/// <c>partners.FeeDetails</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>What the document does and does not say.</b> It declares the two property names and that each is a
/// <see cref="FeeItem"/> (<c>fix</c>, <c>max</c>, <c>min</c>, <c>percent</c>, all plain numbers). It gives
/// <i>no</i> description of either property: it does not say who charges the inner or the outer fee, whether
/// one includes the other, or in what currency or scale the numbers are. The property comments below are
/// therefore limited to the JSON names; an earlier version of this file attributed the fees to "RozetkaPay" and
/// "the external participant", which was the SDK's guess, not the provider's contract.
/// </para>
/// <para>
/// Since 2026-09-30 the operation that returns this shape is no longer in the public document at all (see
/// <see cref="SYT.RozetkaPay.Services.IPartnerService"/>); the schema itself is still published, unreferenced
/// and unchanged. Interpret the values against a real response from your account before relying on them.
/// </para>
/// <para>
/// The historical <see cref="SYT.RozetkaPay.Models.Merchants.PartnersFeeDetails"/> and
/// <see cref="SYT.RozetkaPay.Models.Common.PartnersFeeDetails"/> types describe an older layout and are left
/// untouched for consumers that already compiled against them.
/// </para>
/// </remarks>
public class PartnerFeeDetails
{
    /// <summary>
    /// JSON <c>inner_fee</c>. The document declares its type (<see cref="FeeItem"/>) and nothing else — no
    /// description of what the inner fee is.
    /// </summary>
    [JsonPropertyName("inner_fee")]
    public FeeItem? InnerFee { get; set; }

    /// <summary>
    /// JSON <c>outer_fee</c>. The document declares its type (<see cref="FeeItem"/>) and nothing else — no
    /// description of what the outer fee is.
    /// </summary>
    [JsonPropertyName("outer_fee")]
    public FeeItem? OuterFee { get; set; }
}

/// <summary>
/// Response of the <c>feeDetails</c> operation: <c>GET /api/partners/v1/fee-details</c> (document response
/// <c>FeeDetailsResponse</c>; the operation left the public document on 2026-09-30).
/// </summary>
/// <remarks>
/// The document describes exactly one thing about this body: <c>online</c> is "empty for now". <c>pnfp</c> has
/// no description; the acronym is not expanded anywhere in the document, and the SDK does not guess it (an
/// earlier comment here read it as "pay-now-fund-provider", which nothing published supports).
/// </remarks>
public class PartnerFeeDetailsResponse
{
    /// <summary>
    /// JSON <c>online</c>. The document describes it as "empty for now", so expect <see langword="null"/> or an
    /// object without fees.
    /// </summary>
    [JsonPropertyName("online")]
    public PartnerFeeDetails? Online { get; set; }

    /// <summary>
    /// JSON <c>pnfp</c>. Undescribed by the document — neither the channel it stands for nor when it is
    /// present is published.
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
