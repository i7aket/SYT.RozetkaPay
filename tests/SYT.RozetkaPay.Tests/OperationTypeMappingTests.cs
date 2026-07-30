using System.Reflection;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Every operation the SDK exposes is declared, and carries the request and response types the
/// document declares for it.
/// </summary>
/// <remarks>
/// <para>
/// This is the direction every existing gate leaves open, and the gap is structural rather than an
/// oversight in any one test. <c>ModelFieldCoverageTests</c>, <c>UndeclaredPropertyTests</c> and
/// <c>PropertyTypeParityTests</c> all match an SDK type to a schema <em>of the same name</em> and
/// silently skip a type whose name has no counterpart. <c>OpenApiOperationManifest</c> maps declared
/// operations to service methods and so proves every declared operation is reachable — but never the
/// converse, that every reachable method is declared. And <c>OffSpecRouteTests</c> reflects over
/// string constants only, skipping any value containing a placeholder.
/// </para>
/// <para>
/// Between those three blind spots, whole model families written against superseded documentation
/// passed 1607 tests while failing on the first real call. The audits that found them reached the
/// live gateway; these tests reach the same conclusions from the document, so a build catches them.
/// </para>
/// <para>
/// The checks here are deliberately static. Invoking each operation against a recording handler was
/// the obvious alternative and is worse: it needs a plausible request for every operation, which
/// means encoding the very field knowledge under test, and a method that throws during validation
/// would silently drop out of coverage.
/// </para>
/// </remarks>
public class OperationTypeMappingTests
{
    /// <summary>
    /// Service-contract methods that dispatch no HTTP request, so map to no operation.
    /// </summary>
    private static readonly Dictionary<string, string> NotAnOperation = new(StringComparer.Ordinal);

    /// <summary>
    /// Every method on a service contract corresponds to an operation the document declares.
    /// </summary>
    /// <remarks>
    /// A method with no manifest entry is a method whose route nothing has checked. Five such
    /// methods reached production in 2.0.0, all building their target by string interpolation so
    /// that <c>OffSpecRouteTests</c> could not see them, and all answering <c>404</c> live.
    /// </remarks>
    [Fact]
    public void EveryServiceOperation_ShouldMapToADeclaredOperation()
    {
        HashSet<string> mapped =
        [
            .. OpenApiOperationManifest.All.Select(
                static contract => $"{contract.ServiceInterface.Name}.{contract.ServiceMethod}"),
        ];

        List<string> unmapped = [.. ServiceMethods()
            .Select(static entry => $"{entry.Contract.Name}.{entry.Method.Name}")
            .Where(key => !mapped.Contains(key))
            .Where(static key => !NotAnOperation.ContainsKey(key))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];

        AssertAgainstBaseline("unmapped-operation", unmapped);
    }

    /// <summary>
    /// Every operation the manifest claims is declared really is declared, path and verb both.
    /// </summary>
    [Fact]
    public void EveryMappedOperation_ShouldBeDeclaredInTheDocument()
    {
        HashSet<string> declared = [.. OpenApiSnapshot.DeclaredOperations()
            .Select(static operation => $"{operation.Method} {operation.Path}")];

        List<string> missing = [.. OpenApiOperationManifest.All
            .Select(static contract => $"{contract.Method.ToUpperInvariant()} {contract.PathTemplate}")
            .Where(key => !declared.Contains(key))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];

        Assert.Empty(missing);
    }

    /// <summary>
    /// An operation's return type can receive every field the document declares in its response.
    /// </summary>
    /// <remarks>
    /// Coverage, not equality — a response model may carry more, and several legitimately do. What
    /// it may not do is omit a declared field, because then the provider sends something the caller
    /// has nowhere to read. <c>is_success</c> on the four money operations was exactly this: the
    /// authoritative outcome flag, declared, and unreachable through the returned type.
    /// </remarks>
    [Fact]
    public void EveryOperationReturnType_ShouldReceiveEveryDeclaredResponseField()
    {
        List<string> gaps = [];

        foreach (var contract in OpenApiOperationManifest.All)
        {
            MethodInfo? method = Resolve(contract.ServiceInterface, contract.ServiceMethod);
            if (method is null)
            {
                continue;
            }

            Type returned = UnwrapTask(method.ReturnType);
            IReadOnlyCollection<string> declared =
                OpenApiSnapshot.ResponseFieldsOf(contract.Method, contract.PathTemplate);

            if (declared.Count == 0)
            {
                continue;
            }

            HashSet<string> modelled = [.. WireNames(returned)];

            gaps.AddRange(declared
                .Where(field => !modelled.Contains(field))
                .Select(field =>
                    $"{contract.OperationId} -> {returned.Name} cannot receive '{field}'"));
        }

        AssertAgainstBaseline("response-field", gaps);
    }

