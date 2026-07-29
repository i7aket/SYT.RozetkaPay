using System.ComponentModel.DataAnnotations;

namespace SYT.RozetkaPay.Models.Common;

/// <summary>
/// Enforces the published limits on a merchant <c>metadata</c> dictionary: at most ten entries, each
/// key up to 30 characters, each value up to 200.
/// </summary>
/// <remarks>
/// <para>
/// The limits are the provider's, stated in the <c>Metadata</c> schema. They are worth checking locally
/// because the failure is otherwise a <c>400</c> from the gateway that names neither the offending key
/// nor which of the three limits was exceeded.
/// </para>
/// <para>
/// A <see cref="ValidationAttribute"/> rather than a check inside the serializer: it puts the rule next
/// to the property it governs, and it is the same mechanism the options pipeline already uses. Request
/// models are not yet run through <see cref="Validator"/> before dispatch — EXP-402 is the change that
/// turns that on, for every model at once — so today this rule is enforced wherever a caller validates,
/// and automatically once that lands.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class MetadataLimitsAttribute : ValidationAttribute
{
    /// <summary>Maximum number of entries the provider accepts.</summary>
    public const int MaxEntries = 10;

    /// <summary>Maximum length of a metadata key.</summary>
    public const int MaxKeyLength = 30;

    /// <summary>Maximum length of a metadata value.</summary>
    public const int MaxValueLength = 200;

    /// <inheritdoc />
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            // Absent metadata is valid: the field is optional everywhere it appears.
            return ValidationResult.Success;
        }

        if (value is not IReadOnlyDictionary<string, string> metadata)
        {
            return new ValidationResult(
                $"{validationContext.MemberName} must be a dictionary of string keys and string values.");
        }

        if (metadata.Count > MaxEntries)
        {
            return new ValidationResult(
                $"{validationContext.MemberName} accepts at most {MaxEntries} entries; {metadata.Count} were supplied.");
        }

        foreach (KeyValuePair<string, string> entry in metadata)
        {
            if (entry.Key.Length > MaxKeyLength)
            {
                // The key is named because it identifies the offender, and a metadata key is
                // merchant-chosen rather than customer data.
                return new ValidationResult(
                    $"{validationContext.MemberName} key '{entry.Key}' is {entry.Key.Length} characters; " +
                    $"the limit is {MaxKeyLength}.");
            }

            if (entry.Value?.Length > MaxValueLength)
            {
                // The value is not quoted: it is merchant-supplied and may carry anything.
                return new ValidationResult(
                    $"{validationContext.MemberName} value for key '{entry.Key}' is {entry.Value.Length} " +
                    $"characters; the limit is {MaxValueLength}.");
            }
        }

        return ValidationResult.Success;
    }
}
