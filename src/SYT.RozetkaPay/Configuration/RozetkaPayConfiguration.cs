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
    /// Whether to validate SSL certificates (default: true)
    /// Set to false only for development/testing environments
    /// </summary>
    public bool ValidateSslCertificate { get; set; } = true;

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
} 