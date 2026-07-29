using System.Reflection;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// No published schema declares a field the SDK cannot receive.
/// </summary>
/// <remarks>
/// <para>
/// This is the coarse net under the per-body parity tests. Those compare a handful of request bodies
/// exactly, in both directions. This one sweeps every schema the SDK has a same-named type for and
/// asserts only that nothing declared is missing — a field the provider sends and the SDK has nowhere
/// to put is data the caller silently never sees.
/// </para>
/// <para>
/// It deliberately does not fail on extra properties. Many are inherited from a base class shared with
/// another schema, and a few are convenience members; removing them is a separate judgement per type,
/// whereas a missing field is unambiguous. The exact-match rule belongs to the reconciled list, which
/// is where request bodies are held to it.
/// </para>
/// <para>
/// Schemas are matched to types by name, the way the document and the SDK were written to correspond.
/// A schema with no same-named type is not checked here — that gap is what
/// <c>EnumWireTokenTests</c> covers for enums and what the reconciled list covers for bodies.
/// </para>
/// </remarks>
public class ModelFieldCoverageTests
{
    /// <summary>
    /// Declared fields the SDK deliberately does not carry, and why.
    /// </summary>
    private static readonly Dictionary<string, string> AcceptedGaps = new()
    {
        ["CreatePaymentRequest.campaign_name"] =
            "the operation POST /api/payments/v1/new takes CreatePaymentRequestDev, which does not "
            + "declare campaign_name; the similarly named schema it appears on is referenced by nothing",
    };

    [Fact]
    public void NoPublishedSchema_ShouldDeclareAFieldTheSdkCannotReceive()
    {
        Dictionary<string, string[]> sdkProperties = SdkModelProperties();
        List<string> gaps = [];

        foreach ((string schemaName, IReadOnlyCollection<string> declared) in OpenApiSnapshot.SchemaPropertyNames())
        {
            if (!sdkProperties.TryGetValue(schemaName, out string[]? modelled))
            {
                continue;
            }

            foreach (string field in declared.Except(modelled, StringComparer.Ordinal).Order(StringComparer.Ordinal))
            {
                string key = $"{schemaName}.{field}";
                if (!AcceptedGaps.ContainsKey(key))
                {
                    gaps.Add(key);
                }
            }
        }

        Assert.Empty(gaps);
    }

    /// <summary>
    /// Every accepted gap still exists. A stale exemption hides the field it was written to excuse.
    /// </summary>
    [Fact]
    public void EveryAcceptedGap_ShouldStillBeAGap()
    {
        Dictionary<string, string[]> sdkProperties = SdkModelProperties();
        List<string> stale = [];

        foreach (string key in AcceptedGaps.Keys)
        {
            string[] parts = key.Split('.', 2);
            if (sdkProperties.TryGetValue(parts[0], out string[]? modelled) && modelled.Contains(parts[1]))
            {
                stale.Add(key);
            }
        }

        Assert.Empty(stale);
    }

    private static Dictionary<string, string[]> SdkModelProperties()
    {
        return typeof(SYT.RozetkaPay.RozetkaPayClient).Assembly
            .GetExportedTypes()
            .Where(static type => type.IsClass && !type.IsAbstract && type.Namespace?.Contains(".Models") == true)
            .GroupBy(static type => type.Name, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.First()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(static property => property.GetCustomAttribute<JsonIgnoreAttribute>() is null)
                    .Select(static property =>
                        property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);
    }
}
