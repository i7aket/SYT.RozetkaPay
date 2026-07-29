using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Converters;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Serialization;
using Microsoft.Extensions.Logging;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Base class for all RozetkaPay services providing common HTTP functionality with retry support
/// </summary>
public abstract class BaseService
{
    /// <summary>
    /// Response header carrying the request identifier. Not declared by the official OpenAPI document, but
    /// commonly added by gateways, so it takes precedence over the body identifier when present.
    /// </summary>
    private const string RequestIdHeaderName = "X-Request-Id";

    /// <summary>
    /// Alternative spelling of the request-identifier response header.
    /// </summary>
    private const string LegacyRequestIdHeaderName = "Request-Id";

    /// <summary>
    /// What a transport helper logs when the caller did not supply a static log label.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The real request target and the value written to a log are two different things: the target has to go
    /// on the wire verbatim, while a log sink writes whatever it is handed. Every overload that takes no
    /// separate label therefore fails closed to this constant instead of logging the target. That protects
    /// the services in this assembly and, just as importantly, any externally derived service: a dynamic
    /// target passed to a no-label helper can never become a log entry.
    /// </para>
    /// <para>
    /// A safe label is never derived from the target. Given an arbitrary path there is no reliable way to
    /// tell a static route segment from a caller identifier, so normalizing or pattern-matching the target
    /// would guess - and a wrong guess is exactly the leak. Without an explicit label the answer is this
    /// constant.
    /// </para>
    /// </remarks>
    private const string RedactedEndpointLogLabel = "[redacted]";

    /// <summary>
    /// Optional request header naming the merchant a partner integration acts for.
    /// </summary>
    private const string OnBehalfOfHeaderName = "X-ON-BEHALF-OF";

    /// <summary>
    /// Optional request header carrying the customer authentication token.
    /// </summary>
    private const string CustomerAuthHeaderName = "X-CUSTOMER-AUTH";

    /// <summary>
    /// SDK configuration used by service requests.
    /// </summary>
    protected readonly RozetkaPayConfiguration Configuration;

    /// <summary>
    /// HTTP client used to call RozetkaPay API.
    /// </summary>
    /// <remarks>
    /// The client may be owned by the consumer and shared with other services. Its
    /// <see cref="HttpClient.DefaultRequestHeaders"/> are never read or written here: authentication and
    /// the SDK-configured headers go on each request instead.
    /// </remarks>
    protected readonly HttpClient HttpClient;

    /// <summary>
    /// Optional logger instance.
    /// </summary>
    protected readonly ILogger? Logger;

    /// <summary>
    /// Basic credentials derived from the configuration once, attached to every authenticated request.
    /// </summary>
    private readonly AuthenticationHeaderValue _authorization;

    /// <summary>
    /// The configured user agent, already parsed into the header grammar. Empty when the configuration
    /// names none, in which case the SDK adds no user agent and whatever the caller's client carries is
    /// left to flow.
    /// </summary>
    private readonly ProductInfoHeaderValue[] _userAgent;

    /// <summary>
    /// Snapshotted <c>X-ON-BEHALF-OF</c>, or null when the configuration names none.
    /// </summary>
    private readonly string? _onBehalfOf;

    /// <summary>
    /// Snapshotted <c>X-CUSTOMER-AUTH</c>, or null when the configuration names none.
    /// </summary>
    private readonly string? _customerAuth;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseService"/> class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Authentication and the SDK-configured headers are parsed and snapshotted here, and then attached to
    /// each <see cref="HttpRequestMessage"/> the service builds. They are deliberately <b>not</b> installed
    /// on <see cref="HttpClient.DefaultRequestHeaders"/>: one client is shared by every service of a
    /// <see cref="RozetkaPayClient"/> and may also be owned and used by the consumer, so writing to that
    /// collection made construction order observable, let one service's configuration overwrite another's,
    /// and wrote to shared mutable state while requests were in flight.
    /// </para>
    /// <para>
    /// Snapshotting also keeps the long-standing behaviour that a later edit to the mutable
    /// <paramref name="configuration"/> object cannot silently change the credentials a request carries.
    /// <see cref="RozetkaPayConfiguration.RetryPolicy"/> is read per call and is unaffected.
    /// </para>
    /// </remarks>
    /// <param name="configuration">SDK configuration.</param>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="logger">Optional logger.</param>
    protected BaseService(RozetkaPayConfiguration configuration, HttpClient httpClient, ILogger? logger = null)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Logger = logger;

        // Enforced here, not only in the options validator: a service constructed directly - which is
        // a supported, public way to use this SDK - never goes through the options pipeline, so an
        // earlier revision that guarded only that pipeline let a directly built service dispatch a
        // credential-bearing request over clear text. This constructor is the one point every service
        // passes through, whichever way it was built.
        if (!RozetkaPayEndpointPolicy.IsAcceptable(Configuration.BaseUrl, Configuration.TransportSecurity))
        {
            throw new ArgumentException(
                RozetkaPayEndpointPolicy.DescribeRejection(nameof(RozetkaPayConfiguration.BaseUrl)),
                nameof(configuration));
        }

        // Endpoint and timeout only. Header state belongs to the request, below.
        HttpClient.BaseAddress = new Uri(Configuration.BaseUrl);
        HttpClient.Timeout = Configuration.Timeout;

        _authorization = AuthenticationHeaderValue.Parse(Configuration.GetBasicAuthenticationHeader());

