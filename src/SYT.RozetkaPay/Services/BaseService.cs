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
    protected async Task<TResponse> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            Logger?.LogInformation("Making GET request to {Endpoint}", endpoint);

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
    protected async Task<TResponse> PatchAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            string json = JsonSerializer.Serialize(request, GetJsonSerializerOptions());
            Logger?.LogInformation("Making PATCH request to {Endpoint}", endpoint);

            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Patch, endpoint) { Content = content }, cancellationToken).ConfigureAwait(false);
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
    protected async Task<TResponse> DeleteAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            Logger?.LogInformation("Making DELETE request to {Endpoint}", endpoint);

            HttpResponseMessage response = await HttpClient.DeleteAsync(endpoint, cancellationToken).ConfigureAwait(false);
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
    /// Execute an HTTP operation with retry logic based on the configured retry policy
    /// </summary>
    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
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
    private void HandleErrorResponse(HttpResponseMessage response, string content)
    {
        string? errorMessage = TryParseErrorMessage(content);
        Logger?.LogError("API error response received. StatusCode: {StatusCode}. Message: {Message}", response.StatusCode, errorMessage);

        switch (response.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                throw new RozetkaPayAuthorizationException("Unauthorized: Invalid credentials or deactivated account");
            case HttpStatusCode.Forbidden:
                throw new RozetkaPayAuthorizationException("Forbidden: Access denied");
            case HttpStatusCode.BadRequest:
                throw new RozetkaPayValidationException(errorMessage ?? "Bad request");
            case HttpStatusCode.NotFound:
                throw new RozetkaPayNotFoundException("Resource not found");
            case HttpStatusCode.TooManyRequests:
                double retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? 60;
                throw new RozetkaPayRateLimitException($"Rate limit exceeded. Retry after {retryAfter} seconds");
            case HttpStatusCode.InternalServerError:
                throw new RozetkaPayException("Internal server error");
            default:
                throw new RozetkaPayException(errorMessage != null
                    ? $"API error: {response.StatusCode} - {errorMessage}"
                    : $"API error: {response.StatusCode}");
        }
    }

    /// <summary>
    /// Try to parse error message from response content
    /// </summary>
    private string? TryParseErrorMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            using JsonDocument errorDoc = JsonDocument.Parse(content);
            if (errorDoc.RootElement.TryGetProperty("message", out JsonElement messageElement))
            {
                return messageElement.GetString();
            }
            if (errorDoc.RootElement.TryGetProperty("error", out JsonElement errorElement))
            {
                return errorElement.GetString();
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
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
