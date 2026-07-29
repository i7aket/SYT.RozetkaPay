using SYT.RozetkaPay.Models.AlternativePayments;
using SYT.RozetkaPay.Models.Batch;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Customers;
using SYT.RozetkaPay.Models.InStorePayments;
using SYT.RozetkaPay.Models.Partners;
using SYT.RozetkaPay.Models.PayParts;
using SYT.RozetkaPay.Models.PaymentInstructions;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Models.Payouts;
using SYT.RozetkaPay.Models.Reports;
using SYT.RozetkaPay.Models.Subscriptions;
using SYT.RozetkaPay.Services;

namespace SYT.RozetkaPay.Tests.TestInfrastructure;

/// <summary>
/// The canonical manifest of every operation the pinned RozetkaPay OpenAPI document publishes: one row per
/// operation, no more and no fewer.
/// </summary>
/// <remarks>
/// <para>
/// The manifest is written out by hand on purpose. Deriving rows by reflecting over the SDK would mirror
/// any mistake production already makes - a wrong verb, a wrong route, a legacy member standing in for a
/// published operation - and the suite would agree with the defect instead of catching it. The pinned
/// snapshot and this literal table are two independent statements, and
/// <c>OpenApiOperationContractTests</c> compares them as exact sets.
/// </para>
/// <para>
/// Caller-controlled path and query values carry a hostile suffix - space, <c>+</c>, <c>/</c>, <c>&amp;</c>,
/// <c>=</c>, <c>?</c>, <c>#</c>, <c>%</c>, space, then Cyrillic text - whose single-pass percent-encoding is
/// written out as its own literal. Expected request targets are built by concatenating literals only;
/// <see cref="Uri.EscapeDataString"/> and the production encoding helper are never called here.
/// </para>
/// <para>
/// Body sentinels stay ASCII. The SDK serializer escapes non-ASCII and HTML-sensitive characters, so a
/// hostile body value would only assert the escaping rules of <c>System.Text.Json</c>, which the
/// serializer-focused suites already own. What matters here is that each operation carries its own unique
/// payload, so a row cannot pass on another row's request.
/// </para>
/// </remarks>
internal static class OpenApiOperationManifest
{
    /// <summary>
    /// Raw hostile suffix appended to every caller-controlled identifier.
    /// </summary>
    private const string HostileRawSuffix = " +/&=?#% Привіт";

    /// <summary>
    /// Single-pass percent-encoding of <see cref="HostileRawSuffix"/>: space, <c>+</c>, <c>/</c>,
    /// <c>&amp;</c>, <c>=</c>, <c>?</c>, <c>#</c>, <c>%</c>, space, then the UTF-8 octets of "Привіт".
    /// </summary>
    private const string HostileEncodedSuffix =
        "%20%2B%2F%26%3D%3F%23%25%20%D0%9F%D1%80%D0%B8%D0%B2%D1%96%D1%82";

    /// <summary>Placeholder standing in for a card number. Deliberately not a card-shaped value.</summary>
    private const string CardNumberPlaceholder = "card-number-placeholder";

    /// <summary>Placeholder standing in for a card verification value.</summary>
    private const string CardVerificationPlaceholder = "cvv-placeholder";

    /// <summary>Report window start used by both report operations.</summary>
    private static readonly DateOnly ReportDateFrom = new(2026, 7, 1);

    /// <summary>Report window end used by both report operations.</summary>
    private static readonly DateOnly ReportDateTo = new(2026, 7, 25);

    /// <summary>
    /// Every published operation, exactly once, grouped as in section 6 of the EXP-337 plan.
    /// </summary>
    internal static IReadOnlyList<OpenApiOperationContract> All { get; } =
    [
        .. Payments(),
        .. BatchPayments(),
        .. Payouts(),
        .. PayParts(),
        .. AlternativePayments(),
        .. CustomerWallet(),
        .. Subscriptions(),
        .. Reports(),
        .. InStorePayments(),
        .. Partners(),
        .. MerchantInstructionsAndFinMon()
    ];

