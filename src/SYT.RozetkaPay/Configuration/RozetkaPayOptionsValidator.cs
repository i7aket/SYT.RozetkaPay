using Microsoft.Extensions.Options;

namespace SYT.RozetkaPay.Configuration;

/// <summary>
/// Cross-field validation for <see cref="RozetkaPayOptions"/>, run by the options pipeline alongside the
/// <see cref="System.ComponentModel.DataAnnotations.RequiredAttribute"/> rules on the options themselves.
/// </summary>
/// <remarks>
/// Every failure names the configuration key and the rule it broke, and never reproduces the configured
/// value: the message travels inside <see cref="OptionsValidationException"/>, which application hosts log,
/// so echoing <see cref="RozetkaPayOptions.Password"/> or <see cref="RozetkaPayOptions.Login"/> there would
/// put a credential into the log. The validator takes no dependencies for the same reason — it has nothing
/// to write to.
/// </remarks>
internal sealed class RozetkaPayOptionsValidator : IValidateOptions<RozetkaPayOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, RozetkaPayOptions options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail($"The {RozetkaPayOptions.SectionName} options were not supplied.");
        }

        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.Login))
        {
            failures.Add($"{Key(nameof(RozetkaPayOptions.Login))} is required and must not be empty or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            failures.Add($"{Key(nameof(RozetkaPayOptions.Password))} is required and must not be empty or whitespace.");
        }

        ValidateEndpoint(options, failures);

        if (options.Timeout <= TimeSpan.Zero)
        {
            failures.Add($"{Key(nameof(RozetkaPayOptions.Timeout))} must be greater than zero.");
        }

        ValidateRetryPolicy(options.RetryPolicy, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    /// <summary>
    /// Validate the environment and the endpoint it resolves to, including an explicit
    /// <see cref="RozetkaPayOptions.BaseUrl"/> override.
    /// </summary>
    private static void ValidateEndpoint(RozetkaPayOptions options, List<string> failures)
    {
        bool environmentIsUsable = Enum.IsDefined(options.Environment);
        if (!environmentIsUsable)
        {
            // No fallback to production: an unrecognised environment is a configuration mistake, and quietly
            // sending live traffic somewhere the caller did not ask for would be worse than failing.
            failures.Add(
                $"{Key(nameof(RozetkaPayOptions.Environment))} must be " +
                $"{nameof(RozetkaPayEnvironment.Production)} or {nameof(RozetkaPayEnvironment.Sandbox)}.");
        }

        if (options.BaseUrl is null)
        {
            // The endpoint comes from the environment. Verifying the resolved URL as well keeps the rule that
            // whatever the SDK ends up calling is an absolute http(s) address, whichever path produced it.
            if (environmentIsUsable && !IsAbsoluteHttpUrl(RozetkaPayOptionsMapper.ResolveBaseUrl(options)))
            {
                failures.Add(
                    $"{Key(nameof(RozetkaPayOptions.Environment))} resolves to an endpoint that is not an " +
                    "absolute http or https URL.");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            failures.Add(
                $"{Key(nameof(RozetkaPayOptions.BaseUrl))} must not be empty or whitespace; remove the " +
                $"setting to use the endpoint of {Key(nameof(RozetkaPayOptions.Environment))}.");
            return;
        }

        if (!IsAbsoluteHttpUrl(options.BaseUrl))
        {
            failures.Add($"{Key(nameof(RozetkaPayOptions.BaseUrl))} must be an absolute http or https URL.");
        }
    }

    private static void ValidateRetryPolicy(RetryPolicy? policy, List<string> failures)
    {
        if (policy is null)
        {
            failures.Add($"{Key(nameof(RozetkaPayOptions.RetryPolicy))} must not be null.");
            return;
        }

        if (policy.MaxRetryAttempts < 0)
        {
            failures.Add($"{RetryKey(nameof(RetryPolicy.MaxRetryAttempts))} must not be negative.");
        }

        if (policy.BaseDelay < TimeSpan.Zero)
        {
            failures.Add($"{RetryKey(nameof(RetryPolicy.BaseDelay))} must not be negative.");
        }

        if (policy.MaxDelay < TimeSpan.Zero)
        {
            failures.Add($"{RetryKey(nameof(RetryPolicy.MaxDelay))} must not be negative.");
        }

        if (policy.Enabled)
        {
            if (policy.MaxRetryAttempts <= 0)
            {
                failures.Add(
                    $"{RetryKey(nameof(RetryPolicy.MaxRetryAttempts))} must be greater than zero when " +
                    $"{RetryKey(nameof(RetryPolicy.Enabled))} is true.");
            }

            if (policy.MaxDelay < policy.BaseDelay)
            {
                failures.Add(
                    $"{RetryKey(nameof(RetryPolicy.MaxDelay))} must not be less than " +
                    $"{RetryKey(nameof(RetryPolicy.BaseDelay))} when retries are enabled.");
            }
        }

        if (!Enum.IsDefined(policy.BackoffStrategy))
        {
            failures.Add(
                $"{RetryKey(nameof(RetryPolicy.BackoffStrategy))} must be a defined " +
                $"{nameof(Configuration.BackoffStrategy)} value.");
        }

        if (policy.RetriableStatusCodes is null)
        {
            failures.Add($"{RetryKey(nameof(RetryPolicy.RetriableStatusCodes))} must not be null.");
        }
    }

    /// <summary>
    /// Reject relative URLs, and absolute URLs the SDK cannot speak, before the first request is attempted.
    /// </summary>
    private static bool IsAbsoluteHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static string Key(string property)
    {
        return $"{RozetkaPayOptions.SectionName}:{property}";
    }

    private static string RetryKey(string property)
    {
        return $"{Key(nameof(RozetkaPayOptions.RetryPolicy))}:{property}";
    }
}
