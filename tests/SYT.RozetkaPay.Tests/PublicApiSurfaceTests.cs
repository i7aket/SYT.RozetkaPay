using System.Reflection;
using System.Text;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// The public surface of the package, written down, so that changing it is a decision rather than a
/// side effect.
/// </summary>
/// <remarks>
/// <para>
/// Nothing guarded this. No package validation, no API-compatibility baseline, no approved-surface
/// file — so removing a member, renaming one, or changing a parameter type went out in a green build.
/// This work removed and retyped a great deal on purpose; the point of writing the surface down now is
/// that the next such change has to be deliberate.
/// </para>
/// <para>
/// The baseline is the surface after this work rather than the one 1.0.0 published. Comparing against
/// 1.0.0 would report several hundred intentional differences and be switched off within a week, which
/// is how gates die.
/// </para>
/// <para>
/// When it fails, the diff in the assertion message is the change. Read it: if it is intended, run the
/// test with <c>APPROVE_PUBLIC_API=1</c> to rewrite the approved file, and the diff lands in the commit
/// where a reviewer can see it.
/// </para>
/// </remarks>
public class PublicApiSurfaceTests
{
    private const string ApprovalEnvironmentVariable = "APPROVE_PUBLIC_API";

    [Fact]
    public void PublicSurface_ShouldMatchTheApprovedBaseline()
    {
        string actual = DescribeSurface();
        string path = ApprovedSurfacePath();

        if (Environment.GetEnvironmentVariable(ApprovalEnvironmentVariable) == "1")
        {
            File.WriteAllText(path, actual);
        }

        Assert.True(File.Exists(path), $"Approved surface file is missing: {path}");

        string approved = File.ReadAllText(path).ReplaceLineEndings("\n");
        actual = actual.ReplaceLineEndings("\n");

        if (approved == actual)
        {
            return;
        }

        Assert.Fail(
            "The public surface differs from the approved baseline.\n\n" +
            Describe(approved, actual) +
            $"\nIf the change is intended, re-run with {ApprovalEnvironmentVariable}=1 and commit the " +
            "updated file so the diff is reviewable.");
    }

    /// <summary>
    /// A stable, sorted rendering of every exported type and member.
    /// </summary>
    /// <remarks>
    /// Sorted because reflection order is not stable across runtimes, and a baseline that reorders
    /// itself is a baseline nobody trusts. Nullability is not rendered: it is not part of the metadata
    /// this reads, and a half-true record is worse than an explicitly partial one.
    /// </remarks>
    private static string DescribeSurface()
    {
        Assembly assembly = typeof(SYT.RozetkaPay.RozetkaPayClient).Assembly;
        List<string> lines = [];

        foreach (Type type in assembly.GetExportedTypes().OrderBy(static t => t.FullName, StringComparer.Ordinal))
        {
            lines.Add(type.FullName!);

            IEnumerable<string> members = type
                .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Select(Describe)
                .OfType<string>()
                .Order(StringComparer.Ordinal);

            foreach (string member in members)
            {
                lines.Add("    " + member);
            }
        }

        return string.Join("\n", lines) + "\n";
    }

    private static string? Describe(MemberInfo member)
    {
        switch (member)
        {
            case MethodInfo method when !method.IsSpecialName:
                string parameters = string.Join(
                    ", ",
                    method.GetParameters().Select(static p => $"{Name(p.ParameterType)} {p.Name}"));
                return $"{Name(method.ReturnType)} {method.Name}({parameters})";

            case PropertyInfo property:
                string accessors = (property.CanRead ? "get;" : string.Empty)
                    + (property.CanWrite ? " set;" : string.Empty);
                return $"{Name(property.PropertyType)} {property.Name} {{ {accessors.Trim()} }}";

            case FieldInfo field:
                return $"{Name(field.FieldType)} {field.Name}";

            case ConstructorInfo constructor:
                string arguments = string.Join(
                    ", ",
                    constructor.GetParameters().Select(static p => $"{Name(p.ParameterType)} {p.Name}"));
                return $".ctor({arguments})";

            default:
                return null;
        }
    }

    private static string Name(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return Name(underlying) + "?";
        }

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        string bare = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];

        return $"{bare}<{string.Join(", ", type.GetGenericArguments().Select(Name))}>";
    }

    private static string Describe(string approved, string actual)
    {
        HashSet<string> before = [.. approved.Split('\n')];
        HashSet<string> after = [.. actual.Split('\n')];

        StringBuilder report = new();
        foreach (string line in before.Except(after).Order(StringComparer.Ordinal).Take(40))
        {
            report.AppendLine($"  removed: {line.Trim()}");
        }

        foreach (string line in after.Except(before).Order(StringComparer.Ordinal).Take(40))
        {
            report.AppendLine($"  added  : {line.Trim()}");
        }

        return report.ToString();
    }

    private static string ApprovedSurfacePath()
    {
        // Written next to the sources rather than the test output, so approving a change produces a
        // file diff in the commit instead of a build artefact nobody sees.
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "PublicApiSurface.approved.txt"));
    }
}
