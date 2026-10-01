using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Exceptions;
using SYT.RozetkaPay.Models.Batch;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.PaymentInstructions;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// The annotations on a nested request object are enforced, not decorative — through the properties marked
/// <c>[ValidateNested]</c>, and only through them.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Validator"/> validates the object it is handed and nothing below it, so <c>[Required]</c> on
/// <see cref="OrderRecipient"/> let an incomplete recipient reach the provider, although the document requires
/// all four fields together.
/// </para>
/// <para>
/// Walking into every nested object instead would have been a breaking change: most nested types were never
/// reconciled with the document, and a hosted batch's empty <c>payment_method</c> would have started failing.
/// So the walk is opt-in, and every type it reaches is held to its schema here.
/// </para>
/// </remarks>
public class NestedRequestValidationTests
{
    private const string ValidateNestedAttributeName = "ValidateNestedAttribute";


    private static OrderRecipient Recipient() => new()
    {
        Name = "Державне казначейство України",
        Tin = "12345676",
        Iban = "UA213223130000026007233566001",
        BankName = "Казначейська служба України",
    };

    private static CreateBatchPaymentRequest Batch(OrderRecipient? recipient, CustomerDocument? document = null) => new()
    {
        Currency = "UAH",
        BatchExternalId = "batch-1",
        Mode = BatchPaymentMode.Hosted,
        Customer = new BatchCustomer
        {
            Document = document,
            // Unmarked: an empty payment method (type missing) is still not validated, as before.
            PaymentMethod = new CustomerRequestPaymentMethod(),
        },
        Orders =
        [
            new BatchOrder
            {
                ApiKey = "api-key",
                Amount = 10m,
                Description = "Budget payment",
                ExternalId = "order-1",
                OrderRecipient = recipient,
            },
        ],
    };

    private static CreatePaymentInstructionsRequest Instructions(OrderRecipient? recipient) => new()
    {
        ProcessingType = PaymentInstructionProcessingType.CardPay,
        Method = PaymentInstructionMethod.Purchase,
        Currency = "UAH",
        Orders =
        [
            new PaymentInstructionOrder
            {
                ApiKey = "00000000-0000-0000-0000-000000000001",
                Amount = 100.5m,
                ExternalId = "pi-order-1",
                OrderRecipient = recipient,
            },
        ],
    };

