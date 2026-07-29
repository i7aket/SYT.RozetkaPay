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
                if (DateTime.TryParse(stringValue, null, DateTimeStyles.RoundtripKind, out DateTime parsedDate))
                {
                    return parsedDate.Kind == DateTimeKind.Local ? parsedDate.ToUniversalTime() : parsedDate;
                }

                // Try custom formats
                foreach (string format in DateFormats)
                {
                    if (DateTime.TryParseExact(stringValue, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime customParsedDate))
                        return DateTime.SpecifyKind(customParsedDate, DateTimeKind.Utc);
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
