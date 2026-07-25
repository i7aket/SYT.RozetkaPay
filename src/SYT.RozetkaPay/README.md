# SYT.RozetkaPay

`SYT.RozetkaPay` is a .NET SDK for RozetkaPay API integration.

It provides typed clients and models for:
- Payments (create, confirm, cancel, refund, refund retry/cancel, list, receipt, callback resend)
- PayParts (installments, refund retry/cancel)
- Payouts
- Customers and wallets
- Subscriptions (including gift subscriptions)
- Alternative payments (including callback resend)
- Merchant and FinMon APIs
- Webhook payloads (`PaymentWebhook`)

Every service is exposed through a public interface (`IPaymentService`, `IPayoutService`, …) plus a
single aggregate contract (`IRozetkaPayClient`), so application code can depend on abstractions and
substitute them in unit tests. See [Interfaces and Testing](#interfaces-and-testing).

## Package

- Package ID: `SYT.RozetkaPay`
- Target frameworks: `net9.0`, `net10.0`
- Repository: `https://github.com/i7aket/SYT.RozetkaPay`
- Versioning: release versions are published from SemVer Git tags (`vX.Y.Z[-prerelease]`) via MinVer

## API Compatibility

- API path version implemented by SDK: `v1` (`/api/*/v1/*`)
- OpenAPI schema version: `3.0.3`
- Local spec snapshot: `docs/openapi.json`
- Official docs/source of truth: `https://cdn.rozetkapay.com/public-docs/index.html`
- Last checked against official public docs: `2026-07-25`
- Detailed compatibility notes: `docs/API_COMPATIBILITY.md`

Path coverage and operation parity are reported separately: calling the right path does not prove the
SDK calls the right operation. The pinned snapshot holds `49` paths and `57` operations, and the SDK
covers `49/49` paths and reaches operation parity for those `57` operations. The live official document
observed on `2026-07-25` publishes `59` paths and `67` operations; refreshing the snapshot to that set
is tracked separately, and this SDK does **not** claim live `67/67` parity. See
`docs/API_COMPATIBILITY.md`.

## Known API Response Inconsistency

In production integrations, some endpoints may return numeric fields as JSON numbers (for example `123`) while others return numeric strings (for example `"123"`), which does not always match the OpenAPI type declarations.

This behavior was observed during integration testing and reported to RozetkaPay.  
As of `2026-02-28`, the behavior is still present on some endpoints.

To avoid runtime failures and to remain forward-compatible when API behavior is normalized, the SDK deserializes numeric fields from both formats.
In addition to dedicated converters for `decimal`/`int`/`long` types, global JSON number handling is configured to allow reading numeric values from strings.

## Installation

```bash
dotnet add package SYT.RozetkaPay --prerelease
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
| `RetryPolicy` | object | disabled | `Enabled`, `MaxRetryAttempts`, `BaseDelay`, `MaxDelay`, `BackoffStrategy`, `RetriableStatusCodes`. |

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
  the **same** canonical request target for the conditions it already supports — transport-level
  failures and `429`. A repeat is always the same operation against the same target, never a different
  route, verb or body, and `404` is not a retriable condition.
- **The legacy members still work.** `DeletePaymentFromWalletAsync`, `GetCustomerSubscriptionsAsync`
  and `CancelAsync` are obsolete warnings only, and their route, verb, body and response type are
  unchanged. `CancelAsync` still sends `external_id`, `reason` and `immediate`, none of which maps
  onto the official `refund` option — which is exactly why it was not redirected.

## Webhook Signature Verification

RozetkaPay signs every callback with the merchant password used for the payment operation and sends
the result in the `X-ROZETKAPAY-SIGNATURE` header. Verify it **before** you deserialize the body or
touch any order state. See the official
[callback source verification docs](https://docs.rozetkapay.com/guides/callbacks/).

Inject `IRozetkaPayWebhookSignatureVerifier`; `AddRozetkaPay` registers it as a singleton, and
`RozetkaPayWebhookSignatureVerifier.SignatureHeaderName` is the header name:

```csharp
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

    string? signature = request.Headers[
        RozetkaPayWebhookSignatureVerifier.SignatureHeaderName].FirstOrDefault();

    if (!verifier.Verify(rawBody, signature))
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
- **A missing or malformed header returns `false`, not an exception.** `signature` is nullable on
  purpose, so you do not need a null check before calling `Verify`. Empty, whitespace, wrongly padded
  and non-base64url values all fail closed.
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
