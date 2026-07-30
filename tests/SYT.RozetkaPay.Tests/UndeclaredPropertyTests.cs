using System.Reflection;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// A model that mirrors a schema declares no property of its own that the schema does not.
/// </summary>
/// <remarks>
/// <para>
/// This is the direction <c>ModelFieldCoverageTests</c> leaves open. That test checks only that
/// nothing declared is missing, and says in its own remarks that extras are excluded because many
/// are inherited from a shared base and removing them is a judgement per type. Fair at the time —
/// but nothing ever made the judgement, and the extras accumulated behind it.
/// </para>
/// <para>
/// The severity depends on which way the data flows. On a <strong>request</strong> model an
/// undeclared property is a trap: the caller sets it, the SDK serializes it, the provider drops a
/// field it does not declare, and the intent vanishes with no error anywhere. Every one removed here
/// sat beside the declared field it shadowed, under a name that read like the simpler way to do the
/// same thing — <c>apple_pay_token</c> next to <c>apple_pay</c>, <c>card_number</c> next to
/// <c>cc_number</c>, <c>card</c> next to <c>cc_token</c>. On a <strong>response</strong> model it is
/// misleading rather than harmful: the provider never sends the field, so the property is
/// permanently null and a caller branching on it writes dead code.
/// </para>
/// <para>
/// The rule is deliberately about properties a type <em>declares itself</em>. Inherited ones are a
/// different defect with a different remedy, and mixing them would have meant either exempting
/// dozens of entries — which hollows out the gate — or restructuring the class hierarchy inside a
/// change about undeclared fields. See
/// <see cref="EveryRemainingExtra_ShouldBeInherited"/>.
/// </para>
/// </remarks>
public class UndeclaredPropertyTests
{
    /// <summary>
    /// Undeclared properties a type declares itself and is allowed to keep, each with its reason.
    /// </summary>
    /// <remarks>
    /// Response-side only, and the second test enforces that. An undeclared request property
    /// silently discards what the caller asked for; no reason is good enough to leave one, so the
    /// list cannot hold one. These remain because the document is known to under-document response
    /// bodies, and deleting a property the provider does in fact send would lose data — the opposite
    /// mistake, and a quieter one. Each needs a live call to settle.
    /// </remarks>
    private static readonly Dictionary<string, string> AcceptedExtras = new(StringComparer.Ordinal)
    {
        // Both of these mirror a component under components.responses that the operations reaching
        // them no longer exist for: EXP-403 removed /api/alternative-payments/v1/operations after the
        // live gateway answered 404. Whether the types themselves should go is EXP-420's question,
        // not this one, and deleting fields from a type that is itself under review would only make
        // that harder to judge.
        ["AlternativePaymentOperationResult.amount"] = "response-side; type pending review under EXP-420",
        ["AlternativePaymentOperationResult.created_at"] = "response-side; type pending review under EXP-420",
        ["AlternativePaymentOperationResult.currency"] = "response-side; type pending review under EXP-420",
        ["AlternativePaymentOperationResult.payment_method"] = "response-side; type pending review under EXP-420",
        ["AlternativePaymentOperationResult.processed_at"] = "response-side; type pending review under EXP-420",
        ["AlternativePaymentOperationResult.status"] = "response-side; type pending review under EXP-420",
        ["AlternativePaymentOperationsResult.count"] = "response-side; type pending review under EXP-420",
        ["AlternativePaymentOperationsResult.operations"] = "response-side; type pending review under EXP-420",
        ["AlternativePaymentOperationsResult.total"] = "response-side; type pending review under EXP-420",

        // The rest are read off responses. The document under-documents response bodies — that is how
        // GetBanksAsync and GetPlansAsync shipped expecting a wrapper where the API returns a bare
        // array — so removing a property the provider does send would lose data silently, the
        // opposite mistake and a quieter one. Each needs one live call to settle.
        ["CardItemDetails.brand"] =
            "response-side; sits beside the declared payment_system and may be the provider's own "
            + "spelling of it — a live card lookup settles which",
        ["FeeDetails.rate"] =
            "response-side; the schema declares amount and currency only, but a rate is exactly the "
            + "kind of field an under-documented fee body carries",
        ["PaymentReceiptResult.receipt"] = "response-side; awaiting a live receipt call to confirm",
        ["PaymentSearchList.count"] = "response-side; awaiting a live search call to confirm",
        ["PaymentSearchList.total"] = "response-side; awaiting a live search call to confirm",
        ["PaymentStatusResult.processed_at"] = "response-side; awaiting a live status call to confirm",
        ["PaymentStatusResult.status"] = "response-side; awaiting a live status call to confirm",
        ["SetDefaultCardResponse.default_card_id"] = "response-side; awaiting a live set-default call to confirm",
        ["SetDefaultCardResponse.message"] = "response-side; awaiting a live set-default call to confirm",
        ["SetDefaultCardResponse.status"] = "response-side; awaiting a live set-default call to confirm",
        ["ResultPaymentMethod.payment_system"] = "response-side; awaiting a live payment result to confirm",
        ["ResultPaymentMethod.title"] = "response-side; awaiting a live payment result to confirm",

        // These six are declared, but on the other half of the pair: the five below belong to
        // SubscriptionPaymentDetails and SubscriptionPaymentDetails.id belongs to SubscriptionPayment.
        // The SDK put each on both. Whether the provider flattens the details onto the parent needs
        // one live subscription payment to answer, and until then removing either copy risks
        // dropping the one the provider actually sends.
        ["SubscriptionPayment.amount"] = "response-side; declared on the sibling SubscriptionPaymentDetails",
        ["SubscriptionPayment.created_at"] = "response-side; declared on the sibling SubscriptionPaymentDetails",
        ["SubscriptionPayment.currency"] = "response-side; declared on the sibling SubscriptionPaymentDetails",
        ["SubscriptionPayment.processed_at"] = "response-side; declared on the sibling SubscriptionPaymentDetails",
        ["SubscriptionPayment.status"] = "response-side; declared on the sibling SubscriptionPaymentDetails",
        ["SubscriptionPaymentDetails.id"] = "response-side; declared on the parent SubscriptionPayment",
    };

