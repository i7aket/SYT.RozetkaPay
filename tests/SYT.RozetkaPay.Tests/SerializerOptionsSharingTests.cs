using System.Text.Json;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Serialization;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// The SDK serializes and deserializes through one shared <see cref="JsonSerializerOptions"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="JsonSerializerOptions"/> carries the reflection-derived contract cache for every type it
/// has been asked about. A fresh instance per call throws that cache away and rebuilds it from scratch,
/// which on this SDK's own models measured at roughly three orders of magnitude more work than reusing
/// one - and the transport built a new instance twice per request, once to serialize the body and once
/// to deserialize the response.
/// </para>
/// <para>
/// Sharing is safe precisely because <see cref="System.Text.Json"/> freezes an options instance the
/// first time it is used: after that it is immutable, and the read-only cache is what makes it fast.
/// The mutation test below is what keeps that property from being quietly given up.
/// </para>
/// </remarks>
public class SerializerOptionsSharingTests
{
    [Fact]
    public void Options_ShouldBeASingleSharedInstance()
    {
        Assert.Same(SdkSerializerOptions.Value, SdkSerializerOptions.Value);
    }

    [Fact]
    public void Options_ShouldBeFrozenSoSharingCannotBeSubverted()
    {
        // Proves the instance cannot be reconfigured by one caller behind another's back. If this ever
        // starts failing, sharing has stopped being safe and the perf win has become a race.
        JsonSerializerOptions options = SdkSerializerOptions.Value;

        // Touching it once is what freezes it, in case no other test ran first.
        JsonSerializer.Serialize(1, options);

        Assert.True(options.IsReadOnly);
        Assert.Throws<InvalidOperationException>(() => options.WriteIndented = true);
    }

    [Fact]
    public void Options_ShouldKeepTheSerializationContractTheSdkHasAlwaysHad()
    {
        JsonSerializerOptions options = SdkSerializerOptions.Value;

        // Each of these is load-bearing for the wire format, so an accidental change to the extracted
        // instance is caught here rather than by a provider rejecting a request.
        Assert.Equal(JsonNamingPolicy.SnakeCaseLower, options.PropertyNamingPolicy);
        Assert.False(options.WriteIndented);
        Assert.Equal(JsonNumberHandling.AllowReadingFromString, options.NumberHandling);
        Assert.Equal(JsonIgnoreCondition.WhenWritingNull, options.DefaultIgnoreCondition);
    }
}
