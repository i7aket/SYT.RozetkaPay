using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using SYT.RozetkaPay.Models.AlternativePayments;
using SYT.RozetkaPay.Models.Batch;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.PayParts;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Serialization;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Every schema that declares <c>metadata</c> has it, typed as declared, with the published limits
/// attached.
/// </summary>
/// <remarks>
/// <para>
/// The field is declared in ten places — six request bodies and four operation results. The SDK carried
/// it in two, and in both as <c>Dictionary&lt;string, object&gt;</c> where the schema says the values
/// are strings capped at 200 characters. A caller could put a nested object in it and only find out from
/// a gateway rejection.
/// </para>
/// <para>
/// The ten are listed explicitly rather than discovered. Discovery by name would have quietly skipped
/// whatever the SDK had not modelled yet, which is precisely the state this closes.
/// </para>
/// </remarks>
public class MetadataContractTests
{
    /// <summary>
    /// Every schema site the document declares <c>metadata</c> on, with the SDK type that carries it.
    /// </summary>
    /// <remarks>
    /// Ten sites, nine types. <c>CreatePaymentRequest</c> and <c>CreatePaymentRequestDev</c> are the
    /// same body under two names — only the second is referenced by an operation, and they differ by
    /// one field the referenced one does not have — so a single SDK type serves both.
    /// </remarks>
    private static readonly Dictionary<string, Type> MetadataSites = new()
    {
        ["CreatePaymentRequest"] = typeof(CreatePaymentRequest),
        ["CreatePaymentRequestDev"] = typeof(CreatePaymentRequest),
        ["CreateBatchPaymentRequest"] = typeof(CreateBatchPaymentRequest),
        ["CreateRecurrentPaymentRequest"] = typeof(CreateRecurrentPaymentRequest),
        ["CreatePayPartsOrder"] = typeof(CreatePayPartsOrder),
        ["CreateAlternativePayment"] = typeof(CreateAlternativePayment),
        ["PaymentOperationResult"] = typeof(PaymentOperationResult),
        ["BatchPaymentOperationResult"] = typeof(BatchPaymentOperationResult),
        ["PayPartsOperationResult"] = typeof(PayPartsOperationResult),
        ["AlternativePaymentOperationResult"] = typeof(AlternativePaymentOperationResult),
    };

    public static TheoryData<Type> MetadataBearingTypes
    {
        get
        {
            TheoryData<Type> data = [];
            foreach (Type type in MetadataSites.Values.Distinct())
            {
                data.Add(type);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(MetadataBearingTypes))]
    public void MetadataBearingType_ShouldDeclareItAsAStringDictionaryUnderTheDeclaredLimits(Type modelType)
    {
        PropertyInfo property = modelType.GetProperty("Metadata")
            ?? throw new InvalidOperationException($"{modelType.Name} has no Metadata property.");

        Assert.Equal(typeof(Dictionary<string, string>), property.PropertyType);
        Assert.Equal("metadata", property
            .GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>()!.Name);
        Assert.NotNull(property.GetCustomAttribute<MetadataLimitsAttribute>());
    }

    [Fact]
    public void MetadataBearingTypes_ShouldCoverEverySchemaThatDeclaresIt()
    {
        // Guards the map above against the document growing another metadata site.
        Assert.Equal(10, MetadataSites.Count);
    }

    [Fact]
    public void Metadata_ShouldSerializeAsAFlatStringMap()
    {
        CreatePaymentRequest request = new()
        {
            Amount = 10m,
            Currency = "UAH",
            ExternalId = "order-1",
            Metadata = new Dictionary<string, string> { ["promo_id"] = "12345-54321" },
        };

        string json = JsonSerializer.Serialize(request, SdkSerializerOptions.Value);

        Assert.Contains("""
            "metadata":{"promo_id":"12345-54321"}
            """.Trim(), json, StringComparison.Ordinal);
    }

    [Fact]
    public void Metadata_ShouldBeOmittedWhenAbsent()
    {
        CreatePaymentRequest request = new() { Amount = 10m, Currency = "UAH", ExternalId = "order-1" };

        string json = JsonSerializer.Serialize(request, SdkSerializerOptions.Value);

        Assert.DoesNotContain("metadata", json, StringComparison.Ordinal);
    }

    // =============================================================================================
    // The limits. Checked locally because a gateway rejection names neither the offending key nor
    // which of the three limits it exceeded.
    // =============================================================================================

    [Fact]
    public void Limits_ShouldAcceptTheLargestPermittedMetadata()
    {
        Dictionary<string, string> atTheLimit = Enumerable.Range(0, MetadataLimitsAttribute.MaxEntries)
            .ToDictionary(
                index => new string('k', MetadataLimitsAttribute.MaxKeyLength - 2) + index.ToString("D2"),
                _ => new string('v', MetadataLimitsAttribute.MaxValueLength));

        Assert.Empty(Validate(atTheLimit));
    }

    [Fact]
    public void Limits_ShouldRejectAnEleventhEntry()
    {
        Dictionary<string, string> tooMany = Enumerable.Range(0, MetadataLimitsAttribute.MaxEntries + 1)
            .ToDictionary(index => $"k{index}", _ => "v");

        ValidationResult failure = Assert.Single(Validate(tooMany));
        Assert.Contains("at most 10 entries", failure.ErrorMessage!, StringComparison.Ordinal);
    }

    [Fact]
    public void Limits_ShouldRejectAnOverlongKey()
    {
        Dictionary<string, string> longKey = new()
        {
            [new string('k', MetadataLimitsAttribute.MaxKeyLength + 1)] = "v",
        };

        ValidationResult failure = Assert.Single(Validate(longKey));
        Assert.Contains("the limit is 30", failure.ErrorMessage!, StringComparison.Ordinal);
    }

    [Fact]
    public void Limits_ShouldRejectAnOverlongValue()
    {
        Dictionary<string, string> longValue = new()
        {
            ["promo_id"] = new string('v', MetadataLimitsAttribute.MaxValueLength + 1),
        };

        ValidationResult failure = Assert.Single(Validate(longValue));
        Assert.Contains("the limit is 200", failure.ErrorMessage!, StringComparison.Ordinal);
        Assert.Contains("key 'promo_id'", failure.ErrorMessage!, StringComparison.Ordinal);

        // The offending value is merchant-supplied and may carry anything, so it is counted, not quoted.
        Assert.DoesNotContain(new string('v', 20), failure.ErrorMessage!, StringComparison.Ordinal);
    }

    [Fact]
    public void Limits_ShouldAcceptAbsentMetadata()
    {
        Assert.Empty(Validate(null));
    }

    private static IReadOnlyList<ValidationResult> Validate(Dictionary<string, string>? metadata)
    {
        CreatePaymentRequest request = new()
        {
            Amount = 10m,
            Currency = "UAH",
            ExternalId = "order-1",
            Metadata = metadata,
        };

        List<ValidationResult> results = [];
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        return results;
    }
}
