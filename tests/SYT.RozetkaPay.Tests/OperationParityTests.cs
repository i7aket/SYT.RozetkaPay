using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Customers;
using SYT.RozetkaPay.Models.Subscriptions;
using SYT.RozetkaPay.Services;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Operation-level parity with the pinned OpenAPI document (EXP-355).
///
/// Three published operations were reachable only through a legacy verb/path/body/response shape:
/// <c>deleteCustomerPayment</c>, <c>getSubscriptions</c> and <c>CancelCustomerSubscription</c>.
/// The canonical members added here call the documented operation; the legacy members keep their
/// old wire behavior and are obsolete.
///
/// Expected request targets and bodies are written as literal strings on purpose. Deriving them
/// from <see cref="Uri.EscapeDataString"/>, from <see cref="JsonSerializer"/> or from the production
/// helpers would mirror the implementation and would not detect escaping the wrong value, escaping
/// at the wrong insertion point, escaping twice, or emitting the wrong verb.
///
/// Every request is intercepted by <see cref="ParityRecordingHandler"/> against the fake host
/// <c>https://unit.test</c>, so no test can reach RozetkaPay.
/// </summary>
public class OperationParityTests
{
    // Raw caller input -> single-pass percent-encoded value.
    // "id +/&=?#% Привіт" => space '+' '/' '&' '=' '?' '#' '%' space + UTF-8 octets of "Привіт".
    private const string HostileRawId = "id +/&=?#% Привіт";

    private const string HostileEncodedId =
        "id%20%2B%2F%26%3D%3F%23%25%20%D0%9F%D1%80%D0%B8%D0%B2%D1%96%D1%82";

    // Caller input is raw, never pre-encoded: a literal '%' becomes "%25" exactly once.
    private const string LooksEncodedRawId = "already%2Fencoded";

    private const string LooksEncodedExpected = "already%252Fencoded";

    private const string JsonContentType = "application/json; charset=utf-8";

    // ===================== A. Canonical wallet delete =====================
    // DELETE /api/customers/v1/wallet  (operationId: deleteCustomerPayment)

    [Fact]
    public async Task DeleteCustomerPayment_WithExternalId_ShouldMatchTheOfficialOperation()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json(
            """{"delete":true,"option_id":"opt-1","type":"card"}""");