    [Fact]
    public async Task Batch_IncompleteOrderRecipient_ShouldBeRefusedBeforeTheTransportIsTouched()
    {
        OrderRecipient recipient = Recipient();
        recipient.Iban = null;
        Transport transport = new();

        RozetkaPayValidationException failure = await Assert.ThrowsAsync<RozetkaPayValidationException>(
            () => transport.Batch().CreateBatchPaymentAsync(Batch(recipient)));

        Assert.Equal(0, transport.Attempts);
        Assert.Contains("Orders[0].OrderRecipient: ", failure.Message, StringComparison.Ordinal);
        Assert.Contains("Iban", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PaymentInstruction_IncompleteOrderRecipient_ShouldBeRefusedBeforeTheTransportIsTouched()
    {
        OrderRecipient recipient = Recipient();
        recipient.BankName = string.Empty;
        Transport transport = new();

        RozetkaPayValidationException failure = await Assert.ThrowsAsync<RozetkaPayValidationException>(
            () => transport.Instructions().CreateAsync(Instructions(recipient)));

        Assert.Equal(0, transport.Attempts);
        Assert.Contains("Orders[0].OrderRecipient: ", failure.Message, StringComparison.Ordinal);
        Assert.Contains("BankName", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Batch_IbanShorterThanDocumented_ShouldBeRefusedWithoutQuotingIt()
    {
        OrderRecipient recipient = Recipient();
        recipient.Iban = "UA213223130000"; // 14 characters; the document's minimum is 15
        Transport transport = new();

        RozetkaPayValidationException failure = await Assert.ThrowsAsync<RozetkaPayValidationException>(
            () => transport.Batch().CreateBatchPaymentAsync(Batch(recipient)));

        Assert.Equal(0, transport.Attempts);
        Assert.Contains("Iban", failure.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(recipient.Iban, failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Batch_CustomerDocumentWithoutNumber_ShouldBeRefused()
    {
        Transport transport = new();

        RozetkaPayValidationException failure = await Assert.ThrowsAsync<RozetkaPayValidationException>(
            () => transport.Batch().CreateBatchPaymentAsync(
                Batch(Recipient(), new CustomerDocument { Type = CustomerDocumentType.Id })));

        Assert.Equal(0, transport.Attempts);
        Assert.Contains("Customer.Document: ", failure.Message, StringComparison.Ordinal);
        Assert.Contains("Number", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A complete nested request reaches the transport — including the unmarked, unvalidated empty payment
    /// method a hosted batch has always been allowed to carry.
    /// </summary>
    [Fact]
    public async Task CompleteNestedRequests_ShouldReachTheTransport()
    {
        Transport transport = new();

        await transport.Batch().CreateBatchPaymentAsync(
            Batch(Recipient(), new CustomerDocument { Type = CustomerDocumentType.Passport, Series = "АА", Number = "123456" }));
        await transport.Batch().CreateBatchPaymentAsync(Batch(recipient: null));
        await transport.Instructions().CreateAsync(Instructions(Recipient()));
        await transport.Instructions().CreateAsync(Instructions(recipient: null));

        Assert.Equal(4, transport.Attempts);
    }

    /// <summary>
    /// The objects walked through on the way keep the behaviour they had: 7.0.0 never enforced
    /// <c>BatchOrder.api_key</c> / <c>description</c> or <c>PaymentInstructionOrder.api_key</c>, and the walk
    /// does not start to.
    /// </summary>
    [Fact]
    public async Task WalkedThroughOrders_ShouldKeepTheirOwnAnnotationsUnenforced()
    {
        Transport transport = new();

        CreateBatchPaymentRequest batch = Batch(Recipient());
        batch.Orders[0].ApiKey = null;
        batch.Orders[0].Description = null;

        CreatePaymentInstructionsRequest instructions = Instructions(Recipient());
        instructions.Orders[0].ApiKey = string.Empty;

        await transport.Batch().CreateBatchPaymentAsync(batch);
        await transport.Instructions().CreateAsync(instructions);

        Assert.Equal(2, transport.Attempts);
    }

    /// <summary>
    /// The walk is opt-in: no request type is walked into except through a marked property, and the marker
    /// sits only on the paths listed here — validating, or only walking through.
    /// </summary>
    [Fact]
    public void ValidateNested_ShouldSitOnlyOnTheReconciledPaths()
    {
        string[] marked = [.. ModelTypes()
            .SelectMany(static type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(IsMarked)
                .Select(property => $"{type.Name}.{property.Name}{(ValidatesOwnAnnotations(property) ? string.Empty : " (walk through)")}"))
            .Order(StringComparer.Ordinal)];

        Assert.Equal(
            [
                "BatchCustomer.Document",
                "BatchCustomerRequestUserDetails.Document",
                "BatchOrder.OrderRecipient",
                "CreateBatchPaymentRequest.Customer (walk through)",
                "CreateBatchPaymentRequest.Orders (walk through)",
                "CreatePaymentInstructionsRequest.Orders (walk through)",
                "PaymentInstructionOrder.OrderRecipient",
            ],
            marked);
    }

    /// <summary>
    /// Every type the walk reaches marks exactly its schema's <c>required</c> fields required, and every
    /// length limit it enforces is the one the document declares — so enabling the walk cannot reject a
    /// request the document allows.
    /// </summary>
    [Fact]
    public void EveryTypeReachedByTheWalk_ShouldEnforceOnlyWhatItsSchemaDeclares()
    {
        List<string> mismatches = [];

        foreach (Type type in ValidatedTypes())
        {
            string schema = type.Name;

            HashSet<string> declared = [.. OpenApiSnapshot.RequiredPropertyNamesOfSchema(schema)];
            HashSet<string> marked = [.. OpenApiSnapshot.RequiredJsonPropertyNamesOf(type)];
            if (!declared.SetEquals(marked))
            {
                mismatches.Add(
                    $"{type.Name} required [{string.Join(", ", marked.Order())}] vs {schema} " +
                    $"[{string.Join(", ", declared.Order())}]");
            }

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                (int? min, int? max) = ModelledLengthLimits(property);
                if (min is null && max is null)
                {
                    continue;
                }

                string json = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
                (int? Min, int? Max) documented = OpenApiSnapshot.LengthLimitsOf(schema, json);
                if (documented != (min, max))
                {
                    mismatches.Add($"{type.Name}.{json} length {(min, max)} vs {schema} {documented}");
                }
            }
        }

        Assert.Empty(mismatches);
    }

    [Fact]
    public void TheWalk_ShouldValidateExactlyTheNewNonContractualTransferTypes()
    {
        Assert.Equal(
            ["CustomerDocument", "OrderRecipient"],
            ValidatedTypes().Select(static type => type.Name).Order(StringComparer.Ordinal));
    }

    private static bool IsMarked(PropertyInfo property) => Marker(property) is not null;

    private static Attribute? Marker(PropertyInfo property)
    {
        return property.GetCustomAttributes(inherit: true)
            .OfType<Attribute>()
            .SingleOrDefault(static attribute => attribute.GetType().Name == ValidateNestedAttributeName);
    }

    private static bool ValidatesOwnAnnotations(PropertyInfo property)
    {
        Attribute marker = Marker(property)!;
        return (bool)marker.GetType().GetProperty("OwnAnnotations")!.GetValue(marker)!;
    }

    /// <summary>
    /// Types whose own annotations the walk enforces: the targets of marked properties that do not merely
    /// walk through.
    /// </summary>
    private static IEnumerable<Type> ValidatedTypes()
    {
        HashSet<Type> validated = [];

        foreach (Type type in ModelTypes())
        {
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(IsMarked))
            {
                if (ValidatesOwnAnnotations(property))
                {
                    validated.Add(ElementTypeOf(property.PropertyType));
                }
            }
        }

        return validated;
    }

    private static IEnumerable<Type> ModelTypes()
    {
        return typeof(RozetkaPayClient).Assembly.GetExportedTypes()
            .Where(static type => type.IsClass && type.Namespace?.Contains(".Models") == true);
    }

    private static Type ElementTypeOf(Type type)
    {
        if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type))
        {
            return type.IsArray ? type.GetElementType()! : type.GetGenericArguments().Single();
        }

        return Nullable.GetUnderlyingType(type) ?? type;
    }

    private static (int? Min, int? Max) ModelledLengthLimits(PropertyInfo property)
    {
        int? min = null;
        int? max = null;

        if (property.GetCustomAttribute<StringLengthAttribute>() is { } length)
        {
            max = length.MaximumLength;
            min = length.MinimumLength > 0 ? length.MinimumLength : null;
        }

        if (property.GetCustomAttribute<MaxLengthAttribute>() is { } maxLength)
        {
            max = maxLength.Length;
        }

        if (property.GetCustomAttribute<MinLengthAttribute>() is { } minLength)
        {
            min = minLength.Length;
        }

        return (min, max);
    }

    /// <summary>
    /// A stub transport counting the requests that reach it.
    /// </summary>
    private sealed class Transport
    {
        private int _attempts;

        public int Attempts => _attempts;

        public BatchPaymentService Batch() => new(Configuration(), Client());

        public PaymentInstructionService Instructions() => new(Configuration(), Client());

        private HttpClient Client()
        {
            return new HttpClient(new StubHttpMessageHandler((_, _) =>
            {
                Interlocked.Increment(ref _attempts);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}"),
                });
            }));
        }

        private static RozetkaPayConfiguration Configuration() => new()
        {
            BaseUrl = RozetkaPayOptions.ProductionBaseUrl,
            Login = "test-login",
            Password = "test-password",
            RetryPolicy = RetryPolicy.None,
        };
    }
}
