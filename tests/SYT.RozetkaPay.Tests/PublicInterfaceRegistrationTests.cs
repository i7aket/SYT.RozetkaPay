using Microsoft.Extensions.DependencyInjection;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Extensions;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Verifies the interface aliases registered by <c>AddRozetkaPay</c>: an interface and its concrete
/// type share one scoped instance, scopes stay isolated, and a consumer registration made before
/// <c>AddRozetkaPay</c> wins.
/// </summary>
public class PublicInterfaceRegistrationTests
{
    private static readonly (Type Interface, Type Implementation)[] RegisteredContracts =
    [
        (typeof(IRozetkaPayClient), typeof(RozetkaPayClient)),
        (typeof(IPaymentService), typeof(PaymentService)),
        (typeof(IBatchPaymentService), typeof(BatchPaymentService)),
        (typeof(IPayPartsService), typeof(PayPartsService)),
        (typeof(IPayoutService), typeof(PayoutService)),
        (typeof(ICustomerService), typeof(CustomerService)),
        (typeof(ISubscriptionService), typeof(SubscriptionService)),
        (typeof(IReportService), typeof(ReportService)),
        (typeof(IAlternativePaymentService), typeof(AlternativePaymentService)),
        (typeof(IMerchantService), typeof(MerchantService)),
        (typeof(IFinMonService), typeof(FinMonService))
    ];

    [Fact]
    public void AddRozetkaPay_ShouldRegisterElevenInterfaceAliases()
    {
        ServiceCollection services = CreateRegisteredServices();

        foreach ((Type interfaceType, Type implementationType) in RegisteredContracts)
        {
            ServiceDescriptor interfaceDescriptor = Assert.Single(
                services, descriptor => descriptor.ServiceType == interfaceType);
            ServiceDescriptor concreteDescriptor = Assert.Single(
                services, descriptor => descriptor.ServiceType == implementationType);

            Assert.Equal(ServiceLifetime.Scoped, interfaceDescriptor.Lifetime);
            Assert.Equal(ServiceLifetime.Scoped, concreteDescriptor.Lifetime);
        }

        Assert.Equal(11, RegisteredContracts.Length);
    }

    [Fact]
    public void AddRozetkaPay_ShouldResolveInterfaceAndConcreteToTheSameScopedInstance()
    {
        using ServiceProvider provider = CreateRegisteredServices()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope scope = provider.CreateScope();

        foreach ((Type interfaceType, Type implementationType) in RegisteredContracts)
        {
            object viaInterface = scope.ServiceProvider.GetRequiredService(interfaceType);
            object viaConcreteType = scope.ServiceProvider.GetRequiredService(implementationType);

            Assert.IsAssignableFrom(interfaceType, viaInterface);
            Assert.IsType(implementationType, viaConcreteType);
            Assert.Same(viaConcreteType, viaInterface);

            // Resolving again inside the same scope must not create a second instance.
            Assert.Same(viaInterface, scope.ServiceProvider.GetRequiredService(interfaceType));
            Assert.Same(viaConcreteType, scope.ServiceProvider.GetRequiredService(implementationType));
        }
    }

    [Fact]
    public void AddRozetkaPay_ShouldGiveEachScopeItsOwnInstances()
    {
        using ServiceProvider provider = CreateRegisteredServices()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        using IServiceScope firstScope = provider.CreateScope();
        using IServiceScope secondScope = provider.CreateScope();

        foreach ((Type interfaceType, _) in RegisteredContracts)
        {
            object first = firstScope.ServiceProvider.GetRequiredService(interfaceType);
            object second = secondScope.ServiceProvider.GetRequiredService(interfaceType);

            Assert.NotSame(first, second);
        }
    }

    [Fact]
    public void AddRozetkaPay_ShouldResolveClientContractToTheConcreteClient()
    {
        using ServiceProvider provider = CreateRegisteredServices()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope scope = provider.CreateScope();

        IRozetkaPayClient contract = scope.ServiceProvider.GetRequiredService<IRozetkaPayClient>();
        RozetkaPayClient concrete = scope.ServiceProvider.GetRequiredService<RozetkaPayClient>();

        Assert.Same(concrete, contract);
        Assert.Same(concrete.Payments, contract.Payments);
        Assert.Same(concrete.FinMon, contract.FinMon);
    }

    [Fact]
    public void AddRozetkaPay_ShouldNotOverrideAConsumerRegisteredInterface()
    {
        ServiceCollection services = new();
        services.AddScoped<IPaymentService, FakePaymentService>();
        services.AddRozetkaPay(CreateConfiguration());

        using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope scope = provider.CreateScope();

        IPaymentService payments = scope.ServiceProvider.GetRequiredService<IPaymentService>();

        // The consumer registration wins because the SDK uses TryAdd.
        Assert.IsType<FakePaymentService>(payments);
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IPaymentService));

        // The concrete service and every other contract still resolve from the SDK registrations.
        Assert.IsType<PaymentService>(scope.ServiceProvider.GetRequiredService<PaymentService>());
        Assert.IsType<PayoutService>(scope.ServiceProvider.GetRequiredService<IPayoutService>());
        Assert.IsType<RozetkaPayClient>(scope.ServiceProvider.GetRequiredService<IRozetkaPayClient>());
    }

    [Fact]
    public void AddRozetkaPay_ShouldNotDuplicateInterfaceDescriptorsOnRepeatedRegistration()
    {
        ServiceCollection services = CreateRegisteredServices();
        services.AddRozetkaPay(CreateConfiguration());

        foreach ((Type interfaceType, _) in RegisteredContracts)
        {
            Assert.Single(services, descriptor => descriptor.ServiceType == interfaceType);
        }
    }

    private static ServiceCollection CreateRegisteredServices()
    {
        ServiceCollection services = new();
        services.AddRozetkaPay(CreateConfiguration());
        return services;
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
}
