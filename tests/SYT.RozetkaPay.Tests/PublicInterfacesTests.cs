using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Security;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Guards the public mockable surface added by EXP-331: every concrete SDK type implements its
/// interface, the interfaces expose exactly the concrete operations, and consumers can substitute
/// them without a mocking framework.
/// </summary>
public class PublicInterfacesTests
{
    private static readonly (Type Interface, Type Implementation)[] ServiceContractPairs =
    [
        (typeof(IPaymentService), typeof(PaymentService)),
        (typeof(IBatchPaymentService), typeof(BatchPaymentService)),
        (typeof(IPayPartsService), typeof(PayPartsService)),
        (typeof(IPayoutService), typeof(PayoutService)),
        (typeof(ICustomerService), typeof(CustomerService)),
        (typeof(ISubscriptionService), typeof(SubscriptionService)),
        (typeof(IReportService), typeof(ReportService)),
        (typeof(IAlternativePaymentService), typeof(AlternativePaymentService)),
        (typeof(IMerchantService), typeof(MerchantService)),
        (typeof(IFinMonService), typeof(FinMonService)),
        (typeof(IInStorePaymentService), typeof(InStorePaymentService)),
        (typeof(IPartnerService), typeof(PartnerService)),
        (typeof(IPaymentInstructionService), typeof(PaymentInstructionService))
    ];

    private static readonly (Type Interface, Type Implementation)[] AllContractPairs =
        [(typeof(IRozetkaPayClient), typeof(RozetkaPayClient)), .. ServiceContractPairs];

    /// <summary>
    /// Public interfaces that are part of the package surface without being API service contracts: they
    /// mirror no concrete service operations and hang off no client property, so the parity and client
    /// property checks above do not apply to them.
    /// </summary>
    private static readonly Type[] NonServiceInterfaces = [typeof(IRozetkaPayWebhookSignatureVerifier)];

    private static IEnumerable<Type> AllExportedInterfaces =>
        AllContractPairs.Select(pair => pair.Interface).Concat(NonServiceInterfaces);

    /// <summary>
    /// Expected client property name to service interface mapping, in declaration order.
    /// </summary>
    private static readonly (string PropertyName, Type InterfaceType, Type ConcreteType)[] ClientProperties =
    [
        ("Payments", typeof(IPaymentService), typeof(PaymentService)),
        ("BatchPayments", typeof(IBatchPaymentService), typeof(BatchPaymentService)),
        ("PayParts", typeof(IPayPartsService), typeof(PayPartsService)),
        ("Payouts", typeof(IPayoutService), typeof(PayoutService)),
        ("Customers", typeof(ICustomerService), typeof(CustomerService)),
        ("Subscriptions", typeof(ISubscriptionService), typeof(SubscriptionService)),
        ("Reports", typeof(IReportService), typeof(ReportService)),
        ("AlternativePayments", typeof(IAlternativePaymentService), typeof(AlternativePaymentService)),
        ("Merchants", typeof(IMerchantService), typeof(MerchantService)),
        ("FinMon", typeof(IFinMonService), typeof(FinMonService)),
        ("InStorePayments", typeof(IInStorePaymentService), typeof(InStorePaymentService)),
        ("Partners", typeof(IPartnerService), typeof(PartnerService)),
        ("PaymentInstructions", typeof(IPaymentInstructionService), typeof(PaymentInstructionService))
    ];

    public static TheoryData<Type, Type> ServiceContracts => ToTheoryData(ServiceContractPairs);

    public static TheoryData<Type, Type> AllContracts => ToTheoryData(AllContractPairs);

    [Theory]
    [MemberData(nameof(AllContracts))]
    public void Interface_ShouldBeImplementedByConcreteType(Type interfaceType, Type implementationType)
    {
        Assert.True(interfaceType.IsInterface, $"{interfaceType.Name} must be an interface.");
        Assert.True(interfaceType.IsPublic, $"{interfaceType.Name} must be public.");
        Assert.True(
            interfaceType.IsAssignableFrom(implementationType),
            $"{implementationType.Name} must implement {interfaceType.Name}.");
    }

