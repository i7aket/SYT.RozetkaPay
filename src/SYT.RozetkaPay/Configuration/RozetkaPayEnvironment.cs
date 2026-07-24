namespace SYT.RozetkaPay.Configuration;

/// <summary>
/// RozetkaPay API environment the SDK talks to. Selecting an environment picks the matching endpoint from
/// the two servers published by the official OpenAPI document, so no URL has to be written by hand.
/// </summary>
/// <remarks>
/// The numeric values are part of the public contract: configuration files and stored settings may carry
/// them, so they must stay stable across releases. An explicit
/// <see cref="RozetkaPayOptions.BaseUrl"/> overrides the endpoint chosen here.
/// </remarks>
public enum RozetkaPayEnvironment
{
    /// <summary>
    /// Live merchant traffic against <see cref="RozetkaPayOptions.ProductionBaseUrl"/>. This is the default,
    /// so an application that never sets an environment keeps the endpoint it has always used.
    /// </summary>
    Production = 0,

    /// <summary>
    /// Test traffic against <see cref="RozetkaPayOptions.SandboxBaseUrl"/>, the development server the
    /// official OpenAPI document publishes alongside production. Requires sandbox credentials.
    /// </summary>
    Sandbox = 1
}
