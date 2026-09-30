using SYT.RozetkaPay.Models.PaymentInstructions;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Contract for payment-instruction operations. Implemented by
/// <see cref="PaymentInstructionService"/> and intended as the injection/mocking seam for consumer
/// code.
/// </summary>
/// <remarks>
/// The two operations do not share an authentication mode. <see cref="CreateAsync"/> is authenticated
/// like every other SDK call; <see cref="DeclineAsync(string, string, bool, CancellationToken)"/> (and its two-identifier overload) is declared <c>security: []</c> by the official
/// document and is therefore sent over a separate client that carries no RozetkaPay credential and does
/// not follow redirects.
/// </remarks>
public interface IPaymentInstructionService
{
    /// <summary>
    /// Create payment instructions for a batch of orders. Official operation
    /// <c>createPaymentInstructions</c>: <c>POST /api/payment-instructions/v1/new</c>. Authenticated.
    /// </summary>
    /// <param name="request">Batch request carrying at least one order.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batch URLs and the created instructions</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    Task<PaymentInstructionsResult> CreateAsync(
        CreatePaymentInstructionsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decline a payment instruction. Official operation <c>declinePaymentInstruction</c>:
    /// <c>GET /api/payment-instructions/v1/decline</c>.
    /// </summary>
    /// <remarks>
    /// The operation is explicitly unauthenticated (<c>security: []</c>) and answers with a bare HTTP
    /// <c>302</c>. The SDK sends no RozetkaPay credential on this request, does not follow the redirect,
    /// and does not read the redirect target — it returns the <c>Location</c> header and stops.
    /// Navigating to that URL, and validating it first, is the caller's decision: it is
    /// provider-controlled, and fetching it server-side without validation is a request-forgery sink.
    /// </remarks>
    /// <param name="projectId">
    /// Project ID. Pass the raw value: it is percent-encoded exactly once as a query value.
    /// </param>
    /// <param name="paymentInstructionId">
    /// Payment instruction ID. Pass the raw value: it is percent-encoded exactly once as a query value.
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The <c>302</c> status and the parsed <c>Location</c> header</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="projectId"/> or <paramref name="paymentInstructionId"/> is null.
    /// </exception>
    /// <exception cref="SYT.RozetkaPay.Exceptions.RozetkaPayException">
    /// The provider answered <c>302</c> without a usable <c>Location</c> header, or answered a
    /// successful status other than <c>302</c>.
    /// </exception>
    Task<PaymentInstructionDeclineResult> DeclineAsync(
        string projectId,
        string paymentInstructionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decline a payment instruction, optionally together with every child instruction of a consolidated
    /// batch. Official operation <c>declinePaymentInstruction</c>:
    /// <c>GET /api/payment-instructions/v1/decline</c>, query parameter <c>consolidated</c> (published on
    /// 2026-09-30).
    /// </summary>
    /// <remarks>
    /// Same transport guarantees as the two-identifier overload: no credential, no redirect followed, only
    /// the <c>Location</c> header returned. <paramref name="consolidated"/> <see langword="false"/> is the
    /// document's default, so it is not put on the wire at all and the request is byte-identical to the
    /// two-identifier overload; <see langword="true"/> appends <c>&amp;consolidated=true</c>.
    /// </remarks>
    /// <param name="projectId">
    /// Project ID. Pass the raw value: it is percent-encoded exactly once as a query value.
    /// </param>
    /// <param name="paymentInstructionId">
    /// Payment instruction ID — the consolidated instruction when <paramref name="consolidated"/> is
    /// <see langword="true"/>. Percent-encoded exactly once as a query value.
    /// </param>
    /// <param name="consolidated">
    /// <see langword="true"/> when <paramref name="paymentInstructionId"/> is a consolidated payment
    /// instruction, to decline it together with every child instruction of the batch.
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The <c>302</c> status and the parsed <c>Location</c> header</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="projectId"/> or <paramref name="paymentInstructionId"/> is null.
    /// </exception>
    /// <exception cref="SYT.RozetkaPay.Exceptions.RozetkaPayException">
    /// The provider answered <c>302</c> without a usable <c>Location</c> header, or answered a
    /// successful status other than <c>302</c>.
    /// </exception>
    Task<PaymentInstructionDeclineResult> DeclineAsync(
        string projectId,
        string paymentInstructionId,
        bool consolidated,
        CancellationToken cancellationToken = default);
}
