namespace SYT.RozetkaPay.Tests.TestInfrastructure;

/// <summary>
/// Routes the SDK still calls although the pinned document stopped publishing them on 2026-09-30.
/// </summary>
/// <remarks>
/// <para>
/// A literal list, like every other exemption in this suite: deriving it from the services would make it agree
/// with whatever they happen to call. The route gates (<c>OffSpecRouteTests</c>, <c>DispatchedRouteTests</c>)
/// exclude exactly these, and nothing else; <c>OpenApiOperationContractTests</c> proves the list equals the
/// retired manifest rows, and that every one of them is served only by an obsolete member.
/// </para>
/// <para>
/// This is not an "awaiting confirmation" list. The routes were published and working until 2026-09-30, and
/// they are kept for accounts that have partner or in-store access. The list must never grow: a route that is
/// neither declared nor retired here is a route the SDK invented.
/// </para>
/// </remarks>
internal static class RetiredRoutes
{
    /// <summary><c>VERB /path</c> of each retired operation, mapped to its former operationId.</summary>
    internal static IReadOnlyDictionary<string, string> ByMethodAndPath { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["POST /api/in-store-payments/v1/create"] = "createInStorePayment",
            ["POST /api/in-store-payments/v1/confirm"] = "confirmInStorePayment",
            ["POST /api/in-store-payments/v1/refund"] = "refundInStorePayment",
            ["POST /api/in-store-payments/v1/info"] = "getInStorePaymentInfo",
            ["GET /api/partners/v1/fee-details"] = "feeDetails",
            ["GET /api/partners/v1/merchant-status"] = "merchantStatus",
            ["GET /api/partners/v1/transaction-details"] = "transactionDetails"
        };

    /// <summary>The distinct paths of <see cref="ByMethodAndPath"/>.</summary>
    internal static IReadOnlySet<string> Paths { get; } =
        ByMethodAndPath.Keys.Select(static key => key.Split(' ', 2)[1]).ToHashSet(StringComparer.Ordinal);
}
