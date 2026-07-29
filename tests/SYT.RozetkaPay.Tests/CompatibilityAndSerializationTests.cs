using System.Net;
using System.Text;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Models.AlternativePayments;
using SYT.RozetkaPay.Models.Customers;
using SYT.RozetkaPay.Models.PayParts;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Models.Payouts;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

public class CompatibilityAndSerializationTests
{
    [Fact]
    public async Task PayPartsService_CreateOrder_ShouldSurfaceNotFound_WithoutTryingAnotherRoute()
    {
        List<string> calls = new();
        StubHttpMessageHandler handler = new(async (request, _) =>
        {
            calls.Add(request.RequestUri!.PathAndQuery);
            if (request.RequestUri!.AbsolutePath == "/api/payparts/v1/order/create")
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("""{"message":"not found"}""", Encoding.UTF8, "application/json")
                };
            }

            return Json("""{"id":"order-1","external_id":"ext-1","status":"pending"}""");
        });

        PayPartsService service = new(CreateConfiguration(), CreateHttpClient(handler));
        await Assert.ThrowsAsync<RozetkaPayNotFoundException>(() => service.CreateOrderAsync(new CreatePayPartsOrderRequest
        {
            ExternalId = "ext-1",
            Amount = 1000m,
            Currency = "UAH",
            PartsCount = 3
        }));

        // A 404 can mean the resource is absent, not the route. Reinterpreting it as a missing
        // endpoint hid the real error and sent a second mutation to an unpublished route.
        Assert.Single(calls);
        Assert.Equal("/api/payparts/v1/order/create", calls[0]);
    }

    [Fact]
    public async Task AlternativePaymentService_Create_ShouldSurfaceNotFound_WithoutTryingAnotherRoute()
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

            return Json("""{"id":"alt-1","external_id":"ext-1","status":"success"}""");
        });

        AlternativePaymentService service = new(CreateConfiguration(), CreateHttpClient(handler));
        await Assert.ThrowsAsync<RozetkaPayNotFoundException>(() => service.CreateAsync(new CreateAlternativePaymentRequest
        {
            Amount = 25m,
            Currency = "PLN",
            ExternalId = "ext-1",
            Provider = AlternativePaymentProvider.Imoje
        }));

        // A 404 can mean the resource is absent, not the route. Reinterpreting it as a missing
        // endpoint hid the real error and sent a second mutation to an unpublished route.
        Assert.Single(calls);
        Assert.Equal("/api/alternative-payments/v1/create", calls[0]);
    }

    [Fact]
    public async Task CustomerService_GetWallet_ShouldSurfaceNotFound_WithoutTryingAnotherRoute()
    {
        List<string> calls = new();
        StubHttpMessageHandler handler = new(async (request, _) =>
        {
            calls.Add(request.RequestUri!.PathAndQuery);
            if (request.RequestUri!.AbsolutePath == "/api/customers/v1/wallet")
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("""{"message":"not found"}""", Encoding.UTF8, "application/json")
                };
            }

            return Json("""{"customer_id":"customer-1"}""");
        });

        CustomerService service = new(CreateConfiguration(), CreateHttpClient(handler));
        await Assert.ThrowsAsync<RozetkaPayNotFoundException>(
            () => service.GetCustomerWalletAsync("customer-1"));

        // A 404 can mean the resource is absent, not the route. Reinterpreting it as a missing
        // endpoint hid the real error and sent a second mutation to an unpublished route.
        Assert.Single(calls);
        Assert.Equal("/api/customers/v1/wallet?external_id=customer-1", calls[0]);
    }

    [Fact]
    public async Task PaymentService_GetList_ShouldDeserializeIntegerFieldsFromNumericStrings()
    {
        StubHttpMessageHandler handler = new(async (_, _) => Json("""
            {"payments":[],"count":"2","offset":"1"}
            """));
        PaymentService service = new(CreateConfiguration(), CreateHttpClient(handler));

        PaymentListResponse response = await service.GetListAsync(new PaymentListRequest());

        Assert.Equal(2, response.Count);
        Assert.Equal(1, response.Offset);
    }

    [Fact]
    public async Task PayoutService_RequestPayout_ShouldDeserializeIntegerFieldsFromNumericStrings()
    {
        StubHttpMessageHandler handler = new(async (_, _) => Json("""
            {"external_id":"po-1","fc_id":"123","original_amount":"10.00","payer_amount":"10.00"}
            """));
        PayoutService service = new(CreateConfiguration(), CreateHttpClient(handler));

        PayoutTransactionResult response = await service.RequestPayoutAsync(new RequestPayoutRequest());

        Assert.Equal("po-1", response.ExternalId);
        Assert.Equal(123, response.FcId);
        Assert.Equal(10.00m, response.OriginalAmount);
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

    private static HttpResponseMessage Json(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
}
