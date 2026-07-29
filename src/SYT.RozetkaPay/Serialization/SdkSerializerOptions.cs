using System.Text.Json;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Converters;

namespace SYT.RozetkaPay.Serialization;

/// <summary>
/// The single <see cref="JsonSerializerOptions"/> instance every SDK request and response is
/// serialized through.
/// </summary>
/// <remarks>
/// <para>
/// One instance, deliberately. <see cref="JsonSerializerOptions"/> carries the reflection-derived
/// contract cache for every type it has been asked about, so constructing a fresh one per call
/// discards that cache and rebuilds it: measured on this SDK's own models at roughly three orders of
/// magnitude more work than reusing one. The transport paid that twice per request - once to
/// serialize the body, once to deserialize the response - which dwarfed every allocation the SDK had
/// previously been tuned to avoid.
/// </para>
/// <para>
/// Sharing is safe because <see cref="System.Text.Json"/> freezes an options instance the first time
/// it is used for serialization: from that point it is immutable, which is both why the cache can be
/// reused across threads and why no caller can reconfigure it behind another's back. Nothing here may
/// be mutated after construction, and nothing may hand this instance to code that would try.
/// </para>
/// </remarks>
public static class SdkSerializerOptions
{
    /// <summary>
    /// The shared serializer configuration. Treat it as immutable.
    /// </summary>
    public static JsonSerializerOptions Value { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                // The naming policy stays, and every member whose token differs from its snake-cased
                // name carries [JsonStringEnumMemberName], which takes precedence. Dropping the policy
                // instead would have re-derived tokens for the enums that were already right.
                //
                // allowIntegerValues: false because every published enum is a string enum, so a number
                // is never a valid value on the wire. Accepting one would also make the numeric identity
                // of a member part of the contract, and those numbers shift whenever a member is added
                // or removed.
                new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false),
                new FlexibleDecimalConverter(),
                new FlexibleDecimalConverterNonNullable(),
                new FlexibleInt32Converter(),
                new FlexibleNullableInt32Converter(),
                new FlexibleInt64Converter(),
                new FlexibleNullableInt64Converter()
            }
        };
    }
}
