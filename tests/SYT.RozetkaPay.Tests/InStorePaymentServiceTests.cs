using System.Net;
using System.Text.Json;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Models.InStorePayments;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Wire-level coverage of the four official in-store (POS) operations added by EXP-354.
///
/// The info operation is the interesting one: the official document declares it as a POST that carries no
/// request body. Sending <c>{}</c>, or downgrading it to a GET, would both be a different operation, so
/// both are asserted against explicitly.
///
/// Amounts are strings in the smallest monetary unit. The tests use values with leading zeros and
/// trailing zeros so that any decimal round-trip would show up as changed wire text.
/// </summary>
public class InStorePaymentServiceTests
{
    private const string CreateEndpoint = "/api/in-store-payments/v1/create";

    private const string ConfirmEndpoint = "/api/in-store-payments/v1/confirm";

    private const string RefundEndpoint = "/api/in-store-payments/v1/refund";

    private const string InfoEndpoint = "/api/in-store-payments/v1/info";

    /// <summary>
    /// Synthetic cardholder-shaped values. They are obviously not real card data — no value passes a Luhn
    /// check — so a secret scanner has nothing to flag, while a leak assertion still has a unique marker.
    /// </summary>
    private const string SyntheticCardNumber = "0000-not-a-real-card-number-EXP354";

    private const string SyntheticTrack2 = "not-real-track2-data-EXP354";

    public static TheoryData<HttpStatusCode, Type> ErrorMappings =>
        new()
        {
            { HttpStatusCode.BadRequest, typeof(RozetkaPayValidationException) },
            { HttpStatusCode.Unauthorized, typeof(RozetkaPayAuthorizationException) },
            { HttpStatusCode.Forbidden, typeof(RozetkaPayAuthorizationException) },
            { HttpStatusCode.InternalServerError, typeof(RozetkaPayException) }
        };

    // ===================== create =====================

    [Fact]
    public async Task Create_ShouldPostTheExactOfficialBody()
    {
        RecordingHandler handler = RecordingHandler.Json("""{"fc_id":"fc-1"}""");

        await Exp354TestContext.InStorePayments(handler).CreateAsync(new InStorePaymentCreateRequest
        {
            ExternalId = "payment-1",
            PosTerminalId = "pos-1",
            TerminalSn = "sn-1",
            Amount = "010050",
            Currency = InStorePaymentCurrency.Uah
        });

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, recorded.Method);
        Assert.Equal(CreateEndpoint, recorded.RequestUri.PathAndQuery);
        Assert.Equal(Exp354TestContext.JsonContentType, recorded.ContentType);

