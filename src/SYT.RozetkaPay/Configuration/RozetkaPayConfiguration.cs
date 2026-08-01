namespace SYT.RozetkaPay.Configuration;

/// <summary>
/// Configuration for RozetkaPay API client
/// </summary>
public class RozetkaPayConfiguration
{
    /// <summary>
    /// Base URL for RozetkaPay API
    /// Production: https://api.rozetkapay.com
    /// Development: https://api-epdev.rozetkapay.com
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.rozetkapay.com";

    /// <summary>
    /// API login/username for basic authentication
    /// </summary>
    public required string Login { get; set; }

    /// <summary>
    /// API password for basic authentication
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// Optional X-ON-BEHALF-OF header for partnership mode
    /// Used when one core account operates with several children
    /// </summary>
    public string? OnBehalfOf { get; set; }

    /// <summary>
    /// Optional X-CUSTOMER-AUTH header for customer authentication
    /// RID personal auth token to access customer's wallet
    /// </summary>
    public string? CustomerAuth { get; set; }

    /// <summary>
    /// HTTP timeout for API requests (default: 30 seconds)
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// User agent string for HTTP requests
    /// </summary>
    public string UserAgent { get; set; } = "RozetkaPaySDK/.NET";

    /// <summary>
    /// Which endpoint schemes the SDK will speak. Defaults to
    /// <see cref="RozetkaPayTransportSecurity.HttpsOnly"/>. See
    /// <see cref="RozetkaPayOptions.TransportSecurity"/> for what it does and does not relax.
    /// </summary>
    public RozetkaPayTransportSecurity TransportSecurity { get; set; } = RozetkaPayTransportSecurity.HttpsOnly;

    /// <summary>
    /// Retry policy for failed HTTP requests
    /// </summary>
    public RetryPolicy RetryPolicy { get; set; } = RetryPolicy.Default;

    /// <summary>
    /// Check if the configuration is valid
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(BaseUrl) &&
               !string.IsNullOrEmpty(Login) &&
               !string.IsNullOrEmpty(Password) &&
               Uri.IsWellFormedUriString(BaseUrl, UriKind.Absolute);
    }

    /// <summary>
    /// Get Basic Authentication header value
    /// </summary>
    public string GetBasicAuthenticationHeader()
    {
        string credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{Login}:{Password}"));
        return $"Basic {credentials}";
    }

    /// <summary>
    /// A copy of this configuration that differs only in <see cref="OnBehalfOf"/> (EXP-459).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Partnership mode addresses one child merchant per request, so a platform serving many of them needs
    /// a configuration per child rather than one per process. This produces that copy without the caller
    /// having to know which of the nine properties exist.
    /// </para>
    /// <para>
    /// <c>MemberwiseClone</c> rather than a hand-written copy: a property added later is carried over
    /// automatically. A hand-written constructor would keep compiling while silently dropping the new
    /// property, and the dropped one would most likely be a credential or a timeout — the kinds of value
    /// whose absence is discovered in production.
    /// </para>
    /// <para>
    /// The clone is shallow. <see cref="RetryPolicy"/> is shared with the original, which is intended: it is
    /// read-only configuration, and copying it per child would multiply identical objects for nothing.
    /// </para>
    /// </remarks>
    /// <param name="onBehalfOf">The child merchant to act for. Must not be blank.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="onBehalfOf"/> is <see langword="null"/>, empty or whitespace. This is deliberately not
    /// treated as "act as the platform": a blank child id silently routing a payment to the core account
    /// would move money to the wrong party, and a request that cannot name its merchant must fail loudly.
    /// </exception>
    public RozetkaPayConfiguration WithOnBehalfOf(string onBehalfOf)
    {
        if (string.IsNullOrWhiteSpace(onBehalfOf))
        {
            throw new ArgumentException(
                "The child merchant identifier must not be blank. Acting for nobody is not the same as acting "
                + "for the platform, and treating it as such would route the payment to the wrong account.",
                nameof(onBehalfOf));
        }

        RozetkaPayConfiguration copy = (RozetkaPayConfiguration)MemberwiseClone();
        copy.OnBehalfOf = onBehalfOf;
        return copy;
    }
}
