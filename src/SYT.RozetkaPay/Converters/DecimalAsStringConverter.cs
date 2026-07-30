using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SYT.RozetkaPay.Converters;

/// <summary>
/// Reads a decimal from a JSON string or number, and always writes it as a string.
/// </summary>
/// <remarks>
/// <para>
/// For the fields the document declares as <c>{"type":"string","format":"decimal"}</c> — every
/// <c>Amount</c>, and <c>Product.quantity</c>. <see cref="FlexibleDecimalConverter"/> reads the same
/// two shapes but writes a number, which the gateway rejects: a payment carrying
/// <c>products[].quantity</c> as a JSON number answers
/// <c>400 invalid_request_body param: products.quantity</c>, while the identical body with
/// <c>"quantity":"2"</c> answers <c>200</c>.
/// </para>
/// <para>
/// Reading stays tolerant because the provider is inconsistent in that direction and being strict
/// would break responses that work today. Writing is strict because there is one correct answer and
/// the wire is where it matters.
/// </para>
/// <para>
/// The property stays <c>decimal?</c> rather than becoming <c>string</c>. Retyping it would hand
/// every caller the parsing and formatting problem this converter exists to solve, and invite the
/// culture bug that <c>value.ToString()</c> produces under a comma-decimal locale.
/// </para>
/// </remarks>
public sealed class DecimalAsStringConverter : JsonConverter<decimal?>
{
    /// <inheritdoc />
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.Number:
                return reader.GetDecimal();

            case JsonTokenType.String:
                string? text = reader.GetString();

                if (string.IsNullOrWhiteSpace(text))
                {
                    return null;
                }

                // Invariant, never the ambient culture: the wire format is the wire format whatever
                // locale the process happens to run under.
                return decimal.TryParse(
                    text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed)
                    ? parsed
                    : throw new JsonException(
                        $"Expected a decimal in a JSON string; the value could not be parsed as one.");

            default:
                throw new JsonException(
                    $"Expected a JSON string or number for a decimal, found {reader.TokenType}.");
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();

            return;
        }

        writer.WriteStringValue(value.Value.ToString(CultureInfo.InvariantCulture));
    }
}
