namespace SYT.RozetkaPay.Services;

/// <summary>
/// Obsoletion texts for operations RozetkaPay removed from its public OpenAPI document.
/// </summary>
/// <remarks>
/// <para>
/// On 2026-09-30 the document at <c>https://docs.rozetkapay.com/openapi.json</c> stopped publishing seven
/// operations: the three partner reads (<c>feeDetails</c>, <c>merchantStatus</c>, <c>transactionDetails</c>)
/// and the four in-store operations (<c>createInStorePayment</c>, <c>confirmInStorePayment</c>,
/// <c>getInStorePaymentInfo</c>, <c>refundInStorePayment</c>). Their tags went with them.
/// </para>
/// <para>
/// The SDK keeps the methods, byte-for-byte, and marks them obsolete rather than deleting them. A route
/// leaving the <i>public</i> document is not proof it stopped answering: partner and in-store access is
/// provisioned per account, and an integrator with that access would otherwise lose a working call in a
/// package upgrade with no replacement. What the SDK can no longer do is check these calls against a
/// published contract, and the warning says exactly that.
/// </para>
/// <para>
/// All seven share one diagnostic ID, so a consumer who has confirmed access with RozetkaPay can suppress
/// precisely this warning (<c>&lt;NoWarn&gt;$(NoWarn);RZPAY001&lt;/NoWarn&gt;</c>) without silencing every
/// other <c>CS0618</c>.
/// </para>
/// </remarks>
internal static class RemovedFromPublicDocument
{
    /// <summary>Diagnostic ID reported instead of <c>CS0618</c> for every operation listed here.</summary>
    internal const string DiagnosticId = "RZPAY001";

    /// <summary>Where the diagnostic points.</summary>
    internal const string UrlFormat =
        "https://github.com/i7aket/SYT.RozetkaPay/blob/main/CHANGELOG.md#800---2026-09-30";

    /// <summary>Message for the three partner reads.</summary>
    internal const string PartnerOperation =
        "Removed from the public RozetkaPay OpenAPI on 2026-09-30 (GET /api/partners/v1/*). The SDK still " +
        "sends the same request for accounts with partner access, but it is no longer checked against a " +
        "published contract. Confirm availability with RozetkaPay before relying on it.";

    /// <summary>Message for the four in-store operations.</summary>
    internal const string InStoreOperation =
        "Removed from the public RozetkaPay OpenAPI on 2026-09-30 (POST /api/in-store-payments/v1/*). The SDK " +
        "still sends the same request for accounts with in-store access, but it is no longer checked against " +
        "a published contract. Confirm availability with RozetkaPay before relying on it.";
}
