using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Models.AlternativePayments;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Models.Payouts;
using SYT.RozetkaPay.Models.PayParts;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

public class ServicesCoverageExpansionTests
{
    [Fact]
    public async Task PaymentService_ShouldHitAllCoreEndpoints()
    {
        List<string> calls = new();
        StubHttpMessageHandler handler = new(async (request, _) =>
        {
            calls.Add($"{request.Method} {request.RequestUri!.PathAndQuery}");
            if (request.RequestUri!.AbsolutePath == "/api/payments/v1/callback/resend")
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            return Json("{}");
        });

        PaymentService service = new(CreateConfiguration(), CreateHttpClient(handler));

        await service.CreateAsync(null!);
        await service.CreateRecurrentAsync(null!);
        await service.ConfirmAsync(null!);
        await service.CancelAsync(null!);
        await service.RefundAsync(null!);
        await service.GetInfoAsync("pay-1");

        await service.GetListAsync(new PaymentListRequest
        {
            DateFrom = new DateTime(2026, 2, 28),
            DateTo = new DateTime(2026, 3, 1),
            Status = "success",
            Limit = 10,
            Offset = 5
        });

        await service.GetListAsync(new PaymentListRequest());
        await service.GetReceiptAsync("pay-1");
        await service.CardLookupAsync(null!);
        await service.ResendCallbackAsync(null!);
        await service.ConfirmP2PAsync("p2p-1");
        await service.ConfirmP2PAsync("p2p-2", 100m);

        Assert.Contains("POST /api/payments/v1/new", calls);
        Assert.Contains("POST /api/payments/v1/recurrent", calls);
        Assert.Contains("POST /api/payments/v1/confirm", calls);
        Assert.Contains("POST /api/payments/v1/cancel", calls);
        Assert.Contains("POST /api/payments/v1/refund", calls);
        Assert.Contains("GET /api/payments/v1/info?external_id=pay-1", calls);
        Assert.Contains("GET /api/payments/v1/list?date_from=2026-02-28&date_to=2026-03-01&status=success&limit=10&offset=5", calls);
        Assert.Contains("GET /api/payments/v1/list", calls);
        Assert.Contains("GET /api/payments/v1/receipt?external_id=pay-1", calls);
        Assert.Contains("POST /api/payments/v1/lookup", calls);
        Assert.Contains("POST /api/payments/v1/callback/resend", calls);
        Assert.Contains("POST /api/payments/v1/p2p/confirm", calls);
    }

    [Fact]
    public async Task PaymentService_CreateP2P_ShouldThrowWhenRecipientMissing()
    {
        StubHttpMessageHandler handler = new(async (_, _) => Json("{}"));
        PaymentService service = new(CreateConfiguration(), CreateHttpClient(handler));

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.CreateP2PAsync(new CreatePaymentRequest
            {
                Amount = 1m,
                Currency = "UAH",
                ExternalId = "p2p-1",
                Mode = PaymentMode.Direct
            }));

