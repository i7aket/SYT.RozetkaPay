namespace SYT.RozetkaPay.Configuration;

/// <summary>
/// Which endpoint schemes the SDK is willing to speak.
/// </summary>
/// <remarks>
/// <para>
/// An enum rather than a boolean, for the same reason
/// <see cref="RozetkaPayOptions.Environment"/> is one: the options surface carries no public
/// boolean flags, because a bare <c>true</c> in a configuration file says nothing about what it
/// enables, and the two states here are worth naming.
/// </para>
/// <para>
/// This is not a certificate-validation setting and cannot be used as one. Neither member touches
/// certificate policy — that always follows the platform or the caller-supplied
/// <see cref="System.Net.Http.HttpMessageHandler"/>. What this selects is whether a clear-text
/// endpoint is permitted at all, and only ever for a loopback host.
/// </para>
/// </remarks>
public enum RozetkaPayTransportSecurity
{
    /// <summary>
    /// Only <c>https</c> endpoints are accepted. This is the default.
    /// </summary>
    /// <remarks>
    /// Every request carries a Basic credential, and a partner integration adds
    /// <c>X-ON-BEHALF-OF</c> and <c>X-CUSTOMER-AUTH</c> on top of it. Over clear text all three are
    /// readable by anything on the path.
    /// </remarks>
    HttpsOnly = 0,

    /// <summary>
    /// Additionally accept a plain <c>http</c> endpoint whose host is a loopback address.
    /// </summary>
    /// <remarks>
    /// For integration tests that run a stub gateway on localhost, where there is no certificate to
    /// present and no network segment to observe. The host is checked as well as the scheme, so this
    /// cannot downgrade a gateway that is not on the loopback interface.
    /// </remarks>
    AllowClearTextLoopback = 1
}
