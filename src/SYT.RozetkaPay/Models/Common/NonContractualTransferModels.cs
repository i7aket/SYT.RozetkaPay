using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SYT.RozetkaPay.Models.Common;

/// <summary>
/// Beneficiary of a non-contractual credit transfer, for example a treasury or budget payment (document schema
/// <c>OrderRecipient</c>, published on 2026-09-30).
/// </summary>
/// <remarks>
/// <para>
/// Used on requests by <c>BatchOrder.order_recipient</c> and <c>PaymentInstructionOrder.order_recipient</c>, and
/// returned by <c>BatchOrderDetail.order_recipient</c>. The object is optional, but when it is sent all four
/// fields are required together.
/// </para>
/// <para>
/// On a request the SDK enforces that, and the documented lengths, before sending: an incomplete recipient
/// raises <see cref="SYT.RozetkaPay.Exceptions.RozetkaPayValidationException"/> instead of reaching the
/// provider.
/// </para>
/// <para>
/// Not to be confused with <c>ExpressCheckoutRecipient</c>, the delivery recipient a payment-info response carries
/// under the same JSON name.
/// </para>
/// </remarks>
public class OrderRecipient
{
    /// <summary>
    /// Recipient name, for example <c>Державне казначейство України</c>. At most 255 characters.
    /// </summary>
    [Required]
    [StringLength(255)]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Recipient identification code, for example <c>12345676</c>. At most 20 characters.
    /// </summary>
    [Required]
    [StringLength(20)]
    [JsonPropertyName("tin")]
    public string? Tin { get; set; }

    /// <summary>
    /// Recipient account (IBAN), 15 to 34 characters.
    /// </summary>
    [Required]
    [StringLength(34, MinimumLength = 15)]
    [JsonPropertyName("iban")]
    public string? Iban { get; set; }

    /// <summary>
    /// Recipient's payment service provider (bank) name. At most 255 characters.
    /// </summary>
    [Required]
    [StringLength(255)]
    [JsonPropertyName("bank_name")]
    public string? BankName { get; set; }
}

/// <summary>
/// Identity document of a payer who has no individual tax number (document schema <c>CustomerDocument</c>,
/// published on 2026-09-30).
/// </summary>
/// <remarks>
/// When the payer's <c>tin</c> is passed as well, <c>tin</c> takes precedence. A non-contractual credit transfer
/// requires one of the two.
/// </remarks>
public class CustomerDocument
{
    /// <summary>
    /// Identity document type.
    /// </summary>
    [Required]
    [JsonPropertyName("type")]
    public CustomerDocumentType? Type { get; set; }

    /// <summary>
    /// Document series: two Cyrillic letters for <see cref="CustomerDocumentType.Passport"/>. Omit it for
    /// <see cref="CustomerDocumentType.Id"/>, which has no series. At most 10 characters.
    /// </summary>
    [StringLength(10)]
    [JsonPropertyName("series")]
    public string? Series { get; set; }

    /// <summary>
    /// Document number: six digits for <see cref="CustomerDocumentType.Passport"/>, nine digits for
    /// <see cref="CustomerDocumentType.Id"/>. At most 20 characters.
    /// </summary>
    [Required]
    [StringLength(20)]
    [JsonPropertyName("number")]
    public string? Number { get; set; }
}

/// <summary>
/// Identity document type of a <see cref="CustomerDocument"/> (the inline enum of
/// <c>CustomerDocument.type</c>).
/// </summary>
public enum CustomerDocumentType
{
    /// <summary>Ukrainian booklet passport.</summary>
    [JsonStringEnumMemberName("passport")]
    Passport,

    /// <summary>ID card.</summary>
    [JsonStringEnumMemberName("id")]
    Id,

    /// <summary>Foreign (international) passport.</summary>
    [JsonStringEnumMemberName("foreign-passport")]
    ForeignPassport
}
