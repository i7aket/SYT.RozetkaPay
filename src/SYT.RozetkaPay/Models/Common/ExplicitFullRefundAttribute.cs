using System.ComponentModel.DataAnnotations;

namespace SYT.RozetkaPay.Models.Common;

/// <summary>
/// Refunding the whole payment has to be asked for, not arrived at.
/// </summary>
/// <remarks>
/// <para>
/// The document lets <c>amount</c> be omitted, and omitting it refunds everything. That is a
/// reasonable wire contract and a dangerous default in C#, because <c>decimal?</c> arithmetic
/// propagates null:
/// </para>
/// <code>
/// decimal? toRefund = (booking.Amount - booking.AmountRefunded) / 2;   // null if either is null
/// await payments.RefundAsync(new RefundPaymentRequest { ExternalId = id, Amount = toRefund });
/// </code>
/// <para>
/// That compiles, passes validation, and refunds the entire consultation fee. It is not a contrived
/// example: before EXP-431 the operations a caller would read those figures from returned a model
/// where both were permanently <c>null</c>.
/// </para>
/// <para>
/// So a null amount is now a validation failure unless the caller says they meant it. The flag is
/// <c>[JsonIgnore]</c> — it changes nothing on the wire, where omitting the amount still means the
/// same thing. It exists so that meaning has to be chosen rather than fallen into.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ExplicitFullRefundAttribute : ValidationAttribute
{
    /// <inheritdoc />
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        Type type = value.GetType();
        decimal? amount = type.GetProperty("Amount")?.GetValue(value) as decimal?;
        bool entire = type.GetProperty("RefundEntirePayment")?.GetValue(value) as bool? ?? false;

        if (amount is not null || entire)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(
            "Amount is null, which the provider reads as a full refund. Set Amount, or set "
            + "RefundEntirePayment to say the full refund is intended.",
            ["Amount"]);
    }
}
