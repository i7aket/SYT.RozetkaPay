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
- Last checked against official public docs: `2026-02-28`
- Detailed compatibility notes: `docs/API_COMPATIBILITY.md`

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
| `ValidateSslCertificate` | `bool` | `true` | |
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

### Configuration objects still work

`RozetkaPayConfiguration` and the overloads that take it are unchanged, and remain the way to configure the
client without DI:

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

The SDK throws typed exceptions:
- `RozetkaPayException`
- `RozetkaPayAuthorizationException`
- `RozetkaPayValidationException`
- `RozetkaPayRateLimitException`

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

## Maintainer

|  |  |
|---|---|
| ![Anatoliy Yermakov](https://raw.githubusercontent.com/i7aket/SYT.RozetkaPay/main/src/SYT.RozetkaPay/docs/images/anatoliy-yermakov.jpeg) | Maintained by **Anatoliy Yermakov** for RozetkaPay integrators. Support is provided on a best-effort basis as time permits. |

## License

This project is licensed under the MIT License.
See the `LICENSE` file in the repository root for details.

## Notes

- Public API namespaces use `SYT.RozetkaPay.*`.
