using System.Net;
using System.Text;

namespace SYT.RozetkaPay.Tests.TestInfrastructure;

/// <summary>
/// What the controlled transport answers a captured request with.
/// </summary>
internal enum ContractResponseKind
{
    /// <summary>
    /// A deterministic structured provider error.
    /// </summary>
    /// <remarks>
    /// <c>400</c> is deliberate on two counts. It exercises the SDK error path, so the contract suite
    /// does not have to duplicate the success DTO schema of all 67 operations; and - unlike <c>404</c> -
    /// it never triggers the legacy endpoint fallbacks that several services still carry, so exactly one
    /// request is observed per operation.
    /// </remarks>
    StructuredError,

    /// <summary>
    /// A bare <c>302</c> carrying a <c>Location</c> header and no body: the only documented outcome of
    /// <c>declinePaymentInstruction</c>.
    /// </summary>
    Redirect
}

/// <summary>
/// One outbound request exactly as the transport observed it.
/// </summary>
/// <remarks>
/// The body is captured eagerly because <see cref="HttpClient"/> disposes request content before the
/// caller regains control. No credential value is kept on this record: the
/// <see cref="Authorization"/> header contributes only its scheme and a single boolean, so an assertion
/// failure can never render a credential.
/// </remarks>
internal sealed record ContractRequest
{
    /// <summary>Verb actually sent.</summary>
    public required HttpMethod Method { get; init; }

    /// <summary>Absolute request URI the handler was asked to send to.</summary>
    public required Uri RequestUri { get; init; }

    /// <summary>Whether the request carried any content at all.</summary>
    public required bool HasContent { get; init; }

    /// <summary>Request body, or <see langword="null"/> for a request with no content.</summary>
    public string? Body { get; init; }

    /// <summary>Rendered <c>Content-Type</c>, or <see langword="null"/> when there is no content.</summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Names of the credential-bearing headers present on the request. Values are never captured.
    /// </summary>
    public required IReadOnlyList<string> CredentialHeaderNames { get; init; }

    /// <summary>Scheme of the <c>Authorization</c> header, or <see langword="null"/> when absent.</summary>
    public string? AuthorizationScheme { get; init; }

    /// <summary>
    /// Whether <c>Authorization</c> decodes to exactly the expected placeholder
    /// <c>login:password</c> pair. Computed inside the handler so the decoded value never leaves it.
    /// </summary>
    public required bool CarriesExpectedBasicCredentials { get; init; }

    /// <summary>
    /// Every request header that carries no credential, by name. Safe to render in a failure message.
    /// </summary>
    public required IReadOnlyDictionary<string, string[]> SafeHeaders { get; init; }

    /// <summary>
    /// Whether the token the handler received had already been cancelled by the time the request
    /// arrived. Only <see langword="true"/> when the caller's own token reached the transport.
    /// </summary>
    public required bool CancellationObserved { get; init; }

    /// <summary>Values of one non-credential request header, or an empty array when it is absent.</summary>
    internal string[] SafeHeaderValues(string name)
    {
        return SafeHeaders.TryGetValue(name, out string[]? values) ? values : [];
    }
}

/// <summary>
/// Controlled transport for the OpenAPI operation contract suite: it records every outbound request and
/// answers from a fixed, caller-independent response. Nothing reaches a socket, and no request is ever
/// forwarded.
/// </summary>
internal sealed class ContractRecordingHandler : HttpMessageHandler
{
    /// <summary>
    /// Header names that must never appear on an operation the official document declares anonymous.
    /// </summary>
    internal static readonly string[] CredentialHeaders =
    [
        "Authorization",
        "Proxy-Authorization",
        "X-ON-BEHALF-OF",
        "X-CUSTOMER-AUTH"
    ];

