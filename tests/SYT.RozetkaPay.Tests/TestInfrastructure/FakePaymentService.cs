using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Services;

namespace SYT.RozetkaPay.Tests.TestInfrastructure;

/// <summary>
/// Hand-written <see cref="IPaymentService"/> substitute. It proves the SDK contract is mockable
/// without a mocking framework and without any HTTP traffic: every call is recorded, and only the
/// operations a test actually exercises are implemented.
/// </summary>
internal sealed class FakePaymentService : IPaymentService
{
    public FakePaymentService()
        : this(new PaymentOperationResult())
    {
    }

    public FakePaymentService(PaymentOperationResult cannedResponse)
    {
        CannedResponse = cannedResponse;
    }

    public PaymentOperationResult CannedResponse { get; }

    public CreatePaymentRequest? LastCreateRequest { get; private set; }

    public CancellationToken LastCancellationToken { get; private set; }

    public int CreateCallCount { get; private set; }

    public Task<PaymentOperationResult> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        LastCreateRequest = request;
        LastCancellationToken = cancellationToken;
        CreateCallCount++;
        return Task.FromResult(CannedResponse);
    }

    public Task<PaymentResponse> CreateRecurrentAsync(CreateRecurrentPaymentRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PaymentOperationResult> ConfirmAsync(ConfirmPaymentRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PaymentOperationResult> CancelAsync(CancelPaymentRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PaymentOperationResult> RefundAsync(RefundPaymentRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PaymentOperationResult> RetryRefundAsync(RetryRefundRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PaymentOperationResult> CancelRefundAsync(CancelRefundRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PaymentStatusResult> GetInfoAsync(string externalId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PaymentReceiptResponse> GetReceiptAsync(string externalId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PaymentOperationResult> CardLookupAsync(CreateLookupRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<CallbackResendResponse> ResendCallbackAsync(ResendCallbackRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PaymentOperationResult> CreateP2PAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PaymentResponse> ConfirmP2PAsync(string externalId, decimal? amount = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
