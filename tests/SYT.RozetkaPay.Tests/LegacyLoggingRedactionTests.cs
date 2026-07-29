using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Models.AlternativePayments;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Customers;
using SYT.RozetkaPay.Models.PaymentInstructions;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Models.PayParts;
using SYT.RozetkaPay.Models.Payouts;
using SYT.RozetkaPay.Models.Subscriptions;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// EXP-359: the SDK's own service logging never contains a caller value.
///
/// <para>
/// A request has two different targets. One is the <b>real</b> request target, which has to reach the wire
/// verbatim - escaped identifiers, query values and all. The other is the <b>log label</b>, which is
/// whatever the SDK hands a log sink. Before this change several transport helpers used the first as the
/// second, so an identifier the caller put in a path or a query was written out at Information level, both
/// in the rendered message and in the structured <c>Endpoint</c> / <c>FallbackEndpoint</c> properties.
/// </para>
///
/// <para>
/// The contract these tests pin:
/// </para>
/// <list type="bullet">
/// <item>every overload that takes no separate label <b>fails closed</b> and logs <c>[redacted]</c> - which
/// protects an externally derived service as well as the ones in this assembly;</item>
/// <item>every internal callsite passes an explicit, compile-time static route template, so route-level
/// observability survives the fail-closed change;</item>
/// <item>credentials, request bodies, response bodies, provider error text, raw bodies and the decline
/// <c>Location</c> are not log fields, and the SDK opens no logging scope;</item>
/// <item>nothing about the wire changed - the same verb, target, escaping, body and content type.</item>
/// </list>
///
/// <para>
/// Every target is intercepted by <see cref="RedactionHandler"/> and the configured host is in the reserved
/// <c>.invalid</c> TLD, so no test here performs DNS resolution, opens a socket, waits, or uses a
/// credential. The default disabled retry policy applies unless a test states otherwise.
/// </para>
/// </summary>
public class LegacyLoggingRedactionTests
{
    /// <summary>Helper keys of the <see cref="BaseService"/> matrices.</summary>
    private const string Get = "get";
    private const string Post = "post";
    private const string PostAllowingNoContent = "post-204";
    private const string Patch = "patch";
    private const string PostWithoutBody = "post-bodyless";
    private const string Delete = "delete";
    private const string DeleteWithBody = "delete-body";

    // =========================================================================================
    // 1. BaseService: the no-label overloads fail closed
    // =========================================================================================

    /// <summary>
    /// Every legacy overload keeps its signature and its wire behaviour, and logs <c>[redacted]</c> instead
    /// of the target it was given. The target carries a hostile marker, so a helper that quoted
    /// the real request is distinguishable from one that quoted the label.
    /// </summary>
    [Theory]
    [InlineData(Get)]
    [InlineData(Post)]
    [InlineData(PostAllowingNoContent)]
    [InlineData(Patch)]
    [InlineData(Delete)]
    public async Task NoLabelHelper_ShouldLogRedactedInsteadOfTheRealRequestTarget(string helper)
    {
        (CapturingLoggerProvider logs, ILogger logger) = LoggingRedactionContext.Capture();
        RedactionHandler handler = RedactionHandler.Json();
        LoggingRedactionProbeService probe = LoggingRedactionContext.Probe(handler, logger);

        string primary = PrimaryTarget();

        await InvokeNoLabelAsync(probe, helper, primary);

        // The wire is untouched: the exact strings the caller passed, in the expected order.
        Assert.Equal(primary, handler.Single.Target);

        // Neither marker reached any category, message, structured value or scope - in either spelling.
        LoggingRedactionAssert.NotLoggedInEitherSpelling(
            logs,
            LoggingRedactionContext.PrimaryRawMarker,
            LoggingRedactionContext.PrimaryEncodedMarker);
        LoggingRedactionAssert.NotLoggedInEitherSpelling(
            logs,
            LoggingRedactionContext.FallbackRawMarker,
            LoggingRedactionContext.FallbackEncodedMarker);

        LoggingRedactionAssert.Logged(logs, LoggingRedactionContext.RedactedLabel);
        LoggingRedactionAssert.NoScopes(logs);
    }

    // =========================================================================================
    // 2. BaseService: the label-aware overloads log exactly the label they were given
    // =========================================================================================

    /// <summary>
    /// The full explicit-label surface, including the two DELETE forms and the bodiless POST, which have no
    /// no-label counterpart. The real markers still reach the wire; only the static templates are logged.
    /// </summary>
    [Theory]
    [InlineData(Get)]
    [InlineData(Post)]
    [InlineData(PostAllowingNoContent)]
    [InlineData(Patch)]
    [InlineData(PostWithoutBody)]
    [InlineData(Delete)]
    [InlineData(DeleteWithBody)]
    public async Task LabelAwareHelper_ShouldLogTheStaticLabelAndNeverTheRealRequestTarget(string helper)
    {
        (CapturingLoggerProvider logs, ILogger logger) = LoggingRedactionContext.Capture();
        RedactionHandler handler = RedactionHandler.Json();
        LoggingRedactionProbeService probe = LoggingRedactionContext.Probe(handler, logger);

        string primary = PrimaryTarget();

        await InvokeWithLabelAsync(probe, helper, primary);

        Assert.Equal(primary, handler.Single.Target);

        LoggingRedactionAssert.NotLoggedInEitherSpelling(
            logs,
            LoggingRedactionContext.PrimaryRawMarker,
            LoggingRedactionContext.PrimaryEncodedMarker);
        LoggingRedactionAssert.NotLoggedInEitherSpelling(
            logs,
            LoggingRedactionContext.FallbackRawMarker,
            LoggingRedactionContext.FallbackEncodedMarker);

        LoggingRedactionAssert.Logged(logs, LoggingRedactionContext.ProbeLabel);
        LoggingRedactionAssert.NoScopes(logs);
    }