    /// <summary>
    /// SDK types the document spreads across more than one schema.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Matching a type to the schema of the same name is how the SDK and the document were written
    /// to correspond, and it holds almost everywhere. <c>CustomerInfo</c> is the exception: the
    /// <c>customer</c> field of <c>CreatePaymentRequestDev</c> refs <c>CustomerRequestUserDetails</c>
    /// (sixteen properties), while a separate and much smaller <c>CustomerInfo</c> schema also
    /// exists. One SDK type carries both shapes.
    /// </para>
    /// <para>
    /// Comparing against either alone produces false positives in the dangerous direction. Against
    /// <c>CustomerInfo</c>, five declared request fields — <c>account_number</c>, <c>color_mode</c>,
    /// <c>fingerprint</c>, <c>locale</c>, <c>payment_method</c> — look invented; against
    /// <c>CustomerRequestUserDetails</c>, <c>browser_user_agent</c> does. Deleting any of them would
    /// have removed a working field. The declared surface is the union.
    /// </para>
    /// </remarks>
    private static readonly Dictionary<string, string[]> SchemaAliases = new(StringComparer.Ordinal)
    {
        ["CustomerInfo"] = ["CustomerInfo", "CustomerRequestUserDetails"],
    };

    [Fact]
    public void NoModel_ShouldDeclareAPropertyItsSchemaDoesNotDeclare()
    {
        Dictionary<string, HashSet<string>> declared = DeclaredProperties();

        List<string> extras = [.. OwnProperties()
            .Where(entry => IsModelled(entry.Schema, declared))
            .Where(entry => !DeclaredFor(entry.Schema, declared).Contains(entry.Field))
            .Select(static entry => $"{entry.Schema}.{entry.Field}")
            .Where(static key => !AcceptedExtras.ContainsKey(key))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(extras);
    }

    /// <summary>
    /// Nothing on the request side may be exempted.
    /// </summary>
    /// <remarks>
    /// This is what keeps the list above a record of open questions rather than a way around the
    /// gate: an entry has to name a schema no request body can reach.
    /// </remarks>
    [Fact]
    public void NoAcceptedExtra_ShouldBeOnARequestModel()
    {
        HashSet<string> requestSchemas = [.. OpenApiSnapshot.RequestReachableSchemas()];

        List<string> offenders = [.. AcceptedExtras.Keys
            .Where(key => SchemaNamesOf(key.Split('.', 2)[0]).Any(requestSchemas.Contains))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(offenders);
    }

    /// <summary>
    /// Every exemption still describes a real extra. A stale one hides the property it excuses.
    /// </summary>
    [Fact]
    public void EveryAcceptedExtra_ShouldStillBeAnExtra()
    {
        Dictionary<string, HashSet<string>> declared = DeclaredProperties();
        HashSet<string> own = [.. OwnProperties().Select(static entry => $"{entry.Schema}.{entry.Field}")];

        List<string> stale = [.. AcceptedExtras.Keys
            .Where(key =>
            {
                string[] parts = key.Split('.', 2);
                bool present = own.Contains(key);
                bool undeclared = !DeclaredFor(parts[0], declared).Contains(parts[1]);

                return !present || !undeclared;
            })
            .Order(StringComparer.Ordinal)];

        Assert.Empty(stale);
    }

    /// <summary>
    /// Every exemption says why. An entry with no reason is an entry nobody can review.
    /// </summary>
    [Fact]
    public void EveryAcceptedExtra_ShouldCarryAReason()
    {
        Assert.Empty(AcceptedExtras
            .Where(static entry => string.IsNullOrWhiteSpace(entry.Value))
            .Select(static entry => entry.Key)
            .Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// Undeclared properties that reach a model through a base class rather than its own declaration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These are real — an <c>AlternativePaymentProduct</c> does serialize <c>url</c> and
    /// <c>image</c>, and its schema declares neither — but the remedy is to make the C# hierarchy
    /// compose the way the document does, which is a change to the shape of the model layer rather
    /// than a field to delete. EXP-423 covers it.
    /// </para>
    /// <para>
    /// The claim is machine-checked rather than asserted: this test proves every remaining extra
    /// really is inherited. If someone adds an invented property to a derived type, it appears in
    /// <see cref="NoModel_ShouldDeclareAPropertyItsSchemaDoesNotDeclare"/> instead, where there is
    /// no exemption to hide behind.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryRemainingExtra_ShouldBeInherited()
    {
        Dictionary<string, HashSet<string>> declared = DeclaredProperties();
        HashSet<string> own = [.. OwnProperties().Select(static entry => $"{entry.Schema}.{entry.Field}")];

        // Everything the SDK would actually serialize, inherited members included, that the document
        // does not declare and no exemption covers. Each must have arrived through a base class —
        // anything a type declares itself belongs to the strict rule above, not here.
        List<string> selfDeclared = [.. AllProperties()
            .Where(entry => IsModelled(entry.Schema, declared))
            .Where(entry => !DeclaredFor(entry.Schema, declared).Contains(entry.Field))
            .Select(static entry => $"{entry.Schema}.{entry.Field}")
            .Where(static key => !AcceptedExtras.ContainsKey(key))
            .Where(own.Contains)
            .Order(StringComparer.Ordinal)];

        Assert.Empty(selfDeclared);
    }

    /// <summary>
    /// What the document declares for each name, merged across every component section.
    /// </summary>
    /// <remarks>
    /// <c>SchemaPropertyNames</c> walks <c>schemas</c>, <c>requestBodies</c> and <c>responses</c>,
    /// so one name can arrive several times with different property sets — a request body and the
    /// schema it wraps, for instance. Taking any single entry as the declared surface reports the
    /// others' fields as undeclared. The union is what the document declares under that name.
    /// </remarks>
    private static Dictionary<string, HashSet<string>> DeclaredProperties()
    {
        Dictionary<string, HashSet<string>> merged = new(StringComparer.Ordinal);

        foreach ((string name, IReadOnlyCollection<string> properties) in OpenApiSnapshot.SchemaPropertyNames())
        {
            if (!merged.TryGetValue(name, out HashSet<string>? fields))
            {
                fields = new HashSet<string>(StringComparer.Ordinal);
                merged[name] = fields;
            }

            fields.UnionWith(properties);
        }

        return merged;
    }

    /// <summary>
    /// Everything the document declares for a type, across every schema it is spread over.
    /// </summary>
    private static HashSet<string> DeclaredFor(
        string type, Dictionary<string, HashSet<string>> declared)
    {
        HashSet<string> union = new(StringComparer.Ordinal);

        foreach (string schema in SchemaAliases.TryGetValue(type, out string[]? names) ? names : [type])
        {
            if (declared.TryGetValue(schema, out HashSet<string>? fields))
            {
                union.UnionWith(fields);
            }
        }

        return union;
    }

    /// <summary>
    /// Whether the document models a type at all, under any of its names.
    /// </summary>
    private static bool IsModelled(string type, Dictionary<string, HashSet<string>> declared) =>
        (SchemaAliases.TryGetValue(type, out string[]? names) ? names : [type]).Any(declared.ContainsKey);

    /// <summary>
    /// Every schema name a type is compared against.
    /// </summary>
    private static IEnumerable<string> SchemaNamesOf(string type) =>
        SchemaAliases.TryGetValue(type, out string[]? names) ? names : [type];

    private static IEnumerable<(string Schema, string Field)> OwnProperties() =>
        ModelProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

    private static IEnumerable<(string Schema, string Field)> AllProperties() =>
        ModelProperties(BindingFlags.Public | BindingFlags.Instance);

    private static IEnumerable<(string Schema, string Field)> ModelProperties(BindingFlags binding)
    {
        return typeof(SYT.RozetkaPay.RozetkaPayClient).Assembly
            .GetExportedTypes()
            .Where(static type => type.IsClass && !type.IsAbstract && type.Namespace?.Contains(".Models") == true)
            .GroupBy(static type => type.Name, StringComparer.Ordinal)
            .SelectMany(group => group.First()
                .GetProperties(binding)
                .Where(static property => property.GetCustomAttribute<JsonIgnoreAttribute>() is null)
                .Select(property => (
                    Schema: group.Key,
                    Field: property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name))
                .Distinct());
    }
}
