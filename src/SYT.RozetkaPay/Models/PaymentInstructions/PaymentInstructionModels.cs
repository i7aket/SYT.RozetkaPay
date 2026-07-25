using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json.Serialization;

namespace SYT.RozetkaPay.Models.PaymentInstructions;

/// <summary>
/// Processing type accepted by the official <c>createPaymentInstructions</c> operation.
/// </summary>
/// <remarks>
/// The tokens must be pinned explicitly. Under the SDK snake-case naming policy
/// <c>CardPay</c> would serialize as <c>card_pay</c> and <c>PPay</c> as <c>p_pay</c>, neither of which
/// the provider accepts.
/// </remarks>
public enum PaymentInstructionProcessingType
{
    /// <summary>
    /// Card processing (<c>cardpay</c>).
    /// </summary>
    [JsonStringEnumMemberName("cardpay")]
    CardPay,

    /// <summary>
    /// Instalment processing (<c>ppay</c>).
    /// </summary>
    [JsonStringEnumMemberName("ppay")]
    PPay
}

/// <summary>
/// Payment method accepted by the official <c>createPaymentInstructions</c> operation.
/// </summary>
public enum PaymentInstructionMethod
{
    /// <summary>
    /// Authorization only (<c>auth</c>).
    /// </summary>
    [JsonStringEnumMemberName("auth")]
    Auth,

    /// <summary>
    /// Immediate purchase (<c>purchase</c>).
    /// </summary>
    [JsonStringEnumMemberName("purchase")]
    Purchase
}

/// <summary>
/// Payer details of the official <c>createPaymentInstructions</c> request.
/// </summary>
public class PaymentInstructionPayer
{
    /// <summary>
    /// Taxpayer identification number (JSON string).
    /// </summary>
    [JsonPropertyName("tin")]
    public string? Tin { get; set; }

    /// <summary>
    /// Payer first name (JSON string).
    /// </summary>
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    /// <summary>
    /// Payer last name (JSON string).
    /// </summary>
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    /// <summary>
    /// Payer patronymic (JSON string).
    /// </summary>
    [JsonPropertyName("patronym")]
    public string? Patronym { get; set; }
}

/// <summary>
/// One order of the official <c>createPaymentInstructions</c> request.
/// </summary>
public class PaymentInstructionOrder
{
    /// <summary>
    /// Project API key (JSON string, <c>uuid</c>). Required.
    /// </summary>
    [JsonPropertyName("api_key")]
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Order amount (JSON number, <c>decimal</c>). Required.
    /// </summary>
    [JsonPropertyName("amount")]
    [Required]
    public decimal Amount { get; set; }

    /// <summary>
    /// Order identifier in the caller's system (JSON string). Required.
    /// </summary>
    [JsonPropertyName("external_id")]
    [Required]
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Optional order description (JSON string).
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Request body of the official <c>createPaymentInstructions</c> operation:
/// <c>POST /api/payment-instructions/v1/new</c>.
/// </summary>
public class CreatePaymentInstructionsRequest
{
    /// <summary>
    /// Processing type (JSON string). Required.
    /// </summary>
    [JsonPropertyName("processing_type")]
    [Required]
    public PaymentInstructionProcessingType ProcessingType { get; set; }

    /// <summary>
    /// Payment method (JSON string). Required.
    /// </summary>
    [JsonPropertyName("method")]
    [Required]
    public PaymentInstructionMethod Method { get; set; }

    /// <summary>
    /// Three-letter ISO 4217 currency code (JSON string). Required.
    /// </summary>
    [JsonPropertyName("currency")]
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Optional batch identifier in the caller's system (JSON string).
    /// </summary>
    [JsonPropertyName("batch_external_id")]
    public string? BatchExternalId { get; set; }

    /// <summary>
    /// Optional payer details (JSON object).
    /// </summary>
    [JsonPropertyName("payer")]
    public PaymentInstructionPayer? Payer { get; set; }

    /// <summary>
    /// Orders to create instructions for (JSON array). Required, at least one entry.
    /// </summary>
    [JsonPropertyName("orders")]
    [Required]
    [MinLength(1)]
    public List<PaymentInstructionOrder> Orders { get; set; } = [];
}

/// <summary>
/// One instruction returned by the official <c>createPaymentInstructions</c> operation.
/// </summary>
public class PaymentInstruction
{
    /// <summary>
    /// Instruction identifier (JSON string).
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Order identifier in the caller's system (JSON string).
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Project identifier (JSON string).
    /// </summary>
    [JsonPropertyName("project_id")]
    public string? ProjectId { get; set; }

    /// <summary>
    /// Instruction number (JSON number, <c>decimal</c>).
    /// </summary>
    [JsonPropertyName("number")]
    public decimal? Number { get; set; }

    /// <summary>
    /// Instruction URL (JSON string, <c>uri</c>).
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Instruction download URL (JSON string, <c>uri</c>).
    /// </summary>
    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; set; }
}

/// <summary>
/// Response of the official <c>createPaymentInstructions</c> operation.
/// </summary>
public class PaymentInstructionsResult
{
    /// <summary>
    /// Batch currency (JSON string).
    /// </summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    /// <summary>
    /// Batch identifier in the caller's system (JSON string).
    /// </summary>
    [JsonPropertyName("batch_external_id")]
    public string? BatchExternalId { get; set; }

    /// <summary>
    /// Batch URL (JSON string).
    /// </summary>
    [JsonPropertyName("batch_url")]
    public string? BatchUrl { get; set; }

    /// <summary>
    /// Batch download URL (JSON string).
    /// </summary>
    [JsonPropertyName("batch_download_url")]
    public string? BatchDownloadUrl { get; set; }

    /// <summary>
    /// Created instructions (JSON array).
    /// </summary>
    [JsonPropertyName("instructions")]
    public List<PaymentInstruction>? Instructions { get; set; }
}

/// <summary>
/// Result of the official <c>declinePaymentInstruction</c> operation:
/// <c>GET /api/payment-instructions/v1/decline</c>.
/// </summary>
/// <remarks>
/// The operation answers with a bare HTTP <c>302</c> whose <c>Location</c> header is the whole result.
/// The SDK does not follow it, does not read the target, and carries no credential to it, so this type
/// holds provider output only and never any secret. Deciding whether to navigate to
/// <see cref="Location"/> — and validating it first — belongs to the caller: it is a provider-controlled
/// URL, and server-side fetching of an unvalidated redirect target is a request-forgery sink.
/// </remarks>
/// <param name="StatusCode">Status returned by the provider. Always <c>302</c> on success.</param>
/// <param name="Location">
/// Parsed <c>Location</c> response header. May be relative, exactly as the provider sent it.
/// </param>
public sealed record PaymentInstructionDeclineResult(HttpStatusCode StatusCode, Uri Location);
