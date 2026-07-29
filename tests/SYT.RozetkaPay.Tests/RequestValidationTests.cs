using System.Net;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// A request that violates its own annotations is refused before it reaches the transport.
/// </summary>
/// <remarks>
/// <para>
/// The models carried 202 <c>[Required]</c> attributes and the SDK never read one of them. They
/// documented an intention nothing acted on, which is worse than carrying none: a reader reasonably
/// assumes a marked field is checked.
/// </para>
/// <para>
/// This lands last among the contract work, and the order was deliberate. Sixteen of those annotations
/// contradicted the published document, so turning validation on any earlier would have begun rejecting
/// requests the provider accepts.
/// </para>
/// </remarks>
public class RequestValidationTests
{
    [Fact]
    public async Task AnInvalidBody_ShouldBeRefusedBeforeTheTransportIsTouched()
    {
        int attempts = 0;
        StubHttpMessageHandler handler = new((_, _) =>
        {
            attempts++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}"),
            });
        });

        using HttpClient httpClient = new(handler);
        PaymentService service = new(Configuration(), httpClient);

        // external_id is required by the document and marked required on the model. Empty is what a
        // caller who forgot it actually sends, and the provider's own at-most-one-success guarantee is
        // keyed on this field — so an empty one also silently removes the protection against a retry
        // charging twice.
        RozetkaPayValidationException failure = await Assert.ThrowsAsync<RozetkaPayValidationException>(
            () => service.CreateAsync(new CreatePaymentRequest
            {
                Amount = 10m,
                Currency = "UAH",
                ExternalId = string.Empty,
            }));

        Assert.Equal(0, attempts);
        Assert.Contains("does not satisfy the contract", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AValidBody_ShouldReachTheTransportUnchanged()
    {
        string? body = null;
        StubHttpMessageHandler handler = new(async (request, token) =>
        {
            body = request.Content is null ? null : await request.Content.ReadAsStringAsync(token);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });

        using HttpClient httpClient = new(handler);
        PaymentService service = new(Configuration(), httpClient);

        await service.CreateAsync(new CreatePaymentRequest
        {
            Amount = 10m,
            Currency = "UAH",
            ExternalId = "order-1",
            Mode = PaymentMode.Hosted,
        });

        Assert.NotNull(body);
        Assert.Contains("\"external_id\":\"order-1\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AViolationMessage_ShouldNameTheFieldWithoutQuotingItsValue()
    {
        StubHttpMessageHandler handler = new((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") }));

        using HttpClient httpClient = new(handler);
        PaymentService service = new(Configuration(), httpClient);

        const string secretish = "4111111111111111";

        RozetkaPayValidationException failure = await Assert.ThrowsAsync<RozetkaPayValidationException>(
            () => service.CreateAsync(new CreatePaymentRequest
            {
                Amount = 10m,
                Currency = "UAH",
                ExternalId = string.Empty,
                Description = secretish,
                Metadata = new Dictionary<string, string> { ["pan"] = secretish },
            }));

        // A validation message is a log line waiting to happen, and a request body can carry a card
        // number. Field names and rule text only.
        Assert.DoesNotContain(secretish, failure.Message, StringComparison.Ordinal);
    }

    private static RozetkaPayConfiguration Configuration() => new()
    {
        BaseUrl = RozetkaPayOptions.ProductionBaseUrl,
        Login = "test-login",
        Password = "test-password",
        RetryPolicy = RetryPolicy.None,
    };
}