    [Fact]
    public void Assembly_ShouldExportExactlyTheCoveredInterfaces()
    {
        Type[] exportedInterfaces = typeof(IRozetkaPayClient).Assembly
            .GetExportedTypes()
            .Where(type => type.IsInterface)
            .ToArray();

        // Thirteen API service contracts plus the aggregate client contract - the eleven from EXP-331
        // and the three service contracts added by EXP-354 - plus the webhook verifier from EXP-332.
        // Adding a public interface without listing it here is a deliberate trip wire on the package
        // surface.
        Assert.Equal(14, AllContractPairs.Length);
        Assert.Equal(13, ServiceContractPairs.Length);
        Assert.Single(NonServiceInterfaces);
        Assert.Equal(
            AllExportedInterfaces.Select(type => type.FullName).Order(StringComparer.Ordinal),
            exportedInterfaces.Select(type => type.FullName).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void IRozetkaPayClient_ShouldDeriveFromIDisposable()
    {
        Assert.Contains(typeof(IDisposable), typeof(IRozetkaPayClient).GetInterfaces());
        Assert.DoesNotContain(typeof(IAsyncDisposable), typeof(IRozetkaPayClient).GetInterfaces());
    }

    [Theory]
    [MemberData(nameof(ServiceContracts))]
    public void ServiceInterface_ShouldMirrorConcretePublicOperations(Type interfaceType, Type implementationType)
    {
        List<string> concreteOperations = DescribeMethods(implementationType);
        List<string> interfaceOperations = DescribeMethods(interfaceType);

        // A missing entry means a public operation is not reachable through the contract; an extra
        // entry means the contract promises something the implementation does not provide. The
        // description covers return type, name, generic arity, parameter types, parameter names and
        // optional defaults, none of which the compiler alone keeps in sync.
        Assert.Equal(concreteOperations, interfaceOperations);
        Assert.NotEmpty(concreteOperations);

        // The parity check above compares methods only, so the concrete services must not expose
        // public instance state that the contract would silently omit.
        Assert.Empty(DeclaredProperties(implementationType));
    }

    [Fact]
    public void ServiceOperationCounts_ShouldMatchTheReviewedInventory()
    {
        Dictionary<string, int> expected = new(StringComparer.Ordinal)
        {
            // EXP-403 removed ten operations whose routes the document does not declare and the
            // live gateway answers 404: two here, one on IPayPartsService, three on IPayoutService,
            // two on IAlternativePaymentService, three on IMerchantService.
            //
            // EXP-430 removed seven more that the constant-based gate could not see, because they
            // built their targets by interpolation: one each on IPayParts and IAlternativePayments
            // (GetOperationInfoAsync), IAlternativePaymentService.GetStatusAsync, two on
            // ICustomerService, and two already-obsolete members on ISubscriptionService.
            [nameof(IPaymentService)] = 12,
            [nameof(IBatchPaymentService)] = 3,
            [nameof(IPayPartsService)] = 10,
            [nameof(IPayoutService)] = 5,
            // EXP-355 added two canonical wallet members and four canonical subscription members
            // alongside the preserved legacy ones.
            [nameof(ICustomerService)] = 7,
            // EXP-354 added the canonical UpdateSubscriptionPaymentMethod operation.
            [nameof(ISubscriptionService)] = 16,
            [nameof(IReportService)] = 2,
            [nameof(IAlternativePaymentService)] = 6,
            [nameof(IMerchantService)] = 1,
            [nameof(IFinMonService)] = 1,
            // EXP-354 service contracts. Disposal is not an API operation: PaymentInstructionService
            // implements IDisposable explicitly, so the contract stays at its two official operations.
            [nameof(IInStorePaymentService)] = 4,
            [nameof(IPartnerService)] = 6,
            [nameof(IPaymentInstructionService)] = 2
        };

        Dictionary<string, int> actual = ServiceContractPairs.ToDictionary(
            pair => pair.Interface.Name,
            pair => DescribeMethods(pair.Interface).Count,
            StringComparer.Ordinal);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ServiceInterfaces_ShouldNotExposeStaticHelpers()
    {
        // BuildP2PRequest is a static convenience helper: it stays on the concrete type and must
        // not leak into the instance contract.
        MethodInfo? staticHelper = typeof(PaymentService).GetMethod(
            nameof(PaymentService.BuildP2PRequest),
            BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(staticHelper);
        Assert.DoesNotContain(
            nameof(PaymentService.BuildP2PRequest),
            typeof(IPaymentService).GetMethods().Select(method => method.Name));
    }

    [Fact]
    public void Interfaces_ShouldNotDeclareDefaultImplementations()
    {
        foreach ((Type interfaceType, _) in AllContractPairs)
        {
            foreach (MethodInfo method in interfaceType.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Assert.True(
                    method.IsAbstract,
                    $"{interfaceType.Name}.{method.Name} must stay abstract (no default interface implementation).");
            }
        }
    }

    [Fact]
    public void IRozetkaPayClient_ShouldExposeThirteenReadOnlyServiceProperties()
    {
        PropertyInfo[] properties = DeclaredProperties(typeof(IRozetkaPayClient));

        Assert.Equal(13, ClientProperties.Length);
        Assert.Equal(ClientProperties.Length, properties.Length);

        foreach ((string propertyName, Type interfaceType, _) in ClientProperties)
        {
            PropertyInfo property = Assert.Single(properties, candidate => candidate.Name == propertyName);
            Assert.Equal(interfaceType, property.PropertyType);
            Assert.True(property.CanRead, $"{propertyName} must be readable.");
            Assert.False(property.CanWrite, $"{propertyName} must be read-only.");
        }
    }

    [Fact]
    public void RozetkaPayClient_ShouldKeepConcretePropertyTypes()
    {
        foreach ((string propertyName, _, Type concreteType) in ClientProperties)
        {
            PropertyInfo? property = typeof(RozetkaPayClient).GetProperty(propertyName);

            Assert.NotNull(property);
            Assert.Equal(concreteType, property.PropertyType);
        }
    }

    [Fact]
    public void RozetkaPayClient_ShouldKeepPublicConstructorAndFactory()
    {
        ConstructorInfo? constructor = typeof(RozetkaPayClient).GetConstructor(
            [typeof(RozetkaPayConfiguration), typeof(HttpClient), typeof(ILogger<RozetkaPayClient>)]);
        MethodInfo? factory = typeof(RozetkaPayClient).GetMethod(
            nameof(RozetkaPayClient.Create),
            BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(constructor);
        Assert.NotNull(factory);
        Assert.Equal(typeof(RozetkaPayClient), factory.ReturnType);
    }

    [Fact]
    public void RozetkaPayClient_InterfaceProperties_ShouldReturnTheSameServiceInstances()
    {
        using HttpClient httpClient = CreateStubHttpClient();
        using RozetkaPayClient client = new(CreateConfiguration(), httpClient);
        IRozetkaPayClient contract = client;

        Assert.Same(client.Payments, contract.Payments);
        Assert.Same(client.BatchPayments, contract.BatchPayments);
        Assert.Same(client.PayParts, contract.PayParts);
        Assert.Same(client.Payouts, contract.Payouts);
        Assert.Same(client.Customers, contract.Customers);
        Assert.Same(client.Subscriptions, contract.Subscriptions);
        Assert.Same(client.Reports, contract.Reports);
        Assert.Same(client.AlternativePayments, contract.AlternativePayments);
        Assert.Same(client.Merchants, contract.Merchants);
        Assert.Same(client.FinMon, contract.FinMon);
        Assert.Same(client.InStorePayments, contract.InStorePayments);
        Assert.Same(client.Partners, contract.Partners);
        Assert.Same(client.PaymentInstructions, contract.PaymentInstructions);
    }

    /// <summary>
    /// The three EXP-354 partner result types must be the new correctly shaped ones. The historical
    /// same-concept DTOs in <c>Models.Merchants</c> and <c>Models.Common</c> describe an older layout;
    /// they stay public and untouched, but no new operation may return them.
    /// </summary>
    [Fact]
    public void PartnerService_ShouldNotReturnTheHistoricalMerchantDtos()
    {
        Type[] staleTypes =
        [
            typeof(SYT.RozetkaPay.Models.Merchants.PartnersFeeDetails),
            typeof(SYT.RozetkaPay.Models.Merchants.PartnersTransactionDetails),
            typeof(SYT.RozetkaPay.Models.Merchants.FeeDetailsResponse),
            typeof(SYT.RozetkaPay.Models.Merchants.TransactionDetailsListResponse),
            typeof(SYT.RozetkaPay.Models.Common.PartnersFeeDetails),
            typeof(SYT.RozetkaPay.Models.Common.PartnersTransactionDetails)
        ];

        // Still exported: removing them would break compiled consumers.
        foreach (Type staleType in staleTypes)
        {
            Assert.True(staleType.IsPublic, $"{staleType.FullName} must stay public.");
        }

        IEnumerable<Type> returnedTypes = typeof(IPartnerService)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(method => method.ReturnType)
            .Select(static returnType => returnType.IsGenericType ? returnType.GetGenericArguments()[0] : returnType);

        foreach (Type returnedType in returnedTypes)
        {
            Assert.DoesNotContain(returnedType, staleTypes);
        }

        Assert.Equal(
            typeof(SYT.RozetkaPay.Models.Partners.PartnerFeeDetailsResponse),
            typeof(IPartnerService).GetMethod(
                nameof(IPartnerService.GetFeeDetailsAsync),
                [typeof(CancellationToken)])!.ReturnType.GetGenericArguments()[0]);
        Assert.Equal(
            typeof(SYT.RozetkaPay.Models.Partners.PartnerTransactionDetailsListResponse),
            typeof(IPartnerService).GetMethod(
                nameof(IPartnerService.GetTransactionDetailsAsync),
                [typeof(string), typeof(CancellationToken)])!.ReturnType.GetGenericArguments()[0]);

        // The merchant-status response is deliberately the existing Models.Merchants type: its shape
        // already matches the official partner merchant-status response exactly.
        Assert.Equal(
            typeof(SYT.RozetkaPay.Models.Merchants.MerchantStatusResponse),
            typeof(IPartnerService).GetMethod(
                nameof(IPartnerService.GetMerchantStatusAsync),
                [typeof(CancellationToken)])!.ReturnType.GetGenericArguments()[0]);
    }

    /// <summary>
    /// <see cref="PaymentInstructionService"/> owns an internally created decline client, so it is
    /// disposable — but disposal is a lifetime concern, not an API operation. The implementation is
    /// explicit so the service contract keeps exactly its two official operations, and
    /// <see cref="IRozetkaPayClient"/> stays the only <see cref="IDisposable"/> SDK interface.
    /// </summary>
    [Fact]
    public void PaymentInstructionService_ShouldImplementDisposalExplicitly()
    {
        Assert.Contains(typeof(IDisposable), typeof(PaymentInstructionService).GetInterfaces());
        Assert.DoesNotContain(typeof(IDisposable), typeof(IPaymentInstructionService).GetInterfaces());

        Assert.Null(typeof(PaymentInstructionService).GetMethod(
            "Dispose",
            BindingFlags.Public | BindingFlags.Instance,
            Type.EmptyTypes));

        Type[] disposableInterfaces = AllExportedInterfaces
            .Where(static type => typeof(IDisposable).IsAssignableFrom(type))
            .ToArray();

        Assert.Equal([typeof(IRozetkaPayClient)], disposableInterfaces);
    }

    [Fact]
    public void RozetkaPayClient_ShouldBeUsableThroughTheContractOnly()
    {
        HttpClient httpClient = CreateStubHttpClient();

        using (IRozetkaPayClient client = new RozetkaPayClient(CreateConfiguration(), httpClient))
        {
            Assert.NotNull(client.Payments);
            Assert.NotNull(client.FinMon);
        }

        // The externally supplied HttpClient is not owned by the client, so it survives disposal and is
        // still usable. Asserted by using it, not by reading BaseAddress: the SDK no longer writes that
        // property, and a disposed client throws here.
        httpClient.CancelPendingRequests();
        httpClient.Dispose();
    }

    [Fact]
    public async Task ConsumerDependingOnIPaymentService_ShouldWorkWithHandWrittenFake()
    {
        PaymentOperationResult cannedResponse = new()
        {
            Id = "pay-1",
            ExternalId = "order-1",

            // The declared outcome flag. PaymentOperationResult has no Status - that belonged to
            // the legacy PaymentResponse this operation never actually returned.
            IsSuccess = true
        };
        FakePaymentService fake = new(cannedResponse);
        CheckoutPaymentGateway gateway = new(fake);

        CreatePaymentRequest request = new()
        {
            Amount = 100.00m,
            Currency = "UAH",
            ExternalId = "order-1",
            Mode = PaymentMode.Hosted,
            Customer = new CustomerInfo { Email = "customer@example.com" }
        };

        using CancellationTokenSource cancellationTokenSource = new();
        PaymentOperationResult response = await gateway.PayAsync(request, cancellationTokenSource.Token);

        Assert.Same(cannedResponse, response);
        Assert.Same(request, fake.LastCreateRequest);
        Assert.Equal(cancellationTokenSource.Token, fake.LastCancellationToken);
        Assert.Equal(1, fake.CreateCallCount);
    }

    [Fact]
    public void GeneratedXmlDocumentation_ShouldDocumentEveryInterfaceMember()
    {
        HashSet<string> documented = LoadDocumentedMemberNames();

        foreach (Type interfaceType in AllExportedInterfaces)
        {
            string typeName = interfaceType.FullName!;

            Assert.Contains($"T:{typeName}", documented);

            int documentedMethods = documented.Count(name => name.StartsWith($"M:{typeName}.", StringComparison.Ordinal));
            int documentedProperties = documented.Count(name => name.StartsWith($"P:{typeName}.", StringComparison.Ordinal));

            Assert.Equal(DescribeMethods(interfaceType).Count, documentedMethods);
            Assert.Equal(DeclaredProperties(interfaceType).Length, documentedProperties);
        }
    }

    [Fact]
    public void GeneratedXmlDocumentation_ShouldDocumentRepresentativeMembers()
    {
        HashSet<string> documented = LoadDocumentedMemberNames();

        Assert.Contains("P:SYT.RozetkaPay.IRozetkaPayClient.Payments", documented);
        Assert.Contains("P:SYT.RozetkaPay.IRozetkaPayClient.FinMon", documented);
        Assert.Contains(
            "M:SYT.RozetkaPay.Services.IPaymentService.CreateAsync(SYT.RozetkaPay.Models.Payments.CreatePaymentRequest,System.Threading.CancellationToken)",
            documented);
        Assert.Contains(
            "M:SYT.RozetkaPay.Services.IFinMonService.GetRulesAsync(System.Int32,System.Threading.CancellationToken)",
            documented);
    }

    private static HashSet<string> LoadDocumentedMemberNames()
    {
        string assemblyPath = typeof(IRozetkaPayClient).Assembly.Location;
        string documentationPath = Path.ChangeExtension(assemblyPath, ".xml");

        Assert.True(
            File.Exists(documentationPath),
            $"Generated XML documentation is part of the package and must be produced at {documentationPath}.");

        XElement? members = XDocument.Load(documentationPath).Root?.Element("members");

        Assert.NotNull(members);
        return members
            .Elements("member")
            .Select(member => member.Attribute("name")?.Value ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static TheoryData<Type, Type> ToTheoryData((Type Interface, Type Implementation)[] pairs)
    {
        TheoryData<Type, Type> data = new();
        foreach ((Type interfaceType, Type implementationType) in pairs)
        {
            data.Add(interfaceType, implementationType);
        }

        return data;
    }

    private static PropertyInfo[] DeclaredProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
    }

    /// <summary>
    /// Builds a stable, comparable description of the declared public instance methods of a type.
    /// Constructors, static members, property accessors and inherited members are excluded.
    /// </summary>
    private static List<string> DescribeMethods(Type type)
    {
        return type
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(DescribeMethod)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private static string DescribeMethod(MethodInfo method)
    {
        StringBuilder builder = new();
        builder.Append(DescribeType(method.ReturnType));
        builder.Append(' ');
        builder.Append(method.Name);
        builder.Append('`');
        builder.Append(method.GetGenericArguments().Length);
        builder.Append('(');
        builder.AppendJoin(", ", method.GetParameters().Select(DescribeParameter));
        builder.Append(')');
        return builder.ToString();
    }

    private static string DescribeParameter(ParameterInfo parameter)
    {
        string type = DescribeType(parameter.ParameterType);
        return parameter.IsOptional
            ? $"{type} {parameter.Name} = {DescribeDefaultValue(parameter)}"
            : $"{type} {parameter.Name}";
    }

    /// <summary>
    /// Renders a type without assembly qualification so that failures stay readable.
    /// </summary>
    private static string DescribeType(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.FullName ?? type.Name;
        }

        Type definition = type.GetGenericTypeDefinition();
        string name = definition.FullName ?? definition.Name;
        return $"{name}[{string.Join(",", type.GetGenericArguments().Select(DescribeType))}]";
    }

    /// <summary>
    /// Normalizes optional parameter defaults. Metadata represents <c>= default</c> for a struct and
    /// <c>= null</c> for a reference type without a stored constant, which surfaces through
    /// reflection as <see langword="null"/>, <see cref="DBNull"/> or <see cref="Missing"/> depending
    /// on the runtime; all of those collapse to a single token instead of a brittle literal.
    /// </summary>
    private static string DescribeDefaultValue(ParameterInfo parameter)
    {
        object? defaultValue;
        try
        {
            defaultValue = parameter.DefaultValue;
        }
        catch (FormatException)
        {
            return "<default>";
        }

        if (defaultValue is null || defaultValue is DBNull || ReferenceEquals(defaultValue, Missing.Value))
        {
            return "<default>";
        }

        return Convert.ToString(defaultValue, CultureInfo.InvariantCulture) ?? "<default>";
    }

    private static RozetkaPayConfiguration CreateConfiguration()
    {
        return new RozetkaPayConfiguration
        {
            BaseUrl = "https://api-epdev.rozetkapay.com",
            Login = "login",
            Password = "password"
        };
    }

    private static HttpClient CreateStubHttpClient()
    {
        StubHttpMessageHandler handler = new((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") }));

        return new HttpClient(handler);
    }

    /// <summary>
    /// Consumer-style collaborator that compiles against the SDK contract only, with no reference to
    /// any concrete service type.
    /// </summary>
    private sealed class CheckoutPaymentGateway
    {
        private readonly IPaymentService _payments;

        public CheckoutPaymentGateway(IPaymentService payments)
        {
            _payments = payments;
        }

        public Task<PaymentOperationResult> PayAsync(CreatePaymentRequest request, CancellationToken cancellationToken)
        {
            return _payments.CreateAsync(request, cancellationToken);
        }
    }
}