    /// <summary>
    /// An operation's request type sends exactly the fields the document declares — no more.
    /// </summary>
    /// <remarks>
    /// Exact in the sending direction, because the two failure modes are both silent. A field the
    /// document does not declare is discarded by the provider, so the caller's intent vanishes; a
    /// declared field the type cannot express is a request the caller cannot make.
    /// </remarks>
    [Fact]
    public void EveryOperationRequestType_ShouldSendExactlyTheDeclaredFields()
    {
        List<string> mismatches = [];

        foreach (var contract in OpenApiOperationManifest.All)
        {
            MethodInfo? method = Resolve(contract.ServiceInterface, contract.ServiceMethod);
            if (method is null)
            {
                continue;
            }

            IReadOnlyCollection<string> declared =
                OpenApiSnapshot.RequestFieldsOf(contract.Method, contract.PathTemplate);

            if (declared.Count == 0)
            {
                continue;
            }

            ParameterInfo? body = method.GetParameters().FirstOrDefault(static parameter =>
                parameter.ParameterType.Namespace?.Contains(".Models", StringComparison.Ordinal) == true);

            if (body is null)
            {
                mismatches.Add(
                    $"{contract.OperationId} declares a request body and {contract.ServiceMethod} "
                    + "takes no model parameter");
                continue;
            }

            HashSet<string> modelled = [.. WireNames(body.ParameterType)];

            mismatches.AddRange(declared
                .Where(field => !modelled.Contains(field))
                .Select(field => $"{contract.OperationId} -> {body.ParameterType.Name} cannot send '{field}'"));

            mismatches.AddRange(modelled
                .Where(field => !declared.Contains(field))
                .Select(field =>
                    $"{contract.OperationId} -> {body.ParameterType.Name} sends undeclared '{field}'"));
        }

        AssertAgainstBaseline("request-field", mismatches);
    }

    /// <summary>
    /// Every exemption names a method that still exists and still sends nothing.
    /// </summary>
    [Fact]
    public void EveryExemption_ShouldStillNameARealMethod()
    {
        HashSet<string> present = [.. ServiceMethods()
            .Select(static entry => $"{entry.Contract.Name}.{entry.Method.Name}")];

        List<string> stale = [.. NotAnOperation.Keys
            .Where(key => !present.Contains(key))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(stale);
    }

    /// <summary>
    /// The overload to judge an operation by, when a contract declares several of the same name.
    /// </summary>
    /// <remarks>
    /// The richest overload, because a narrower one is a convenience over the same operation and
    /// omitting a declared field there is a deliberate default rather than a gap. Deterministic by
    /// parameter count so the result does not depend on reflection order.
    /// </remarks>
    private static MethodInfo? Resolve(Type contract, string name)
    {
        return contract
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => string.Equals(method.Name, name, StringComparison.Ordinal))
            .OrderByDescending(static method => method.GetParameters().Length)
            .FirstOrDefault();
    }


