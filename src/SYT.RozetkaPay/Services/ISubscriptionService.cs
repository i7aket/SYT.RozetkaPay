using SYT.RozetkaPay.Models.Subscriptions;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Contract for subscription management operations. Implemented by
/// <see cref="SubscriptionService"/> and intended as the injection/mocking seam for consumer code.
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Get plans
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription plans response</returns>
    Task<SubscriptionPlansResponse> GetPlansAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create subscription plan
    /// </summary>
    /// <param name="request">Create subscription plan request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription plan response</returns>
    Task<SubscriptionPlanResponse> CreatePlanAsync(CreateSubscriptionPlanRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivate plan
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that completes when the plan is deactivated</returns>
    Task DeactivatePlanAsync(string planId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get plan
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription plan response</returns>
    Task<SubscriptionPlanResponse> GetPlanAsync(string planId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update plan
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="request">Update subscription plan request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription plan response</returns>
    Task<SubscriptionPlanResponse> UpdatePlanAsync(string planId, UpdateSubscriptionPlanRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create subscription
    /// </summary>
    /// <param name="request">Subscription creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription response</returns>
    Task<SubscriptionResponse> CreateAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create gifted subscription
    /// </summary>
    /// <param name="request">Gift subscription request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Create subscription response</returns>
    Task<CreateSubscriptionResponse> GiftAsync(GiftSubscriptionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get customer subscriptions
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Customer subscriptions response</returns>
    Task<CustomerSubscriptionsResponse> GetCustomerSubscriptionsAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivate subscription
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that completes when the subscription is deactivated</returns>
    Task DeactivateAsync(string subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get subscription
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription response</returns>
    Task<SubscriptionResponse> GetAsync(string subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update subscription
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="request">Update subscription request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription response</returns>
    Task<SubscriptionResponse> UpdateAsync(string subscriptionId, UpdateSubscriptionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get subscription payments
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription payments response</returns>
    Task<SubscriptionPaymentsResponse> GetPaymentsAsync(string subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel subscription
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="request">Cancel subscription request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that completes when the subscription is cancelled</returns>
    Task CancelAsync(string subscriptionId, CancelSubscriptionRequest request, CancellationToken cancellationToken = default);
}
