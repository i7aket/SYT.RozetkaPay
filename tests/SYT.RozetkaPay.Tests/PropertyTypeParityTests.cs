using System.Reflection;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// A property's CLR type can read what the document says the field holds.
/// </summary>
/// <remarks>
/// <para>
/// The parity work compared property <em>names</em> and required-ness and never types. Three defects
/// reached the live gateway before anyone noticed: two operations returning a bare array where the SDK
/// expected a wrapper object, and an integer field typed as a string. All three threw on the first real
/// call.
/// </para>
/// <para>
/// The rule here is compatibility rather than equality, because two mismatches are deliberate:
/// </para>
/// <list type="bullet">
/// <item>
/// A field the document calls <c>string</c> that the SDK reads as <c>decimal</c> or <c>int</c>. The API
/// returns numeric fields sometimes as numbers and sometimes as strings — a known inconsistency reported
/// to RozetkaPay and still present — so the SDK reads both through its flexible converters. Narrowing
/// these to <c>string</c> would hand every caller the parsing problem the converters exist to solve.
/// </item>
/// <item>
/// A field the document declares as an <c>enum</c> that the SDK reads as a <c>string</c>, or the
/// reverse. Both round-trip; which is better is a judgement per field, not a defect.
/// </item>
/// </list>
/// <para>
/// Anything else fails, and the exemption list below has to name the field and say why.
/// </para>
/// </remarks>
public class PropertyTypeParityTests
{
    /// <summary>
    /// Pairs of (document kind, SDK kind) that are compatible rather than identical.
    /// </summary>
    private static readonly HashSet<(string Declared, string Modelled)> Compatible =
    [
        ("enum", "string"),
        ("string", "enum"),
        ("integer", "number"),
        ("number", "integer"),

        // The flexible converters read a JSON string into a number. See the class remarks.
        //
        // Response-side only, and EXP-429 is why that qualifier now matters. Reading a number out of
        // a JSON string costs nothing and the provider genuinely sends both. WRITING a number where
        // the document declares a string is a hard 400: products with a numeric quantity answered
        // 400 invalid_request_body param: products.quantity while the same body with "quantity":"2"
        // answered 200. This blanket exemption is what hid that, so the request side is now held to
        // exact types by RequestJsonTypeTests and only responses reach this list.
        ("string", "number"),
        ("string", "integer"),
    ];

    /// <summary>
    /// Individual fields whose modelled type differs from the declared one on purpose.
    /// </summary>
    private static readonly Dictionary<string, string> AcceptedMismatches = new()
    {
        ["AlternativePaymentOperationDetails.status_code"] =
            "declared as a composed object wrapping ResponseCode; modelled as the enum itself, which is "
            + "the useful half",
        ["PayPartsOperationDetails.status_code"] =
            "declared as a composed object wrapping ResponseCode; modelled as the enum itself",
        ["SubscriptionPaymentMethod.recurrent_id"] =
            "the document gives this node a description and no type; a string is what it carries",
    };

    [Fact]
    public void EveryModelledProperty_ShouldBeAbleToReadWhatTheDocumentDeclares()
    {
        Dictionary<string, Dictionary<string, string>> modelled = ModelledKinds();
        List<string> mismatches = [];

        foreach ((string schema, IReadOnlyDictionary<string, string> declared) in OpenApiSnapshot.SchemaPropertyKinds())
        {
            if (!modelled.TryGetValue(schema, out Dictionary<string, string>? mine))
            {
                continue;
            }

            foreach ((string field, string declaredKind) in declared)
            {
                if (!mine.TryGetValue(field, out string? modelledKind) ||
                    declaredKind == modelledKind ||
                    Compatible.Contains((declaredKind, modelledKind)) ||
                    AcceptedMismatches.ContainsKey($"{schema}.{field}"))
                {
                    continue;
                }

                mismatches.Add($"{schema}.{field}: document={declaredKind} sdk={modelledKind}");
            }
        }

        Assert.Empty(mismatches.Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// The kind each SDK property can read, reduced to the vocabulary the document uses.
    /// </summary>
    private static Dictionary<string, Dictionary<string, string>> ModelledKinds()
    {
        return typeof(SYT.RozetkaPay.RozetkaPayClient).Assembly
            .GetExportedTypes()
            .Where(static type => type.IsClass && !type.IsAbstract && type.Namespace?.Contains(".Models") == true)
            .GroupBy(static type => type.Name, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.First()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(static property => property.GetCustomAttribute<JsonIgnoreAttribute>() is null)
                    .GroupBy(
                        static property =>
                            property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name,
                        StringComparer.Ordinal)
                    .ToDictionary(
                        static byName => byName.Key,
                        static byName => Kind(byName.First().PropertyType),
                        StringComparer.Ordinal),
                StringComparer.Ordinal);
    }

    private static string Kind(Type type)
    {
        Type resolved = Nullable.GetUnderlyingType(type) ?? type;

        if (resolved.IsEnum) return "enum";
        if (resolved == typeof(bool)) return "boolean";
        if (resolved == typeof(int) || resolved == typeof(long)) return "integer";
        if (resolved == typeof(decimal) || resolved == typeof(double) || resolved == typeof(float)) return "number";

        // A date is a string on the wire, whichever CLR type reads it.
        if (resolved == typeof(string) || resolved == typeof(Guid) ||
            resolved == typeof(DateTime) || resolved == typeof(DateTimeOffset) || resolved == typeof(DateOnly))
        {
            return "string";
        }

        if (resolved.IsGenericType && resolved.GetGenericTypeDefinition() == typeof(List<>)) return "array";

        return "object";
    }
}