        DeleteCustomerPaymentResult result = await ParityTestContext.Customers(handler)
            .DeleteCustomerPaymentAsync(
                "customer-9",
                new DeleteCustomerPaymentRequest { OptionId = "opt-1", Type = "card" });

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, recorded.Method);
        Assert.Equal("/api/customers/v1/wallet?external_id=customer-9", recorded.RequestUri.PathAndQuery);
        Assert.Equal("/api/customers/v1/wallet", recorded.RequestUri.AbsolutePath);
        Assert.Equal(string.Empty, recorded.RequestUri.Fragment);
        Assert.Equal("""{"option_id":"opt-1","type":"card"}""", recorded.Body);
        Assert.Equal(JsonContentType, recorded.ContentType);

        Assert.True(result.Delete);
        Assert.Equal("opt-1", result.OptionId);
        Assert.Equal(PaymentMethodType.Card, result.Type);
    }

    [Fact]
    public async Task DeleteCustomerPayment_WithoutExternalId_ShouldSendNoQueryAndCarryCustomerAuth()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json(
            """{"delete":true,"option_id":"opt-2","type":"card"}""");

        DeleteCustomerPaymentResult result = await ParityTestContext
            .Customers(handler, ParityTestContext.WithCustomerAuth())
            .DeleteCustomerPaymentAsync(new DeleteCustomerPaymentRequest { OptionId = "opt-2", Type = "card" });

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, recorded.Method);
        Assert.Equal("/api/customers/v1/wallet", recorded.RequestUri.PathAndQuery);
        Assert.Equal(string.Empty, recorded.RequestUri.Query);
        Assert.Equal("""{"option_id":"opt-2","type":"card"}""", recorded.Body);
        Assert.Equal(
            ParityTestContext.CustomerAuthPlaceholder,
            Assert.Single(recorded.HeaderValues("X-CUSTOMER-AUTH")));
        Assert.True(result.Delete);
    }

    [Fact]
    public async Task DeleteCustomerPayment_ShouldEscapeHostileExternalIdAsOneQueryValue()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");

        await ParityTestContext.Customers(handler)
            .DeleteCustomerPaymentAsync(HostileRawId, new DeleteCustomerPaymentRequest { OptionId = "opt-1" });

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(
            $"/api/customers/v1/wallet?external_id={HostileEncodedId}",
            recorded.RequestUri.PathAndQuery);
        Assert.Equal("/api/customers/v1/wallet", recorded.RequestUri.AbsolutePath);
        Assert.Equal(string.Empty, recorded.RequestUri.Fragment);
        Assert.Equal(new[] { "external_id" }, ParityTestContext.QueryKeys(recorded.RequestUri));
    }

    [Fact]
    public async Task DeleteCustomerPayment_ShouldTreatPercentLookingExternalIdAsRawValue()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");

        await ParityTestContext.Customers(handler)
            .DeleteCustomerPaymentAsync(
                LooksEncodedRawId,
                new DeleteCustomerPaymentRequest { OptionId = "opt-1" });

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(
            $"/api/customers/v1/wallet?external_id={LooksEncodedExpected}",
            recorded.RequestUri.PathAndQuery);
    }

    [Fact]
    public async Task DeleteCustomerPayment_ShouldRejectNullRequestBeforeAnyTransport()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");

        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => ParityTestContext.Customers(handler).DeleteCustomerPaymentAsync(null!));

        Assert.Equal("request", exception.ParamName);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task DeleteCustomerPayment_ExternalIdOverload_ShouldRejectNullRequestBeforeAnyTransport()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");

        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => ParityTestContext.Customers(handler).DeleteCustomerPaymentAsync("customer-9", null!));

        Assert.Equal("request", exception.ParamName);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task DeleteCustomerPayment_ShouldPropagateCancellationToTheHandler()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");
        using CancellationTokenSource source = new();
        handler.OnRequest = (_, _) => source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ParityTestContext.Customers(handler).DeleteCustomerPaymentAsync(
                new DeleteCustomerPaymentRequest { OptionId = "opt-1" },
                source.Token));

        // The handler saw the caller token, not CancellationToken.None: cancelling the source from
        // inside the handler is observable on the token the transport received.
        Assert.True(Assert.Single(handler.Requests).CancellationObservedAfterCancel);
    }

    [Fact]
    public async Task DeleteCustomerPayment_ShouldNotSendAnythingWhenTheTokenIsAlreadyCancelled()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");
        using CancellationTokenSource source = new();
        await source.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ParityTestContext.Customers(handler).DeleteCustomerPaymentAsync(
                new DeleteCustomerPaymentRequest { OptionId = "opt-1" },
                source.Token));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task DeleteCustomerPayment_ExternalIdOverload_ShouldNotSendAnythingWhenTheTokenIsAlreadyCancelled()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");
        using CancellationTokenSource source = new();
        await source.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ParityTestContext.Customers(handler).DeleteCustomerPaymentAsync(
                "customer-9",
                new DeleteCustomerPaymentRequest { OptionId = "opt-1" },
                source.Token));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task DeleteCustomerPayment_ShouldNotFallBackToTheLegacyRouteOnNotFound()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Error(HttpStatusCode.NotFound);

        await Assert.ThrowsAsync<RozetkaPayNotFoundException>(
            () => ParityTestContext.Customers(handler).DeleteCustomerPaymentAsync(
                "customer-9",
                new DeleteCustomerPaymentRequest { OptionId = "opt-1" }));

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, recorded.Method);
        Assert.Equal("/api/customers/v1/wallet", recorded.RequestUri.AbsolutePath);
        Assert.DoesNotContain("/cards/", recorded.RequestUri.AbsolutePath, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task DeleteCustomerPayment_ShouldMakeExactlyOneRequestOnAnyFailure(HttpStatusCode status)
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Error(status);

        await Assert.ThrowsAnyAsync<RozetkaPayException>(
            () => ParityTestContext.Customers(handler).DeleteCustomerPaymentAsync(
                "customer-9",
                new DeleteCustomerPaymentRequest { OptionId = "opt-1" }));

        Assert.Single(handler.Requests);
    }

    // ===================== B. Canonical subscription list =====================
    // GET /api/subscriptions/v1/subscriptions  (operationId: getSubscriptions)

    [Fact]
    public async Task GetSubscriptions_WithExternalId_ShouldMatchTheOfficialOperation()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("[]");

        await ParityTestContext.Subscriptions(handler).GetSubscriptionsAsync("customer-9");

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, recorded.Method);
        Assert.Equal(
            "/api/subscriptions/v1/subscriptions?external_id=customer-9",
            recorded.RequestUri.PathAndQuery);
        Assert.Equal("/api/subscriptions/v1/subscriptions", recorded.RequestUri.AbsolutePath);
        Assert.Null(recorded.Body);
    }

    [Fact]
    public async Task GetSubscriptions_WithoutExternalId_ShouldSendNoQueryAndCarryCustomerAuth()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("[]");

        await ParityTestContext
            .Subscriptions(handler, ParityTestContext.WithCustomerAuth())
            .GetSubscriptionsAsync();

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, recorded.Method);
        Assert.Equal("/api/subscriptions/v1/subscriptions", recorded.RequestUri.PathAndQuery);
        Assert.Equal(string.Empty, recorded.RequestUri.Query);

        // No artificial customer path segment: the official operation identifies the customer by
        // header or query only.
        Assert.DoesNotContain("/customer/", recorded.RequestUri.AbsolutePath, StringComparison.Ordinal);
        Assert.Equal(
            ParityTestContext.CustomerAuthPlaceholder,
            Assert.Single(recorded.HeaderValues("X-CUSTOMER-AUTH")));
    }

    [Fact]
    public async Task GetSubscriptions_ShouldEscapeHostileExternalIdAsOneQueryValue()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("[]");

        await ParityTestContext.Subscriptions(handler).GetSubscriptionsAsync(HostileRawId);

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(
            $"/api/subscriptions/v1/subscriptions?external_id={HostileEncodedId}",
            recorded.RequestUri.PathAndQuery);
        Assert.Equal("/api/subscriptions/v1/subscriptions", recorded.RequestUri.AbsolutePath);
        Assert.Equal(string.Empty, recorded.RequestUri.Fragment);
        Assert.Equal(new[] { "external_id" }, ParityTestContext.QueryKeys(recorded.RequestUri));
    }

    [Fact]
    public async Task GetSubscriptions_ShouldTreatPercentLookingExternalIdAsRawValue()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("[]");

        await ParityTestContext.Subscriptions(handler).GetSubscriptionsAsync(LooksEncodedRawId);

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(
            $"/api/subscriptions/v1/subscriptions?external_id={LooksEncodedExpected}",
            recorded.RequestUri.PathAndQuery);
    }

    [Fact]
    public async Task GetSubscriptions_ShouldReadTheOfficialEmptyRootArrayAsAnEmptyList()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("[]");

        SubscriptionList result = await ParityTestContext.Subscriptions(handler).GetSubscriptionsAsync();

        Assert.NotNull(result.Subscriptions);
        Assert.Empty(result.Subscriptions);
    }

    [Fact]
    public async Task GetSubscriptions_ShouldReadTheOfficialRootArrayIntoTypedSubscriptions()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json(
            """
            [
              {
                "id": "sub-1",
                "plan_id": "plan-1",
                "state": "active",
                "price": 149.99,
                "currency": "UAH",
                "auto_renew": true,
                "created_at": "2026-07-25T10:11:12Z"
              },
              { "id": "sub-2", "state": "pending" }
            ]
            """);

        SubscriptionList result = await ParityTestContext.Subscriptions(handler).GetSubscriptionsAsync();

        Assert.NotNull(result.Subscriptions);
        Assert.Equal(2, result.Subscriptions.Count);

        Subscription first = result.Subscriptions[0];
        Assert.Equal("sub-1", first.Id);
        Assert.Equal("plan-1", first.PlanId);
        Assert.Equal(SubscriptionState.Active, first.State);
        Assert.Equal(149.99m, first.Price);
        Assert.Equal("UAH", first.Currency);
        Assert.True(first.AutoRenew);
        Assert.Equal(new DateTime(2026, 7, 25, 10, 11, 12, DateTimeKind.Utc), first.CreatedAt!.Value.ToUniversalTime());

        Assert.Equal("sub-2", result.Subscriptions[1].Id);
        Assert.Equal(SubscriptionState.Pending, result.Subscriptions[1].State);
    }

    [Fact]
    public async Task GetSubscriptions_ShouldStillReadTheLegacyWrapperObjectSpelling()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json(
            """{"subscriptions":[{"id":"sub-3","state":"inactive"}]}""");

        SubscriptionList result = await ParityTestContext.Subscriptions(handler).GetSubscriptionsAsync();

        Subscription only = Assert.Single(result.Subscriptions!);
        Assert.Equal("sub-3", only.Id);
        Assert.Equal(SubscriptionState.Inactive, only.State);
    }

    [Fact]
    public async Task GetSubscriptions_ShouldKeepAnExplicitWrapperNullDistinctFromAnEmptyArray()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("""{"subscriptions":null}""");

        SubscriptionList result = await ParityTestContext.Subscriptions(handler).GetSubscriptionsAsync();

        Assert.Null(result.Subscriptions);
    }

    [Fact]
    public void SubscriptionList_ShouldSerializeToTheOfficialRootArray()
    {
        SubscriptionList list = new()
        {
            Subscriptions = [new Subscription { Id = "sub-1", State = SubscriptionState.Active }]
        };

        string json = JsonSerializer.Serialize(list, ParityTestContext.SerializerOptions());

        Assert.StartsWith("[", json, StringComparison.Ordinal);
        Assert.EndsWith("]", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"subscriptions\"", json, StringComparison.Ordinal);
        Assert.Contains("\"id\":\"sub-1\"", json, StringComparison.Ordinal);
        Assert.Contains("\"state\":\"active\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SubscriptionList_ShouldSerializeAnAbsentListAsTheOfficialEmptyArray()
    {
        // The official response has no spelling for "no array at all"; normalizing to the documented
        // empty array keeps every serialized SubscriptionList readable by an official consumer.
        string json = JsonSerializer.Serialize(new SubscriptionList(), ParityTestContext.SerializerOptions());

        Assert.Equal("[]", json);
    }

    [Fact]
    public void SubscriptionList_ShouldRoundTripTheOfficialRootArray()
    {
        JsonSerializerOptions options = ParityTestContext.SerializerOptions();
        SubscriptionList original = new()
        {
            Subscriptions = [new Subscription { Id = "sub-1" }, new Subscription { Id = "sub-2" }]
        };

        SubscriptionList? round = JsonSerializer.Deserialize<SubscriptionList>(
            JsonSerializer.Serialize(original, options),
            options);

        Assert.NotNull(round);
        Assert.Equal(new[] { "sub-1", "sub-2" }, round.Subscriptions!.Select(item => item.Id));
    }

    [Theory]
    [InlineData("\"a string root\"")]
    [InlineData("42")]
    [InlineData("true")]
    [InlineData("""{"subscriptions":42}""")]
    [InlineData("""{"subscriptions":"not-an-array"}""")]
    public void SubscriptionList_ShouldRejectAnUnrelatedRootShape(string payload)
    {
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<SubscriptionList>(payload, ParityTestContext.SerializerOptions()));
    }

    [Fact]
    public async Task GetSubscriptions_ShouldSurfaceAJsonNullBodyAsTheExistingDeserializationFailure()
    {
        // Documented existing serializer contract: a body that deserializes to null is an SDK error,
        // not an empty list. Pinned here so the converter cannot quietly change it.
        ParityRecordingHandler handler = ParityRecordingHandler.Json("null");

        RozetkaPayException exception = await Assert.ThrowsAsync<RozetkaPayException>(
            () => ParityTestContext.Subscriptions(handler).GetSubscriptionsAsync());

        Assert.Equal("Unable to deserialize API response", exception.Message);
    }

    [Fact]
    public async Task GetSubscriptions_ShouldPropagateCancellationToTheHandler()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("[]");
        using CancellationTokenSource source = new();
        handler.OnRequest = (_, _) => source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ParityTestContext.Subscriptions(handler).GetSubscriptionsAsync(source.Token));

        Assert.True(Assert.Single(handler.Requests).CancellationObservedAfterCancel);
    }

    [Fact]
    public async Task GetSubscriptions_ExternalIdOverload_ShouldPropagateCancellationToTheHandler()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("[]");
        using CancellationTokenSource source = new();
        handler.OnRequest = (_, _) => source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ParityTestContext.Subscriptions(handler).GetSubscriptionsAsync("customer-9", source.Token));

        Assert.True(Assert.Single(handler.Requests).CancellationObservedAfterCancel);
    }

    [Fact]
    public async Task GetSubscriptions_ShouldNotFallBackToTheLegacyCustomerRouteOnNotFound()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Error(HttpStatusCode.NotFound);

        await Assert.ThrowsAsync<RozetkaPayNotFoundException>(
            () => ParityTestContext.Subscriptions(handler).GetSubscriptionsAsync("customer-9"));

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal("/api/subscriptions/v1/subscriptions", recorded.RequestUri.AbsolutePath);
        Assert.DoesNotContain("/customer/", recorded.RequestUri.AbsolutePath, StringComparison.Ordinal);
    }

    // ===================== C. Canonical subscription cancel =====================
    // DELETE /api/subscriptions/v1/subscriptions/{subscription_id}/cancel
    // (operationId: CancelCustomerSubscription)

    [Fact]
    public async Task CancelCustomerSubscription_WithoutOptions_ShouldMatchTheOfficialOperation()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("""{"message":"cancelled"}""");

        DefaultResponse response = await ParityTestContext.Subscriptions(handler)
            .CancelCustomerSubscriptionAsync("sub-1");

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, recorded.Method);
        Assert.Equal(
            "/api/subscriptions/v1/subscriptions/sub-1/cancel",
            recorded.RequestUri.PathAndQuery);
        Assert.Equal(string.Empty, recorded.RequestUri.Query);
        Assert.Null(recorded.Body);
        Assert.Equal("cancelled", response.Message);
    }

    [Fact]
    public async Task CancelCustomerSubscription_WithExternalIdOnly_ShouldSendOnlyThatQueryParameter()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");

        await ParityTestContext.Subscriptions(handler).CancelCustomerSubscriptionAsync(
            "sub-1",
            new CancelCustomerSubscriptionOptions { ExternalId = "customer-9" });

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, recorded.Method);
        Assert.Equal(
            "/api/subscriptions/v1/subscriptions/sub-1/cancel?external_id=customer-9",
            recorded.RequestUri.PathAndQuery);
        Assert.Null(recorded.Body);
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public async Task CancelCustomerSubscription_WithRefundOnly_ShouldRenderLowercaseBoolean(
        bool refund,
        string expected)
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");

        await ParityTestContext.Subscriptions(handler).CancelCustomerSubscriptionAsync(
            "sub-1",
            new CancelCustomerSubscriptionOptions { Refund = refund });

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(
            $"/api/subscriptions/v1/subscriptions/sub-1/cancel?refund={expected}",
            recorded.RequestUri.PathAndQuery);
        Assert.Null(recorded.Body);
    }

    [Fact]
    public async Task CancelCustomerSubscription_WithBothOptions_ShouldOrderExternalIdBeforeRefund()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");

        await ParityTestContext.Subscriptions(handler).CancelCustomerSubscriptionAsync(
            "sub-1",
            new CancelCustomerSubscriptionOptions { ExternalId = "customer-9", Refund = true });

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(
            "/api/subscriptions/v1/subscriptions/sub-1/cancel?external_id=customer-9&refund=true",
            recorded.RequestUri.PathAndQuery);
        Assert.Equal(new[] { "external_id", "refund" }, ParityTestContext.QueryKeys(recorded.RequestUri));
    }

    [Fact]
    public async Task CancelCustomerSubscription_WithEmptyOptions_ShouldSendNoQueryAtAll()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");

        await ParityTestContext.Subscriptions(handler)
            .CancelCustomerSubscriptionAsync("sub-1", new CancelCustomerSubscriptionOptions());

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal("/api/subscriptions/v1/subscriptions/sub-1/cancel", recorded.RequestUri.PathAndQuery);
        Assert.Equal(string.Empty, recorded.RequestUri.Query);
    }

    [Fact]
    public async Task CancelCustomerSubscription_ShouldIncludeAnEmptyExternalIdInsteadOfOmittingIt()
    {
        // Empty is not null: the provider owns non-empty validation, so an explicitly empty value is
        // sent and rejected server-side rather than silently dropped by the SDK.
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");

        await ParityTestContext.Subscriptions(handler).CancelCustomerSubscriptionAsync(
            "sub-1",
            new CancelCustomerSubscriptionOptions { ExternalId = string.Empty });

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(
            "/api/subscriptions/v1/subscriptions/sub-1/cancel?external_id=",
            recorded.RequestUri.PathAndQuery);
        Assert.Equal(new[] { "external_id" }, ParityTestContext.QueryKeys(recorded.RequestUri));
    }

    [Fact]
    public async Task CancelCustomerSubscription_ShouldEscapeHostileExternalIdAsOneQueryValue()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");

        await ParityTestContext.Subscriptions(handler).CancelCustomerSubscriptionAsync(
            "sub-1",
            new CancelCustomerSubscriptionOptions { ExternalId = HostileRawId, Refund = false });

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(
            $"/api/subscriptions/v1/subscriptions/sub-1/cancel?external_id={HostileEncodedId}&refund=false",
            recorded.RequestUri.PathAndQuery);
        Assert.Equal("/api/subscriptions/v1/subscriptions/sub-1/cancel", recorded.RequestUri.AbsolutePath);
        Assert.Equal(string.Empty, recorded.RequestUri.Fragment);
        Assert.Equal(new[] { "external_id", "refund" }, ParityTestContext.QueryKeys(recorded.RequestUri));
    }

    [Fact]
    public async Task CancelCustomerSubscription_ShouldKeepHostileSubscriptionIdInOneSegment()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");

        await ParityTestContext.Subscriptions(handler).CancelCustomerSubscriptionAsync(HostileRawId);

        ParityRequest recorded = Assert.Single(handler.Requests);
        Uri uri = recorded.RequestUri;
        string expected = $"/api/subscriptions/v1/subscriptions/{HostileEncodedId}/cancel";
        Assert.Equal(expected, uri.PathAndQuery);
        Assert.Equal(expected, uri.AbsolutePath);
        Assert.Equal(string.Empty, uri.Query);
        Assert.Equal(string.Empty, uri.Fragment);
        Assert.Equal(expected.Split('/').Length, uri.Segments.Length);
    }

    [Fact]
    public async Task CancelCustomerSubscription_ShouldTreatPercentLookingPathAndQueryValuesAsRawValues()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");

        await ParityTestContext.Subscriptions(handler).CancelCustomerSubscriptionAsync(
            LooksEncodedRawId,
            new CancelCustomerSubscriptionOptions { ExternalId = LooksEncodedRawId });

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(
            $"/api/subscriptions/v1/subscriptions/{LooksEncodedExpected}/cancel"
            + $"?external_id={LooksEncodedExpected}",
            recorded.RequestUri.PathAndQuery);
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    public async Task CancelCustomerSubscription_ShouldRejectExactDotSegmentBeforeAnyRequest(string dotValue)
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");

        ArgumentException withoutOptions = await Assert.ThrowsAsync<ArgumentException>(
            () => ParityTestContext.Subscriptions(handler).CancelCustomerSubscriptionAsync(dotValue));
        ArgumentException withOptions = await Assert.ThrowsAsync<ArgumentException>(
            () => ParityTestContext.Subscriptions(handler).CancelCustomerSubscriptionAsync(
                dotValue,
                new CancelCustomerSubscriptionOptions { Refund = true }));

        Assert.Equal("subscriptionId", withoutOptions.ParamName);
        Assert.Equal("subscriptionId", withOptions.ParamName);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task CancelCustomerSubscription_ShouldRejectNullSubscriptionIdBeforeAnyRequest()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");

        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => ParityTestContext.Subscriptions(handler).CancelCustomerSubscriptionAsync(null!));

        Assert.Equal("subscriptionId", exception.ParamName);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task CancelCustomerSubscription_ShouldRejectNullOptionsBeforeAnyRequest()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");

        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => ParityTestContext.Subscriptions(handler).CancelCustomerSubscriptionAsync("sub-1", null!));

        Assert.Equal("options", exception.ParamName);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task CancelCustomerSubscription_ShouldNeverSendABodyUnderAnyOptionPermutation()
    {
        foreach (CancelCustomerSubscriptionOptions options in CancelOptionPermutations())
        {
            ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");

            await ParityTestContext.Subscriptions(handler).CancelCustomerSubscriptionAsync("sub-1", options);

            ParityRequest recorded = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Delete, recorded.Method);
            Assert.Null(recorded.Body);
            Assert.Null(recorded.ContentType);
        }
    }

    [Fact]
    public async Task CancelCustomerSubscription_ShouldPropagateCancellationToTheHandler()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");
        using CancellationTokenSource source = new();
        handler.OnRequest = (_, _) => source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ParityTestContext.Subscriptions(handler)
                .CancelCustomerSubscriptionAsync("sub-1", source.Token));

        Assert.True(Assert.Single(handler.Requests).CancellationObservedAfterCancel);
    }

    [Fact]
    public async Task CancelCustomerSubscription_OptionsOverload_ShouldPropagateCancellationToTheHandler()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");
        using CancellationTokenSource source = new();
        handler.OnRequest = (_, _) => source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ParityTestContext.Subscriptions(handler).CancelCustomerSubscriptionAsync(
                "sub-1",
                new CancelCustomerSubscriptionOptions { Refund = true },
                source.Token));

        Assert.True(Assert.Single(handler.Requests).CancellationObservedAfterCancel);
    }

    /// <summary>
    /// The bodiless canonical cancel shares the DELETE transport with the wallet delete, so the
    /// already-cancelled contract is pinned on both forms: neither may reach a handler.
    /// </summary>
    [Fact]
    public async Task CancelCustomerSubscription_ShouldNotSendAnythingWhenTheTokenIsAlreadyCancelled()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");
        using CancellationTokenSource source = new();
        await source.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ParityTestContext.Subscriptions(handler)
                .CancelCustomerSubscriptionAsync("sub-1", source.Token));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task CancelCustomerSubscription_OptionsOverload_ShouldNotSendAnythingWhenTheTokenIsAlreadyCancelled()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");
        using CancellationTokenSource source = new();
        await source.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ParityTestContext.Subscriptions(handler).CancelCustomerSubscriptionAsync(
                "sub-1",
                new CancelCustomerSubscriptionOptions { ExternalId = "customer-9", Refund = true },
                source.Token));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task CancelCustomerSubscription_ShouldNotFallBackToThePostRouteOnNotFound()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Error(HttpStatusCode.NotFound);

        await Assert.ThrowsAsync<RozetkaPayNotFoundException>(
            () => ParityTestContext.Subscriptions(handler).CancelCustomerSubscriptionAsync(
                "sub-1",
                new CancelCustomerSubscriptionOptions { Refund = true }));

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, recorded.Method);
        Assert.NotEqual(HttpMethod.Post, recorded.Method);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task CancelCustomerSubscription_ShouldMakeExactlyOneRequestOnAnyFailure(HttpStatusCode status)
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Error(status);

        await Assert.ThrowsAnyAsync<RozetkaPayException>(
            () => ParityTestContext.Subscriptions(handler).CancelCustomerSubscriptionAsync("sub-1"));

        Assert.Single(handler.Requests);
    }

    // ===================== D. Legacy regression =====================

    [Fact]
    public async Task Legacy_DeletePaymentFromWallet_ShouldKeepTheLegacyRouteVerbAndResponse()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json(
            """{"status":"ok","message":"deleted"}""");

