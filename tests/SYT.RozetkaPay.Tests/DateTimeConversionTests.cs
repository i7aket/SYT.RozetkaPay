using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Converters;
using SYT.RozetkaPay.Serialization;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// The converter writes an ISO-8601 string ending in <c>Z</c>. That suffix is a claim about the
/// instant, and it has to be true.
/// </summary>
/// <remarks>
/// <para>
/// It used to append the literal without calling <see cref="DateTime.ToUniversalTime"/>, so a local
/// value went out shifted by the machine's offset and labelled universal. A build agent running on UTC
/// could never see it: the bug only appears where the offset is non-zero, which is everywhere the code
/// actually runs. The existing coverage passed only already-UTC values.
/// </para>
/// <para>
/// The tests below construct the expected value from the input rather than hard-coding a string, so
/// they assert the same thing in every timezone — including UTC, where the local case degenerates to
/// the already-correct one.
/// </para>
/// </remarks>
public class DateTimeConversionTests
{
    [Fact]
    public void Write_ShouldConvertLocalTimeToUtcRatherThanRelabelIt()
    {
        DateTime local = new(2026, 7, 29, 12, 0, 0, DateTimeKind.Local);
        string expected = local.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);

        string json = Serialize(new Carrier { Moment = local });

        Assert.Equal($$"""{"moment":"{{expected}}Z"}""", json);
    }

    [Fact]
    public void Write_ShouldLeaveUtcUnchanged()
    {
        DateTime utc = new(2026, 7, 29, 10, 0, 0, DateTimeKind.Utc);

        string json = Serialize(new Carrier { Moment = utc });

        Assert.Equal("""{"moment":"2026-07-29T10:00:00.000Z"}""", json);
    }

    [Fact]
    public void Write_ShouldTreatUnspecifiedAsAlreadyUtc()
    {
        // The API only ever emits UTC, so a value that lost its Kind passing through a serialization
        // layer is UTC. Guessing "local" would corrupt exactly the values that round-tripped correctly.
        DateTime unspecified = new(2026, 7, 29, 10, 0, 0, DateTimeKind.Unspecified);

        string json = Serialize(new Carrier { Moment = unspecified });

        Assert.Equal("""{"moment":"2026-07-29T10:00:00.000Z"}""", json);
    }

    [Fact]
    public void Read_ShouldReturnUtcForAUnixTimestamp()
    {
        DateTime parsed = Deserialize("0");

        Assert.Equal(DateTimeKind.Utc, parsed.Kind);
        Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), parsed);
    }

    [Fact]
    public void Read_ShouldNormalizeAnOffsetToUtc()
    {
        DateTime parsed = Deserialize("\"2026-07-29T12:00:00+02:00\"");

        Assert.Equal(DateTimeKind.Utc, parsed.Kind);
        Assert.Equal(new DateTime(2026, 7, 29, 10, 0, 0, DateTimeKind.Utc), parsed);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveTheInstantThroughALocalValue()
    {
        // The whole point, stated once: whatever the machine's offset, the instant that comes back is
        // the instant that went in.
        DateTime local = new DateTime(2026, 7, 29, 12, 0, 0, DateTimeKind.Local);

        string json = Serialize(new Carrier { Moment = local });
        DateTime parsed = JsonSerializer.Deserialize<Carrier>(json, SdkSerializerOptions.Value)!.Moment;

        Assert.Equal(local.ToUniversalTime(), parsed);
    }

    [Fact]
    public void NullableConverter_ShouldFollowTheSameRules()
    {
        DateTime local = new(2026, 7, 29, 12, 0, 0, DateTimeKind.Local);
        string expected = local.ToUniversalTime()
            .ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);

        Assert.Equal(
            $$"""{"moment":"{{expected}}Z"}""",
            JsonSerializer.Serialize(new NullableCarrier { Moment = local }, SdkSerializerOptions.Value));

        // Null is omitted rather than written, which is the SDK-wide WhenWritingNull policy.
        Assert.Equal(
            "{}",
            JsonSerializer.Serialize(new NullableCarrier(), SdkSerializerOptions.Value));
    }

    private static string Serialize(Carrier carrier)
    {
        return JsonSerializer.Serialize(carrier, SdkSerializerOptions.Value);
    }

    private static DateTime Deserialize(string momentJson)
    {
        return JsonSerializer.Deserialize<Carrier>($$"""{"moment":{{momentJson}}}""", SdkSerializerOptions.Value)!
            .Moment;
    }

    /// <summary>
    /// Mirrors how production applies the converter: per property, by attribute. The shared serializer
    /// options do not carry it, so a test serializing a bare <see cref="DateTime"/> would exercise
    /// System.Text.Json's default and prove nothing about this converter.
    /// </summary>
    private sealed class Carrier
    {
        [JsonPropertyName("moment")]
        [JsonConverter(typeof(FlexibleDateTimeConverter))]
        public DateTime Moment { get; set; }
    }

    private sealed class NullableCarrier
    {
        [JsonPropertyName("moment")]
        [JsonConverter(typeof(NullableFlexibleDateTimeConverter))]
        public DateTime? Moment { get; set; }
    }
}
