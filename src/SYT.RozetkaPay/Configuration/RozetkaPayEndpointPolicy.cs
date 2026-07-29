namespace SYT.RozetkaPay.Configuration;

/// <summary>
/// The single rule deciding whether the SDK will talk to a given endpoint.
/// </summary>
/// <remarks>
/// <para>
/// There are two ways to build the SDK — through <c>AddRozetkaPay</c>, where the options pipeline
/// validates the endpoint before anything is resolved, and by constructing a service or
/// <see cref="RozetkaPayClient"/> directly, which never touches that pipeline. The rule lives here so
/// both reach the same verdict: an earlier revision enforced it only on the options path, and a
/// directly constructed service happily dispatched a credential-bearing request over clear text.
/// </para>
/// <para>
/// The host is examined, not only the scheme. Accepting <c>http</c> on the strength of
/// <see cref="RozetkaPayTransportSecurity.AllowClearTextLoopback"/> alone would let a setting whose
/// entire purpose is to unblock a stub gateway on localhost silently downgrade a production endpoint.
/// </para>
/// </remarks>
public static class RozetkaPayEndpointPolicy
{
    /// <summary>
    /// Whether the SDK may send requests to <paramref name="endpoint"/> under
    /// <paramref name="transportSecurity"/>.
    /// </summary>
    /// <param name="endpoint">Candidate endpoint. A relative or malformed URL is never acceptable.</param>
    /// <param name="transportSecurity">Which schemes the caller has permitted.</param>
    /// <returns><see langword="true"/> when the endpoint may be used.</returns>
    public static bool IsAcceptable(string? endpoint, RozetkaPayTransportSecurity transportSecurity)
    {
        if (string.IsNullOrWhiteSpace(endpoint) ||
            !Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        if (uri.Scheme == Uri.UriSchemeHttps)
        {
            return true;
        }

        return uri.Scheme == Uri.UriSchemeHttp &&
               transportSecurity == RozetkaPayTransportSecurity.AllowClearTextLoopback &&
               uri.IsLoopback;
    }

    /// <summary>
    /// The message describing why an endpoint was refused, phrased for whoever configured it.
    /// </summary>
    /// <param name="settingName">Name of the setting carrying the endpoint, as the caller spells it.</param>
    /// <returns>A message naming the rule and the way to opt into loopback clear text.</returns>
    public static string DescribeRejection(string settingName)
    {
        return $"{settingName} must be an absolute https URL. Plain http is accepted only for a " +
               $"loopback host, and only when {nameof(RozetkaPayOptions.TransportSecurity)} is " +
               $"{nameof(RozetkaPayTransportSecurity.AllowClearTextLoopback)}.";
    }
}