    /// <summary>
    /// The body a helper sent is never a log field, whichever helper sent it.
    /// </summary>
    [Theory]
    [InlineData(Post)]
    [InlineData(PostAllowingNoContent)]
    [InlineData(Patch)]
    [InlineData(DeleteWithBody)]
    public async Task BodyCarryingHelper_ShouldSendTheBodyAndNeverLogIt(string helper)
    {
        (CapturingLoggerProvider logs, ILogger logger) = LoggingRedactionContext.Capture();
        RedactionHandler handler = RedactionHandler.Json();
        LoggingRedactionProbeService probe = LoggingRedactionContext.Probe(handler, logger);

        await InvokeWithLabelAsync(probe, helper, PrimaryTarget());

        // Every request really carried the body, as JSON.
        Assert.All(handler.Requests, request =>
        {
            Assert.True(request.HasContent);
            Assert.Equal("application/json; charset=utf-8", request.ContentType);
            Assert.Contains(
                LoggingRedactionContext.RequestBodyMarker,
                request.Body!,
                StringComparison.Ordinal);
        });

        LoggingRedactionAssert.NotLogged(logs, LoggingRedactionContext.RequestBodyMarker);
    }

    // =========================================================================================
    // 3. Service callsites: every dynamic target logs a static route template
    // =========================================================================================

    // ---------- PaymentService ----------

    [Fact]
    public async Task PaymentInfo_ShouldNotLogTheExternalIdInTheQuery()
    {
        const string Row = "payment-info";
        const string Label = "/api/payments/v1/info";

        (RedactionHandler handler, CapturingLoggerProvider logs, PaymentService service) =
            Arrange(static (c, h, l) => new PaymentService(c, h, l));

        await service.GetInfoAsync(LoggingRedactionContext.RawMarker(Row));

        Assert.Equal($"{Label}?external_id={LoggingRedactionContext.EncodedMarker(Row)}", handler.Single.Target);
        AssertRedacted(logs, [Row], Label);
    }

    [Fact]
    public async Task PaymentList_ShouldNotLogTheFilterValuesInTheQuery()
    {
        const string Row = "payment-list";
        const string Label = "/api/payments/v1/list";

        (RedactionHandler handler, CapturingLoggerProvider logs, PaymentService service) =
            Arrange(static (c, h, l) => new PaymentService(c, h, l));

        await service.GetListAsync(new PaymentListRequest
        {
            Status = LoggingRedactionContext.RawMarker(Row),
            Limit = 25
        });

        Assert.Equal(
            $"{Label}?status={LoggingRedactionContext.EncodedMarker(Row)}&limit=25",
            handler.Single.Target);
        AssertRedacted(logs, [Row], Label);
    }

    [Fact]
    public async Task PaymentReceipt_ShouldNotLogTheExternalIdInTheQuery()
    {
        const string Row = "payment-receipt";
        const string Label = "/api/payments/v1/receipt";

        (RedactionHandler handler, CapturingLoggerProvider logs, PaymentService service) =
            Arrange(static (c, h, l) => new PaymentService(c, h, l));

        await service.GetReceiptAsync(LoggingRedactionContext.RawMarker(Row));

        Assert.Equal($"{Label}?external_id={LoggingRedactionContext.EncodedMarker(Row)}", handler.Single.Target);
        AssertRedacted(logs, [Row], Label);
    }

    /// <summary>
    /// The one operation that logged a request-body value directly, rather than through the transport
    /// helper. Both the external ID and the amount are still sent; neither is logged, and no substitute
    /// message was introduced - the route label is the whole log.
    /// </summary>
    [Fact]
    public async Task ConfirmP2P_ShouldNotLogTheExternalIdOrTheAmount()
    {
        const string Row = "p2p-confirm";
        const string Label = "/api/payments/v1/p2p/confirm";
        const decimal Amount = 4242.4242m;

        (RedactionHandler handler, CapturingLoggerProvider logs, PaymentService service) =
            Arrange(static (c, h, l) => new PaymentService(c, h, l));

        await service.ConfirmP2PAsync(LoggingRedactionContext.BodyMarker(Row), Amount);

        RedactionRequest request = handler.Single;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(Label, request.Target);
        Assert.Contains(LoggingRedactionContext.BodyMarker(Row), request.Body!, StringComparison.Ordinal);
        Assert.Contains(Amount.ToString(CultureInfo.InvariantCulture), request.Body!, StringComparison.Ordinal);

        LoggingRedactionAssert.NotLogged(logs, LoggingRedactionContext.BodyMarker(Row));
        LoggingRedactionAssert.NotLogged(logs, Amount.ToString(CultureInfo.InvariantCulture));
        LoggingRedactionAssert.Logged(logs, Label);
        LoggingRedactionAssert.NoScopes(logs);

        // Exactly the two transport statements: the route label, and the response status.
        Assert.Equal(2, logs.Entries.Count);
    }

    // ---------- PayoutService ----------

    [Fact]
    public async Task PayoutInfo_ShouldNotLogTheExternalIdInTheQuery()
    {
        const string Row = "payout-info";
        const string Label = "/api/payouts/v1/info";

        (RedactionHandler handler, CapturingLoggerProvider logs, PayoutService service) =
            Arrange(static (c, h, l) => new PayoutService(c, h, l));

        await service.GetInfoAsync(LoggingRedactionContext.RawMarker(Row));

        Assert.Equal($"{Label}?external_id={LoggingRedactionContext.EncodedMarker(Row)}", handler.Single.Target);
        AssertRedacted(logs, [Row], Label);
    }

    [Fact]
    public async Task PayoutList_ShouldNotLogTheFilterValuesInTheQuery()
    {
        const string Row = "payout-list";
        const string Label = "/api/payouts/v1/list";

        (RedactionHandler handler, CapturingLoggerProvider logs, PayoutService service) =
            Arrange(static (c, h, l) => new PayoutService(c, h, l));

        await service.GetListAsync(new PayoutListRequest { Status = LoggingRedactionContext.RawMarker(Row) });

        Assert.Equal($"{Label}?status={LoggingRedactionContext.EncodedMarker(Row)}", handler.Single.Target);
        AssertRedacted(logs, [Row], Label);
    }

    [Fact]
    public async Task PayoutAccountBalance_ShouldNotLogTheMerchantEntityIdInTheQuery()
    {
        const string Row = "payout-account-balance";
        const string Label = "/api/payouts/v1/account-balance";

        (RedactionHandler handler, CapturingLoggerProvider logs, PayoutService service) =
            Arrange(static (c, h, l) => new PayoutService(c, h, l));

        await service.GetAccountBalanceAsync(LoggingRedactionContext.RawMarker(Row));

        Assert.Equal(
            $"{Label}?merchant_entity_id={LoggingRedactionContext.EncodedMarker(Row)}",
            handler.Single.Target);
        AssertRedacted(logs, [Row], Label);
    }

