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

## Package

- Package ID: `SYT.RozetkaPay`
- Current channel: `0.1.0-alpha.1`
- Target frameworks: `net9.0`, `net10.0`
- Repository: `https://github.com/i7aket/SYT.RozetkaPay`

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
    "BaseUrl": "https://api.rozetkapay.com"
  }
}
```

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

PaymentService payments = serviceProvider.GetRequiredService<PaymentService>();

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

## Direct Client Usage (without DI)

```csharp
using SYT.RozetkaPay;

RozetkaPayClient client = RozetkaPayClient.Create(
    baseUrl: "https://api.rozetkapay.com",
    login: "your_login",
    password: "your_password");

var paymentInfo = await client.Payments.GetInfoAsync("external-order-id");
```

## Webhook Payload Handling

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
