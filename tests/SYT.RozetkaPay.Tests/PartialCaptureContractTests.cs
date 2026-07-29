using System.Text.Json;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Serialization;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Partial capture and partial cancel are published operations, and the SDK could express neither.
/// </summary>
/// <remarks>
/// <para>
/// Both request bodies declare <c>amount</c>, <c>currency</c>, <c>products</c>, <c>payload</c> and
/// <c>callback_url</c> alongside the identifier, with only <c>external_id</c> required. Confirming or
/// cancelling part of a payment is what those fields are for.
/// </para>
/// <para>
/// <c>CancelPaymentRequest</c> instead carried a <c>reason</c> field that appears nowhere in the
/// document. A caller filling it in was writing to a field the gateway ignores.
/// </para>
/// </remarks>
public class PartialCaptureContractTests
{
    // The property-set comparison for these two bodies lives in RequestBodyParityTests, which applies
    // the same check to every reconciled body. What is left here is what is specific to them: that a
    // partial amount actually reaches the wire, and that a full-amount call is unchanged.

    [Fact]
    public void ConfirmPaymentRequest_ShouldSerializeAPartialCapture()
    {
        ConfirmPaymentRequest request = new()
        {
            ExternalId = "order-1",
            Amount = 12.34m,
            Currency = "UAH",
            Payload = "payload-1",
            CallbackUrl = "https://merchant.example/callback",
            Products = [new Product { Sku = "sku-1", Name = "Item", Quantity = 1, Price = 12.34m }],
        };

        string json = JsonSerializer.Serialize(request, SdkSerializerOptions.Value);

        Assert.Contains("\"amount\":12.34", json, StringComparison.Ordinal);
        Assert.Contains("\"currency\":\"UAH\"", json, StringComparison.Ordinal);
        Assert.Contains("\"payload\":\"payload-1\"", json, StringComparison.Ordinal);
        Assert.Contains("\"callback_url\":\"https://merchant.example/callback\"", json, StringComparison.Ordinal);
        Assert.Contains("\"products\":[", json, StringComparison.Ordinal);
    }

    [Fact]
    public void CancelPaymentRequest_ShouldSerializeAPartialCancel()
    {
        CancelPaymentRequest request = new()
        {
            ExternalId = "order-1",
            Amount = 5m,
            Currency = "UAH",
        };

        string json = JsonSerializer.Serialize(request, SdkSerializerOptions.Value);

        Assert.Contains("\"amount\":5", json, StringComparison.Ordinal);
        Assert.Contains("\"currency\":\"UAH\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void OmittedOptionalFields_ShouldNotReachTheWire()
    {
        // The full-amount call must stay exactly what it was: an identifier and nothing else. Sending
        // a null amount would be a different request from sending no amount.
        CancelPaymentRequest request = new() { ExternalId = "order-1" };

        string json = JsonSerializer.Serialize(request, SdkSerializerOptions.Value);

        Assert.Equal("""{"external_id":"order-1"}""", json);
    }

    [Fact]
    public void CancelPaymentRequest_ShouldNotCarryAnInventedReasonField()
    {
        Assert.Null(typeof(CancelPaymentRequest).GetProperty("Reason"));
    }
}