    // ---------- FinMonService ----------

    /// <summary>
    /// The recipient IPN is an <see cref="int"/>, so it cannot carry reserved characters - but it is still a
    /// caller value in the query, and still must not be logged.
    /// </summary>
    [Fact]
    public async Task FinMonPreLimits_ShouldNotLogTheRecipientIpn()
    {
        const int RecipientIpn = 987654321;
        const string Label = "/api/finmon/v1/p2p-payment/pre-limits";

        (RedactionHandler handler, CapturingLoggerProvider logs, FinMonService service) =
            Arrange(static (c, h, l) => new FinMonService(c, h, l));

        await service.GetRulesAsync(RecipientIpn);

        Assert.Equal($"{Label}?recipient_ipn={RecipientIpn}", handler.Single.Target);
        LoggingRedactionAssert.NotLogged(logs, RecipientIpn.ToString(CultureInfo.InvariantCulture));
        LoggingRedactionAssert.Logged(logs, Label);
        LoggingRedactionAssert.NoScopes(logs);
    }

    // ---------- AlternativePaymentService ----------

    [Fact]
    public async Task AlternativePaymentOperationLookup_ShouldNotLogTheExternalIdInThePath()
    {
        const string Row = "alternative-operation";
        const string Label = "/api/alternative-payments/v1/operation/{external_id}";

        (RedactionHandler handler, CapturingLoggerProvider logs, AlternativePaymentService service) =
            Arrange(static (c, h, l) => new AlternativePaymentService(c, h, l));

        await service.GetOperationInfoAsync(LoggingRedactionContext.RawMarker(Row));

        Assert.Equal(
            $"/api/alternative-payments/v1/operation/{LoggingRedactionContext.EncodedMarker(Row)}",
            handler.Single.Target);
        AssertRedacted(logs, [Row], Label);
    }

    [Fact]
    public async Task AlternativePaymentOperationFallback_ShouldNotLogEitherRealRequestTarget()
    {
        const string ExternalRow = "alternative-fallback-external";
        const string OperationRow = "alternative-fallback-operation";
        const string PrimaryLabel = "/api/alternative-payments/v1/info/operation";

        (RedactionHandler handler, CapturingLoggerProvider logs, AlternativePaymentService service) =
            Arrange(static (c, h, l) => new AlternativePaymentService(c, h, l));

        await service.GetOperationInfoAsync(
            LoggingRedactionContext.RawMarker(ExternalRow),
            LoggingRedactionContext.RawMarker(OperationRow));

        Assert.Single(handler.Requests);
        Assert.Equal(
            $"{PrimaryLabel}?external_id={LoggingRedactionContext.EncodedMarker(ExternalRow)}" +
            $"&operation_id={LoggingRedactionContext.EncodedMarker(OperationRow)}",
            handler.Requests[0].Target);

        AssertRedacted(logs, [ExternalRow, OperationRow], PrimaryLabel);
    }

    [Fact]
    public async Task AlternativePaymentOperations_ShouldNotLogTheFilterValuesInTheQuery()
    {
        const string Row = "alternative-operations";
        const string Label = "/api/alternative-payments/v1/operations";

        (RedactionHandler handler, CapturingLoggerProvider logs, AlternativePaymentService service) =
            Arrange(static (c, h, l) => new AlternativePaymentService(c, h, l));

        await service.GetOperationsAsync(new GetAlternativePaymentOperationsRequest
        {
            Status = LoggingRedactionContext.RawMarker(Row)
        });

        Assert.Equal($"{Label}?status={LoggingRedactionContext.EncodedMarker(Row)}", handler.Single.Target);
        AssertRedacted(logs, [Row], Label);
    }

    [Fact]
    public async Task AlternativePaymentInfo_ShouldNotLogTheExternalIdInTheQuery()
    {
        const string Row = "alternative-info";
        const string Label = "/api/alternative-payments/v1/info";

        (RedactionHandler handler, CapturingLoggerProvider logs, AlternativePaymentService service) =
            Arrange(static (c, h, l) => new AlternativePaymentService(c, h, l));

        await service.GetInfoAsync(LoggingRedactionContext.RawMarker(Row));

        Assert.Equal($"{Label}?external_id={LoggingRedactionContext.EncodedMarker(Row)}", handler.Single.Target);
        AssertRedacted(logs, [Row], Label);
    }

    [Fact]
    public async Task AlternativePaymentStatus_ShouldNotLogThePaymentIdInThePath()
    {
        const string Row = "alternative-status";
        const string Label = "/api/alternative-payments/v1/{payment_id}/status";

        (RedactionHandler handler, CapturingLoggerProvider logs, AlternativePaymentService service) =
            Arrange(static (c, h, l) => new AlternativePaymentService(c, h, l));

        await service.GetStatusAsync(LoggingRedactionContext.RawMarker(Row));

        Assert.Equal(
            $"/api/alternative-payments/v1/{LoggingRedactionContext.EncodedMarker(Row)}/status",
            handler.Single.Target);
        AssertRedacted(logs, [Row], Label);
    }

    // ---------- PayPartsService ----------

    [Fact]
    public async Task PayPartsOperationLookup_ShouldNotLogTheOperationIdInThePath()
    {
        const string Row = "payparts-operation";
        const string Label = "/api/payparts/v1/operation/{operation_id}";

        (RedactionHandler handler, CapturingLoggerProvider logs, PayPartsService service) =
            Arrange(static (c, h, l) => new PayPartsService(c, h, l));

        await service.GetOperationInfoAsync(LoggingRedactionContext.RawMarker(Row));

        Assert.Equal(
            $"/api/payparts/v1/operation/{LoggingRedactionContext.EncodedMarker(Row)}",
            handler.Single.Target);
        AssertRedacted(logs, [Row], Label);
    }

