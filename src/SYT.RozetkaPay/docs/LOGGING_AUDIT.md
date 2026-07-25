# SDK Service Logging Audit

This document is the complete audit of what the SDK's **own** service logging writes. It covers every
transport helper in `BaseService` and every callsite in all 13 service implementation files.

It is a statement about the SDK's own log statements. The two other log-producing surfaces — the shared
retry warning and the API error log — are described in [Surfaces that stay separate](#surfaces-that-stay-separate),
and the built-in `IHttpClientFactory` handler logging is removed outright (see the same section).

## The real request target and the log label are two different things

Every request has two:

| | What it is | Where it goes |
|---|---|---|
| **Real request target** | The concrete path and query, with every caller value percent-encoded once at its own insertion point. | The wire, verbatim. |
| **Log label** | A static route template chosen by the SDK at compile time. | A log sink. |

They used to be the same string in most helpers, which is exactly why a caller identifier ended up in log
output: a sink writes whatever it is handed, in the rendered message **and** in the structured state
(`Endpoint`, `FallbackEndpoint`). Separating the two is the whole change.

**A label is never derived from a target.** Given an arbitrary path there is no reliable way to tell a static
route segment from a caller identifier — `/api/payparts/v1/operation/info` and
`/api/payparts/v1/operation/12345` have the same shape — so normalizing, pattern-matching, or regex-scrubbing
the target would be a guess, and a wrong guess is the leak. There are exactly two possibilities: an explicit
static label, or `[redacted]`.

### No-label overloads fail closed

Every `BaseService` overload that takes no separate label logs the constant `[redacted]` instead of the
target it was given. This protects the services in this assembly **and** any externally derived service: a
third party that passes a dynamic request target to a no-label helper cannot thereby publish it.

Failing closed is a logging change only. The signature, verb, request target, escaping, body, content type,
retry behaviour, cancellation behaviour, error mapping and disposal of every legacy overload are unchanged.

### An explicit label must be a static route template

A label passed by SDK code is always a `const` or a string literal. It is never built with interpolation,
concatenation, `Uri.EscapeDataString`, `RequestTargetEncoding.EscapePathSegment`, or any value read from a
request DTO. Where a route carries a caller identifier, the label spells the parameter name instead of the
value — `/api/subscriptions/v1/subscriptions/{subscription_id}`.

## What is never a log field

None of the following is logged by any SDK service, on any code path:

- **Credentials** — the configured login and password, the derived `Basic` value, `X-CUSTOMER-AUTH`,
  `X-ON-BEHALF-OF`, or any other request header.
- **Request bodies** — no helper logs the JSON it serialized, and no operation logs a value it is about to
  put in a body.
- **Response bodies** — success payloads are deserialized and returned, never logged.
- **Provider error text and raw payloads** — the parsed provider `message` and
  `RozetkaPayApiError.RawBody` are kept on the thrown exception and are not log fields.
- **Exception objects and messages** — no logging call receives an `Exception` or renders its message.
- **The decline redirect `Location`** — returned to the caller, never logged.
- **Caller identifiers** — no external, customer, card, plan, subscription, operation, payment, project,
  instruction or merchant identifier, in either its raw or its percent-encoded spelling.

The SDK also opens **no logging scope**. A scope is written by a sink exactly like a state value, so "no
scopes" is part of the contract rather than an incidental detail.

## BaseService overload matrix

`[redacted]` marks a fail-closed no-label overload; *explicit label* marks the overload that takes one.

| Helper | Log label | Note |
|---|---|---|
| `GetAsync(endpoint, ct)` | `[redacted]` | Delegates to the explicit overload. |
| `GetAsync(endpoint, endpointForLogging, ct)` | explicit label | Unchanged by EXP-359. |
| `GetAsyncWithFallback(endpoint, fallbackEndpoint, ct)` | `[redacted]` / `[redacted]` | Delegates. |
| `GetAsyncWithFallback(endpoint, endpointForLogging, fallbackEndpoint, fallbackEndpointForLogging, ct)` | explicit primary / fallback labels | Added by EXP-359. |
| `PostAsync(endpoint, request, ct)` | `[redacted]` | Delegates. |
| `PostAsync(endpoint, endpointForLogging, request, ct)` | explicit label | Added by EXP-359. |
| `PostAsyncWithFallback(endpoint, fallbackEndpoint, request, ct)` | `[redacted]` / `[redacted]` | Delegates. |
| `PostAsyncWithFallback(endpoint, endpointForLogging, fallbackEndpoint, fallbackEndpointForLogging, request, ct)` | explicit primary / fallback labels | Added by EXP-359. |
| `PostAsyncWithNoContent(endpoint, request, ct)` | `[redacted]` | Delegates. |
| `PostAsyncWithNoContent(endpoint, endpointForLogging, request, ct)` | explicit label | Added by EXP-359. |
| `PostAsyncWithNoContentWithFallback(endpoint, fallbackEndpoint, request, ct)` | `[redacted]` / `[redacted]` | Delegates. |
| `PostAsyncWithNoContentWithFallback(endpoint, endpointForLogging, fallbackEndpoint, fallbackEndpointForLogging, request, ct)` | explicit primary / fallback labels | Added by EXP-359. |
| `PatchAsync(endpoint, request, ct)` | `[redacted]` | Delegates. |
| `PatchAsync(endpoint, endpointForLogging, request, ct)` | explicit label | Unchanged by EXP-359. |
| `PostWithoutBodyAsync(endpoint, endpointForLogging, ct)` | explicit label | No no-label form exists. |
| `DeleteAsync(endpoint, ct)` | `[redacted]` | Delegates. |
| `DeleteAsync(endpoint, endpointForLogging, ct)` | explicit label | Unchanged by EXP-359. |
| `DeleteAsync(endpoint, endpointForLogging, request, ct)` | explicit label | No no-label form exists. |

Every no-label overload is a one-line forward to its label-aware counterpart, so the two share a single
implementation of the transport, the retry loop, the cancellation guard and the disposal.

The new fallback argument order deliberately pairs each real target with its own label — primary target,
primary label, fallback target, fallback label — so a label cannot drift onto the wrong request.

### Fallback logging

A fallback wrapper writes one Information entry between the primary `404` and the fallback request:

```text
Primary endpoint <primary label> returned 404. Falling back to <fallback label>.
```

Both values are labels. Neither real target appears, and the entry is written **after** the cancellation
guard, so a caller who cancelled during the primary request gets neither the fallback request nor this line.

## Service callsite matrix

All 13 service implementation files. Every row passes an explicit static label; no internal callsite reaches
a no-label overload, so `[redacted]` never appears in the log output of an SDK operation.

`{param}` in a label is a literal brace-wrapped parameter name written into the template, not a value.

### PaymentService

| Method | Real target | Log label |
|---|---|---|
| `CreateAsync` | `/api/payments/v1/new` | same |
| `CreateRecurrentAsync` | `/api/payments/v1/recurrent` | same |
| `ConfirmAsync` | `/api/payments/v1/confirm` | same |
| `CancelAsync` | `/api/payments/v1/cancel` | same |
| `RefundAsync` | `/api/payments/v1/refund` | same |
| `RetryRefundAsync` | `/api/payments/v1/refund/retry` | same |
| `CancelRefundAsync` | `/api/payments/v1/refund/cancel` | same |
| `CardLookupAsync` | `/api/payments/v1/lookup` | same |
| `ResendCallbackAsync` | `/api/payments/v1/callback/resend` | same |
| `ConfirmP2PAsync` | `/api/payments/v1/p2p/confirm` | same |
| `GetInfoAsync` | `/api/payments/v1/info?external_id=<escaped externalId>` | `/api/payments/v1/info` |
| `GetListAsync` | `/api/payments/v1/list?<escaped filters and pagination>` | `/api/payments/v1/list` |
| `GetReceiptAsync` | `/api/payments/v1/receipt?external_id=<escaped externalId>` | `/api/payments/v1/receipt` |

`ConfirmP2PAsync` also had its own `Confirming P2P payment {ExternalId} with amount {Amount}` statement,
which logged an identifier and a request-body value. It is **removed**. No substitute message was added: the
transport helper already writes the safe route label, and the two values are still sent in the body
unchanged.

### BatchPaymentService

| Method | Real target | Log label |
|---|---|---|
| `CreateBatchPaymentAsync` | `/api/payments/batch/v1/new` | same |
| `ConfirmBatchPaymentAsync` | `/api/payments/batch/v1/confirm` | same |
| `CancelBatchPaymentAsync` | `/api/payments/batch/v1/cancel` | same |

### PayPartsService

| Method | Real target | Log label |
|---|---|---|
| `CreateOrderAsync` | `/api/payparts/v1/order/create` → `/api/payparts/v1/new` | same / same |
| `ConfirmOrderAsync` | `/api/payparts/v1/order/confirm` → `/api/payments/v1/payparts/confirm` | same / same |
| `CancelOrderAsync` | `/api/payparts/v1/order/cancel` → `/api/payments/v1/payparts/cancel` | same / same |
| `RefundOrderAsync` | `/api/payparts/v1/refund` → `/api/payments/v1/payparts/refund` | same / same |
| `RetryRefundAsync` | `/api/payparts/v1/refund/retry` | same |
| `CancelRefundAsync` | `/api/payparts/v1/refund/cancel` | same |
| `GetOperationInfoAsync(operationId)` | `/api/payparts/v1/operation/<escaped operationId>` | `/api/payparts/v1/operation/{operation_id}` |
| `GetOperationInfoAsync(externalId, operationId)` | `/api/payparts/v1/info/operation?external_id=<escaped>&operation_id=<escaped>` → `/api/payparts/v1/operation/<escaped operationId>` | `/api/payparts/v1/info/operation` / `/api/payparts/v1/operation/{operation_id}` |
| `GetInfoAsync` | `/api/payparts/v1/info?external_id=<escaped externalId>` | `/api/payparts/v1/info` |
| `GetOperationsAsync` | `/api/payparts/v1/operations?<escaped filters and pagination>` | `/api/payparts/v1/operations` |
| `GetBanksAsync` | `/api/payparts/v1/banks/info` → `/api/payparts/v1/banks` | same / same |
| `ResendCallbackAsync` | `/api/payparts/v1/callback/resend` | same |

`→` marks the `404` fallback. Both `GetBanksAsync` targets are static, so both are their own labels and that
fallback entry stays fully informative.

### PayoutService

| Method | Real target | Log label |
|---|---|---|
| `CreateAsync` | `/api/payouts/v1/new` | same |
| `RequestPayoutAsync` | `/api/payouts/v1/request-payout` | same |
| `GetInfoAsync` | `/api/payouts/v1/info?external_id=<escaped externalId>` | `/api/payouts/v1/info` |
| `GetListAsync` | `/api/payouts/v1/list?<escaped filters and pagination>` | `/api/payouts/v1/list` |
| `GetBalanceAsync` | `/api/payouts/v1/balance` | same |
| `GetAccountBalanceAsync` | `/api/payouts/v1/account-balance?merchant_entity_id=<escaped>` | `/api/payouts/v1/account-balance` |
| `ResendCallbackAsync` | `/api/payouts/v1/resend-callback` | same |
| `CancelCashPayoutAsync` | `/api/payouts/v1/cancel-payout` | same |

### CustomerService

| Method | Real target | Log label |
|---|---|---|
| `GetCustomerWalletAsync` | `/api/customers/v1/wallet?external_id=<escaped customerId>` → `/api/customers/v1/<escaped customerId>/wallet` | `/api/customers/v1/wallet` / `/api/customers/v1/{customer_id}/wallet` |
| `AddCardToWalletAsync` | `/api/customers/v1/wallet?external_id=<escaped customerId>` → `/api/customers/v1/<escaped customerId>/cards` | `/api/customers/v1/wallet` / `/api/customers/v1/{customer_id}/cards` |
| `DeleteCustomerPaymentAsync(request)` | `/api/customers/v1/wallet` | same |
| `DeleteCustomerPaymentAsync(externalId, request)` | `/api/customers/v1/wallet?external_id=<escaped externalId>` | `/api/customers/v1/wallet` |
| `DeletePaymentFromWalletAsync` *(obsolete)* | `/api/customers/v1/<escaped customerId>/cards/<escaped cardId>` | `/api/customers/v1/{customer_id}/cards/{card_id}` |
| `GetWalletItemAsync` | `/api/customers/v1/wallet/find?external_id=<escaped>&option_id=<escaped>` → `/api/customers/v1/<escaped customerId>/cards/<escaped cardId>` | `/api/customers/v1/wallet/find` / `/api/customers/v1/{customer_id}/cards/{card_id}` |
| `GetCardConfirmationStatusAsync` | `/api/customers/v1/wallet/confirmation/status?external_id=<escaped>&option_id=<escaped>` → `/api/customers/v1/<escaped customerId>/cards/<escaped cardId>/confirmation` | `/api/customers/v1/wallet/confirmation/status` / `/api/customers/v1/{customer_id}/cards/{card_id}/confirmation` |
| `SetDefaultCardAsync` | `/api/customers/v1/wallet/settings/set?external_id=<escaped customerId>` → `/api/customers/v1/<escaped customerId>/cards/default` | `/api/customers/v1/wallet/settings/set` / `/api/customers/v1/{customer_id}/cards/default` |
| `GetCustomerCardsAsync` | `/api/customers/v1/<escaped customerId>/cards` | `/api/customers/v1/{customer_id}/cards` |

The two official body-DELETE overloads already used `/api/customers/v1/wallet` as their label and are
unchanged.

### SubscriptionService

| Method | Real target | Log label |
|---|---|---|
| `GetPlansAsync` | `/api/subscriptions/v1/plans` | same |
| `CreatePlanAsync` | `/api/subscriptions/v1/plans` | same |
| `GetPlanAsync`, `UpdatePlanAsync`, `DeactivatePlanAsync` | `/api/subscriptions/v1/plans/<escaped planId>` | `/api/subscriptions/v1/plans/{plan_id}` |
| `CreateAsync` | `/api/subscriptions/v1/subscriptions` | same |
| `GiftAsync` | `/api/subscriptions/v1/subscriptions/gift` | same |
| `GetSubscriptionsAsync()` | `/api/subscriptions/v1/subscriptions` | same |
| `GetSubscriptionsAsync(externalId)` | `/api/subscriptions/v1/subscriptions?external_id=<escaped>` | `/api/subscriptions/v1/subscriptions` |
| `GetCustomerSubscriptionsAsync` *(obsolete)* | `/api/subscriptions/v1/subscriptions/customer/<escaped customerId>` | `/api/subscriptions/v1/subscriptions/customer/{customer_id}` |
| `GetAsync`, `UpdateAsync`, `DeactivateAsync` | `/api/subscriptions/v1/subscriptions/<escaped subscriptionId>` | `/api/subscriptions/v1/subscriptions/{subscription_id}` |
| `GetPaymentsAsync` | `/api/subscriptions/v1/subscriptions/<escaped subscriptionId>/payments` | `/api/subscriptions/v1/subscriptions/{subscription_id}/payments` |
| `CancelCustomerSubscriptionAsync` (both overloads) | `/api/subscriptions/v1/subscriptions/<escaped subscriptionId>/cancel[?<escaped options>]` | `/api/subscriptions/v1/subscriptions/{subscription_id}/cancel` |
| `CancelAsync` *(obsolete)* | `/api/subscriptions/v1/subscriptions/<escaped subscriptionId>/cancel` | `/api/subscriptions/v1/subscriptions/{subscription_id}/cancel` |
| `UpdatePaymentMethodAsync` | `/api/subscriptions/v1/subscriptions/<escaped subscriptionId>/payment-method` | `/api/subscriptions/v1/subscriptions/{subscription_id}/payment-method` |

The official query, cancel and payment-method labels predate EXP-359 and are unchanged.

### ReportService

| Method | Real target | Log label |
|---|---|---|
| `GetPaymentsReportAsync` | `/api/reports/v1/payments` | same |
| `GetTransactionsReportAsync` | `/api/reports/v1/transactions` | same |

### AlternativePaymentService

| Method | Real target | Log label |
|---|---|---|
| `CreateAsync`, `CreateOperationAsync` | `/api/alternative-payments/v1/create` → `/api/alternative-payments/v1/new` | same / same |
| `RefundAsync` | `/api/alternative-payments/v1/refund` | same |
| `ResendCallbackAsync` | `/api/alternative-payments/v1/callback/resend` | same |
| `GetAvailableMethodsAsync` | `/api/alternative-payments/v1/methods` | same |
| `GetOperationInfoAsync(externalId)` | `/api/alternative-payments/v1/operation/<escaped externalId>` | `/api/alternative-payments/v1/operation/{external_id}` |
| `GetOperationInfoAsync(externalId, operationId)` | `/api/alternative-payments/v1/info/operation?external_id=<escaped>&operation_id=<escaped>` → `/api/alternative-payments/v1/operation/<escaped externalId>` | `/api/alternative-payments/v1/info/operation` / `/api/alternative-payments/v1/operation/{external_id}` |
| `GetOperationsAsync` | `/api/alternative-payments/v1/operations?<escaped filters and pagination>` | `/api/alternative-payments/v1/operations` |
| `GetInfoAsync` | `/api/alternative-payments/v1/info?external_id=<escaped externalId>` | `/api/alternative-payments/v1/info` |
| `GetStatusAsync` | `/api/alternative-payments/v1/<escaped paymentId>/status` | `/api/alternative-payments/v1/{payment_id}/status` |

Note the asymmetry, which the label reflects exactly: this operation's fallback is addressed by the
**external** ID, while the PayParts equivalent falls back on the **operation** ID.

### MerchantService

| Method | Real target | Log label |
|---|---|---|
| `GetInfoAsync` | `/api/merchants/v1/me` | same |
| `GetSettingsAsync` | `/api/merchant/v1/settings` | same |
| `UpdateSettingsAsync` | `/api/merchant/v1/settings` | same |
| `GetCommissionRatesAsync` | `/api/merchant/v1/commission-rates` | same |

### FinMonService

| Method | Real target | Log label |
|---|---|---|
| `GetRulesAsync` | `/api/finmon/v1/p2p-payment/pre-limits?recipient_ipn=<escaped recipientIpn>` | `/api/finmon/v1/p2p-payment/pre-limits` |

The recipient IPN is an `int` and therefore cannot carry reserved characters, but it is still a caller value
in the query and is still not logged.

### InStorePaymentService

Already safe before EXP-359; the three static POSTs moved to the explicit `PostAsync` overload so that the
fail-closed change could not silently downgrade their labels.

| Method | Real target | Log label |
|---|---|---|
| `CreateAsync` | `/api/in-store-payments/v1/create` | same |
| `ConfirmAsync` | `/api/in-store-payments/v1/confirm` | same |
| `RefundAsync` | `/api/in-store-payments/v1/refund` | same |
| `GetInfoAsync` | `/api/in-store-payments/v1/info?external_id=<escaped externalId>` | `/api/in-store-payments/v1/info` |

Confirm and refund carry cardholder data (`CardNumber`, `EncryptedTrack2`). No body is logged.

### PartnerService

Already safe before EXP-359 and unchanged: every overload passed the static route as its label.

| Method | Real target | Log label |
|---|---|---|
| `GetFeeDetailsAsync` (both overloads) | `/api/partners/v1/fee-details[?merchant_project_id=<escaped>]` | `/api/partners/v1/fee-details` |
| `GetMerchantStatusAsync` (both overloads) | `/api/partners/v1/merchant-status[?<escaped options>]` | `/api/partners/v1/merchant-status` |
| `GetTransactionDetailsAsync` (both overloads) | `/api/partners/v1/transaction-details?<escaped options>` | `/api/partners/v1/transaction-details` |

### PaymentInstructionService

| Method | Real target | Log label |
|---|---|---|
| `CreateAsync` | `/api/payment-instructions/v1/new` | same |
| `DeclineAsync` | `<base>/api/payment-instructions/v1/decline?project_id=<escaped>&payment_instruction_id=<escaped>` | `/api/payment-instructions/v1/decline` |

`CreateAsync` moved to the explicit `PostAsync` overload. `DeclineAsync` keeps its own transport — it needs
an unauthenticated, non-redirecting client — and already logged only the static `DeclineEndpoint`. It logs
neither identifier, neither the returned `Location`, nor any header.

## Surfaces that stay separate

These were already protected and are **not** changed by EXP-359.

### The shared retry warning

One `Warning` per retry, identical for every operation:

```text
Retry {RetryNumber} of {MaxRetryAttempts} scheduled after {FailureKind}, HTTP status {StatusCode}, in {DelayMilliseconds}ms
```

`FailureKind` is an exception **type name**. The exception object is not passed to the logger and its message
is not rendered. The entry carries no request target — not even a label — no provider text, no response
body, and no credential.

### The API error log

One `Error` per non-success response:

```text
RozetkaPay API error. StatusCode: {StatusCode}. ApiCode: {ApiCode}. RequestId: {RequestId}
```

These three fields are deliberately **retained**: they are what support correspondence needs and none of them
is caller content. The parsed provider `message` and `RozetkaPayApiError.RawBody` are not logged, and the
caller still gets both through the thrown exception.

### The built-in factory logging

`AddRozetkaPay` calls `RemoveAllLoggers()` on both named clients (`RozetkaPay` and
`RozetkaPay.PaymentInstructions.Decline`), so nothing is emitted under
`System.Net.Http.HttpClient.RozetkaPay.*`. That logging writes the request URI and does **not** redact path
segments, and its header redaction applies to the rendered message only — the structured state carries real
header values. It is not configurable, so it is removed rather than tuned. This predates EXP-359 and is
unchanged by it.

Adding your own request telemetry is unaffected; log a target you have redacted yourself. See the Logging
section of the package README.

## The wire is unchanged

EXP-359 changed log labels and removed one log statement. It changed nothing a server or a caller can
observe: HTTP verb, request target, query parameter order and escaping, request body, content type, response
deserialization, retry policy and `Retry-After` handling, cancellation semantics including the pre-dispatch
guard and the fallback catch boundary, the exception hierarchy and error mapping including
`RozetkaPayApiError.RawBody`, request/response/content disposal, the `DeclineAsync` redirect semantics,
package dependencies and target frameworks.

## How this is enforced

`tests/SYT.RozetkaPay.Tests/LegacyLoggingRedactionTests.cs`, with
`tests/SYT.RozetkaPay.Tests/TestInfrastructure/LoggingRedactionTestInfrastructure.cs`, runs on `net9.0` and
`net10.0` and asserts:

- the no-label matrix — GET, POST, POST accepting `204`, PATCH, DELETE, and the three fallback wrappers —
  logs `[redacted]` and sends the exact target it was given;
- the explicit-label matrix — all of the above plus the bodiless POST and the JSON-body DELETE — logs the
  label and only the label;
- each dynamic service callsite sends the expected raw-to-encoded value at the correct insertion point while
  logging its static template, and never `[redacted]`;
- hostile caller markers, in both raw and percent-encoded spelling, appear in no category, rendered message,
  structured value or scope;
- credentials, request bodies, success response bodies, provider error text, raw bodies and the decline
  `Location` are absent, while the exception keeps its mapped type and `RawBody`. The credential case first
  proves the credential was there to leak: the handler-observed request carries the `Basic` scheme, a
  parameter that decodes to exactly the configured `login:password`, and `X-CUSTOMER-AUTH` /
  `X-ON-BEHALF-OF` with exactly the configured values — otherwise a regression that stopped sending
  authentication would satisfy every absence assertion;
- the retry warning still carries no target, provider text or exception message;
- no entry has any scope.

Leak markers are synthetic, unique, and obviously not credentials. Every target is intercepted and the
configured host is in the reserved `.invalid` TLD, so the suite performs no DNS resolution, opens no socket,
and uses no credential.
