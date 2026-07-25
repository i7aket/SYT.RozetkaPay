using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Converters;
using SYT.RozetkaPay.Exceptions;
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
    /// SDK configuration used by service requests.
    /// </summary>
    protected readonly RozetkaPayConfiguration Configuration;

    /// <summary>
    /// HTTP client used to call RozetkaPay API.
    /// </summary>
    protected readonly HttpClient HttpClient;

    /// <summary>
    /// Optional logger instance.
    /// </summary>
    protected readonly ILogger? Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseService"/> class.
    /// </summary>
    /// <param name="configuration">SDK configuration.</param>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="logger">Optional logger.</param>
    protected BaseService(RozetkaPayConfiguration configuration, HttpClient httpClient, ILogger? logger = null)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Logger = logger;

        // Configure HttpClient
        HttpClient.BaseAddress = new Uri(Configuration.BaseUrl);
        HttpClient.Timeout = Configuration.Timeout;
        HttpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(Configuration.GetBasicAuthenticationHeader());

        HttpClient.DefaultRequestHeaders.UserAgent.Clear();
        if (!string.IsNullOrWhiteSpace(Configuration.UserAgent))
        {
            HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Configuration.UserAgent);
        }

        ApplyOptionalHeader("X-ON-BEHALF-OF", Configuration.OnBehalfOf);
        ApplyOptionalHeader("X-CUSTOMER-AUTH", Configuration.CustomerAuth);
    }

    /// <summary>
    /// Make a GET request to the specified endpoint with retry support
    /// </summary>
    protected Task<TResponse> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
    {
        return GetAsync<TResponse>(endpoint, endpoint, cancellationToken);
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

            HttpResponseMessage response = await HttpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
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
    /// Make a GET request to the primary endpoint and fallback to secondary endpoint on 404.
    /// </summary>
    protected async Task<TResponse> GetAsyncWithFallback<TResponse>(
        string endpoint,
        string fallbackEndpoint,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetAsync<TResponse>(endpoint, cancellationToken).ConfigureAwait(false);
        }
        catch (RozetkaPayNotFoundException)
        {
            Logger?.LogInformation("Primary endpoint {Endpoint} returned 404. Falling back to {FallbackEndpoint}.", endpoint, fallbackEndpoint);
            return await GetAsync<TResponse>(fallbackEndpoint, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Make a POST request to the specified endpoint with JSON body and retry support
    /// </summary>
    protected async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            string json = JsonSerializer.Serialize(request, GetJsonSerializerOptions());
            Logger?.LogInformation("Making POST request to {Endpoint}", endpoint);

            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await HttpClient.PostAsync(endpoint, content, cancellationToken).ConfigureAwait(false);
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
    /// Make a POST request to the primary endpoint and fallback to secondary endpoint on 404.
    /// </summary>
    protected async Task<TResponse> PostAsyncWithFallback<TRequest, TResponse>(
        string endpoint,
        string fallbackEndpoint,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await PostAsync<TRequest, TResponse>(endpoint, request, cancellationToken).ConfigureAwait(false);
        }
        catch (RozetkaPayNotFoundException)
        {
            Logger?.LogInformation("Primary endpoint {Endpoint} returned 404. Falling back to {FallbackEndpoint}.", endpoint, fallbackEndpoint);
            return await PostAsync<TRequest, TResponse>(fallbackEndpoint, request, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Make a POST request that can handle both JSON responses and 204 No Content responses
    /// </summary>
    protected async Task<TResponse> PostAsyncWithNoContent<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
        where TResponse : new()
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            string json = JsonSerializer.Serialize(request, GetJsonSerializerOptions());
            Logger?.LogInformation("Making POST request to {Endpoint}", endpoint);

            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await HttpClient.PostAsync(endpoint, content, cancellationToken).ConfigureAwait(false);
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
    /// Make a POST request with 204 support to the primary endpoint and fallback to secondary endpoint on 404.
    /// </summary>
    protected async Task<TResponse> PostAsyncWithNoContentWithFallback<TRequest, TResponse>(
        string endpoint,
        string fallbackEndpoint,
        TRequest request,
        CancellationToken cancellationToken = default)
        where TResponse : new()
    {
        try
        {
            return await PostAsyncWithNoContent<TRequest, TResponse>(endpoint, request, cancellationToken).ConfigureAwait(false);
        }
        catch (RozetkaPayNotFoundException)
        {
            Logger?.LogInformation("Primary endpoint {Endpoint} returned 404. Falling back to {FallbackEndpoint}.", endpoint, fallbackEndpoint);
            return await PostAsyncWithNoContent<TRequest, TResponse>(fallbackEndpoint, request, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Make a PATCH request to the specified endpoint with JSON body and retry support
    /// </summary>
    protected Task<TResponse> PatchAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
    {
        return PatchAsync<TRequest, TResponse>(endpoint, endpoint, request, cancellationToken);
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

            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            using HttpRequestMessage message = new(HttpMethod.Patch, endpoint) { Content = content };

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

            using HttpRequestMessage request = new(HttpMethod.Post, endpoint);

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
    /// Make a DELETE request to the specified endpoint with retry support
    /// </summary>
    protected Task<TResponse> DeleteAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
    {
        return DeleteAsync<TResponse>(endpoint, endpoint, cancellationToken);
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
    /// handler after the caller has cancelled. The pre-dispatch check inside
    /// <see cref="HttpClient.SendAsync(HttpRequestMessage, CancellationToken)"/> is a runtime
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

            using HttpRequestMessage request = new(HttpMethod.Delete, endpoint);
            if (content is not null)
            {
                request.Content = new StringContent(content, Encoding.UTF8, "application/json");
            }

            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
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
    /// Available to derived services so that an operation needing its own transport — an official POST
    /// with no body, or a redirect-only GET — reuses this single retry loop instead of duplicating it.
    /// A repeat is always the same request against the same target: this method never changes route,
    /// verb, body, or authentication mode.
    /// </remarks>
    /// <typeparam name="T">Result of one attempt.</typeparam>
    /// <param name="operation">One complete attempt, including reading and mapping the response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        RetryPolicy retryPolicy = Configuration.RetryPolicy;
        int currentAttempt = 0;
        Exception? lastException = null;

        while (currentAttempt <= retryPolicy.MaxRetryAttempts)
        {
            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (RozetkaPayException ex) when (ex.InnerException is HttpRequestException &&
                retryPolicy.ShouldRetry(ex.InnerException) && currentAttempt < retryPolicy.MaxRetryAttempts)
            {
                lastException = ex.InnerException;
                currentAttempt++;
                await HandleRetryAsync(currentAttempt, lastException, retryPolicy, cancellationToken).ConfigureAwait(false);
            }
            catch (RozetkaPayRateLimitException ex) when (retryPolicy.ShouldRetry(HttpStatusCode.TooManyRequests) &&
                currentAttempt < retryPolicy.MaxRetryAttempts)
            {
                lastException = ex;
                currentAttempt++;
                await HandleRetryAsync(currentAttempt, lastException, retryPolicy, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (retryPolicy.ShouldRetry(ex) && currentAttempt < retryPolicy.MaxRetryAttempts)
            {
                lastException = ex;
                currentAttempt++;
                await HandleRetryAsync(currentAttempt, lastException, retryPolicy, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex) when (retryPolicy.ShouldRetry(ex) && currentAttempt < retryPolicy.MaxRetryAttempts)
            {
                lastException = ex;
                currentAttempt++;
                await HandleRetryAsync(currentAttempt, lastException, retryPolicy, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Don't retry on non-retriable exceptions
                throw;
            }
        }

        // If we get here, we've exhausted all retry attempts
        if (lastException != null)
        {
            Logger?.LogError(lastException, "Request failed after {AttemptCount} attempts", currentAttempt);
            throw new RozetkaPayException($"Request failed after {currentAttempt} attempts: {lastException.Message}", lastException);
        }

        // This should never happen, but just in case
        throw new RozetkaPayException("Request failed for unknown reason");
    }

    /// <summary>
    /// Handle retry delay and logging
    /// </summary>
    private async Task HandleRetryAsync(int attempt, Exception? exception, RetryPolicy retryPolicy, CancellationToken cancellationToken)
    {
        TimeSpan delay = retryPolicy.CalculateDelay(attempt);

        Logger?.LogWarning("Request attempt {Attempt} failed{Exception}. Retrying in {Delay}ms",
            attempt, exception != null ? $" with exception: {exception.Message}" : "", delay.TotalMilliseconds);

        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
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
                double retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? 60;
                throw new RozetkaPayRateLimitException($"Rate limit exceeded. Retry after {retryAfter} seconds", apiError);
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

    private void ApplyOptionalHeader(string headerName, string? headerValue)
    {
        HttpClient.DefaultRequestHeaders.Remove(headerName);
        if (!string.IsNullOrWhiteSpace(headerValue))
        {
            HttpClient.DefaultRequestHeaders.Add(headerName, headerValue);
        }
    }

    /// <summary>
    /// Get JSON serializer options with proper naming policy and converters
    /// </summary>
    protected JsonSerializerOptions GetJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            Converters = {
                new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower),
                new FlexibleDecimalConverter(),
                new FlexibleDecimalConverterNonNullable(),
                new FlexibleInt32Converter(),
                new FlexibleNullableInt32Converter(),
                new FlexibleInt64Converter(),
                new FlexibleNullableInt64Converter()
            }
        };
    }
}
