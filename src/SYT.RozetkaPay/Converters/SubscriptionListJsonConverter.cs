using System.Text.Json;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Models.Subscriptions;

namespace SYT.RozetkaPay.Converters;

/// <summary>
/// Maps the official <c>getSubscriptions</c> response - a root JSON array of subscriptions - onto the
/// existing <see cref="SubscriptionList"/> wrapper type (EXP-355).
/// </summary>
/// <remarks>
/// <para>
/// The public model predates the operation audit and spells the payload as
/// <c>{ "subscriptions": [...] }</c>. Replacing the type would break source and binary compatibility,
/// so the type is kept and the wire shape is corrected here instead.
/// </para>
/// <para>
/// Reading accepts both spellings: the official root array and the historical wrapper object. Writing
/// always emits the official root array, so anything this SDK serializes is readable by a consumer of
/// the documented contract. Null and empty are deliberately distinct on the way in - an official
/// <c>[]</c> yields an empty list while a wrapper carrying <c>"subscriptions": null</c> yields
/// <see langword="null"/> - and an absent list normalizes to <c>[]</c> on the way out, because the
/// official schema has no spelling for "no array at all".
/// </para>
/// <para>
/// The converter is internal: it is an implementation detail of the wire mapping and is applied to
/// <see cref="SubscriptionList"/> through <see cref="JsonConverterAttribute"/>. Nested values are
/// handled as <see cref="Subscription"/> and <see cref="List{T}"/>, never as
/// <see cref="SubscriptionList"/>, so this converter cannot re-enter itself.
/// </para>
/// </remarks>
internal sealed class SubscriptionListJsonConverter : JsonConverter<SubscriptionList>
{
    /// <summary>
    /// Property name of the historical wrapper spelling, matched exactly.
    /// </summary>
    private const string SubscriptionsPropertyName = "subscriptions";

    /// <summary>
    /// Read either the official root array or the legacy wrapper object.
    /// </summary>
    public override SubscriptionList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.StartArray => new SubscriptionList
            {
                Subscriptions = JsonSerializer.Deserialize<List<Subscription>>(ref reader, options)
            },
            JsonTokenType.StartObject => ReadLegacyWrapper(ref reader, options),
            _ => throw new JsonException(
                $"Expected the official subscription array or the legacy wrapper object, found {reader.TokenType}.")
        };
    }

    /// <summary>
    /// Write the official root array. An absent list is written as the documented empty array.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, SubscriptionList value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        foreach (Subscription subscription in value.Subscriptions ?? [])
        {
            JsonSerializer.Serialize(writer, subscription, options);
        }

        writer.WriteEndArray();
    }

    /// <summary>
    /// Read the historical <c>{ "subscriptions": [...] }</c> spelling. Unknown members are skipped so
    /// that a gateway adding a field cannot turn a readable response into a failure, but a
    /// <c>subscriptions</c> member that is neither an array nor null is a genuine shape error.
    /// </summary>
    private static SubscriptionList ReadLegacyWrapper(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        SubscriptionList result = new();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return result;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(
                    $"Expected a property name in the legacy subscription wrapper object, found {reader.TokenType}.");
            }

            bool isSubscriptions = reader.ValueTextEquals(SubscriptionsPropertyName);
            reader.Read();

            if (!isSubscriptions)
            {
                reader.Skip();
                continue;
            }

            result.Subscriptions = reader.TokenType switch
            {
                JsonTokenType.Null => null,
                JsonTokenType.StartArray => JsonSerializer.Deserialize<List<Subscription>>(ref reader, options),
                _ => throw new JsonException(
                    $"Expected an array or null for '{SubscriptionsPropertyName}', found {reader.TokenType}.")
            };
        }

        throw new JsonException("Unexpected end of the legacy subscription wrapper object.");
    }
}
