using System.Net;
using System.Text.Json;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Models.Subscriptions;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Wire-level coverage of the official <c>UpdateSubscriptionPaymentMethod</c> operation added by
/// EXP-354: <c>PATCH /api/subscriptions/v1/subscriptions/{subscription_id}/payment-method</c>.
///
/// Expected request targets, JSON and enum tokens are written as literal strings on purpose. Deriving
/// them from <see cref="Uri.EscapeDataString"/> or from the SDK serializer would mirror the
/// implementation and would not detect the wrong verb, a value escaped twice, or an enum token renamed
/// by a naming policy.
/// </summary>
public class SubscriptionPaymentMethodUpdateTests
{
    private const string ExpectedLogLabel =
        "/api/subscriptions/v1/subscriptions/{subscription_id}/payment-method";

    private const string SuccessBody = """{"message":"Payment method updated"}""";

    /// <summary>
    /// Every documented <c>type</c> token, paired with the C# enum value that must produce it. Under the
    /// SDK snake-case policy alone several of these would still be right by luck, so the mapping is
    /// pinned here rather than assumed.
    /// </summary>
    public static TheoryData<SubscriptionPaymentMethodUpdateType, string> PaymentMethodTypeTokens =>
        new()
        {
            { SubscriptionPaymentMethodUpdateType.CcToken, "cc_token" },
            { SubscriptionPaymentMethodUpdateType.Wallet, "wallet" },
            { SubscriptionPaymentMethodUpdateType.GooglePay, "google_pay" },
            { SubscriptionPaymentMethodUpdateType.ApplePay, "apple_pay" },
            { SubscriptionPaymentMethodUpdateType.RecurrentId, "recurrent_id" }
        };

    public static TheoryData<HttpStatusCode, Type> ErrorMappings =>
        new()
        {
            { HttpStatusCode.BadRequest, typeof(RozetkaPayValidationException) },
            { HttpStatusCode.Unauthorized, typeof(RozetkaPayAuthorizationException) },
            { HttpStatusCode.Forbidden, typeof(RozetkaPayAuthorizationException) },
            { HttpStatusCode.NotFound, typeof(RozetkaPayNotFoundException) },
            { HttpStatusCode.InternalServerError, typeof(RozetkaPayException) }
        };

