using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Subscriptions;
using Microsoft.Extensions.Logging;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Service for subscription management operations
/// </summary>
public class SubscriptionService : BaseService, ISubscriptionService
{
    /// <summary>
    /// Route of the official subscription-list operation. Used both as the request target and as the
    /// static log label, so a caller identifier carried in the query never reaches a log sink.
    /// </summary>
    private const string SubscriptionsEndpoint = "/api/subscriptions/v1/subscriptions";

    /// <summary>
    /// Route of both plan-collection operations - the GET that lists plans and the POST that creates one -
    /// and their log label.
    /// </summary>
    private const string PlansEndpoint = "/api/subscriptions/v1/plans";

    /// <summary>
    /// Route of the gift-subscription operation, and its log label.
    /// </summary>
    private const string GiftSubscriptionEndpoint = "/api/subscriptions/v1/subscriptions/gift";

    /// <summary>
    /// Static route template of the per-plan operations - GET, PATCH and DELETE - used as the log label
    /// only. The real request target carries the escaped plan identifier, which must not be logged.
    /// </summary>
    private const string PlanLogLabel = "/api/subscriptions/v1/plans/{plan_id}";

    /// <summary>
    /// Static route template of the per-subscription operations, used as the log label only.
    /// </summary>
    private const string SubscriptionLogLabel = "/api/subscriptions/v1/subscriptions/{subscription_id}";

    /// <summary>
    /// Static route template of the subscription-payments lookup, used as the log label only.
    /// </summary>
    private const string SubscriptionPaymentsLogLabel =
        "/api/subscriptions/v1/subscriptions/{subscription_id}/payments";

    /// <summary>
    /// Static route template of the legacy per-customer subscription list, used as the log label only.
    /// </summary>
    private const string CustomerSubscriptionsLogLabel =
        "/api/subscriptions/v1/subscriptions/customer/{customer_id}";

    /// <summary>
    /// Static route template of the official cancel operation, used as the log label only. The real
    /// request target carries the escaped subscription identifier, which must not be logged.
    /// </summary>
    private const string CancelSubscriptionLogLabel =
        "/api/subscriptions/v1/subscriptions/{subscription_id}/cancel";

    /// <summary>
    /// Static route template of the official payment-method update operation, used as the log label
    /// only. The real request target carries the escaped subscription identifier, which must not be
    /// logged.
    /// </summary>
    private const string UpdatePaymentMethodLogLabel =
        "/api/subscriptions/v1/subscriptions/{subscription_id}/payment-method";

    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionService"/> class.
    /// </summary>
    /// <param name="configuration">SDK configuration.</param>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="logger">Optional logger.</param>
    public SubscriptionService(RozetkaPayConfiguration configuration, HttpClient httpClient, ILogger? logger = null)
        : base(configuration, httpClient, logger)
    {
    }

    // ===================== PLANS (5 endpoints) =====================