    [Fact]
    public async Task PayPartsOperationFallback_ShouldNotLogEitherRealRequestTarget()
    {
        const string ExternalRow = "payparts-fallback-external";
        const string OperationRow = "payparts-fallback-operation";
        const string PrimaryLabel = "/api/payparts/v1/info/operation";

        (RedactionHandler handler, CapturingLoggerProvider logs, PayPartsService service) =
            Arrange(static (c, h, l) => new PayPartsService(c, h, l));

        await service.GetOperationInfoAsync(
            LoggingRedactionContext.RawMarker(ExternalRow),
            LoggingRedactionContext.RawMarker(OperationRow));

        Assert.Single(handler.Requests);
        Assert.Equal(
            $"{PrimaryLabel}?external_id={LoggingRedactionContext.EncodedMarker(ExternalRow)}" +
            $"&operation_id={LoggingRedactionContext.EncodedMarker(OperationRow)}",
            handler.Requests[0].Target);

        // The fallback of this operation is addressed by the operation ID, not the external ID.

        AssertRedacted(logs, [ExternalRow, OperationRow], PrimaryLabel);
    }

    [Fact]
    public async Task PayPartsInfo_ShouldNotLogTheExternalIdInTheQuery()
    {
        const string Row = "payparts-info";
        const string Label = "/api/payparts/v1/info";

        (RedactionHandler handler, CapturingLoggerProvider logs, PayPartsService service) =
            Arrange(static (c, h, l) => new PayPartsService(c, h, l));

        await service.GetInfoAsync(LoggingRedactionContext.RawMarker(Row));

        Assert.Equal($"{Label}?external_id={LoggingRedactionContext.EncodedMarker(Row)}", handler.Single.Target);
        AssertRedacted(logs, [Row], Label);
    }

    [Fact]
    public async Task PayPartsOperations_ShouldNotLogTheFilterValuesInTheQuery()
    {
        const string Row = "payparts-operations";
        const string Label = "/api/payparts/v1/operations";

        (RedactionHandler handler, CapturingLoggerProvider logs, PayPartsService service) =
            Arrange(static (c, h, l) => new PayPartsService(c, h, l));

        await service.GetOperationsAsync(new PayPartsOperationsListRequest
        {
            Status = LoggingRedactionContext.RawMarker(Row)
        });

        Assert.Equal($"{Label}?status={LoggingRedactionContext.EncodedMarker(Row)}", handler.Single.Target);
        AssertRedacted(logs, [Row], Label);
    }

    /// <summary>
    /// A fallback whose two targets are both static: the real routes are their own labels, so the fallback
    /// entry stays as informative as it was.
    /// </summary>
    [Fact]
    public async Task PayPartsBanks_ShouldLogBothStaticRoutes()
    {
        (RedactionHandler handler, CapturingLoggerProvider logs, PayPartsService service) =
            Arrange(static (c, h, l) => new PayPartsService(c, h, l));

        await service.GetBanksAsync();

        Assert.Single(handler.Requests);

        LoggingRedactionAssert.Logged(logs, "/api/payparts/v1/banks/info");
        LoggingRedactionAssert.Logged(logs, "/api/payparts/v1/banks");
        LoggingRedactionAssert.NotLogged(logs, LoggingRedactionContext.RedactedLabel);
        LoggingRedactionAssert.NoScopes(logs);
    }

    // ---------- CustomerService ----------

    [Fact]
    public async Task CustomerWallet_ShouldNotLogEitherRealRequestTarget()
    {
        const string Row = "customer-wallet";
        const string PrimaryLabel = "/api/customers/v1/wallet";

        (RedactionHandler handler, CapturingLoggerProvider logs, CustomerService service) =
            Arrange(static (c, h, l) => new CustomerService(c, h, l));

        await service.GetCustomerWalletAsync(LoggingRedactionContext.RawMarker(Row));

        Assert.Single(handler.Requests);
        Assert.Equal(
            $"{PrimaryLabel}?external_id={LoggingRedactionContext.EncodedMarker(Row)}",
            handler.Requests[0].Target);

        AssertRedacted(logs, [Row], PrimaryLabel);
    }

    [Fact]
    public async Task AddCardToWallet_ShouldNotLogEitherRealRequestTarget()
    {
        const string Row = "customer-add-card";
        const string PrimaryLabel = "/api/customers/v1/wallet";

        (RedactionHandler handler, CapturingLoggerProvider logs, CustomerService service) =
            Arrange(static (c, h, l) => new CustomerService(c, h, l));

        await service.AddCardToWalletAsync(
            LoggingRedactionContext.RawMarker(Row),
            new AddCardToWalletRequest
            {
                Card = new WalletCardDetails
                {
                    Number = "0000000000000000",
                    ExpMonth = "01",
                    ExpYear = "30",
                    Cvv = "000"
                }
            });

        Assert.Single(handler.Requests);
        Assert.Equal(
            $"{PrimaryLabel}?external_id={LoggingRedactionContext.EncodedMarker(Row)}",
            handler.Requests[0].Target);

        AssertRedacted(logs, [Row], PrimaryLabel);
    }

    /// <summary>
    /// The obsolete legacy DELETE. Its route, verb and response type are unchanged; only its log label is.
    /// </summary>
    [Fact]
    public async Task LegacyDeletePaymentFromWallet_ShouldNotLogEitherIdentifier()
    {
        const string CustomerRow = "legacy-delete-customer";
        const string CardRow = "legacy-delete-card";
        const string Label = "/api/customers/v1/{customer_id}/cards/{card_id}";

        (RedactionHandler handler, CapturingLoggerProvider logs, CustomerService service) =
            Arrange(static (c, h, l) => new CustomerService(c, h, l));

#pragma warning disable CS0618 // Deliberate regression coverage for the obsolete legacy route.
        await service.DeletePaymentFromWalletAsync(
            LoggingRedactionContext.RawMarker(CustomerRow),
            LoggingRedactionContext.RawMarker(CardRow));
#pragma warning restore CS0618

        RedactionRequest request = handler.Single;
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal(
            $"/api/customers/v1/{LoggingRedactionContext.EncodedMarker(CustomerRow)}" +
            $"/cards/{LoggingRedactionContext.EncodedMarker(CardRow)}",
            request.Target);

        AssertRedacted(logs, [CustomerRow, CardRow], Label);
    }

    [Fact]
    public async Task WalletItemLookup_ShouldNotLogEitherRealRequestTarget()
    {
        const string CustomerRow = "wallet-item-customer";
        const string CardRow = "wallet-item-card";
        const string PrimaryLabel = "/api/customers/v1/wallet/find";

        (RedactionHandler handler, CapturingLoggerProvider logs, CustomerService service) =
            Arrange(static (c, h, l) => new CustomerService(c, h, l));

        await service.GetWalletItemAsync(
            LoggingRedactionContext.RawMarker(CustomerRow),
            LoggingRedactionContext.RawMarker(CardRow));

        Assert.Single(handler.Requests);
        Assert.Equal(
            $"{PrimaryLabel}?external_id={LoggingRedactionContext.EncodedMarker(CustomerRow)}" +
            $"&option_id={LoggingRedactionContext.EncodedMarker(CardRow)}",
            handler.Requests[0].Target);

        AssertRedacted(logs, [CustomerRow, CardRow], PrimaryLabel);
    }