    [Fact]
    public async Task UpdatePaymentMethod_ShouldSendPatchToTheOfficialTarget()
    {
        RecordingHandler handler = RecordingHandler.Json(SuccessBody);

        await Exp354TestContext.Subscriptions(handler)
            .UpdatePaymentMethodAsync("subscription-1", MinimalRequest());

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, recorded.Method);
        Assert.Equal(
            "/api/subscriptions/v1/subscriptions/subscription-1/payment-method",
            recorded.RequestUri.PathAndQuery);
        Assert.Equal(string.Empty, recorded.RequestUri.Query);
        Assert.Equal(string.Empty, recorded.RequestUri.Fragment);
        Assert.Equal(Exp354TestContext.JsonContentType, recorded.ContentType);
    }

    [Theory]
    [InlineData(Exp354TestContext.HostileRawId, Exp354TestContext.HostileEncodedId)]
    [InlineData(Exp354TestContext.LooksEncodedRawId, Exp354TestContext.LooksEncodedExpectedId)]
    [InlineData("plain-id", "plain-id")]
    public async Task UpdatePaymentMethod_ShouldEscapeTheSubscriptionIdExactlyOnce(
        string rawSubscriptionId,
        string expectedSegment)
    {
        RecordingHandler handler = RecordingHandler.Json(SuccessBody);

        await Exp354TestContext.Subscriptions(handler)
            .UpdatePaymentMethodAsync(rawSubscriptionId, MinimalRequest());

        Exp354Request recorded = Assert.Single(handler.Requests);

        // The handler-observed target, not the string the service built: a value escaped at the wrong
        // insertion point or escaped twice only shows up here.
        Assert.Equal(
            $"/api/subscriptions/v1/subscriptions/{expectedSegment}/payment-method",
            recorded.RequestUri.PathAndQuery);

        // The identifier stays inside exactly one path segment: "/", "api/", "subscriptions/", "v1/",
        // "subscriptions/", the identifier, then "payment-method".
        Assert.Equal(7, recorded.RequestUri.Segments.Length);
        Assert.Equal($"{expectedSegment}/", recorded.RequestUri.Segments[5]);
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    public async Task UpdatePaymentMethod_ShouldRejectDotSegments(string subscriptionId)
    {
        RecordingHandler handler = RecordingHandler.Json(SuccessBody);
        SubscriptionService service = Exp354TestContext.Subscriptions(handler);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdatePaymentMethodAsync(subscriptionId, MinimalRequest()));

        Assert.Equal("subscriptionId", exception.ParamName);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task UpdatePaymentMethod_ShouldRejectNullArguments()
    {
        RecordingHandler handler = RecordingHandler.Json(SuccessBody);
        SubscriptionService service = Exp354TestContext.Subscriptions(handler);

        ArgumentNullException nullRequest = await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.UpdatePaymentMethodAsync("subscription-1", null!));
        Assert.Equal("request", nullRequest.ParamName);

        ArgumentNullException nullId = await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.UpdatePaymentMethodAsync(null!, MinimalRequest()));
        Assert.Equal("subscriptionId", nullId.ParamName);

        Assert.Empty(handler.Requests);
    }

    /// <summary>
    /// The subscription identifier must never reach a log sink — neither through the rendered message nor
    /// through the structured state a sink actually writes.
    /// </summary>
    [Fact]
    public async Task UpdatePaymentMethod_ShouldLogTheStaticRouteOnly()
    {
        RecordingHandler handler = RecordingHandler.Json(SuccessBody);
        RecordingLogger logger = new();

        await Exp354TestContext.Subscriptions(handler, logger: logger)
            .UpdatePaymentMethodAsync(Exp354TestContext.SecretSubscriptionId, MinimalRequest());

        // The structured state carries the static route template, not the caller's identifier.
        Assert.Contains(logger.StateValues, value => value.Contains(ExpectedLogLabel, StringComparison.Ordinal));
        Assert.All(
            logger.AllText,
            text => Assert.DoesNotContain(Exp354TestContext.SecretSubscriptionId, text, StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(PaymentMethodTypeTokens))]
    public async Task UpdatePaymentMethod_ShouldSerializeEveryTypeTokenExactly(
        SubscriptionPaymentMethodUpdateType type,
        string expectedToken)
    {
        RecordingHandler handler = RecordingHandler.Json(SuccessBody);

        await Exp354TestContext.Subscriptions(handler).UpdatePaymentMethodAsync(
            "subscription-1",
            new UpdateSubscriptionPaymentMethodRequest
            {
                PaymentMethod = new SubscriptionPaymentMethodUpdate { Type = type }
            });

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal("{\"payment_method\":{\"type\":\"" + expectedToken + "\"}}", recorded.Body);
    }

    [Fact]
    public async Task UpdatePaymentMethod_ShouldSerializeTheCcTokenMethod()
    {
        RecordingHandler handler = RecordingHandler.Json(SuccessBody);

        await Exp354TestContext.Subscriptions(handler).UpdatePaymentMethodAsync(
            "subscription-1",
            new UpdateSubscriptionPaymentMethodRequest
            {
                PaymentMethod = new SubscriptionPaymentMethodUpdate
                {
                    Type = SubscriptionPaymentMethodUpdateType.CcToken,
                    CcToken = new CustomerCCTokenRequestPaymentMethod
                    {
                        Token = "card-token-1",
                        Use3DSFlow = true,
                        SaveToWallet = true
                    }
                }
            });

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal(
            """{"payment_method":{"type":"cc_token","cc_token":{"token":"card-token-1","use_3ds_flow":true,"save_to_wallet":true}}}""",
            recorded.Body);
    }

    [Fact]
    public async Task UpdatePaymentMethod_ShouldSerializeTheWalletMethod()
    {
        RecordingHandler handler = RecordingHandler.Json(SuccessBody);

        await Exp354TestContext.Subscriptions(handler).UpdatePaymentMethodAsync(
            "subscription-1",
            new UpdateSubscriptionPaymentMethodRequest
            {
                PaymentMethod = new SubscriptionPaymentMethodUpdate
                {
                    Type = SubscriptionPaymentMethodUpdateType.Wallet,
                    Wallet = new CustomerWalletRequestPaymentMethod { OptionId = "option-1" }
                }
            });

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal(
            """{"payment_method":{"type":"wallet","wallet":{"option_id":"option-1"}}}""",
            recorded.Body);
    }

    [Fact]
    public async Task UpdatePaymentMethod_ShouldSerializeApplePayAndGooglePayIntoTheirOwnProperties()
    {
        RecordingHandler applePayHandler = RecordingHandler.Json(SuccessBody);
        await Exp354TestContext.Subscriptions(applePayHandler).UpdatePaymentMethodAsync(
            "subscription-1",
            new UpdateSubscriptionPaymentMethodRequest
            {
                PaymentMethod = new SubscriptionPaymentMethodUpdate
                {
                    Type = SubscriptionPaymentMethodUpdateType.ApplePay,
                    ApplePay = new CustomerAppleGooglePayRequestPaymentMethod { Token = "apple-token-1" }
                }
            });

        Assert.Equal(
            """{"payment_method":{"type":"apple_pay","apple_pay":{"token":"apple-token-1"}}}""",
            Assert.Single(applePayHandler.Requests).Body);

        RecordingHandler googlePayHandler = RecordingHandler.Json(SuccessBody);
        await Exp354TestContext.Subscriptions(googlePayHandler).UpdatePaymentMethodAsync(
            "subscription-1",
            new UpdateSubscriptionPaymentMethodRequest
            {
                PaymentMethod = new SubscriptionPaymentMethodUpdate
                {
                    Type = SubscriptionPaymentMethodUpdateType.GooglePay,
                    GooglePay = new CustomerAppleGooglePayRequestPaymentMethod
                    {
                        Token = "google-token-1",
                        Use3DSFlow = false
                    }
                }
            });

        Assert.Equal(
            """{"payment_method":{"type":"google_pay","google_pay":{"token":"google-token-1","use_3ds_flow":false}}}""",
            Assert.Single(googlePayHandler.Requests).Body);
    }

    /// <summary>
    /// The recurrent identifier is declared as a string. It must stay text: turning it into a number
    /// would lose precision on the 18-digit identifiers the provider issues.
    /// </summary>
    [Fact]
    public async Task UpdatePaymentMethod_ShouldSerializeTheRecurrentIdAsText()
    {
        RecordingHandler handler = RecordingHandler.Json(SuccessBody);

        await Exp354TestContext.Subscriptions(handler).UpdatePaymentMethodAsync(
            "subscription-1",
            new UpdateSubscriptionPaymentMethodRequest
            {
                PaymentMethod = new SubscriptionPaymentMethodUpdate
                {
                    Type = SubscriptionPaymentMethodUpdateType.RecurrentId,
                    RecurrentId = new SubscriptionRecurrentIdPaymentMethod
                    {
                        RecurrentId = "593292035525113984"
                    }
                }
            });

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal(
            """{"payment_method":{"type":"recurrent_id","recurrent_id":{"recurrent_id":"593292035525113984"}}}""",
            recorded.Body);
    }

    [Fact]
    public async Task UpdatePaymentMethod_ShouldSerializeEveryOptionalField()
    {
        RecordingHandler handler = RecordingHandler.Json(SuccessBody);

        await Exp354TestContext.Subscriptions(handler).UpdatePaymentMethodAsync(
            "subscription-1",
            new UpdateSubscriptionPaymentMethodRequest
            {
                ExternalId = "customer-9",
                ResultUrl = "https://merchant.example/result",
                AutoRenew = true,
                PaymentMethod = new SubscriptionPaymentMethodUpdate
                {
                    Type = SubscriptionPaymentMethodUpdateType.Wallet,
                    Wallet = new CustomerWalletRequestPaymentMethod { OptionId = "option-1" }
                },
                Fingerprint = new BrowserFingerprint
                {
                    BrowserAcceptHeader = "text/html",
                    BrowserColorDepth = "24",
                    BrowserIpAddress = "203.0.113.10",
                    BrowserJavaEnabled = "false",
                    BrowserLanguage = "uk-UA",
                    BrowserScreenHeight = "1080",
                    BrowserScreenWidth = "1920",
                    BrowserTimeZone = "Europe/Kyiv",
                    BrowserTimeZoneOffset = "-180",
                    BrowserUserAgent = "unit-test-agent"
                }
            });

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.NotNull(recorded.Body);

        using JsonDocument body = JsonDocument.Parse(recorded.Body!);
        JsonElement root = body.RootElement;

        Assert.Equal("customer-9", root.GetProperty("external_id").GetString());
        Assert.Equal("https://merchant.example/result", root.GetProperty("result_url").GetString());
        Assert.True(root.GetProperty("auto_renew").GetBoolean());
        Assert.Equal("text/html", root.GetProperty("fingerprint").GetProperty("browser_accept_header").GetString());
        Assert.Equal("Europe/Kyiv", root.GetProperty("fingerprint").GetProperty("browser_time_zone").GetString());
    }

    /// <summary>
    /// <c>auto_renew</c> is an external wire value, so <see langword="false"/> must be sent and only
    /// <see langword="null"/> may be omitted. Collapsing the two would silently enable auto-renew.
    /// </summary>
    [Theory]
    [InlineData(true, """{"payment_method":{"type":"wallet"},"auto_renew":true}""")]
    [InlineData(false, """{"payment_method":{"type":"wallet"},"auto_renew":false}""")]
    [InlineData(null, """{"payment_method":{"type":"wallet"}}""")]
    public async Task UpdatePaymentMethod_ShouldDistinguishFalseFromOmittedAutoRenew(
        bool? autoRenew,
        string expectedBody)
    {
        RecordingHandler handler = RecordingHandler.Json(SuccessBody);

        await Exp354TestContext.Subscriptions(handler).UpdatePaymentMethodAsync(
            "subscription-1",
            new UpdateSubscriptionPaymentMethodRequest
            {
                AutoRenew = autoRenew,
                PaymentMethod = new SubscriptionPaymentMethodUpdate
                {
                    Type = SubscriptionPaymentMethodUpdateType.Wallet
                }
            });

        Assert.Equal(expectedBody, Assert.Single(handler.Requests).Body);
    }

    [Fact]
    public async Task UpdatePaymentMethod_ShouldReturnTheTypedResponse()
    {
        RecordingHandler handler = RecordingHandler.Json(
            """{"message":"Confirm the payment","user_action":{"type":"redirect","value":"https://acs.example/3ds"}}""");

        UpdateSubscriptionPaymentMethodResponse response = await Exp354TestContext.Subscriptions(handler)
            .UpdatePaymentMethodAsync("subscription-1", MinimalRequest());

        Assert.Equal("Confirm the payment", response.Message);
        Assert.NotNull(response.UserAction);
        Assert.Equal("redirect", response.UserAction!.Type);
        Assert.Equal("https://acs.example/3ds", response.UserAction.Value);
    }

    [Fact]
    public async Task UpdatePaymentMethod_ShouldReturnMessageOnlyWhenNoActionIsRequired()
    {
        RecordingHandler handler = RecordingHandler.Json(SuccessBody);

        UpdateSubscriptionPaymentMethodResponse response = await Exp354TestContext.Subscriptions(handler)
            .UpdatePaymentMethodAsync("subscription-1", MinimalRequest());

        Assert.Equal("Payment method updated", response.Message);
        Assert.Null(response.UserAction);
    }

    /// <summary>
    /// The configured <c>X-CUSTOMER-AUTH</c> header identifies the customer for this operation, so it must
    /// still be attached — and must still stay out of the logs.
    /// </summary>
    [Fact]
    public async Task UpdatePaymentMethod_ShouldCarryTheConfiguredCustomerAuthHeader()
    {
        RecordingHandler handler = RecordingHandler.Json(SuccessBody);
        RecordingLogger logger = new();

        await Exp354TestContext
            .Subscriptions(handler, Exp354TestContext.WithCustomerAuth(), logger)
            .UpdatePaymentMethodAsync(Exp354TestContext.SecretSubscriptionId, MinimalRequest());

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal(
            [Exp354TestContext.CustomerAuthPlaceholder],
            recorded.Headers["X-CUSTOMER-AUTH"]);
        Assert.True(recorded.Headers.ContainsKey("Authorization"));

        Assert.All(
            logger.AllText,
            text => Assert.DoesNotContain(
                Exp354TestContext.CustomerAuthPlaceholder,
                text,
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task UpdatePaymentMethod_ShouldPropagateCancellation()
    {
        RecordingHandler handler = RecordingHandler.Json(SuccessBody);
        using CancellationTokenSource cancellation = new();
        handler.OnRequest = (_, _) => cancellation.Cancel();

        SubscriptionService service = Exp354TestContext.Subscriptions(handler);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.UpdatePaymentMethodAsync("subscription-1", MinimalRequest(), cancellation.Token));

        // The token really reached the transport rather than being dropped on the way.
        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.True(recorded.CancellationRequestedOnArrival);
    }

    [Theory]
    [MemberData(nameof(ErrorMappings))]
    public async Task UpdatePaymentMethod_ShouldMapErrorsThroughTheExistingExceptions(
        HttpStatusCode status,
        Type expectedExceptionType)
    {
        RecordingHandler handler = RecordingHandler.Error(
            status,
            """{"code":"subscription_invalid","message":"Provider rejected the update","error_id":"req-42"}""");
        SubscriptionService service = Exp354TestContext.Subscriptions(handler);

        RozetkaPayException exception = (RozetkaPayException)await Assert.ThrowsAnyAsync<Exception>(
            () => service.UpdatePaymentMethodAsync("subscription-1", MinimalRequest()));

        Assert.IsType(expectedExceptionType, exception);
        Assert.NotNull(exception.ApiError);
        Assert.Equal(status, exception.ApiError!.StatusCode);
        Assert.Equal("subscription_invalid", exception.ApiError.Code);
        Assert.Equal("req-42", exception.ApiError.RequestId);
    }

    /// <summary>
    /// The historical <see cref="SubscriptionPaymentMethod"/> describes a different shape and is used by
    /// other operations. EXP-354 must not repurpose it.
    /// </summary>
    [Fact]
    public void UpdateRequest_ShouldNotReuseTheHistoricalPaymentMethodType()
    {
        Assert.NotEqual(
            typeof(SubscriptionPaymentMethod),
            typeof(UpdateSubscriptionPaymentMethodRequest).GetProperty(
                nameof(UpdateSubscriptionPaymentMethodRequest.PaymentMethod))!.PropertyType);

        Assert.Equal(
            typeof(SubscriptionPaymentMethodUpdate),
            typeof(UpdateSubscriptionPaymentMethodRequest).GetProperty(
                nameof(UpdateSubscriptionPaymentMethodRequest.PaymentMethod))!.PropertyType);

        // The historical type keeps its shape for the operations that already use it.
        Assert.NotNull(typeof(SubscriptionPaymentMethod).GetProperty(nameof(SubscriptionPaymentMethod.Card)));
        Assert.NotNull(
            typeof(SubscriptionPaymentMethod).GetProperty(nameof(SubscriptionPaymentMethod.RecurrentToken)));
    }

    private static UpdateSubscriptionPaymentMethodRequest MinimalRequest()
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