    /// <summary>
    /// Expected size of each coverage group. The sizes are asserted, and they sum to the 67 operations the
    /// pinned document declares.
    /// </summary>
    internal static IReadOnlyDictionary<string, int> ExpectedGroupSizes { get; } =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Payments"] = 11,
            ["Batch payments"] = 3,
            ["Payouts"] = 5,
            ["PayParts"] = 10,
            ["Alternative payments"] = 5,
            ["Customer wallet"] = 6,
            ["Subscriptions"] = 14,
            ["Reports"] = 2,
            ["In-store payments"] = 4,
            ["Partner operations"] = 3,
            ["Merchant, payment instructions and FinMon"] = 4
        };

    /// <summary>A caller-supplied raw identifier: an operation-unique slug plus the hostile suffix.</summary>
    private static string Raw(string slug) => slug + HostileRawSuffix;

    /// <summary>
    /// The same identifier as it must appear on the wire: the slug - which is made only of unreserved
    /// characters and therefore passes through unchanged - plus the encoded suffix.
    /// </summary>
    private static string Enc(string slug) => slug + HostileEncodedSuffix;

    // ===================== Payments - 11 =====================

    private static OpenApiOperationContract[] Payments() =>
    [
        new()
        {
            OperationId = "createPayment",
            Method = "POST",
            PathTemplate = "/api/payments/v1/new",
            Group = "Payments",
            ServiceInterface = typeof(IPaymentService),
            ServiceMethod = nameof(IPaymentService.CreateAsync),
            ExpectedPathAndQuery = "/api/payments/v1/new",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op01-create-payment\"", "\"amount\":101.01"],
            InvokeAsync = (host, token) => host.Payments.CreateAsync(
                new CreatePaymentRequest
                {
                    Amount = 101.01m,
                    Currency = "UAH",
                    ExternalId = "op01-create-payment",
                    Mode = PaymentMode.Direct
                },
                token)
        },
        new()
        {
            OperationId = "createRecurrentPayment",
            Method = "POST",
            PathTemplate = "/api/payments/v1/recurrent",
            Group = "Payments",
            ServiceInterface = typeof(IPaymentService),
            ServiceMethod = nameof(IPaymentService.CreateRecurrentAsync),
            ExpectedPathAndQuery = "/api/payments/v1/recurrent",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments =
                ["\"external_id\":\"op02-create-recurrent\"", "\"recurrent_id\":\"op02-recurrent-id\""],
            InvokeAsync = (host, token) => host.Payments.CreateRecurrentAsync(
                new CreateRecurrentPaymentRequest
                {
                    Amount = 102.02m,
                    ExternalId = "op02-create-recurrent",
                    RecurrentId = "op02-recurrent-id"
                },
                token)
        },
        new()
        {
            OperationId = "confirmPayment",
            Method = "POST",
            PathTemplate = "/api/payments/v1/confirm",
            Group = "Payments",
            ServiceInterface = typeof(IPaymentService),
            ServiceMethod = nameof(IPaymentService.ConfirmAsync),
            ExpectedPathAndQuery = "/api/payments/v1/confirm",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op03-confirm-payment\"", "\"amount\":103.03"],
            InvokeAsync = (host, token) => host.Payments.ConfirmAsync(
                new ConfirmPaymentRequest { ExternalId = "op03-confirm-payment", Amount = 103.03m },
                token)
        },
        new()
        {
            OperationId = "cancelPayment",
            Method = "POST",
            PathTemplate = "/api/payments/v1/cancel",
            Group = "Payments",
            ServiceInterface = typeof(IPaymentService),
            ServiceMethod = nameof(IPaymentService.CancelAsync),
            ExpectedPathAndQuery = "/api/payments/v1/cancel",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op04-cancel-payment\""],
            InvokeAsync = (host, token) => host.Payments.CancelAsync(
                new CancelPaymentRequest { ExternalId = "op04-cancel-payment" },
                token)
        },
        new()
        {
            OperationId = "refundPayment",
            Method = "POST",
            PathTemplate = "/api/payments/v1/refund",
            Group = "Payments",
            ServiceInterface = typeof(IPaymentService),
            ServiceMethod = nameof(IPaymentService.RefundAsync),
            ExpectedPathAndQuery = "/api/payments/v1/refund",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op05-refund-payment\"", "\"amount\":105.05"],
            InvokeAsync = (host, token) => host.Payments.RefundAsync(
                new RefundPaymentRequest { ExternalId = "op05-refund-payment", Amount = 105.05m },
                token)
        },
        new()
        {
            OperationId = "retryRefund",
            Method = "POST",
            PathTemplate = "/api/payments/v1/refund/retry",
            Group = "Payments",
            ServiceInterface = typeof(IPaymentService),
            ServiceMethod = nameof(IPaymentService.RetryRefundAsync),
            ExpectedPathAndQuery = "/api/payments/v1/refund/retry",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op06-retry-refund\""],
            InvokeAsync = (host, token) => host.Payments.RetryRefundAsync(
                new RetryRefundRequest { ExternalId = "op06-retry-refund" },
                token)
        },
        new()
        {
            OperationId = "cancelRefund",
            Method = "POST",
            PathTemplate = "/api/payments/v1/refund/cancel",
            Group = "Payments",
            ServiceInterface = typeof(IPaymentService),
            ServiceMethod = nameof(IPaymentService.CancelRefundAsync),
            ExpectedPathAndQuery = "/api/payments/v1/refund/cancel",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op07-cancel-refund\""],
            InvokeAsync = (host, token) => host.Payments.CancelRefundAsync(
                new CancelRefundRequest { ExternalId = "op07-cancel-refund" },
                token)
        },
        new()
        {
            OperationId = "paymentInfo",
            Method = "GET",
            PathTemplate = "/api/payments/v1/info",
            Group = "Payments",
            ServiceInterface = typeof(IPaymentService),
            ServiceMethod = nameof(IPaymentService.GetInfoAsync),
            ExpectedPathAndQuery = "/api/payments/v1/info?external_id=" + "op08-payment-info" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.Payments.GetInfoAsync(Raw("op08-payment-info"), token)
        },
        new()
        {
            OperationId = "resendCallback",
            Method = "POST",
            PathTemplate = "/api/payments/v1/callback/resend",
            Group = "Payments",
            ServiceInterface = typeof(IPaymentService),
            ServiceMethod = nameof(IPaymentService.ResendCallbackAsync),
            ExpectedPathAndQuery = "/api/payments/v1/callback/resend",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op09-resend-callback\""],
            InvokeAsync = (host, token) => host.Payments.ResendCallbackAsync(
                new ResendCallbackRequest { ExternalId = "op09-resend-callback" },
                token)
        },
        new()
        {
            OperationId = "cardLookup",
            Method = "POST",
            PathTemplate = "/api/payments/v1/lookup",
            Group = "Payments",
            ServiceInterface = typeof(IPaymentService),
            ServiceMethod = nameof(IPaymentService.CardLookupAsync),
            ExpectedPathAndQuery = "/api/payments/v1/lookup",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = [$"\"card_number\":\"{CardNumberPlaceholder}-op10\""],
            InvokeAsync = (host, token) => host.Payments.CardLookupAsync(
                new CardLookupRequest { CardNumber = $"{CardNumberPlaceholder}-op10" },
                token)
        },
        new()
        {
            OperationId = "getPaymentReceipt",
            Method = "GET",
            PathTemplate = "/api/payments/v1/receipt",
            Group = "Payments",
            ServiceInterface = typeof(IPaymentService),
            ServiceMethod = nameof(IPaymentService.GetReceiptAsync),
            ExpectedPathAndQuery = "/api/payments/v1/receipt?external_id=" + "op11-get-receipt" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.Payments.GetReceiptAsync(Raw("op11-get-receipt"), token)
        }
    ];

    // ===================== Batch payments - 3 =====================

    private static OpenApiOperationContract[] BatchPayments() =>
    [
        new()
        {
            OperationId = "createBatchPayment",
            Method = "POST",
            PathTemplate = "/api/payments/batch/v1/new",
            Group = "Batch payments",
            ServiceInterface = typeof(IBatchPaymentService),
            ServiceMethod = nameof(IBatchPaymentService.CreateBatchPaymentAsync),
            ExpectedPathAndQuery = "/api/payments/batch/v1/new",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments =
                ["\"batch_external_id\":\"op12-batch-create\"", "\"external_id\":\"op12-batch-order\""],
            InvokeAsync = (host, token) => host.BatchPayments.CreateBatchPaymentAsync(
                new CreateBatchPaymentRequest
                {
                    BatchExternalId = "op12-batch-create",
                    Currency = "UAH",
                    Orders =
                    [
                        new BatchOrder { Amount = 112.12m, ExternalId = "op12-batch-order" }
                    ]
                },
                token)
        },
        new()
        {
            OperationId = "confirmBatchPayment",
            Method = "POST",
            PathTemplate = "/api/payments/batch/v1/confirm",
            Group = "Batch payments",
            ServiceInterface = typeof(IBatchPaymentService),
            ServiceMethod = nameof(IBatchPaymentService.ConfirmBatchPaymentAsync),
            ExpectedPathAndQuery = "/api/payments/batch/v1/confirm",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments =
                ["\"batch_external_id\":\"op13-batch-confirm\"", "\"external_id\":\"op13-batch-payment\""],
            InvokeAsync = (host, token) => host.BatchPayments.ConfirmBatchPaymentAsync(
                new ConfirmBatchPaymentRequest
                {
                    BatchExternalId = "op13-batch-confirm",
                    ExternalId = "op13-batch-payment"
                },
                token)
        },
        new()
        {
            OperationId = "cancelBatchPayment",
            Method = "POST",
            PathTemplate = "/api/payments/batch/v1/cancel",
            Group = "Batch payments",
            ServiceInterface = typeof(IBatchPaymentService),
            ServiceMethod = nameof(IBatchPaymentService.CancelBatchPaymentAsync),
            ExpectedPathAndQuery = "/api/payments/batch/v1/cancel",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op14-batch-cancel\""],
            InvokeAsync = (host, token) => host.BatchPayments.CancelBatchPaymentAsync(
                new CancelBatchPaymentRequest { ExternalId = "op14-batch-cancel" },
                token)
        }
    ];

    // ===================== Payouts - 5 =====================

    private static OpenApiOperationContract[] Payouts() =>
    [
        new()
        {
            // The canonical create is RequestPayoutAsync. PayoutService.CreateAsync still posts to the
            // legacy /api/payouts/v1/new route, which the official document does not publish.
            OperationId = "createPayout",
            Method = "POST",
            PathTemplate = "/api/payouts/v1/request-payout",
            Group = "Payouts",
            ServiceInterface = typeof(IPayoutService),
            ServiceMethod = nameof(IPayoutService.RequestPayoutAsync),
            ExpectedPathAndQuery = "/api/payouts/v1/request-payout",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op15-request-payout\"", "\"entity_id\":\"op15-payer-entity\""],
            InvokeAsync = (host, token) => host.Payouts.RequestPayoutAsync(
                new RequestPayoutRequest
                {
                    Order = new PayoutOrderDetails
                    {
                        Currency = "UAH",
                        Description = "op15 payout",
                        ExternalId = "op15-request-payout",
                        OriginalAmount = "115.15"
                    },
                    Payer = new PayoutPayer { EntityId = "op15-payer-entity" },
                    Recipient = new PayoutRecipient
                    {
                        PayoutType = PayoutType.Card,
                        Card = new CardRecipient
                        {
                            FirstName = "op15-first",
                            LastName = "op15-last",
                            CardData = new CardData { Token = "op15-card-token" }
                        }
                    }
                },
                token)
        },
        new()
        {
            OperationId = "getPayoutTransactionStatus",
            Method = "GET",
            PathTemplate = "/api/payouts/v1/info",
            Group = "Payouts",
            ServiceInterface = typeof(IPayoutService),
            ServiceMethod = nameof(IPayoutService.GetInfoAsync),
            ExpectedPathAndQuery = "/api/payouts/v1/info?external_id=" + "op16-payout-info" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.Payouts.GetInfoAsync(Raw("op16-payout-info"), token)
        },
        new()
        {
            OperationId = "getMerchantAccountBalance",
            Method = "GET",
            PathTemplate = "/api/payouts/v1/account-balance",
            Group = "Payouts",
            ServiceInterface = typeof(IPayoutService),
            ServiceMethod = nameof(IPayoutService.GetAccountBalanceAsync),
            ExpectedPathAndQuery =
                "/api/payouts/v1/account-balance?merchant_entity_id=" + "op17-account-balance" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.Payouts.GetAccountBalanceAsync(Raw("op17-account-balance"), token)
        },
        new()
        {
            OperationId = "resendPayoutCallback",
            Method = "POST",
            PathTemplate = "/api/payouts/v1/resend-callback",
            Group = "Payouts",
            ServiceInterface = typeof(IPayoutService),
            ServiceMethod = nameof(IPayoutService.ResendCallbackAsync),
            ExpectedPathAndQuery = "/api/payouts/v1/resend-callback",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op18-resend-payout-callback\""],
            InvokeAsync = (host, token) => host.Payouts.ResendCallbackAsync(
                new ResendPayoutCallbackRequest { ExternalId = "op18-resend-payout-callback" },
                token)
        },
        new()
        {
            OperationId = "cancelCashPayout",
            Method = "POST",
            PathTemplate = "/api/payouts/v1/cancel-payout",
            Group = "Payouts",
            ServiceInterface = typeof(IPayoutService),
            ServiceMethod = nameof(IPayoutService.CancelCashPayoutAsync),
            ExpectedPathAndQuery = "/api/payouts/v1/cancel-payout",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op19-cancel-cash-payout\""],
            InvokeAsync = (host, token) => host.Payouts.CancelCashPayoutAsync(
                new CancelCashPayoutRequest { ExternalId = "op19-cancel-cash-payout" },
                token)
        }
    ];

    // ===================== PayParts - 10 =====================

    private static OpenApiOperationContract[] PayParts() =>
    [
        new()
        {
            OperationId = "payPartsCreateOrder",
            Method = "POST",
            PathTemplate = "/api/payparts/v1/order/create",
            Group = "PayParts",
            ServiceInterface = typeof(IPayPartsService),
            ServiceMethod = nameof(IPayPartsService.CreateOrderAsync),
            ExpectedPathAndQuery = "/api/payparts/v1/order/create",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op20-payparts-create\"", "\"parts_count\":3"],
            InvokeAsync = (host, token) => host.PayParts.CreateOrderAsync(
                new CreatePayPartsOrderRequest
                {
                    ExternalId = "op20-payparts-create",
                    Amount = 120.20m,
                    Currency = "UAH",
                    PartsCount = 3
                },
                token)
        },
        new()
        {
            OperationId = "payPartsConfirmOrder",
            Method = "POST",
            PathTemplate = "/api/payparts/v1/order/confirm",
            Group = "PayParts",
            ServiceInterface = typeof(IPayPartsService),
            ServiceMethod = nameof(IPayPartsService.ConfirmOrderAsync),
            ExpectedPathAndQuery = "/api/payparts/v1/order/confirm",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op21-payparts-confirm\""],
            InvokeAsync = (host, token) => host.PayParts.ConfirmOrderAsync(
                new ConfirmPayPartsRequest { ExternalId = "op21-payparts-confirm" },
                token)
        },
        new()
        {
            OperationId = "payPartsCancelOrder",
            Method = "POST",
            PathTemplate = "/api/payparts/v1/order/cancel",
            Group = "PayParts",
            ServiceInterface = typeof(IPayPartsService),
            ServiceMethod = nameof(IPayPartsService.CancelOrderAsync),
            ExpectedPathAndQuery = "/api/payparts/v1/order/cancel",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op22-payparts-cancel\""],
            InvokeAsync = (host, token) => host.PayParts.CancelOrderAsync(
                new CancelPayPartsRequest { ExternalId = "op22-payparts-cancel" },
                token)
        },
        new()
        {
            OperationId = "payPartsRefund",
            Method = "POST",
            PathTemplate = "/api/payparts/v1/refund",
            Group = "PayParts",
            ServiceInterface = typeof(IPayPartsService),
            ServiceMethod = nameof(IPayPartsService.RefundOrderAsync),
            ExpectedPathAndQuery = "/api/payparts/v1/refund",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op23-payparts-refund\"", "\"amount\":123.23"],
            InvokeAsync = (host, token) => host.PayParts.RefundOrderAsync(
                new RefundPayPartsOrderRequest { ExternalId = "op23-payparts-refund", Amount = 123.23m },
                token)
        },
        new()
        {
            OperationId = "payPartsRetryRefund",
            Method = "POST",
            PathTemplate = "/api/payparts/v1/refund/retry",
            Group = "PayParts",
            ServiceInterface = typeof(IPayPartsService),
            ServiceMethod = nameof(IPayPartsService.RetryRefundAsync),
            ExpectedPathAndQuery = "/api/payparts/v1/refund/retry",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op24-payparts-retry-refund\""],
            InvokeAsync = (host, token) => host.PayParts.RetryRefundAsync(
                new RetryRefundPPayRequest { ExternalId = "op24-payparts-retry-refund" },
                token)
        },
        new()
        {
            OperationId = "payPartsCancelRefund",
            Method = "POST",
            PathTemplate = "/api/payparts/v1/refund/cancel",
            Group = "PayParts",
            ServiceInterface = typeof(IPayPartsService),
            ServiceMethod = nameof(IPayPartsService.CancelRefundAsync),
            ExpectedPathAndQuery = "/api/payparts/v1/refund/cancel",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op25-payparts-cancel-refund\""],
            InvokeAsync = (host, token) => host.PayParts.CancelRefundAsync(
                new CancelRefundPPayRequest { ExternalId = "op25-payparts-cancel-refund" },
                token)
        },
        new()
        {
            // The canonical two-argument overload. The one-argument overload targets the legacy
            // /api/payparts/v1/operation/{id} route, which the official document does not publish.
            OperationId = "payPartsOperationInfo",
            Method = "GET",
            PathTemplate = "/api/payparts/v1/info/operation",
            Group = "PayParts",
            ServiceInterface = typeof(IPayPartsService),
            ServiceMethod = nameof(IPayPartsService.GetOperationInfoAsync),
            ExpectedPathAndQuery =
                "/api/payparts/v1/info/operation?external_id=" + "op26-payparts-external" + HostileEncodedSuffix +
                "&operation_id=" + "op26-payparts-operation" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.PayParts.GetOperationInfoAsync(
                Raw("op26-payparts-external"),
                Raw("op26-payparts-operation"),
                token)
        },
        new()
        {
            OperationId = "payPartsRequestInfo",
            Method = "GET",
            PathTemplate = "/api/payparts/v1/info",
            Group = "PayParts",
            ServiceInterface = typeof(IPayPartsService),
            ServiceMethod = nameof(IPayPartsService.GetInfoAsync),
            ExpectedPathAndQuery = "/api/payparts/v1/info?external_id=" + "op27-payparts-info" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.PayParts.GetInfoAsync(Raw("op27-payparts-info"), token)
        },
        new()
        {
            OperationId = "payPartsGetBanksInfo",
            Method = "GET",
            PathTemplate = "/api/payparts/v1/banks/info",
            Group = "PayParts",
            ServiceInterface = typeof(IPayPartsService),
            ServiceMethod = nameof(IPayPartsService.GetBanksAsync),
            ExpectedPathAndQuery = "/api/payparts/v1/banks/info",
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.PayParts.GetBanksAsync(token)
        },
        new()
        {
            OperationId = "resendPayPartsCallback",
            Method = "POST",
            PathTemplate = "/api/payparts/v1/callback/resend",
            Group = "PayParts",
            ServiceInterface = typeof(IPayPartsService),
            ServiceMethod = nameof(IPayPartsService.ResendCallbackAsync),
            ExpectedPathAndQuery = "/api/payparts/v1/callback/resend",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op29-payparts-resend-callback\""],
            InvokeAsync = (host, token) => host.PayParts.ResendCallbackAsync(
                new PayPartsResendCallbackRequest { ExternalId = "op29-payparts-resend-callback" },
                token)
        }
    ];

    // ===================== Alternative payments - 5 =====================

    private static OpenApiOperationContract[] AlternativePayments() =>
    [
        new()
        {
            // CreateOperationAsync is the member typed against the official response schema
            // (AlternativePaymentOperationResult); CreateAsync keeps the older loosely typed response.
            OperationId = "createAlternativePayment",
            Method = "POST",
            PathTemplate = "/api/alternative-payments/v1/create",
            Group = "Alternative payments",
            ServiceInterface = typeof(IAlternativePaymentService),
            ServiceMethod = nameof(IAlternativePaymentService.CreateOperationAsync),
            ExpectedPathAndQuery = "/api/alternative-payments/v1/create",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op30-alternative-create\"", "\"amount\":130.30"],
            InvokeAsync = (host, token) => host.AlternativePayments.CreateOperationAsync(
                new CreateAlternativePaymentRequest
                {
                    Amount = 130.30m,
                    Currency = "UAH",
                    ExternalId = "op30-alternative-create",
                    Provider = AlternativePaymentProvider.Imoje
                },
                token)
        },
        new()
        {
            OperationId = "alternativePaymentRefund",
            Method = "POST",
            PathTemplate = "/api/alternative-payments/v1/refund",
            Group = "Alternative payments",
            ServiceInterface = typeof(IAlternativePaymentService),
            ServiceMethod = nameof(IAlternativePaymentService.RefundAsync),
            ExpectedPathAndQuery = "/api/alternative-payments/v1/refund",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op31-alternative-refund\"", "\"amount\":131.31"],
            InvokeAsync = (host, token) => host.AlternativePayments.RefundAsync(
                new RefundAlternativePaymentRequest { ExternalId = "op31-alternative-refund", Amount = 131.31m },
                token)
        },
        new()
        {
            // The canonical two-argument overload. The one-argument overload targets the legacy
            // /api/alternative-payments/v1/operation/{externalId} route.
            OperationId = "alternativePaymentOperationInfo",
            Method = "GET",
            PathTemplate = "/api/alternative-payments/v1/info/operation",
            Group = "Alternative payments",
            ServiceInterface = typeof(IAlternativePaymentService),
            ServiceMethod = nameof(IAlternativePaymentService.GetOperationInfoAsync),
            ExpectedPathAndQuery =
                "/api/alternative-payments/v1/info/operation?external_id=" + "op32-alternative-external" +
                HostileEncodedSuffix + "&operation_id=" + "op32-alternative-operation" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.AlternativePayments.GetOperationInfoAsync(
                Raw("op32-alternative-external"),
                Raw("op32-alternative-operation"),
                token)
        },
        new()
        {
            OperationId = "alternativePaymentRequestInfo",
            Method = "GET",
            PathTemplate = "/api/alternative-payments/v1/info",
            Group = "Alternative payments",
            ServiceInterface = typeof(IAlternativePaymentService),
            ServiceMethod = nameof(IAlternativePaymentService.GetInfoAsync),
            ExpectedPathAndQuery =
                "/api/alternative-payments/v1/info?external_id=" + "op33-alternative-info" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.AlternativePayments.GetInfoAsync(Raw("op33-alternative-info"), token)
        },
        new()
        {
            OperationId = "resendAlternativePaymentCallback",
            Method = "POST",
            PathTemplate = "/api/alternative-payments/v1/callback/resend",
            Group = "Alternative payments",
            ServiceInterface = typeof(IAlternativePaymentService),
            ServiceMethod = nameof(IAlternativePaymentService.ResendCallbackAsync),
            ExpectedPathAndQuery = "/api/alternative-payments/v1/callback/resend",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op34-alternative-resend-callback\""],
            InvokeAsync = (host, token) => host.AlternativePayments.ResendCallbackAsync(
                new ResendAlternativePaymentCallbackRequest { ExternalId = "op34-alternative-resend-callback" },
                token)
        }
    ];

    // ===================== Customer wallet - 6 =====================

    private static OpenApiOperationContract[] CustomerWallet() =>
    [
        new()
        {
            // EXP-355 canonical operation. DeletePaymentFromWalletAsync is the obsolete legacy member and
            // targets /api/customers/v1/{customerId}/cards/{cardId}, which the document does not publish.
            OperationId = "deleteCustomerPayment",
            Method = "DELETE",
            PathTemplate = "/api/customers/v1/wallet",
            Group = "Customer wallet",
            ServiceInterface = typeof(ICustomerService),
            ServiceMethod = nameof(ICustomerService.DeleteCustomerPaymentAsync),
            ExpectedPathAndQuery = "/api/customers/v1/wallet?external_id=" + "op35-wallet-delete" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"option_id\":\"op35-wallet-option\""],
            InvokeAsync = (host, token) => host.Customers.DeleteCustomerPaymentAsync(
                Raw("op35-wallet-delete"),
                new DeleteCustomerPaymentRequest { OptionId = "op35-wallet-option", Type = "card" },
                token)
        },
        new()
        {
            OperationId = "getCustomerInfoAndWallet",
            Method = "GET",
            PathTemplate = "/api/customers/v1/wallet",
            Group = "Customer wallet",
            ServiceInterface = typeof(ICustomerService),
            ServiceMethod = nameof(ICustomerService.GetCustomerWalletAsync),
            ExpectedPathAndQuery = "/api/customers/v1/wallet?external_id=" + "op36-wallet-info" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.Customers.GetCustomerWalletAsync(Raw("op36-wallet-info"), token)
        },
        new()
        {
            OperationId = "addCustomerPayment",
            Method = "POST",
            PathTemplate = "/api/customers/v1/wallet",
            Group = "Customer wallet",
            ServiceInterface = typeof(ICustomerService),
            ServiceMethod = nameof(ICustomerService.AddCardToWalletAsync),
            ExpectedPathAndQuery = "/api/customers/v1/wallet?external_id=" + "op37-wallet-add" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = [$"\"number\":\"{CardNumberPlaceholder}-op37\""],
            InvokeAsync = (host, token) => host.Customers.AddCardToWalletAsync(
                Raw("op37-wallet-add"),
                new AddCardToWalletRequest
                {
                    Card = new WalletCardDetails
                    {
                        Number = $"{CardNumberPlaceholder}-op37",
                        ExpMonth = "12",
                        ExpYear = "2030",
                        Cvv = CardVerificationPlaceholder
                    }
                },
                token)
        },
        new()
        {
            OperationId = "getCustomerWalletItem",
            Method = "GET",
            PathTemplate = "/api/customers/v1/wallet/find",
            Group = "Customer wallet",
            ServiceInterface = typeof(ICustomerService),
            ServiceMethod = nameof(ICustomerService.GetWalletItemAsync),
            ExpectedPathAndQuery =
                "/api/customers/v1/wallet/find?external_id=" + "op38-wallet-find-customer" + HostileEncodedSuffix +
                "&option_id=" + "op38-wallet-find-option" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.Customers.GetWalletItemAsync(
                Raw("op38-wallet-find-customer"),
                Raw("op38-wallet-find-option"),
                token)
        },
        new()
        {
            OperationId = "getCustomerWalletConfirmationStatus",
            Method = "GET",
            PathTemplate = "/api/customers/v1/wallet/confirmation/status",
            Group = "Customer wallet",
            ServiceInterface = typeof(ICustomerService),
            ServiceMethod = nameof(ICustomerService.GetCardConfirmationStatusAsync),
            ExpectedPathAndQuery =
                "/api/customers/v1/wallet/confirmation/status?external_id=" + "op39-wallet-status-customer" +
                HostileEncodedSuffix + "&option_id=" + "op39-wallet-status-option" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.Customers.GetCardConfirmationStatusAsync(
                Raw("op39-wallet-status-customer"),
                Raw("op39-wallet-status-option"),
                token)
        },
        new()
        {
            OperationId = "setDefaultCard",
            Method = "POST",
            PathTemplate = "/api/customers/v1/wallet/settings/set",
            Group = "Customer wallet",
            ServiceInterface = typeof(ICustomerService),
            ServiceMethod = nameof(ICustomerService.SetDefaultCardAsync),
            ExpectedPathAndQuery =
                "/api/customers/v1/wallet/settings/set?external_id=" + "op40-wallet-default" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"card_id\":\"op40-wallet-card\""],
            InvokeAsync = (host, token) => host.Customers.SetDefaultCardAsync(
                Raw("op40-wallet-default"),
                new SetDefaultCardRequest { CardId = "op40-wallet-card" },
                token)
        }
    ];

    // ===================== Subscription plans and subscriptions - 14 =====================

    private static OpenApiOperationContract[] Subscriptions() =>
    [
        new()
        {
            OperationId = "getPlans",
            Method = "GET",
            PathTemplate = "/api/subscriptions/v1/plans",
            Group = "Subscriptions",
            ServiceInterface = typeof(ISubscriptionService),
            ServiceMethod = nameof(ISubscriptionService.GetPlansAsync),
            ExpectedPathAndQuery = "/api/subscriptions/v1/plans",
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.Subscriptions.GetPlansAsync(token)
        },
        new()
        {
            OperationId = "createPlan",
            Method = "POST",
            PathTemplate = "/api/subscriptions/v1/plans",
            Group = "Subscriptions",
            ServiceInterface = typeof(ISubscriptionService),
            ServiceMethod = nameof(ISubscriptionService.CreatePlanAsync),
            ExpectedPathAndQuery = "/api/subscriptions/v1/plans",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"name\":\"op42-plan-name\"", "\"amount\":142.42"],
            InvokeAsync = (host, token) => host.Subscriptions.CreatePlanAsync(
                new CreateSubscriptionPlanRequest
                {
                    Name = "op42-plan-name",
                    Amount = 142.42m,
                    Currency = "UAH",
                    Frequency = "monthly"
                },
                token)
        },
        new()
        {
            OperationId = "deactivatePlan",
            Method = "DELETE",
            PathTemplate = "/api/subscriptions/v1/plans/{plan_id}",
            Group = "Subscriptions",
            ServiceInterface = typeof(ISubscriptionService),
            ServiceMethod = nameof(ISubscriptionService.DeactivatePlanAsync),
            ExpectedPathAndQuery = "/api/subscriptions/v1/plans/" + "op43-plan-id" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.Subscriptions.DeactivatePlanAsync(Raw("op43-plan-id"), token)
        },
        new()
        {
            OperationId = "getPlan",
            Method = "GET",
            PathTemplate = "/api/subscriptions/v1/plans/{plan_id}",
            Group = "Subscriptions",
            ServiceInterface = typeof(ISubscriptionService),
            ServiceMethod = nameof(ISubscriptionService.GetPlanAsync),
            ExpectedPathAndQuery = "/api/subscriptions/v1/plans/" + "op44-plan-id" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.Subscriptions.GetPlanAsync(Raw("op44-plan-id"), token)
        },
        new()
        {
            OperationId = "updatePlan",
            Method = "PATCH",
            PathTemplate = "/api/subscriptions/v1/plans/{plan_id}",
            Group = "Subscriptions",
            ServiceInterface = typeof(ISubscriptionService),
            ServiceMethod = nameof(ISubscriptionService.UpdatePlanAsync),
            ExpectedPathAndQuery = "/api/subscriptions/v1/plans/" + "op45-plan-id" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"name\":\"op45-plan-name\""],
            InvokeAsync = (host, token) => host.Subscriptions.UpdatePlanAsync(
                Raw("op45-plan-id"),
                new UpdateSubscriptionPlanRequest { Name = "op45-plan-name" },
                token)
        },
        new()
        {
            OperationId = "createSubscription",
            Method = "POST",
            PathTemplate = "/api/subscriptions/v1/subscriptions",
            Group = "Subscriptions",
            ServiceInterface = typeof(ISubscriptionService),
            ServiceMethod = nameof(ISubscriptionService.CreateAsync),
            ExpectedPathAndQuery = "/api/subscriptions/v1/subscriptions",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op46-subscription-create\"", "\"amount\":146.46"],
            InvokeAsync = (host, token) => host.Subscriptions.CreateAsync(
                new CreateSubscriptionRequest
                {
                    ExternalId = "op46-subscription-create",
                    Amount = 146.46m,
                    Currency = "UAH",
                    Frequency = "monthly"
                },
                token)
        },
        new()
        {
            // EXP-355 canonical list operation. GetCustomerSubscriptionsAsync is the obsolete legacy
            // member and targets /api/subscriptions/v1/subscriptions/customer/{customerId}.
            OperationId = "getSubscriptions",
            Method = "GET",
            PathTemplate = "/api/subscriptions/v1/subscriptions",
            Group = "Subscriptions",
            ServiceInterface = typeof(ISubscriptionService),
            ServiceMethod = nameof(ISubscriptionService.GetSubscriptionsAsync),
            ExpectedPathAndQuery =
                "/api/subscriptions/v1/subscriptions?external_id=" + "op47-subscription-list" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) =>
                host.Subscriptions.GetSubscriptionsAsync(Raw("op47-subscription-list"), token)
        },
        new()
        {
            OperationId = "giftSubscription",
            Method = "POST",
            PathTemplate = "/api/subscriptions/v1/subscriptions/gift",
            Group = "Subscriptions",
            ServiceInterface = typeof(ISubscriptionService),
            ServiceMethod = nameof(ISubscriptionService.GiftAsync),
            ExpectedPathAndQuery = "/api/subscriptions/v1/subscriptions/gift",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"plan_id\":\"op48-gift-plan\"", "\"recurrent_id\":\"op48-gift-recurrent\""],
            InvokeAsync = (host, token) => host.Subscriptions.GiftAsync(
                new GiftSubscriptionRequest
                {
                    CallbackUrl = "https://merchant.invalid/op48/callback",
                    Customer = new SubscriptionCustomer { Email = "op48@merchant.invalid" },
                    PlanId = "op48-gift-plan",
                    RecurrentId = "op48-gift-recurrent",
                    ResultUrl = "https://merchant.invalid/op48/result",
                    StartDate = "2026-08-01"
                },
                token)
        },
        new()
        {
            OperationId = "deactivateSubscription",
            Method = "DELETE",
            PathTemplate = "/api/subscriptions/v1/subscriptions/{subscription_id}",
            Group = "Subscriptions",
            ServiceInterface = typeof(ISubscriptionService),
            ServiceMethod = nameof(ISubscriptionService.DeactivateAsync),
            ExpectedPathAndQuery =
                "/api/subscriptions/v1/subscriptions/" + "op49-subscription-id" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.Subscriptions.DeactivateAsync(Raw("op49-subscription-id"), token)
        },
        new()
        {
            OperationId = "getSubscription",
            Method = "GET",
            PathTemplate = "/api/subscriptions/v1/subscriptions/{subscription_id}",
            Group = "Subscriptions",
            ServiceInterface = typeof(ISubscriptionService),
            ServiceMethod = nameof(ISubscriptionService.GetAsync),
            ExpectedPathAndQuery =
                "/api/subscriptions/v1/subscriptions/" + "op50-subscription-id" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.Subscriptions.GetAsync(Raw("op50-subscription-id"), token)
        },
        new()
        {
            OperationId = "updateSubscription",
            Method = "PATCH",
            PathTemplate = "/api/subscriptions/v1/subscriptions/{subscription_id}",
            Group = "Subscriptions",
            ServiceInterface = typeof(ISubscriptionService),
            ServiceMethod = nameof(ISubscriptionService.UpdateAsync),
            ExpectedPathAndQuery =
                "/api/subscriptions/v1/subscriptions/" + "op51-subscription-id" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"amount\":151.51"],
            InvokeAsync = (host, token) => host.Subscriptions.UpdateAsync(
                Raw("op51-subscription-id"),
                new UpdateSubscriptionRequest { Amount = 151.51m },
                token)
        },
        new()
        {
            OperationId = "getSubscriptionPayments",
            Method = "GET",
            PathTemplate = "/api/subscriptions/v1/subscriptions/{subscription_id}/payments",
            Group = "Subscriptions",
            ServiceInterface = typeof(ISubscriptionService),
            ServiceMethod = nameof(ISubscriptionService.GetPaymentsAsync),
            ExpectedPathAndQuery =
                "/api/subscriptions/v1/subscriptions/" + "op52-subscription-id" + HostileEncodedSuffix + "/payments",
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.Subscriptions.GetPaymentsAsync(Raw("op52-subscription-id"), token)
        },
        new()
        {
            // EXP-355 canonical cancel: a DELETE with no body. The obsolete legacy CancelAsync member
            // POSTs a body the official operation does not declare.
            OperationId = "CancelCustomerSubscription",
            Method = "DELETE",
            PathTemplate = "/api/subscriptions/v1/subscriptions/{subscription_id}/cancel",
            Group = "Subscriptions",
            ServiceInterface = typeof(ISubscriptionService),
            ServiceMethod = nameof(ISubscriptionService.CancelCustomerSubscriptionAsync),
            ExpectedPathAndQuery =
                "/api/subscriptions/v1/subscriptions/" + "op53-subscription-id" + HostileEncodedSuffix + "/cancel",
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) =>
                host.Subscriptions.CancelCustomerSubscriptionAsync(Raw("op53-subscription-id"), token)
        },
        new()
        {
            OperationId = "UpdateSubscriptionPaymentMethod",
            Method = "PATCH",
            PathTemplate = "/api/subscriptions/v1/subscriptions/{subscription_id}/payment-method",
            Group = "Subscriptions",
            ServiceInterface = typeof(ISubscriptionService),
            ServiceMethod = nameof(ISubscriptionService.UpdatePaymentMethodAsync),
            ExpectedPathAndQuery =
                "/api/subscriptions/v1/subscriptions/" + "op54-subscription-id" + HostileEncodedSuffix +
                "/payment-method",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"token\":\"op54-cc-token\"", "\"type\":\"cc_token\""],
            InvokeAsync = (host, token) => host.Subscriptions.UpdatePaymentMethodAsync(
                Raw("op54-subscription-id"),
                new UpdateSubscriptionPaymentMethodRequest
                {
                    PaymentMethod = new SubscriptionPaymentMethodUpdate
                    {
                        Type = SubscriptionPaymentMethodUpdateType.CcToken,
                        CcToken = new CustomerCCTokenRequestPaymentMethod { Token = "op54-cc-token" }
                    }
                },
                token)
        }
    ];

    // ===================== Reports - 2 =====================

    private static OpenApiOperationContract[] Reports() =>
    [
        new()
        {
            OperationId = "getPaymentsRequest",
            Method = "POST",
            PathTemplate = "/api/reports/v1/payments",
            Group = "Reports",
            ServiceInterface = typeof(IReportService),
            ServiceMethod = nameof(IReportService.GetPaymentsReportAsync),
            ExpectedPathAndQuery = "/api/reports/v1/payments",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"date_from\":\"2026-07-01\"", "\"op55-report-field\""],
            InvokeAsync = (host, token) => host.Reports.GetPaymentsReportAsync(
                new PaymentsReportRequest
                {
                    DateFrom = ReportDateFrom,
                    DateTo = ReportDateTo,
                    Fields = ["op55-report-field"]
                },
                token)
        },
        new()
        {
            OperationId = "getTransactionsRequest",
            Method = "POST",
            PathTemplate = "/api/reports/v1/transactions",
            Group = "Reports",
            ServiceInterface = typeof(IReportService),
            ServiceMethod = nameof(IReportService.GetTransactionsReportAsync),
            ExpectedPathAndQuery = "/api/reports/v1/transactions",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"date_to\":\"2026-07-25\"", "\"op56-operation-type\""],
            InvokeAsync = (host, token) => host.Reports.GetTransactionsReportAsync(
                new TransactionsReportRequest
                {
                    DateFrom = ReportDateFrom,
                    DateTo = ReportDateTo,
                    OperationTypes = ["op56-operation-type"]
                },
                token)
        }
    ];

    // ===================== In-store payments - 4 =====================

    private static OpenApiOperationContract[] InStorePayments() =>
    [
        new()
        {
            OperationId = "createInStorePayment",
            Method = "POST",
            PathTemplate = "/api/in-store-payments/v1/create",
            Group = "In-store payments",
            ServiceInterface = typeof(IInStorePaymentService),
            ServiceMethod = nameof(IInStorePaymentService.CreateAsync),
            ExpectedPathAndQuery = "/api/in-store-payments/v1/create",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op57-in-store-create\"", "\"amount\":\"157.57\""],
            InvokeAsync = (host, token) => host.InStorePayments.CreateAsync(
                new InStorePaymentCreateRequest
                {
                    ExternalId = "op57-in-store-create",
                    PosTerminalId = "op57-pos-terminal",
                    TerminalSn = "op57-terminal-sn",
                    Amount = "157.57"
                },
                token)
        },
        new()
        {
            OperationId = "confirmInStorePayment",
            Method = "POST",
            PathTemplate = "/api/in-store-payments/v1/confirm",
            Group = "In-store payments",
            ServiceInterface = typeof(IInStorePaymentService),
            ServiceMethod = nameof(IInStorePaymentService.ConfirmAsync),
            ExpectedPathAndQuery = "/api/in-store-payments/v1/confirm",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments = ["\"external_id\":\"op58-in-store-confirm\"", "\"amount\":\"158.58\""],
            InvokeAsync = (host, token) => host.InStorePayments.ConfirmAsync(
                new InStorePaymentConfirmRequest
                {
                    ExternalId = "op58-in-store-confirm",
                    PosTerminalId = "op58-pos-terminal",
                    Amount = "158.58",
                    PosPaymentStatus = "success"
                },
                token)
        },
        new()
        {
            OperationId = "refundInStorePayment",
            Method = "POST",
            PathTemplate = "/api/in-store-payments/v1/refund",
            Group = "In-store payments",
            ServiceInterface = typeof(IInStorePaymentService),
            ServiceMethod = nameof(IInStorePaymentService.RefundAsync),
            ExpectedPathAndQuery = "/api/in-store-payments/v1/refund",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments =
                ["\"payment_external_id\":\"op59-in-store-payment\"", "\"refund_external_id\":\"op59-in-store-refund\""],
            InvokeAsync = (host, token) => host.InStorePayments.RefundAsync(
                new InStorePaymentRefundRequest
                {
                    PaymentExternalId = "op59-in-store-payment",
                    RefundExternalId = "op59-in-store-refund",
                    TerminalSn = "op59-terminal-sn",
                    Amount = "159.59",
                    PaymentSystem = "op59-payment-system",
                    PosTerminalId = "op59-pos-terminal",
                    CardNumber = $"{CardNumberPlaceholder}-op59",
                    BankAcquirer = "op59-bank-acquirer",
                    AuthorizationCode = "op59-authorization-code"
                },
                token)
        },
        new()
        {
            // The official operation is a POST that declares no request body: no content object at all,
            // and no downgrade to GET.
            OperationId = "getInStorePaymentInfo",
            Method = "POST",
            PathTemplate = "/api/in-store-payments/v1/info",
            Group = "In-store payments",
            ServiceInterface = typeof(IInStorePaymentService),
            ServiceMethod = nameof(IInStorePaymentService.GetInfoAsync),
            ExpectedPathAndQuery =
                "/api/in-store-payments/v1/info?external_id=" + "op60-in-store-info" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.InStorePayments.GetInfoAsync(Raw("op60-in-store-info"), token)
        }
    ];

    // ===================== Partner operations - 3 =====================

    private static OpenApiOperationContract[] Partners() =>
    [
        new()
        {
            OperationId = "feeDetails",
            Method = "GET",
            PathTemplate = "/api/partners/v1/fee-details",
            Group = "Partner operations",
            ServiceInterface = typeof(IPartnerService),
            ServiceMethod = nameof(IPartnerService.GetFeeDetailsAsync),
            ExpectedPathAndQuery =
                "/api/partners/v1/fee-details?merchant_project_id=" + "op61-merchant-project" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.Partners.GetFeeDetailsAsync(Raw("op61-merchant-project"), token)
        },
        new()
        {
            OperationId = "merchantStatus",
            Method = "GET",
            PathTemplate = "/api/partners/v1/merchant-status",
            Group = "Partner operations",
            ServiceInterface = typeof(IPartnerService),
            ServiceMethod = nameof(IPartnerService.GetMerchantStatusAsync),
            ExpectedPathAndQuery =
                "/api/partners/v1/merchant-status?merchant_project_id=" + "op62-merchant-project" +
                HostileEncodedSuffix + "&merchant_entity_id=" + "op62-merchant-entity" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.Partners.GetMerchantStatusAsync(
                new PartnerMerchantStatusOptions
                {
                    MerchantProjectId = Raw("op62-merchant-project"),
                    MerchantEntityId = Raw("op62-merchant-entity")
                },
                token)
        },
        new()
        {
            OperationId = "transactionDetails",
            Method = "GET",
            PathTemplate = "/api/partners/v1/transaction-details",
            Group = "Partner operations",
            ServiceInterface = typeof(IPartnerService),
            ServiceMethod = nameof(IPartnerService.GetTransactionDetailsAsync),
            ExpectedPathAndQuery =
                "/api/partners/v1/transaction-details?merchant_entity_id=" + "op63-merchant-entity" +
                HostileEncodedSuffix + "&merchant_order_id=" + "op63-merchant-order" + HostileEncodedSuffix +
                "&unified_external_id=" + "op63-unified-external" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.Partners.GetTransactionDetailsAsync(
                Raw("op63-merchant-entity"),
                new PartnerTransactionDetailsOptions
                {
                    MerchantOrderId = Raw("op63-merchant-order"),
                    UnifiedExternalId = Raw("op63-unified-external")
                },
                token)
        }
    ];

    // ===================== Merchant, payment instructions and FinMon - 4 =====================

    private static OpenApiOperationContract[] MerchantInstructionsAndFinMon() =>
    [
        new()
        {
            OperationId = "validateMerchantKeys",
            Method = "GET",
            PathTemplate = "/api/merchants/v1/me",
            Group = "Merchant, payment instructions and FinMon",
            ServiceInterface = typeof(IMerchantService),
            ServiceMethod = nameof(IMerchantService.GetInfoAsync),
            ExpectedPathAndQuery = "/api/merchants/v1/me",
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.Merchants.GetInfoAsync(token)
        },
        new()
        {
            OperationId = "createPaymentInstructions",
            Method = "POST",
            PathTemplate = "/api/payment-instructions/v1/new",
            Group = "Merchant, payment instructions and FinMon",
            ServiceInterface = typeof(IPaymentInstructionService),
            ServiceMethod = nameof(IPaymentInstructionService.CreateAsync),
            ExpectedPathAndQuery = "/api/payment-instructions/v1/new",
            Body = ContractBodyPolicy.Json,
            Auth = ContractAuthPolicy.Authenticated,
            ExpectedBodyFragments =
                ["\"batch_external_id\":\"op65-instruction-batch\"", "\"external_id\":\"op65-instruction-order\""],
            InvokeAsync = (host, token) => host.PaymentInstructions.CreateAsync(
                new CreatePaymentInstructionsRequest
                {
                    ProcessingType = PaymentInstructionProcessingType.CardPay,
                    Method = PaymentInstructionMethod.Purchase,
                    Currency = "UAH",
                    BatchExternalId = "op65-instruction-batch",
                    Orders =
                    [
                        new PaymentInstructionOrder
                        {
                            ApiKey = "op65-instruction-order-key-placeholder",
                            Amount = 165.65m,
                            ExternalId = "op65-instruction-order"
                        }
                    ]
                },
                token)
        },
        new()
        {
            // The single anonymous operation. It runs over the dedicated non-redirecting client, carries
            // no credential-bearing header, sends no content, and returns the Location of a 302 without
            // ever requesting the redirect target.
            OperationId = "declinePaymentInstruction",
            Method = "GET",
            PathTemplate = "/api/payment-instructions/v1/decline",
            Group = "Merchant, payment instructions and FinMon",
            ServiceInterface = typeof(IPaymentInstructionService),
            ServiceMethod = nameof(IPaymentInstructionService.DeclineAsync),
            ExpectedPathAndQuery =
                "/api/payment-instructions/v1/decline?project_id=" + "op66-decline-project" + HostileEncodedSuffix +
                "&payment_instruction_id=" + "op66-decline-instruction" + HostileEncodedSuffix,
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Anonymous,
            Response = ContractResponseKind.Redirect,
            InvokeAsync = (host, token) => host.PaymentInstructions.DeclineAsync(
                Raw("op66-decline-project"),
                Raw("op66-decline-instruction"),
                token)
        },
        new()
        {
            // recipient_ipn is an integer on the SDK surface, so there is no caller string to make
            // hostile. The value is unique to this row instead.
            OperationId = "getP2PLimits",
            Method = "GET",
            PathTemplate = "/api/finmon/v1/p2p-payment/pre-limits",
            Group = "Merchant, payment instructions and FinMon",
            ServiceInterface = typeof(IFinMonService),
            ServiceMethod = nameof(IFinMonService.GetRulesAsync),
            ExpectedPathAndQuery = "/api/finmon/v1/p2p-payment/pre-limits?recipient_ipn=1670000067",
            Body = ContractBodyPolicy.None,
            Auth = ContractAuthPolicy.Authenticated,
            InvokeAsync = (host, token) => host.FinMon.GetRulesAsync(1670000067, token)
        }
    ];
}
