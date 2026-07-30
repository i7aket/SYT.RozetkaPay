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
