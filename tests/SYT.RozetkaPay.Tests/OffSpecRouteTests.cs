using System.Reflection;
using System.Text.RegularExpressions;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// The SDK calls no route the published document does not declare.
/// </summary>
/// <remarks>
/// <para>
/// Sixteen once did. Six were fallback targets for the hidden <c>404</c> retry EXP-385 deleted and
/// had gone uncalled since. The other ten were listed here awaiting confirmation, on the reading
/// that they might be earlier spellings of operations the document now publishes elsewhere.
/// </para>
/// <para>
/// They were not. Called against the live gateway with credentials that return <c>200</c> on a
/// declared control route, all ten answered <c>404</c> — and the POST-only ones answered <c>404</c>
/// rather than <c>405</c>, so it is the path that is absent, not the verb. A public method that
/// cannot succeed is worse than a missing one: it reads as a supported operation. All ten are gone,
/// along with the request and response types that existed only to feed them.
/// </para>
/// <para>
/// The list below is now empty and should stay so. Adding an entry means shipping a method the
/// document does not back, which is the decision this file exists to make visible.
/// </para>
/// <para>
/// The drift job checks the snapshot against the live document. This checks the SDK against the
/// snapshot in the one direction that job cannot see: routes the SDK invents.
/// </para>
/// </remarks>
public class OffSpecRouteTests
{
    /// <summary>
    /// Routes the SDK calls that the document does not declare, awaiting confirmation from RozetkaPay.
    /// </summary>
    /// <remarks>
    /// Deliberately a literal list. Deriving it would make it agree with whatever the services happen
    /// to do, which is the failure mode that let five contract-test fixtures pin the defects they were
    /// meant to catch.
    /// </remarks>
    private static readonly HashSet<string> AwaitingConfirmation = [];

    [Fact]
    public void TheSdk_ShouldCallNoUndeclaredRouteBeyondTheOnesAwaitingConfirmation()
    {
        HashSet<string> declared = [.. OpenApiSnapshot.DeclaredPaths()];

        List<string> undeclared = [.. RouteConstants()
            .Where(route => !declared.Contains(route))
            .Where(route => !AwaitingConfirmation.Contains(route))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(undeclared);
    }

    /// <summary>
    /// Every route awaiting confirmation is still called. One that is not has been resolved and the
    /// entry should go, rather than sitting here implying an open question.
    /// </summary>
    [Fact]
    public void EveryRouteAwaitingConfirmation_ShouldStillBeCalled()
    {
        HashSet<string> called = [.. RouteConstants()];

        List<string> stale = [.. AwaitingConfirmation.Where(route => !called.Contains(route)).Order(StringComparer.Ordinal)];

        Assert.Empty(stale);
    }

    [Fact]
    public void NoRouteAwaitingConfirmation_ShouldAlreadyBeDeclared()
    {
        // If the document starts declaring one, the question is answered and the entry is noise.
        HashSet<string> declared = [.. OpenApiSnapshot.DeclaredPaths()];

        List<string> resolved = [.. AwaitingConfirmation.Where(declared.Contains).Order(StringComparer.Ordinal)];

        Assert.Empty(resolved);
    }

    /// <summary>
    /// Every literal request target the services hold, read out of the compiled constants.
    /// </summary>
    /// <remarks>
    /// Templates carrying a placeholder are skipped: those are log labels, never sent as written.
    /// </remarks>
    private static IEnumerable<string> RouteConstants()
    {
        return typeof(BaseService).Assembly
            .GetTypes()
            .Where(static type => type.Namespace == "SYT.RozetkaPay.Services")
            .SelectMany(static type => type.GetFields(
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            .Where(static field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(static field => (string?)field.GetRawConstantValue())
            .Where(static value => value is not null && value.StartsWith("/api/", StringComparison.Ordinal))
            .Select(static value => value!)
            .Where(static value => !value.Contains('{', StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal);
    }
}
