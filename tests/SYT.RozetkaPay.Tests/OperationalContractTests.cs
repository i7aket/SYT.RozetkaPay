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

    /// <summary>
    /// A progressing operation must not collapse into its earlier delivery.
    /// </summary>
    /// <remarks>
    /// Found by the pre-release audit. One operation is delivered more than once as it progresses —
    /// pending, then final — and a key without the status treats the final delivery as a duplicate.
    /// A consumer following the property's own advice would reject it and never mark the payment
    /// paid: the dedup meant to protect the booking losing it instead.
    /// </remarks>
    [Fact]
    public void TheEventKey_ShouldSeparateAPendingDeliveryFromItsFinalOne()
    {
        PaymentWebhook pending = Read("""
        {"id":"pay-1","operation":"payment","is_success":false,
         "details":{"operation_id":"op-1","status":"pending"}}
        """);

        PaymentWebhook settled = Read("""
        {"id":"pay-1","operation":"payment","is_success":true,
         "details":{"operation_id":"op-1","status":"success"}}
        """);

        Assert.Equal(pending.Details!.OperationId, settled.Details!.OperationId);
        Assert.NotEqual(pending.EventKey, settled.EventKey);
    }

    private static PaymentWebhook Read(string body) =>
        JsonSerializer.Deserialize<PaymentWebhook>(body, SdkSerializerOptions.Value)!;
}
