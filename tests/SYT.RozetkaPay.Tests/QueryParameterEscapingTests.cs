using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Models.AlternativePayments;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Models.Payouts;
using SYT.RozetkaPay.Models.PayParts;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Query-value escaping contract (EXP-335).
///
/// Expected request targets are written as literal strings on purpose. Deriving them from
/// <see cref="Uri.EscapeDataString"/> would mirror the implementation and would not detect
/// escaping the wrong source value, escaping at the wrong insertion point, or escaping twice.
/// </summary>
public class QueryParameterEscapingTests
{
    // Raw caller input -> single-pass percent-encoded value.
    // "payment +/&=?#% Привіт" =>
    //   space '+' '/' '&' '=' '?' '#' '%' space + UTF-8 octets of "Привіт".
    private const string HostilePaymentExternalId = "payment +/&=?#% Привіт";

    private const string EncodedHostilePaymentExternalId =
        "payment%20%2B%2F%26%3D%3F%23%25%20%D0%9F%D1%80%D0%B8%D0%B2%D1%96%D1%82";

    [Fact]
    public async Task PaymentService_GetInfo_ShouldEscapeHostileExternalIdAsOneQueryValue()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PaymentService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetInfoAsync(HostilePaymentExternalId);

        Uri uri = handler.LastRequest!.RequestUri!;
        Assert.Equal(
            "/api/payments/v1/info?external_id=" + EncodedHostilePaymentExternalId,
            uri.PathAndQuery);
        Assert.Equal(string.Empty, uri.Fragment);
        Assert.Equal(new[] { "external_id" }, QueryEscapingTestContext.QueryKeys(uri));
    }

    [Fact]
    public async Task PaymentService_GetReceipt_ShouldEscapeHostileExternalIdAsOneQueryValue()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PaymentService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        // 'Ї' (U+0407) encodes to the UTF-8 octets 0xD0 0x87.
        await service.GetReceiptAsync("receipt#1?a=b&c=d+50%Ї");

        Uri uri = handler.LastRequest!.RequestUri!;
        Assert.Equal(
            "/api/payments/v1/receipt?external_id=receipt%231%3Fa%3Db%26c%3Dd%2B50%25%D0%87",
            uri.PathAndQuery);
        Assert.Equal("/api/payments/v1/receipt", uri.AbsolutePath);
        Assert.Equal(string.Empty, uri.Fragment);
        Assert.Equal(new[] { "external_id" }, QueryEscapingTestContext.QueryKeys(uri));
    }

    [Fact]
    public async Task PaymentService_GetInfo_ShouldTreatPercentLookingInputAsRawValue()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PaymentService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetInfoAsync("already%2Fencoded");

        Assert.Equal(
            "/api/payments/v1/info?external_id=already%252Fencoded",
            handler.LastRequest!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task PaymentService_GetInfo_ShouldLeaveUnreservedAsciiUnchanged()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PaymentService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetInfoAsync("pay-1");

        Assert.Equal(
            "/api/payments/v1/info?external_id=pay-1",
            handler.LastRequest!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task PaymentService_GetInfo_ShouldMatchDocumentedRequestEncodingExample()
    {
        // Pins the example in the package README ("Request Encoding") to real behavior.
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PaymentService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetInfoAsync("order 42+A&status=success");

        Assert.Equal(
            "/api/payments/v1/info?external_id=order%2042%2BA%26status%3Dsuccess",
            handler.LastRequest!.RequestUri!.PathAndQuery);
    }






    [Fact]
    public async Task PayoutService_GetInfo_ShouldEscapeHostileExternalId()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PayoutService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetInfoAsync("payout&x=1#f");

        Uri uri = handler.LastRequest!.RequestUri!;
        Assert.Equal("/api/payouts/v1/info?external_id=payout%26x%3D1%23f", uri.PathAndQuery);
        Assert.Equal(string.Empty, uri.Fragment);
        Assert.Equal(new[] { "external_id" }, QueryEscapingTestContext.QueryKeys(uri));
    }





    [Fact]
    public async Task PayoutService_GetAccountBalance_ShouldStayEncodedExactlyOnce()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PayoutService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetAccountBalanceAsync("merchant&1");

        Assert.Equal(
            "/api/payouts/v1/account-balance?merchant_entity_id=merchant%261",
            handler.LastRequest!.RequestUri!.PathAndQuery);
    }



    [Fact]
    public async Task PayPartsService_AlreadyEscapedEndpoints_ShouldStayEncodedExactlyOnce()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PayPartsService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetInfoAsync("ext&id=2");
        Assert.Equal(
            "/api/payparts/v1/info?external_id=ext%26id%3D2",
            handler.LastRequest!.RequestUri!.PathAndQuery);

        await service.GetOperationInfoAsync("ext&1", "op=2");
        Assert.Equal(
            "/api/payparts/v1/info/operation?external_id=ext%261&operation_id=op%3D2",
            handler.LastRequest!.RequestUri!.PathAndQuery);
    }



    [Fact]
    public async Task AlternativePaymentService_AlreadyEscapedEndpoints_ShouldStayEncodedExactlyOnce()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        AlternativePaymentService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetInfoAsync("alt&id=3");
        Assert.Equal(
            "/api/alternative-payments/v1/info?external_id=alt%26id%3D3",
            handler.LastRequest!.RequestUri!.PathAndQuery);

        await service.GetOperationInfoAsync("e&1", "o=2");
        Assert.Equal(
            "/api/alternative-payments/v1/info/operation?external_id=e%261&operation_id=o%3D2",
            handler.LastRequest!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task FinMonService_GetRules_ShouldSendSingleRecipientIpnQueryValue()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        FinMonService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetRulesAsync(1234567890);

        Uri uri = handler.LastRequest!.RequestUri!;
        Assert.Equal(
            "/api/finmon/v1/p2p-payment/pre-limits?recipient_ipn=1234567890",
            uri.PathAndQuery);
        Assert.Equal("/api/finmon/v1/p2p-payment/pre-limits", uri.AbsolutePath);
        Assert.Equal(new[] { "recipient_ipn" }, QueryEscapingTestContext.QueryKeys(uri));
        Assert.Equal(string.Empty, uri.Fragment);
    }

    [Fact]
    public void PublicSurface_ShouldKeepQueryCarryingSignaturesUnchanged()
    {
        Assert.Equal(
            new[] { typeof(string), typeof(CancellationToken) },
            ParameterTypes(typeof(IPaymentService), nameof(IPaymentService.GetInfoAsync)));
        Assert.Equal(
            new[] { typeof(string), typeof(CancellationToken) },
            ParameterTypes(typeof(IPaymentService), nameof(IPaymentService.GetReceiptAsync)));
        Assert.Equal(
            new[] { typeof(string), typeof(CancellationToken) },
            ParameterTypes(typeof(IPayoutService), nameof(IPayoutService.GetInfoAsync)));

        // The FinMon pre-limits parameter is an int in this SDK, matching the integer
        // recipient_ipn query parameter of the published OpenAPI document.
        Assert.Equal(
            new[] { typeof(int), typeof(CancellationToken) },
            ParameterTypes(typeof(IFinMonService), nameof(IFinMonService.GetRulesAsync)));
    }


    [Fact]
    public void PublicSurface_ShouldNotExposeEscapingOccurrenceFlags()
    {
        string[] forbidden = { "IsEncoded", "HasEscapedQuery", "WasSanitized", "DidEscape" };

        List<string> offenders = new();
        foreach (Type type in typeof(PaymentService).Assembly.GetExportedTypes())
        {
            foreach (MemberInfo member in type.GetMembers(
                         BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                if (member is MethodBase { IsFamily: false, IsPublic: false })
                {
                    continue;
                }

                if (forbidden.Contains(member.Name, StringComparer.Ordinal))
                {
                    offenders.Add($"{type.FullName}.{member.Name}");
                }
            }
        }

        Assert.Empty(offenders);
    }

    private static Type[] ParameterTypes(Type contract, string methodName)
    {
        MethodInfo method = contract.GetMethod(methodName)
                            ?? throw new InvalidOperationException($"{contract.Name}.{methodName} is missing.");
        return method.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
    }

    private static Type PropertyType<T>(string propertyName)
    {
        PropertyInfo property = typeof(T).GetProperty(propertyName)
                                ?? throw new InvalidOperationException($"{typeof(T).Name}.{propertyName} is missing.");
        return property.PropertyType;
    }
}