    [Fact]
    public async Task CardConfirmationStatus_ShouldNotLogEitherRealRequestTarget()
    {
        const string CustomerRow = "confirmation-customer";
        const string CardRow = "confirmation-card";
        const string PrimaryLabel = "/api/customers/v1/wallet/confirmation/status";

        (RedactionHandler handler, CapturingLoggerProvider logs, CustomerService service) =
            Arrange(static (c, h, l) => new CustomerService(c, h, l));

        await service.GetCardConfirmationStatusAsync(
            LoggingRedactionContext.RawMarker(CustomerRow),
            LoggingRedactionContext.RawMarker(CardRow));

        Assert.Single(handler.Requests);
        Assert.Equal(
            $"{PrimaryLabel}?external_id={LoggingRedactionContext.EncodedMarker(CustomerRow)}" +
            $"&option_id={LoggingRedactionContext.EncodedMarker(CardRow)}",
            handler.Requests[0].Target);

        AssertRedacted(logs, [CustomerRow, CardRow], PrimaryLabel);
    }

    [Fact]
    public async Task SetDefaultCard_ShouldNotLogEitherRealRequestTarget()
    {
        const string Row = "set-default-card";
        const string PrimaryLabel = "/api/customers/v1/wallet/settings/set";

        (RedactionHandler handler, CapturingLoggerProvider logs, CustomerService service) =
            Arrange(static (c, h, l) => new CustomerService(c, h, l));

        await service.SetDefaultCardAsync(
            LoggingRedactionContext.RawMarker(Row),
            new SetDefaultCardRequest
            {
                OptionId = new Guid("00000000-0000-0000-0000-000000000359"),
                Type = WalletOptionType.Card
            });

        Assert.Single(handler.Requests);
        Assert.Equal(
            $"{PrimaryLabel}?external_id={LoggingRedactionContext.EncodedMarker(Row)}",
            handler.Requests[0].Target);

        AssertRedacted(logs, [Row], PrimaryLabel);
    }

    [Fact]
    public async Task CustomerCards_ShouldNotLogTheCustomerIdInThePath()
    {
        const string Row = "customer-cards";
        const string Label = "/api/customers/v1/{customer_id}/cards";

        (RedactionHandler handler, CapturingLoggerProvider logs, CustomerService service) =
            Arrange(static (c, h, l) => new CustomerService(c, h, l));

        await service.GetCustomerCardsAsync(LoggingRedactionContext.RawMarker(Row));

        Assert.Equal(
            $"/api/customers/v1/{LoggingRedactionContext.EncodedMarker(Row)}/cards",
            handler.Single.Target);
        AssertRedacted(logs, [Row], Label);
    }

    // ---------- SubscriptionService ----------

    [Fact]
    public async Task GetPlan_ShouldNotLogThePlanIdInThePath()
    {
        const string Row = "plan-get";
        const string Label = "/api/subscriptions/v1/plans/{plan_id}";

        (RedactionHandler handler, CapturingLoggerProvider logs, SubscriptionService service) =
            Arrange(static (c, h, l) => new SubscriptionService(c, h, l));

        await service.GetPlanAsync(LoggingRedactionContext.RawMarker(Row));

        Assert.Equal(
            $"/api/subscriptions/v1/plans/{LoggingRedactionContext.EncodedMarker(Row)}",
            handler.Single.Target);
        AssertRedacted(logs, [Row], Label);
    }

    [Fact]
    public async Task UpdatePlan_ShouldNotLogThePlanIdInThePath()
    {
        const string Row = "plan-update";
        const string Label = "/api/subscriptions/v1/plans/{plan_id}";

        (RedactionHandler handler, CapturingLoggerProvider logs, SubscriptionService service) =
            Arrange(static (c, h, l) => new SubscriptionService(c, h, l));

        await service.UpdatePlanAsync(
            LoggingRedactionContext.RawMarker(Row),
            new UpdateSubscriptionPlanRequest());

        RedactionRequest request = handler.Single;
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal(
            $"/api/subscriptions/v1/plans/{LoggingRedactionContext.EncodedMarker(Row)}",
            request.Target);
        AssertRedacted(logs, [Row], Label);
    }

    [Fact]
    public async Task DeactivatePlan_ShouldNotLogThePlanIdInThePath()
    {
        const string Row = "plan-deactivate";
        const string Label = "/api/subscriptions/v1/plans/{plan_id}";

        (RedactionHandler handler, CapturingLoggerProvider logs, SubscriptionService service) =
            Arrange(static (c, h, l) => new SubscriptionService(c, h, l));

        await service.DeactivatePlanAsync(LoggingRedactionContext.RawMarker(Row));

        RedactionRequest request = handler.Single;
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal(
            $"/api/subscriptions/v1/plans/{LoggingRedactionContext.EncodedMarker(Row)}",
            request.Target);
        AssertRedacted(logs, [Row], Label);
    }

    [Fact]
    public async Task LegacyCustomerSubscriptions_ShouldNotLogTheCustomerIdInThePath()
    {
        const string Row = "legacy-customer-subscriptions";
        const string Label = "/api/subscriptions/v1/subscriptions/customer/{customer_id}";

        (RedactionHandler handler, CapturingLoggerProvider logs, SubscriptionService service) =
            Arrange(static (c, h, l) => new SubscriptionService(c, h, l));

#pragma warning disable CS0618 // Deliberate regression coverage for the obsolete legacy route.
        await service.GetCustomerSubscriptionsAsync(LoggingRedactionContext.RawMarker(Row));
#pragma warning restore CS0618

        Assert.Equal(
            $"/api/subscriptions/v1/subscriptions/customer/{LoggingRedactionContext.EncodedMarker(Row)}",
            handler.Single.Target);
        AssertRedacted(logs, [Row], Label);
    }

