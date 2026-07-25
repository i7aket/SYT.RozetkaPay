using Microsoft.Extensions.DependencyInjection;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Extensions;
using SYT.RozetkaPay.Models.Merchants;
using SYT.RozetkaPay.Services;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// The one test in this repository that talks to a real RozetkaPay environment: a single read-only call to
/// the sandbox, opt-in through environment variables.
/// </summary>
/// <remarks>
/// <para>
/// This is deliberately not part of the 67-operation coverage. Most published operations create, confirm,
/// cancel, refund, or pay out real money, and calling them against a shared sandbox would leave provider-side
/// state behind. The deterministic 67/67 coverage lives in <see cref="OpenApiOperationContractTests"/>; the
/// only thing proven here is that the credentials, endpoint selection, TLS, and authentication headers
/// actually work end to end against RozetkaPay.
/// </para>
/// <para>
/// The operation used - <c>validateMerchantKeys</c>, <c>GET /api/merchants/v1/me</c> - exists precisely to be
/// called this way: it reads back the identity of the calling merchant and changes nothing.
/// </para>
/// <para>
/// Without both environment variables the test is skipped with a stated reason and makes no network call. It
/// therefore never fails an ordinary build, and never reports a pass it did not earn. Run it explicitly:
/// </para>
/// <code>
/// ROZETKAPAY_SANDBOX_LOGIN='&lt;login&gt;' \
/// ROZETKAPAY_SANDBOX_PASSWORD='&lt;password&gt;' \
/// dotnet test tests/SYT.RozetkaPay.Tests/SYT.RozetkaPay.Tests.csproj -c Release --filter 'Category=Sandbox'
/// </code>
/// </remarks>
public class SandboxSmokeTests
{
    /// <summary>
    /// Bound on the single live call. Long enough for a cold TLS handshake to a remote host, short enough
    /// that an unreachable provider fails the run instead of hanging it.
    /// </summary>
    private static readonly TimeSpan LiveTimeout = TimeSpan.FromSeconds(25);

    /// <summary>
    /// Calls the merchant identity endpoint against the real sandbox and asserts a typed response came back.
    /// </summary>
    /// <remarks>
    /// Read-only, one attempt, no retry loop, and no fallback to production: <see cref="RetryPolicy.None"/>
    /// plus an explicit <see cref="RozetkaPayEnvironment.Sandbox"/>. The assertion is deliberately minimal -
    /// that a typed response deserialized at all - because every richer invariant would be a claim about one
    /// merchant account's data rather than about the SDK.
    /// </remarks>
    [SandboxFact]
    [Trait("Category", "Sandbox")]
    public async Task ValidateMerchantKeys_ShouldAnswerOverTheLiveSandbox()
    {
        // Resolved through the supported DI/options route rather than an ad-hoc URL, so this exercises the
        // endpoint selection a consumer actually gets.
        ServiceCollection services = new();
        services.AddRozetkaPay(options =>
        {
            options.Login = Environment.GetEnvironmentVariable(SandboxFactAttribute.LoginVariableName)!;
            options.Password = Environment.GetEnvironmentVariable(SandboxFactAttribute.PasswordVariableName)!;
            options.Environment = RozetkaPayEnvironment.Sandbox;
            options.Timeout = LiveTimeout;
            options.RetryPolicy = RetryPolicy.None;
        });

        await using ServiceProvider provider =
            services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope scope = provider.CreateScope();

        // The endpoint is the official sandbox constant, checked before anything is sent. Only the base URL
        // is read; no credential is inspected.
        RozetkaPayConfiguration configuration =
            scope.ServiceProvider.GetRequiredService<RozetkaPayConfiguration>();
        Assert.Equal(RozetkaPayOptions.SandboxBaseUrl, configuration.BaseUrl);
        Assert.NotEqual(RozetkaPayOptions.ProductionBaseUrl, configuration.BaseUrl);

        IMerchantService merchants = scope.ServiceProvider.GetRequiredService<IMerchantService>();

        using CancellationTokenSource timeout = new(LiveTimeout);

        // A provider authentication or contract failure is allowed to surface as the SDK's own exception. It
        // is not caught and re-wrapped here: the SDK's messages are already free of credentials, and adding a
        // custom diagnostic is how a credential ends up in CI output.
        MerchantValidationResponse response = await merchants.GetInfoAsync(timeout.Token);

        // Typed, non-null, and nothing about it is printed.
        Assert.NotNull(response);
    }
}

