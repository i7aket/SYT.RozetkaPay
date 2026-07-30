using System.Text.Json;
using System.Text.Json.Serialization;

namespace SYT.RozetkaPay.Converters;

/// <summary>
/// Reads a nullable enum, yielding <c>null</c> for a token this SDK version does not know.
/// </summary>
/// <remarks>
/// <para>
/// A closed enum is the right model for a value the provider promises to keep closed. It is the
/// wrong model for one they extend between releases, and <c>ResponseCode</c> is extended: it carries
/// 184 values today and gained several in the audit window alone.
/// </para>
/// <para>
/// With a strict converter, one unrecognised token made the <em>entire response</em> unreadable. The
/// payment had succeeded, the money had moved, and <c>GetInfoAsync</c> threw for that booking
/// permanently — as a raw <c>JsonException</c> that the caller's <c>catch (RozetkaPayException)</c>
/// did not even catch. A field the SDK cannot name is a poor reason to lose the fifty it can.
/// </para>
/// <para>
/// Only nullable properties get this. A non-nullable enum is a discriminator the SDK itself sets on
/// the way out, where an unknown value means a bug rather than a provider release, and silence would
/// hide it. The trade-off is stated plainly: the unrecognised token's text is lost, and a caller who
/// needs it must read the raw body. Losing one token beats losing the response.
/// </para>
/// </remarks>
/// <typeparam name="TEnum">The enum being read.</typeparam>
public sealed class TolerantNullableEnumConverter<TEnum> : JsonConverter<TEnum?>
    where TEnum : struct, Enum
{
    private readonly JsonConverter<TEnum> _strict;

    /// <summary>
    /// Creates the converter over the SDK's standard string-enum reading.
    /// </summary>
    /// <param name="options">Serializer options supplying the underlying enum converter.</param>
    public TolerantNullableEnumConverter(JsonSerializerOptions options)
    {
        _strict = (JsonConverter<TEnum>)new JsonStringEnumConverter(
            JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false)
            .CreateConverter(typeof(TEnum), options);
    }

    /// <inheritdoc />
    public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        // The reader cannot be rewound after a failed read, so the attempt is made on a copy and the
        // real one is advanced only once the outcome is known.
        Utf8JsonReader attempt = reader;

        try
        {
            TEnum value = _strict.Read(ref attempt, typeof(TEnum), options);
            reader = attempt;

            return value;
        }
        catch (JsonException)
        {
            reader.Skip();

            return null;
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();

            return;
        }

        _strict.Write(writer, value.Value, options);
    }
}

/// <summary>
/// Supplies <see cref="TolerantNullableEnumConverter{TEnum}"/> for every nullable enum property.
/// </summary>
public sealed class TolerantNullableEnumConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        return Nullable.GetUnderlyingType(typeToConvert)?.IsEnum == true;
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type enumType = Nullable.GetUnderlyingType(typeToConvert)!;

        return (JsonConverter)Activator.CreateInstance(
            typeof(TolerantNullableEnumConverter<>).MakeGenericType(enumType), options)!;
    }
}
