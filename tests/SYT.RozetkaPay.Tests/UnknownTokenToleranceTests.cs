using System.Net;
using System.Text.Json;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Serialization;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// A token this SDK version does not know costs one field, not the whole response.
/// </summary>
/// <remarks>
/// <para>
/// <c>ResponseCode</c> carries 184 values and the provider adds more between releases. With a strict
/// converter, one unrecognised token made the entire reply unreadable: the payment had succeeded, the
/// money had moved, and reading it back threw permanently — as a raw <c>JsonException</c> that the
/// documented <c>catch (RozetkaPayException)</c> did not catch and that carried no body to explain
/// itself.
/// </para>
/// </remarks>
public class UnknownTokenToleranceTests
{
    [Fact]
    public void AnUnknownToken_ShouldCostOneFieldRatherThanTheResponse()
    {
        const string body = """
        {"external_id":"order-1","is_success":true,
         "purchase_details":[{"status_code":"a_code_shipped_after_this_release","amount":12.34}]}
        """;

        PaymentOperationResult result =
            JsonSerializer.Deserialize<PaymentOperationResult>(body, SdkSerializerOptions.Value)!;

        // Everything beside the unknown token survives - which is the whole point.
        Assert.Equal("order-1", result.ExternalId);
        Assert.True(result.IsSuccess);
    }

    /// <summary>
    /// A known token still reads as itself. Tolerance must not become indifference.
    /// </summary>
    [Fact]
    public void AKnownToken_ShouldStillBeRead()
    {
        const string body = """{"type":"cc_number"}""";

        CustomerRequestPaymentMethod method =
            JsonSerializer.Deserialize<CustomerRequestPaymentMethod>(body, SdkSerializerOptions.Value)!;

        Assert.Equal(SYT.RozetkaPay.Models.Common.PaymentMethodType.CCNumber, method.Type);
    }

    /// <summary>
    /// A body the SDK genuinely cannot read fails inside the documented hierarchy, carrying the body.
    /// </summary>
    [Fact]
    public async Task AnUnreadableBody_ShouldFailInsideTheHierarchyWithTheEvidenceAttached()
    {
        StubHttpMessageHandler handler = new((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"amount\": not-json }", System.Text.Encoding.UTF8, "application/json"),
            }));

        using HttpClient httpClient = new(handler);
        PaymentService service = new(Configuration(), httpClient);

        RozetkaPayException failure =
            await Assert.ThrowsAsync<RozetkaPayException>(() => service.GetInfoAsync("order-1"));

        Assert.NotNull(failure.ApiError);
        Assert.Contains("not-json", failure.ApiError!.RawBody, StringComparison.Ordinal);
        Assert.IsType<JsonException>(failure.InnerException);
    }

    /// <summary>
    /// An empty body on a <c>200</c> is an error, not an object of defaults.
    /// </summary>
    /// <remarks>
    /// After EXP-431 an all-null <c>PaymentOperationResult</c> is exactly what a successful hosted
    /// creation also looks like, so synthesising one made a truncated response indistinguishable from
    /// a real reply.
    /// </remarks>
    [Fact]
    public async Task AnEmptyBodyOnTwoHundred_ShouldNotBecomeAnObjectOfDefaults()
    {
        StubHttpMessageHandler handler = new((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) }));

        using HttpClient httpClient = new(handler);
        PaymentService service = new(Configuration(), httpClient);

        RozetkaPayException failure =
            await Assert.ThrowsAsync<RozetkaPayException>(() => service.GetInfoAsync("order-1"));

        Assert.Contains("empty body", failure.Message, StringComparison.Ordinal);
    }

    private static RozetkaPayConfiguration Configuration() => new()
    {
        BaseUrl = RozetkaPayOptions.ProductionBaseUrl,
        Login = "probe-login",
        Password = "probe-password",
        RetryPolicy = RetryPolicy.None,
    };
}
