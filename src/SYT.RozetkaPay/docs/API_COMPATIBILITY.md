# API Compatibility Matrix

## Scope

- SDK: `SYT.RozetkaPay` (`0.1.0-alpha.1`)
- API path family: `v1` (`/api/*/v1/*`)
- OpenAPI schema version: `3.0.3`

## Source of Truth

- Official public docs: `https://cdn.rozetkapay.com/public-docs/index.html`
- Official OpenAPI document: `https://docs.rozetkapay.com/openapi.json`
- Local snapshot used by this repository: `docs/openapi.json`

## Path Coverage and Operation Parity Are Different Metrics

An SDK method can call the right **path** and still call the wrong **operation** — a different HTTP
verb, a body the operation does not declare, or a response shape the operation does not return. Path
coverage therefore does not imply operation parity, and the two are reported separately below.

### View 1 — pinned repository snapshot (`docs/openapi.json`)

- SHA-256: `309e61bf2185706c137f2d270d767b31777f7a4d09f2f2e0fb900fe36601cc44`
- Paths: `49`
- Operations: `57`
- Path coverage: `49/49`
- Operation parity: reached for the pinned `57`-operation set as of EXP-355, after the three
  corrections below; proven by `tests/SYT.RozetkaPay.Tests/OperationParityTests.cs`.
- Additional legacy compatibility routes in SDK: `25`

### View 2 — live official document, observed `2026-07-25`

- SHA-256: `98a9cf2a74b7df6edcaa17872d63f6bc9de96d77ca85a8adfb6a91af05c8e67a`
- Paths: `59`
- Operations: `67`
- The live document publishes ten operations and ten paths that the pinned snapshot does not contain.
  Refreshing the snapshot and covering those operations is **EXP-354**, and the final `67/67`
  sandbox/auth/webhook coverage is **EXP-337**. This SDK does **not** claim live `67/67` parity.

## Corrected Operation Mismatches (EXP-355)

Each row is a published operation the SDK previously reached only through a legacy verb, path, body,
or response shape. Both members exist; the legacy one is obsolete and unchanged on the wire.

| operationId | Official verb and path | Canonical SDK method | Legacy SDK method | Fallback policy |
|---|---|---|---|---|
| `deleteCustomerPayment` | `DELETE /api/customers/v1/wallet` (optional `external_id`, JSON body, `DeleteCustomerPaymentResult`) | `ICustomerService.DeleteCustomerPaymentAsync(request, ct)` and `(externalId, request, ct)` | `ICustomerService.DeletePaymentFromWalletAsync(customerId, cardId, ct)` — `DELETE /api/customers/v1/{customerId}/cards/{cardId}`, no body, `DeleteCardFromWalletResponse` | none |
| `getSubscriptions` | `GET /api/subscriptions/v1/subscriptions` (optional `external_id`, root JSON array of `Subscription`) | `ISubscriptionService.GetSubscriptionsAsync(ct)` and `(externalId, ct)` | `ISubscriptionService.GetCustomerSubscriptionsAsync(customerId, ct)` — `GET /api/subscriptions/v1/subscriptions/customer/{customerId}`, `CustomerSubscriptionsResponse` wrapper | none |
| `CancelCustomerSubscription` | `DELETE /api/subscriptions/v1/subscriptions/{subscription_id}/cancel` (optional `external_id`, optional lowercase `refund`, no body, `DefaultResponse`) | `ISubscriptionService.CancelCustomerSubscriptionAsync(subscriptionId, ct)` and `(subscriptionId, options, ct)` | `ISubscriptionService.CancelAsync(subscriptionId, request, ct)` — `POST` to the same path with an `external_id`/`reason`/`immediate` body, returns `Task` | none |

### Why there is no fallback

The canonical and legacy members do not describe the same call:

- the wallet responses are different types (`DeleteCustomerPaymentResult` vs `DeleteCardFromWalletResponse`);
- the list responses are different types (root array vs wrapper object);
- the cancel request semantics differ, and the legacy `reason` and `immediate` fields have no honest
  mapping onto the official `refund` option — guessing `immediate -> refund` would silently change what
  the caller asked for.

No canonical operation therefore ever switches to a legacy route or verb. A canonical `404` makes
**exactly one** HTTP request and throws `RozetkaPayNotFoundException`; `400`, `401`, `403`, `412`, `429`
and `500` keep their existing exception mapping. A fallback would hide an operation-parity error behind
a response that looks successful.

Retrying is a separate concern, and EXP-355 did not change it. The default `RetryPolicy` is disabled, so
by default a canonical call is a single request. When a retry policy is enabled, the SDK may repeat the
**same** canonical request target for the conditions it already supports — transport-level failures
(`HttpRequestException`, `TaskCanceledException`, `SocketException`) and `429`. Such a repeat is always
the same operation against the same target; it is never a different route, verb, or body. A `404` is not
a retriable condition, so it is never repeated and never followed by a legacy attempt.

### Why the old members are only obsolete

Removing or redirecting them would break compiled consumers. Every pre-existing signature keeps its name,
parameters, return type and wire behaviour; each carries an `[Obsolete]` warning naming its replacement.

## Response Shape Compatibility

`SubscriptionList` keeps its public shape — the class, the `Subscriptions` property, its type and its
base type are unchanged — while an internal `JsonConverter<SubscriptionList>` maps the official root
array onto it. Reads accept both the official array and the historical `{ "subscriptions": [...] }`
wrapper; writes always emit the official root array. Null and empty stay distinct: an official `[]`
yields an empty list, a wrapper carrying `"subscriptions": null` yields `null`, and an absent list is
normalized to `[]` when serializing, because the official schema has no spelling for "no array at all".

## Last Verification

- Date: `2026-07-25`
- Result: three operation-level parity defects corrected; snapshot path coverage re-validated and
  reported separately from operation parity.
- Snapshot path count: `49`; snapshot operation count: `57`
- SDK service coverage for snapshot OpenAPI paths: `49/49`

## Known Runtime Inconsistency (Observed in Integrations)

- Some API responses return numeric values as JSON strings (for example, `"100.00"` or `"2"`) instead of JSON numbers (`100.00`, `2`), despite OpenAPI declaring numeric types.
- This behavior was observed in integration testing and reported to RozetkaPay.
- As of `2026-02-28`, this is still reproducible on selected endpoints.
- SDK mitigation:
  - flexible decimal converters for `decimal` / `decimal?`
  - flexible integer converters for `int` / `int?` / `long` / `long?`
  - global `JsonNumberHandling.AllowReadingFromString` enabled in serializer options
  - converters are read-compatible with both formats, so SDK remains stable before and after potential API-side normalization.
