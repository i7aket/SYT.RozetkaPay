using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Services;
using SYT.RozetkaPay.Tests.TestInfrastructure;

namespace SYT.RozetkaPay.Tests;

/// <summary>
/// Every route the SDK actually dispatches is one the document declares.
/// </summary>
/// <remarks>
/// <para>
/// This watches the wire instead of reading the source, and that is the whole point. Two earlier
/// gates both failed on the same class of defect for the same reason — they read what the code says
/// rather than observing what it does.
/// </para>
/// <para>
/// <c>OffSpecRouteTests</c> reflects over string constants and skips any value containing a
/// placeholder, so five interpolated targets were invisible to it. <c>OperationTypeMappingTests</c>
/// trusts <c>OpenApiOperationManifest</c>, which is hand-written and can disagree with the code:
/// <c>GetOperationInfoAsync</c> is mapped there to a declared operation while the implementation
/// builds <c>/api/payparts/v1/operation/{id}</c> — undeclared, and <c>404</c> against the live
/// gateway. Neither gate could see it. A recording handler can.
/// </para>
/// <para>
/// Each operation is invoked with an auto-filled request. Filling is generic rather than
/// per-operation on purpose: hand-written fixtures would encode the very field knowledge under test,
/// which is how six contract fixtures ended up pinning the defects they were meant to catch.
/// Required members are populated because validation now runs before dispatch, and a request that
/// fails validation never reaches the handler and would silently drop out of coverage — so an
/// operation that dispatches nothing is itself a failure here.
/// </para>
/// </remarks>
public class DispatchedRouteTests
{
    /// <summary>
    /// Operations whose dispatched route is undeclared, and the ticket that removes them.
    /// </summary>
    /// <remarks>
    /// Deliberately a literal list, and deliberately small. Every entry is a public method a caller
    /// can reach that cannot succeed.
    /// </remarks>
    private static readonly Dictionary<string, string> KnownUndeclared = new(StringComparer.Ordinal)
    {
        ["DELETE /api/customers/v1/{}/cards/{}"] = "EXP-429; declared form is DELETE /api/customers/v1/wallet",
        ["GET /api/alternative-payments/v1/operation/{}"] = "EXP-429; declared form is /info/operation, 404 live",
        ["GET /api/alternative-payments/v1/{}/status"] = "EXP-429; no declared equivalent, 404 live",
        ["GET /api/customers/v1/{}/cards"] = "EXP-429; declared form is GET /api/customers/v1/wallet, 404 live",
        ["GET /api/payparts/v1/operation/{}"] = "EXP-429; declared form is /info/operation, 404 live",

        // Found only by this gate. Static reading missed both: the manifest maps these methods to
        // declared operations, and the targets are interpolated so the constant-based gate skipped
        // them too.
        ["GET /api/subscriptions/v1/subscriptions/customer/{}"] = "EXP-429; no declared equivalent",
        ["POST /api/subscriptions/v1/subscriptions/{}/cancel"] = "EXP-429; the declared cancel is DELETE",
    };

    [Fact]
    public async Task EveryDispatchedRoute_ShouldBeDeclaredInTheDocument()
    {
        (List<string> undeclared, List<string> silent) = await ObserveAsync();

        List<string> offenders = [.. undeclared
            .Where(static route => !KnownUndeclared.ContainsKey(route))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];

        Assert.Empty(offenders);
    }

