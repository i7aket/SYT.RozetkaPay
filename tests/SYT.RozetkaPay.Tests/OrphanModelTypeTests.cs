using System.Reflection;
using System.Text.Json;
using SYT.RozetkaPay.Models.Payments;
using Xunit;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// A public model type must be reachable: either the SDK uses it, or the document declares it, or it is a
/// deliberate consumer-facing shape named below. Anything else is a promise to callers that nothing keeps
/// (EXP-420).
/// </summary>
/// <remarks>
/// <para>
/// This exists because the cleanup it followed could not have been done safely without it. Two of the
/// removals in EXP-420 were wrong on the first pass and were only caught by tests: <c>RefundPPayRequest</c>
/// is declared under <c>components.requestBodies</c> (not <c>schemas</c>), and <c>FeeDetailsResponse</c> /
/// <c>TransactionDetailsListResponse</c> are pinned by an existing test that says removing them would break
/// compiled consumers. Both facts are invisible to a name-based sweep, so this gate checks <b>every</b>
/// component section and honours the pins.
/// </para>
/// <para>
/// The gate also earns its keep on second-order orphans: removing a parent leaves its child types
/// unreferenced. The first EXP-420 pass created six such orphans, and only re-running the detection found
/// them.
/// </para>
/// </remarks>
public class OrphanModelTypeTests
{
    /// <summary>
    /// Types that are unreferenced <b>by design</b>: the caller is the consumer, so nothing inside the SDK
    /// can point at them. Each entry is a decision, not an exemption — adding one means claiming a caller
    /// needs it.
    /// </summary>
    private static readonly HashSet<string> DeliberatelyConsumerFacing = new(StringComparer.Ordinal)
    {
        // Callers deserialize an inbound webhook into this. Nothing in the SDK references it *because*
        // the caller is on the receiving end (see EXP-441, which added EventKey for dedup).
        nameof(PaymentWebhook),

        // Applied as [ExplicitFullRefund] — the C# attribute shorthand drops the suffix, which is exactly
        // the blind spot a name-based sweep has. It is used; the sweep just cannot see it.
        "ExplicitFullRefundAttribute",
    };

    [Fact]
    public void EveryPublicModelType_ShouldBeReachable()
    {
        HashSet<string> declared = DeclaredComponentNames();
        HashSet<string> referenced = ReferencedByLibrary();

        List<string> unreachable = ModelTypes()
            .Select(type => type.Name)
            .Where(name => !DeliberatelyConsumerFacing.Contains(name))
            .Where(name => !declared.Contains(name))
            .Where(name => !referenced.Contains(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            unreachable.Count == 0,
            "These public model types are referenced by nothing in the SDK and declared by no component in "
            + "the document. Either wire them up, or delete them, or — if a caller genuinely needs one — add "
            + "it to DeliberatelyConsumerFacing with the reason:\n  "
            + string.Join("\n  ", unreachable));
    }

    /// <summary>
    /// The allow-list must stay honest: an entry for a type that no longer exists is a stale exemption that
    /// would silently widen the gate for a future type of the same name.
    /// </summary>
    [Fact]
    public void TheConsumerFacingAllowList_ShouldNotNameTypesThatAreGone()
    {
        HashSet<string> existing = ModelTypes().Select(type => type.Name).ToHashSet(StringComparer.Ordinal);

        List<string> stale = DeliberatelyConsumerFacing
            .Where(name => !existing.Contains(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(stale.Count == 0, "Allow-list names types that no longer exist: " + string.Join(", ", stale));
    }

    private static IEnumerable<Type> ModelTypes() =>
        typeof(PaymentWebhook).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace?.StartsWith("SYT.RozetkaPay.Models", StringComparison.Ordinal) == true)
            .Where(type => !type.IsNested);

    /// <summary>
    /// Every name the document declares, across <b>all</b> component sections — not just
    /// <c>components.schemas</c>. Missing <c>requestBodies</c> is precisely how the first pass nearly deleted
    /// a declared request body.
    /// </summary>
    private static HashSet<string> DeclaredComponentNames()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "openapi.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));

        HashSet<string> names = new(StringComparer.Ordinal);
        if (!document.RootElement.TryGetProperty("components", out JsonElement components))
        {
            return names;
        }

        foreach (JsonProperty section in components.EnumerateObject())
        {
            foreach (JsonProperty entry in section.Value.EnumerateObject())
            {
                names.Add(entry.Name);
            }
        }

        return names;
    }

    /// <summary>
    /// Type names the library itself points at, read from metadata rather than from source text: a member's
    /// declared type, a generic argument, a base type, or a service parameter/return. Metadata cannot be
    /// fooled by a name that only appears inside a comment.
    /// </summary>
    private static HashSet<string> ReferencedByLibrary()
    {
        HashSet<string> referenced = new(StringComparer.Ordinal);

        foreach (Type type in typeof(PaymentWebhook).Assembly.GetTypes())
        {
            if (type.BaseType is { } baseType)
            {
                AddType(referenced, baseType);
            }

            foreach (PropertyInfo property in type.GetProperties(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                if (property.DeclaringType == type)
                {
                    AddType(referenced, property.PropertyType);
                }
            }

            foreach (MethodInfo method in type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                if (method.DeclaringType != type)
                {
                    continue;
                }

                AddType(referenced, method.ReturnType);

                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    AddType(referenced, parameter.ParameterType);
                }
            }

            foreach (CustomAttributeData attribute in type.GetCustomAttributesData())
            {
                AddType(referenced, attribute.AttributeType);
            }

            foreach (MemberInfo member in type.GetMembers(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                foreach (CustomAttributeData attribute in member.GetCustomAttributesData())
                {
                    AddType(referenced, attribute.AttributeType);
                }
            }
        }

        return referenced;
    }

    /// <summary>
    /// Records a type and everything it wraps: <c>Task&lt;List&lt;Plan&gt;&gt;</c> must count as a reference
    /// to <c>Plan</c>, otherwise every collection-returning operation would look like it references nothing.
    /// A type never counts as referencing itself — that is the whole point of the check.
    /// </summary>
    private static void AddType(HashSet<string> referenced, Type type)
    {
        Type current = type;

        if (current.IsByRef || current.IsPointer || current.IsArray)
        {
            current = current.GetElementType() ?? current;
        }

        Type? nullable = Nullable.GetUnderlyingType(current);
        if (nullable is not null)
        {
            current = nullable;
        }

        referenced.Add(current.Name);

        if (current.IsGenericType)
        {
            foreach (Type argument in current.GetGenericArguments())
            {
                AddType(referenced, argument);
            }
        }
    }
}
