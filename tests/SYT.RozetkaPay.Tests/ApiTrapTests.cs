using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Serialization;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Three places where the natural thing to write compiled and was wrong.
/// </summary>
public class ApiTrapTests
{
    /// <summary>
    /// Forgetting the mode is a validation failure, not a request for raw card acceptance.
    /// </summary>
    [Fact]
    public void AnOmittedMode_ShouldFailValidationRatherThanMeanDirect()
    {
        CreatePaymentRequest request = new()
        {
            Amount = 10m,
            Currency = "UAH",
            ExternalId = "order-1",
        };

        Assert.Null(request.Mode);

        // The zero value is Direct - the PCI-scope flow. That is what a forgotten field used to mean.
        Assert.Equal(PaymentMode.Direct, default(PaymentMode));

        Assert.False(Validate(request, out List<ValidationResult> failures));
        Assert.Contains(failures, f => f.MemberNames.Contains(nameof(CreatePaymentRequest.Mode)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void AnAmountThatIsNotAPayment_ShouldFailValidation(decimal amount)
    {
        CreatePaymentRequest request = new()
        {
            Amount = amount,
            Currency = "UAH",
            ExternalId = "order-1",
            Mode = PaymentMode.Hosted,
        };

        Assert.False(Validate(request, out List<ValidationResult> failures));
        Assert.Contains(failures, f => f.MemberNames.Contains(nameof(CreatePaymentRequest.Amount)));
    }

    /// <summary>
    /// A null amount arrived at by arithmetic must not pass for "refund everything".
    /// </summary>
    /// <remarks>
    /// The scenario is real rather than contrived: before EXP-431 the operations a caller reads those
    /// figures from returned a model where both were permanently null, so the subtraction below was
    /// the natural code and the result was a full refund.
    /// </remarks>
    [Fact]
    public void ANullRefundAmount_ShouldNotSilentlyRefundEverything()
    {
        decimal? paid = null;
        decimal? alreadyRefunded = null;
        decimal? half = (paid - alreadyRefunded) / 2;

        Assert.Null(half);

        RefundPaymentRequest request = new() { ExternalId = "order-1", Amount = half };

        Assert.False(Validate(request, out List<ValidationResult> failures));
        Assert.Contains(failures, f => f.MemberNames.Contains(nameof(RefundPaymentRequest.Amount)));
    }

    [Fact]
    public void AnIntendedFullRefund_ShouldStillBePossible()
    {
        RefundPaymentRequest request = new() { ExternalId = "order-1", RefundEntirePayment = true };

        Assert.True(Validate(request, out _));

        // The wire is unchanged: omitting the amount is still how a full refund is expressed.
        string json = JsonSerializer.Serialize(request, SdkSerializerOptions.Value);
        Assert.DoesNotContain("\"amount\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("RefundEntirePayment", json, StringComparison.OrdinalIgnoreCase);
    }

    private static bool Validate(object instance, out List<ValidationResult> failures)
    {
        failures = [];

        return Validator.TryValidateObject(
            instance, new ValidationContext(instance), failures, validateAllProperties: true);
    }
}

/// <summary>
/// A date reads the same whatever locale the process happens to run under.
/// </summary>
/// <remarks>
/// <para>
/// The first parse used the ambient culture, so <c>28.02.2026</c> was handled by that branch on a
/// day-first machine and fell through to the exact-format list everywhere else — and the fallback
/// carried a relabel-versus-convert bug that shifted the result by the machine's UTC offset.
/// </para>
/// <para>
/// Both halves had to be wrong for the defect to appear, which is why it survived: the developer's
/// locale hid it, and the test that would have caught it was written on the same machine.
/// </para>
/// </remarks>
public class DateCultureTests
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("uk-UA")]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    public void ADate_ShouldReadIdenticallyUnderAnyCulture(string culture)
    {
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(culture);

        DateHolder dayFirst = JsonSerializer.Deserialize<DateHolder>(
            """{"value":"28.02.2026 10:20:30"}""", SdkSerializerOptions.Value)!;

        DateHolder iso = JsonSerializer.Deserialize<DateHolder>(
            """{"value":"2026-02-28T10:20:30Z"}""", SdkSerializerOptions.Value)!;

        Assert.Equal(new DateTime(2026, 2, 28, 10, 20, 30, DateTimeKind.Utc), dayFirst.Value);
        Assert.Equal(new DateTime(2026, 2, 28, 10, 20, 30, DateTimeKind.Utc), iso.Value);
        Assert.Equal(DateTimeKind.Utc, dayFirst.Value.Kind);
    }

    private sealed class DateHolder
    {
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        [System.Text.Json.Serialization.JsonConverter(typeof(SYT.RozetkaPay.Converters.FlexibleDateTimeConverter))]
        public DateTime Value { get; set; }
    }
}
