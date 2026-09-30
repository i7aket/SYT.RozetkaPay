using SYT.RozetkaPay.Models.InStorePayments;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Contract for in-store (POS) payment operations. Implemented by
/// <see cref="InStorePaymentService"/> and intended as the injection/mocking seam for consumer code.
/// </summary>
/// <remarks>
/// <para>
/// The confirm and refund requests carry cardholder data. The SDK never logs a request body, a
/// response body, or any identifier from these operations, and callers must apply the same rule.
/// </para>
/// <para>
/// <b>Removed from the public document on 2026-09-30.</b> RozetkaPay no longer publishes any
/// <c>/api/in-store-payments/v1/*</c> operation (nor the <c>In-Store Payments</c> tag) in
/// <c>https://docs.rozetkapay.com/openapi.json</c>. Every method here is therefore
/// <see cref="ObsoleteAttribute"/> (diagnostic <c>RZPAY001</c>) but unchanged on the wire: the same route,
/// body and response type as in 7.0.0, for accounts that still have in-store access.
/// </para>
/// </remarks>
public interface IInStorePaymentService
{
    /// <summary>
    /// Register an in-store payment. Official operation <c>createInStorePayment</c>:
    /// <c>POST /api/in-store-payments/v1/create</c>.
    /// </summary>
    /// <param name="request">Create request.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created transaction and its receipt data</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    [Obsolete(RemovedFromPublicDocument.InStoreOperation, DiagnosticId = RemovedFromPublicDocument.DiagnosticId, UrlFormat = RemovedFromPublicDocument.UrlFormat)]
    Task<InStorePaymentCreateResponse> CreateAsync(
        InStorePaymentCreateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirm an in-store payment. Official operation <c>confirmInStorePayment</c>:
    /// <c>POST /api/in-store-payments/v1/confirm</c>.
    /// </summary>
    /// <param name="request">Confirm request. Carries cardholder data; never log it.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmed transaction and its receipt data</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    [Obsolete(RemovedFromPublicDocument.InStoreOperation, DiagnosticId = RemovedFromPublicDocument.DiagnosticId, UrlFormat = RemovedFromPublicDocument.UrlFormat)]
    Task<InStorePaymentConfirmResponse> ConfirmAsync(
        InStorePaymentConfirmRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refund an in-store payment. Official operation <c>refundInStorePayment</c>:
    /// <c>POST /api/in-store-payments/v1/refund</c>.
    /// </summary>
    /// <param name="request">Refund request. Carries cardholder data; never log it.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Refund transaction and its receipt data</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    [Obsolete(RemovedFromPublicDocument.InStoreOperation, DiagnosticId = RemovedFromPublicDocument.DiagnosticId, UrlFormat = RemovedFromPublicDocument.UrlFormat)]
    Task<InStorePaymentRefundResponse> RefundAsync(
        InStorePaymentRefundRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Read the state of an in-store payment. Official operation <c>getInStorePaymentInfo</c>:
    /// <c>POST /api/in-store-payments/v1/info?external_id={externalId}</c>. The operation is declared
    /// as a POST that sends no request body, and the SDK sends none.
    /// </summary>
    /// <param name="externalId">
    /// Payment identifier in the caller's system. Pass the raw value: it is percent-encoded exactly
    /// once as the <c>external_id</c> query value. An empty string is not null and is sent as an empty
    /// value.
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current transaction state</returns>
    /// <exception cref="ArgumentNullException"><paramref name="externalId"/> is null.</exception>
    [Obsolete(RemovedFromPublicDocument.InStoreOperation, DiagnosticId = RemovedFromPublicDocument.DiagnosticId, UrlFormat = RemovedFromPublicDocument.UrlFormat)]
    Task<InStorePaymentInfoResponse> GetInfoAsync(
        string externalId,
        CancellationToken cancellationToken = default);
}
