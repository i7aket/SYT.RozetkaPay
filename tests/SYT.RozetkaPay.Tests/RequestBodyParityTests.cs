using SYT.RozetkaPay.Models.AlternativePayments;
using SYT.RozetkaPay.Models.Batch;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Request bodies that have been reconciled with the published document carry exactly the properties
/// it declares — no fewer, and none invented.
/// </summary>
/// <remarks>
/// <para>
/// The list below grows as the contract work lands, one body per ticket. It is deliberately explicit
/// rather than derived: a body appears here only once someone has compared it against the document and
/// fixed what differed, so the list doubles as the record of what has actually been checked.
/// </para>
/// <para>
/// Both directions matter. A missing field is a capability the SDK cannot express — partial cancel was
/// unreachable for exactly that reason. An extra field is worse in a quieter way: it invites callers to
/// fill in something the gateway ignores, which is what <c>CancelPaymentRequest.reason</c> did.
/// </para>
/// </remarks>
public class RequestBodyParityTests
{
    /// <summary>
    /// Each reconciled request body, named as the document names it, with the model that carries it.
    /// </summary>
    public static TheoryData<string, Type> ReconciledBodies => new()
    {
        { "ConfirmPaymentRequest", typeof(ConfirmPaymentRequest) },
        { "CancelPaymentRequest", typeof(CancelPaymentRequest) },
        { "CreateLookupRequest", typeof(CreateLookupRequest) },
        { "CreateRecurrentPaymentRequest", typeof(CreateRecurrentPaymentRequest) },
        { "ConfirmBatchPaymentRequest", typeof(ConfirmBatchPaymentRequest) },
        { "CancelBatchPaymentRequest", typeof(CancelBatchPaymentRequest) },
        { "CreateBatchPaymentRequest", typeof(CreateBatchPaymentRequest) },
        { "CreateAlternativePayment", typeof(CreateAlternativePayment) },
        // The body POST /api/payments/v1/new actually takes. The similarly named
        // CreatePaymentRequest schema is referenced by no operation and differs by one field.
        { "CreatePaymentRequestDev", typeof(CreatePaymentRequest) },
    };

    [Theory]
    [MemberData(nameof(ReconciledBodies))]
    public void ReconciledBody_ShouldCarryExactlyTheDeclaredProperties(string requestBodyName, Type modelType)
    {
        HashSet<string> declared = [.. OpenApiSnapshot.RequestBodyPropertyNames(requestBodyName)];
        HashSet<string> modelled = [.. OpenApiSnapshot.JsonPropertyNamesOf(modelType)];

        Assert.Equal(declared, modelled);
    }

    [Theory]
    [MemberData(nameof(ReconciledBodies))]
    public void ReconciledBody_ShouldMarkExactlyTheRequiredPropertiesRequired(string requestBodyName, Type modelType)
    {
        // The [Required] attributes are not enforced yet — EXP-402 turns validation on. Aligning them
        // first is the point: switching validation on over markings that contradict the document would
        // start rejecting valid requests, so the markings have to be right before anything reads them.
        HashSet<string> declared = [.. OpenApiSnapshot.RequiredRequestBodyPropertyNames(requestBodyName)];
        HashSet<string> marked = [.. OpenApiSnapshot.RequiredJsonPropertyNamesOf(modelType)];

        Assert.Equal(declared, marked);
    }
}