    /// <summary>
    /// Every known-undeclared route is still dispatched. One that is not has been fixed, and the
    /// entry has to go with it.
    /// </summary>
    [Fact]
    public async Task EveryKnownUndeclaredRoute_ShouldStillBeDispatched()
    {
        (List<string> undeclared, _) = await ObserveAsync();
        HashSet<string> observed = [.. undeclared];

        List<string> stale = [.. KnownUndeclared.Keys
            .Where(route => !observed.Contains(route))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(stale);
    }

    /// <summary>
    /// Reports how much of the surface this gate actually observed.
    /// </summary>
    /// <remarks>
    /// A gate that silently observes nothing passes forever. Rather than assert a count that would
    /// need editing on every change, this fails only if coverage collapses — which is what a broken
    /// auto-filler looks like.
    /// </remarks>
    [Fact]
    public async Task TheGate_ShouldObserveMostOfTheSurface()
    {
        (List<string> undeclared, List<string> silent) = await ObserveAsync();

        int observed = undeclared.Count + DeclaredHits;

        // IPaymentInstructionService.CreateAsync and IPaymentService.CreateP2PAsync send nothing:
        // both reject the generically filled request before dispatch. Named here so the two are a
        // known limit of this gate rather than an unnoticed hole.
        Assert.True(
            observed >= 40,
            $"Only {observed} operations dispatched; {silent.Count} sent nothing: "
            + string.Join(", ", silent.Take(15)));
    }

    private static int DeclaredHits;

    /// <summary>
    /// Invokes every service operation against a recording handler and classifies what it sent.
    /// </summary>
    private static async Task<(List<string> Undeclared, List<string> Silent)> ObserveAsync()
    {
        string[] templates = [.. OpenApiSnapshot.DeclaredOperations().Select(static o => $"{o.Method} {o.Path}")];
        List<string> undeclared = [];
        List<string> silent = [];
        int declaredHits = 0;

        foreach ((Type contract, Type implementation) in ServicePairs())
        {
            foreach (MethodInfo method in contract.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.IsSpecialName || method.ReturnType == typeof(void))
                {
                    continue;
                }

                string? observedRoute = await InvokeAndCaptureAsync(implementation, method);

                if (observedRoute is null)
                {
                    silent.Add($"{contract.Name}.{method.Name}");
                    continue;
                }

                if (templates.Any(template => Matches(observedRoute, template)))
                {
                    declaredHits++;
                }
                else
                {
                    undeclared.Add(observedRoute);
                }
            }
        }

        DeclaredHits = declaredHits;

        return (undeclared, silent);
    }

