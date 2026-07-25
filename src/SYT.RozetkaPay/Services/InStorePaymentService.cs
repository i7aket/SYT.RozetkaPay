using Microsoft.Extensions.Logging;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Models.InStorePayments;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Service for in-store (POS) payment operations
/// </summary>
/// <remarks>
/// Every log statement uses the static route only. No request body, response body, external ID, card
/// number, encrypted track 2 value, or receipt URL is logged by this service.
/// </remarks>
public class InStorePaymentService : BaseService, IInStorePaymentService
{
    private const string CreateEndpoint = "/api/in-store-payments/v1/create";

    private const string ConfirmEndpoint = "/api/in-store-payments/v1/confirm";

    private const string RefundEndpoint = "/api/in-store-payments/v1/refund";

    /// <summary>
    /// Route of the official info operation. Also the log label: the real request target carries the
    /// caller's external ID in the query, which must not be logged.
    /// </summary>
    private const string InfoEndpoint = "/api/in-store-payments/v1/info";

    /// <summary>
    /// Initializes a new instance of the <see cref="InStorePaymentService"/> class.
    /// </summary>
    /// <param name="configuration">SDK configuration.</param>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="logger">Optional logger.</param>
    public InStorePaymentService(RozetkaPayConfiguration configuration, HttpClient httpClient, ILogger? logger = null)
        : base(configuration, httpClient, logger)
    {
    }

    /// <summary>
    /// Register an in-store payment
    /// POST /api/in-store-payments/v1/create
    /// </summary>
    /// <param name="request">Create request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created transaction and its receipt data</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    public async Task<InStorePaymentCreateResponse> CreateAsync(
        InStorePaymentCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await PostAsync<InStorePaymentCreateRequest, InStorePaymentCreateResponse>(
            CreateEndpoint,
            CreateEndpoint,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Confirm an in-store payment
    /// POST /api/in-store-payments/v1/confirm
    /// </summary>
    /// <param name="request">Confirm request. Carries cardholder data; never log it.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmed transaction and its receipt data</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    public async Task<InStorePaymentConfirmResponse> ConfirmAsync(
        InStorePaymentConfirmRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await PostAsync<InStorePaymentConfirmRequest, InStorePaymentConfirmResponse>(
            ConfirmEndpoint,
            ConfirmEndpoint,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Refund an in-store payment
    /// POST /api/in-store-payments/v1/refund
    /// </summary>
    /// <param name="request">Refund request. Carries cardholder data; never log it.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Refund transaction and its receipt data</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    public async Task<InStorePaymentRefundResponse> RefundAsync(
        InStorePaymentRefundRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await PostAsync<InStorePaymentRefundRequest, InStorePaymentRefundResponse>(
            RefundEndpoint,
            RefundEndpoint,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Read the state of an in-store payment
    /// POST /api/in-store-payments/v1/info?external_id={externalId}
    /// </summary>
    /// <remarks>
    /// The official operation is a POST that declares no request body, so no body is sent. Sending an
    /// empty JSON object, or downgrading the verb to GET, would both be a different operation.
    /// </remarks>
    /// <param name="externalId">
    /// Payment identifier in the caller's system. Passed raw and escaped once as the query value.
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current transaction state</returns>
    /// <exception cref="ArgumentNullException"><paramref name="externalId"/> is null.</exception>
    public async Task<InStorePaymentInfoResponse> GetInfoAsync(
        string externalId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(externalId);

        return await PostWithoutBodyAsync<InStorePaymentInfoResponse>(
            $"{InfoEndpoint}?external_id={Uri.EscapeDataString(externalId)}",
            InfoEndpoint,
            cancellationToken);
    }
}