    [Fact]
    public async Task GetSubscription_ShouldNotLogTheSubscriptionIdInThePath()
    {
        const string Row = "subscription-get";
        const string Label = "/api/subscriptions/v1/subscriptions/{subscription_id}";

        (RedactionHandler handler, CapturingLoggerProvider logs, SubscriptionService service) =
            Arrange(static (c, h, l) => new SubscriptionService(c, h, l));

        await service.GetAsync(LoggingRedactionContext.RawMarker(Row));

        Assert.Equal(
            $"/api/subscriptions/v1/subscriptions/{LoggingRedactionContext.EncodedMarker(Row)}",
            handler.Single.Target);
        AssertRedacted(logs, [Row], Label);
    }

    [Fact]
    public async Task UpdateSubscription_ShouldNotLogTheSubscriptionIdInThePath()
    {
        const string Row = "subscription-update";
        const string Label = "/api/subscriptions/v1/subscriptions/{subscription_id}";

        (RedactionHandler handler, CapturingLoggerProvider logs, SubscriptionService service) =
            Arrange(static (c, h, l) => new SubscriptionService(c, h, l));

        await service.UpdateAsync(
            LoggingRedactionContext.RawMarker(Row),
            new UpdateSubscriptionRequest());

        RedactionRequest request = handler.Single;
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal(
            $"/api/subscriptions/v1/subscriptions/{LoggingRedactionContext.EncodedMarker(Row)}",
            request.Target);
        AssertRedacted(logs, [Row], Label);
    }

    [Fact]
    public async Task DeactivateSubscription_ShouldNotLogTheSubscriptionIdInThePath()
    {
        const string Row = "subscription-deactivate";
        const string Label = "/api/subscriptions/v1/subscriptions/{subscription_id}";

        (RedactionHandler handler, CapturingLoggerProvider logs, SubscriptionService service) =
            Arrange(static (c, h, l) => new SubscriptionService(c, h, l));

        await service.DeactivateAsync(LoggingRedactionContext.RawMarker(Row));

        RedactionRequest request = handler.Single;
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal(
            $"/api/subscriptions/v1/subscriptions/{LoggingRedactionContext.EncodedMarker(Row)}",
            request.Target);
        AssertRedacted(logs, [Row], Label);
    }

    [Fact]
    public async Task SubscriptionPayments_ShouldNotLogTheSubscriptionIdInThePath()
    {
        const string Row = "subscription-payments";
        const string Label = "/api/subscriptions/v1/subscriptions/{subscription_id}/payments";

        (RedactionHandler handler, CapturingLoggerProvider logs, SubscriptionService service) =
            Arrange(static (c, h, l) => new SubscriptionService(c, h, l));

        await service.GetPaymentsAsync(LoggingRedactionContext.RawMarker(Row));

        Assert.Equal(
            $"/api/subscriptions/v1/subscriptions/{LoggingRedactionContext.EncodedMarker(Row)}/payments",
            handler.Single.Target);
        AssertRedacted(logs, [Row], Label);
    }

    [Fact]
    public async Task LegacyCancelSubscription_ShouldNotLogTheSubscriptionIdInThePath()
    {
        const string Row = "legacy-cancel-subscription";
        const string Label = "/api/subscriptions/v1/subscriptions/{subscription_id}/cancel";

        (RedactionHandler handler, CapturingLoggerProvider logs, SubscriptionService service) =
            Arrange(static (c, h, l) => new SubscriptionService(c, h, l));

#pragma warning disable CS0618 // Deliberate regression coverage for the obsolete legacy route.
        await service.CancelAsync(
            LoggingRedactionContext.RawMarker(Row),
            new CancelSubscriptionRequest { ExternalId = "subscription-id-placeholder" });
#pragma warning restore CS0618

        RedactionRequest request = handler.Single;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(
            $"/api/subscriptions/v1/subscriptions/{LoggingRedactionContext.EncodedMarker(Row)}/cancel",
            request.Target);
        AssertRedacted(logs, [Row], Label);
    }

    // =========================================================================================
    // 4. Sensitive payloads that are not log fields
    // =========================================================================================

    /// <summary>
    /// No configured credential reaches a sink, in any spelling: neither the login or password, nor the
    /// <c>Basic</c> value derived from them, nor the two optional authentication headers.
    /// </summary>
    /// <remarks>
    /// Absence is only meaningful if the credential was there to leak, so the first half of this test proves
    /// the handler really received all three headers, with exactly the configured values, under their
    /// production names. Otherwise a regression that stopped sending authentication altogether would satisfy
    /// every "not logged" assertion below.
    /// </remarks>
    [Fact]
    public async Task NoConfiguredCredential_ShouldEverReachALogSink()
    {
        (CapturingLoggerProvider logs, ILogger logger) = LoggingRedactionContext.Capture();
        RedactionHandler handler = RedactionHandler.Json();
        LoggingRedactionProbeService probe = LoggingRedactionContext.Probe(
            handler,
            logger,
            LoggingRedactionContext.ConfigurationWithCredentials());

        await probe.GetWithLabelAsync(PrimaryTarget(), LoggingRedactionContext.ProbeLabel);

        RedactionRequest request = handler.Single;

        // The credential really did reach the handler: the expected scheme, and a parameter that decodes to
        // exactly the configured login and password with one separating colon. Decoded rather than
        // recomputed, so the assertion does not re-implement the SDK's own encoder.
        Assert.Equal(LoggingRedactionContext.BasicScheme, request.AuthorizationScheme);
        Assert.NotNull(request.AuthorizationParameter);

        string basicParameter = request.AuthorizationParameter!;
        Assert.Equal(
            $"{LoggingRedactionContext.LoginPlaceholder}:{LoggingRedactionContext.PasswordPlaceholder}",
            Encoding.UTF8.GetString(Convert.FromBase64String(basicParameter)));

        // Both optional authentication headers reached the handler under their production names, carrying
        // exactly the configured values and nothing else.
        Assert.Equal(
            LoggingRedactionContext.CustomerAuthPlaceholder,
            request.Header(LoggingRedactionContext.CustomerAuthHeaderName));
        Assert.Equal(
            LoggingRedactionContext.OnBehalfOfPlaceholder,
            request.Header(LoggingRedactionContext.OnBehalfOfHeaderName));

        // None of it reached a sink - including the derived Basic parameter, which is the credential in the
        // exact form it travels in.
        LoggingRedactionAssert.NotLogged(logs, LoggingRedactionContext.LoginPlaceholder);
        LoggingRedactionAssert.NotLogged(logs, LoggingRedactionContext.PasswordPlaceholder);
        LoggingRedactionAssert.NotLogged(logs, LoggingRedactionContext.CustomerAuthPlaceholder);
        LoggingRedactionAssert.NotLogged(logs, LoggingRedactionContext.OnBehalfOfPlaceholder);
        LoggingRedactionAssert.NotLogged(logs, basicParameter);
        LoggingRedactionAssert.NotLogged(logs, "Basic ");
        LoggingRedactionAssert.NotLogged(logs, LoggingRedactionContext.CustomerAuthHeaderName);
        LoggingRedactionAssert.NotLogged(logs, LoggingRedactionContext.OnBehalfOfHeaderName);
        LoggingRedactionAssert.NoScopes(logs);
    }