        Assert.Contains("Recipient information is required", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PaymentService_CreateP2P_ShouldUseNewPaymentEndpointWhenRecipientExists()
    {
        StubHttpMessageHandler handler = new(async (_, _) => Json("{}"));
        PaymentService service = new(CreateConfiguration(), CreateHttpClient(handler));

        await service.CreateP2PAsync(new CreatePaymentRequest
        {
            Amount = 10m,
            Currency = "UAH",
            ExternalId = "p2p-2",
            Mode = PaymentMode.Direct,
            Recipient = new RecipientRequestUserDetails
            {
                PaymentMethod = new RecipientRequestPaymentMethod
                {
                    Type = "card_number",
                    CardNumber = new RecipientCCNumberRequestPaymentMethod
                    {
                        Number = "4111111111111111",
                        ExpirationMonth = 12,
                        ExpirationYear = 2030
                    }
                }
            }
        });

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/api/payments/v1/new", handler.LastRequest.RequestUri!.PathAndQuery);
    }

    [Fact]
    public void PaymentService_BuildP2PRequest_ShouldPopulateExpectedFields()
    {
        CreatePaymentRequest request = PaymentService.BuildP2PRequest(
            amount: 50m,
            currency: "UAH",
            externalId: "p2p-3",
            recipientCardNumber: "4111111111111111",
            recipientExpMonth: "01",
            recipientExpYear: "2030");

        Assert.Equal(PaymentMode.Direct, request.Mode);
        Assert.Equal("p2p-3", request.ExternalId);
        Assert.NotNull(request.Recipient);
        Assert.Equal("card_number", request.Recipient!.PaymentMethod.Type);
        Assert.Equal("4111111111111111", request.Recipient.PaymentMethod.CardNumber!.Number);
        Assert.Equal(1, request.Recipient.PaymentMethod.CardNumber.ExpirationMonth);
        Assert.Equal(2030, request.Recipient.PaymentMethod.CardNumber.ExpirationYear);
    }

    [Fact]
    public async Task AlternativePaymentService_ShouldHitAllEndpoints()
    {
        List<string> calls = new();
        StubHttpMessageHandler handler = new(async (request, _) =>
        {
            calls.Add($"{request.Method} {request.RequestUri!.PathAndQuery}");
            if (request.RequestUri!.AbsolutePath == "/api/alternative-payments/v1/callback/resend")
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            return Json("{}");
        });

        AlternativePaymentService service = new(CreateConfiguration(), CreateHttpClient(handler));

        await service.CreateAsync(null!);
        await service.CreateOperationAsync(null!);
        await service.RefundAsync(null!);
        await service.ResendCallbackAsync(null!);
        await service.GetOperationInfoAsync("ext-1");
        await service.GetOperationInfoAsync("ext-2", "op-2");

        await service.GetOperationsAsync(new GetAlternativePaymentOperationsRequest
        {
            DateFrom = "2026-02-28",
            DateTo = "2026-03-01",
            Status = "success",
            Limit = 20,
            Offset = 3
        });
        await service.GetOperationsAsync(new GetAlternativePaymentOperationsRequest());

        await service.GetInfoAsync("ext-3");
        await service.GetAvailableMethodsAsync();
        await service.GetStatusAsync("payment-1");

        Assert.Contains("POST /api/alternative-payments/v1/create", calls);
        Assert.Contains("POST /api/alternative-payments/v1/refund", calls);
        Assert.Contains("POST /api/alternative-payments/v1/callback/resend", calls);
        Assert.Contains("GET /api/alternative-payments/v1/operation/ext-1", calls);
        Assert.Contains("GET /api/alternative-payments/v1/info/operation?external_id=ext-2&operation_id=op-2", calls);
        Assert.Contains("GET /api/alternative-payments/v1/operations?date_from=2026-02-28&date_to=2026-03-01&status=success&limit=20&offset=3", calls);
        Assert.Contains("GET /api/alternative-payments/v1/operations", calls);
        Assert.Contains("GET /api/alternative-payments/v1/info?external_id=ext-3", calls);
        Assert.Contains("GET /api/alternative-payments/v1/methods", calls);
        Assert.Contains("GET /api/alternative-payments/v1/payment-1/status", calls);
    }

    [Fact]
    public async Task AlternativePaymentService_GetOperationInfo_ShouldFallbackOnNotFound()
    {
        List<string> calls = new();
        StubHttpMessageHandler handler = new(async (request, _) =>
        {
            calls.Add(request.RequestUri!.PathAndQuery);
            if (request.RequestUri!.AbsolutePath == "/api/alternative-payments/v1/info/operation")
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("""{"message":"not found"}""", Encoding.UTF8, "application/json")
                };
            }

            return Json("{}");
        });

        AlternativePaymentService service = new(CreateConfiguration(), CreateHttpClient(handler));
        await service.GetOperationInfoAsync("ext-10", "op-10");

