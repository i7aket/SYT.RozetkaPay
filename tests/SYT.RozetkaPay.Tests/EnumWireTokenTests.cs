using System.Reflection;
using System.Text.Json;
using SYT.RozetkaPay.Models.AlternativePayments;
using SYT.RozetkaPay.Models.Batch;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Merchants;
using SYT.RozetkaPay.Models.PayParts;
using SYT.RozetkaPay.Models.Subscriptions;
using SYT.RozetkaPay.Serialization;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Every SDK enum that mirrors a published one serializes to exactly the tokens that schema declares,
/// and reads all of them back.
/// </summary>
/// <remarks>
/// <para>
/// The members used to be annotated with <c>[JsonPropertyName]</c>, which <see cref="System.Text.Json"/>
/// ignores on enum members. The declared tokens were therefore documentation only: the wire value came
/// from the converter's naming policy instead, so <c>UK</c> went out as <c>"uk"</c> and <c>LeaseLink</c>
/// as <c>"lease_link"</c> — neither of which the provider accepts, and the README quick start used one.
/// </para>
/// <para>
/// The expected tokens are read from the pinned snapshot, never retyped here: a list copied into a test
/// can carry the same typo as the code it checks. Values are collected through <c>allOf</c> as well as
/// directly, because two published schemas inherit their entire value set that way — an earlier version
/// of this test looked only for a direct <c>enum</c> key, and was green while both of those schemas were
/// wrong in production.
/// </para>
/// <para>
/// <see cref="EveryPublishedEnumSchema_ShouldBeEitherModelledOrRecordedAsNotModelled"/> is what keeps
/// that from recurring: leaving a published schema out has to be a visible decision, not an omission.
/// </para>
/// </remarks>
public class EnumWireTokenTests
{
    /// <summary>
    /// Published enum schemas the SDK deliberately does not model, and why.
    /// </summary>
    /// <remarks>
    /// Each is the value set of a request or response field the SDK has no property for. They belong to
    /// the missing-model work tracked in the contract block; recording them here states that the gap is
    /// known rather than letting the coverage test pass in silence.
    /// </remarks>
    private static readonly Dictionary<string, string> UnmodelledSchemas = new()
    {
        ["CampaignName"] = "no SDK property carries a campaign name",
        ["FiscalizationStatus"] = "fiscalization is not modelled",
        ["FiscalizationStatusCode"] = "fiscalization is not modelled",
        ["PayPartsPaymentMethodType"] = "PayParts payment-method details are not modelled",
        ["PlanFrequencyType"] = "the plan model still carries frequency as a string",
        ["SubscriptionPaymentMethodType"] = "subscription payment-method details are not modelled",
        ["ErrorType"] = "errors surface through RozetkaPayApiError, not a typed enum",
    };

    /// <summary>
    /// Each published enum schema paired with the SDK type that mirrors it.
    /// </summary>
    /// <remarks>
    /// <c>AlternativePaymentResponseCode</c> and <c>PayPartsResponseCode</c> both map to
    /// <see cref="ResponseCode"/>: in the document each is <c>allOf: [$ref ResponseCode]</c> and nothing
    /// else, so they are that type under two names rather than two types.
    /// </remarks>
    private static readonly Dictionary<string, Type> ModelledSchemas = new()
    {
        ["ActionType"] = typeof(ActionType),
        ["AlternativePaymentMethodType"] = typeof(AlternativePaymentMethodType),
        ["AlternativePaymentOperationType"] = typeof(AlternativePaymentOperationType),
        ["AlternativePaymentProvider"] = typeof(AlternativePaymentProvider),
        ["AlternativePaymentResponseCode"] = typeof(ResponseCode),
        ["BatchMethodType"] = typeof(BatchMethodType),
        ["BatchPaymentMode"] = typeof(BatchPaymentMode),
        ["CheckoutColorMode"] = typeof(CheckoutColorMode),
        ["CustomerCheckoutLocale"] = typeof(CustomerCheckoutLocale),
        ["MerchantStatus"] = typeof(MerchantStatus),
        ["OperationStatus"] = typeof(OperationStatus),
        ["OperationType"] = typeof(OperationType),
        ["PayPartsOperationType"] = typeof(PayPartsOperationType),
        ["PayPartsPaymentMode"] = typeof(PayPartsPaymentMode),
        ["PayPartsResponseCode"] = typeof(ResponseCode),
        ["PaymentMethodType"] = typeof(PaymentMethodType),
        ["PaymentMode"] = typeof(PaymentMode),
        ["PlanState"] = typeof(PlanState),
        ["ResponseCode"] = typeof(ResponseCode),
        ["SubscriptionCallbackType"] = typeof(SubscriptionCallbackType),
        ["SubscriptionPaymentState"] = typeof(SubscriptionPaymentState),
        ["SubscriptionState"] = typeof(SubscriptionState),
    };

