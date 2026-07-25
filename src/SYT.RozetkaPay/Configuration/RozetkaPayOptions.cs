using System.ComponentModel.DataAnnotations;

namespace SYT.RozetkaPay.Configuration;

/// <summary>
/// Strongly typed RozetkaPay SDK settings, bound from the <c>RozetkaPay</c> configuration section or
/// configured in code, and validated before the application starts serving traffic.
/// </summary>
/// <remarks>
/// <para>
/// Register the options with one of the <c>AddRozetkaPay</c> overloads. Validation runs through the standard
/// options pipeline — <see cref="ValidationAttribute"/> rules plus a cross-field validator — and is checked
/// at startup, so a broken configuration fails immediately instead of at the first payment request. An
/// invalid configuration surfaces as
/// <see cref="Microsoft.Extensions.Options.OptionsValidationException"/>; its message names the offending
/// setting and never echoes a credential.
/// </para>
/// <para>
/// The value resolved by <c>Microsoft.Extensions.Options.IOptions&lt;RozetkaPayOptions&gt;</c> is a stable
/// snapshot taken once per application. Changing the underlying configuration file at run time does not
/// re-configure the SDK: credentials, the HTTP client, and the webhook verifier are all built from that one
/// snapshot, and rotating them requires a restart.
/// </para>
/// </remarks>
public sealed class RozetkaPayOptions
{
    /// <summary>
    /// Configuration section the SDK binds these options from.
    /// </summary>
    public const string SectionName = "RozetkaPay";

    /// <summary>
    /// Production endpoint, the <see cref="RozetkaPayEnvironment.Production"/> server published by the
    /// official RozetkaPay OpenAPI document.
    /// </summary>
    public const string ProductionBaseUrl = "https://api.rozetkapay.com";

    /// <summary>
    /// Sandbox endpoint, the <see cref="RozetkaPayEnvironment.Sandbox"/> (development) server published by
    /// the official RozetkaPay OpenAPI document.
    /// </summary>
    public const string SandboxBaseUrl = "https://api-epdev.rozetkapay.com";

    /// <summary>
    /// API login used for basic authentication. Required.
    /// </summary>
    [Required]
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// API password used for basic authentication, and the key RozetkaPay signs callbacks with. Required.
    /// Keep it out of source control: use user secrets, environment variables, or a secret store.
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Environment to talk to. Defaults to <see cref="RozetkaPayEnvironment.Production"/> and selects the
    /// endpoint unless <see cref="BaseUrl"/> is set.
    /// </summary>
    public RozetkaPayEnvironment Environment { get; set; } = RozetkaPayEnvironment.Production;

    /// <summary>
    /// Explicit API base URL, overriding the endpoint of <see cref="Environment"/>. Leave it
    /// <see langword="null"/> — the default — to use the endpoint of the selected environment; set it only
    /// to reach a private gateway, a proxy, or a local test server. Must be an absolute
    /// <c>http</c> or <c>https</c> URL; an empty or whitespace value is a configuration error rather than
    /// "not set".
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Optional <c>X-ON-BEHALF-OF</c> header for partnership mode, where one core account operates with
    /// several children.
    /// </summary>
    public string? OnBehalfOf { get; set; }

    /// <summary>
    /// Optional <c>X-CUSTOMER-AUTH</c> header carrying the RID personal token that grants access to a
    /// customer's wallet.
    /// </summary>
    public string? CustomerAuth { get; set; }

    /// <summary>
    /// HTTP timeout for API requests. Must be greater than zero. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// User agent sent with every request. Defaults to <c>RozetkaPaySDK/.NET</c>.
    /// </summary>
    public string UserAgent { get; set; } = "RozetkaPaySDK/.NET";

    /// <summary>
    /// Retry policy for failed HTTP requests. Retries are disabled by default.
    /// </summary>
    public RetryPolicy RetryPolicy { get; set; } = RetryPolicy.Default;
}