#pragma warning disable CS0618 // Deliberate legacy regression call.
        DeleteCardFromWalletResponse response = await ParityTestContext.Customers(handler)
            .DeletePaymentFromWalletAsync("customer-9", "card-4");
#pragma warning restore CS0618

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, recorded.Method);
        Assert.Equal("/api/customers/v1/customer-9/cards/card-4", recorded.RequestUri.PathAndQuery);
        Assert.Null(recorded.Body);
        Assert.Equal("ok", response.Status);
        Assert.Equal("deleted", response.Message);
    }

    [Fact]
    public async Task Legacy_GetCustomerSubscriptions_ShouldKeepTheLegacyRouteAndWrapperResponse()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json(
            """{"subscriptions":[{"id":"sub-9"}],"total":1,"count":1}""");

#pragma warning disable CS0618 // Deliberate legacy regression call.
        CustomerSubscriptionsResponse response = await ParityTestContext.Subscriptions(handler)
            .GetCustomerSubscriptionsAsync("customer-9");
#pragma warning restore CS0618

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, recorded.Method);
        Assert.Equal(
            "/api/subscriptions/v1/subscriptions/customer/customer-9",
            recorded.RequestUri.PathAndQuery);
        Assert.Equal("sub-9", Assert.Single(response.Subscriptions!).Id);
        Assert.Equal(1, response.Total);
        Assert.Equal(1, response.Count);
    }

    [Fact]
    public async Task Legacy_Cancel_ShouldKeepThePostVerbAndTheFullLegacyBody()
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");

