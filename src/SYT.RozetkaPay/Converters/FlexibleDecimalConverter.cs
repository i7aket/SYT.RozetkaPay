using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SYT.RozetkaPay.Converters;

/// <summary>
/// JSON converter that handles decimal values from both string and number formats
/// Required because RozetkaPay API sometimes returns amounts as strings "0.00" instead of numbers 0.00
/// This was observed during integration testing and reported to RozetkaPay.
/// </summary>
public class FlexibleDecimalConverter : JsonConverter<decimal?>
{
    /// <summary>
    /// Read JSON value and convert to decimal, handling both string and number formats
    /// </summary>
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                // Standard case: JSON number
                return reader.GetDecimal();
                
            case JsonTokenType.String:
                // API sometimes returns amounts as strings
                string? stringValue = reader.GetString();
                if (string.IsNullOrEmpty(stringValue))
                {
                    return null;
                }
                
                if (decimal.TryParse(stringValue, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result))
                {
                    return result;
                }
                
                throw new JsonException($"Unable to convert '{stringValue}' to decimal");
                
            case JsonTokenType.Null:
                return null;
                
            default:
                throw new JsonException($"Unexpected token type {reader.TokenType} when reading decimal");
        }
    }

    /// <summary>
    /// Write decimal value as JSON number (standard format for requests)
    /// </summary>
    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
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
}

/// <summary>
/// JSON converter for non-nullable decimal values
/// </summary>
public class FlexibleDecimalConverterNonNullable : JsonConverter<decimal>
{
    /// <summary>
    /// Read JSON value and convert to decimal, handling both string and number formats
    /// </summary>
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                // Standard case: JSON number
                return reader.GetDecimal();
                
            case JsonTokenType.String:
                // API sometimes returns amounts as strings
                string? stringValue = reader.GetString();
                if (string.IsNullOrEmpty(stringValue))
                {
                    return 0m;
                }
                
                if (decimal.TryParse(stringValue, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result))
                {
                    return result;
                }
                
                throw new JsonException($"Unable to convert '{stringValue}' to decimal");
                
            case JsonTokenType.Null:
                return 0m;
                
            default:
                throw new JsonException($"Unexpected token type {reader.TokenType} when reading decimal");
        }
    }

    /// <summary>
    /// Write decimal value as JSON number (standard format for requests)
    /// </summary>
    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
} 