    /// <summary>
    /// Structured provider error body. It echoes no caller sentinel, so a row cannot pass by finding
    /// its own input reflected back.
    /// </summary>
    internal const string StructuredErrorBody =
        """{"code":"contract_probe_rejected","message":"Controlled transport rejected the probe.","error_id":"contract-probe"}""";

    /// <summary>
    /// Redirect target announced by the controlled <c>302</c>. The host is in the reserved
    /// <c>.invalid</c> TLD, so following it could not resolve even if the SDK regressed.
    /// </summary>
    internal const string RedirectLocation = "https://decline-redirect-target.invalid/declined";

    private readonly ContractResponseKind _responseKind;

    private readonly string _expectedBasicCredentials;

    private readonly List<ContractRequest> _requests = [];

    /// <summary>
    /// Create a recording transport.
    /// </summary>
    /// <param name="responseKind">Response every request is answered with.</param>
    /// <param name="expectedBasicCredentials">
    /// The placeholder <c>login:password</c> pair the SDK is expected to send, in plain text. The
    /// handler decodes what it observed and compares; neither string is stored on a captured request.
    /// </param>
    internal ContractRecordingHandler(ContractResponseKind responseKind, string expectedBasicCredentials)
    {
        _responseKind = responseKind;
        _expectedBasicCredentials = expectedBasicCredentials;
    }

    /// <summary>
    /// Runs inside the handler, before the response is produced. Used to cancel the caller's token
    /// while the transport is in flight, which is observable only if that token really was propagated.
    /// </summary>
    internal Action? OnRequest { get; set; }

    /// <summary>Every request the transport observed, in order.</summary>
    internal IReadOnlyList<ContractRequest> Requests => _requests;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string? body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        Dictionary<string, string[]> safeHeaders = request.Headers
            .Where(static header => !CredentialHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(
                static header => header.Key,
                static header => header.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);

        string[] credentialHeaderNames = CredentialHeaders
            .Where(name => request.Headers.Contains(name))
            .ToArray();

        OnRequest?.Invoke();

        _requests.Add(new ContractRequest
        {
            Method = request.Method,
            RequestUri = request.RequestUri!,
            HasContent = request.Content is not null,
            Body = body,
            ContentType = request.Content?.Headers.ContentType?.ToString(),
            CredentialHeaderNames = credentialHeaderNames,
            AuthorizationScheme = request.Headers.Authorization?.Scheme,
            CarriesExpectedBasicCredentials = MatchesExpectedBasicCredentials(request),
            SafeHeaders = safeHeaders,
            CancellationObserved = cancellationToken.IsCancellationRequested
        });

        cancellationToken.ThrowIfCancellationRequested();

        return _responseKind switch
        {
            ContractResponseKind.Redirect => CreateRedirect(),
            _ => CreateStructuredError()
        };
    }

    private static HttpResponseMessage CreateRedirect()
    {
        HttpResponseMessage response = new(HttpStatusCode.Redirect);
        response.Headers.Add("Location", RedirectLocation);
        return response;
    }

    private static HttpResponseMessage CreateStructuredError()
    {
        return new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(StructuredErrorBody, Encoding.UTF8, "application/json")
        };
    }

    /// <summary>
    /// Whether the observed <c>Authorization</c> header is Basic and decodes, as UTF-8, to exactly the
    /// expected placeholder <c>login:password</c> pair.
    /// </summary>
    /// <remarks>
    /// The comparison decodes what the SDK produced instead of re-encoding the expectation, so it is an
    /// independent check rather than a restatement of the production formula. The decoded value stays
    /// inside this method.
    /// </remarks>
    private bool MatchesExpectedBasicCredentials(HttpRequestMessage request)
    {
        if (request.Headers.Authorization is not { Scheme: "Basic", Parameter: { } parameter })
        {
            return false;
        }

        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(parameter);
        }
        catch (FormatException)
        {
            return false;
        }

        return string.Equals(
            Encoding.UTF8.GetString(decoded),
            _expectedBasicCredentials,
            StringComparison.Ordinal);
    }
}
