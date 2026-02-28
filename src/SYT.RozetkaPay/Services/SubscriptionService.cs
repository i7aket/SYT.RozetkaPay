using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Models.Subscriptions;
using Microsoft.Extensions.Logging;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Service for subscription management operations
/// </summary>
public class SubscriptionService : BaseService
{
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
    public async Task<SubscriptionPlansResponse> GetPlansAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<SubscriptionPlansResponse>("/api/subscriptions/v1/plans", cancellationToken);
    }

    /// <summary>
    /// Create subscription plan
    /// POST /api/subscriptions/v1/plans
    /// </summary>
    /// <param name="request">Create subscription plan request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription plan response</returns>
    public async Task<SubscriptionPlanResponse> CreatePlanAsync(CreateSubscriptionPlanRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<CreateSubscriptionPlanRequest, SubscriptionPlanResponse>("/api/subscriptions/v1/plans", request, cancellationToken);
    }

    /// <summary>
    /// Deactivate plan
    /// DELETE /api/subscriptions/v1/plans/{planId}
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DeactivatePlanAsync(string planId, CancellationToken cancellationToken = default)
    {
        await DeleteAsync<object>($"/api/subscriptions/v1/plans/{planId}", cancellationToken);
    }

    /// <summary>
    /// Get plan
    /// GET /api/subscriptions/v1/plans/{planId}
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription plan response</returns>
    public async Task<SubscriptionPlanResponse> GetPlanAsync(string planId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<SubscriptionPlanResponse>($"/api/subscriptions/v1/plans/{planId}", cancellationToken);
    }

    /// <summary>
    /// Update plan
    /// PATCH /api/subscriptions/v1/plans/{planId}
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="request">Update subscription plan request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription plan response</returns>
    public async Task<SubscriptionPlanResponse> UpdatePlanAsync(string planId, UpdateSubscriptionPlanRequest request, CancellationToken cancellationToken = default)
    {
        return await PatchAsync<UpdateSubscriptionPlanRequest, SubscriptionPlanResponse>($"/api/subscriptions/v1/plans/{planId}", request, cancellationToken);
    }

    // ===================== SUBSCRIPTIONS (6 endpoints) =====================

    /// <summary>
    /// Create subscription
    /// POST /api/subscriptions/v1/subscriptions
    /// </summary>
    /// <param name="request">Subscription creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription response</returns>
    public async Task<SubscriptionResponse> CreateAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<CreateSubscriptionRequest, SubscriptionResponse>("/api/subscriptions/v1/subscriptions", request, cancellationToken);
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
        return await PostAsync<GiftSubscriptionRequest, CreateSubscriptionResponse>("/api/subscriptions/v1/subscriptions/gift", request, cancellationToken);
    }

    /// <summary>
    /// Get customer subscriptions
    /// GET /api/subscriptions/v1/subscriptions/customer/{customerId}
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Customer subscriptions response</returns>
    public async Task<CustomerSubscriptionsResponse> GetCustomerSubscriptionsAsync(string customerId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<CustomerSubscriptionsResponse>($"/api/subscriptions/v1/subscriptions/customer/{customerId}", cancellationToken);
    }

    /// <summary>
    /// Deactivate subscription
    /// DELETE /api/subscriptions/v1/subscriptions/{subscriptionId}
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DeactivateAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        await DeleteAsync<object>($"/api/subscriptions/v1/subscriptions/{subscriptionId}", cancellationToken);
    }

    /// <summary>
    /// Get subscription
    /// GET /api/subscriptions/v1/subscriptions/{subscriptionId}
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription response</returns>
    public async Task<SubscriptionResponse> GetAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<SubscriptionResponse>($"/api/subscriptions/v1/subscriptions/{subscriptionId}", cancellationToken);
    }

    /// <summary>
    /// Update subscription
    /// PATCH /api/subscriptions/v1/subscriptions/{subscriptionId}
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="request">Update subscription request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription response</returns>
    public async Task<SubscriptionResponse> UpdateAsync(string subscriptionId, UpdateSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        return await PatchAsync<UpdateSubscriptionRequest, SubscriptionResponse>($"/api/subscriptions/v1/subscriptions/{subscriptionId}", request, cancellationToken);
    }

    /// <summary>
    /// Get subscription payments
    /// GET /api/subscriptions/v1/subscriptions/{subscriptionId}/payments
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription payments response</returns>
    public async Task<SubscriptionPaymentsResponse> GetPaymentsAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<SubscriptionPaymentsResponse>($"/api/subscriptions/v1/subscriptions/{subscriptionId}/payments", cancellationToken);
    }

    /// <summary>
    /// Cancel subscription
    /// DELETE /api/subscriptions/v1/subscriptions/{subscriptionId}/cancel
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="request">Cancel subscription request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task CancelAsync(string subscriptionId, CancelSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        await PostAsync<CancelSubscriptionRequest, object>($"/api/subscriptions/v1/subscriptions/{subscriptionId}/cancel", request, cancellationToken);
    }
} 
