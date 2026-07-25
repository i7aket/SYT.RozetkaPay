using System.Net;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Models.InStorePayments;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Models.Subscriptions;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Deterministic disposal of the <see cref="HttpResponseMessage"/> in the transport helpers EXP-354 added.
///
/// A response that is not disposed holds its content — and, on a real handler, the connection behind it —
/// until the finalizer runs. Both the success path and the error path must release it, and the error path is
/// the one that is easy to get wrong: the status-to-exception mapper throws, so disposal has to happen on
/// the way out rather than after the last statement.
///
/// Disposal is observed through <see cref="DisposalTrackingResponse"/> and
/// <see cref="DisposalTrackingContent"/>, which nothing else in the test disposes, so a set flag can only
/// have come from the SDK.
/// </summary>
public class Exp354ResponseDisposalTests
{
    private const string ErrorBody = """{"code":"rejected","message":"Provider rejected the request"}""";

    // ===================== safe-label PATCH (new EXP-354 overload) =====================

    [Fact]
    public async Task PatchWithStaticLabel_ShouldDisposeTheResponse_OnSuccess()
    {
        DisposalTrackingHandler handler = DisposalTrackingHandler.Json(
            HttpStatusCode.OK,
            """{"message":"updated"}""");
        SubscriptionService service = new(
            Exp354TestContext.CreateConfiguration(),
            Exp354TestContext.CreateHttpClient(handler));

        UpdateSubscriptionPaymentMethodResponse response =
            await service.UpdatePaymentMethodAsync("subscription-1", MinimalUpdateRequest());

        Assert.Equal("updated", response.Message);
        AssertDisposed(handler);
    }

    [Fact]
    public async Task PatchWithStaticLabel_ShouldDisposeTheResponse_OnError()
    {
        DisposalTrackingHandler handler = DisposalTrackingHandler.Json(HttpStatusCode.BadRequest, ErrorBody);
        SubscriptionService service = new(
            Exp354TestContext.CreateConfiguration(),
            Exp354TestContext.CreateHttpClient(handler));

        await Assert.ThrowsAsync<RozetkaPayValidationException>(
            () => service.UpdatePaymentMethodAsync("subscription-1", MinimalUpdateRequest()));

        // The mapper throws, so disposal must happen on the way out of the helper.
        AssertDisposed(handler);
    }

    // ===================== bodyless POST (new EXP-354 helper) =====================

    [Fact]
    public async Task PostWithoutBody_ShouldDisposeTheResponse_OnSuccess()
    {
        DisposalTrackingHandler handler = DisposalTrackingHandler.Json(
            HttpStatusCode.OK,
            """{"fc_id":"fc-1"}""");
        InStorePaymentService service = new(
            Exp354TestContext.CreateConfiguration(),
            Exp354TestContext.CreateHttpClient(handler));

        InStorePaymentInfoResponse response = await service.GetInfoAsync("payment-1");

        Assert.Equal("fc-1", response.FcId);
        AssertDisposed(handler);
    }

    [Fact]
    public async Task PostWithoutBody_ShouldDisposeTheResponse_OnError()
    {
        DisposalTrackingHandler handler = DisposalTrackingHandler.Json(HttpStatusCode.BadRequest, ErrorBody);
        InStorePaymentService service = new(
            Exp354TestContext.CreateConfiguration(),
            Exp354TestContext.CreateHttpClient(handler));

        await Assert.ThrowsAsync<RozetkaPayValidationException>(() => service.GetInfoAsync("payment-1"));

        AssertDisposed(handler);
    }

    // ===================== decline transport =====================

    /// <summary>
    /// The decline helper already used a <c>using</c> response. These two cases pin that, so the guarantee
    /// cannot be lost in a later edit.
    /// </summary>
    [Fact]
    public async Task Decline_ShouldDisposeTheResponse_OnRedirect()
    {
        DisposalTrackingHandler decline = DisposalTrackingHandler.Redirect("https://provider.example/declined");
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(decline);
        PaymentInstructionService service = Exp354TestContext
            .PaymentInstructions(RecordingHandler.Json("{}"), declineClient);

        using (service as IDisposable)
        {
            await service.DeclineAsync("project-1", "pi-1");
        }

        AssertDisposed(decline);
    }

    [Fact]
    public async Task Decline_ShouldDisposeTheResponse_OnError()
    {
        DisposalTrackingHandler decline = DisposalTrackingHandler.Json(HttpStatusCode.Conflict, ErrorBody);
        using HttpClient declineClient = Exp354TestContext.CreateDeclineHttpClient(decline);
        PaymentInstructionService service = Exp354TestContext
            .PaymentInstructions(RecordingHandler.Json("{}"), declineClient);

        using (service as IDisposable)
        {
            await Assert.ThrowsAsync<RozetkaPayException>(() => service.DeclineAsync("project-1", "pi-1"));
        }

        AssertDisposed(decline);
    }

    // ===================== legacy PATCH routed through the fixed helper =====================

    /// <summary>
    /// The pre-existing two-argument <c>PatchAsync</c> delegates to the EXP-354 static-label overload, so it
    /// inherits the disposal fix. Its signature, request target, body, and logging are unchanged; only the
    /// response lifetime improved. Pinned here so the inherited guarantee is explicit rather than incidental.
    /// </summary>
    [Fact]
    public async Task LegacyPatch_ShouldAlsoDisposeTheResponse()
    {
        DisposalTrackingHandler handler = DisposalTrackingHandler.Json(
            HttpStatusCode.OK,
            """{"id":"plan-1"}""");
        SubscriptionService service = new(
            Exp354TestContext.CreateConfiguration(),
            Exp354TestContext.CreateHttpClient(handler));

        SubscriptionPlanResponse response = await service.UpdatePlanAsync(
            "plan-1",
            new UpdateSubscriptionPlanRequest { Name = "Renamed" });

        Assert.Equal("plan-1", response.Id);
        AssertDisposed(handler);
    }

    /// <summary>
    /// Every response the handler produced must have been disposed, content included.
    /// </summary>
    private static void AssertDisposed(DisposalTrackingHandler handler)
    {
        DisposalTrackingResponse response = Assert.Single(handler.Responses);

        Assert.True(response.Disposed, "the HttpResponseMessage must be disposed by the SDK.");
        Assert.True(response.TrackedContent.Disposed, "the response content must be disposed by the SDK.");
    }

    private static UpdateSubscriptionPaymentMethodRequest MinimalUpdateRequest()
    {
        return new UpdateSubscriptionPaymentMethodRequest
        {
            PaymentMethod = new SubscriptionPaymentMethodUpdate
            {
                Type = SubscriptionPaymentMethodUpdateType.Wallet,
                Wallet = new CustomerWalletRequestPaymentMethod { OptionId = "option-1" }
            }
        };
    }
}
