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
    public async Task PaymentService_GetList_ShouldKeepHostileStatusAsOneQueryValue()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PaymentService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetListAsync(new PaymentListRequest
        {
            DateFrom = new DateTime(2026, 2, 28),
            DateTo = new DateTime(2026, 3, 1),
            Status = "success&offset=999#fragment +",
            Limit = 10,
            Offset = 5
        });

        Uri uri = handler.LastRequest!.RequestUri!;
        Assert.Equal(
            "/api/payments/v1/list?date_from=2026-02-28&date_to=2026-03-01"
            + "&status=success%26offset%3D999%23fragment%20%2B&limit=10&offset=5",
            uri.PathAndQuery);
        Assert.Equal(string.Empty, uri.Fragment);
        Assert.Equal(
            new[] { "date_from", "date_to", "status", "limit", "offset" },
            QueryEscapingTestContext.QueryKeys(uri));
        Assert.Single(QueryEscapingTestContext.QueryKeys(uri), key => key == "offset");
    }

    [Fact]
    public async Task PaymentService_GetList_ShouldKeepOrdinaryAsciiFiltersUnchanged()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PaymentService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetListAsync(new PaymentListRequest
        {
            DateFrom = new DateTime(2026, 2, 28),
            DateTo = new DateTime(2026, 3, 1),
            Status = "success",
            Limit = 10,
            Offset = 5
        });

        Assert.Equal(
            "/api/payments/v1/list?date_from=2026-02-28&date_to=2026-03-01&status=success&limit=10&offset=5",
            handler.LastRequest!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task PaymentService_GetList_ShouldOmitQueryWhenNoFilterIsSet()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PaymentService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetListAsync(new PaymentListRequest());

        Uri uri = handler.LastRequest!.RequestUri!;
        Assert.Equal("/api/payments/v1/list", uri.PathAndQuery);
        Assert.DoesNotContain("?", uri.PathAndQuery, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task PaymentService_GetList_ShouldOmitNullOrEmptyStatus(string? status)
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PaymentService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetListAsync(new PaymentListRequest { Status = status, Limit = 1 });

        Assert.Equal(
            "/api/payments/v1/list?limit=1",
            handler.LastRequest!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task PaymentService_GetList_ShouldKeepWhitespaceStatusAsEncodedValue()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PaymentService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetListAsync(new PaymentListRequest { Status = "   " });

        Assert.Equal(
            "/api/payments/v1/list?status=%20%20%20",
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
    public async Task PayoutService_GetList_ShouldEscapeHostileDateFrom()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PayoutService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetListAsync(new PayoutListRequest
        {
            DateFrom = "2026-01-01&status=hijacked",
            DateTo = "2026-02-28",
            Status = "success",
            Limit = 10,
            Offset = 1
        });

        Uri uri = handler.LastRequest!.RequestUri!;
        Assert.Equal(
            "/api/payouts/v1/list?date_from=2026-01-01%26status%3Dhijacked"
            + "&date_to=2026-02-28&status=success&limit=10&offset=1",
            uri.PathAndQuery);
        Assert.Equal(
            new[] { "date_from", "date_to", "status", "limit", "offset" },
            QueryEscapingTestContext.QueryKeys(uri));
        Assert.Single(QueryEscapingTestContext.QueryKeys(uri), key => key == "status");
    }

    [Fact]
    public async Task PayoutService_GetList_ShouldEscapeHostileDateTo()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PayoutService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetListAsync(new PayoutListRequest
        {
            DateFrom = "2026-01-01",
            DateTo = "2026-02-28#frag?y=2"
        });

        Uri uri = handler.LastRequest!.RequestUri!;
        Assert.Equal(
            "/api/payouts/v1/list?date_from=2026-01-01&date_to=2026-02-28%23frag%3Fy%3D2",
            uri.PathAndQuery);
        Assert.Equal(string.Empty, uri.Fragment);
    }

    [Fact]
    public async Task PayoutService_GetList_ShouldEscapeHostileStatus()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PayoutService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetListAsync(new PayoutListRequest { Status = "pending/failed?x=1", Limit = 5 });

        Assert.Equal(
            "/api/payouts/v1/list?status=pending%2Ffailed%3Fx%3D1&limit=5",
            handler.LastRequest!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task PayoutService_GetList_ShouldKeepOrdinaryValuesAndOmissionsUnchanged()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PayoutService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetListAsync(new PayoutListRequest
        {
            DateFrom = "2026-02-01",
            DateTo = "2026-02-28",
            Status = "success",
            Limit = 10,
            Offset = 1
        });
        Assert.Equal(
            "/api/payouts/v1/list?date_from=2026-02-01&date_to=2026-02-28&status=success&limit=10&offset=1",
            handler.LastRequest!.RequestUri!.PathAndQuery);

        await service.GetListAsync(new PayoutListRequest { DateFrom = string.Empty, Status = string.Empty });
        Assert.Equal("/api/payouts/v1/list", handler.LastRequest!.RequestUri!.PathAndQuery);

        await service.GetListAsync(new PayoutListRequest());
        Assert.Equal("/api/payouts/v1/list", handler.LastRequest!.RequestUri!.PathAndQuery);
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
    public async Task PayPartsService_GetOperations_ShouldEscapeHostileStatus()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PayPartsService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetOperationsAsync(new PayPartsOperationsListRequest
        {
            DateFrom = new DateTime(2026, 2, 28),
            DateTo = new DateTime(2026, 3, 1),
            Status = "pending#x&limit=1",
            Limit = 50,
            Offset = 2
        });

        Uri uri = handler.LastRequest!.RequestUri!;
        Assert.Equal(
            "/api/payparts/v1/operations?date_from=2026-02-28&date_to=2026-03-01"
            + "&status=pending%23x%26limit%3D1&limit=50&offset=2",
            uri.PathAndQuery);
        Assert.Equal(string.Empty, uri.Fragment);
        Assert.Single(QueryEscapingTestContext.QueryKeys(uri), key => key == "limit");
    }

    [Fact]
    public async Task PayPartsService_GetOperations_ShouldKeepOrdinaryValuesAndEmptyQueryUnchanged()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PayPartsService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetOperationsAsync(new PayPartsOperationsListRequest
        {
            DateFrom = new DateTime(2026, 2, 28),
            DateTo = new DateTime(2026, 3, 1),
            Status = "pending",
            Limit = 50,
            Offset = 2
        });
        Assert.Equal(
            "/api/payparts/v1/operations?date_from=2026-02-28&date_to=2026-03-01&status=pending&limit=50&offset=2",
            handler.LastRequest!.RequestUri!.PathAndQuery);

        await service.GetOperationsAsync(new PayPartsOperationsListRequest());
        Assert.Equal("/api/payparts/v1/operations", handler.LastRequest!.RequestUri!.PathAndQuery);
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
    public async Task AlternativePaymentService_GetOperations_ShouldEscapeRawStringDatesWithoutReformatting()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        AlternativePaymentService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetOperationsAsync(new GetAlternativePaymentOperationsRequest
        {
            DateFrom = "2026-01-01T00:00:00&x=1",
            DateTo = "2026-01-31",
            Status = "success?a=b",
            Limit = 20,
            Offset = 3
        });

        Uri uri = handler.LastRequest!.RequestUri!;
        Assert.Equal(
            "/api/alternative-payments/v1/operations?date_from=2026-01-01T00%3A00%3A00%26x%3D1"
            + "&date_to=2026-01-31&status=success%3Fa%3Db&limit=20&offset=3",
            uri.PathAndQuery);
        Assert.Equal(
            new[] { "date_from", "date_to", "status", "limit", "offset" },
            QueryEscapingTestContext.QueryKeys(uri));
    }

    [Fact]
    public async Task AlternativePaymentService_GetOperations_ShouldKeepOrdinaryValuesAndEmptyQueryUnchanged()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        AlternativePaymentService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await service.GetOperationsAsync(new GetAlternativePaymentOperationsRequest
        {
            DateFrom = "2026-02-28",
            DateTo = "2026-03-01",
            Status = "success",
            Limit = 20,
            Offset = 3
        });
        Assert.Equal(
            "/api/alternative-payments/v1/operations?date_from=2026-02-28&date_to=2026-03-01"
            + "&status=success&limit=20&offset=3",
            handler.LastRequest!.RequestUri!.PathAndQuery);

        await service.GetOperationsAsync(new GetAlternativePaymentOperationsRequest());
        Assert.Equal(
            "/api/alternative-payments/v1/operations",
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
            new[] { typeof(PaymentListRequest), typeof(CancellationToken) },
            ParameterTypes(typeof(IPaymentService), nameof(IPaymentService.GetListAsync)));
        Assert.Equal(
            new[] { typeof(string), typeof(CancellationToken) },
            ParameterTypes(typeof(IPayoutService), nameof(IPayoutService.GetInfoAsync)));
        Assert.Equal(
            new[] { typeof(PayoutListRequest), typeof(CancellationToken) },
            ParameterTypes(typeof(IPayoutService), nameof(IPayoutService.GetListAsync)));
        Assert.Equal(
            new[] { typeof(PayPartsOperationsListRequest), typeof(CancellationToken) },
            ParameterTypes(typeof(IPayPartsService), nameof(IPayPartsService.GetOperationsAsync)));
        Assert.Equal(
            new[] { typeof(GetAlternativePaymentOperationsRequest), typeof(CancellationToken) },
            ParameterTypes(
                typeof(IAlternativePaymentService),
                nameof(IAlternativePaymentService.GetOperationsAsync)));

        // The FinMon pre-limits parameter is an int in this SDK, matching the integer
        // recipient_ipn query parameter of the published OpenAPI document.
        Assert.Equal(
            new[] { typeof(int), typeof(CancellationToken) },
            ParameterTypes(typeof(IFinMonService), nameof(IFinMonService.GetRulesAsync)));
    }

    [Fact]
    public void PublicSurface_ShouldKeepListRequestPropertyTypesUnchanged()
    {
        Assert.Equal(typeof(DateTime?), PropertyType<PaymentListRequest>(nameof(PaymentListRequest.DateFrom)));
        Assert.Equal(typeof(DateTime?), PropertyType<PaymentListRequest>(nameof(PaymentListRequest.DateTo)));
        Assert.Equal(typeof(string), PropertyType<PaymentListRequest>(nameof(PaymentListRequest.Status)));
        Assert.Equal(typeof(int?), PropertyType<PaymentListRequest>(nameof(PaymentListRequest.Limit)));
        Assert.Equal(typeof(int?), PropertyType<PaymentListRequest>(nameof(PaymentListRequest.Offset)));

        Assert.Equal(typeof(string), PropertyType<PayoutListRequest>(nameof(PayoutListRequest.DateFrom)));
        Assert.Equal(typeof(string), PropertyType<PayoutListRequest>(nameof(PayoutListRequest.DateTo)));
        Assert.Equal(
            typeof(string),
            PropertyType<GetAlternativePaymentOperationsRequest>(
                nameof(GetAlternativePaymentOperationsRequest.DateFrom)));
        Assert.Equal(
            typeof(DateTime?),
            PropertyType<PayPartsOperationsListRequest>(nameof(PayPartsOperationsListRequest.DateFrom)));
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
    public async Task PaymentService_GetList_ShouldFormatDatesInvariantlyUnderNonGregorianCulture()
    {
        // th-TH resolves to the Thai Buddhist calendar, so a culture-dependent "yyyy-MM-dd"
        // would render 2026-02-28 as 2569-02-28.
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PaymentService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await WithCultureAsync(
            new CultureInfo("th-TH"),
            () => service.GetListAsync(new PaymentListRequest
            {
                DateFrom = new DateTime(2026, 2, 28),
                DateTo = new DateTime(2026, 3, 1),
                Limit = 10,
                Offset = 5
            }));

        Assert.Equal(
            "/api/payments/v1/list?date_from=2026-02-28&date_to=2026-03-01&limit=10&offset=5",
            handler.LastRequest!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task PayPartsService_GetOperations_ShouldFormatDatesInvariantlyUnderNonGregorianCulture()
    {
        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PayPartsService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await WithCultureAsync(
            new CultureInfo("th-TH"),
            () => service.GetOperationsAsync(new PayPartsOperationsListRequest
            {
                DateFrom = new DateTime(2026, 2, 28),
                DateTo = new DateTime(2026, 3, 1),
                Limit = 50
            }));

        Assert.Equal(
            "/api/payparts/v1/operations?date_from=2026-02-28&date_to=2026-03-01&limit=50",
            handler.LastRequest!.RequestUri!.PathAndQuery);
    }

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

    [Fact]
    public async Task PaymentService_GetList_ShouldFormatPaginationInvariantlyUnderCustomNegativeSign()
    {
        CultureInfo culture = (CultureInfo)CultureInfo.GetCultureInfo("en-US").Clone();
        culture.NumberFormat.NegativeSign = "!NEG!";

        StubHttpMessageHandler handler = QueryEscapingTestContext.EmptyJsonHandler();
        PaymentService service = new(
            QueryEscapingTestContext.CreateConfiguration(),
            QueryEscapingTestContext.CreateHttpClient(handler));

        await WithCultureAsync(
            culture,
            () => service.GetListAsync(new PaymentListRequest { Limit = 10, Offset = -5 }));

        Assert.Equal(
            "/api/payments/v1/list?limit=10&offset=-5",
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
