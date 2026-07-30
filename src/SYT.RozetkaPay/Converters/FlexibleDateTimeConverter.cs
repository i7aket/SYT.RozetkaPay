using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SYT.RozetkaPay.Converters;

/// <summary>
/// JSON converter for flexible DateTime handling
/// Handles various date formats that may come from RozetkaPay API
/// Based on official RozetkaPay CDN documentation: https://cdn.rozetkapay.com/public-docs/index.html
/// </summary>
public class FlexibleDateTimeConverter : JsonConverter<DateTime>
{
    private static readonly string[] DateFormats = 
    {
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-ddTHH:mm:ss.fffZ",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd",
        "dd.MM.yyyy HH:mm:ss",
        "dd.MM.yyyy"
    };

    /// <summary>
    /// Reads a JSON token and converts it to <see cref="DateTime"/>.
    /// </summary>
    /// <param name="reader">JSON reader.</param>
    /// <param name="typeToConvert">Target type.</param>
    /// <param name="options">Serializer options.</param>
    /// <returns>Parsed date-time value.</returns>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                string? stringValue = reader.GetString();
                if (string.IsNullOrEmpty(stringValue))
                    return DateTime.MinValue;

                // Try to parse as ISO format first (with timezone info)
                if (// Invariant, not the ambient culture. ISO-8601 - what this API emits - parses the same
            // everywhere, so the practical risk was low, but a non-ISO string would have parsed
            // differently per machine locale, and the exact-format fallbacks below are already
            // invariant. A converter that is culture-sensitive in one branch and not the others is a
            // trap waiting for the first server configured differently from the developer's laptop.
            DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsedDate))
                {
                    return parsedDate.Kind == DateTimeKind.Local ? parsedDate.ToUniversalTime() : parsedDate;
                }

                // Try custom formats
                foreach (string format in DateFormats)
                {
                    // AdjustToUniversal alongside AssumeUniversal, and the pair matters. AssumeUniversal
                    // alone reads the text as UTC and then hands back the LOCAL equivalent, so
                    // SpecifyKind(Utc) relabelled a converted value instead of converting it - the same
                    // relabel-versus-convert bug EXP-390 fixed elsewhere, still living here. It stayed
                    // hidden because a day-first machine culture parsed "28.02.2026" in the branch
                    // above and never reached this one.
                    if (DateTime.TryParseExact(
                            stringValue,
                            format,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                            out DateTime customParsedDate))
                    {
                        return DateTime.SpecifyKind(customParsedDate, DateTimeKind.Utc);
                    }
                }

                throw new JsonException($"Unable to parse date: {stringValue}");
                
            case JsonTokenType.Number:
                // Handle Unix timestamp (seconds since epoch)
                long unixTime = reader.GetInt64();

                // .DateTime yields Kind=Unspecified, which then serialized back out as if it were UTC
                // without ever having been converted. .UtcDateTime states what the value actually is.
                return DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
        }
        
        throw new JsonException($"Unexpected token type: {reader.TokenType}");
    }

    /// <summary>
    /// Writes a <see cref="DateTime"/> value using ISO-8601 UTC format.
    /// </summary>
    /// <param name="writer">JSON writer.</param>
    /// <param name="value">Date-time value.</param>
    /// <param name="options">Serializer options.</param>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // The trailing 'Z' asserts UTC, so the value has to actually be UTC. Appending it to a local
        // time shifted the instant by the machine's offset and then called the result universal - a bug
        // invisible on a UTC build agent and wrong everywhere else.
        //
        // Unspecified is treated as already-UTC rather than as local. The API only ever emits UTC, so a
        // value that lost its Kind passing through a serialization layer is UTC; guessing "local" would
        // corrupt it. A local value, by contrast, states what it is and is converted.
        DateTime utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value;

        writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ss.fff'Z'", CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// JSON converter for nullable DateTime
/// </summary>
public class NullableFlexibleDateTimeConverter : JsonConverter<DateTime?>
{
    /// <summary>
    /// Reads a JSON token and converts it to nullable <see cref="DateTime"/>.
    /// </summary>
    /// <param name="reader">JSON reader.</param>
    /// <param name="typeToConvert">Target type.</param>
    /// <param name="options">Serializer options.</param>
    /// <returns>Parsed date-time value or <c>null</c>.</returns>
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
            
        FlexibleDateTimeConverter converter = new FlexibleDateTimeConverter();
        return converter.Read(ref reader, typeof(DateTime), options);
    }

    /// <summary>
    /// Writes a nullable <see cref="DateTime"/> value.
    /// </summary>
    /// <param name="writer">JSON writer.</param>
    /// <param name="value">Date-time value or <c>null</c>.</param>
    /// <param name="options">Serializer options.</param>
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            FlexibleDateTimeConverter converter = new FlexibleDateTimeConverter();
            converter.Write(writer, value.Value, options);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
} 
