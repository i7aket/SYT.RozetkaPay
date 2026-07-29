using System.Text.Json;
using SYT.RozetkaPay.Models.AlternativePayments;
using SYT.RozetkaPay.Models.Batch;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.PayParts;
using SYT.RozetkaPay.Models.Subscriptions;
using SYT.RozetkaPay.Serialization;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Every SDK enum that mirrors a published one must serialize to exactly the tokens the OpenAPI
/// document declares — no more, no fewer, spelled identically.
/// </summary>
/// <remarks>
/// <para>
/// The members used to be annotated with <c>[JsonPropertyName]</c>, which <see cref="System.Text.Json"/>
/// ignores on enum members. The declared tokens were therefore documentation only: the wire value came
/// from the converter's naming policy instead, so <c>UK</c> went out as <c>"uk"</c> and
/// <c>LeaseLink</c> as <c>"lease_link"</c>, neither of which the provider accepts. The README quick
/// start used one of them.
/// </para>
/// <para>
/// The expected tokens are read from the pinned snapshot rather than repeated here. Repeating them
/// would mean a typo in the test could agree with a typo in production; and the snapshot is itself
/// checked against the live document, so a stale expectation fails there rather than weakening this.
/// </para>
/// <para>
/// Serialization goes through <see cref="SdkSerializerOptions.Value"/> — the options the SDK really
/// uses — so a converter change is caught here too, not just an annotation change.
/// </para>
/// </remarks>
public class EnumWireTokenTests
{
    /// <summary>
    /// Each SDK enum paired with the schema in the OpenAPI document that publishes its values.
    /// </summary>
    /// <remarks>
    /// Written out rather than discovered by matching type names against schema names. Name matching
    /// would silently drop a pair the day either side is renamed, and a test that quietly stops
    /// checking something is worse than one that fails.
    /// </remarks>
    public static TheoryData<string, Type> PublishedEnums => new()
    {
        { "ActionType", typeof(ActionType) },
        { "AlternativePaymentMethodType", typeof(AlternativePaymentMethodType) },
        { "AlternativePaymentOperationType", typeof(AlternativePaymentOperationType) },
        { "AlternativePaymentProvider", typeof(AlternativePaymentProvider) },
        { "BatchMethodType", typeof(BatchMethodType) },
        { "BatchPaymentMode", typeof(BatchPaymentMode) },
        { "CheckoutColorMode", typeof(CheckoutColorMode) },
        { "CustomerCheckoutLocale", typeof(CustomerCheckoutLocale) },
        { "ErrorType", typeof(ErrorType) },
        { "MerchantStatus", typeof(SYT.RozetkaPay.Models.Merchants.MerchantStatus) },
        { "OperationStatus", typeof(OperationStatus) },
        { "OperationType", typeof(OperationType) },
        { "PayPartsOperationType", typeof(PayPartsOperationType) },
        { "PayPartsPaymentMode", typeof(PayPartsPaymentMode) },
        { "PaymentMethodType", typeof(PaymentMethodType) },
        { "PaymentMode", typeof(PaymentMode) },
        { "PlanState", typeof(PlanState) },
        { "ResponseCode", typeof(ResponseCode) },
        { "SubscriptionCallbackType", typeof(SubscriptionCallbackType) },
        { "SubscriptionPaymentState", typeof(SubscriptionPaymentState) },
        { "SubscriptionState", typeof(SubscriptionState) },
    };

    [Theory]
    [MemberData(nameof(PublishedEnums))]
    public void Enum_ShouldSerializeToExactlyTheDeclaredTokens(string schemaName, Type enumType)
    {
        HashSet<string> expected = [.. DeclaredValues(schemaName)];
        HashSet<string> actual = [.. WireTokensOf(enumType)];

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(PublishedEnums))]
    public void Enum_ShouldReadBackEveryDeclaredToken(string schemaName, Type enumType)
    {
        // A response carrying a value the SDK cannot parse turns a successful call into a JsonException,
        // so reading matters as much as writing.
        foreach (string token in DeclaredValues(schemaName))
        {
            object? parsed = JsonSerializer.Deserialize(
                $"\"{token}\"",
                enumType,
                SdkSerializerOptions.Value);

            Assert.NotNull(parsed);
        }
    }

    [Fact]
    public void Locale_ShouldSerializeUppercase()
    {
        // The exact value the README quick start passes. It was going out as "uk".
        Assert.Equal("\"UK\"", JsonSerializer.Serialize(CustomerCheckoutLocale.UK, SdkSerializerOptions.Value));
    }

    [Fact]
    public void Provider_ShouldNotAcquireAnInventedWordBreak()
    {
        Assert.Equal(
            "\"leaselink\"",
            JsonSerializer.Serialize(AlternativePaymentProvider.LeaseLink, SdkSerializerOptions.Value));
    }

    [Fact]
    public void ThreeDsResponseCodes_ShouldKeepTheirLeadingDigit()
    {
        // Snake-casing the member name produced "three_ds_required"; the published token starts with a
        // digit, which no naming policy would ever have derived from a C# identifier.
        Assert.Equal(
            "\"3ds_required\"",
            JsonSerializer.Serialize(ResponseCode.ThreeDsRequired, SdkSerializerOptions.Value));
    }

    private static IReadOnlyList<string> DeclaredValues(string schemaName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "openapi.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));

        return [.. document.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty(schemaName)
            .GetProperty("enum")
            .EnumerateArray()
            .Select(static value => value.GetString()!)];
    }

    private static IEnumerable<string> WireTokensOf(Type enumType)
    {
        return Enum.GetValues(enumType)
            .Cast<object>()
            .Select(member => JsonSerializer.Serialize(member, enumType, SdkSerializerOptions.Value).Trim('"'));
    }
}