/// <summary>
/// Culture-sensitive cases. These mutate the ambient culture, so they run in their own
/// non-parallel collection and restore both cultures in a finally block.
/// </summary>
[Collection(QueryParameterEscapingCultureTests.CollectionName)]
public class QueryParameterEscapingCultureTests
{
    internal const string CollectionName = "QueryParameterEscapingCulture";



    [Fact]
    public async Task FinMonService_GetRules_ShouldFormatIntegerInvariantlyUnderCustomNegativeSign()
    {
        // A negative sign is the one part of int formatting a culture can change, so the
        // culture is built with an explicit non-ASCII-looking sign instead of relying on
        // whatever a given ICU version ships for a real locale.
        CultureInfo culture = (CultureInfo)CultureInfo.GetCultureInfo("en-US").Clone();
        culture.NumberFormat.NegativeSign = "!NEG!";

        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        FinMonService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await WithCultureAsync(culture, () => service.GetRulesAsync(-1234567890));

        Assert.Equal(
            "/api/finmon/v1/p2p-payment/pre-limits?recipient_ipn=-1234567890",
            handler.LastRequest!.RequestUri!.PathAndQuery);
    }


    private static async Task WithCultureAsync(CultureInfo culture, Func<Task> action)
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            await action();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}

[CollectionDefinition(QueryParameterEscapingCultureTests.CollectionName, DisableParallelization = true)]
public class QueryParameterEscapingCultureCollection
{
}

internal static class QueryEscapingTestContext
{
    internal static StubHttpMessageHandler EmptyJsonHandler()
    {
        return new StubHttpMessageHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            }));
    }

    internal static RozetkaPayConfiguration CreateConfiguration()
    {
        return new RozetkaPayConfiguration
        {
            BaseUrl = "https://api.rozetkapay.com",
            Login = "login",
            Password = "password",
            RetryPolicy = RetryPolicy.None,
            UserAgent = "SYT.RozetkaPay.Tests"
        };
    }

    internal static HttpClient CreateHttpClient(StubHttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.rozetkapay.com")
        };
    }

    internal static string[] QueryKeys(Uri uri)
    {
        string query = uri.Query;
        if (query.Length <= 1)
        {
            return Array.Empty<string>();
        }

        return query
            .Substring(1)
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2)[0])
            .ToArray();
    }
}