        Assert.Equal(2, calls.Count);
        Assert.Equal("/api/alternative-payments/v1/info/operation?external_id=ext-10&operation_id=op-10", calls[0]);
        Assert.Equal("/api/alternative-payments/v1/operation/ext-10", calls[1]);
    }

    [Fact]
    public async Task AlternativePaymentService_CreateOperation_ShouldFallbackOnNotFound()
    {
        List<string> calls = new();
        StubHttpMessageHandler handler = new(async (request, _) =>
        {
            calls.Add(request.RequestUri!.PathAndQuery);
            if (request.RequestUri!.AbsolutePath == "/api/alternative-payments/v1/create")
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("""{"message":"not found"}""", Encoding.UTF8, "application/json")
                };
            }

            return Json("{}");
        });

        AlternativePaymentService service = new(CreateConfiguration(), CreateHttpClient(handler));
        await service.CreateOperationAsync(null!);

        Assert.Equal(2, calls.Count);
        Assert.Equal("/api/alternative-payments/v1/create", calls[0]);
        Assert.Equal("/api/alternative-payments/v1/new", calls[1]);
    }

    [Fact]
    public async Task PayPartsService_ShouldHitAllEndpoints()
    {
        List<string> calls = new();
        StubHttpMessageHandler handler = new(async (request, _) =>
        {
            calls.Add($"{request.Method} {request.RequestUri!.PathAndQuery}");
            return Json("{}");
        });

        PayPartsService service = new(CreateConfiguration(), CreateHttpClient(handler));

        await service.CreateOrderAsync(null!);
        await service.ConfirmOrderAsync(null!);
        await service.CancelOrderAsync(null!);
        await service.RefundOrderAsync(null!);
        await service.RetryRefundAsync(null!);
        await service.CancelRefundAsync(null!);
        await service.GetOperationInfoAsync("op-1");
        await service.GetOperationInfoAsync("ext-1", "op-1");
        await service.GetInfoAsync("ext-2");

        await service.GetOperationsAsync(new PayPartsOperationsListRequest
        {
            DateFrom = new DateTime(2026, 2, 28),
            DateTo = new DateTime(2026, 3, 1),
            Status = "pending",
            Limit = 50,
            Offset = 2
        });
        await service.GetOperationsAsync(new PayPartsOperationsListRequest());

        await service.GetBanksAsync();
        await service.ResendCallbackAsync(null!);

        Assert.Contains("POST /api/payparts/v1/order/create", calls);
        Assert.Contains("POST /api/payparts/v1/order/confirm", calls);
        Assert.Contains("POST /api/payparts/v1/order/cancel", calls);
        Assert.Contains("POST /api/payparts/v1/refund", calls);
        Assert.Contains("POST /api/payparts/v1/refund/retry", calls);
        Assert.Contains("POST /api/payparts/v1/refund/cancel", calls);
        Assert.Contains("GET /api/payparts/v1/operation/op-1", calls);
        Assert.Contains("GET /api/payparts/v1/info/operation?external_id=ext-1&operation_id=op-1", calls);
        Assert.Contains("GET /api/payparts/v1/info?external_id=ext-2", calls);
        Assert.Contains("GET /api/payparts/v1/operations?date_from=2026-02-28&date_to=2026-03-01&status=pending&limit=50&offset=2", calls);
        Assert.Contains("GET /api/payparts/v1/operations", calls);
        Assert.Contains("GET /api/payparts/v1/banks/info", calls);
        Assert.Contains("POST /api/payparts/v1/callback/resend", calls);
    }

    [Fact]
    public async Task PayPartsService_ConfirmCancelRefund_ShouldFallbackOnNotFound()
    {
        Dictionary<string, int> primaryCalls = new()
        {
            ["/api/payparts/v1/order/confirm"] = 0,
            ["/api/payparts/v1/order/cancel"] = 0,
            ["/api/payparts/v1/refund"] = 0
        };
        List<string> calls = new();

        StubHttpMessageHandler handler = new(async (request, _) =>
        {
            string path = request.RequestUri!.AbsolutePath;
            calls.Add(request.RequestUri!.PathAndQuery);
            if (primaryCalls.ContainsKey(path))
            {
                primaryCalls[path]++;
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("""{"message":"not found"}""", Encoding.UTF8, "application/json")
                };
            }

            return Json("{}");
        });

        PayPartsService service = new(CreateConfiguration(), CreateHttpClient(handler));
        await service.ConfirmOrderAsync(null!);
        await service.CancelOrderAsync(null!);
        await service.RefundOrderAsync(null!);

        Assert.Contains("/api/payments/v1/payparts/confirm", calls);
        Assert.Contains("/api/payments/v1/payparts/cancel", calls);
        Assert.Contains("/api/payments/v1/payparts/refund", calls);
    }

    [Fact]
    public async Task PayPartsService_GetOperationInfoAndBanks_ShouldFallbackOnNotFound()
    {
        List<string> calls = new();
        StubHttpMessageHandler handler = new(async (request, _) =>
        {
            calls.Add(request.RequestUri!.PathAndQuery);
            if (request.RequestUri!.AbsolutePath is "/api/payparts/v1/info/operation" or "/api/payparts/v1/banks/info")
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("""{"message":"not found"}""", Encoding.UTF8, "application/json")
                };
            }

            return Json("{}");
        });

        PayPartsService service = new(CreateConfiguration(), CreateHttpClient(handler));
        await service.GetOperationInfoAsync("external-1", "operation-1");
        await service.GetBanksAsync();

        Assert.Equal("/api/payparts/v1/info/operation?external_id=external-1&operation_id=operation-1", calls[0]);
        Assert.Equal("/api/payparts/v1/operation/operation-1", calls[1]);
        Assert.Equal("/api/payparts/v1/banks/info", calls[2]);
        Assert.Equal("/api/payparts/v1/banks", calls[3]);
    }

    [Fact]
    public async Task CustomerService_ShouldHitDirectEndpoints()
    {
        StubHttpMessageHandler handler = new(async (_, _) => Json("{}"));
        CustomerService service = new(CreateConfiguration(), CreateHttpClient(handler));

        await service.DeletePaymentFromWalletAsync("customer-1", "card-1");
        Assert.Equal("/api/customers/v1/customer-1/cards/card-1", handler.LastRequest!.RequestUri!.PathAndQuery);

        await service.GetCustomerCardsAsync("customer-1");
        Assert.Equal("/api/customers/v1/customer-1/cards", handler.LastRequest!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task CustomerService_WalletOperations_ShouldFallbackOnNotFound()
    {
        List<string> calls = new();
        StubHttpMessageHandler handler = new(async (request, _) =>
        {
            calls.Add(request.RequestUri!.PathAndQuery);
            if (request.RequestUri!.AbsolutePath is
                "/api/customers/v1/wallet" or
                "/api/customers/v1/wallet/find" or
                "/api/customers/v1/wallet/confirmation/status" or
                "/api/customers/v1/wallet/settings/set")
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("""{"message":"not found"}""", Encoding.UTF8, "application/json")
                };
            }

            return Json("{}");
        });

        CustomerService service = new(CreateConfiguration(), CreateHttpClient(handler));
        await service.GetCustomerWalletAsync("customer-7");
        await service.AddCardToWalletAsync("customer-7", null!);
        await service.GetWalletItemAsync("customer-7", "card-7");
        await service.GetCardConfirmationStatusAsync("customer-7", "card-7");
        await service.SetDefaultCardAsync("customer-7", null!);

        Assert.Contains("/api/customers/v1/customer-7/wallet", calls);
        Assert.Contains("/api/customers/v1/customer-7/cards", calls);
        Assert.Contains("/api/customers/v1/customer-7/cards/card-7", calls);
        Assert.Contains("/api/customers/v1/customer-7/cards/card-7/confirmation", calls);
        Assert.Contains("/api/customers/v1/customer-7/cards/default", calls);
    }

    [Fact]
    public async Task PayoutService_ShouldHitAllEndpoints()
    {
        List<string> calls = new();
        StubHttpMessageHandler handler = new(async (request, _) =>
        {
            calls.Add($"{request.Method} {request.RequestUri!.PathAndQuery}");
            if (request.RequestUri!.AbsolutePath == "/api/payouts/v1/resend-callback")
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            return Json("{}");
        });

        PayoutService service = new(CreateConfiguration(), CreateHttpClient(handler));
        await service.CreateAsync(null!);
        await service.RequestPayoutAsync(null!);
        await service.GetInfoAsync("payout-1");

        await service.GetListAsync(new PayoutListRequest
        {
            DateFrom = "2026-02-01",
            DateTo = "2026-02-28",
            Status = "success",
            Limit = 10,
            Offset = 1
        });
        await service.GetListAsync(new PayoutListRequest());

        await service.GetBalanceAsync();
        await service.GetAccountBalanceAsync("merchant 1");
        await service.ResendCallbackAsync(null!);
        await service.CancelCashPayoutAsync(null!);

        Assert.Contains("POST /api/payouts/v1/new", calls);
        Assert.Contains("POST /api/payouts/v1/request-payout", calls);
        Assert.Contains("GET /api/payouts/v1/info?external_id=payout-1", calls);
        Assert.Contains("GET /api/payouts/v1/list?date_from=2026-02-01&date_to=2026-02-28&status=success&limit=10&offset=1", calls);
        Assert.Contains("GET /api/payouts/v1/list", calls);
        Assert.Contains("GET /api/payouts/v1/balance", calls);
        Assert.Contains("GET /api/payouts/v1/account-balance?merchant_entity_id=merchant%201", calls);
        Assert.Contains("POST /api/payouts/v1/resend-callback", calls);
        Assert.Contains("POST /api/payouts/v1/cancel-payout", calls);
    }

    [Fact]
    public async Task SubscriptionService_ShouldHitAllEndpoints()
    {
        List<string> calls = new();
        StubHttpMessageHandler handler = new(async (request, _) =>
        {
            calls.Add($"{request.Method} {request.RequestUri!.PathAndQuery}");
            return Json("{}");
        });

        SubscriptionService service = new(CreateConfiguration(), CreateHttpClient(handler));

        await service.GetPlansAsync();
        await service.CreatePlanAsync(null!);
        await service.DeactivatePlanAsync("plan-1");
        await service.GetPlanAsync("plan-1");
        await service.UpdatePlanAsync("plan-1", null!);

        await service.CreateAsync(null!);
        await service.GiftAsync(null!);
        await service.GetCustomerSubscriptionsAsync("customer-1");
        await service.DeactivateAsync("sub-1");
        await service.GetAsync("sub-1");
        await service.UpdateAsync("sub-1", null!);
        await service.GetPaymentsAsync("sub-1");
        await service.CancelAsync("sub-1", null!);

        Assert.Contains("GET /api/subscriptions/v1/plans", calls);
        Assert.Contains("POST /api/subscriptions/v1/plans", calls);
        Assert.Contains("DELETE /api/subscriptions/v1/plans/plan-1", calls);
        Assert.Contains("GET /api/subscriptions/v1/plans/plan-1", calls);
        Assert.Contains("PATCH /api/subscriptions/v1/plans/plan-1", calls);

        Assert.Contains("POST /api/subscriptions/v1/subscriptions", calls);
        Assert.Contains("POST /api/subscriptions/v1/subscriptions/gift", calls);
        Assert.Contains("GET /api/subscriptions/v1/subscriptions/customer/customer-1", calls);
        Assert.Contains("DELETE /api/subscriptions/v1/subscriptions/sub-1", calls);
        Assert.Contains("GET /api/subscriptions/v1/subscriptions/sub-1", calls);
        Assert.Contains("PATCH /api/subscriptions/v1/subscriptions/sub-1", calls);
        Assert.Contains("GET /api/subscriptions/v1/subscriptions/sub-1/payments", calls);
        Assert.Contains("POST /api/subscriptions/v1/subscriptions/sub-1/cancel", calls);
    }

    [Fact]
    public async Task MerchantBatchReportFinMonServices_ShouldHitEndpoints()
    {
        List<string> calls = new();
        StubHttpMessageHandler handler = new(async (request, _) =>
        {
            calls.Add($"{request.Method} {request.RequestUri!.PathAndQuery}");
            return Json("{}");
        });

        RozetkaPayConfiguration configuration = CreateConfiguration();

        MerchantService merchant = new(configuration, CreateHttpClient(handler));
        await merchant.GetInfoAsync();
        await merchant.GetSettingsAsync();
        await merchant.UpdateSettingsAsync(null!);
        await merchant.GetCommissionRatesAsync();

        BatchPaymentService batch = new(configuration, CreateHttpClient(handler));
        await batch.CreateBatchPaymentAsync(null!);
        await batch.ConfirmBatchPaymentAsync(null!);
        await batch.CancelBatchPaymentAsync(null!);

        ReportService report = new(configuration, CreateHttpClient(handler));
        await report.GetPaymentsReportAsync(null!);
        await report.GetTransactionsReportAsync(null!);

        FinMonService finMon = new(configuration, CreateHttpClient(handler));
        await finMon.GetRulesAsync(1234567890);

        Assert.Contains("GET /api/merchants/v1/me", calls);
        Assert.Contains("GET /api/merchant/v1/settings", calls);
        Assert.Contains("POST /api/merchant/v1/settings", calls);
        Assert.Contains("GET /api/merchant/v1/commission-rates", calls);

        Assert.Contains("POST /api/payments/batch/v1/new", calls);
        Assert.Contains("POST /api/payments/batch/v1/confirm", calls);
        Assert.Contains("POST /api/payments/batch/v1/cancel", calls);

        Assert.Contains("POST /api/reports/v1/payments", calls);
        Assert.Contains("POST /api/reports/v1/transactions", calls);

        Assert.Contains("GET /api/finmon/v1/p2p-payment/pre-limits?recipient_ipn=1234567890", calls);
    }

    private static RozetkaPayConfiguration CreateConfiguration(RetryPolicy? retryPolicy = null, string? onBehalfOf = null, string? customerAuth = null)
    {
        return new RozetkaPayConfiguration
        {
            BaseUrl = "https://api.rozetkapay.com",
            Login = "login",
            Password = "password",
            RetryPolicy = retryPolicy ?? RetryPolicy.None,
            OnBehalfOf = onBehalfOf,
            CustomerAuth = customerAuth,
            UserAgent = "SYT.RozetkaPay.Tests"
        };
    }

    private static HttpClient CreateHttpClient(StubHttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.rozetkapay.com")
        };
    }

    private static HttpResponseMessage Json(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
}