#pragma warning disable CS0618 // Deliberate legacy regression call.
        await ParityTestContext.Subscriptions(handler).CancelAsync(
            "sub-1",
            new CancelSubscriptionRequest
            {
                ExternalId = "customer-9",
                Reason = "user requested",
                Immediate = true
            });
#pragma warning restore CS0618

        ParityRequest recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, recorded.Method);
        Assert.Equal("/api/subscriptions/v1/subscriptions/sub-1/cancel", recorded.RequestUri.PathAndQuery);

        // Neither legacy field may be discarded: they cannot be mapped onto the official refund option.
        Assert.Equal(
            """{"external_id":"customer-9","reason":"user requested","immediate":true}""",
            recorded.Body);
    }

    // ===================== E. Public surface and obsolete policy =====================

    public static TheoryData<string, string, Type[]> LegacyMembers => new()
    {
        {
            nameof(ICustomerService.DeletePaymentFromWalletAsync),
            "Use DeleteCustomerPaymentAsync(...). This member calls the legacy "
                + "/api/customers/v1/{customerId}/cards/{cardId} route.",
            [typeof(string), typeof(string), typeof(CancellationToken)]
        },
        {
            nameof(ISubscriptionService.GetCustomerSubscriptionsAsync),
            "Use GetSubscriptionsAsync(...). This member calls the legacy "
                + "/api/subscriptions/v1/subscriptions/customer/{customerId} route and returns the "
                + "legacy wrapper model.",
            [typeof(string), typeof(CancellationToken)]
        },
        {
            nameof(ISubscriptionService.CancelAsync),
            "Use CancelCustomerSubscriptionAsync(...). The legacy Reason and Immediate fields "
                + "cannot be mapped safely to the official refund query option.",
            [typeof(string), typeof(CancelSubscriptionRequest), typeof(CancellationToken)]
        }
    };

    [Theory]
    [MemberData(nameof(LegacyMembers))]
    public void LegacyMember_ShouldCarryTheSameActionableObsoleteMessageOnContractAndImplementation(
        string methodName,
        string expectedMessage,
        Type[] parameterTypes)
    {
        foreach (Type declaringType in ResolveDeclaringTypes(methodName))
        {
            MethodInfo? method = declaringType.GetMethod(methodName, parameterTypes);

            Assert.NotNull(method);

            ObsoleteAttribute? obsolete = method.GetCustomAttribute<ObsoleteAttribute>();

            Assert.NotNull(obsolete);
            Assert.Equal(expectedMessage, obsolete.Message);
            Assert.False(obsolete.IsError, $"{declaringType.Name}.{methodName} must stay a warning, not an error.");
        }
    }

    /// <summary>
    /// Every signature that existed before EXP-355 must still exist with the same return type and
    /// parameter list, so a compiled consumer keeps binding.
    /// </summary>
    [Theory]
    [MemberData(nameof(PreservedSignatures))]
    public void PreExistingSignature_ShouldStillExist(Type declaringType, string methodName, Type returnType, Type[] parameterTypes)
    {
        MethodInfo? method = declaringType.GetMethod(methodName, parameterTypes);

        Assert.NotNull(method);
        Assert.Equal(returnType, method.ReturnType);
    }

    public static TheoryData<Type, string, Type, Type[]> PreservedSignatures => new()
    {
        {
            typeof(ICustomerService),
            nameof(ICustomerService.DeletePaymentFromWalletAsync),
            typeof(Task<DeleteCardFromWalletResponse>),
            [typeof(string), typeof(string), typeof(CancellationToken)]
        },
        {
            typeof(CustomerService),
            nameof(CustomerService.DeletePaymentFromWalletAsync),
            typeof(Task<DeleteCardFromWalletResponse>),
            [typeof(string), typeof(string), typeof(CancellationToken)]
        },
        {
            typeof(ISubscriptionService),
            nameof(ISubscriptionService.GetCustomerSubscriptionsAsync),
            typeof(Task<CustomerSubscriptionsResponse>),
            [typeof(string), typeof(CancellationToken)]
        },
        {
            typeof(SubscriptionService),
            nameof(SubscriptionService.GetCustomerSubscriptionsAsync),
            typeof(Task<CustomerSubscriptionsResponse>),
            [typeof(string), typeof(CancellationToken)]
        },
        {
            typeof(ISubscriptionService),
            nameof(ISubscriptionService.CancelAsync),
            typeof(Task),
            [typeof(string), typeof(CancelSubscriptionRequest), typeof(CancellationToken)]
        },
        {
            typeof(SubscriptionService),
            nameof(SubscriptionService.CancelAsync),
            typeof(Task),
            [typeof(string), typeof(CancelSubscriptionRequest), typeof(CancellationToken)]
        }
    };

    [Theory]
    [MemberData(nameof(CanonicalSignatures))]
    public void CanonicalSignature_ShouldExistOnContractAndImplementationWithoutObsolete(
        Type declaringType,
        string methodName,
        Type returnType,
        Type[] parameterTypes)
    {
        MethodInfo? method = declaringType.GetMethod(methodName, parameterTypes);

        Assert.NotNull(method);
        Assert.Equal(returnType, method.ReturnType);
        Assert.Null(method.GetCustomAttribute<ObsoleteAttribute>());
    }

    public static TheoryData<Type, string, Type, Type[]> CanonicalSignatures
    {
        get
        {
            TheoryData<Type, string, Type, Type[]> data = new();

            foreach (Type type in new[] { typeof(ICustomerService), typeof(CustomerService) })
            {
                data.Add(type, "DeleteCustomerPaymentAsync", typeof(Task<DeleteCustomerPaymentResult>),
                    [typeof(DeleteCustomerPaymentRequest), typeof(CancellationToken)]);
                data.Add(type, "DeleteCustomerPaymentAsync", typeof(Task<DeleteCustomerPaymentResult>),
                    [typeof(string), typeof(DeleteCustomerPaymentRequest), typeof(CancellationToken)]);
            }

            foreach (Type type in new[] { typeof(ISubscriptionService), typeof(SubscriptionService) })
            {
                data.Add(type, "GetSubscriptionsAsync", typeof(Task<SubscriptionList>),
                    [typeof(CancellationToken)]);
                data.Add(type, "GetSubscriptionsAsync", typeof(Task<SubscriptionList>),
                    [typeof(string), typeof(CancellationToken)]);
                data.Add(type, "CancelCustomerSubscriptionAsync", typeof(Task<DefaultResponse>),
                    [typeof(string), typeof(CancellationToken)]);
                data.Add(type, "CancelCustomerSubscriptionAsync", typeof(Task<DefaultResponse>),
                    [typeof(string), typeof(CancelCustomerSubscriptionOptions), typeof(CancellationToken)]);
            }

            return data;
        }
    }

    [Fact]
    public void CancelCustomerSubscriptionOptions_ShouldExposeExactlyTheTwoNullableQueryOptions()
    {
        PropertyInfo[] properties = typeof(CancelCustomerSubscriptionOptions)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        Assert.True(typeof(CancelCustomerSubscriptionOptions).IsPublic);
        Assert.Equal(2, properties.Length);

        PropertyInfo externalId = Assert.Single(properties, property => property.Name == "ExternalId");
        PropertyInfo refund = Assert.Single(properties, property => property.Name == "Refund");

        Assert.Equal(typeof(string), externalId.PropertyType);
        Assert.Equal(typeof(bool?), refund.PropertyType);
        Assert.True(externalId.CanWrite && refund.CanWrite);

        // The object is a query-option carrier, never a JSON body: it must not declare wire names.
        foreach (PropertyInfo property in properties)
        {
            Assert.Empty(property.GetCustomAttributes(
                typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute),
                inherit: true));
        }
    }

    [Fact]
    public void SubscriptionList_ShouldKeepItsPublicShape()
    {
        PropertyInfo? subscriptions = typeof(SubscriptionList).GetProperty("Subscriptions");

        Assert.NotNull(subscriptions);
        Assert.Equal(typeof(List<Subscription>), subscriptions.PropertyType);
        Assert.Equal(typeof(object), typeof(SubscriptionList).BaseType);
        Assert.True(subscriptions.CanRead && subscriptions.CanWrite);
    }

    [Fact]
    public void SubscriptionListConverter_ShouldNotBePartOfThePublicSurface()
    {
        Assert.DoesNotContain(
            typeof(SubscriptionList).Assembly.GetExportedTypes(),
            type => type.Name.Contains("SubscriptionListJsonConverter", StringComparison.Ordinal));
    }

    // ===================== F. Error mapping and log hygiene =====================

    [Theory]
    [MemberData(nameof(CanonicalOperations))]
    public async Task CanonicalOperation_ShouldMapStructuredApiErrorsWithoutLeakingCallerData(string operation)
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Error(
            HttpStatusCode.BadRequest,
            """{"code":"invalid_request","message":"card belongs to another customer","error_id":"err-77"}""",
            requestId: "req-4242");
        TestInfrastructure.TestLogger<OperationParityTests> logger = new();

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(
            () => InvokeCanonicalAsync(operation, handler, logger));

        Assert.NotNull(exception.ApiError);
        Assert.Equal(HttpStatusCode.BadRequest, exception.ApiError!.StatusCode);
        Assert.Equal("invalid_request", exception.ApiError.Code);
        Assert.Equal("req-4242", exception.ApiError.RequestId);

        string log = string.Join("\n", logger.Messages);

        // Safe diagnostics are present.
        Assert.Contains("BadRequest", log, StringComparison.Ordinal);
        Assert.Contains("invalid_request", log, StringComparison.Ordinal);
        Assert.Contains("req-4242", log, StringComparison.Ordinal);

        // Caller-controlled and provider-supplied content is not.
        Assert.DoesNotContain(ParityTestContext.SecretExternalId, log, StringComparison.Ordinal);
        Assert.DoesNotContain(ParityTestContext.SecretOptionId, log, StringComparison.Ordinal);
        Assert.DoesNotContain(ParityTestContext.SecretSubscriptionId, log, StringComparison.Ordinal);
        Assert.DoesNotContain(ParityTestContext.CustomerAuthPlaceholder, log, StringComparison.Ordinal);
        Assert.DoesNotContain("card belongs to another customer", log, StringComparison.Ordinal);
        Assert.DoesNotContain("err-77", log, StringComparison.Ordinal);
        Assert.DoesNotContain("option_id", log, StringComparison.Ordinal);
        Assert.DoesNotContain("external_id", log, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(CanonicalOperations))]
    public async Task CanonicalOperation_ShouldLogOnlyTheStaticRouteTemplateOnSuccess(string operation)
    {
        ParityRecordingHandler handler = ParityRecordingHandler.Json("{}");
        TestInfrastructure.TestLogger<OperationParityTests> logger = new();

        await InvokeCanonicalAsync(operation, handler, logger);

        string log = string.Join("\n", logger.Messages);

        Assert.Contains(ExpectedLogLabel(operation), log, StringComparison.Ordinal);
        Assert.DoesNotContain(ParityTestContext.SecretExternalId, log, StringComparison.Ordinal);
        Assert.DoesNotContain(ParityTestContext.SecretOptionId, log, StringComparison.Ordinal);
        Assert.DoesNotContain(ParityTestContext.SecretSubscriptionId, log, StringComparison.Ordinal);
        Assert.DoesNotContain(ParityTestContext.CustomerAuthPlaceholder, log, StringComparison.Ordinal);
        Assert.DoesNotContain("external_id=", log, StringComparison.Ordinal);
    }

    public static TheoryData<string> CanonicalOperations => ["wallet-delete", "subscription-list", "subscription-cancel"];

    // ===================== Helpers =====================

    private static string ExpectedLogLabel(string operation)
    {
        return operation switch
        {
            "wallet-delete" => "/api/customers/v1/wallet",
            "subscription-list" => "/api/subscriptions/v1/subscriptions",
            "subscription-cancel" => "/api/subscriptions/v1/subscriptions/{subscription_id}/cancel",
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unmapped operation.")
        };
    }

    private static Task InvokeCanonicalAsync(
        string operation,
        ParityRecordingHandler handler,
        TestInfrastructure.TestLogger<OperationParityTests> logger)
    {
        RozetkaPayConfiguration configuration = ParityTestContext.WithCustomerAuth();

        return operation switch
        {
            "wallet-delete" => ParityTestContext.Customers(handler, configuration, logger)
                .DeleteCustomerPaymentAsync(
                    ParityTestContext.SecretExternalId,
                    new DeleteCustomerPaymentRequest { OptionId = ParityTestContext.SecretOptionId }),
            "subscription-list" => ParityTestContext.Subscriptions(handler, configuration, logger)
                .GetSubscriptionsAsync(ParityTestContext.SecretExternalId),
            "subscription-cancel" => ParityTestContext.Subscriptions(handler, configuration, logger)
                .CancelCustomerSubscriptionAsync(
                    ParityTestContext.SecretSubscriptionId,
                    new CancelCustomerSubscriptionOptions
                    {
                        ExternalId = ParityTestContext.SecretExternalId,
                        Refund = true
                    }),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unmapped operation.")
        };
    }

    private static IEnumerable<CancelCustomerSubscriptionOptions> CancelOptionPermutations()
    {
        yield return new CancelCustomerSubscriptionOptions();
        yield return new CancelCustomerSubscriptionOptions { ExternalId = "customer-9" };
        yield return new CancelCustomerSubscriptionOptions { ExternalId = string.Empty };
        yield return new CancelCustomerSubscriptionOptions { Refund = true };
        yield return new CancelCustomerSubscriptionOptions { Refund = false };
        yield return new CancelCustomerSubscriptionOptions { ExternalId = "customer-9", Refund = true };
        yield return new CancelCustomerSubscriptionOptions { ExternalId = "customer-9", Refund = false };
    }

    private static IEnumerable<Type> ResolveDeclaringTypes(string methodName)
    {
        if (methodName == nameof(ICustomerService.DeletePaymentFromWalletAsync))
        {
            yield return typeof(ICustomerService);
            yield return typeof(CustomerService);
            yield break;
        }

        yield return typeof(ISubscriptionService);
        yield return typeof(SubscriptionService);
    }
}

