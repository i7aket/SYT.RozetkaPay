using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SYT.RozetkaPay.Converters;

/// <summary>
/// JSON converter that handles integer values from both string and number formats.
/// Added to tolerate numeric-string responses observed in integration testing.
/// </summary>
public class FlexibleInt32Converter : JsonConverter<int>
{
    /// <summary>
    /// Read JSON value and convert to int.
    /// </summary>
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetInt32(),
            JsonTokenType.String => Parse(reader.GetString()),
            JsonTokenType.Null => 0,
            _ => throw new JsonException($"Unexpected token type {reader.TokenType} when reading int")
        };
    }

    /// <summary>
    /// Write int value as JSON number.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }

    private static int Parse(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return 0;
        }

        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
        {
            return intValue;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decimalValue))
        {
            if (decimal.Truncate(decimalValue) != decimalValue)
            {
                throw new JsonException($"Unable to convert '{rawValue}' to int without precision loss");
            }
            return (int)decimalValue;
        }

        throw new JsonException($"Unable to convert '{rawValue}' to int");
    }
}

/// <summary>
/// JSON converter that handles nullable integer values from both string and number formats.
/// </summary>
public class FlexibleNullableInt32Converter : JsonConverter<int?>
{
    /// <summary>
    /// Read JSON value and convert to nullable int.
    /// </summary>
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetInt32(),
            JsonTokenType.String => ParseNullable(reader.GetString()),
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Unexpected token type {reader.TokenType} when reading nullable int")
        };
    }

    /// <summary>
    /// Write nullable int value as JSON number.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }

    private static int? ParseNullable(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
        {
            return intValue;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decimalValue))
        {
            if (decimal.Truncate(decimalValue) != decimalValue)
            {
                throw new JsonException($"Unable to convert '{rawValue}' to nullable int without precision loss");
            }
            return (int)decimalValue;
        }

        throw new JsonException($"Unable to convert '{rawValue}' to nullable int");
    }
}

/// <summary>
/// JSON converter that handles long values from both string and number formats.
/// </summary>
public class FlexibleInt64Converter : JsonConverter<long>
{
    /// <summary>
    /// Read JSON value and convert to long.
    /// </summary>
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetInt64(),
            JsonTokenType.String => Parse(reader.GetString()),
            JsonTokenType.Null => 0L,
            _ => throw new JsonException($"Unexpected token type {reader.TokenType} when reading long")
        };
    }

    /// <summary>
    /// Write long value as JSON number.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }

    private static long Parse(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return 0L;
        }

        if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue))
        {
            return longValue;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decimalValue))
        {
            if (decimal.Truncate(decimalValue) != decimalValue)
            {
                throw new JsonException($"Unable to convert '{rawValue}' to long without precision loss");
            }
            return (long)decimalValue;
        }

        throw new JsonException($"Unable to convert '{rawValue}' to long");
    }
}

/// <summary>
/// JSON converter that handles nullable long values from both string and number formats.
/// </summary>
public class FlexibleNullableInt64Converter : JsonConverter<long?>
{
    /// <summary>
    /// Read JSON value and convert to nullable long.
    /// </summary>
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetInt64(),
            JsonTokenType.String => ParseNullable(reader.GetString()),
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Unexpected token type {reader.TokenType} when reading nullable long")
        };
    }

    /// <summary>
    /// Write nullable long value as JSON number.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }

    private static long? ParseNullable(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue))
        {
            return longValue;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decimalValue))
        {
            if (decimal.Truncate(decimalValue) != decimalValue)
            {
                throw new JsonException($"Unable to convert '{rawValue}' to nullable long without precision loss");
            }
            return (long)decimalValue;
        }

        throw new JsonException($"Unable to convert '{rawValue}' to nullable long");
    }
}
