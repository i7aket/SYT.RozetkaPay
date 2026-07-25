namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Marks a test that talks to the real RozetkaPay sandbox, and skips it unless both sandbox environment
/// variables are present.
/// </summary>
/// <remarks>
/// <para>
/// Absent credentials produce an explicit skip with a stated reason - never a silent pass, and never a
/// failure of an ordinary build. A test that quietly succeeded because it never ran would be the worst of
/// the three outcomes: it would report live coverage that does not exist.
/// </para>
/// <para>
/// Only the presence of the two variables is ever examined. Neither value is read into a field, rendered
/// into the skip reason, or compared against anything, and the reason does not say which of the two is
/// missing - that alone would leak which half of a credential pair an environment holds.
/// </para>
/// <para>
/// Presence is decided once, at test discovery. Setting the variables inside an already-running test process
/// does not un-skip anything; export them before starting the run, as
/// <c>src/SYT.RozetkaPay/docs/API_COMPATIBILITY.md</c> documents.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SandboxFactAttribute : FactAttribute
{
    /// <summary>Environment variable holding the sandbox API login.</summary>
    public const string LoginVariableName = "ROZETKAPAY_SANDBOX_LOGIN";

    /// <summary>Environment variable holding the sandbox API password.</summary>
    public const string PasswordVariableName = "ROZETKAPAY_SANDBOX_PASSWORD";

    /// <summary>
    /// Reason reported when the live check is skipped. It names the two variables so the run is actionable,
    /// states plainly that nothing was called, and reveals nothing about either value.
    /// </summary>
    public const string MissingCredentialsSkipReason =
        $"Requires {LoginVariableName} and {PasswordVariableName}. No network call was made.";

    /// <summary>
    /// Creates the attribute, skipping the test when either variable is absent.
    /// </summary>
    public SandboxFactAttribute()
    {
        Skip = ResolveSkipReason(
            Environment.GetEnvironmentVariable(LoginVariableName),
            Environment.GetEnvironmentVariable(PasswordVariableName));
    }

    /// <summary>
    /// The skip reason for a given pair of environment-variable readings, or <see langword="null"/> when the
    /// live check may run.
    /// </summary>
    /// <remarks>
    /// Split out from the constructor so the decision can be tested for every combination without mutating
    /// the environment of a running test process - which would be visible to tests running in parallel.
    /// Whitespace counts as absent: a variable exported empty is a misconfiguration, and treating it as a
    /// credential would send a blank login to the provider.
    /// </remarks>
    /// <param name="login">Value of <see cref="LoginVariableName"/>, or <see langword="null"/>.</param>
    /// <param name="password">Value of <see cref="PasswordVariableName"/>, or <see langword="null"/>.</param>
    internal static string? ResolveSkipReason(string? login, string? password)
    {
        bool present = !string.IsNullOrWhiteSpace(login) && !string.IsNullOrWhiteSpace(password);

        return present ? null : MissingCredentialsSkipReason;
    }

    /// <summary>
    /// Whether both sandbox variables are present in this process. Reads presence only.
    /// </summary>
    internal static bool CredentialsArePresent()
    {
        return ResolveSkipReason(
            Environment.GetEnvironmentVariable(LoginVariableName),
            Environment.GetEnvironmentVariable(PasswordVariableName)) is null;
    }
}