    /// <summary>
    /// Compares a section against the recorded baseline of divergences known to exist today.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The honest alternative was to assert empty and leave the suite red, which nobody can work
    /// with, or to skip the checks until the fixes land, which is how a gate never arrives. So the
    /// current divergences are written down instead, in a file a reviewer can read.
    /// </para>
    /// <para>
    /// The comparison is exact in both directions, and that is the point. A new divergence fails
    /// because it is not in the file; a fixed one also fails, because the line must be deleted when
    /// the defect is. A baseline that only guards against growth quietly stops describing reality.
    /// </para>
    /// </remarks>
    private static void AssertAgainstBaseline(string section, IEnumerable<string> actual)
    {
        string[] found = [.. actual.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
        string path = BaselinePath();

        if (Environment.GetEnvironmentVariable("APPROVE_DIVERGENCES") == "1")
        {
            Rewrite(path, section, found);
        }

        string[] expected = [.. Section(path, section)];

        string[] appeared = [.. found.Except(expected, StringComparer.Ordinal)];
        string[] resolved = [.. expected.Except(found, StringComparer.Ordinal)];

        Assert.True(
            appeared.Length == 0,
            $"New contract divergences in '{section}' that the baseline does not record:\n  "
            + string.Join("\n  ", appeared));

        Assert.True(
            resolved.Length == 0,
            $"These '{section}' divergences are fixed — delete them from "
            + $"{Path.GetFileName(path)} in the same commit:\n  "
            + string.Join("\n  ", resolved));
    }

    private static IEnumerable<string> Section(string path, string section)
    {
        bool inside = false;

        foreach (string line in File.ReadLines(path))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                inside = string.Equals(line[3..].Trim(), section, StringComparison.Ordinal);
                continue;
            }

            if (inside && line.Length > 0 && !line.StartsWith('#'))
            {
                yield return line;
            }
        }
    }

    private static void Rewrite(string path, string section, string[] found)
    {
        List<string> output = [];
        bool inside = false, written = false;

        foreach (string line in File.ReadLines(path))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                inside = string.Equals(line[3..].Trim(), section, StringComparison.Ordinal);
                output.Add(line);

                if (inside)
                {
                    output.AddRange(found);
                    written = true;
                }

                continue;
            }

            if (!inside)
            {
                output.Add(line);
            }
        }

        if (!written)
        {
            output.Add(string.Empty);
            output.Add($"## {section}");
            output.AddRange(found);
        }

        File.WriteAllLines(path, output);
    }

    private static string BaselinePath()
    {
        // Beside the sources, so approving a change lands as a reviewable diff rather than a build
        // artefact nobody sees - the same reasoning as PublicApiSurface.approved.txt.
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "KnownContractDivergences.txt"));
    }

    private static IEnumerable<(Type Contract, MethodInfo Method)> ServiceMethods()
    {
        return typeof(BaseService).Assembly
            .GetExportedTypes()
            .Where(static type => type.IsInterface && type.Namespace == "SYT.RozetkaPay.Services")
            .SelectMany(static type => type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(static method => !method.IsSpecialName)
                .Select(method => (Contract: type, Method: method)));
    }

    private static Type UnwrapTask(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>)
            ? type.GetGenericArguments()[0]
            : type;
    }

    /// <summary>
    /// The wire names a type can carry, following the collection element when it is a list.
    /// </summary>
    /// <remarks>
    /// A wrapper around a single collection is followed too. Several operations return a root JSON
    /// array which the SDK maps onto a named wrapper through a converter -
    /// <c>SubscriptionListJsonConverter</c> is the worked example. That is a deliberate design, not a
    /// defect, but comparing the wrapper's one property against the array element's fields reported
    /// every element field as missing: 34 for <c>getSubscriptions</c> alone, all of them false. The
    /// element is what the document describes, so the element is what gets compared.
    /// </remarks>
    private static IEnumerable<string> WireNames(Type type)
    {
        Type resolved = Nullable.GetUnderlyingType(type) ?? type;

        if (resolved.IsGenericType && resolved.GetGenericTypeDefinition() == typeof(List<>))
        {
            resolved = resolved.GetGenericArguments()[0];
        }

        PropertyInfo[] declared = resolved
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(static property => property.GetCustomAttribute<JsonIgnoreAttribute>() is null)
            .ToArray();

        if (declared.Length == 1 &&
            declared[0].PropertyType.IsGenericType &&
            declared[0].PropertyType.GetGenericTypeDefinition() == typeof(List<>))
        {
            resolved = declared[0].PropertyType.GetGenericArguments()[0];
        }

        return resolved
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(static property => property.GetCustomAttribute<JsonIgnoreAttribute>() is null)
            .Select(static property =>
                property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name);
    }
}