/// <summary>
/// One request as the handler observed it. The body is captured eagerly because
/// <see cref="HttpClient"/> disposes request content before the caller regains control.
/// </summary>
internal sealed record ParityRequest(
    HttpMethod Method,
    Uri RequestUri,
    string? Body,
    string? ContentType,
    IReadOnlyDictionary<string, string[]> Headers,
    bool CancellationObservedAfterCancel)
{
    internal string[] HeaderValues(string name)
    {
        return Headers.TryGetValue(name, out string[]? values) ? values : [];
    }
}

/// <summary>
/// No-network handler recording every request, including request headers and the observed content
/// type. Responses are served from a fixed status and body so a canonical operation cannot silently
/// retry or fall back without the extra request becoming visible.
/// </summary>
internal sealed class ParityRecordingHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _status;
    private readonly string _body;
    private readonly string? _requestId;
    private readonly List<ParityRequest> _requests = [];

    private ParityRecordingHandler(HttpStatusCode status, string body, string? requestId)
    {
        _status = status;
        _body = body;
        _requestId = requestId;
    }

    /// <summary>
    /// Runs inside the handler before the response is produced. Used to cancel a token while the
    /// transport is in flight, which is only observable if the caller token really was propagated.
    /// </summary>
    internal Action<HttpRequestMessage, CancellationToken>? OnRequest { get; set; }

    internal IReadOnlyList<ParityRequest> Requests => _requests;

    internal static ParityRecordingHandler Json(string body)
    {
        return new ParityRecordingHandler(HttpStatusCode.OK, body, null);
    }

    internal static ParityRecordingHandler Error(
        HttpStatusCode status,
        string body = """{"code":"not_found","message":"Resource not found"}""",
        string? requestId = null)
    {
        return new ParityRecordingHandler(status, body, requestId);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string? body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        Dictionary<string, string[]> headers = request.Headers.ToDictionary(
            header => header.Key,
            header => header.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase);

        OnRequest?.Invoke(request, cancellationToken);

        _requests.Add(new ParityRequest(
            request.Method,
            request.RequestUri!,
            body,
            request.Content?.Headers.ContentType?.ToString(),
            headers,
            cancellationToken.IsCancellationRequested));

        cancellationToken.ThrowIfCancellationRequested();

        HttpResponseMessage response = new(_status)
        {
            Content = new StringContent(_body, Encoding.UTF8, "application/json")
        };

        if (_requestId is not null)
        {
            response.Headers.Add("X-Request-Id", _requestId);
        }

        return response;
    }
}

