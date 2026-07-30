using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SYT.RozetkaPay.Converters;

/// <summary>
/// Reads an integer from a JSON string or number, and always writes it as a string.
/// </summary>
/// <remarks>
/// The integer counterpart of <see cref="DecimalAsStringConverter"/>, for fields the document
/// declares as a string while the natural CLR type is a whole number. Same split: tolerant reading
/// because the provider is inconsistent, strict writing because the gateway validates it.
/// </remarks>
public sealed class IntegerAsStringConverter : JsonConverter<int?>
{
    /// <inheritdoc />
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.Number:
                return reader.GetInt32();

            case JsonTokenType.String:
                string? text = reader.GetString();

                if (string.IsNullOrWhiteSpace(text))
                {
                    return null;
                }

                return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                    ? parsed
                    : throw new JsonException(
                        "Expected an integer in a JSON string; the value could not be parsed as one.");

            default:
                throw new JsonException(
                    $"Expected a JSON string or number for an integer, found {reader.TokenType}.");
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();

            return;
        }

        writer.WriteStringValue(value.Value.ToString(CultureInfo.InvariantCulture));
    }
}