    /// <summary>
    /// A successful response body is not a log field either.
    /// </summary>
    [Fact]
    public async Task SuccessResponseBody_ShouldNotReachALogSink()
    {
        (CapturingLoggerProvider logs, ILogger logger) = LoggingRedactionContext.Capture();
        RedactionHandler handler = RedactionHandler.Json(
            $$"""{"outcome":"{{LoggingRedactionContext.ResponseBodyMarker}}"}""");
        LoggingRedactionProbeService probe = LoggingRedactionContext.Probe(handler, logger);

        RedactionResult result = await probe.GetWithLabelAsync(
            PrimaryTarget(),
            LoggingRedactionContext.ProbeLabel);

        // The body really was read and deserialized.
        Assert.Equal(LoggingRedactionContext.ResponseBodyMarker, result.Outcome);

        LoggingRedactionAssert.NotLogged(logs, LoggingRedactionContext.ResponseBodyMarker);
    }

    /// <summary>
    /// A hostile provider error body is mapped exactly as before and kept verbatim on
    /// <see cref="RozetkaPayApiError.RawBody"/>, while neither the provider message nor the raw body is
    /// logged - and the three safe error fields still are.
    /// </summary>
    [Fact]
    public async Task ProviderErrorBody_ShouldStayOnTheExceptionAndOutOfTheLogs()
    {
        const string ProviderMessage = "provider-error-message-must-never-be-logged-EXP359";
        const string RequestId = "request-id-EXP359";
        string body =
            $$"""{"code":"declined","message":"{{ProviderMessage}}","error_id":"{{RequestId}}"}""";

        (CapturingLoggerProvider logs, ILogger logger) = LoggingRedactionContext.Capture();
        RedactionHandler handler = RedactionHandler.Error(HttpStatusCode.BadRequest, body);
        LoggingRedactionProbeService probe = LoggingRedactionContext.Probe(handler, logger);

        RozetkaPayValidationException exception = await Assert.ThrowsAsync<RozetkaPayValidationException>(
            () => probe.GetWithLabelAsync(PrimaryTarget(), LoggingRedactionContext.ProbeLabel));

        // The evidence a caller depends on is unchanged: same mapped type, same raw body, same fields.
        Assert.NotNull(exception.ApiError);
        Assert.Equal(HttpStatusCode.BadRequest, exception.ApiError!.StatusCode);
        Assert.Equal("declined", exception.ApiError.Code);
        Assert.Equal(RequestId, exception.ApiError.RequestId);
        Assert.Equal(body, exception.ApiError.RawBody);

        LoggingRedactionAssert.NotLogged(logs, ProviderMessage);
        LoggingRedactionAssert.NotLogged(logs, body);

        // The safe error fields are deliberately retained.
        LoggingRedactionAssert.Logged(logs, "BadRequest");
        LoggingRedactionAssert.Logged(logs, "declined");
        LoggingRedactionAssert.Logged(logs, RequestId);
        LoggingRedactionAssert.NoScopes(logs);
    }

    /// <summary>
    /// The decline redirect target is returned to the caller and never logged, and neither identifier
    /// appears - the route label is the whole log entry.
    /// </summary>
    [Fact]
    public async Task DeclineRedirectLocation_ShouldBeReturnedAndNeverLogged()
    {
        const string ProjectRow = "decline-project";
        const string InstructionRow = "decline-instruction";
        const string Location = "https://provider.invalid/declined?marker=location-must-never-be-logged-EXP359";
        const string Label = "/api/payment-instructions/v1/decline";

        (CapturingLoggerProvider logs, ILogger logger) = LoggingRedactionContext.Capture(
            "SYT.RozetkaPay.Services.PaymentInstructionService");
        RedactionHandler decline = RedactionHandler.Redirect(Location);

        PaymentInstructionService service = new(
            LoggingRedactionContext.Configuration(),
            LoggingRedactionContext.Client(RedactionHandler.Json()),
            LoggingRedactionContext.Client(decline),
            logger);

        using (service as IDisposable)
        {
            PaymentInstructionDeclineResult result = await service.DeclineAsync(
                LoggingRedactionContext.RawMarker(ProjectRow),
                LoggingRedactionContext.RawMarker(InstructionRow));

            Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
            Assert.Equal(Location, result.Location.ToString());
        }

        LoggingRedactionAssert.NotLogged(logs, Location);
        LoggingRedactionAssert.NotLogged(logs, "location-must-never-be-logged-EXP359");
        AssertRedacted(logs, [ProjectRow, InstructionRow], Label);
    }