internal static class ParityTestContext
{
    /// <summary>
    /// Fake host. Every request is intercepted by the recording handler, so no DNS lookup or network
    /// traffic can occur even if a test regresses.
    /// </summary>
    private const string BaseUrl = "https://unit.test";

    /// <summary>
    /// Distinctive, unmistakably synthetic markers. They are long and unique so that a log-leak
    /// assertion cannot pass by accident, and they are obviously not credentials so that secret
    /// scanners have nothing to flag.
    /// </summary>
    internal const string CustomerAuthPlaceholder = "customer-auth-placeholder-not-a-real-token-EXP355";

    internal const string SecretExternalId = "external-id-placeholder-must-never-be-logged-EXP355";

    internal const string SecretOptionId = "option-id-placeholder-must-never-be-logged-EXP355";

    internal const string SecretSubscriptionId = "subscription-id-placeholder-must-never-be-logged-EXP355";

    internal static RozetkaPayConfiguration CreateConfiguration()
    {
        return new RozetkaPayConfiguration
        {
            BaseUrl = BaseUrl,
            Login = "unit-test-login",
            Password = "unit-test-placeholder",
            RetryPolicy = RetryPolicy.None,
            UserAgent = "SYT.RozetkaPay.Tests"
        };
    }

    internal static RozetkaPayConfiguration WithCustomerAuth()
    {
        RozetkaPayConfiguration configuration = CreateConfiguration();
        configuration.CustomerAuth = CustomerAuthPlaceholder;
        return configuration;
    }

