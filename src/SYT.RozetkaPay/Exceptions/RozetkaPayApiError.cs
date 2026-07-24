using System.Net;

namespace SYT.RozetkaPay.Exceptions;

/// <summary>
/// Structured transport details of a failed RozetkaPay API call: the HTTP status, the provider error
/// code, the request identifier, and the unmodified response body.
/// </summary>
/// <remarks>
/// <para>
/// The SDK attaches an instance to every exception raised from a non-success HTTP response; exceptions
/// constructed by hand and failures that never reached an HTTP response carry no details. See
/// <see cref="RozetkaPayException.ApiError"/>.
/// </para>
/// <para>
/// <see cref="Code"/> is text rather than an enum on purpose: the provider adds error codes between SDK
/// releases, and an unrecognized code must survive unchanged instead of failing to deserialize or being
/// mapped onto a wrong fallback value.
/// </para>
/// <para>
/// Treat <see cref="RawBody"/> as sensitive. It is the provider payload verbatim, so it can contain
/// customer data; the SDK never logs it. Scrub it before writing it to a log or a store.
/// </para>
/// </remarks>
public sealed class RozetkaPayApiError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RozetkaPayApiError"/> class.
    /// </summary>
    /// <param name="statusCode">HTTP status code of the failed response.</param>
    /// <param name="code">
    /// Provider error code as text, or <see langword="null"/> when the response carries none. A blank
    /// value is normalized to <see langword="null"/>; outer whitespace is trimmed.
    /// </param>
    /// <param name="requestId">
    /// Request identifier reported by the provider, or <see langword="null"/> when the response carries
    /// none. A blank value is normalized to <see langword="null"/>; outer whitespace is trimmed.
    /// </param>
    /// <param name="rawBody">
    /// Response body exactly as received. Use <see cref="string.Empty"/> for an empty body.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="rawBody"/> is <see langword="null"/>.</exception>
    public RozetkaPayApiError(
        HttpStatusCode statusCode,
        string? code,
        string? requestId,
        string rawBody)
    {
        ArgumentNullException.ThrowIfNull(rawBody);

        StatusCode = statusCode;
        Code = NormalizeIdentifier(code);
        RequestId = NormalizeIdentifier(requestId);
        RawBody = rawBody;
    }

    /// <summary>
    /// HTTP status code of the failed response.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Provider error code as text — for example <c>payment_declined</c> — or <see langword="null"/> when
    /// the response carries none. Codes unknown to this SDK version are returned unchanged.
    /// </summary>
    public string? Code { get; }

    /// <summary>
    /// Request identifier for support correspondence, taken from the response headers or from the
    /// <c>error_id</c> field of the payload, or <see langword="null"/> when the response carries none.
    /// </summary>
    public string? RequestId { get; }

    /// <summary>
    /// Response body exactly as received, never <see langword="null"/> and never rewritten;
    /// <see cref="string.Empty"/> when the response had no body. May contain customer data — scrub it
    /// before logging or storing it.
    /// </summary>
    public string RawBody { get; }

    private static string? NormalizeIdentifier(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