    public static TheoryData<string> PublishedEnumSchemas
    {
        get
        {
            TheoryData<string> data = [];
            foreach (string name in ModelledSchemas.Keys.Order())
            {
                data.Add(name);
            }

            return data;
        }
    }

    /// <summary>
    /// Every published enum schema is either mirrored by an SDK type or recorded as deliberately not
    /// modelled. One in neither list fails here rather than going unchecked.
    /// </summary>
    [Fact]
    public void EveryPublishedEnumSchema_ShouldBeEitherModelledOrRecordedAsNotModelled()
    {
        string[] unaccounted = [.. OpenApiSnapshot.EnumSchemaNames()
            .Where(name => !ModelledSchemas.ContainsKey(name) && !UnmodelledSchemas.ContainsKey(name))
            .Order()];

        Assert.Empty(unaccounted);
    }

    [Theory]
    [MemberData(nameof(PublishedEnumSchemas))]
    public void Enum_ShouldSerializeToExactlyTheDeclaredTokens(string schemaName)
    {
        HashSet<string> expected = [.. OpenApiSnapshot.EnumValues(schemaName)];
        HashSet<string> actual = [.. WireTokensOf(ModelledSchemas[schemaName])];

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(PublishedEnumSchemas))]
    public void Enum_ShouldReadBackEveryDeclaredToken(string schemaName)
    {
        // A response carrying a value the SDK cannot parse turns a successful call into a JsonException,
        // so reading matters as much as writing.
        Type enumType = ModelledSchemas[schemaName];

        foreach (string token in OpenApiSnapshot.EnumValues(schemaName))
        {
            object? parsed = JsonSerializer.Deserialize($"\"{token}\"", enumType, SdkSerializerOptions.Value);
            Assert.NotNull(parsed);
        }
    }

    /// <summary>
    /// No SDK enum accepts a bare integer off the wire, in either direction.
    /// </summary>
    /// <remarks>
    /// Every published enum is a string enum, so a number is never valid. Accepting one would also make
    /// a member's numeric identity part of the contract — and those numbers shift whenever a member is
    /// added or removed, which this change did to six enums.
    /// </remarks>
    [Theory]
    [MemberData(nameof(PublishedEnumSchemas))]
    public void Enum_ShouldRejectABareInteger(string schemaName)
    {
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize("0", ModelledSchemas[schemaName], SdkSerializerOptions.Value));
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
        // digit, which no naming policy could have derived from a C# identifier.
        Assert.Equal(
            "\"3ds_required\"",
            JsonSerializer.Serialize(ResponseCode.ThreeDsRequired, SdkSerializerOptions.Value));
    }

    /// <summary>
    /// No enum member is correct only by accident.
    /// </summary>
    /// <remarks>
    /// A member whose snake-cased name happens to equal its token needs no attribute. One whose name
    /// does not — an acronym, a dotted event name, a leading digit — is correct only until someone
    /// renames it. This sweeps every exported enum, published or not, and names the ones relying on
    /// coincidence.
    /// </remarks>
    [Fact]
    public void EveryEnumMember_ShouldSerializeToATokenItDeclares()
    {
        List<string> relyingOnCoincidence = [];

        foreach (Type enumType in typeof(ResponseCode).Assembly.GetExportedTypes().Where(static t => t.IsEnum))
        {
            foreach (object member in Enum.GetValues(enumType))
            {
                string name = Enum.GetName(enumType, member)!;
                string token = JsonSerializer.Serialize(member, enumType, SdkSerializerOptions.Value).Trim('"');

                bool annotated = enumType.GetField(name)!
                    .GetCustomAttributes()
                    .Any(static attribute => attribute.GetType().Name == "JsonStringEnumMemberNameAttribute");

                if (!annotated && token != SnakeCaseLower(name))
                {
                    relyingOnCoincidence.Add($"{enumType.Name}.{name} -> \"{token}\"");
                }
            }
        }

        Assert.Empty(relyingOnCoincidence);
    }

    private static IEnumerable<string> WireTokensOf(Type enumType)
    {
        return Enum.GetValues(enumType)
            .Cast<object>()
            .Select(member => JsonSerializer.Serialize(member, enumType, SdkSerializerOptions.Value).Trim('"'));
    }

    private static string SnakeCaseLower(string name)
    {
        return System.Text.RegularExpressions.Regex
            .Replace(name, "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", "_")
            .ToLowerInvariant();
    }
}
