using System.Net;
using System.Net.Sockets;

namespace SYT.RozetkaPay.Configuration;

/// <summary>
/// Defines retry policy for HTTP requests (similar to Stripe SDK)
/// </summary>
public class RetryPolicy
{
    /// <summary>
    /// Maximum number of retry attempts (default: 0 - disabled)
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 0;

    /// <summary>
    /// Base delay between retries (default: 1 second)
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum delay between retries (default: 30 seconds)
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Backoff strategy for calculating retry delays
    /// </summary>
    public BackoffStrategy BackoffStrategy { get; set; } = BackoffStrategy.ExponentialWithJitter;

    /// <summary>
    /// HTTP status codes that should trigger a retry
    /// </summary>
    public HashSet<HttpStatusCode> RetriableStatusCodes { get; set; } = new()
    {
        HttpStatusCode.InternalServerError,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout,
        HttpStatusCode.TooManyRequests,
        HttpStatusCode.RequestTimeout
    };

    /// <summary>
    /// Whether to enable retries (default: false - like Stripe SDK)
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Create a default retry policy (disabled, like Stripe SDK)
    /// </summary>
    public static RetryPolicy Default => new();

    /// <summary>
    /// Create a retry policy with no retries (explicitly disabled)
    /// </summary>
    public static RetryPolicy None => new() { Enabled = false, MaxRetryAttempts = 0 };

    /// <summary>
    /// Create a retry policy with standard settings (enabled with 3 retries)
    /// </summary>
    public static RetryPolicy Standard => new() 
    { 
        Enabled = true, 
        MaxRetryAttempts = 3,
        BaseDelay = TimeSpan.FromSeconds(1),
        MaxDelay = TimeSpan.FromSeconds(30),
        BackoffStrategy = BackoffStrategy.ExponentialWithJitter
    };

    /// <summary>
    /// Calculate the delay for a specific retry attempt
    /// </summary>
    public TimeSpan CalculateDelay(int attempt)
    {
        return BackoffStrategy switch
        {
            BackoffStrategy.Fixed => BaseDelay,
            BackoffStrategy.Linear => TimeSpan.FromMilliseconds(BaseDelay.TotalMilliseconds * attempt),
            BackoffStrategy.Exponential => CalculateExponentialDelay(attempt, false),
            BackoffStrategy.ExponentialWithJitter => CalculateExponentialDelay(attempt, true),
            _ => BaseDelay
        };
    }

    /// <summary>
    /// Determine if a status code should trigger a retry
    /// </summary>
    public bool ShouldRetry(HttpStatusCode statusCode)
    {
        return Enabled && RetriableStatusCodes.Contains(statusCode);
    }

    /// <summary>
    /// Determine if an exception should trigger a retry
    /// </summary>
    public bool ShouldRetry(Exception exception)
    {
        if (!Enabled) return false;

        // TaskCanceledException is deliberately absent, and its absence is the point. It is exactly
        // what the SDK's own timeout raises, so including it repeated a request that had already been
        // dispatched and might already have been carried out: one ambiguous payment creation became
        // four real POSTs, protected only by the provider's per-external_id promise and with nothing
        // said to the caller. A connect failure and a timeout after dispatch are not the same risk.
        return exception is HttpRequestException or SocketException;
    }

    private TimeSpan CalculateExponentialDelay(int attempt, bool withJitter)
    {
        TimeSpan delay = TimeSpan.FromMilliseconds(BaseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
        
        if (delay > MaxDelay)
        {
            delay = MaxDelay;
        }

        if (withJitter)
        {
            // Add random jitter (±25% of the calculated delay).
            //
            // Random.Shared rather than a per-call `new Random()`: the old form allocated a fresh generator -
            // and its internal state - on every retry delay, which is pure garbage on a path that can run for
            // every attempt of every request. Random.Shared is the runtime's process-wide instance and is
            // documented as thread-safe, so no lock, ThreadLocal or per-policy generator is needed. The
            // formula, the ±25% band and the non-negative clamp are unchanged.
            double jitterRange = delay.TotalMilliseconds * 0.25;
            double jitter = (Random.Shared.NextDouble() - 0.5) * 2 * jitterRange;
            delay = TimeSpan.FromMilliseconds(Math.Max(0, delay.TotalMilliseconds + jitter));
        }

        return delay;
    }
}

/// <summary>
/// Backoff strategies for retry delays
/// </summary>
public enum BackoffStrategy
{
    /// <summary>
    /// Fixed delay between retries
    /// </summary>
    Fixed,

    /// <summary>
    /// Linear increase in delay (delay * attempt)
    /// </summary>
    Linear,

    /// <summary>
    /// Exponential increase in delay (2^attempt)
    /// </summary>
    Exponential,

    /// <summary>
    /// Exponential increase with random jitter to avoid thundering herd
    /// </summary>
    ExponentialWithJitter
} 