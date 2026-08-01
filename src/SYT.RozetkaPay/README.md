# SYT.RozetkaPay

`SYT.RozetkaPay` is a .NET SDK for RozetkaPay API integration.

It provides typed clients and models for:
- Payments (create, confirm, cancel, refund, refund retry/cancel, list, receipt, callback resend)
- PayParts (installments, refund retry/cancel)
- Payouts
- Customers and wallets
- Subscriptions (including gift subscriptions and payment-method replacement)
- Alternative payments (including callback resend)
- In-store (POS) payments (create, confirm, refund, info)
- Partner reporting (fee details, merchant status, transaction details)
- Payment instructions (batch creation and the unauthenticated decline redirect)
- Merchant and FinMon APIs
- Webhook payloads (`PaymentWebhook`)

Every service is exposed through a public interface (`IPaymentService`, `IPayoutService`, …) plus a
single aggregate contract (`IRozetkaPayClient`), so application code can depend on abstractions and
substitute them in unit tests. See [Interfaces and Testing](#interfaces-and-testing).

## Package

- Package ID: `SYT.RozetkaPay`
- Target framework: `net10.0`
- Repository: `https://github.com/i7aket/SYT.RozetkaPay`
- Versioning: release versions are published from SemVer Git tags (`vX.Y.Z[-prerelease]`) via MinVer
- The package ships an embedded icon, and a companion symbol package (`.snupkg`) is published to
  nuget.org alongside it

### Debugging into the SDK source

The published symbols carry Source Link metadata pinned to the exact repository commit each
release was built from, so a supported debugger can step from a compiled call straight into the
SDK source for that release. In Visual Studio, JetBrains Rider or VS Code, enable Source Link and
the NuGet.org symbol server and turn *Just My Code* off.

This applies to released packages restored from nuget.org. A local build from uncommitted changes
has no published commit to download source from, so stepping into it relies on your local files
instead.

## API Compatibility

- API path version implemented by SDK: `v1` (`/api/*/v1/*`)
- OpenAPI schema version: `3.0.3`
- Local spec snapshot: `docs/openapi.json`
- Official docs/source of truth: `https://cdn.rozetkapay.com/public-docs/index.html`
- Last checked against official public docs: `2026-07-25`
- Detailed compatibility notes: `docs/API_COMPATIBILITY.md`

Coverage is reported at three levels, because the weaker ones were being read as the stronger.

- **Routes.** The pinned snapshot holds `59` paths and `67` operations, and the SDK has a typed method
  for each. This has been true for a while and says nothing about what those methods send.
- **Request bodies.** Fifteen request bodies are checked property-by-property against the document, in
  both directions, so a missing field and an invented one both fail the build. The list is in
  `RequestBodyParityTests` and it is the record of what has actually been compared. Bodies not on it
  have not been.
- **Fields the SDK can receive.** No published schema declares a field the SDK has nowhere to put,
  with one recorded exception. `ModelFieldCoverageTests` holds that, and every exemption carries a
  reason and fails when it goes stale.

Enum values are compared as exact token sets against the document, in both directions, including the
two schemas that inherit their values through `allOf`.

Every one of those `67` operations has an executable contract row: the SDK method is invoked for real and
the request it produces — verb, concrete request target, percent-encoding, body policy, and authentication
headers — is asserted against the pinned document. The manifest and the document are compared as exact
sets, so an operation that is added, removed, renamed, duplicated, or moved to another verb fails the
build. Outbound authentication and the inbound webhook signature pipeline are additionally proven against
a real Kestrel server over a real socket. All of it runs in ordinary CI, on `net10.0`, with
no network access.

All of that is a statement about the **pinned document**, which a CI job compares against the live one
on every run — so "pinned" does not mean "possibly stale". It is **not** a claim that a live RozetkaPay
environment has answered all `67` operations — most of them move real money, so the SDK does not call them
against a live environment. The only live check is one opt-in, read-only merchant identity call; see
[Live sandbox smoke test](#live-sandbox-smoke-test) and `docs/API_COMPATIBILITY.md`.

## Known API Response Inconsistency

In production integrations, some endpoints may return numeric fields as JSON numbers (for example `123`) while others return numeric strings (for example `"123"`), which does not always match the OpenAPI type declarations.

This behavior was observed during integration testing and reported to RozetkaPay.  
As of `2026-02-28`, the behavior is still present on some endpoints.

To avoid runtime failures and to remain forward-compatible when API behavior is normalized, the SDK deserializes numeric fields from both formats.
In addition to dedicated converters for `decimal`/`int`/`long` types, global JSON number handling is configured to allow reading numeric values from strings.

## When a payment's state is unknown

A call that never came back is the one failure that can leave money in an unknown state. Two things about
it are easy to get wrong, and both cost real money.

### A timeout is a `RozetkaPayException`, and it tells you how many times you hit the provider

`RozetkaPayTransportException` derives from `RozetkaPayException`, so the documented catch clause sees it:

```csharp
try
{
    PaymentOperationResult result = await payments.CreateAsync(request, ct);
}
catch (RozetkaPayTransportException transport)
{
    // MayHaveReachedProvider is always true: by the time this is thrown the request was dispatched.
    // A payment may exist. Do not decide anything from the absence of a response.
    logger.LogError(
        "Payment {ExternalId} is ambiguous: timeout={IsTimeout}, dispatched {Attempts} time(s).",
        request.ExternalId, transport.IsTimeout, transport.AttemptsDispatched);
}
catch (OperationCanceledException) when (ct.IsCancellationRequested)
{
    // Your own cancellation stays yours and needs no reconciliation.
}
```

A timeout is **not retried**, even with `RetryPolicy.Enabled = true`. A connect failure costs nothing to
repeat; a timeout after dispatch may already have taken the money, so repeating it silently would turn one
ambiguous creation into several real ones.

### `data_not_found` from `/info` is **not** proof the payment does not exist

Verified against the live API: `GetInfoAsync` answers `data_not_found` for a payment that demonstrably
exists, for as long as the hosted checkout is unpaid — four attempts over twelve seconds, all
`data_not_found`, while the checkout page was open and working.

Reading that as "it was never created" and retrying with a fresh `external_id` **charges the customer
twice**. The absence of a record is not evidence of the absence of a payment.

### The safe protocol

1. Derive `external_id` **deterministically** from your own order — never a fresh GUID per attempt. The
   provider deduplicates by `external_id`, so a repeat of the same logical payment lands on the same
   payment instead of creating a second one.
2. Retry the *same* `external_id` on an ambiguous failure. That is safe precisely because of (1).
3. Treat the **callback** as the source of truth for the final state, not a poll of `/info`. Deduplicate
   inbound callbacks on `PaymentWebhook.EventKey`.
4. Never treat a missing record, a timeout, or a cancellation as "no payment happened".

## Status and trademarks

`SYT.RozetkaPay` is an **independent, community-maintained** SDK. It is not published, endorsed or
supported by RozetkaPay, and no affiliation is claimed.

RozetkaPay is a trademark of its owner. The name is used here only to say which API this library
speaks to. The package icon is an original mark generated from `assets/package-icon.svg`; it is not
the RozetkaPay logo and is not derived from any third-party asset.

For support with the payment service itself, contact RozetkaPay. For problems with this library, open
an issue on this repository.


## Installation

```bash
dotnet add package SYT.RozetkaPay
```

## Quick Start (ASP.NET Core)

### 1) Configure credentials

```json
{
  "RozetkaPay": {
    "Login": "your_login",
    "Password": "your_password",
    "Environment": "Production"
  }
}
```

`Environment` picks the endpoint, so no URL has to be written by hand. Keep the password out of source
control — see [Configuration](#configuration).

### 2) Register SDK in DI

```csharp
using SYT.RozetkaPay.Extensions;

builder.Services.AddRozetkaPay(builder.Configuration);
```

### 3) Create a hosted payment

```csharp
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Services;

IPaymentService payments = serviceProvider.GetRequiredService<IPaymentService>();

CreatePaymentRequest request = new()
{
    Amount = 100.00m,
    Currency = "UAH",
    ExternalId = $"order-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
    Mode = PaymentMode.Hosted,
    CallbackUrl = "https://example.com/api/v1/webhooks/rozetkapay",
    ResultUrl = "https://example.com/checkout/result",
    Confirm = true,
    Description = "Order payment",
    Customer = new CustomerInfo
    {
        Email = "customer@example.com",
        ColorMode = CheckoutColorMode.White,
        UserInfo = new CustomerUserInfo
        {
            Locale = CustomerCheckoutLocale.UK
        }
    }
};

PaymentResponse response = await payments.CreateAsync(request, cancellationToken);
string? checkoutUrl = response.Action?.Value ?? response.CheckoutUrl;
```

### 4) Or inject the whole SDK behind one contract

```csharp
using SYT.RozetkaPay;

IRozetkaPayClient client = serviceProvider.GetRequiredService<IRozetkaPayClient>();

PaymentResponse info = await client.Payments.GetInfoAsync("external-order-id", cancellationToken);
```

## Configuration

The SDK binds the `RozetkaPay` configuration section to `RozetkaPayOptions`
(`RozetkaPayOptions.SectionName`) and validates it through the standard options pipeline.

| Setting | Type | Default | Notes |
| --- | --- | --- | --- |
| `Login` | `string` | — | Required. |
| `Password` | `string` | — | Required. Also the key RozetkaPay signs callbacks with. |
| `Environment` | `Production` \| `Sandbox` | `Production` | Selects the endpoint. |
| `BaseUrl` | `string?` | *unset* | Explicit endpoint override; see below. |
| `OnBehalfOf` | `string?` | *unset* | `X-ON-BEHALF-OF` header (partnership mode). |
| `CustomerAuth` | `string?` | *unset* | `X-CUSTOMER-AUTH` header (customer wallet access). |
| `Timeout` | `TimeSpan` | `00:00:30` | Must be greater than zero. |
| `UserAgent` | `string` | `RozetkaPaySDK/.NET` | |
| `RetryPolicy` | object | disabled | `Enabled`, `MaxRetryAttempts`, `BaseDelay`, `MaxDelay`, `BackoffStrategy`, `RetriableStatusCodes`. See [Retry policy](#retry-policy). |

### Environments

| `Environment` | Endpoint |
| --- | --- |
| `Production` (default) | `https://api.rozetkapay.com` |
| `Sandbox` | `https://api-epdev.rozetkapay.com` |

Both are the servers published by the official RozetkaPay OpenAPI document, and both are available as
constants: `RozetkaPayOptions.ProductionBaseUrl` and `RozetkaPayOptions.SandboxBaseUrl`. Sandbox needs
sandbox credentials — production credentials will not authenticate there.

```json
{
  "RozetkaPay": {
    "Login": "your_sandbox_login",
    "Password": "your_sandbox_password",
    "Environment": "Sandbox"
  }
}
```

`Environment` defaults to `Production`, so an application that never sets it keeps talking to the endpoint
it always has.

### Live sandbox smoke test

The repository ships one test that talks to a real RozetkaPay environment, and it is off by default. It
calls only `validateMerchantKeys` (`GET /api/merchants/v1/me`) — a read-only identity check that changes
nothing — and it is skipped unless **both** environment variables are present:

```bash
ROZETKAPAY_SANDBOX_LOGIN='<your sandbox login>' \
ROZETKAPAY_SANDBOX_PASSWORD='<your sandbox password>' \
dotnet test tests/SYT.RozetkaPay.Tests/SYT.RozetkaPay.Tests.csproj -c Release --filter 'Category=Sandbox'
```

Everything else is deterministic and needs no credentials and no network:

```bash
dotnet test tests/SYT.RozetkaPay.Tests/SYT.RozetkaPay.Tests.csproj -c Release --filter 'Category!=Sandbox'
```

Without the variables the test reports
`Requires ROZETKAPAY_SANDBOX_LOGIN and ROZETKAPAY_SANDBOX_PASSWORD. No network call was made.` and makes
no request. Missing credentials are never a silent pass and never break an ordinary build.

No mutating operation — create, confirm, cancel, refund, payout, subscription, callback resend, report
generation, or payment instruction — is ever called against a live environment by this repository's tests.
Use sandbox credentials only, never production ones, and keep both out of source control, shell history,
and CI logs.

### BaseUrl override

`BaseUrl` overrides the endpoint of `Environment` and is meant for a private gateway, a proxy, or a local
test server. Leave it out to use the endpoint of the selected environment; only an absent value means
"not set", and an empty or whitespace `BaseUrl` is rejected rather than treated as unset. It must be an
absolute `http` or `https` URL.

### Configuring in code

For worker services, console applications, and tests there is an overload that takes the options directly —
no `IConfiguration` required:

```csharp
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Extensions;

services.AddRozetkaPay(options =>
{
    options.Login = login;
    options.Password = password;
    options.Environment = RozetkaPayEnvironment.Sandbox;
});
```

`RozetkaPayOptions` is available to your own code through `IOptions<RozetkaPayOptions>`.

### Startup validation

Validation is registered with `ValidateOnStart()`, so a broken configuration fails while the host is
starting rather than on the first payment request. Failures surface as `OptionsValidationException`, and the
message names the configuration key and the rule it broke — it never contains a login, a password, or an
authentication header.

Validated rules: `Login` and `Password` are present and not whitespace; `Environment` is a defined value
(there is no silent fallback to production); an explicit `BaseUrl` is an absolute `http`/`https` URL;
`Timeout` is greater than zero; and the retry policy is internally consistent (no negative attempts or
delays, and when retries are enabled, at least one attempt with `MaxDelay` no smaller than `BaseDelay`).

### Retry policy

**Retries are disabled by default.** Out of the box every operation is exactly one HTTP request; `RetryPolicy`
has to be turned on deliberately.

```csharp
services.AddRozetkaPay(options =>
{
    options.Login = login;
    options.Password = password;
    options.RetryPolicy = new RetryPolicy
    {
        Enabled = true,
        MaxRetryAttempts = 3,                            // 3 retries -> at most 4 total attempts
        BaseDelay = TimeSpan.FromSeconds(1),
        MaxDelay = TimeSpan.FromSeconds(30),
        BackoffStrategy = BackoffStrategy.ExponentialWithJitter
    };
});
```

`RetryPolicy.Standard` is the same thing preconfigured; `RetryPolicy.None` and `RetryPolicy.Default` are
disabled.

#### How many attempts

`MaxRetryAttempts` counts the retries **after** the initial call, so the total number of requests is exactly
`1 + MaxRetryAttempts`: `0` means one attempt, `1` means at most two, `3` means at most four. With
`Enabled = false` the budget is ignored entirely and one request is sent.

#### Which failures are repeated

A failure is repeated only when **all** of these hold: the policy is enabled, the budget still has room, the
caller's `CancellationToken` has not been cancelled, and the failure is one of:

- **an HTTP response whose status is in `RetriableStatusCodes`.** The decision reads the status of the response
  the SDK actually received — `RozetkaPayException.ApiError.StatusCode` — never an exception type or message.
  The default set is exactly:

  | Status | |
  |---|---|
  | `408` | `RequestTimeout` |
  | `429` | `TooManyRequests` |
  | `500` | `InternalServerError` |
  | `502` | `BadGateway` |
  | `503` | `ServiceUnavailable` |
  | `504` | `GatewayTimeout` |

- **a transport failure:** `HttpRequestException`, `SocketException`, or a `TaskCanceledException` that
  represents a timeout while the caller's own token is still live. These are the categories
  `RetryPolicy.ShouldRetry(Exception)` publishes, and the runtime honours exactly them.

`RetriableStatusCodes` is a plain `HashSet<HttpStatusCode>` and is honoured as configured — no status is
hard-coded on top of it. Remove `503` and a `503` is no longer repeated; add `409` and a `409` is; set it to
an empty collection and no status is ever repeated, while transport failures still are. A status outside the
default set keeps its usual exception type when it is retried, so a retried `400` still ends as
`RozetkaPayValidationException`.

Anything else — a validation failure the caller made, a response the SDK could not deserialize, an SDK
exception you constructed yourself — is not retried. An exception carrying no `ApiError` never came from an
HTTP response, so its class name alone does not make it retriable.

#### What a repeat sends

A repeat is the **same** request: same verb, same concrete request target including query values, same body
bytes, same content type, same authentication mode. The SDK never changes route, verb, or body between
attempts, and never follows a redirect it was told to return. Each attempt builds and releases its own
`HttpRequestMessage` and `HttpResponseMessage`, so nothing is carried over from a spent attempt — and each
fresh message is given the same authentication and configured headers, rather than picking them up from the
client's defaults (see [Supplying your own `HttpClient`](#supplying-your-own-httpclient)).

#### How long it waits

Non-`429` failures wait `BackoffStrategy` applied to `BaseDelay`, capped by `MaxDelay` for the exponential
strategies:

| `BackoffStrategy` | Delay before retry *n* |
|---|---|
| `Fixed` | `BaseDelay` |
| `Linear` | `BaseDelay × n` |
| `Exponential` | `BaseDelay × 2^(n-1)`, capped at `MaxDelay` |
| `ExponentialWithJitter` (default) | as `Exponential`, ±25 % random jitter |

Jitter is drawn from the runtime's shared random source (`Random.Shared`). Once that shared instance has
been initialized by the runtime, computing a delay no longer allocates a new generator per retry — the
previous implementation constructed one on every jittered delay — and `Random.Shared` is documented as safe
to use from multiple threads, so concurrent retries can compute delays at the same time. The band is ±25 % of
the already capped delay, and the result is never negative.

A `429` is the one case where the provider decides. If the response carries a `Retry-After` header the SDK
honours it **instead of** the backoff:

- **delta-seconds** (`Retry-After: 5`) is used as given;
- an **HTTP-date** is converted to a delay when the response is mapped;
- a value of zero, or a date already in the past, means retry immediately;
- a positive value is **capped by `MaxDelay`**, so a mistaken or hostile header cannot park a request for
  hours;
- an **absent or unparseable** header is treated as no hint at all and the configured backoff applies. An
  invalid header never replaces `RozetkaPayRateLimitException` with a parser error.

`Retry-After` on any status other than `429` is ignored for delay purposes. The wait observes the caller's
`CancellationToken`: cancelling during it ends the operation without sending the next request.

#### Cancellation

Caller cancellation is never a reason to retry. Once your token is cancelled the SDK does not schedule a
delay, does not invoke another attempt, and propagates the `OperationCanceledException` unwrapped. A
`TaskCanceledException` that comes from a **timeout** while your token is still live is a transport failure and
stays retriable.

**A token you have already cancelled sends nothing.** The SDK checks it itself, in its own shared transport
code, before the transport helper writes its `Making … request to …` log, before your request object is
serialized to JSON, before any retry bookkeeping, before an `HttpRequestMessage` exists, and before
`HttpClient` or your `HttpMessageHandler` is invoked. Your handler is called exactly **zero** times.

This is the SDK's own guarantee, not the runtime's. `HttpClient` also has a pre-dispatch check, but it fired
at different points across the frameworks this package used to target and behaves differently per verb, so
relying on it would make a cancelled request mean different things depending on where it ran. It is not
relied on, and dropping the second target framework does not change that — the guarantee is ours to keep, not
the runtime's to happen to provide.

The contract covers every transport family, with no verb left out:

| Helper family | Covered |
|---|---|
| Authenticated `GET` | yes |
| Authenticated `POST` with a JSON body | yes |
| Authenticated `POST` that accepts `204`/empty | yes |
| Authenticated `PATCH` with a JSON body | yes |
| Authenticated `POST` with **no** body at all | yes |
| Authenticated `DELETE`, with and without a JSON body | yes |
| The legacy-route `404` fallback wrappers (`GET`, `POST`, `POST`-accepting-`204`) | yes — primary **and** fallback |
| The unauthenticated, non-redirecting payment-instruction decline | yes |

Three further guarantees:

- **Your token comes back.** The `OperationCanceledException` carries **your** `CancellationToken`, not one
  the SDK invented, so `exception.CancellationToken == yourToken` holds and you can still tell your own
  cancellation from a timeout. (These are `async` methods: the exception surfaces when you `await` the returned
  task — the SDK does not promise to throw synchronously before the task is handed to you.)
- **A cancelled fallback is not a fallback.** If the primary endpoint answers `404` and your token is cancelled
  before the fallback is dispatched, the SDK stops there: no fallback request, and not even the "falling back"
  log line.
- **The retry policy does not change any of this.** Enabled or disabled, with any budget, the semantics are
  identical — the check happens before the policy is even read.

Cancelling **during** a request in flight is the other case, and it is unchanged: that one attempt is already at
the transport and may be observed there, but it is the only attempt. No retry follows, no fallback follows, and
the attempt still releases its request, body, response, and response content.

#### When the budget runs out

The exception you catch is the one the last attempt produced — not a wrapper. A retried-then-exhausted `429`
still throws `RozetkaPayRateLimitException`; an exhausted `500` still throws `RozetkaPayException` with its
usual message. Its `RozetkaPayApiError` carries the **final** response's status, provider code, request
identifier, and raw body, so support correspondence quotes the attempt that actually ended the call. Earlier
attempts' evidence is not merged in and not retained.

Each retry writes one `Warning`: the retry number, the budget, the failure category, the HTTP status when
there was a response, and the computed delay. It deliberately contains no exception message, no response
body, no provider text, no request target, and no credential — see [Logging](#logging).

#### Retries and money

> **A retry repeats a real request.** For a mutating financial operation — creating a payment, confirming,
> refunding — the provider may have already accepted the attempt that appeared to fail, so a repeat can result
> in a second operation. The SDK cannot make that safe on its own and does not claim exactly-once delivery.
> Before enabling retries for mutating calls, send a stable `external_id` / idempotency value you generate
> once per business intent and reuse across attempts, and reconcile by that identifier. If you cannot, keep
> retries off for those operations, or restrict `RetriableStatusCodes` to conditions your integration can prove
> are safe to repeat.

### One snapshot, no hot reload

The options value is read once and turned into a single snapshot that the named `HttpClient`, every service,
and the webhook verifier share, so they can never disagree about credentials or endpoint. Editing
`appsettings.json` at run time does **not** re-configure the SDK; rotating credentials or switching
environment requires a restart. `IOptionsMonitor<RozetkaPayOptions>` reload semantics are deliberately not
supported.

### Storing the password

Never commit credentials. Use `dotnet user-secrets` locally, environment variables or a secret store in
production, and repository secrets in CI:

```bash
dotnet user-secrets set "RozetkaPay:Password" "<your password>"
```

### TLS certificate validation

The SDK does not configure certificate validation and has no setting that turns it off. Every HTTPS request
is validated by the platform's own trust policy — the `HttpMessageHandler` defaults of .NET, using the
operating system trust store. When you pass your own `HttpClient`, its handler policy is yours and the SDK
neither inspects nor replaces it.

`ValidateSslCertificate` was **removed** from `RozetkaPayOptions` and `RozetkaPayConfiguration`. It never
reached an `HttpMessageHandler`: setting it to `false` changed no handler and no TLS behaviour, so it only
ever promised something the SDK did not do. Assigning it now fails to compile, and leaving the old
configuration key in place fails fast:

```text
RozetkaPay:ValidateSslCertificate was removed because it never controlled the HTTP handler. TLS certificate
validation always follows the platform or caller-supplied HttpMessageHandler policy. Remove this
configuration key.
```

`AddRozetkaPay(IConfiguration)` throws that `InvalidOperationException` whenever the key is present,
whatever its value — an ignored key would let an operator believe a TLS policy they configured is still in
force. The message names the key and never its value. **Migration: delete the key.** No replacement setting
is needed; validation was already the platform's.

To trust a certificate the platform does not:

- **Production — install the CA in the OS trust store.** A private or corporate CA belongs in the machine's
  trust store, where it applies to the whole host and is auditable. Nothing changes in the SDK.
- **Local or test infrastructure — own the handler yourself.** Build the `HttpClient` in your own code with
  a handler narrowed to the one certificate you mean to accept (pin its thumbprint; never accept every
  error), and hand it to `new RozetkaPayClient(configuration, httpClient)`. The trust decision then lives in
  your application, visible in review, and cannot leak into production through configuration.
- **Never install a trust-all callback in production.** `DangerousAcceptAnyServerCertificateValidator`, a
  `ServerCertificateCustomValidationCallback` that returns `true`, or any equivalent that ignores
  `SslPolicyErrors` disables authentication of the payment endpoint and opens the traffic — credentials and
  card data included — to interception. The SDK will not do this on your behalf, and there is no
  configuration that asks it to.

### Configuration objects still work

`RozetkaPayConfiguration` and the overloads that take it are still supported, and remain the way to
configure the client without DI. The only change is the removal of the obsolete `ValidateSslCertificate`
property described under **TLS certificate validation** above; nothing else about these overloads changed:

```csharp
services.AddRozetkaPay(new RozetkaPayConfiguration
{
    Login = login,
    Password = password,
    BaseUrl = RozetkaPayOptions.SandboxBaseUrl
});

services.AddRozetkaPay(login, password);
```

A `RozetkaPayConfiguration` is copied when it is registered, and its `BaseUrl` acts as an explicit endpoint
override. It also stays resolvable from DI as the SDK's configuration snapshot.

## Interfaces and Testing

`AddRozetkaPay` registers every service twice — once as its concrete type and once as its interface —
and both resolve to the **same scoped instance**:

| Contract | Implementation |
|---|---|
| `IRozetkaPayClient` | `RozetkaPayClient` |
| `IPaymentService` | `PaymentService` |
| `IBatchPaymentService` | `BatchPaymentService` |
| `IPayPartsService` | `PayPartsService` |
| `IPayoutService` | `PayoutService` |
| `ICustomerService` | `CustomerService` |
| `ISubscriptionService` | `SubscriptionService` |
| `IReportService` | `ReportService` |
| `IAlternativePaymentService` | `AlternativePaymentService` |
| `IMerchantService` | `MerchantService` |
| `IFinMonService` | `FinMonService` |
| `IInStorePaymentService` | `InStorePaymentService` |
| `IPartnerService` | `PartnerService` |
| `IPaymentInstructionService` | `PaymentInstructionService` |

The aggregate `IRozetkaPayClient` exposes each of them as a read-only property: `Payments`,
`BatchPayments`, `PayParts`, `Payouts`, `Customers`, `Subscriptions`, `Reports`, `AlternativePayments`,
`Merchants`, `FinMon`, `InStorePayments`, `Partners`, and `PaymentInstructions`.

Guidance:

- Depend on the interface at your application boundary — inject `IPaymentService` into the class that
  needs payments, or `IRozetkaPayClient` when a component needs the whole surface.
- The concrete types remain public and unchanged, so existing code that injects `PaymentService` or
  reads `client.Payments` as a `PaymentService` keeps compiling.
- Registrations use `TryAdd`, so an interface you register **before** calling `AddRozetkaPay` is not
  overwritten. That is how you swap in a fake for a single service.
- `RozetkaPayClient` builds its own service instances, so replacing `IPaymentService` in the container
  does not change `client.Payments`. Substitute `IRozetkaPayClient` to replace the aggregate, or the
  individual service interface for fine-grained injection.

In tests you can substitute a contract with any mocking framework, or with a plain hand-written fake
that needs no extra dependency:

```csharp
internal sealed class FakePaymentService : IPaymentService
{
    public CreatePaymentRequest? LastRequest { get; private set; }

    public Task<PaymentResponse> CreateAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        return Task.FromResult(new PaymentResponse { Status = "success" });
    }

    // Implement (or throw from) the remaining members your test does not exercise.
}
```

```csharp
ServiceCollection services = new();
services.AddScoped<IPaymentService>(_ => new FakePaymentService());
services.AddRozetkaPay(configuration); // does not overwrite the fake
```

## Direct Client Usage (without DI)

`RozetkaPayClient` creates its own `HttpClient` when you do not supply one, so dispose the client
when you construct it yourself. `IRozetkaPayClient` derives from `IDisposable` for that reason.

```csharp
using SYT.RozetkaPay;

using IRozetkaPayClient client = RozetkaPayClient.Create(
    baseUrl: "https://api.rozetkapay.com",
    login: "your_login",
    password: "your_password");

var paymentInfo = await client.Payments.GetInfoAsync("external-order-id");
```

### Supplying your own `HttpClient`

You can hand the SDK a client you own — one from your own `IHttpClientFactory`, one you share with other
services, or one wired to a test handler.

**The SDK does not write to its `DefaultRequestHeaders`.** Authentication (`Authorization`), the configured
`User-Agent`, and the optional `X-ON-BEHALF-OF` / `X-CUSTOMER-AUTH` headers are attached to each
`HttpRequestMessage` the SDK builds, and rebuilt for every retry attempt. That means:

- **your defaults survive.** A header you set on the client — including your own `Authorization` or
  `User-Agent` — is still there after construction and after every call. Nothing is cleared or removed;
- **no duplicates on the wire.** For a name the SDK sets on the request, the request value wins outright:
  `HttpClient` merges a default only for names the request does not already carry. The provider sees exactly
  one `Authorization` and one `User-Agent`;
- **your headers still flow.** Any name the SDK does *not* set — tracing, correlation, anything of your
  own — is merged onto the request as usual. If you configure no `OnBehalfOf` or `CustomerAuth`, a default of
  that name on your client is left alone and keeps being sent;
- **services do not fight over the client.** `RozetkaPayClient` builds every service over one client; two
  services configured differently over the same client each send their own credentials, including
  concurrently. Construction order does not change what anything sends.

Two properties are still set on a client you supply: `BaseAddress` and `Timeout` are taken from the SDK
configuration, so the endpoint and the timeout cannot disagree with the validated options. Everything else,
including the client's lifetime, stays yours — `RozetkaPayClient` disposes only a client it created itself.

## Request Encoding

Pass **raw** values to the SDK. Every value the SDK puts into a query string — external IDs, status
and date filters, pagination — is percent-encoded as a single query value, so a value containing
`&`, `=`, `?`, `#`, `/`, `%`, a space, or non-ASCII text stays one value instead of changing the
request target.

```csharp
// Raw value: one query parameter, no injected "status" and no fragment.
await client.Payments.GetInfoAsync("order 42+A&status=success");
// GET /api/payments/v1/info?external_id=order%2042%2BA%26status%3Dsuccess
```

A space becomes `%20` (never `+`), a literal `+` becomes `%2B`, and non-ASCII text is sent as UTF-8
percent-encoded octets. Because the SDK encodes exactly once, a pre-encoded value is treated as
literal text — `already%2Fencoded` is sent as `already%252Fencoded`. Values made only of unreserved
characters, such as `external-order-id` or `2026-02-28`, are unchanged. List filter dates and
pagination always use the invariant culture, so the ambient culture cannot alter a request URI.

## Request Identifier Encoding

Identifiers that the SDK places into the request **path** — plan, subscription, customer, card,
payment, operation, and external IDs — follow the same rule: pass them **raw** and never pre-encode
them. A query value and a path segment are different contexts, and each is encoded in its own
context, so reserved characters stay data instead of becoming request-target structure.

```csharp
// Raw value: one path segment, no extra segment from '/', no query from '?', no fragment from '#'.
await client.Subscriptions.GetPlanAsync("plan 7/8?x=1#z");
// GET /api/subscriptions/v1/plans/plan%207%2F8%3Fx%3D1%23z
```

Because the SDK encodes exactly once, a pre-encoded identifier is treated as literal text —
`already%2Fencoded` is sent as `already%252Fencoded`. An identifier made only of unreserved
characters, such as `plan-123`, reaches the wire byte-for-byte unchanged.

The identifiers `.` and `..` cannot be used and throw `ArgumentException` naming the parameter,
before any request is sent:

```csharp
// throws ArgumentException (ParamName: "planId")
await client.Subscriptions.GetPlanAsync("..");
```

They are rejected rather than encoded because `.` is an RFC 3986 unreserved character that
percent-encoding leaves unchanged, and `System.Uri` removes exact dot segments while building the
request. Sending them would silently address a different endpoint than the one you asked for. Every
other identifier is preserved. This rule does not change any endpoint name or HTTP method.

## Canonical Wallet and Subscription Operations

Three published operations were previously reachable only through a legacy verb, path, body or
response shape. The canonical members below call the documented operation; the legacy members are
`[Obsolete]` and keep their old behaviour byte-for-byte, so existing code keeps compiling and keeps
sending the same requests.

| operationId | Official request | Canonical method |
|---|---|---|
| `deleteCustomerPayment` | `DELETE /api/customers/v1/wallet` + JSON body | `ICustomerService.DeleteCustomerPaymentAsync` |
| `getSubscriptions` | `GET /api/subscriptions/v1/subscriptions` | `ISubscriptionService.GetSubscriptionsAsync` |
| `CancelCustomerSubscription` | `DELETE /api/subscriptions/v1/subscriptions/{subscription_id}/cancel`, no body | `ISubscriptionService.CancelCustomerSubscriptionAsync` |

### Delete a payment method from the wallet

```csharp
using SYT.RozetkaPay.Models.Customers;
using SYT.RozetkaPay.Services;

ICustomerService customers = serviceProvider.GetRequiredService<ICustomerService>();

DeleteCustomerPaymentRequest request = new()
{
    OptionId = "b1f0c1d2-0000-4000-8000-000000000000",
    Type = "card"
};

// Identify the customer by external ID.
DeleteCustomerPaymentResult byExternalId =
    await customers.DeleteCustomerPaymentAsync("customer-42", request, cancellationToken);

// Or rely on the configured CustomerAuth (X-CUSTOMER-AUTH) and send no external_id at all.
DeleteCustomerPaymentResult byCustomerAuth =
    await customers.DeleteCustomerPaymentAsync(request, cancellationToken);

bool removed = byExternalId.Delete;
```

### List customer subscriptions

```csharp
using SYT.RozetkaPay.Models.Subscriptions;
using SYT.RozetkaPay.Services;

ISubscriptionService subscriptions = serviceProvider.GetRequiredService<ISubscriptionService>();

SubscriptionList byExternalId =
    await subscriptions.GetSubscriptionsAsync("customer-42", cancellationToken);

// Or rely on the configured CustomerAuth (X-CUSTOMER-AUTH).
SubscriptionList byCustomerAuth = await subscriptions.GetSubscriptionsAsync(cancellationToken);

foreach (Subscription subscription in byExternalId.Subscriptions ?? [])
{
    Console.WriteLine($"{subscription.Id}: {subscription.State}");
}
```

The official response is a **root JSON array**. `SubscriptionList` keeps its existing public shape and
an internal converter maps that array onto `Subscriptions`, so the type is unchanged for source and
binary compatibility. The historical `{ "subscriptions": [...] }` wrapper is still read; an official
`[]` gives an empty list while a wrapper carrying `"subscriptions": null` gives `null`.

### Cancel a subscription

```csharp
using SYT.RozetkaPay.Models.Common;
using SYT.RozetkaPay.Models.Subscriptions;

// With explicit query options.
DefaultResponse cancelled = await subscriptions.CancelCustomerSubscriptionAsync(
    "1d85591b-891b-4b10-9d60-2078940d8e74",
    new CancelCustomerSubscriptionOptions { ExternalId = "customer-42", Refund = true },
    cancellationToken);
// DELETE /api/subscriptions/v1/subscriptions/1d85591b-.../cancel?external_id=customer-42&refund=true

// Or without options, letting the provider apply its default refund handling.
DefaultResponse cancelledDefault = await subscriptions.CancelCustomerSubscriptionAsync(
    "1d85591b-891b-4b10-9d60-2078940d8e74",
    cancellationToken);
// DELETE /api/subscriptions/v1/subscriptions/1d85591b-.../cancel
```

Rules that matter:

- **Pass raw identifiers.** `externalId` and `CancelCustomerSubscriptionOptions.ExternalId` are
  encoded exactly once as query values; `subscriptionId` is encoded exactly once as one path segment.
  See [Request Encoding](#request-encoding) and
  [Request Identifier Encoding](#request-identifier-encoding).
- **`CancelCustomerSubscriptionOptions` is not a request body.** The cancel operation sends **no body
  at all**; both members are rendered as query parameters, always in the order `external_id`, then
  `refund`, and `refund` is rendered lowercase (`true`/`false`).
- **`null` omits, empty does not.** A `null` option is left out of the request target. An empty
  `ExternalId` is sent as `external_id=` and validated by the provider.
- **A canonical operation never switches to a legacy route or verb.** A canonical `404` makes exactly
  one HTTP request and throws `RozetkaPayNotFoundException`, because the request and response shapes
  differ and a silent fallback would hide a parity error.
- **Retries repeat the same target, never a different one.** The default `RetryPolicy` is disabled, so
  a canonical call is a single request out of the box. With a retry policy enabled, the SDK may repeat
  the **same** canonical request target for the conditions the policy declares — the configured
  `RetriableStatusCodes` and transport-level failures, described under
  [Retry policy](#retry-policy). A repeat is always the same operation against the same target, never a
  different route, verb or body. `404` is not in the default retriable set, and even a caller who adds it
  gets a repeat of the same canonical request — never a legacy route.
- **The legacy members still work.** `DeletePaymentFromWalletAsync`, `GetCustomerSubscriptionsAsync`
  and `CancelAsync` are obsolete warnings only, and their route, verb, body and response type are
  unchanged. `CancelAsync` still sends `external_id`, `reason` and `immediate`, none of which maps
  onto the official `refund` option — which is exactly why it was not redirected.

## Replacing a Subscription Payment Method

`ISubscriptionService.UpdatePaymentMethodAsync` calls
`PATCH /api/subscriptions/v1/subscriptions/{subscription_id}/payment-method`. The configured
`CustomerAuth` (`X-CUSTOMER-AUTH`) identifies the customer when it is set.

```csharp
using SYT.RozetkaPay.Models.Payments;
using SYT.RozetkaPay.Models.Subscriptions;

UpdateSubscriptionPaymentMethodResponse result = await subscriptions.UpdatePaymentMethodAsync(
    "1d85591b-891b-4b10-9d60-2078940d8e74",
    new UpdateSubscriptionPaymentMethodRequest
    {
        ResultUrl = "https://example.com/subscription/updated",
        AutoRenew = true,
        PaymentMethod = new SubscriptionPaymentMethodUpdate
        {
            Type = SubscriptionPaymentMethodUpdateType.Wallet,
            Wallet = new CustomerWalletRequestPaymentMethod
            {
                OptionId = "b1f0c1d2-0000-4000-8000-000000000000"
            }
        }
    },
    cancellationToken);

// A 3DS or redirect step, when the provider requires one.
string? nextStep = result.UserAction?.Value;
```

`SubscriptionPaymentMethodUpdateType` covers every documented method — `CcToken`, `Wallet`, `GooglePay`,
`ApplePay`, `RecurrentId` — and each one fills the matching nested property. `AutoRenew` is tri-state:
`true` and `false` are both sent, and only `null` leaves the provider setting untouched. This is a new
type; the historical `SubscriptionPaymentMethod` describes a different shape and is unchanged.

## In-Store (POS) Payments

`IInStorePaymentService` covers the four official in-store operations.

```csharp
using SYT.RozetkaPay.Models.InStorePayments;
using SYT.RozetkaPay.Services;

IInStorePaymentService inStore = serviceProvider.GetRequiredService<IInStorePaymentService>();

InStorePaymentCreateResponse created = await inStore.CreateAsync(
    new InStorePaymentCreateRequest
    {
        ExternalId = "pos-order-1",
        PosTerminalId = "terminal-1",
        TerminalSn = "SN-0001",
        Amount = "10050",              // smallest monetary unit, as text
        Currency = InStorePaymentCurrency.Uah
    },
    cancellationToken);

InStorePaymentInfoResponse state = await inStore.GetInfoAsync("pos-order-1", cancellationToken);
```

Rules that matter:

- **Amounts are strings, in the smallest monetary unit.** `"10050"` means `100.50 UAH`. The official
  schema declares a string, so the SDK carries the value verbatim: leading zeros and exact provider text
  survive in both directions. Mapping the field onto `decimal` would rewrite it.
- **`Currency` is an enum whose only wire form is `"980"`.** `InStorePaymentCurrency.Uah` serializes to
  the literal string `980`, the ISO 4217 numeric code.
- **`GetInfoAsync` is a `POST` that sends no body.** The official operation declares no request body, so
  the SDK sends none — not an empty JSON object — and does not downgrade the verb to `GET`. The external
  ID travels as the `external_id` query value.
- **Confirm and refund carry cardholder data.** `CardNumber` and `EncryptedTrack2` on
  `InStorePaymentConfirmRequest` and `InStorePaymentRefundRequest` are sensitive. The SDK never logs a
  request body, a response body, or any identifier from these operations — apply the same rule in your
  own logs, error trackers, and crash reports, and keep these fields out of anything you persist for
  debugging.
- **The three receipt shapes are three types.** `InStorePaymentCreateReceiptData`,
  `InStorePaymentConfirmReceiptData` and `InStorePaymentRefundReceiptData` model what each operation
  actually returns; the refund receipt has no `fc_name`, because the official refund schema declares
  none.

## Partner Reporting

`IPartnerService` covers the three official partner operations. Every input is a query value: pass raw
values and the SDK encodes each exactly once.

```csharp
using SYT.RozetkaPay.Models.Partners;
using SYT.RozetkaPay.Services;

IPartnerService partners = serviceProvider.GetRequiredService<IPartnerService>();

// No query at all.
PartnerFeeDetailsResponse fees = await partners.GetFeeDetailsAsync(cancellationToken);

// GET /api/partners/v1/merchant-status?merchant_project_id=...&merchant_entity_id=...
MerchantStatusResponse status = await partners.GetMerchantStatusAsync(
    new PartnerMerchantStatusOptions
    {
        MerchantProjectId = "project-1",
        MerchantEntityId = "entity-1"
    },
    cancellationToken);

// merchant_entity_id is required by the operation, so it is a method parameter.
PartnerTransactionDetailsListResponse transactions = await partners.GetTransactionDetailsAsync(
    "entity-1",
    new PartnerTransactionDetailsOptions { MerchantOrderId = "order-1" },
    cancellationToken);
```

Rules that matter:

- **The no-argument overloads send no query string, not a bare `?`.**
- **`null` omits, empty does not.** A `null` option is left out of the request target; an empty string is
  sent as `merchant_project_id=` and validated by the provider.
- **Parameter order is fixed by the SDK**, so two identical calls always produce the same request target:
  `merchant_project_id` then `merchant_entity_id` for merchant status, and `merchant_entity_id`,
  `merchant_order_id`, `unified_external_id` for transaction details.
- **Result types are the `Models.Partners` ones.** `PartnerFeeDetailsResponse` and
  `PartnerTransactionDetailsListResponse` match the official responses. The similarly named historical
  types in `Models.Merchants` and `Models.Common` describe an older layout; they stay public and
  unchanged for consumers already compiled against them, but no partner operation returns them.
  Merchant status deliberately reuses `Models.Merchants.MerchantStatusResponse`, whose shape already
  matches the official response.
- **`ProcessedAt` is a string.** The official schema declares no date format, so the value is not parsed.

## Partnership Mode (paying on behalf of a child merchant)

RozetkaPay routes a payment to a child merchant with the `X-ON-BEHALF-OF` header — *"partnership mode,
when one core account operates with several children"*. Authentication stays the platform's, and the
child is named by identifier, so a platform never handles a merchant's own credentials.

For a single child, set it once in configuration (`OnBehalfOf`). For a platform, where each payment goes
to a *different* child, use `ActingFor`:

```csharp
IPaymentService payments = serviceProvider.GetRequiredService<IPaymentService>();

// One client, one HttpClient, many merchants.
PaymentOperationResult first = await payments
    .ActingFor("merchant-a")
    .CreateAsync(request, cancellationToken);

PaymentOperationResult second = await payments
    .ActingFor("merchant-b")
    .CreateAsync(other, cancellationToken);
```

Rules that matter:

- **`ActingFor` returns a new service; it does not mutate the one you called it on.** The original keeps
  whatever scope it had, and the copy shares your `HttpClient`. Nothing here is a field a concurrent
  request could read mid-change, which is the failure a mutable `OnBehalfOf` property would invite.
- **A per-call scope replaces the configured value**, it does not combine with it. Calling `ActingFor` on
  an already-scoped service re-scopes the copy rather than nesting.
- **A blank identifier is refused, with an `ArgumentException`, before anything is sent.** This is the one
  rule worth stating a reason for: acting for nobody is not the same as acting for the platform. Had the
  blank simply dropped the header, the request would have *succeeded* — booked to the core account, with
  nothing in the status code or the logs to say the expert was never paid.
- **An identifier that is not a legal header value raises `FormatException` while scoping**, the same
  error the configured value raises, so one `catch` covers both routes to the header.
- **The identifier never reaches a log.** Not by redaction — the SDK's service logging writes static route
  templates only, and `AddRozetkaPay` removes the factory's handler logging outright (see
  [Logging](#logging)). A test asserts it for the scoped value specifically, because that value never
  passes through the configuration the existing redaction tests cover.

## Payment Instructions

`IPaymentInstructionService` covers the two official payment-instruction operations. They do **not**
share an authentication mode.

```csharp
using SYT.RozetkaPay.Models.PaymentInstructions;
using SYT.RozetkaPay.Services;

IPaymentInstructionService instructions =
    serviceProvider.GetRequiredService<IPaymentInstructionService>();

// Authenticated: POST /api/payment-instructions/v1/new
PaymentInstructionsResult batch = await instructions.CreateAsync(
    new CreatePaymentInstructionsRequest
    {
        ProcessingType = PaymentInstructionProcessingType.CardPay,
        Method = PaymentInstructionMethod.Purchase,
        Currency = "UAH",
        BatchExternalId = "batch-1",
        Orders =
        [
            new PaymentInstructionOrder
            {
                ApiKey = "11111111-1111-1111-1111-111111111111",
                Amount = 100.50m,
                ExternalId = "order-1"
            }
        ]
    },
    cancellationToken);
```

`ProcessingType` and `Method` serialize to exactly `cardpay`/`ppay` and `auth`/`purchase`. The tokens are
pinned on the enum members rather than derived from the SDK naming policy, which would otherwise emit
`card_pay` and `p_pay`.

### Declining an instruction is unauthenticated and does not follow the redirect

`declinePaymentInstruction` is the one operation the official document declares `security: []`, and its
documented success is a bare HTTP `302` whose `Location` header is the entire result.

```csharp
PaymentInstructionDeclineResult declined = await instructions.DeclineAsync(
    "project-1",
    "instruction-1",
    cancellationToken);

// Always 302 on success. Location is the provider's target - the SDK did not visit it.
HttpStatusCode status = declined.StatusCode;
Uri location = declined.Location;
```

What the SDK guarantees, and what it deliberately leaves to you:

- **No RozetkaPay credential is sent.** The request goes over a dedicated client that carries no
  `Authorization`, `Proxy-Authorization`, `X-ON-BEHALF-OF` or `X-CUSTOMER-AUTH` header, even when your
  configuration sets them. Because `HttpClient` has no per-request redirect switch, this is a separate
  client over a separate handler rather than a flag.
- **The redirect is never followed.** That client's primary handler has `AllowAutoRedirect = false`. The
  SDK reads the `Location` header, returns it, and makes exactly one HTTP request.
- **The target is never fetched.** `DeclineAsync` does not read, resolve, or follow `Location`, and never
  copies a credential to the host named in it.
- **Navigating there is your decision, and validate it first.** `Location` is provider-controlled input.
  Redirecting a browser to it is the normal use. Fetching it *server-side* without validating scheme and
  host is a server-side request-forgery sink — treat it as untrusted data.
- **Neither identifier nor the location is logged.** Only the static route
  `/api/payment-instructions/v1/decline` reaches a log sink. A `302` without a usable `Location`, or a
  successful status that is not `302`, throws `RozetkaPayException` with a message that repeats neither
  the header value nor either identifier.
- **`302` is success, never an error or a retry trigger.** Other statuses map through the same
  status-to-exception table as every other operation, with `RozetkaPayApiError` attached.

When you construct the service yourself, the ordinary constructor is already safe — it builds its own
credential-free non-redirecting client and owns it, so dispose the service (or let the container own it)
to release that client:

```csharp
using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Services;

PaymentInstructionService service = new(configuration, httpClient);
using (service as IDisposable)
{
    PaymentInstructionDeclineResult result = await service.DeclineAsync("project-1", "instruction-1");
}
```

There is also a constructor that accepts a decline `HttpClient` you prepared yourself — this is what
`AddRozetkaPay` uses, with a dedicated named client. Such a client **must** be configured with
`AllowAutoRedirect = false`; that cannot be checked through the public `HttpClient` surface, so it is your
guarantee. What can be checked is: a client carrying any credential-bearing default header is rejected at
construction rather than silently stripped, and a client you supplied is never disposed by the service.

## Logging

Two things could produce HTTP log output when the SDK is registered through `AddRozetkaPay`: the SDK's own
service logging, and the built-in `IHttpClientFactory` handler logging. The second is removed outright; the
first writes static route templates only. Both statements hold for **every** operation.

### The built-in factory logging is removed — for every operation

**`AddRozetkaPay` calls `RemoveAllLoggers()` on both of its named clients** (`RozetkaPay` and
`RozetkaPay.PaymentInstructions.Decline`), so entries under `System.Net.Http.HttpClient.RozetkaPay.*` are not
emitted at all. This applies to every SDK operation that goes through those clients, new and pre-existing
alike.

It is deliberate. That logging writes the request URI, and while `Microsoft.Extensions.Http` redacts the whole
query to `?*`, it does **not** redact path segments — so any identifier the SDK places in a path reached the
log verbatim at Information level. Its header logging is redacted in the rendered message only; the
structured state of those entries carries the real header values, so a structured sink would record
`Authorization` and `X-CUSTOMER-AUTH` in clear at Trace level. Neither behaviour is configurable
(`RedactLoggedHeaders` covers headers only, and there is no hook for the URI), so the loggers are removed
outright.

### What the SDK's own service logging contains

**This is an SDK-wide contract now.** Every service operation logs a **static route template** and the
response status — never the real request target. The full per-operation audit is
[`docs/LOGGING_AUDIT.md`](docs/LOGGING_AUDIT.md).

A request has two targets, and they are deliberately different things. The **real request target** carries
your values, percent-encoded once each, and goes on the wire verbatim. The **log label** is a static route
template the SDK chose at compile time, and it is the only one a log sink ever sees:

```text
info: SYT.RozetkaPay.Services.SubscriptionService
      Making PATCH request to /api/subscriptions/v1/subscriptions/{subscription_id}/payment-method
dbug: SYT.RozetkaPay.Services.SubscriptionService
      Response status: OK
```

The `{subscription_id}` above is literal text, not your identifier. Where a route carries a value in the
**query**, the label is the path with no query at all.

None of the following is logged by any SDK service, on any code path: a caller identifier (external,
customer, card, plan, subscription, operation, payment, project, instruction or merchant), in either raw or
percent-encoded spelling; a credential, whether the configured login and password, the derived `Basic` value,
`X-CUSTOMER-AUTH`, `X-ON-BEHALF-OF`, or any other header; a request body, including a card number or an
encrypted track 2 value; a success response body; the parsed provider error message or
`RozetkaPayApiError.RawBody`; an exception object or its message; the `Location` that `DeclineAsync` returns.
The SDK also opens **no logging scope**.

`IPaymentService.ConfirmP2PAsync` used to log the external ID and the amount directly. That statement is
gone, and no substitute replaced it — the route label is the whole log entry, and both values are still sent
in the request body unchanged.

#### If you derive your own service from `BaseService`

The transport helpers that take no separate log label **fail closed**: they log the constant `[redacted]`
instead of the target they were given.

```text
info: MyCompany.Services.MyRozetkaPayService
      Making GET request to [redacted]
```

That is on purpose. A safe label cannot be derived from a target — `/api/payparts/v1/operation/info` and
`/api/payparts/v1/operation/12345` have the same shape, so any normalization would be guessing, and a wrong
guess is the leak. To keep route-level observability, pass your own static route template to the label-aware
overload:

```csharp
// Fails closed: logs "[redacted]".
return await GetAsync<MyResponse>($"/api/things/v1/{escapedId}", cancellationToken);

// Logs the template. The identifier still reaches the wire.
private const string ThingLogLabel = "/api/things/v1/{thing_id}";

return await GetAsync<MyResponse>($"/api/things/v1/{escapedId}", ThingLogLabel, cancellationToken);
```

The label must be a compile-time constant or literal. Never build one by interpolating an identifier,
concatenating a caller value, or reading a request DTO — that reintroduces exactly the leak the overload
exists to prevent. Every helper family has a label-aware form: `GetAsync`, `PostAsync`,
`PostAsyncWithNoContent`, `PatchAsync`, `PostWithoutBodyAsync`, `DeleteAsync` (with and without a body), and
all three `404` fallback wrappers, which take a label per side so the fallback entry names two templates and
no real target.

#### The retry and error logs

The **shared retry warning** is the same statement for every operation. It reports the retry number, the
configured budget, the failure category (an exception type name), the HTTP status when the failure came from
a response, and the computed delay in milliseconds — and nothing else. It does not render the exception
message, and carries no request target, no response body, no provider text, and no credential.

The **API error log** deliberately keeps three safe fields — `StatusCode`, `ApiCode` and `RequestId` — because
they are what support correspondence needs and none of them is caller content. The provider message and the
raw body are not logged; you still get both from the thrown exception.

Both surfaces predate this contract and are unchanged by it, as is the removal of the factory logging above.

### Adding your own HTTP telemetry

Removing the factory loggers does not touch your logging. If you need request-level telemetry for these
clients, add it with a `DelegatingHandler` or an `IHttpClientLogger` that logs a target you have redacted
yourself:

```csharp
services.AddRozetkaPay(configuration);
services.AddHttpClient("RozetkaPay").AddHttpMessageHandler(() => new MyRedactingLoggingHandler());
```

Log the route template, or a target with the identifier removed — not `request.RequestUri` as it stands.

## Webhook Signature Verification

RozetkaPay signs every callback with the merchant password used for the payment operation and sends
the result in the `X-ROZETKAPAY-SIGNATURE` header. Verify it **before** you deserialize the body or
touch any order state. See the official
[callback source verification docs](https://docs.rozetkapay.com/guides/callbacks/).

Inject `IRozetkaPayWebhookSignatureVerifier`; `AddRozetkaPay` registers it as a singleton, and
`RozetkaPayWebhookSignatureVerifier.SignatureHeaderName` is the header name:

```csharp
using Microsoft.Extensions.Primitives;
using SYT.RozetkaPay.Security;

app.MapPost("/callbacks/rozetkapay", async (
    HttpRequest request,
    IRozetkaPayWebhookSignatureVerifier verifier) =>
{
    // Read the body exactly as it arrived, before any JSON parsing.
    byte[] rawBody;
    using (MemoryStream buffer = new())
    {
        await request.Body.CopyToAsync(buffer);
        rawBody = buffer.ToArray();
    }

    // Exactly one header value. Zero is unauthenticated; more than one is ambiguous, and picking one of
    // them would let the sender choose which body is treated as authentic.
    StringValues header = request.Headers[
        RozetkaPayWebhookSignatureVerifier.SignatureHeaderName];

    if (header.Count != 1 || !verifier.Verify(rawBody, header[0]))
    {
        return Results.Unauthorized();
    }

    // Only now is it safe to deserialize rawBody and update the order.
    return Results.Ok();
});
```

Rules that matter:

- **Verify the raw bytes.** The signature covers the exact body RozetkaPay sent. Parsing the JSON and
  re-serializing it changes whitespace and property order, and the signature will no longer match.
  Deserialize only after `Verify` has returned `true`, and deserialize from those same bytes.
- **A missing or malformed header returns `false`, not an exception.** The parameter is nullable on
  purpose, so you do not need a null check before calling `Verify`. Empty, whitespace, wrongly padded
  and non-base64url values all fail closed.
- **Reject more than one signature header value yourself.** `Verify` takes a single value and cannot see
  that a second one arrived. `FirstOrDefault()` — or any other "pick one" — lets a sender append a header
  and choose which value is checked. Require `header.Count == 1` before verifying, as above.
- **The SDK does not touch `HttpRequest`.** Reading and, if your pipeline needs to read it again,
  rewinding or buffering the request stream (`request.EnableBuffering()`) is the application's job.
- **The SDK does not dictate an HTTP response.** Map `false` to 401 or 400 according to your own
  policy. Note that RozetkaPay treats any non-200 response as a delivery failure and will retry.
- **Never log the body, the signature, or the password.** The verifier takes no logger for exactly
  this reason.
- **Both overloads are equivalent** for a body that is already a UTF-8 string:
  `Verify(string, string?)` encodes it as UTF-8 and forwards to the byte overload. Passing `null` as
  the string payload throws `ArgumentNullException`.

The verifier is immutable and thread-safe, so the single registered instance can serve every request.
It reproduces the provider's algorithm, `base64url_encode(sha1(password + base64url_encode(body) +
password))`, compares digests in constant time, and accepts only the canonical padded base64url form
of the header.

## Webhook Payload Handling

Deserialize the payload only after
[the signature has been verified](#webhook-signature-verification).

```csharp
using System.Text.Json;
using SYT.RozetkaPay.Models.Payments;

PaymentWebhook? webhook = JsonSerializer.Deserialize<PaymentWebhook>(
    jsonPayload,
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

if (webhook?.IsSuccess == true &&
    string.Equals(webhook.Details?.Status, "success", StringComparison.OrdinalIgnoreCase))
{
    // mark order as paid
}
```

## Error Handling

The SDK throws typed exceptions, all deriving from `RozetkaPayException`:

| HTTP status | Exception |
|---|---|
| 400 | `RozetkaPayValidationException` |
| 401, 403 | `RozetkaPayAuthorizationException` |
| 404 | `RozetkaPayNotFoundException` |
| 429 | `RozetkaPayRateLimitException` |
| 500 and any other non-success status | `RozetkaPayException` |

```csharp
try
{
    var response = await payments.CreateAsync(request, cancellationToken);
}
catch (RozetkaPayValidationException ex)
{
    // invalid request payload
}
catch (RozetkaPayAuthorizationException ex)
{
    // wrong credentials or access denied
}
```

### Structured API error details

Every exception raised from a non-success HTTP response carries a `RozetkaPayApiError` on the
`RozetkaPayException.ApiError` property:

| Member | Type | Meaning |
|---|---|---|
| `StatusCode` | `System.Net.HttpStatusCode` | HTTP status of the failed response |
| `Code` | `string?` | Provider error code as text, `null` when the response carries none |
| `RequestId` | `string?` | Request identifier for support correspondence, `null` when absent |
| `RawBody` | `string` | Response body exactly as received, `string.Empty` for an empty body |

```csharp
try
{
    PaymentResponse response = await payments.GetInfoAsync(
        externalId,
        cancellationToken);
}
catch (RozetkaPayException exception)
    when (exception.ApiError is { } error)
{
    logger.LogWarning(
        "RozetkaPay request failed: status={Status}, code={Code}, requestId={RequestId}",
        error.StatusCode,
        error.Code,
        error.RequestId);

    // Treat error.RawBody as sensitive. Scrub it before logging or storage.
}
```

`Code` is a `string`, not an enum, on purpose: RozetkaPay adds error codes between SDK releases, so an
unrecognized code is returned unchanged instead of failing to deserialize or being mapped onto a wrong
fallback value. A numeric code is returned as its raw JSON text. The code is read from the top-level
`code` field, falling back to `error.code`.

`RequestId` is resolved in a fixed order — the `X-Request-Id` response header, then the `Request-Id`
response header, then the `error_id` field of the payload, then `error.error_id` — and is `null` when the
response carries none. Header name matching is case-insensitive, and blank values are skipped. The
request-ID headers are not declared by the official OpenAPI document but are commonly added by gateways;
`error_id` is the identifier the documented error payload declares.

`RawBody` is the provider payload verbatim — never reformatted, never replaced by a parser error — so a
malformed body, a plain-text body, or fields this SDK version does not model all remain inspectable. A
body the SDK cannot parse still produces the status-specific exception above, with `Code` and `RequestId`
left `null`.

> **`RawBody` may contain customer or provider data.** The SDK never logs it and never puts it in
> `Exception.Message` or `Exception.ToString()`; it logs only the HTTP status, the API code, and the
> request ID. Scrub the raw body before writing it to a log, a store, or an error tracker.

`ApiError` is `null` when the failure did not come from an HTTP response — a manually constructed
exception, a transport failure, or a response the SDK could not deserialize:

```csharp
// ApiError is null: nothing was received from the API.
var manual = new RozetkaPayException("cannot reach the gateway");
```

All pre-existing exception constructors — parameterless, `(string message)`, and
`(string message, Exception innerException)` — remain public and unchanged on every exception type, and
none is obsolete.

## Maintainer

|  |  |
|---|---|
| ![Anatoliy Yermakov](https://raw.githubusercontent.com/i7aket/SYT.RozetkaPay/main/src/SYT.RozetkaPay/docs/images/anatoliy-yermakov.jpeg) | Maintained by **Anatoliy Yermakov** for RozetkaPay integrators. Support is provided on a best-effort basis as time permits. |

## License

This project is licensed under the MIT License.
See the `LICENSE` file in the repository root for details.

## Notes

- Public API namespaces use `SYT.RozetkaPay.*`.