    /// <summary>
    /// Get plans
    /// GET /api/subscriptions/v1/plans
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription plans response</returns>
    public async Task<List<Plan>> GetPlansAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<List<Plan>>(PlansEndpoint, PlansEndpoint, cancellationToken);
    }

    /// <summary>
    /// Create subscription plan
    /// POST /api/subscriptions/v1/plans
    /// </summary>
    /// <param name="request">Create subscription plan request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription plan response</returns>
    public async Task<Plan> CreatePlanAsync(CreatePlanRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<CreatePlanRequest, Plan>(
            PlansEndpoint,
            PlansEndpoint,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Deactivate plan
    /// DELETE /api/subscriptions/v1/plans/{planId}
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<DefaultResponse> DeactivatePlanAsync(string planId, CancellationToken cancellationToken = default)
    {
        string encodedPlanId = RequestTargetEncoding.EscapePathSegment(planId, nameof(planId));
        return await DeleteAsync<DefaultResponse>($"{PlansEndpoint}/{encodedPlanId}", PlanLogLabel, cancellationToken);
    }

    /// <summary>
    /// Get plan
    /// GET /api/subscriptions/v1/plans/{planId}
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription plan response</returns>
    public async Task<Plan> GetPlanAsync(string planId, CancellationToken cancellationToken = default)
    {
        string encodedPlanId = RequestTargetEncoding.EscapePathSegment(planId, nameof(planId));
        return await GetAsync<Plan>(
            $"{PlansEndpoint}/{encodedPlanId}",
            PlanLogLabel,
            cancellationToken);
    }

    /// <summary>
    /// Update plan
    /// PATCH /api/subscriptions/v1/plans/{planId}
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="request">Update subscription plan request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription plan response</returns>
    public async Task<DefaultResponse> UpdatePlanAsync(string planId, UpdatePlanRequest request, CancellationToken cancellationToken = default)
    {
        string encodedPlanId = RequestTargetEncoding.EscapePathSegment(planId, nameof(planId));
        return await PatchAsync<UpdatePlanRequest, DefaultResponse>(
            $"{PlansEndpoint}/{encodedPlanId}",
            PlanLogLabel,
            request,
            cancellationToken);
    }

    // ===================== SUBSCRIPTIONS (6 endpoints) =====================

    /// <summary>
    /// Create subscription
    /// POST /api/subscriptions/v1/subscriptions
    /// </summary>
    /// <param name="request">Subscription creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription response</returns>
    public async Task<CreateSubscriptionResponse> CreateAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<CreateSubscriptionRequest, CreateSubscriptionResponse>(
            SubscriptionsEndpoint,
            SubscriptionsEndpoint,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Create gifted subscription
    /// POST /api/subscriptions/v1/subscriptions/gift
    /// </summary>
    /// <param name="request">Gift subscription request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Create subscription response</returns>
    public async Task<CreateSubscriptionResponse> GiftAsync(GiftSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<GiftSubscriptionRequest, CreateSubscriptionResponse>(
            GiftSubscriptionEndpoint,
            GiftSubscriptionEndpoint,
            request,
            cancellationToken);
    }

    /// <summary>
    /// List customer subscriptions, identifying the customer through the configured
    /// <c>X-CUSTOMER-AUTH</c> header.
    /// GET /api/subscriptions/v1/subscriptions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The official root JSON array of subscriptions</returns>
    public async Task<SubscriptionList> GetSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<SubscriptionList>(SubscriptionsEndpoint, SubscriptionsEndpoint, cancellationToken);
    }

    /// <summary>
    /// List customer subscriptions, identifying the customer by external ID.
    /// GET /api/subscriptions/v1/subscriptions?external_id={externalId}
    /// </summary>
    /// <param name="externalId">User ID in the caller's system. Passed raw and escaped once.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The official root JSON array of subscriptions</returns>
    /// <exception cref="ArgumentNullException"><paramref name="externalId"/> is null.</exception>
    public async Task<SubscriptionList> GetSubscriptionsAsync(string externalId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(externalId);

        return await GetAsync<SubscriptionList>(
            $"{SubscriptionsEndpoint}?external_id={Uri.EscapeDataString(externalId)}",
            SubscriptionsEndpoint,
            cancellationToken);
    }

    /// <summary>
    /// Deactivate subscription
    /// DELETE /api/subscriptions/v1/subscriptions/{subscriptionId}
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<DefaultResponse> DeactivateAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        string encodedSubscriptionId = RequestTargetEncoding.EscapePathSegment(subscriptionId, nameof(subscriptionId));
        return await DeleteAsync<DefaultResponse>(
            $"{SubscriptionsEndpoint}/{encodedSubscriptionId}",
            SubscriptionLogLabel,
            cancellationToken);
    }

    /// <summary>
    /// Get subscription
    /// GET /api/subscriptions/v1/subscriptions/{subscriptionId}
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription response</returns>
    public async Task<Subscription> GetAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        string encodedSubscriptionId = RequestTargetEncoding.EscapePathSegment(subscriptionId, nameof(subscriptionId));
        return await GetAsync<Subscription>(
            $"{SubscriptionsEndpoint}/{encodedSubscriptionId}",
            SubscriptionLogLabel,
            cancellationToken);
    }

    /// <summary>
    /// Update subscription
    /// PATCH /api/subscriptions/v1/subscriptions/{subscriptionId}
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="request">Update subscription request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription response</returns>
    public async Task<DefaultResponse> UpdateAsync(string subscriptionId, UpdateSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        string encodedSubscriptionId = RequestTargetEncoding.EscapePathSegment(subscriptionId, nameof(subscriptionId));
        return await PatchAsync<UpdateSubscriptionRequest, DefaultResponse>(
            $"{SubscriptionsEndpoint}/{encodedSubscriptionId}",
            SubscriptionLogLabel,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Get subscription payments
    /// GET /api/subscriptions/v1/subscriptions/{subscriptionId}/payments
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription payments response</returns>
    public async Task<List<SubscriptionPayment>> GetPaymentsAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        string encodedSubscriptionId = RequestTargetEncoding.EscapePathSegment(subscriptionId, nameof(subscriptionId));
        return await GetAsync<List<SubscriptionPayment>>(
            $"{SubscriptionsEndpoint}/{encodedSubscriptionId}/payments",
            SubscriptionPaymentsLogLabel,
            cancellationToken);
    }

    /// <summary>
    /// Cancel a subscription with the provider default refund handling. Sends no request body.
    /// DELETE /api/subscriptions/v1/subscriptions/{subscriptionId}/cancel
    /// </summary>
    /// <param name="subscriptionId">Subscription ID. Passed raw and escaped once as one path segment.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Default provider response</returns>
    /// <exception cref="ArgumentNullException"><paramref name="subscriptionId"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="subscriptionId"/> is exactly "." or "..".</exception>
    public async Task<DefaultResponse> CancelCustomerSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        string encodedSubscriptionId = RequestTargetEncoding.EscapePathSegment(subscriptionId, nameof(subscriptionId));

        return await DeleteAsync<DefaultResponse>(
            $"{SubscriptionsEndpoint}/{encodedSubscriptionId}/cancel",
            CancelSubscriptionLogLabel,
            cancellationToken);
    }

    /// <summary>
    /// Cancel a subscription with explicit query options. Sends no request body.
    /// DELETE /api/subscriptions/v1/subscriptions/{subscriptionId}/cancel?external_id={externalId}&amp;refund={refund}
    /// </summary>
    /// <param name="subscriptionId">Subscription ID. Passed raw and escaped once as one path segment.</param>
    /// <param name="options">Optional <c>external_id</c> and <c>refund</c> query parameters.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Default provider response</returns>
    /// <exception cref="ArgumentNullException"><paramref name="subscriptionId"/> or <paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="subscriptionId"/> is exactly "." or "..".</exception>
    public async Task<DefaultResponse> CancelCustomerSubscriptionAsync(string subscriptionId, CancelCustomerSubscriptionOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        string encodedSubscriptionId = RequestTargetEncoding.EscapePathSegment(subscriptionId, nameof(subscriptionId));

        // Deterministic order, and null means "omit". An empty external ID is not null: it is sent as
        // an empty value so the provider - which owns non-empty validation - can reject it.
        List<string> query = new(2);
        if (options.ExternalId is not null)
        {
            query.Add($"external_id={Uri.EscapeDataString(options.ExternalId)}");
        }

        if (options.Refund is { } refund)
        {
            query.Add(refund ? "refund=true" : "refund=false");
        }

        string endpoint = $"{SubscriptionsEndpoint}/{encodedSubscriptionId}/cancel";
        if (query.Count > 0)
        {
            endpoint += $"?{string.Join('&', query)}";
        }

        return await DeleteAsync<DefaultResponse>(endpoint, CancelSubscriptionLogLabel, cancellationToken);
    }

    /// <summary>
    /// Replace the payment method of a subscription. Official operation
    /// <c>UpdateSubscriptionPaymentMethod</c>:
    /// <c>PATCH /api/subscriptions/v1/subscriptions/{subscription_id}/payment-method</c>.
    /// </summary>
    /// <param name="subscriptionId">Subscription ID. Passed raw and escaped once as one path segment.</param>
    /// <param name="request">New payment method, plus the optional official request fields.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Provider message and, when the provider requires one, a pending user action</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="subscriptionId"/> or <paramref name="request"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="subscriptionId"/> is exactly "." or "..".</exception>
    public async Task<UpdateSubscriptionPaymentMethodResponse> UpdatePaymentMethodAsync(
        string subscriptionId,
        UpdateSubscriptionPaymentMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string encodedSubscriptionId = RequestTargetEncoding.EscapePathSegment(subscriptionId, nameof(subscriptionId));

        return await PatchAsync<UpdateSubscriptionPaymentMethodRequest, UpdateSubscriptionPaymentMethodResponse>(
            $"{SubscriptionsEndpoint}/{encodedSubscriptionId}/payment-method",
            UpdatePaymentMethodLogLabel,
            request,
            cancellationToken);
    }

}
