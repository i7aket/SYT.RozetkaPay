using System.Text.Json;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Serialization;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// A request writes the JSON type the document declares, not merely one the provider might read.
/// </summary>
/// <remarks>
/// <para>
/// <c>PropertyTypeParityTests</c> treats a declared string modelled as a number as compatible,
/// because the flexible converters read both shapes. That holds for responses and fails for
/// requests, where there is a validator on the other end:
/// </para>
/// <code>
/// products[].quantity as a JSON number -> 400 invalid_request_body  param: products.quantity
/// products[].quantity as "2"           -> 200
/// </code>
/// <para>
/// So the tolerance is response-side only, and these tests hold the sending direction to the
/// declared type. The properties stay <c>decimal?</c> and <c>int?</c>: retyping them to
/// <c>string</c> would hand every caller the formatting problem — including the culture bug that
/// <c>ToString()</c> produces under a comma-decimal locale — which is exactly what the converters
/// exist to absorb.
/// </para>
/// </remarks>
public class RequestJsonTypeTests
{
    [Fact]
    public void ProductAmountsAndQuantity_ShouldSerializeAsJsonStrings()
    {
        string json = JsonSerializer.Serialize(
            new Product { Name = "Consultation", Quantity = 2, NetAmount = 0.83m, VatAmount = 0.17m },
            SdkSerializerOptions.Value);

        Assert.Contains("\"quantity\":\"2\"", json, StringComparison.Ordinal);
        Assert.Contains("\"net_amount\":\"0.83\"", json, StringComparison.Ordinal);
        Assert.Contains("\"vat_amount\":\"0.17\"", json, StringComparison.Ordinal);

        // The shape that was rejected. Named so a regression is unmistakable in the failure output.
        Assert.DoesNotContain("\"quantity\":2", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// Reading stays tolerant: the provider sends these both ways and both must load.
    /// </summary>
    [Theory]
    [InlineData("""{"quantity":"2","net_amount":"0.83"}""")]
    [InlineData("""{"quantity":2,"net_amount":0.83}""")]
    public void EitherWireShape_ShouldStillBeReadable(string body)
    {
        Product product = JsonSerializer.Deserialize<Product>(body, SdkSerializerOptions.Value)!;

        Assert.Equal(2, product.Quantity);
        Assert.Equal(0.83m, product.NetAmount);
    }

    /// <summary>
    /// Formatting is invariant, whatever locale the process runs under.
    /// </summary>
    /// <remarks>
    /// A comma-decimal culture would otherwise write <c>"0,83"</c>, which the gateway would reject —
    /// and only on machines configured that way, which is the worst kind of defect to chase.
    /// </remarks>
    [Theory]
    [InlineData("uk-UA")]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    public void Formatting_ShouldNotFollowTheAmbientCulture(string culture)
    {
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(culture);

        string json = JsonSerializer.Serialize(
            new Product { Name = "Consultation", NetAmount = 0.83m }, SdkSerializerOptions.Value);

        Assert.Contains("\"net_amount\":\"0.83\"", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// A null stays null rather than becoming an empty string the provider would have to interpret.
    /// </summary>
    [Fact]
    public void AnAbsentAmount_ShouldNotBecomeAnEmptyString()
    {
        string json = JsonSerializer.Serialize(new Product { Name = "Consultation" }, SdkSerializerOptions.Value);

        Assert.DoesNotContain("\"net_amount\":\"\"", json, StringComparison.Ordinal);
    }
}
