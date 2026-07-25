using System.Net;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Models.Merchants;
using SYT.RozetkaPay.Models.Partners;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Wire-level coverage of the three official partner operations added by EXP-354.
///
/// All three are GETs whose only inputs are query parameters, so the tests are mostly about the request
/// target: no trailing '?' when nothing is sent, deterministic parameter order, one escaping pass per
/// value, and "null omits / empty sends" kept distinct.
/// </summary>
public class PartnerServiceTests
{
    private const string FeeDetailsEndpoint = "/api/partners/v1/fee-details";

    private const string MerchantStatusEndpoint = "/api/partners/v1/merchant-status";

    private const string TransactionDetailsEndpoint = "/api/partners/v1/transaction-details";

    public static TheoryData<HttpStatusCode, Type> ErrorMappings =>
        new()
        {
            { HttpStatusCode.BadRequest, typeof(RozetkaPayValidationException) },
            { HttpStatusCode.TooManyRequests, typeof(RozetkaPayRateLimitException) },
            { HttpStatusCode.InternalServerError, typeof(RozetkaPayException) }
        };

    // ===================== fee details =====================

    /// <summary>
    /// A request with nothing to send must not carry a bare '?': that is a different request target, and
    /// some gateways treat it as one.
    /// </summary>
    [Fact]
    public async Task GetFeeDetails_WithoutProject_ShouldSendNoQueryAtAll()
    {
        RecordingHandler handler = RecordingHandler.Json("{}");

        await Exp354TestContext.Partners(handler).GetFeeDetailsAsync();

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, recorded.Method);
        Assert.Equal(FeeDetailsEndpoint, recorded.RequestUri.PathAndQuery);
        Assert.Equal(string.Empty, recorded.RequestUri.Query);
        Assert.DoesNotContain("?", recorded.RequestUri.PathAndQuery);
        Assert.False(recorded.HasContent);
    }

    [Theory]
    [InlineData(Exp354TestContext.HostileRawId, Exp354TestContext.HostileEncodedId)]
    [InlineData(Exp354TestContext.LooksEncodedRawId, Exp354TestContext.LooksEncodedExpectedId)]
    [InlineData("project-1", "project-1")]
    [InlineData("", "")]
    public async Task GetFeeDetails_WithProject_ShouldSendOneEscapedQueryValue(
        string rawProjectId,
        string expectedValue)
    {
        RecordingHandler handler = RecordingHandler.Json("{}");

        await Exp354TestContext.Partners(handler).GetFeeDetailsAsync(rawProjectId);

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal(
            $"{FeeDetailsEndpoint}?merchant_project_id={expectedValue}",
            recorded.RequestUri.PathAndQuery);
        Assert.Equal(FeeDetailsEndpoint, recorded.RequestUri.AbsolutePath);
        Assert.Equal(string.Empty, recorded.RequestUri.Fragment);
    }

    [Fact]
    public async Task GetFeeDetails_ShouldMapInnerAndOuterFeesPerChannel()
    {
        RecordingHandler handler = RecordingHandler.Json("""
            {
              "online": {
                "inner_fee": { "fix": 1.5, "max": 100, "min": 0.5, "percent": 2.75 },
                "outer_fee": { "fix": 0.25, "max": 50, "min": 0.1, "percent": 1.1 }
              },
              "pnfp": {
                "inner_fee": { "fix": 3, "max": 300, "min": 3, "percent": 0.9 }
              }
            }
            """);

        PartnerFeeDetailsResponse response = await Exp354TestContext.Partners(handler).GetFeeDetailsAsync();

        Assert.NotNull(response.Online);
        Assert.Equal(1.5m, response.Online!.InnerFee!.Fix);
        Assert.Equal(100m, response.Online.InnerFee.Max);
        Assert.Equal(0.5m, response.Online.InnerFee.Min);
        Assert.Equal(2.75m, response.Online.InnerFee.Percent);
        Assert.Equal(0.25m, response.Online.OuterFee!.Fix);
        Assert.Equal(1.1m, response.Online.OuterFee.Percent);

        Assert.NotNull(response.Pnfp);
        Assert.Equal(0.9m, response.Pnfp!.InnerFee!.Percent);

        // An absent channel object stays null rather than becoming an empty one.
        Assert.Null(response.Pnfp.OuterFee);
    }

    // ===================== merchant status =====================

    [Fact]
    public async Task GetMerchantStatus_WithoutOptions_ShouldSendNoQueryAtAll()
    {
        RecordingHandler handler = RecordingHandler.Json("{}");

        await Exp354TestContext.Partners(handler).GetMerchantStatusAsync();

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, recorded.Method);
        Assert.Equal(MerchantStatusEndpoint, recorded.RequestUri.PathAndQuery);
        Assert.DoesNotContain("?", recorded.RequestUri.PathAndQuery);
    }

    /// <summary>
    /// Empty options are not the same as no options overload: both send no query, but the options path
    /// must reach the same target rather than a bare '?'.
    /// </summary>
    [Fact]
    public async Task GetMerchantStatus_WithEmptyOptions_ShouldSendNoQueryAtAll()
    {
        RecordingHandler handler = RecordingHandler.Json("{}");

        await Exp354TestContext.Partners(handler).GetMerchantStatusAsync(new PartnerMerchantStatusOptions());

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal(MerchantStatusEndpoint, recorded.RequestUri.PathAndQuery);
        Assert.DoesNotContain("?", recorded.RequestUri.PathAndQuery);
    }

    [Fact]
    public async Task GetMerchantStatus_WithBothOptions_ShouldUseTheDocumentedOrder()
    {
        RecordingHandler handler = RecordingHandler.Json("{}");

        await Exp354TestContext.Partners(handler).GetMerchantStatusAsync(new PartnerMerchantStatusOptions
        {
            MerchantEntityId = "entity-1",
            MerchantProjectId = "project-1"
        });

        Exp354Request recorded = Assert.Single(handler.Requests);

        // Project first, entity second - regardless of the order the caller set the properties in.
        Assert.Equal(
            $"{MerchantStatusEndpoint}?merchant_project_id=project-1&merchant_entity_id=entity-1",
            recorded.RequestUri.PathAndQuery);
    }

    [Theory]
    [InlineData("project-1", null, "?merchant_project_id=project-1")]
    [InlineData(null, "entity-1", "?merchant_entity_id=entity-1")]
    [InlineData("", null, "?merchant_project_id=")]
    [InlineData(null, "", "?merchant_entity_id=")]
    [InlineData("", "", "?merchant_project_id=&merchant_entity_id=")]
    public async Task GetMerchantStatus_ShouldOmitNullAndSendEmpty(
        string? projectId,
        string? entityId,
        string expectedQuery)
    {
        RecordingHandler handler = RecordingHandler.Json("{}");

        await Exp354TestContext.Partners(handler).GetMerchantStatusAsync(new PartnerMerchantStatusOptions
        {
            MerchantProjectId = projectId,
            MerchantEntityId = entityId
        });

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal($"{MerchantStatusEndpoint}{expectedQuery}", recorded.RequestUri.PathAndQuery);
    }

    [Fact]
    public async Task GetMerchantStatus_ShouldEscapeBothOptionsExactlyOnce()
    {
        RecordingHandler handler = RecordingHandler.Json("{}");

        await Exp354TestContext.Partners(handler).GetMerchantStatusAsync(new PartnerMerchantStatusOptions
        {
            MerchantProjectId = Exp354TestContext.HostileRawId,
            MerchantEntityId = Exp354TestContext.LooksEncodedRawId
        });

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal(
            $"{MerchantStatusEndpoint}?merchant_project_id={Exp354TestContext.HostileEncodedId}" +
            $"&merchant_entity_id={Exp354TestContext.LooksEncodedExpectedId}",
            recorded.RequestUri.PathAndQuery);

        // Everything stayed inside the two values: no third parameter appeared, and the path is intact.
        Assert.Equal(MerchantStatusEndpoint, recorded.RequestUri.AbsolutePath);
        Assert.Equal(string.Empty, recorded.RequestUri.Fragment);
    }

    /// <summary>
    /// The response is deliberately the existing <see cref="MerchantStatusResponse"/>: its shape already
    /// matches the official partner merchant-status response, including the status enumeration.
    /// </summary>
    [Theory]
    [InlineData("onboarding", MerchantStatus.Onboarding)]
    [InlineData("activated", MerchantStatus.Activated)]
    [InlineData("blocked", MerchantStatus.Blocked)]
    [InlineData("external_merchant", MerchantStatus.ExternalMerchant)]
    public async Task GetMerchantStatus_ShouldMapEveryStatusToken(string token, MerchantStatus expected)
    {
        RecordingHandler handler = RecordingHandler.Json($$"""{"status":"{{token}}"}""");

        MerchantStatusResponse response = await Exp354TestContext.Partners(handler).GetMerchantStatusAsync();

        Assert.Equal(expected, response.Status);
    }

    [Fact]
    public async Task GetMerchantStatus_ShouldMapEntityAndProjectDetails()
    {
        RecordingHandler handler = RecordingHandler.Json("""
            {
              "entity": {
                "bank_details_number": "UA00000000000000000000",
                "business_registration_number": "12345678",
                "id": "entity-1",
                "name": "Entity",
                "status": "activated"
              },
              "project": { "id": "project-1", "name": "Project", "status": "activated" },
              "status": "activated"
            }
            """);

        MerchantStatusResponse response = await Exp354TestContext.Partners(handler).GetMerchantStatusAsync();

        Assert.Equal("entity-1", response.Entity!.Id);
        Assert.Equal("Entity", response.Entity.Name);
        Assert.Equal("12345678", response.Entity.BusinessRegistrationNumber);
        Assert.Equal("UA00000000000000000000", response.Entity.BankDetailsNumber);
        Assert.Equal("project-1", response.Project!.Id);
        Assert.Equal("Project", response.Project.Name);
        Assert.Equal(MerchantStatus.Activated, response.Status);
    }

    // ===================== transaction details =====================

    [Theory]
    [InlineData(Exp354TestContext.HostileRawId, Exp354TestContext.HostileEncodedId)]
    [InlineData(Exp354TestContext.LooksEncodedRawId, Exp354TestContext.LooksEncodedExpectedId)]
    [InlineData("entity-1", "entity-1")]
    [InlineData("", "")]
    public async Task GetTransactionDetails_RequiredOnly_ShouldSendOneEscapedQueryValue(
        string rawEntityId,
        string expectedValue)
    {
        RecordingHandler handler = RecordingHandler.Json("{}");

        await Exp354TestContext.Partners(handler).GetTransactionDetailsAsync(rawEntityId);

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, recorded.Method);
        Assert.Equal(
            $"{TransactionDetailsEndpoint}?merchant_entity_id={expectedValue}",
            recorded.RequestUri.PathAndQuery);
        Assert.False(recorded.HasContent);
    }

    [Fact]
    public async Task GetTransactionDetails_WithBothOptions_ShouldUseTheDocumentedOrder()
    {
        RecordingHandler handler = RecordingHandler.Json("{}");

        await Exp354TestContext.Partners(handler).GetTransactionDetailsAsync(
            "entity-1",
            new PartnerTransactionDetailsOptions
            {
                UnifiedExternalId = "uid-1",
                MerchantOrderId = "order-1"
            });

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal(
            $"{TransactionDetailsEndpoint}?merchant_entity_id=entity-1&merchant_order_id=order-1&unified_external_id=uid-1",
            recorded.RequestUri.PathAndQuery);
    }

    [Theory]
    [InlineData(null, null, "")]
    [InlineData("order-1", null, "&merchant_order_id=order-1")]
    [InlineData(null, "uid-1", "&unified_external_id=uid-1")]
    [InlineData("", null, "&merchant_order_id=")]
    [InlineData(null, "", "&unified_external_id=")]
    public async Task GetTransactionDetails_ShouldOmitNullAndSendEmpty(
        string? orderId,
        string? unifiedExternalId,
        string expectedTail)
    {
        RecordingHandler handler = RecordingHandler.Json("{}");

        await Exp354TestContext.Partners(handler).GetTransactionDetailsAsync(
            "entity-1",
            new PartnerTransactionDetailsOptions
            {
                MerchantOrderId = orderId,
                UnifiedExternalId = unifiedExternalId
            });

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal(
            $"{TransactionDetailsEndpoint}?merchant_entity_id=entity-1{expectedTail}",
            recorded.RequestUri.PathAndQuery);
    }

    [Fact]
    public async Task GetTransactionDetails_ShouldEscapeEveryValueExactlyOnce()
    {
        RecordingHandler handler = RecordingHandler.Json("{}");

        await Exp354TestContext.Partners(handler).GetTransactionDetailsAsync(
            Exp354TestContext.HostileRawId,
            new PartnerTransactionDetailsOptions
            {
                MerchantOrderId = Exp354TestContext.LooksEncodedRawId,
                UnifiedExternalId = "uid +1"
            });

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal(
            $"{TransactionDetailsEndpoint}?merchant_entity_id={Exp354TestContext.HostileEncodedId}" +
            $"&merchant_order_id={Exp354TestContext.LooksEncodedExpectedId}" +
            "&unified_external_id=uid%20%2B1",
            recorded.RequestUri.PathAndQuery);
        Assert.Equal(TransactionDetailsEndpoint, recorded.RequestUri.AbsolutePath);
    }

    [Fact]
    public async Task GetTransactionDetails_ShouldMapEveryDeclaredField()
    {
        RecordingHandler handler = RecordingHandler.Json("""
            {
              "transactions": [
                {
                  "card_mask": "444455******1111",
                  "merchant_entity_id": "entity-1",
                  "merchant_fee_amount": "1.50",
                  "merchant_order_id": "order-1",
                  "unified_external_id": "uid-1",
                  "method": "card",
                  "order_description": "Order description",
                  "order_id": "internal-1",
                  "pay_way": "online",
                  "payment_amount": "100.00",
                  "payment_original_amount": "110.00",
                  "payment_recipient_amount": "98.50",
                  "processed_at": "2026-07-25 10:11:12",
                  "recipient_card_mask": "555566******2222",
                  "status": "success"
                },
                { "status": "pending" }
              ]
            }
            """);

        PartnerTransactionDetailsListResponse response = await Exp354TestContext.Partners(handler)
            .GetTransactionDetailsAsync("entity-1");

        Assert.NotNull(response.Transactions);
        Assert.Equal(2, response.Transactions!.Count);

        PartnerTransactionDetails first = response.Transactions[0];
        Assert.Equal("444455******1111", first.CardMask);
        Assert.Equal("entity-1", first.MerchantEntityId);
        Assert.Equal("1.50", first.MerchantFeeAmount);
        Assert.Equal("order-1", first.MerchantOrderId);
        Assert.Equal("uid-1", first.UnifiedExternalId);
        Assert.Equal("card", first.Method);
        Assert.Equal("Order description", first.OrderDescription);
        Assert.Equal("internal-1", first.OrderId);
        Assert.Equal("online", first.PayWay);
        Assert.Equal("100.00", first.PaymentAmount);
        Assert.Equal("110.00", first.PaymentOriginalAmount);
        Assert.Equal("98.50", first.PaymentRecipientAmount);
        Assert.Equal("555566******2222", first.RecipientCardMask);
        Assert.Equal("success", first.Status);

        // processed_at is declared as a bare string, so it stays text rather than being parsed.
        Assert.Equal("2026-07-25 10:11:12", first.ProcessedAt);
        Assert.Equal(typeof(string), typeof(PartnerTransactionDetails).GetProperty(
            nameof(PartnerTransactionDetails.ProcessedAt))!.PropertyType);

        // Absent fields stay null instead of becoming empty strings.
        Assert.Null(response.Transactions[1].CardMask);
        Assert.Equal("pending", response.Transactions[1].Status);
    }

    [Fact]
    public async Task GetTransactionDetails_ShouldKeepAnEmptyListDistinctFromNull()
    {
        RecordingHandler emptyList = RecordingHandler.Json("""{"transactions":[]}""");
        PartnerTransactionDetailsListResponse empty = await Exp354TestContext.Partners(emptyList)
            .GetTransactionDetailsAsync("entity-1");
        Assert.NotNull(empty.Transactions);
        Assert.Empty(empty.Transactions!);

        RecordingHandler absent = RecordingHandler.Json("{}");
        PartnerTransactionDetailsListResponse missing = await Exp354TestContext.Partners(absent)
            .GetTransactionDetailsAsync("entity-1");
        Assert.Null(missing.Transactions);
    }

    // ===================== cross-cutting =====================

    [Fact]
    public async Task EveryOverload_ShouldRejectNullArguments()
    {
        RecordingHandler handler = RecordingHandler.Json("{}");
        PartnerService service = Exp354TestContext.Partners(handler);

        Assert.Equal(
            "merchantProjectId",
            (await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetFeeDetailsAsync(null!))).ParamName);
        Assert.Equal(
            "options",
            (await Assert.ThrowsAsync<ArgumentNullException>(
                () => service.GetMerchantStatusAsync((PartnerMerchantStatusOptions)null!))).ParamName);
        Assert.Equal(
            "merchantEntityId",
            (await Assert.ThrowsAsync<ArgumentNullException>(
                () => service.GetTransactionDetailsAsync(null!))).ParamName);
        Assert.Equal(
            "merchantEntityId",
            (await Assert.ThrowsAsync<ArgumentNullException>(
                () => service.GetTransactionDetailsAsync(null!, new PartnerTransactionDetailsOptions()))).ParamName);
        Assert.Equal(
            "options",
            (await Assert.ThrowsAsync<ArgumentNullException>(
                () => service.GetTransactionDetailsAsync("entity-1", null!))).ParamName);

        Assert.Empty(handler.Requests);
    }

    /// <summary>
    /// No partner identifier may reach a log sink; only the static route does.
    /// </summary>
    [Fact]
    public async Task EveryOperation_ShouldLogTheStaticRouteOnly()
    {
        RecordingHandler handler = RecordingHandler.Json("{}");
        RecordingLogger logger = new();
        PartnerService service = Exp354TestContext.Partners(handler, logger: logger);

        await service.GetFeeDetailsAsync(Exp354TestContext.SecretProjectId);
        await service.GetMerchantStatusAsync(new PartnerMerchantStatusOptions
        {
            MerchantProjectId = Exp354TestContext.SecretProjectId,
            MerchantEntityId = Exp354TestContext.SecretMerchantId
        });
        await service.GetTransactionDetailsAsync(
            Exp354TestContext.SecretMerchantId,
            new PartnerTransactionDetailsOptions { MerchantOrderId = Exp354TestContext.SecretExternalId });

        string[] forbidden =
        [
            Exp354TestContext.SecretProjectId,
            Exp354TestContext.SecretMerchantId,
            Exp354TestContext.SecretExternalId
        ];

        foreach (string text in logger.AllText)
        {
            foreach (string marker in forbidden)
            {
                Assert.DoesNotContain(marker, text, StringComparison.Ordinal);
            }
        }

        Assert.Contains(logger.StateValues, value => value.Contains(FeeDetailsEndpoint, StringComparison.Ordinal));
        Assert.Contains(logger.StateValues, value => value.Contains(MerchantStatusEndpoint, StringComparison.Ordinal));
        Assert.Contains(
            logger.StateValues,
            value => value.Contains(TransactionDetailsEndpoint, StringComparison.Ordinal));

        // No log entry carries a query string at all.
        Assert.All(logger.AllText, text => Assert.DoesNotContain("merchant_project_id=", text, StringComparison.Ordinal));
        Assert.All(logger.AllText, text => Assert.DoesNotContain("merchant_entity_id=", text, StringComparison.Ordinal));
    }

    [Fact]
    public async Task EveryOperation_ShouldPropagateCancellation()
    {
        await AssertCancels((service, token) => service.GetFeeDetailsAsync(token));
        await AssertCancels((service, token) => service.GetFeeDetailsAsync("project-1", token));
        await AssertCancels((service, token) => service.GetMerchantStatusAsync(token));
        await AssertCancels(
            (service, token) => service.GetMerchantStatusAsync(new PartnerMerchantStatusOptions(), token));
        await AssertCancels((service, token) => service.GetTransactionDetailsAsync("entity-1", token));
        await AssertCancels(
            (service, token) => service.GetTransactionDetailsAsync(
                "entity-1",
                new PartnerTransactionDetailsOptions(),
                token));
    }

    [Theory]
    [MemberData(nameof(ErrorMappings))]
    public async Task EveryOperation_ShouldMapErrorsThroughTheExistingExceptions(
        HttpStatusCode status,
        Type expectedExceptionType)
    {
        RecordingHandler handler = RecordingHandler.Error(
            status,
            """{"code":"partner_denied","message":"Partner access denied","error_id":"req-11"}""");
        PartnerService service = Exp354TestContext.Partners(handler);

        RozetkaPayException failure = (RozetkaPayException)await Assert.ThrowsAnyAsync<Exception>(
            () => service.GetFeeDetailsAsync());

        Assert.IsType(expectedExceptionType, failure);
        Assert.Equal(status, failure.ApiError!.StatusCode);
        Assert.Equal("partner_denied", failure.ApiError.Code);
        Assert.Equal("req-11", failure.ApiError.RequestId);
    }

    /// <summary>
    /// Every partner call is authenticated, so the credential header must be attached — and must stay out
    /// of the logs.
    /// </summary>
    [Fact]
    public async Task EveryOperation_ShouldBeAuthenticated()
    {
        RecordingHandler handler = RecordingHandler.Json("{}");
        RecordingLogger logger = new();

        await Exp354TestContext.Partners(handler, logger: logger).GetFeeDetailsAsync();

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.True(recorded.Headers.ContainsKey("Authorization"));
        Assert.All(
            logger.AllText,
            text => Assert.DoesNotContain("Basic ", text, StringComparison.Ordinal));
    }

    private static async Task AssertCancels(Func<PartnerService, CancellationToken, Task> operation)
    {
        RecordingHandler handler = RecordingHandler.Json("{}");
        using CancellationTokenSource cancellation = new();
        handler.OnRequest = (_, _) => cancellation.Cancel();

        PartnerService service = Exp354TestContext.Partners(handler);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation(service, cancellation.Token));

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.True(recorded.CancellationRequestedOnArrival);
    }
}