public class BaseServiceResilienceAndErrorsTests
{
    [Fact]
    public async Task BaseService_ShouldThrowUnauthorizedException()
    {
        StubHttpMessageHandler handler = new(async (_, _) => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        PaymentService service = new(CreateConfiguration(), CreateHttpClient(handler));

        await Assert.ThrowsAsync<RozetkaPayAuthorizationException>(async () =>
            await service.GetInfoAsync("id-1"));
    }

    [Fact]
    public async Task BaseService_ShouldThrowForbiddenException()
    {
        StubHttpMessageHandler handler = new(async (_, _) => new HttpResponseMessage(HttpStatusCode.Forbidden));
        PaymentService service = new(CreateConfiguration(), CreateHttpClient(handler));

        await Assert.ThrowsAsync<RozetkaPayAuthorizationException>(async () =>
            await service.GetInfoAsync("id-2"));
    }

    [Fact]
    public async Task BaseService_ShouldThrowRateLimitException()
    {
        StubHttpMessageHandler handler = new(async (_, _) =>
        {
            HttpResponseMessage response = new(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(15));
            return response;
        });
        PaymentService service = new(CreateConfiguration(), CreateHttpClient(handler));

        RozetkaPayRateLimitException ex = await Assert.ThrowsAsync<RozetkaPayRateLimitException>(async () =>
            await service.GetInfoAsync("id-3"));

        Assert.Contains("Retry after 15 seconds", ex.Message);
    }

    [Fact]
    public async Task BaseService_ShouldThrowInternalServerErrorException()
    {
        StubHttpMessageHandler handler = new(async (_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        PaymentService service = new(CreateConfiguration(), CreateHttpClient(handler));

        RozetkaPayException ex = await Assert.ThrowsAsync<RozetkaPayException>(async () =>
            await service.GetInfoAsync("id-4"));

        Assert.Contains("Internal server error", ex.Message);
    }

    [Fact]
    public async Task BaseService_ShouldUseErrorFieldFromPayloadForDefaultStatuses()
    {
        StubHttpMessageHandler handler = new(async (_, _) => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent("""{"error":"state conflict"}""", Encoding.UTF8, "application/json")
        });
        PaymentService service = new(CreateConfiguration(), CreateHttpClient(handler));

        RozetkaPayException ex = await Assert.ThrowsAsync<RozetkaPayException>(async () =>
            await service.GetInfoAsync("id-5"));

        Assert.Contains("state conflict", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BaseService_BadRequestWithInvalidJson_ShouldReturnGenericValidationException()
    {
        StubHttpMessageHandler handler = new(async (_, _) => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("not-a-json", Encoding.UTF8, "application/json")
        });
        PaymentService service = new(CreateConfiguration(), CreateHttpClient(handler));

        RozetkaPayValidationException ex = await Assert.ThrowsAsync<RozetkaPayValidationException>(async () =>
            await service.GetInfoAsync("id-6"));

        Assert.Equal("Bad request", ex.Message);
    }

    [Fact]
    public async Task BaseService_ShouldApplyOptionalHeadersToRequests()
    {
        StubHttpMessageHandler handler = new(async (_, _) => Json("{}"));
        PaymentService service = new(
            CreateConfiguration(onBehalfOf: "merchant-child", customerAuth: "rid-token"),
            CreateHttpClient(handler),
            NullLogger.Instance);

        await service.GetInfoAsync("id-7");

        Assert.NotNull(handler.LastRequest);
        Assert.True(handler.LastRequest!.Headers.TryGetValues("X-ON-BEHALF-OF", out IEnumerable<string>? onBehalfValues));
        Assert.Equal("merchant-child", Assert.Single(onBehalfValues));
        Assert.True(handler.LastRequest.Headers.TryGetValues("X-CUSTOMER-AUTH", out IEnumerable<string>? customerAuthValues));
        Assert.Equal("rid-token", Assert.Single(customerAuthValues));
    }

    [Fact]
    public async Task BaseService_ShouldRetryOnHttpRequestExceptionWhenEnabled()
    {
        int calls = 0;
        StubHttpMessageHandler handler = new(async (_, _) =>
        {
            calls++;
            if (calls == 1)
            {
                throw new HttpRequestException("network glitch");
            }

            return Json("{}");
        });

        PaymentService service = new(CreateConfiguration(retryPolicy: RetryOnceImmediately()), CreateHttpClient(handler));
        await service.GetInfoAsync("id-8");

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task BaseService_ShouldRetryOnTaskCanceledExceptionWhenEnabled()
    {
        int calls = 0;
        StubHttpMessageHandler handler = new(async (_, _) =>
        {
            calls++;
            if (calls == 1)
            {
                throw new TaskCanceledException("timeout");
            }

            return Json("{}");
        });

        PaymentService service = new(CreateConfiguration(retryPolicy: RetryOnceImmediately()), CreateHttpClient(handler));
        await service.GetInfoAsync("id-9");

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task BaseService_ShouldRetryOnRateLimitWhenEnabled()
    {
        int calls = 0;
        StubHttpMessageHandler handler = new(async (_, _) =>
        {
            calls++;
            if (calls == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            }

            return Json("{}");
        });

        PaymentService service = new(CreateConfiguration(retryPolicy: RetryOnceImmediately()), CreateHttpClient(handler));
        await service.GetInfoAsync("id-10");

        Assert.Equal(2, calls);
    }

    private static RetryPolicy RetryOnceImmediately()
    {
        return new RetryPolicy
        {
            Enabled = true,
            MaxRetryAttempts = 1,
            BaseDelay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            BackoffStrategy = BackoffStrategy.Fixed
        };
    }

    private static RozetkaPayConfiguration CreateConfiguration(RetryPolicy? retryPolicy = null, string? onBehalfOf = null, string? customerAuth = null)
    {
        return new RozetkaPayConfiguration
        {
            BaseUrl = "https://api.rozetkapay.com",
            Login = "login",
            Password = "password",
            RetryPolicy = retryPolicy ?? RetryPolicy.None,
            OnBehalfOf = onBehalfOf,
            CustomerAuth = customerAuth,
            UserAgent = "SYT.RozetkaPay.Tests"
        };
    }

    private static HttpClient CreateHttpClient(StubHttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.rozetkapay.com")
        };
    }

    private static HttpResponseMessage Json(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
}