    /// <summary>
    /// Builds the service over a recording handler, calls the method, and returns
    /// <c>VERB /path</c> with concrete segments replaced by <c>{}</c>.
    /// </summary>
    private static async Task<string?> InvokeAndCaptureAsync(Type implementation, MethodInfo method)
    {
        string? captured = null;

        StubHttpMessageHandler handler = new((request, _) =>
        {
            captured ??= $"{request.Method.Method} {request.RequestUri!.AbsolutePath}";

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json"),
            });
        });

        using HttpClient httpClient = new(handler);

        object? service = Construct(implementation, httpClient);

        if (service is null)
        {
            return null;
        }

        try
        {
            object?[] arguments = [.. method.GetParameters().Select(static p => Fill(p.ParameterType, 0))];

            if (method.Invoke(service, arguments) is Task task)
            {
                await task;
            }
        }
        catch (Exception)
        {
            // A rejected argument, a validation failure or a deserialization complaint are all fine:
            // the question is only what left the process before it happened.
        }
        finally
        {
            (service as IDisposable)?.Dispose();
        }

        return captured is null ? null : Normalise(captured);
    }

    /// <summary>
    /// Replaces the concrete values this test supplied with <c>{}</c>, so a dispatched route can be
    /// compared against a declared template.
    /// </summary>
    private static string Normalise(string route)
    {
        string[] parts = route.Split(' ', 2);

        IEnumerable<string> segments = parts[1]
            .Split('/')
            .Select(static segment => segment == Probe ? "{}" : segment);

        return $"{parts[0]} {string.Join('/', segments)}";
    }

    /// <summary>
    /// Whether a dispatched route matches a declared template, a <c>{param}</c> matching one segment.
    /// </summary>
    private static bool Matches(string route, string template)
    {
        string[] left = route.Split(' ', 2);
        string[] right = template.Split(' ', 2);

        if (!string.Equals(left[0], right[0], StringComparison.Ordinal))
        {
            return false;
        }

        string[] actual = left[1].Split('/');
        string[] declared = right[1].Split('/');

        if (actual.Length != declared.Length)
        {
            return false;
        }

        return actual.Zip(declared).All(static pair =>
            pair.Second.StartsWith('{') || string.Equals(pair.First, pair.Second, StringComparison.Ordinal));
    }

    /// <summary>
    /// The value every string argument is filled with.
    /// </summary>
    /// <remarks>
    /// Deliberately not a word that could appear in a real path. Normalisation replaces segments
    /// equal to this value with a placeholder, so a plain word like "probe" would mask a literal
    /// path segment of the same name and hide part of the route being checked.
    /// </remarks>
    private const string Probe = "x-probe-7f3a";

    /// <summary>
    /// Builds a service over the given client, whatever constructor shape it offers.
    /// </summary>
    /// <remarks>
    /// Every service takes <c>(configuration, httpClient, ILogger? logger = null)</c>, and
    /// <c>Activator.CreateInstance</c> does not fill optional parameters — the two-argument call
    /// silently found no constructor, which is why the coverage guard reported zero dispatches on
    /// the first run. Each public constructor is tried, longest first, with every parameter filled.
    /// </remarks>
    private static object? Construct(Type implementation, HttpClient httpClient)
    {
        foreach (ConstructorInfo constructor in implementation
            .GetConstructors()
            .OrderByDescending(static candidate => candidate.GetParameters().Length))
        {
            object?[] arguments = [.. constructor.GetParameters().Select(parameter =>
                parameter.ParameterType == typeof(RozetkaPayConfiguration) ? Configuration()
                : parameter.ParameterType == typeof(HttpClient) ? httpClient
                : parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType)
                : null)];

            try
            {
                return constructor.Invoke(arguments);
            }
            catch (Exception)
            {
                // Try a narrower constructor.
            }
        }

        return null;
    }

    /// <summary>
    /// A value of the requested type, filling required members so validation lets the call through.
    /// </summary>
    private static object? Fill(Type type, int depth)
    {
        Type resolved = Nullable.GetUnderlyingType(type) ?? type;

        if (resolved == typeof(CancellationToken)) return CancellationToken.None;
        if (resolved == typeof(string)) return Probe;
        if (resolved == typeof(bool)) return true;
        if (resolved == typeof(int) || resolved == typeof(long)) return 1;
        if (resolved == typeof(decimal)) return 1m;
        if (resolved == typeof(double) || resolved == typeof(float)) return 1d;
        if (resolved == typeof(Guid)) return Guid.Empty;
        if (resolved == typeof(DateTime)) return new DateTime(2026, 7, 30, 0, 0, 0, DateTimeKind.Utc);
        if (resolved == typeof(DateTimeOffset)) return DateTimeOffset.UnixEpoch;
        if (resolved == typeof(DateOnly)) return new DateOnly(2026, 7, 30);
        if (resolved.IsEnum) return Enum.GetValues(resolved).GetValue(0);

        if (depth > 3)
        {
            return null;
        }

        if (resolved.IsGenericType && resolved.GetGenericTypeDefinition() == typeof(List<>))
        {
            object list = Activator.CreateInstance(resolved)!;
            object? element = Fill(resolved.GetGenericArguments()[0], depth + 1);

            if (element is not null)
            {
                resolved.GetMethod("Add")!.Invoke(list, [element]);
            }

            return list;
        }

        if (resolved.IsGenericType && resolved.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            return Activator.CreateInstance(resolved);
        }

        if (!resolved.IsClass || resolved.GetConstructor(Type.EmptyTypes) is null)
        {
            return null;
        }

        object instance = Activator.CreateInstance(resolved)!;

        foreach (PropertyInfo property in resolved.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite || property.GetCustomAttribute<RequiredAttribute>() is null)
            {
                continue;
            }

            object? value = Fill(property.PropertyType, depth + 1);
            if (value is not null)
            {
                property.SetValue(instance, value);
            }
        }

        return instance;
    }

    private static IEnumerable<(Type Contract, Type Implementation)> ServicePairs()
    {
        Assembly assembly = typeof(BaseService).Assembly;

        foreach (Type contract in assembly.GetExportedTypes()
            .Where(static type => type.IsInterface && type.Namespace == "SYT.RozetkaPay.Services")
            .OrderBy(static type => type.Name, StringComparer.Ordinal))
        {
            Type? implementation = assembly.GetExportedTypes().FirstOrDefault(type =>
                type.IsClass && !type.IsAbstract && contract.IsAssignableFrom(type));

            if (implementation is not null)
            {
                yield return (contract, implementation);
            }
        }
    }

    private static RozetkaPayConfiguration Configuration() => new()
    {
        BaseUrl = RozetkaPayOptions.ProductionBaseUrl,
        Login = "probe-login",
        Password = "probe-password",
        RetryPolicy = RetryPolicy.None,
    };
}
