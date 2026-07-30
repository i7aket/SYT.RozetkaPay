using SYT.RozetkaPay.Models.Common;
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
    Task<List<SubscriptionPlanResponse>> GetPlansAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create subscription plan
    /// </summary>
    /// <param name="request">Create subscription plan request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Subscription plan response</returns>
    Task<SubscriptionPlanResponse> CreatePlanAsync(CreatePlanRequest request, CancellationToken cancellationToken = default);

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
    /// List customer subscriptions, identifying the customer through the configured
    /// <c>X-CUSTOMER-AUTH</c> header. Official operation <c>getSubscriptions</c>:
    /// <c>GET /api/subscriptions/v1/subscriptions</c>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The official root JSON array of subscriptions</returns>
    Task<SubscriptionList> GetSubscriptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// List customer subscriptions, identifying the customer by external ID. Official operation
    /// <c>getSubscriptions</c>: <c>GET /api/subscriptions/v1/subscriptions</c>.
    /// </summary>
    /// <param name="externalId">
    /// User ID in the caller's system. Pass the raw value: it is percent-encoded exactly once as the
    /// <c>external_id</c> query value.
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The official root JSON array of subscriptions</returns>
    /// <exception cref="ArgumentNullException"><paramref name="externalId"/> is null.</exception>
    Task<SubscriptionList> GetSubscriptionsAsync(string externalId, CancellationToken cancellationToken = default);


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
    /// Cancel a subscription with the provider default refund handling. Official operation
    /// <c>CancelCustomerSubscription</c>:
    /// <c>DELETE /api/subscriptions/v1/subscriptions/{subscription_id}/cancel</c>. The operation
    /// sends no request body.
    /// </summary>
    /// <param name="subscriptionId">
    /// Subscription ID. Pass the raw value: it is percent-encoded exactly once as a single path
    /// segment.
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Default provider response</returns>
    /// <exception cref="ArgumentNullException"><paramref name="subscriptionId"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="subscriptionId"/> is exactly "." or "..".</exception>
    Task<DefaultResponse> CancelCustomerSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a subscription with explicit query options. Official operation
    /// <c>CancelCustomerSubscription</c>:
    /// <c>DELETE /api/subscriptions/v1/subscriptions/{subscription_id}/cancel</c>. The operation
    /// sends no request body; <paramref name="options"/> is rendered as query parameters only.
    /// </summary>
    /// <param name="subscriptionId">
    /// Subscription ID. Pass the raw value: it is percent-encoded exactly once as a single path
    /// segment.
    /// </param>
    /// <param name="options">Optional <c>external_id</c> and <c>refund</c> query parameters.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Default provider response</returns>
    /// <exception cref="ArgumentNullException"><paramref name="subscriptionId"/> or <paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="subscriptionId"/> is exactly "." or "..".</exception>
    Task<DefaultResponse> CancelCustomerSubscriptionAsync(string subscriptionId, CancelCustomerSubscriptionOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replace the payment method of a subscription. Official operation
    /// <c>UpdateSubscriptionPaymentMethod</c>:
    /// <c>PATCH /api/subscriptions/v1/subscriptions/{subscription_id}/payment-method</c>. The
    /// configured <c>X-CUSTOMER-AUTH</c> header, when present, identifies the customer.
    /// </summary>
    /// <param name="subscriptionId">
    /// Subscription ID. Pass the raw value: it is percent-encoded exactly once as a single path
    /// segment.
    /// </param>
    /// <param name="request">New payment method, plus the optional official request fields.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Provider message and, when the provider requires one, a pending user action</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="subscriptionId"/> or <paramref name="request"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="subscriptionId"/> is exactly "." or "..".</exception>
    Task<UpdateSubscriptionPaymentMethodResponse> UpdatePaymentMethodAsync(
        string subscriptionId,
        UpdateSubscriptionPaymentMethodRequest request,
        CancellationToken cancellationToken = default);

}
