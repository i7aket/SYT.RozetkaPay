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
        : this(new PaymentResponse())
    {
    }

    public FakePaymentService(PaymentResponse cannedResponse)
    {
        CannedResponse = cannedResponse;
    }

    public PaymentResponse CannedResponse { get; }

    public CreatePaymentRequest? LastCreateRequest { get; private set; }

    public CancellationToken LastCancellationToken { get; private set; }

    public int CreateCallCount { get; private set; }

    public Task<PaymentResponse> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        LastCreateRequest = request;
        LastCancellationToken = cancellationToken;
        CreateCallCount++;
        return Task.FromResult(CannedResponse);
    }

    public Task<PaymentResponse> CreateRecurrentAsync(CreateRecurrentPaymentRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PaymentResponse> ConfirmAsync(ConfirmPaymentRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PaymentResponse> CancelAsync(CancelPaymentRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PaymentResponse> RefundAsync(RefundPaymentRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PaymentOperationResult> RetryRefundAsync(RetryRefundRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PaymentOperationResult> CancelRefundAsync(CancelRefundRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PaymentResponse> GetInfoAsync(string externalId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PaymentListResponse> GetListAsync(PaymentListRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PaymentReceiptResponse> GetReceiptAsync(string externalId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<CardLookupResponse> CardLookupAsync(CardLookupRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<CallbackResendResponse> ResendCallbackAsync(ResendCallbackRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PaymentResponse> CreateP2PAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PaymentResponse> ConfirmP2PAsync(string externalId, decimal? amount = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
