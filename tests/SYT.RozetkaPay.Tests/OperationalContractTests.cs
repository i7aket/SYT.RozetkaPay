using System.Text.Json;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Serialization;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Things a consumer needs to operate this safely, rather than things the document declares.
/// </summary>
public class OperationalContractTests
{
    /// <summary>
    /// Two different events for one payment must not collapse into one key.
    /// </summary>
    /// <remarks>
    /// The obvious choice is wrong, which is why the SDK now names the right one. <c>Id</c> is the
    /// <em>payment</em> identifier, identical across every delivery for that payment, so deduplicating
    /// on it silently drops the refund notification for a payment already seen — the failure mode
    /// being guarded against, arriving through the guard itself.
    /// </remarks>
    [Fact]
    public void TheEventKey_ShouldSeparateDifferentEventsForOnePayment()
    {
        PaymentWebhook payment = Read("""
        {"id":"393352880498675712","operation":"payment","details":{"operation_id":"op-1"}}
        """);

        PaymentWebhook refund = Read("""
        {"id":"393352880498675712","operation":"refund","details":{"operation_id":"op-2"}}
        """);

        Assert.Equal(payment.Id, refund.Id);
        Assert.NotEqual(payment.EventKey, refund.EventKey);
    }

    /// <summary>
    /// Two deliveries of the same event must collapse. That is what makes it a dedup key.
    /// </summary>
    [Fact]
    public void TheEventKey_ShouldCollapseARedelivery()
    {
        const string body = """
        {"id":"393352880498675712","operation":"payment","details":{"operation_id":"op-1"}}
        """;

        Assert.Equal(Read(body).EventKey, Read(body).EventKey);
    }

    /// <summary>
    /// A payload without an operation id still yields a usable key.
    /// </summary>
    /// <remarks>
    /// <c>Details.OperationId</c> is nullable. Falling back to payment plus operation keeps events of
    /// different kinds distinct, which is the property that matters; two deliveries of the same kind
    /// for one payment collapse, which is what deduplication is for.
    /// </remarks>
    [Fact]
    public void TheEventKey_ShouldStillSeparateByOperationWithoutAnOperationId()
    {
        PaymentWebhook payment = Read("""{"id":"pay-1","operation":"payment"}""");
        PaymentWebhook refund = Read("""{"id":"pay-1","operation":"refund"}""");

        Assert.NotEqual(payment.EventKey, refund.EventKey);
    }

    /// <summary>
    /// The key is not sent back to the provider.
    /// </summary>
    [Fact]
    public void TheEventKey_ShouldNotBeSerialized()
    {
        string json = JsonSerializer.Serialize(Read("""{"id":"pay-1"}"""), SdkSerializerOptions.Value);

        Assert.DoesNotContain("EventKey", json, StringComparison.OrdinalIgnoreCase);
    }

    private static PaymentWebhook Read(string body) =>
        JsonSerializer.Deserialize<PaymentWebhook>(body, SdkSerializerOptions.Value)!;
}