        // Required fields only: every optional one is omitted, and the amount keeps its exact text.
        Assert.Equal(
            """{"external_id":"payment-1","pos_terminal_id":"pos-1","terminal_sn":"sn-1","amount":"010050","currency":"980"}""",
            recorded.Body);
    }

    /// <summary>
    /// The currency enum has to reach the wire as the literal string <c>"980"</c>. No naming policy could
    /// produce that from a C# identifier, so the token is pinned by attribute and asserted here.
    /// </summary>
    [Fact]
    public async Task Create_ShouldSerializeCurrencyAsTheNumericIsoToken()
    {
        RecordingHandler handler = RecordingHandler.Json("""{"fc_id":"fc-1"}""");

        await Exp354TestContext.InStorePayments(handler).CreateAsync(MinimalCreateRequest());

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Contains("\"currency\":\"980\"", recorded.Body!);

        // Neither the C# identifier nor a snake-cased form of it may reach the wire.
        Assert.DoesNotContain("uah", recorded.Body!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_ShouldSendEveryOptionalFieldWhenSupplied()
    {
        RecordingHandler handler = RecordingHandler.Json("""{"fc_id":"fc-1"}""");

        await Exp354TestContext.InStorePayments(handler).CreateAsync(new InStorePaymentCreateRequest
        {
            ExternalId = "payment-1",
            PosTerminalId = "pos-1",
            TerminalSn = "sn-1",
            Amount = "10050",
            Currency = InStorePaymentCurrency.Uah,
            Stan = "000123",
            BatchId = "batch-1",
            MerchantId = "merchant-1",
            OrderId = "order-1"
        });

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal(
            """{"external_id":"payment-1","pos_terminal_id":"pos-1","terminal_sn":"sn-1","amount":"10050","currency":"980","stan":"000123","batch_id":"batch-1","merchant_id":"merchant-1","order_id":"order-1"}""",
            recorded.Body);
    }

    /// <summary>
    /// An empty string is not null: the provider owns non-empty validation, so an explicitly empty value
    /// must reach the wire rather than being silently dropped.
    /// </summary>
    [Fact]
    public async Task Create_ShouldSendEmptyOptionalValuesAndOmitOnlyNulls()
    {
        RecordingHandler handler = RecordingHandler.Json("""{"fc_id":"fc-1"}""");

        await Exp354TestContext.InStorePayments(handler).CreateAsync(new InStorePaymentCreateRequest
        {
            ExternalId = "payment-1",
            PosTerminalId = "pos-1",
            TerminalSn = "sn-1",
            Amount = "10050",
            Currency = InStorePaymentCurrency.Uah,
            Stan = string.Empty,
            BatchId = null
        });

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Contains("\"stan\":\"\"", recorded.Body!);
        Assert.DoesNotContain("batch_id", recorded.Body!);
    }

    [Fact]
    public async Task Create_ShouldMapTheTypedResponseAndReceipt()
    {
        RecordingHandler handler = RecordingHandler.Json("""
            {
              "fc_id": "fc-1",
              "external_id": "payment-1",
              "created_at": "2026-07-25T10:11:12Z",
              "transaction_status": "pending",
              "transaction_status_code": "0",
              "amount": "010050",
              "order_id": "order-1",
              "receipt_data": {
                "payment_instruction_date": "2026-07-25",
                "merchant_name": "Merchant",
                "edrpou_ipn": "12345678",
                "iban": "UA000000000000000000000000000",
                "fc_name": "FinCompany",
                "amount": "010050",
                "fee_amount": "000050",
                "description": "Receipt",
                "license": "licence-1",
                "address": "Kyiv",
                "id_nbu": "nbu-1"
              }
            }
            """);

        InStorePaymentCreateResponse response = await Exp354TestContext.InStorePayments(handler)
            .CreateAsync(MinimalCreateRequest());

        Assert.Equal("fc-1", response.FcId);
        Assert.Equal("payment-1", response.ExternalId);
        Assert.Equal(new DateTime(2026, 7, 25, 10, 11, 12, DateTimeKind.Utc), response.CreatedAt!.Value.ToUniversalTime());
        Assert.Equal("pending", response.TransactionStatus);
        Assert.Equal("0", response.TransactionStatusCode);

        // The amount keeps its leading zero: a decimal round-trip would have produced "10050".
        Assert.Equal("010050", response.Amount);
        Assert.Equal("order-1", response.OrderId);

        Assert.NotNull(response.ReceiptData);
        Assert.Equal("2026-07-25", response.ReceiptData!.PaymentInstructionDate);
        Assert.Equal("Merchant", response.ReceiptData.MerchantName);
        Assert.Equal("12345678", response.ReceiptData.EdrpouIpn);
        Assert.Equal("FinCompany", response.ReceiptData.FcName);
        Assert.Equal("000050", response.ReceiptData.FeeAmount);
        Assert.Equal("licence-1", response.ReceiptData.License);
        Assert.Equal("Kyiv", response.ReceiptData.Address);
        Assert.Equal("nbu-1", response.ReceiptData.IdNbu);
    }

    // ===================== confirm =====================

    [Fact]
    public async Task Confirm_ShouldPostTheExactOfficialBody()
    {
        RecordingHandler handler = RecordingHandler.Json("""{"fc_id":"fc-1"}""");

        await Exp354TestContext.InStorePayments(handler).ConfirmAsync(new InStorePaymentConfirmRequest
        {
            ExternalId = "payment-1",
            PosTerminalId = "pos-1",
            Amount = "10050",
            PosPaymentStatus = "approved"
        });

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, recorded.Method);
        Assert.Equal(ConfirmEndpoint, recorded.RequestUri.PathAndQuery);
        Assert.Equal(
            """{"external_id":"payment-1","pos_terminal_id":"pos-1","amount":"10050","pos_payment_status":"approved"}""",
            recorded.Body);
    }

    [Fact]
    public async Task Confirm_ShouldMapTheTypedResponseAndConfirmReceipt()
    {
        RecordingHandler handler = RecordingHandler.Json("""
            {
              "fc_id": "fc-1",
              "finalised_at": "2026-07-25 10:11:12",
              "external_id": "payment-1",
              "transaction_status": "success",
              "transaction_status_code": "0",
              "amount": "10050",
              "order_id": "order-1",
              "fiscal_receipt": "receipt-1",
              "receipt_data": {
                "payment_instruction_id": "pi-1",
                "payment_system": "visa",
                "payment_instruction_date": "2026-07-25",
                "bank_acquirer": "Bank",
                "bank_edrpou": "87654321",
                "pos_terminal_id": "pos-1",
                "merchant_name": "Merchant",
                "edrpou_ipn": "12345678",
                "iban": "UA000000000000000000000000000",
                "address_sale_point": "Kyiv",
                "rrn": "rrn-1",
                "fc_id": "fc-1",
                "fc_name": "FinCompany",
                "description": "Receipt"
              }
            }
            """);

        InStorePaymentConfirmResponse response = await Exp354TestContext.InStorePayments(handler)
            .ConfirmAsync(MinimalConfirmRequest());

        // finalised_at is declared as a bare string, so it stays text rather than being parsed.
        Assert.Equal("2026-07-25 10:11:12", response.FinalisedAt);
        Assert.Equal("success", response.TransactionStatus);
        Assert.Equal("receipt-1", response.FiscalReceipt);

        Assert.NotNull(response.ReceiptData);
        Assert.Equal("pi-1", response.ReceiptData!.PaymentInstructionId);
        Assert.Equal("visa", response.ReceiptData.PaymentSystem);
        Assert.Equal("Bank", response.ReceiptData.BankAcquirer);
        Assert.Equal("87654321", response.ReceiptData.BankEdrpou);
        Assert.Equal("Kyiv", response.ReceiptData.AddressSalePoint);
        Assert.Equal("rrn-1", response.ReceiptData.Rrn);
        Assert.Equal("FinCompany", response.ReceiptData.FcName);
    }

    // ===================== refund =====================

    [Fact]
    public async Task Refund_ShouldPostTheExactOfficialBody()
    {
        RecordingHandler handler = RecordingHandler.Json("""{"fc_id":"fc-1"}""");

        await Exp354TestContext.InStorePayments(handler).RefundAsync(new InStorePaymentRefundRequest
        {
            PaymentExternalId = "payment-1",
            RefundExternalId = "refund-1",
            TerminalSn = "sn-1",
            Amount = "010050",
            PaymentSystem = "visa",
            PosTerminalId = "pos-1",
            CardNumber = SyntheticCardNumber,
            BankAcquirer = "Bank",
            AuthorizationCode = "auth-1"
        });

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, recorded.Method);
        Assert.Equal(RefundEndpoint, recorded.RequestUri.PathAndQuery);
        Assert.Equal(
            $$"""
              {"payment_external_id":"payment-1","refund_external_id":"refund-1","terminal_sn":"sn-1","amount":"010050","payment_system":"visa","pos_terminal_id":"pos-1","card_number":"{{SyntheticCardNumber}}","bank_acquirer":"Bank","authorization_code":"auth-1"}
              """,
            recorded.Body);
    }

    /// <summary>
    /// The official refund receipt declares no <c>fc_name</c>. Modelling one type for all three receipts
    /// would have made that field appear on an operation that never returns it.
    /// </summary>
    [Fact]
    public void Refund_ReceiptShape_ShouldNotOfferConfirmOnlyFields()
    {
        Assert.Null(typeof(InStorePaymentRefundReceiptData).GetProperty("FcName"));
        Assert.NotNull(typeof(InStorePaymentConfirmReceiptData).GetProperty("FcName"));

        // The create receipt is a third, genuinely different shape.
        Assert.NotNull(typeof(InStorePaymentCreateReceiptData).GetProperty("License"));
        Assert.Null(typeof(InStorePaymentConfirmReceiptData).GetProperty("License"));
        Assert.Null(typeof(InStorePaymentRefundReceiptData).GetProperty("License"));
    }

    [Fact]
    public async Task Refund_ShouldMapTheTypedResponseAndRefundReceipt()
    {
        RecordingHandler handler = RecordingHandler.Json("""
            {
              "fc_id": "fc-2",
              "finalised_at": "2026-07-25 11:00:00",
              "external_id": "refund-1",
              "transaction_status": "refunded",
              "transaction_status_code": "0",
              "amount": "010050",
              "order_id": "order-1",
              "receipt_data": {
                "payment_instruction_id": "pi-2",
                "payment_system": "visa",
                "bank_acquirer": "Bank",
                "rrn": "rrn-2",
                "fc_id": "fc-2",
                "description": "Refund receipt"
              }
            }
            """);

        InStorePaymentRefundResponse response = await Exp354TestContext.InStorePayments(handler)
            .RefundAsync(MinimalRefundRequest());

        Assert.Equal("fc-2", response.FcId);
        Assert.Equal("refund-1", response.ExternalId);
        Assert.Equal("refunded", response.TransactionStatus);
        Assert.Equal("010050", response.Amount);
        Assert.NotNull(response.ReceiptData);
        Assert.Equal("pi-2", response.ReceiptData!.PaymentInstructionId);
        Assert.Equal("rrn-2", response.ReceiptData.Rrn);
        Assert.Equal("Refund receipt", response.ReceiptData.Description);
    }

    // ===================== info =====================

    /// <summary>
    /// The heart of the operation: a POST, with the identifier in the query, and no request content at
    /// all.
    /// </summary>
    [Fact]
    public async Task GetInfo_ShouldSendABodylessPost()
    {
        RecordingHandler handler = RecordingHandler.Json("""{"fc_id":"fc-1"}""");

        await Exp354TestContext.InStorePayments(handler).GetInfoAsync("payment-1");

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, recorded.Method);
        Assert.Equal($"{InfoEndpoint}?external_id=payment-1", recorded.RequestUri.PathAndQuery);

        // No content object at all - not an empty string, and certainly not "{}".
        Assert.False(recorded.HasContent);
        Assert.Null(recorded.Body);
        Assert.Null(recorded.ContentType);
    }

    [Fact]
    public async Task GetInfo_ShouldNotBeDowngradedToGet()
    {
        RecordingHandler handler = RecordingHandler.Json("""{"fc_id":"fc-1"}""");

        await Exp354TestContext.InStorePayments(handler).GetInfoAsync("payment-1");

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.NotEqual(HttpMethod.Get, recorded.Method);
    }

    [Theory]
    [InlineData(Exp354TestContext.HostileRawId, Exp354TestContext.HostileEncodedId)]
    [InlineData(Exp354TestContext.LooksEncodedRawId, Exp354TestContext.LooksEncodedExpectedId)]
    [InlineData("plain-id", "plain-id")]
    [InlineData("", "")]
    public async Task GetInfo_ShouldEscapeTheExternalIdExactlyOnce(string rawExternalId, string expectedValue)
    {
        RecordingHandler handler = RecordingHandler.Json("""{"fc_id":"fc-1"}""");

        await Exp354TestContext.InStorePayments(handler).GetInfoAsync(rawExternalId);

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Equal($"{InfoEndpoint}?external_id={expectedValue}", recorded.RequestUri.PathAndQuery);
        Assert.Equal(InfoEndpoint, recorded.RequestUri.AbsolutePath);

        // Everything stays inside one query value: no extra parameter, no fragment.
        Assert.Equal($"?external_id={expectedValue}", recorded.RequestUri.Query);
        Assert.Equal(string.Empty, recorded.RequestUri.Fragment);
    }

    [Fact]
    public async Task GetInfo_ShouldMapTheTypedResponse()
    {
        RecordingHandler handler = RecordingHandler.Json("""
            {"fc_id":"fc-1","created_at":"2026-07-25T10:11:12Z","transaction_status":"success","transaction_status_code":"0"}
            """);

        InStorePaymentInfoResponse response = await Exp354TestContext.InStorePayments(handler)
            .GetInfoAsync("payment-1");

        Assert.Equal("fc-1", response.FcId);
        Assert.Equal(
            new DateTime(2026, 7, 25, 10, 11, 12, DateTimeKind.Utc),
            response.CreatedAt!.Value.ToUniversalTime());
        Assert.Equal("success", response.TransactionStatus);
        Assert.Equal("0", response.TransactionStatusCode);
    }

    // ===================== cross-cutting =====================

    [Fact]
    public async Task EveryOperation_ShouldRejectNullArguments()
    {
        RecordingHandler handler = RecordingHandler.Json("{}");
        InStorePaymentService service = Exp354TestContext.InStorePayments(handler);

        Assert.Equal(
            "request",
            (await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!))).ParamName);
        Assert.Equal(
            "request",
            (await Assert.ThrowsAsync<ArgumentNullException>(() => service.ConfirmAsync(null!))).ParamName);
        Assert.Equal(
            "request",
            (await Assert.ThrowsAsync<ArgumentNullException>(() => service.RefundAsync(null!))).ParamName);
        Assert.Equal(
            "externalId",
            (await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetInfoAsync(null!))).ParamName);

        Assert.Empty(handler.Requests);
    }

    /// <summary>
    /// Nothing sensitive may reach a log sink: not the external ID, not the card number, not the track 2
    /// data, and not the request or response body.
    /// </summary>
    [Fact]
    public async Task EveryOperation_ShouldLogTheStaticRouteOnly()
    {
        RecordingHandler handler = RecordingHandler.Json(
            $$"""{"fc_id":"fc-1","external_id":"{{Exp354TestContext.SecretExternalId}}"}""");
        RecordingLogger logger = new();
        InStorePaymentService service = Exp354TestContext.InStorePayments(handler, logger: logger);

        await service.CreateAsync(new InStorePaymentCreateRequest
        {
            ExternalId = Exp354TestContext.SecretExternalId,
            PosTerminalId = "pos-1",
            TerminalSn = "sn-1",
            Amount = "10050",
            Currency = InStorePaymentCurrency.Uah,
            MerchantId = Exp354TestContext.SecretMerchantId
        });

        await service.ConfirmAsync(new InStorePaymentConfirmRequest
        {
            ExternalId = Exp354TestContext.SecretExternalId,
            PosTerminalId = "pos-1",
            Amount = "10050",
            PosPaymentStatus = "approved",
            CardNumber = SyntheticCardNumber,
            EncryptedTrack2 = SyntheticTrack2
        });

        await service.RefundAsync(new InStorePaymentRefundRequest
        {
            PaymentExternalId = Exp354TestContext.SecretExternalId,
            RefundExternalId = "refund-1",
            TerminalSn = "sn-1",
            Amount = "10050",
            PaymentSystem = "visa",
            PosTerminalId = "pos-1",
            CardNumber = SyntheticCardNumber,
            BankAcquirer = "Bank",
            AuthorizationCode = "auth-1",
            EncryptedTrack2 = SyntheticTrack2
        });

        await service.GetInfoAsync(Exp354TestContext.SecretExternalId);

        string[] forbidden =
        [
            Exp354TestContext.SecretExternalId,
            Exp354TestContext.SecretMerchantId,
            SyntheticCardNumber,
            SyntheticTrack2
        ];

        foreach (string text in logger.AllText)
        {
            foreach (string marker in forbidden)
            {
                Assert.DoesNotContain(marker, text, StringComparison.Ordinal);
            }
        }

        // The static routes are what the log does carry.
        Assert.Contains(logger.StateValues, value => value.Contains(CreateEndpoint, StringComparison.Ordinal));
        Assert.Contains(logger.StateValues, value => value.Contains(ConfirmEndpoint, StringComparison.Ordinal));
        Assert.Contains(logger.StateValues, value => value.Contains(RefundEndpoint, StringComparison.Ordinal));
        Assert.Contains(logger.StateValues, value => value.Contains(InfoEndpoint, StringComparison.Ordinal));
    }

    [Fact]
    public async Task EveryOperation_ShouldPropagateCancellation()
    {
        // Each operation gets its own handler so that the in-flight cancellation is unambiguous.
        await AssertCancels((service, token) => service.CreateAsync(MinimalCreateRequest(), token));
        await AssertCancels((service, token) => service.ConfirmAsync(MinimalConfirmRequest(), token));
        await AssertCancels((service, token) => service.RefundAsync(MinimalRefundRequest(), token));
        await AssertCancels((service, token) => service.GetInfoAsync("payment-1", token));
    }

    [Theory]
    [MemberData(nameof(ErrorMappings))]
    public async Task EveryOperation_ShouldMapErrorsThroughTheExistingExceptions(
        HttpStatusCode status,
        Type expectedExceptionType)
    {
        RecordingHandler handler = RecordingHandler.Error(
            status,
            """{"code":"pos_declined","message":"Terminal rejected the operation","error_id":"req-7"}""");
        InStorePaymentService service = Exp354TestContext.InStorePayments(handler);

        RozetkaPayException createFailure = (RozetkaPayException)await Assert.ThrowsAnyAsync<Exception>(
            () => service.CreateAsync(MinimalCreateRequest()));

        Assert.IsType(expectedExceptionType, createFailure);
        Assert.Equal(status, createFailure.ApiError!.StatusCode);
        Assert.Equal("pos_declined", createFailure.ApiError.Code);
        Assert.Equal("req-7", createFailure.ApiError.RequestId);

        // The bodyless POST path maps failures through exactly the same switch.
        RozetkaPayException infoFailure = (RozetkaPayException)await Assert.ThrowsAnyAsync<Exception>(
            () => service.GetInfoAsync("payment-1"));

        Assert.IsType(expectedExceptionType, infoFailure);
        Assert.Equal(status, infoFailure.ApiError!.StatusCode);
    }

    /// <summary>
    /// Amount stays exactly the text the caller supplied, in both directions. A decimal mapping would
    /// normalize away leading and trailing zeros.
    /// </summary>
    [Theory]
    [InlineData("010050")]
    [InlineData("10050")]
    [InlineData("0")]
    [InlineData("00")]
    public async Task Amount_ShouldRoundTripAsExactText(string amount)
    {
        RecordingHandler handler = RecordingHandler.Json($$"""{"amount":"{{amount}}"}""");

        InStorePaymentCreateResponse response = await Exp354TestContext.InStorePayments(handler)
            .CreateAsync(new InStorePaymentCreateRequest
            {
                ExternalId = "payment-1",
                PosTerminalId = "pos-1",
                TerminalSn = "sn-1",
                Amount = amount,
                Currency = InStorePaymentCurrency.Uah
            });

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.Contains($"\"amount\":\"{amount}\"", recorded.Body!);
        Assert.Equal(amount, response.Amount);
    }

    /// <summary>
    /// Amounts are modelled as strings on every in-store DTO. A <see cref="decimal"/> anywhere here would
    /// silently rewrite provider text.
    /// </summary>
    [Fact]
    public void AmountProperties_ShouldAllBeStrings()
    {
        Type[] types =
        [
            typeof(InStorePaymentCreateRequest),
            typeof(InStorePaymentConfirmRequest),
            typeof(InStorePaymentRefundRequest),
            typeof(InStorePaymentCreateResponse),
            typeof(InStorePaymentConfirmResponse),
            typeof(InStorePaymentRefundResponse),
            typeof(InStorePaymentCreateReceiptData)
        ];

        foreach (Type type in types)
        {
            foreach (System.Reflection.PropertyInfo property in type.GetProperties())
            {
                if (property.Name.Contains("Amount", StringComparison.Ordinal))
                {
                    Assert.Equal(typeof(string), property.PropertyType);
                }
            }
        }
    }

    private static async Task AssertCancels(Func<InStorePaymentService, CancellationToken, Task> operation)
    {
        RecordingHandler handler = RecordingHandler.Json("{}");
        using CancellationTokenSource cancellation = new();
        handler.OnRequest = (_, _) => cancellation.Cancel();

        InStorePaymentService service = Exp354TestContext.InStorePayments(handler);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation(service, cancellation.Token));

        Exp354Request recorded = Assert.Single(handler.Requests);
        Assert.True(recorded.CancellationRequestedOnArrival);
    }

    private static InStorePaymentCreateRequest MinimalCreateRequest()
    {
        return new InStorePaymentCreateRequest
        {
            ExternalId = "payment-1",
            PosTerminalId = "pos-1",
            TerminalSn = "sn-1",
            Amount = "10050",
            Currency = InStorePaymentCurrency.Uah
        };
    }

    private static InStorePaymentConfirmRequest MinimalConfirmRequest()
    {
        return new InStorePaymentConfirmRequest
        {
            ExternalId = "payment-1",
            PosTerminalId = "pos-1",
            Amount = "10050",
            PosPaymentStatus = "approved"
        };
    }

    private static InStorePaymentRefundRequest MinimalRefundRequest()
    {
        return new InStorePaymentRefundRequest
        {
            PaymentExternalId = "payment-1",
            RefundExternalId = "refund-1",
            TerminalSn = "sn-1",
            Amount = "10050",
            PaymentSystem = "visa",
            PosTerminalId = "pos-1",
            CardNumber = SyntheticCardNumber,
            BankAcquirer = "Bank",
            AuthorizationCode = "auth-1"
        };
    }
}
