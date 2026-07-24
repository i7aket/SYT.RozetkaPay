namespace SYT.RozetkaPay.Exceptions;

/// <summary>
/// Base exception for all RozetkaPay SDK errors
/// </summary>
public class RozetkaPayException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RozetkaPayException"/> class.
    /// </summary>
    public RozetkaPayException() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RozetkaPayException"/> class with an error message.
    /// </summary>
    /// <param name="message">Error message.</param>
    public RozetkaPayException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RozetkaPayException"/> class with an error message and inner exception.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Inner exception.</param>
    public RozetkaPayException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RozetkaPayException"/> class with an error message and
    /// the structured details of a failed API response.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Inner exception, or <see langword="null"/>.</param>
    /// <param name="apiError">Structured details of the failed API response.</param>
    /// <exception cref="ArgumentNullException"><paramref name="apiError"/> is <see langword="null"/>.</exception>
    protected internal RozetkaPayException(string message, Exception? innerException, RozetkaPayApiError apiError)
        : base(message, innerException)
    {
        ApiError = apiError ?? throw new ArgumentNullException(nameof(apiError));
    }

    /// <summary>
    /// Structured details of the failed API response — HTTP status, provider error code, request
    /// identifier, and the unmodified response body — or <see langword="null"/> when the failure did not
    /// come from an HTTP response, for example a manually constructed exception, a transport failure, or a
    /// response the SDK could not deserialize.
    /// </summary>
    public RozetkaPayApiError? ApiError { get; }
}

/// <summary>
/// Exception thrown for authentication/authorization errors (401, 403)
/// </summary>
public class RozetkaPayAuthorizationException : RozetkaPayException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RozetkaPayAuthorizationException"/> class.
    /// </summary>
    public RozetkaPayAuthorizationException() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RozetkaPayAuthorizationException"/> class with an error message.
    /// </summary>
    /// <param name="message">Error message.</param>
    public RozetkaPayAuthorizationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RozetkaPayAuthorizationException"/> class with an error message and inner exception.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Inner exception.</param>
    public RozetkaPayAuthorizationException(string message, Exception innerException) : base(message, innerException) { }

    internal RozetkaPayAuthorizationException(string message, RozetkaPayApiError apiError)
        : base(message, null, apiError)
    {
    }
}

/// <summary>
/// Exception thrown for validation errors (400)
/// </summary>
public class RozetkaPayValidationException : RozetkaPayException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RozetkaPayValidationException"/> class.
    /// </summary>
    public RozetkaPayValidationException() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RozetkaPayValidationException"/> class with an error message.
    /// </summary>
    /// <param name="message">Error message.</param>
    public RozetkaPayValidationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RozetkaPayValidationException"/> class with an error message and inner exception.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Inner exception.</param>
    public RozetkaPayValidationException(string message, Exception innerException) : base(message, innerException) { }

    internal RozetkaPayValidationException(string message, RozetkaPayApiError apiError)
        : base(message, null, apiError)
    {
    }
}

/// <summary>
/// Exception thrown for rate limit errors (429)
/// </summary>
public class RozetkaPayRateLimitException : RozetkaPayException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RozetkaPayRateLimitException"/> class.
    /// </summary>
    public RozetkaPayRateLimitException() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RozetkaPayRateLimitException"/> class with an error message.
    /// </summary>
    /// <param name="message">Error message.</param>
    public RozetkaPayRateLimitException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RozetkaPayRateLimitException"/> class with an error message and inner exception.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Inner exception.</param>
    public RozetkaPayRateLimitException(string message, Exception innerException) : base(message, innerException) { }

    internal RozetkaPayRateLimitException(string message, RozetkaPayApiError apiError)
        : base(message, null, apiError)
    {
    }
}

/// <summary>
/// Exception thrown for not found errors (404)
/// </summary>
public class RozetkaPayNotFoundException : RozetkaPayException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RozetkaPayNotFoundException"/> class.
    /// </summary>
    public RozetkaPayNotFoundException() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RozetkaPayNotFoundException"/> class with an error message.
    /// </summary>
    /// <param name="message">Error message.</param>
    public RozetkaPayNotFoundException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RozetkaPayNotFoundException"/> class with an error message and inner exception.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Inner exception.</param>
    public RozetkaPayNotFoundException(string message, Exception innerException) : base(message, innerException) { }

    internal RozetkaPayNotFoundException(string message, RozetkaPayApiError apiError)
        : base(message, null, apiError)
    {
    }
}