/// <summary>
/// Deterministic coverage of the skip mechanism itself. These cases carry no <c>Category=Sandbox</c> trait:
/// they must run in ordinary CI, because "the live test is correctly skipped" is exactly the property that
/// stops absent credentials from silently becoming a green live check.
/// </summary>
public class SandboxSkipBehaviorTests
{
    public static TheoryData<string?, string?> IncompleteCredentialPairs =>
        new()
        {
            { null, null },
            { "present", null },
            { null, "present" },
            { string.Empty, "present" },
            { "present", string.Empty },
            { "   ", "present" },
            { "present", "   " }
        };

    [Theory]
    [MemberData(nameof(IncompleteCredentialPairs))]
    public void ResolveSkipReason_ShouldSkip_WhenEitherVariableIsAbsentOrBlank(string? login, string? password)
    {
        Assert.Equal(
            SandboxFactAttribute.MissingCredentialsSkipReason,
            SandboxFactAttribute.ResolveSkipReason(login, password));
    }

    [Fact]
    public void ResolveSkipReason_ShouldNotSkip_WhenBothVariablesArePresent()
    {
        Assert.Null(SandboxFactAttribute.ResolveSkipReason("a-login", "a-password"));
    }

    [Fact]
    public void SkipReason_ShouldNameBothVariables_AndStateThatNothingWasCalled()
    {
        string reason = SandboxFactAttribute.MissingCredentialsSkipReason;

        Assert.Contains(SandboxFactAttribute.LoginVariableName, reason, StringComparison.Ordinal);
        Assert.Contains(SandboxFactAttribute.PasswordVariableName, reason, StringComparison.Ordinal);
        Assert.Contains("No network call was made.", reason, StringComparison.Ordinal);
    }

    [Fact]
    public void SkipReason_ShouldNotRevealWhichVariableIsMissingOrAnyValue()
    {
        // One reason for every incomplete combination: the text cannot depend on which half was supplied,
        // and it cannot contain a supplied value.
        const string secretish = "value-that-must-not-appear";

        foreach ((string? login, string? password) in
            new (string?, string?)[] { (secretish, null), (null, secretish), (secretish, "   ") })
        {
            string? reason = SandboxFactAttribute.ResolveSkipReason(login, password);

            Assert.Equal(SandboxFactAttribute.MissingCredentialsSkipReason, reason);
            Assert.DoesNotContain(secretish, reason, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SandboxVariableNames_ShouldBeExactlyTheDocumentedOnes()
    {
        // Pinned literally: the documented manual command and CI would silently stop opting in if a name
        // drifted, and the live check would look permanently skipped for no visible reason.
        Assert.Equal("ROZETKAPAY_SANDBOX_LOGIN", SandboxFactAttribute.LoginVariableName);
        Assert.Equal("ROZETKAPAY_SANDBOX_PASSWORD", SandboxFactAttribute.PasswordVariableName);
    }

    [Fact]
    public void SandboxFact_ShouldSkipOrRun_ConsistentlyWithTheCurrentEnvironment()
    {
        SandboxFactAttribute attribute = new();

        if (SandboxFactAttribute.CredentialsArePresent())
        {
            Assert.Null(attribute.Skip);
        }
        else
        {
            Assert.Equal(SandboxFactAttribute.MissingCredentialsSkipReason, attribute.Skip);
        }
    }
}