    internal static JsonSerializerOptions SerializerOptions()
    {
        return SerializerOptionsProbe.Instance.Options;
    }

    internal static CustomerService Customers(
        ParityRecordingHandler handler,
        RozetkaPayConfiguration? configuration = null,
        Microsoft.Extensions.Logging.ILogger? logger = null)
    {
        return new CustomerService(configuration ?? CreateConfiguration(), CreateHttpClient(handler), logger);
    }

    internal static SubscriptionService Subscriptions(
        ParityRecordingHandler handler,
        RozetkaPayConfiguration? configuration = null,
        Microsoft.Extensions.Logging.ILogger? logger = null)
    {
        return new SubscriptionService(configuration ?? CreateConfiguration(), CreateHttpClient(handler), logger);
    }

    internal static string[] QueryKeys(Uri uri)
    {
        string query = uri.Query;
        if (query.Length <= 1)
        {
            return [];
        }

        return query[1..]
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2)[0])
            .ToArray();
    }

    private static HttpClient CreateHttpClient(ParityRecordingHandler handler)
    {
        return new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
    }

    /// <summary>
    /// Exposes the exact serializer options the SDK uses on the wire, so a converter test cannot pass
    /// against a hand-built option set that production never applies.
    /// </summary>
    private sealed class SerializerOptionsProbe : BaseService
    {
        internal static readonly SerializerOptionsProbe Instance = new();

        private SerializerOptionsProbe()
            : base(CreateConfiguration(), new HttpClient(ParityRecordingHandler.Json("{}")))
        {
        }

        internal JsonSerializerOptions Options => GetJsonSerializerOptions();
    }
}