        // One throwaway message is the scratch header collection for all three configured values. It is
        // disposed here and never retained, and the caller's client is not touched.
        using (HttpRequestMessage scratch = new())
        {
            _userAgent = ParseUserAgent(scratch, Configuration.UserAgent);
            _onBehalfOf = ValidateOptionalHeader(scratch, OnBehalfOfHeaderName, Configuration.OnBehalfOf);
            _customerAuth = ValidateOptionalHeader(scratch, CustomerAuthHeaderName, Configuration.CustomerAuth);
        }
    }

    /// <summary>
    /// Parse the configured user agent into immutable header values without touching any client.
    /// </summary>
    /// <remarks>
    /// The full user-agent grammar - several products, comments - is only implemented by the header parser
    /// itself, so <paramref name="scratch"/> supplies a real request-header collection to parse into and the
    /// resulting entries are copied out. Parsing here rather than at send time keeps the existing contract
    /// that an invalid user agent fails while the service is being constructed instead of on the first HTTP
    /// call.
    /// </remarks>
    /// <param name="scratch">Throwaway message owned and disposed by the constructor.</param>
    /// <param name="userAgent">Configured user agent. Blank means the SDK adds none.</param>
    /// <exception cref="FormatException">The configured user agent is not valid header syntax.</exception>
    private static ProductInfoHeaderValue[] ParseUserAgent(HttpRequestMessage scratch, string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return [];
        }

        scratch.Headers.UserAgent.ParseAdd(userAgent);
        return [.. scratch.Headers.UserAgent];
    }

    /// <summary>
    /// Validate an optional header value and return what the SDK will send, or null when the configuration
    /// names none. Blank is treated as absent, exactly as before.
    /// </summary>
    /// <remarks>
    /// The value is added to <paramref name="scratch"/> for the same reason the user agent is parsed there:
    /// until EXP-341 these headers were installed with <c>DefaultRequestHeaders.Add</c>, which validated
    /// them during construction. Snapshotting must not turn a rejected value - a bare CR or LF that is not a
    /// legal continuation, say - into a failure on the first request instead. The check is the header
    /// grammar itself rather than a hand-written scan, so it cannot drift from what the request will accept.
    /// </remarks>
    /// <param name="scratch">Throwaway message owned and disposed by the constructor.</param>
    /// <param name="headerName">Header the value will be sent under.</param>
    /// <param name="headerValue">Configured value. Blank means the SDK sends nothing under this name.</param>
    /// <exception cref="FormatException">The configured value is not a valid header value.</exception>
    private static string? ValidateOptionalHeader(
        HttpRequestMessage scratch,
        string headerName,
        string? headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return null;
        }

        scratch.Headers.Add(headerName, headerValue);
        return headerValue;
    }

    /// <summary>
    /// The single place an authenticated <see cref="HttpRequestMessage"/> is built. Every transport helper
    /// in this class goes through it, inside the retry attempt, so no verb can be left carrying the shared
    /// client's default headers instead of its own.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nothing here reads, adds to or removes from <see cref="HttpClient.DefaultRequestHeaders"/>. A header
    /// set on the request wins outright over a caller default of the same name -
    /// <see cref="HttpClient"/> merges defaults only for names the request does not already carry, and
    /// never concatenates the two - so the wire carries exactly one Authorization, one user agent and one
    /// value of each configured optional header.
    /// </para>
    /// <para>
    /// The converse is deliberate too: when the configuration names no optional value, the caller's own
    /// default of that name is left alone rather than stripped. The SDK does not own that collection.
    /// </para>
    /// <para>
    /// The values themselves are immutable and shared across requests and threads, so no per-request
    /// copying is needed.
    /// </para>
    /// </remarks>
    /// <param name="method">HTTP verb of the official operation. Never substituted.</param>
    /// <param name="endpoint">Request target actually sent, including any query values.</param>
    /// <returns>A fresh request owned by the caller, which disposes it together with any content.</returns>
    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string endpoint)
    {
        HttpRequestMessage request = new(method, endpoint);

        request.Headers.Authorization = _authorization;

        foreach (ProductInfoHeaderValue userAgent in _userAgent)
        {
            request.Headers.UserAgent.Add(userAgent);
        }

        if (_onBehalfOf is not null)
        {
            request.Headers.Add(OnBehalfOfHeaderName, _onBehalfOf);
        }

        if (_customerAuth is not null)
        {
            request.Headers.Add(CustomerAuthHeaderName, _customerAuth);
        }

        return request;
    }

    /// <summary>
    /// Make a GET request to the specified endpoint with retry support. The real request target is not
    /// logged: with no explicit label this logs <c>[redacted]</c>.
    /// </summary>
    /// <remarks>
    /// See <see cref="RedactedEndpointLogLabel"/> for why a label is never derived from the target. Pass
    /// <see cref="GetAsync{TResponse}(string, string, CancellationToken)"/> a static route template to keep
    /// route-level observability.
    /// </remarks>
    protected Task<TResponse> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
    {
        return GetAsync<TResponse>(endpoint, RedactedEndpointLogLabel, cancellationToken);
    }

    /// <summary>
    /// Make a GET request to the specified endpoint, logging <paramref name="endpointForLogging"/>
    /// instead of the real request target.
    /// </summary>
    /// <param name="endpoint">Request target actually sent, including any query values.</param>
    /// <param name="endpointForLogging">
    /// Static route template written by the SDK. Callers pass this when the request target carries a
    /// caller identifier, so that the identifier never reaches a log sink.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task<TResponse> GetAsync<TResponse>(string endpoint, string endpointForLogging, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            Logger?.LogInformation("Making GET request to {Endpoint}", endpointForLogging);

            // Built inside the attempt: an HttpRequestMessage is single-use, so a retry must never reuse the
            // one a previous attempt sent. Each fresh message carries the same snapshotted headers.
            using HttpRequestMessage message = CreateAuthenticatedRequest(HttpMethod.Get, endpoint);

            // The response owns its content, and on a real handler the connection behind it, until it is
            // disposed. The body is read into a string first, so disposing here releases both - including
            // when HandleErrorResponse throws on the way out.
            using HttpResponseMessage response = await HttpClient
                .SendAsync(message, cancellationToken)
                .ConfigureAwait(false);
            string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            Logger?.LogDebug("Response status: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                HandleErrorResponse(response, content);
            }

            return DeserializeResponse<TResponse>(content, response.StatusCode);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Make a GET request to the primary endpoint and fallback to secondary endpoint on 404. Neither real
    /// request target is logged: with no explicit labels both log as <c>[redacted]</c>.
    /// </summary>
    protected Task<TResponse> GetAsyncWithFallback<TResponse>(
        string endpoint,
        string fallbackEndpoint,
        CancellationToken cancellationToken = default)
    {
        return GetAsyncWithFallback<TResponse>(
            endpoint,
            RedactedEndpointLogLabel,
            fallbackEndpoint,
            RedactedEndpointLogLabel,
            cancellationToken);
    }

    /// <summary>
    /// Make a GET request to the primary endpoint and fallback to secondary endpoint on 404, logging the
    /// supplied static labels instead of either real request target.
    /// </summary>
    /// <remarks>
    /// A caller who cancelled while the primary request was in flight gets no fallback at all: the check is
    /// the first statement of the catch, so the fallback path is abandoned before it announces itself and
    /// before a second request is built. Only <see cref="RozetkaPayNotFoundException"/> is caught, so an
    /// <see cref="OperationCanceledException"/> from the primary attempt leaves this method unchanged.
    /// </remarks>
    /// <param name="endpoint">Primary request target actually sent, including any query values.</param>
    /// <param name="endpointForLogging">Static route template of the primary target.</param>
    /// <param name="fallbackEndpoint">Fallback request target actually sent.</param>
    /// <param name="fallbackEndpointForLogging">Static route template of the fallback target.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task<TResponse> GetAsyncWithFallback<TResponse>(
        string endpoint,
        string endpointForLogging,
        string fallbackEndpoint,
        string fallbackEndpointForLogging,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetAsync<TResponse>(endpoint, endpointForLogging, cancellationToken).ConfigureAwait(false);
        }
        catch (RozetkaPayNotFoundException)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Logger?.LogInformation(
                "Primary endpoint {Endpoint} returned 404. Falling back to {FallbackEndpoint}.",
                endpointForLogging,
                fallbackEndpointForLogging);
            return await GetAsync<TResponse>(fallbackEndpoint, fallbackEndpointForLogging, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Make a POST request to the specified endpoint with JSON body and retry support. The real request
    /// target is not logged: with no explicit label this logs <c>[redacted]</c>.
    /// </summary>
    protected Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
    {
        return PostAsync<TRequest, TResponse>(endpoint, RedactedEndpointLogLabel, request, cancellationToken);
    }

    /// <summary>
    /// Make a POST request carrying a JSON body, logging <paramref name="endpointForLogging"/> instead of
    /// the real request target.
    /// </summary>
    /// <param name="endpoint">Request target actually sent, including any query values.</param>
    /// <param name="endpointForLogging">
    /// Static route template written by the SDK. Callers pass this when the request target carries a
    /// caller identifier, so that the identifier never reaches a log sink.
    /// </param>
    /// <param name="request">Body serialized with the SDK serializer options. Never logged.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task<TResponse> PostAsync<TRequest, TResponse>(
        string endpoint,
        string endpointForLogging,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            string json = JsonSerializer.Serialize(request, GetJsonSerializerOptions());
            Logger?.LogInformation("Making POST request to {Endpoint}", endpointForLogging);

            // Body and request are built inside the attempt and owned by it: disposing the request disposes
            // the content, and a retry always sends a freshly built request rather than a spent one.
            using HttpRequestMessage message = CreateAuthenticatedRequest(HttpMethod.Post, endpoint);
            message.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await HttpClient
                .SendAsync(message, cancellationToken)
                .ConfigureAwait(false);
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            Logger?.LogDebug("Response status: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                HandleErrorResponse(response, responseContent);
            }

            return DeserializeResponse<TResponse>(responseContent, response.StatusCode);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Make a POST request to the primary endpoint and fallback to secondary endpoint on 404. Neither real
    /// request target is logged: with no explicit labels both log as <c>[redacted]</c>.
    /// </summary>
    protected Task<TResponse> PostAsyncWithFallback<TRequest, TResponse>(
        string endpoint,
        string fallbackEndpoint,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        return PostAsyncWithFallback<TRequest, TResponse>(
            endpoint,
            RedactedEndpointLogLabel,
            fallbackEndpoint,
            RedactedEndpointLogLabel,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Make a POST request to the primary endpoint and fallback to secondary endpoint on 404, logging the
    /// supplied static labels instead of either real request target.
    /// </summary>
    /// <remarks>
    /// Same cancellation boundary as <see cref="GetAsyncWithFallback{TResponse}(string, string, CancellationToken)"/>:
    /// a cancelled caller never reaches the fallback log, the second serialization of the body, or the
    /// fallback request.
    /// </remarks>
    /// <param name="endpoint">Primary request target actually sent, including any query values.</param>
    /// <param name="endpointForLogging">Static route template of the primary target.</param>
    /// <param name="fallbackEndpoint">Fallback request target actually sent.</param>
    /// <param name="fallbackEndpointForLogging">Static route template of the fallback target.</param>
    /// <param name="request">Body serialized with the SDK serializer options. Never logged.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task<TResponse> PostAsyncWithFallback<TRequest, TResponse>(
        string endpoint,
        string endpointForLogging,
        string fallbackEndpoint,
        string fallbackEndpointForLogging,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await PostAsync<TRequest, TResponse>(endpoint, endpointForLogging, request, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (RozetkaPayNotFoundException)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Logger?.LogInformation(
                "Primary endpoint {Endpoint} returned 404. Falling back to {FallbackEndpoint}.",
                endpointForLogging,
                fallbackEndpointForLogging);
            return await PostAsync<TRequest, TResponse>(
                fallbackEndpoint,
                fallbackEndpointForLogging,
                request,
                cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Make a POST request that can handle both JSON responses and 204 No Content responses. The real
    /// request target is not logged: with no explicit label this logs <c>[redacted]</c>.
    /// </summary>
    protected Task<TResponse> PostAsyncWithNoContent<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
        where TResponse : new()
    {
        return PostAsyncWithNoContent<TRequest, TResponse>(
            endpoint,
            RedactedEndpointLogLabel,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Make a POST request that can handle both JSON responses and 204 No Content responses, logging
    /// <paramref name="endpointForLogging"/> instead of the real request target.
    /// </summary>
    /// <param name="endpoint">Request target actually sent, including any query values.</param>
    /// <param name="endpointForLogging">
    /// Static route template written by the SDK. Callers pass this when the request target carries a
    /// caller identifier, so that the identifier never reaches a log sink.
    /// </param>
    /// <param name="request">Body serialized with the SDK serializer options. Never logged.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task<TResponse> PostAsyncWithNoContent<TRequest, TResponse>(
        string endpoint,
        string endpointForLogging,
        TRequest request,
        CancellationToken cancellationToken = default)
        where TResponse : new()
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            string json = JsonSerializer.Serialize(request, GetJsonSerializerOptions());
            Logger?.LogInformation("Making POST request to {Endpoint}", endpointForLogging);

            // Same per-attempt ownership and per-request headers as the plain POST helper.
            using HttpRequestMessage message = CreateAuthenticatedRequest(HttpMethod.Post, endpoint);
            message.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await HttpClient
                .SendAsync(message, cancellationToken)
                .ConfigureAwait(false);
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            Logger?.LogDebug("Response status: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                HandleErrorResponse(response, responseContent);
            }

            // Handle 204 No Content - return default instance
            if (response.StatusCode == HttpStatusCode.NoContent || string.IsNullOrWhiteSpace(responseContent))
            {
                Logger?.LogDebug("Received 204 No Content or empty response, returning default instance");
                return new TResponse();
            }

            return DeserializeResponse<TResponse>(responseContent, response.StatusCode);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Make a POST request with 204 support to the primary endpoint and fallback to secondary endpoint on
    /// 404. Neither real request target is logged: with no explicit labels both log as <c>[redacted]</c>.
    /// </summary>
    protected Task<TResponse> PostAsyncWithNoContentWithFallback<TRequest, TResponse>(
        string endpoint,
        string fallbackEndpoint,
        TRequest request,
        CancellationToken cancellationToken = default)
        where TResponse : new()
    {
        return PostAsyncWithNoContentWithFallback<TRequest, TResponse>(
            endpoint,
            RedactedEndpointLogLabel,
            fallbackEndpoint,
            RedactedEndpointLogLabel,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Make a POST request with 204 support to the primary endpoint and fallback to secondary endpoint on
    /// 404, logging the supplied static labels instead of either real request target.
    /// </summary>
    /// <remarks>
    /// Same cancellation boundary as <see cref="GetAsyncWithFallback{TResponse}(string, string, CancellationToken)"/>.
    /// </remarks>
    /// <param name="endpoint">Primary request target actually sent, including any query values.</param>
    /// <param name="endpointForLogging">Static route template of the primary target.</param>
    /// <param name="fallbackEndpoint">Fallback request target actually sent.</param>
    /// <param name="fallbackEndpointForLogging">Static route template of the fallback target.</param>
    /// <param name="request">Body serialized with the SDK serializer options. Never logged.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task<TResponse> PostAsyncWithNoContentWithFallback<TRequest, TResponse>(
        string endpoint,
        string endpointForLogging,
        string fallbackEndpoint,
        string fallbackEndpointForLogging,
        TRequest request,
        CancellationToken cancellationToken = default)
        where TResponse : new()
    {
        try
        {
            return await PostAsyncWithNoContent<TRequest, TResponse>(
                endpoint,
                endpointForLogging,
                request,
                cancellationToken).ConfigureAwait(false);
        }
        catch (RozetkaPayNotFoundException)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Logger?.LogInformation(
                "Primary endpoint {Endpoint} returned 404. Falling back to {FallbackEndpoint}.",
                endpointForLogging,
                fallbackEndpointForLogging);
            return await PostAsyncWithNoContent<TRequest, TResponse>(
                fallbackEndpoint,
                fallbackEndpointForLogging,
                request,
                cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Make a PATCH request to the specified endpoint with JSON body and retry support. The real request
    /// target is not logged: with no explicit label this logs <c>[redacted]</c>.
    /// </summary>
    protected Task<TResponse> PatchAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
    {
        return PatchAsync<TRequest, TResponse>(endpoint, RedactedEndpointLogLabel, request, cancellationToken);
    }

    /// <summary>
    /// Make a PATCH request carrying a JSON body, logging <paramref name="endpointForLogging"/> instead
    /// of the real request target.
    /// </summary>
    /// <param name="endpoint">Request target actually sent, including any query values.</param>
    /// <param name="endpointForLogging">
    /// Static route template written by the SDK. Callers pass this when the request target carries a
    /// caller identifier, so that the identifier never reaches a log sink.
    /// </param>
    /// <param name="request">Body serialized with the SDK serializer options. Never logged.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task<TResponse> PatchAsync<TRequest, TResponse>(
        string endpoint,
        string endpointForLogging,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            string json = JsonSerializer.Serialize(request, GetJsonSerializerOptions());
            Logger?.LogInformation("Making PATCH request to {Endpoint}", endpointForLogging);

            using HttpRequestMessage message = CreateAuthenticatedRequest(HttpMethod.Patch, endpoint);
            message.Content = new StringContent(json, Encoding.UTF8, "application/json");

            // The response owns its content, and on a real handler the connection behind it, until it is
            // disposed. The body is read into a string first, so disposing here releases both - including
            // when HandleErrorResponse throws on the way out.
            using HttpResponseMessage response = await HttpClient
                .SendAsync(message, cancellationToken)
                .ConfigureAwait(false);
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            Logger?.LogDebug("Response status: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                HandleErrorResponse(response, responseContent);
            }

            return DeserializeResponse<TResponse>(responseContent, response.StatusCode);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Make a POST request that carries no request body at all, logging
    /// <paramref name="endpointForLogging"/> instead of the real request target.
    /// </summary>
    /// <remarks>
    /// Some official operations are declared as POST with parameters in the query and no request body.
    /// The request is built explicitly and <see cref="HttpRequestMessage.Content"/> is left null, so the
    /// SDK never sends an invented <c>{}</c> body that the operation does not declare, and never
    /// downgrades an official POST to a GET.
    /// </remarks>
    /// <param name="endpoint">Request target actually sent, including any query values.</param>
    /// <param name="endpointForLogging">Static route template written by the SDK.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task<TResponse> PostWithoutBodyAsync<TResponse>(
        string endpoint,
        string endpointForLogging,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            Logger?.LogInformation("Making POST request to {Endpoint}", endpointForLogging);

            using HttpRequestMessage request = CreateAuthenticatedRequest(HttpMethod.Post, endpoint);

            // Disposed on every path, including when HandleErrorResponse throws below.
            using HttpResponseMessage response = await HttpClient
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            Logger?.LogDebug("Response status: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                HandleErrorResponse(response, responseContent);
            }

            return DeserializeResponse<TResponse>(responseContent, response.StatusCode);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Make a DELETE request to the specified endpoint with retry support. The real request target is not
    /// logged: with no explicit label this logs <c>[redacted]</c>.
    /// </summary>
    protected Task<TResponse> DeleteAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
    {
        return DeleteAsync<TResponse>(endpoint, RedactedEndpointLogLabel, cancellationToken);
    }

    /// <summary>
    /// Make a DELETE request without a body, logging <paramref name="endpointForLogging"/> instead of
    /// the real request target.
    /// </summary>
    /// <param name="endpoint">Request target actually sent, including any query values.</param>
    /// <param name="endpointForLogging">
    /// Static route template written by the SDK. Callers pass this when the request target carries a
    /// caller identifier, so that the identifier never reaches a log sink.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected Task<TResponse> DeleteAsync<TResponse>(string endpoint, string endpointForLogging, CancellationToken cancellationToken = default)
    {
        return SendDeleteAsync<TResponse>(endpoint, endpointForLogging, content: null, cancellationToken);
    }

    /// <summary>
    /// Make a DELETE request carrying a JSON body, logging <paramref name="endpointForLogging"/>
    /// instead of the real request target.
    /// </summary>
    /// <remarks>
    /// <see cref="HttpClient.DeleteAsync(string, CancellationToken)"/> cannot carry a body, so the
    /// request is built explicitly. The verb is never downgraded to POST: an official DELETE stays a
    /// DELETE. The serialized body is never logged.
    /// </remarks>
    /// <param name="endpoint">Request target actually sent, including any query values.</param>
    /// <param name="endpointForLogging">Static route template written by the SDK.</param>
    /// <param name="request">Body serialized with the SDK serializer options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected Task<TResponse> DeleteAsync<TRequest, TResponse>(
        string endpoint,
        string endpointForLogging,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        // This is the one helper that serializes outside the attempt, so it needs its own guard: the shared
        // one below would run after the caller's body had already been serialized.
        cancellationToken.ThrowIfCancellationRequested();

        string json = JsonSerializer.Serialize(request, GetJsonSerializerOptions());
        return SendDeleteAsync<TResponse>(endpoint, endpointForLogging, json, cancellationToken);
    }

    /// <summary>
    /// Shared DELETE transport. <paramref name="content"/> is the serialized body, or null for the
    /// bodiless form.
    /// </summary>
    /// <remarks>
    /// An already-cancelled token is rejected here, before the retry loop and before
    /// <see cref="HttpClient"/> is touched, so no DELETE - with or without a body - can reach a
    /// handler after the caller has cancelled. Since EXP-357 every verb has that guarantee from
    /// <see cref="ExecuteWithRetryAsync"/>; this check stays because it is the direct contract of the
    /// bodiless DELETE path and holds for any internal caller of this method. The pre-dispatch check
    /// inside <see cref="HttpClient.SendAsync(HttpRequestMessage, CancellationToken)"/> is a runtime
    /// implementation detail that differs between target frameworks and is not relied on.
    /// </remarks>
    private async Task<TResponse> SendDeleteAsync<TResponse>(
        string endpoint,
        string endpointForLogging,
        string? content,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await ExecuteWithRetryAsync(async () =>
        {
            Logger?.LogInformation("Making DELETE request to {Endpoint}", endpointForLogging);

            using HttpRequestMessage request = CreateAuthenticatedRequest(HttpMethod.Delete, endpoint);
            if (content is not null)
            {
                request.Content = new StringContent(content, Encoding.UTF8, "application/json");
            }

            // Disposed on every path, including when HandleErrorResponse throws below.
            using HttpResponseMessage response = await HttpClient
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            Logger?.LogDebug("Response status: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                HandleErrorResponse(response, responseContent);
            }

            return DeserializeResponse<TResponse>(responseContent, response.StatusCode);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Execute an HTTP operation with retry logic based on the configured retry policy
    /// </summary>
    /// <remarks>
    /// <para>
    /// Available to derived services so that an operation needing its own transport — an official POST
    /// with no body, or a redirect-only GET — reuses this single retry loop instead of duplicating it.
    /// A repeat is always the same request against the same target: this method never changes route,
    /// verb, body, or authentication mode.
    /// </para>
    /// <para>
    /// <paramref name="operation"/> is invoked exactly once per attempt, for at most
    /// <c>1 + </c><see cref="RetryPolicy.MaxRetryAttempts"/> attempts, and must therefore build and release
    /// its own request and response: an <see cref="HttpRequestMessage"/> is single-use and cannot be carried
    /// across a retry. Which failures are repeated is decided in one place — see
    /// <see cref="ShouldRetryFailure"/> — and a failure that is not repeated, including the last one when the
    /// budget is spent, propagates exactly as the attempt raised it.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">Result of one attempt.</typeparam>
    /// <param name="operation">One complete attempt, including reading and mapping the response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        // The single pre-dispatch cancellation contract of the SDK, and deliberately the first thing this
        // method does: before the retry policy is read, before any counter exists, and before the attempt
        // delegate runs. Because every helper passes its complete attempt through here, that one line is also
        // before helper logging, body serialization, request allocation, and any HttpClient or handler call.
        // The pre-dispatch check inside HttpClient is not relied on: it is a runtime implementation detail
        // that differs between target frameworks and between verbs, so basing the SDK's own guarantee on it
        // would make cancellation mean different things on net9.0 and net10.0. ThrowIfCancellationRequested
        // rather than a hand-built exception, so the caller's own token reaches the caller unchanged.
        cancellationToken.ThrowIfCancellationRequested();

        RetryPolicy retryPolicy = Configuration.RetryPolicy;

        // Retries after the first attempt, so the total is exactly 1 + MaxRetryAttempts.
        int retryCount = 0;

        while (true)
        {
            // The same contract at every later attempt boundary: a token cancelled while the previous attempt
            // ran, or during a retry delay short enough not to be awaited at all, must not enter the next
            // attempt either. Complementary to - not a replacement for - the cancellation check in
            // ShouldRetryFailure, which is what stops a cancelled failure from scheduling a delay or buying a
            // repeat in the first place.
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operation().ConfigureAwait(false);
            }
            // An exception filter rather than a catch body: when the budget is spent, or the failure was
            // never retriable, the last attempt's own exception leaves this method untouched. That is what
            // keeps the status-specific SDK exception - and its RozetkaPayApiError evidence, including the
            // raw response body - intact instead of being replaced by a wrapper that erases both.
            catch (Exception failure) when (ShouldRetryFailure(failure, retryPolicy, retryCount, cancellationToken))
            {
                retryCount++;
                await DelayBeforeRetryAsync(retryCount, failure, retryPolicy, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// The single retry decision. A failure is repeated only when the policy is enabled, the budget still
    /// has room, the caller has not cancelled, and the failure itself is retriable.
    /// </summary>
    /// <remarks>
    /// An HTTP failure is recognized by the response evidence the SDK attached to it, never by the exception
    /// type or its message, so <see cref="RetryPolicy.RetriableStatusCodes"/> is honoured exactly as
    /// configured — default set or custom set. An SDK exception constructed by hand carries no evidence and is
    /// therefore never treated as an HTTP failure, whatever its class name suggests.
    /// </remarks>
    private static bool ShouldRetryFailure(
        Exception failure,
        RetryPolicy retryPolicy,
        int retryCount,
        CancellationToken cancellationToken)
    {
        if (!retryPolicy.Enabled || retryCount >= retryPolicy.MaxRetryAttempts)
        {
            return false;
        }

        // The caller's decision outranks the policy: a cancelled operation buys no further attempt, and is
        // not delayed on the way out either.
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        if (failure is RozetkaPayException { ApiError: { } apiError })
        {
            return retryPolicy.ShouldRetry(apiError.StatusCode);
        }

        // A transport failure raised directly. The categories are exactly the ones
        // RetryPolicy.ShouldRetry(Exception) publishes.
        if (retryPolicy.ShouldRetry(failure))
        {
            return true;
        }

        // The one wrapper the SDK unwraps is its own: a RozetkaPayException over a retryable transport
        // category, which is the pre-existing wrapped-transport path. An arbitrary exception is never made
        // retriable by whatever it happens to carry as its inner exception - the SDK did not raise it, and its
        // inner exception is not evidence about the transport.
        return failure is RozetkaPayException { InnerException: { } inner } && retryPolicy.ShouldRetry(inner);
    }

    /// <summary>
    /// Report the retry and wait for it.
    /// </summary>
    private async Task DelayBeforeRetryAsync(
        int retryNumber,
        Exception failure,
        RetryPolicy retryPolicy,
        CancellationToken cancellationToken)
    {
        TimeSpan delay = ResolveRetryDelay(retryNumber, failure, retryPolicy);

        // Actionable without becoming a leak: which retry, what kind of failure, the HTTP status when there
        // was a response, and the wait. The exception object is deliberately not passed and its message is
        // deliberately not rendered - both can carry provider text, a raw body, or a caller identifier, and a
        // log sink writes whatever it is given.
        Logger?.LogWarning(
            "Retry {RetryNumber} of {MaxRetryAttempts} scheduled after {FailureKind}, HTTP status {StatusCode}, in {DelayMilliseconds}ms",
            retryNumber,
            retryPolicy.MaxRetryAttempts,
            failure.GetType().Name,
            DescribeStatus(failure),
            delay.TotalMilliseconds);

        if (delay > TimeSpan.Zero)
        {
            // The caller's token, so cancelling during the wait ends the operation instead of starting
            // another request.
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// How long to wait before the next attempt.
    /// </summary>
    /// <remarks>
    /// A <c>429</c> that arrived with a usable <c>Retry-After</c> uses the server's own figure, bounded by
    /// <see cref="RetryPolicy.MaxDelay"/> so that a hostile or mistaken header cannot park a request for
    /// hours behind the caller's back; a hint of zero or in the past means retry immediately. Every other
    /// failure - and a <c>429</c> whose header was absent or unparseable - uses the configured backoff
    /// unchanged.
    /// </remarks>
    private static TimeSpan ResolveRetryDelay(int retryNumber, Exception failure, RetryPolicy retryPolicy)
    {
        // The hint is parsed while the response is still open and attached to the mapped exception there, so
        // only a real 429 can reach this branch and no header is re-read - or re-parsed out of a message - at
        // retry time.
        if (failure is RozetkaPayRateLimitException { RetryAfter: { } hint })
        {
            if (hint <= TimeSpan.Zero)
            {
                return TimeSpan.Zero;
            }

            return hint > retryPolicy.MaxDelay ? retryPolicy.MaxDelay : hint;
        }

        return retryPolicy.CalculateDelay(retryNumber);
    }

    /// <summary>
    /// The HTTP status behind a failure as a log-safe token, or <c>none</c> when the failure never reached a
    /// response.
    /// </summary>
    private static string DescribeStatus(Exception failure)
    {
        return failure is RozetkaPayException { ApiError: { } apiError }
            ? ((int)apiError.StatusCode).ToString(CultureInfo.InvariantCulture)
            : "none";
    }

    /// <summary>
    /// Handle error responses and throw appropriate exceptions
    /// </summary>
    /// <remarks>
    /// Available to derived services so that an operation with its own transport maps failures through
    /// exactly this switch. Duplicating the switch elsewhere would let one operation drift into a
    /// different exception type for the same status code. The method always throws.
    /// </remarks>
    /// <param name="response">Failed response. Only its status code and headers are read here.</param>
    /// <param name="content">
    /// Response body, already read exactly once by the caller. Kept verbatim on
    /// <see cref="RozetkaPayApiError.RawBody"/> and never logged.
    /// </param>
    protected void HandleErrorResponse(HttpResponseMessage response, string content)
    {
        // The body is read once by the caller and kept verbatim: it is the only place a caller can inspect
        // provider fields this SDK version does not know about.
        string rawBody = content ?? string.Empty;
        ParseErrorPayload(rawBody, out string? apiCode, out string? errorMessage, out string? bodyErrorId);

        string? requestId = TryGetFirstNonBlankHeaderValue(response, RequestIdHeaderName)
            ?? TryGetFirstNonBlankHeaderValue(response, LegacyRequestIdHeaderName)
            ?? bodyErrorId;

        RozetkaPayApiError apiError = new RozetkaPayApiError(response.StatusCode, apiCode, requestId, rawBody);

        // Only safe identifiers are logged. The raw body and the provider message can carry customer data.
        Logger?.LogError(
            "RozetkaPay API error. StatusCode: {StatusCode}. ApiCode: {ApiCode}. RequestId: {RequestId}",
            apiError.StatusCode,
            apiError.Code,
            apiError.RequestId);

        switch (response.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                throw new RozetkaPayAuthorizationException("Unauthorized: Invalid credentials or deactivated account", apiError);
            case HttpStatusCode.Forbidden:
                throw new RozetkaPayAuthorizationException("Forbidden: Access denied", apiError);
            case HttpStatusCode.BadRequest:
                throw new RozetkaPayValidationException(errorMessage ?? "Bad request", apiError);
            case HttpStatusCode.NotFound:
                throw new RozetkaPayNotFoundException("Resource not found", apiError);
            case HttpStatusCode.TooManyRequests:
                ReadRetryAfter(response, out double retryAfter, out TimeSpan? retryAfterHint);
                throw new RozetkaPayRateLimitException(
                    $"Rate limit exceeded. Retry after {retryAfter} seconds",
                    apiError,
                    retryAfterHint);
            case HttpStatusCode.InternalServerError:
                throw new RozetkaPayException("Internal server error", null, apiError);
            default:
                throw new RozetkaPayException(
                    errorMessage != null
                        ? $"API error: {response.StatusCode} - {errorMessage}"
                        : $"API error: {response.StatusCode}",
                    null,
                    apiError);
        }
    }

    /// <summary>
    /// Read the <c>Retry-After</c> header of a <c>429</c> exactly once, as both the figure the exception
    /// message has always reported and the delay the retry loop can act on.
    /// </summary>
    /// <remarks>
    /// The header is converted here, while the response is still open, because the retry decision happens
    /// after the response has been disposed. An HTTP-date becomes a delay relative to now; delta-seconds are
    /// taken as they are. A header the typed accessor cannot parse is treated as absent rather than allowed to
    /// replace <see cref="RozetkaPayRateLimitException"/> with a parser error, and the message keeps the
    /// delta-seconds form - and the historical <c>60</c> fallback - that consumers already depend on.
    /// </remarks>
    /// <param name="response">Failed <c>429</c> response.</param>
    /// <param name="messageSeconds">Seconds to report in the exception message.</param>
    /// <param name="hint">
    /// Delay the provider asked for, or <see langword="null"/> when the header was absent or unparseable.
    /// </param>
    private static void ReadRetryAfter(HttpResponseMessage response, out double messageSeconds, out TimeSpan? hint)
    {
        const double DefaultMessageSeconds = 60;

        RetryConditionHeaderValue? retryAfter;
        try
        {
            retryAfter = response.Headers.RetryAfter;
        }
        catch (FormatException)
        {
            // Defensive rather than observed: on both target frameworks the typed accessor yields null for a
            // value it cannot parse. The guard keeps that a guarantee of this SDK instead of a detail of the
            // runtime, so a provider header can never surface as a parser error in place of the 429.
            retryAfter = null;
        }

        messageSeconds = retryAfter?.Delta?.TotalSeconds ?? DefaultMessageSeconds;

        hint = retryAfter switch
        {
            { Delta: { } delta } => delta,
            { Date: { } date } => date - DateTimeOffset.UtcNow,
            _ => null
        };
    }

    /// <summary>
    /// Read the provider error code, human-readable message, and error identifier out of a response body.
    /// A body the SDK cannot parse leaves every field null instead of hiding the HTTP failure behind a
    /// parser error.
    /// </summary>
    private static void ParseErrorPayload(string content, out string? code, out string? message, out string? errorId)
    {
        code = null;
        message = null;
        errorId = null;

        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(content);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            bool hasNestedError = root.TryGetProperty("error", out JsonElement nestedError)
                && nestedError.ValueKind == JsonValueKind.Object;

            // The code falls back to the nested object only when the top level does not declare it, so an
            // explicit top-level null stays null. The request identifier is a precedence chain instead.
            code = ReadDeclaredIdentifier(root, "code", hasNestedError ? nestedError : null);

            errorId = ReadIdentifier(root, "error_id")
                ?? (hasNestedError ? ReadIdentifier(nestedError, "error_id") : null);

            message = ReadText(root, "message")
                ?? ReadText(root, "error")
                ?? (hasNestedError ? ReadText(nestedError, "message") : null);
        }
        catch (JsonException)
        {
            // A malformed body must not replace the status-specific SDK exception. The caller still gets the
            // body verbatim through RozetkaPayApiError.RawBody.
        }
    }

    /// <summary>
    /// Read a provider identifier, preferring the top-level property and falling back to the nested error
    /// object only when the top level does not declare it at all.
    /// </summary>
    private static string? ReadDeclaredIdentifier(JsonElement root, string propertyName, JsonElement? nestedError)
    {
        if (root.TryGetProperty(propertyName, out JsonElement element))
        {
            return ReadIdentifierValue(element);
        }

        return nestedError is { } nested ? ReadIdentifier(nested, propertyName) : null;
    }

    /// <summary>
    /// Read a provider identifier from a single object, or null when the property is absent or carries a
    /// value that cannot be represented as an identifier.
    /// </summary>
    private static string? ReadIdentifier(JsonElement owner, string propertyName)
    {
        return owner.TryGetProperty(propertyName, out JsonElement element)
            ? ReadIdentifierValue(element)
            : null;
    }

    /// <summary>
    /// Keep a provider identifier as text. A numeric value keeps its raw JSON text, so a code this SDK
    /// version does not know about is never mapped onto a wrong enum value; any other shape, and a blank
    /// string, yields null so that a precedence chain treats it as absent.
    /// </summary>
    private static string? ReadIdentifierValue(JsonElement element)
    {
        string? value = element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            _ => null
        };

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>
    /// Read a string property, ignoring values of any other JSON kind.
    /// </summary>
    private static string? ReadText(JsonElement owner, string propertyName)
    {
        return owner.TryGetProperty(propertyName, out JsonElement element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }

    /// <summary>
    /// Read the first non-blank value of a response header. Header name matching is case-insensitive.
    /// </summary>
    private static string? TryGetFirstNonBlankHeaderValue(HttpResponseMessage response, string headerName)
    {
        if (!response.Headers.TryGetValues(headerName, out IEnumerable<string>? values))
        {
            return null;
        }

        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private TResponse DeserializeResponse<TResponse>(string content, HttpStatusCode statusCode)
    {
        if (statusCode == HttpStatusCode.NoContent || string.IsNullOrWhiteSpace(content))
        {
            return CreateEmptyResponse<TResponse>();
        }

        TResponse? response = JsonSerializer.Deserialize<TResponse>(content, GetJsonSerializerOptions());
        if (response is null)
        {
            throw new RozetkaPayException("Unable to deserialize API response");
        }

        return response;
    }

    private static TResponse CreateEmptyResponse<TResponse>()
    {
        if (typeof(TResponse) == typeof(object))
        {
            return (TResponse)(object)new object();
        }

        if (typeof(TResponse).IsValueType)
        {
            return default!;
        }

        object? instance = Activator.CreateInstance(typeof(TResponse));
        if (instance is TResponse typedInstance)
        {
            return typedInstance;
        }

        return default!;
    }

    /// <summary>
    /// The shared JSON serializer options used by every request and response in this SDK.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns the one instance from <see cref="SdkSerializerOptions"/> rather than building a new one.
    /// This method used to construct a fresh <see cref="JsonSerializerOptions"/> on every call, and it is
    /// called twice per request - to serialize the body and to deserialize the response. Each new
    /// instance carries its own reflection-derived contract cache, so every call rebuilt from scratch
    /// what the previous one had just computed.
    /// </para>
    /// <para>
    /// The returned instance is frozen by <see cref="System.Text.Json"/> on first use and is shared
    /// across every service and every thread. Do not mutate it, and do not hand it to anything that
    /// would: an attempt throws, which is the guarantee that keeps sharing safe rather than racy.
    /// </para>
    /// </remarks>
    protected JsonSerializerOptions GetJsonSerializerOptions()
    {
        return SdkSerializerOptions.Value;
    }
}
