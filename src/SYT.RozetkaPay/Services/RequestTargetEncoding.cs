namespace SYT.RozetkaPay.Services;

/// <summary>
/// Percent-encoding for caller-controlled values inserted into a request-target path.
/// </summary>
/// <remarks>
/// A path segment and a query value are different contexts. Query values are escaped at their own
/// insertion points; this helper covers the path, where a raw '/' would create a segment and a raw
/// '?' or '#' would end the path altogether.
/// </remarks>
internal static class RequestTargetEncoding
{
    /// <summary>
    /// Escape a caller-supplied identifier so that it stays inside exactly one path segment.
    /// </summary>
    /// <param name="value">Raw identifier. Callers pass the value verbatim and never pre-encode it.</param>
    /// <param name="parameterName">Name of the public parameter the value came from.</param>
    /// <returns>The identifier, percent-encoded exactly once.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is exactly "." or "..".</exception>
    internal static string EscapePathSegment(string value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);

        // "." and ".." cannot be carried through as data. Uri.EscapeDataString leaves the RFC 3986
        // unreserved '.' unchanged, and System.Uri removes exact dot segments while building the
        // request - including from the "%2E" and "%2E%2E" spellings - so the handler would receive a
        // different endpoint than the caller asked for. Failing here keeps that impossible to miss.
        if (value is "." or "..")
        {
            throw new ArgumentException(
                "A path-segment identifier cannot be '.' or '..' because URI normalization would change the request target.",
                parameterName);
        }

        return Uri.EscapeDataString(value);
    }
}
