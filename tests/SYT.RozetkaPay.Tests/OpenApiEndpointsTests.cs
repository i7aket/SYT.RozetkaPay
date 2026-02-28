using System.Net;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Models.AlternativePayments;
using SYT.RozetkaPay.Models.PayParts;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Models.Subscriptions;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

public class OpenApiEndpointsTests
{
    [Fact]
    public async Task PaymentService_RetryRefund_ShouldCallExpectedEndpoint()
    {
        StubHttpMessageHandler handler = new(async (_, _) => CreateJsonResponse("""
            {"external_id":"payment-1","is_success":true,"action_required":false}
            """));
        PaymentService service = new(CreateConfiguration(), CreateHttpClient(handler));

        PaymentOperationResult response = await service.RetryRefundAsync(new RetryRefundRequest { ExternalId = "payment-1" });

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/api/payments/v1/refund/retry", handler.LastRequest.RequestUri!.PathAndQuery);
        Assert.Contains(@"""external_id"":""payment-1""", handler.LastRequestBody);
        Assert.True(response.IsSuccess);
        Assert.Equal("payment-1", response.ExternalId);
    }

    [Fact]
    public async Task PaymentService_CancelRefund_ShouldCallExpectedEndpoint()
    {
        StubHttpMessageHandler handler = new(async (_, _) => CreateJsonResponse("""
            {"external_id":"payment-2","is_success":true,"action_required":false}
            """));
        PaymentService service = new(CreateConfiguration(), CreateHttpClient(handler));

        PaymentOperationResult response = await service.CancelRefundAsync(new CancelRefundRequest { ExternalId = "payment-2" });

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/api/payments/v1/refund/cancel", handler.LastRequest.RequestUri!.PathAndQuery);
        Assert.Contains(@"""external_id"":""payment-2""", handler.LastRequestBody);
        Assert.True(response.IsSuccess);
        Assert.Equal("payment-2", response.ExternalId);
    }

    [Fact]
    public async Task PayPartsService_RetryRefund_ShouldCallExpectedEndpoint()
    {
        StubHttpMessageHandler handler = new(async (_, _) => CreateJsonResponse("""
            {"external_id":"payparts-1","is_success":true,"action_required":false}
            """));
        PayPartsService service = new(CreateConfiguration(), CreateHttpClient(handler));

        PayPartsOperationResult response = await service.RetryRefundAsync(new RetryRefundPPayRequest { ExternalId = "payparts-1" });

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/api/payparts/v1/refund/retry", handler.LastRequest.RequestUri!.PathAndQuery);
        Assert.Contains(@"""external_id"":""payparts-1""", handler.LastRequestBody);
        Assert.True(response.IsSuccess);
        Assert.Equal("payparts-1", response.ExternalId);
    }

    [Fact]
    public async Task PayPartsService_CancelRefund_ShouldCallExpectedEndpoint()
    {
        StubHttpMessageHandler handler = new(async (_, _) => CreateJsonResponse("""
            {"external_id":"payparts-2","is_success":true,"action_required":false}
            """));
        PayPartsService service = new(CreateConfiguration(), CreateHttpClient(handler));

        PayPartsOperationResult response = await service.CancelRefundAsync(new CancelRefundPPayRequest { ExternalId = "payparts-2" });

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/api/payparts/v1/refund/cancel", handler.LastRequest.RequestUri!.PathAndQuery);
        Assert.Contains(@"""external_id"":""payparts-2""", handler.LastRequestBody);
        Assert.True(response.IsSuccess);
        Assert.Equal("payparts-2", response.ExternalId);
    }

    [Fact]
    public async Task AlternativePaymentService_ResendCallback_ShouldHandleNoContent()
    {
        StubHttpMessageHandler handler = new(async (_, _) => new HttpResponseMessage(HttpStatusCode.NoContent));
        AlternativePaymentService service = new(CreateConfiguration(), CreateHttpClient(handler));

        AlternativePaymentCallbackResendResponse response = await service.ResendCallbackAsync(
            new ResendAlternativePaymentCallbackRequest
            {
                ExternalId = "alt-1",
                Operation = AlternativePaymentCallbackOperation.Refund
            });

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/api/alternative-payments/v1/callback/resend", handler.LastRequest.RequestUri!.PathAndQuery);
        Assert.Contains(@"""external_id"":""alt-1""", handler.LastRequestBody);
        Assert.Contains(@"""operation"":""refund""", handler.LastRequestBody);
        Assert.Equal("success", response.Status);
    }

    [Fact]
    public async Task SubscriptionService_Gift_ShouldCallExpectedEndpoint()
    {
        StubHttpMessageHandler handler = new(async (_, _) => CreateJsonResponse("""
            {"payment":{"id":"payment-1"},"subscription":{"id":"subscription-1"}}
            """));
        SubscriptionService service = new(CreateConfiguration(), CreateHttpClient(handler));

        CreateSubscriptionResponse response = await service.GiftAsync(new GiftSubscriptionRequest
        {
            CallbackUrl = "https://merchant.example/callback",
            Customer = new SubscriptionCustomer { Email = "customer@example.com" },
            PlanId = "plan-1",
            ResultUrl = "https://merchant.example/result",
            StartDate = "2026-02-28T12:00:00Z",
            RecurrentId = "593292035525113984",
            GiftedPeriods = 2,
            GiftedUnifiedExternalId = "gifted-uid-1"
        });

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/api/subscriptions/v1/subscriptions/gift", handler.LastRequest.RequestUri!.PathAndQuery);
        Assert.Contains(@"""plan_id"":""plan-1""", handler.LastRequestBody);
        Assert.Contains(@"""recurrent_id"":""593292035525113984""", handler.LastRequestBody);
        Assert.Contains(@"""gifted_periods"":2", handler.LastRequestBody);
        Assert.Equal("payment-1", response.Payment?.Id);
        Assert.Equal("subscription-1", response.Subscription?.Id);
    }

    private static RozetkaPayConfiguration CreateConfiguration()
    {
        return new RozetkaPayConfiguration
        {
            BaseUrl = "https://api.rozetkapay.com",
            Login = "login",
            Password = "password",
            RetryPolicy = RetryPolicy.None
        };
    }

    private static HttpClient CreateHttpClient(StubHttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.rozetkapay.com")
        };
    }

    private static HttpResponseMessage CreateJsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }
}
