using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SYT.RozetkaPay.Tests.TestInfrastructure;

/// <summary>
/// A real ASP.NET Core/Kestrel host, used by the EXP-337 HTTP-boundary tests as the far end of a real
/// socket. The SDK talks to it over an ordinary <see cref="HttpClient"/>, so what the endpoint observes
/// is what the SDK actually put on the wire - not what a stubbed <see cref="HttpMessageHandler"/> was
/// handed before serialization.
/// </summary>
/// <remarks>
/// <para>
/// The host binds to <see cref="IPAddress.Loopback"/> only, and to port <c>0</c> so the operating system
/// picks a free port. Nothing is reachable from outside the machine, no fixed port is claimed, and any
/// number of these can run concurrently under a parallel test run.
/// </para>
/// <para>
/// The bound address is read from <see cref="IServerAddressesFeature"/> after the server has started, so
/// <see cref="BaseAddress"/> is the port Kestrel really chose. There is no polling and no sleeping
/// anywhere in this type: <see cref="StartAsync"/> completes once the listener is accepting.
/// </para>
/// </remarks>
internal sealed class LoopbackWebApplication : IAsyncDisposable
{
    /// <summary>Bounded stop, so a wedged host fails the test rather than hanging the run.</summary>
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(10);

    private readonly IHost _host;

    private bool _stopped;

    private LoopbackWebApplication(IHost host, Uri baseAddress)
    {
        _host = host;
        BaseAddress = baseAddress;
    }

    /// <summary>
    /// Absolute base address of the running host, for example <c>http://127.0.0.1:53124</c>, with no
    /// trailing slash so it can be concatenated with an SDK route.
    /// </summary>
    internal Uri BaseAddress { get; }

    /// <summary>
    /// Base address as the string form the SDK configuration expects.
    /// </summary>
    internal string BaseUrl => BaseAddress.GetLeftPart(UriPartial.Authority);

    /// <summary>
    /// Start a host that serves exactly the endpoints <paramref name="configureEndpoints"/> maps.
    /// </summary>
    /// <param name="configureEndpoints">Endpoint registrations, run once during startup.</param>
    /// <param name="cancellationToken">
    /// Cancels startup. It is propagated to the host, so a cancelled caller does not leave a listener
    /// behind.
    /// </param>
    internal static async Task<LoopbackWebApplication> StartAsync(
        Action<IEndpointRouteBuilder> configureEndpoints,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configureEndpoints);

        // An empty builder: no ambient configuration sources, no console logging, no launch profile. The
        // host is exactly Kestrel plus routing, so a test cannot be influenced by machine configuration.
        WebApplicationBuilder builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseKestrelCore();
        builder.WebHost.ConfigureKestrel(static options =>
        {
            // Loopback only, ephemeral port. Never IPAddress.Any, and never a hard-coded port.
            options.Listen(IPAddress.Loopback, 0);
        });
        builder.Services.AddRoutingCore();

        WebApplication application = builder.Build();
        application.UseRouting();
        configureEndpoints(application);

        try
        {
            await application.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await application.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        try
        {
            return new LoopbackWebApplication(application, ResolveBaseAddress(application));
        }
        catch
        {
            // A host that started but cannot report its address must still be stopped.
            await StopAndDisposeAsync(application).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Stops the listener and releases the host. Safe to call more than once, and reached even when a
    /// test fails mid-assertion because callers hold this behind <c>await using</c>.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_stopped)
        {
            return;
        }

        _stopped = true;
        await StopAndDisposeAsync(_host).ConfigureAwait(false);
    }

    private static async Task StopAndDisposeAsync(IHost host)
    {
        try
        {
            using CancellationTokenSource shutdown = new(ShutdownTimeout);
            await host.StopAsync(shutdown.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // The bounded stop elapsed. Disposal below still tears the listener down.
        }
        finally
        {
            if (host is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                host.Dispose();
            }
        }
    }

    /// <summary>
    /// Read the address Kestrel bound, which is only known after startup because the port was ephemeral.
    /// </summary>
    private static Uri ResolveBaseAddress(IHost host)
    {
        IServerAddressesFeature? addresses = host.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>();

        string? address = addresses?.Addresses.FirstOrDefault();
        if (address is null)
        {
            throw new InvalidOperationException(
                "The loopback host started without reporting a bound address.");
        }

        Uri baseAddress = new(address);

        // A regression that binds every interface, or a non-loopback address, must fail here rather than
        // quietly expose a test endpoint to the network.
        if (!IPAddress.TryParse(baseAddress.Host, out IPAddress? ip) || !IPAddress.IsLoopback(ip))
        {
            throw new InvalidOperationException(
                $"The loopback host must bind a loopback address, but bound '{baseAddress.Host}'.");
        }

        return baseAddress;
    }
}