    /// <summary>
    /// The shared retry warning is the one statement every operation shares. It reports the retry number,
    /// the budget, the failure category, the HTTP status and the delay - and no request target, provider
    /// text, raw body, or exception message.
    /// </summary>
    [Fact]
    public async Task RetryWarning_ShouldCarryNoTargetProviderTextOrExceptionMessage()
    {
        const string ProviderMessage = "retry-provider-message-must-never-be-logged-EXP359";
        string body = $$"""{"code":"server_error","message":"{{ProviderMessage}}"}""";

        RozetkaPayConfiguration configuration = LoggingRedactionContext.Configuration();
        configuration.RetryPolicy = new RetryPolicy
        {
            Enabled = true,
            MaxRetryAttempts = 1,
            BaseDelay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            BackoffStrategy = BackoffStrategy.Fixed
        };

        (CapturingLoggerProvider logs, ILogger logger) = LoggingRedactionContext.Capture();
        RedactionHandler handler = RedactionHandler.Error(HttpStatusCode.InternalServerError, body);
        LoggingRedactionProbeService probe = LoggingRedactionContext.Probe(handler, logger, configuration);

        RozetkaPayException exception = await Assert.ThrowsAsync<RozetkaPayException>(
            () => probe.GetWithLabelAsync(PrimaryTarget(), LoggingRedactionContext.ProbeLabel));

        // The retry really happened: 1 + MaxRetryAttempts requests, and the last failure propagated.
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(body, exception.ApiError!.RawBody);

        CapturedLogEntry warning = Assert.Single(
            logs.Entries,
            entry => entry.Level == LogLevel.Warning);
        Assert.Contains("Retry 1 of 1", warning.Message, StringComparison.Ordinal);
        Assert.Contains("HTTP status 500", warning.Message, StringComparison.Ordinal);

        Assert.DoesNotContain(ProviderMessage, warning.AllText.Aggregate(string.Empty, string.Concat));
        Assert.DoesNotContain(body, warning.AllText.Aggregate(string.Empty, string.Concat));
        Assert.DoesNotContain(
            LoggingRedactionContext.PrimaryEncodedMarker,
            warning.AllText.Aggregate(string.Empty, string.Concat));

        LoggingRedactionAssert.NotLogged(logs, ProviderMessage);
        LoggingRedactionAssert.NotLoggedInEitherSpelling(
            logs,
            LoggingRedactionContext.PrimaryRawMarker,
            LoggingRedactionContext.PrimaryEncodedMarker);
        LoggingRedactionAssert.NoScopes(logs);
    }

    /// <summary>
    /// A hostile <c>404</c> reaches the caller without its provider message or raw body being logged, and
    /// without a second request being sent anywhere.
    /// </summary>
    [Fact]
    public async Task HostileNotFound_ShouldNotLogTheProviderMessage_AndShouldNotRetryElsewhere()
    {
        (CapturingLoggerProvider logs, ILogger logger) = LoggingRedactionContext.Capture();
        RedactionHandler handler = RedactionHandler.NotFoundThenJson();
        LoggingRedactionProbeService probe = LoggingRedactionContext.Probe(handler, logger);

        await Assert.ThrowsAsync<RozetkaPayNotFoundException>(
            () => probe.GetWithLabelAsync(PrimaryTarget(), LoggingRedactionContext.ProbeLabel));

        Assert.Single(handler.Requests);
        LoggingRedactionAssert.NotLogged(logs, LoggingRedactionContext.ProviderMessageMarker);
        LoggingRedactionAssert.NotLogged(logs, LoggingRedactionContext.NotFoundBody);
    }

    // =========================================================================================
    // Helpers
    // =========================================================================================

    private static string PrimaryTarget()
    {
        return $"{LoggingRedactionContext.ProbeRoute}/{LoggingRedactionContext.PrimaryEncodedMarker}";
    }

    private static string FallbackTarget()
    {
        return $"{LoggingRedactionContext.ProbeFallbackRoute}/{LoggingRedactionContext.FallbackEncodedMarker}";
    }

    private static RedactionPayload Payload()
    {
        return new RedactionPayload { Marker = LoggingRedactionContext.RequestBodyMarker };
    }

    private static Task InvokeNoLabelAsync(
        LoggingRedactionProbeService probe,
        string helper,
        string primary)
    {
        return helper switch
        {
            Get => probe.LegacyGetAsync(primary),
            Post => probe.LegacyPostAsync(primary, Payload()),
            PostAllowingNoContent => probe.LegacyPostAllowingNoContentAsync(primary, Payload()),
            Patch => probe.LegacyPatchAsync(primary, Payload()),
            Delete => probe.LegacyDeleteAsync(primary),
            _ => throw new ArgumentOutOfRangeException(nameof(helper), helper, "Unknown helper key.")
        };
    }

    private static Task InvokeWithLabelAsync(
        LoggingRedactionProbeService probe,
        string helper,
        string primary)
    {
        string label = LoggingRedactionContext.ProbeLabel;

        return helper switch
        {
            Get => probe.GetWithLabelAsync(primary, label),
            Post => probe.PostWithLabelAsync(primary, label, Payload()),
            PostAllowingNoContent => probe.PostAllowingNoContentWithLabelAsync(primary, label, Payload()),
            Patch => probe.PatchWithLabelAsync(primary, label, Payload()),
            PostWithoutBody => probe.PostWithoutBodyWithLabelAsync(primary, label),
            Delete => probe.DeleteWithLabelAsync(primary, label),
            DeleteWithBody => probe.DeleteWithBodyAndLabelAsync(primary, label, Payload()),
            _ => throw new ArgumentOutOfRangeException(nameof(helper), helper, "Unknown helper key.")
        };
    }

    /// <summary>
    /// A real service over an intercepting transport, logging under its own production category.
    /// </summary>
    private static (RedactionHandler Handler, CapturingLoggerProvider Logs, TService Service) Arrange<TService>(
        Func<RozetkaPayConfiguration, HttpClient, ILogger, TService> factory,
        bool fallback = false)
        where TService : BaseService
    {
        CapturingLoggerProvider logs = new();
        ILogger logger = logs.CreateLogger(typeof(TService).FullName!);
        RedactionHandler handler = RedactionHandler.Json();
        TService service = factory(
            LoggingRedactionContext.Configuration(),
            LoggingRedactionContext.Client(handler),
            logger);

        return (handler, logs, service);
    }

    /// <summary>
    /// Every caller value is absent from the logs in both spellings, every expected static label is
    /// present, and the SDK opened no scope.
    /// </summary>
    private static void AssertRedacted(
        CapturingLoggerProvider logs,
        string[] rows,
        params string[] labels)
    {
        foreach (string row in rows)
        {
            LoggingRedactionAssert.NotLoggedInEitherSpelling(
                logs,
                LoggingRedactionContext.RawMarker(row),
                LoggingRedactionContext.EncodedMarker(row));
        }

        foreach (string label in labels)
        {
            LoggingRedactionAssert.Logged(logs, label);
        }

        // A fail-closed label would satisfy "no marker was logged" while silently losing route-level
        // observability, so an internal callsite must never produce one.
        LoggingRedactionAssert.NotLogged(logs, LoggingRedactionContext.RedactedLabel);
        LoggingRedactionAssert.NoScopes(logs);
    }
}
