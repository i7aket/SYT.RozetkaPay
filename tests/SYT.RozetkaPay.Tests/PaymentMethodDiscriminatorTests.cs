using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Serialization;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// The payment-method discriminator is the enum the document declares, not a free string.
/// </summary>
/// <remarks>
/// <para>
/// <c>CustomerRequestPaymentMethod.type</c> is declared as <c>PaymentMethodType</c>, a closed set of
/// six values, and it decides which sibling object the request must carry. The SDK modelled it as
/// <c>string?</c>, which let a live probe send <c>"cc"</c> — not one of the six — and have the request
/// built, validated, serialized and sent without a word.
/// </para>
/// <para>
/// <c>PropertyTypeParityTests</c> treats enum-versus-string as compatible on purpose, so it could not
/// catch this. That reasoning holds where both forms round-trip and the choice is taste. It does not
/// hold for a closed discriminator, where the string form has no upside and drops the only guarantee
/// the enum offers.
/// </para>
/// </remarks>
public class PaymentMethodDiscriminatorTests
{
    [Fact]
    public void TheDiscriminator_ShouldBeTheDeclaredEnumRatherThanAString()
    {
        PropertyInfo type = typeof(CustomerRequestPaymentMethod)
            .GetProperty(nameof(CustomerRequestPaymentMethod.Type))!;

        Assert.Equal(typeof(PaymentMethodType?), type.PropertyType);
    }

    /// <summary>
    /// Nullable, so that forgetting the field fails validation instead of defaulting to a method the
    /// caller never chose.
    /// </summary>
    [Fact]
    public void OmittingTheDiscriminator_ShouldNotSilentlyMeanTheFirstEnumMember()
    {
        CustomerRequestPaymentMethod method = new();

        Assert.Null(method.Type);

        // The zero value is cc_token. A non-nullable property would have produced it here, and the
        // request would have gone out naming a payment method nobody selected.
        Assert.Equal(PaymentMethodType.CCToken, default(PaymentMethodType));

        List<ValidationResult> failures = [];
        bool valid = Validator.TryValidateObject(
            method, new ValidationContext(method), failures, validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(failures, failure => failure.MemberNames.Contains(nameof(CustomerRequestPaymentMethod.Type)));
    }

    /// <summary>
    /// Every value the document lists, spelled on the wire exactly as the document spells it.
    /// </summary>
    [Theory]
    [InlineData(PaymentMethodType.CCToken, "cc_token")]
    [InlineData(PaymentMethodType.CCNumber, "cc_number")]
    [InlineData(PaymentMethodType.Wallet, "wallet")]
    [InlineData(PaymentMethodType.GooglePay, "google_pay")]
    [InlineData(PaymentMethodType.ApplePay, "apple_pay")]
    [InlineData(PaymentMethodType.Card, "card")]
    public void EachDeclaredValue_ShouldSerializeToItsDocumentedToken(PaymentMethodType value, string token)
    {
        string json = JsonSerializer.Serialize(
            new CustomerRequestPaymentMethod { Type = value }, SdkSerializerOptions.Value);

        Assert.Contains($"\"type\":\"{token}\"", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// The probe that started this. <c>cc</c> is not one of the six, and the type system is now what
    /// says so — there is no longer a way to express it.
    /// </summary>
    [Fact]
    public void AValueOutsideTheDeclaredSet_ShouldNotBeReadable()
    {
        const string offEnum = """{"type":"cc"}""";

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<CustomerRequestPaymentMethod>(offEnum, SdkSerializerOptions.Value));
    }

    /// <summary>
    /// The enum carries exactly the six values the document declares, no more.
    /// </summary>
    [Fact]
    public void TheEnum_ShouldCarryExactlyTheDeclaredValues()
    {
        string[] wire = [.. typeof(PaymentMethodType)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(static field =>
                field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name ?? field.Name)
            .Order(StringComparer.Ordinal)];

        Assert.Equal(
            ["apple_pay", "card", "cc_number", "cc_token", "google_pay", "wallet"],
            wire);
    }
}
