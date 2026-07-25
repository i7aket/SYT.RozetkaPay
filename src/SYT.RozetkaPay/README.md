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
SDK calls the right operation. The pinned snapshot now holds `59` paths and `67` operations — the
official document as observed on `2026-07-25` — and the SDK covers `59/59` paths with a typed method
for each of those `67` operations.

That is a statement about the **pinned document**, verified by
`tests/SYT.RozetkaPay.Tests/OpenApi59OperationTests.cs` and by the per-operation wire tests. It is
**not** a claim that a live sandbox has answered all 67 operations; end-to-end sandbox, authentication
and webhook coverage is tracked separately. See `docs/API_COMPATIBILITY.md`.

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

Two things produce HTTP log output when the SDK is registered through `AddRozetkaPay`: the SDK's own service
logging, and the built-in `IHttpClientFactory` handler logging. They have different guarantees, and the
difference matters — so read this section as scoped, not as a blanket claim.

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

There is no SDK-wide guarantee here, and none is claimed. The service logging was audited for the ten
operations of the OpenAPI 59/67 sync only.

**For those ten operations**, the logging captured by their tests contains the static route template and the
response status, and no caller identifier, credential, request body, response body, card number, encrypted
track 2 value, `RozetkaPayApiError.RawBody`, or — for the decline operation — the `Location` it returns:

```text
info: SYT.RozetkaPay.Services.SubscriptionService
      Making PATCH request to /api/subscriptions/v1/subscriptions/{subscription_id}/payment-method
dbug: SYT.RozetkaPay.Services.SubscriptionService
      Response status: OK
```

The identifiers covered are the subscription `subscription_id`, the in-store `external_id`, the partner
query identifiers, the payment-instruction payer and order values, and the decline `project_id` and
`payment_instruction_id`. Those operations are listed in `docs/API_COMPATIBILITY.md` under
**New Operations**.

The guarantee is exactly what the tests measure, under the default disabled retry policy: a per-operation
leak test in each service suite — `SubscriptionPaymentMethodUpdateTests`, `InStorePaymentServiceTests`,
`PartnerServiceTests` and `PaymentInstructionServiceTests`, which between them cover all ten operations —
plus `Exp354FactoryLoggingTests`, which drives a real `AddRozetkaPay` through a capturing
`ILoggerProvider` and checks the whole logging pipeline rather than the service statements alone.

**Every other SDK operation logs whatever it logged before this change, and that is not audited or claimed
to be identifier- or content-safe.** Concretely, in the current code:

- most operations log their **real request target**, and several routes embed a caller identifier in it —
  `ICustomerService.DeletePaymentFromWalletAsync` logs `/api/customers/v1/<customerId>/cards/<cardId>`,
  `IAlternativePaymentService.GetOperationAsync` logs
  `/api/alternative-payments/v1/operation/<externalId>`, `IPayPartsService` operation lookups log
  `/api/payparts/v1/operation/<operationId>`, and list operations log their query string;
- some operations log **method-specific values**. `IPaymentService.ConfirmP2PAsync` logs the external ID and
  the amount it is about to send;
- when you **enable retries**, the shared retry warning includes the transport exception message. That text
  comes from the runtime or the provider and the SDK does not control what it contains. Retries are disabled
  by default, so this path is inactive unless you turn it on.

Changing any of that would mean touching operations outside the scope of this change, so it was left alone.
Treat SDK log output as potentially containing identifiers and values unless the operation is one of the ten
listed above.

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
