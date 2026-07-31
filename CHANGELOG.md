# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Maintainers move entries out of `Unreleased` into a new versioned section
immediately before tagging a release (see the release process in `README.md`).

## [Unreleased]

## [6.0.0] - 2026-07-31

One type was doing two jobs, and the contract gate had been told to accept it.

**Breaking.** `CreatePaymentRequest.Customer` changes type. Read below before upgrading.

### Changed

- **`CreatePaymentRequest.Customer` is now `CustomerRequestUserDetails`, not `CustomerInfo`.** The document
  keeps two customer schemas and separates them by *direction*: `CustomerRequestUserDetails` (16 fields,
  composed with `allOf` over `BaseRequestUserDetails`) is referenced only by request bodies, and
  `CustomerInfo` (6 flat fields) only by responses. One C# class served both.

  The consequence was quiet: a caller could set `Address` on the customer block of an operation whose schema
  does not declare it, and the field was serialized, sent and discarded. Nothing in the signature said so,
  because the type was the same either way.

  **To upgrade:** change `new CustomerInfo { … }` to `new CustomerRequestUserDetails { … }` wherever you
  build a payment request. Every property keeps its name and type, so nothing else moves. If you were
  reading a customer block off a *response*, keep `CustomerInfo` — that is now the response shape, and it
  gained `BrowserUserAgent`, which the old class did not have at all.

### Removed

- **The duplicate customer class.** `CustomerRequestUserDetails` already existed with the same base and the
  same six own properties; the class named `CustomerInfo` was its twin. This is the third time in this SDK
  that the correct type turned out to be sitting unused beside the wrong one — the earlier three were fixed
  in `3.0.0`. All four shared one cause: the contract test matched C# types to schemas *by name*, so an
  unmatched duplicate could live indefinitely.

### Notes

The gate that made this invisible is gone with it. `SchemaAliases` existed for exactly one entry — this
conflation — and while it lived, a type was compared against the *union* of two schemas: a field declared by
either one passed, even on the operation whose schema declares nothing of the sort. The alias did not
describe the problem, it concealed it. One type is now compared against one schema, exactly.

`JustifiedInheritedExtras` is consequently empty, and `EveryJustification_ShouldStillHold` — added in
`5.0.0` for precisely this reason — is what caught its own entries going stale when the type split.

## [5.0.0] - 2026-07-31

`4.0.0` made the SDK safe to operate. This release makes its public surface mean something.

Every public type is a promise to a caller. Thirty-seven of them were promises nothing kept: no service
referenced them, and no component in the document declared them. Two more types were promising fields the
provider does not accept or send. Both are removals, so both are breaking — but what is removed could not
have worked in the first place.

**Breaking.** Read `Removed` before upgrading.

### Removed

**37 public model types that nothing could reach.** Each was referenced by no service and declared by no
component in `docs/openapi.json` — including `requestBodies` and `responses`, not only `schemas`. Most were
legacy twins of a type the document does declare and the SDK already returns (`SubscriptionResponse` beside
`Subscription`, `RecipientPaymentMethod` beside `RecipientRequestPaymentMethod`), or children of a parent
removed in `4.0.0` (`PaymentResponse` is gone, so its six child types had nothing left to belong to).

Find your name here rather than diffing two packages:

| namespace | removed |
| --- | --- |
| `Models.Batch` | `BatchFee`, `BatchOrderDetails`, `PaymentMethodResponse` |
| `Models.Common` | `Customer` |
| `Models.Customers` | `CustomerWalletResponse`, `WalletCard`, `WalletTransaction` |
| `Models.FinMon` | `FinMonCard`, `FinMonCustomer`, `FinMonRule`, `RuleCondition`, `StatusHistoryItem`, `TriggeredRule` |
| `Models.Merchants` | `CommissionRate`, `MerchantContact` |
| `Models.PayParts` | `CreatePayPartsOrderRequest`, `PayPartsError` |
| `Models.Payments` | `ApplePayToken`, `CallbackInfo`, `CardDetails`, `CardInfo`, `GooglePayToken`, `PaymentCustomer`, `PaymentDetails`, `PaymentError`, `PaymentMethod`, `PaymentMethodInfo`, `RecipientCardNumber`, `RecipientCardToken`, `RecipientPaymentMethod`, `RecipientWallet`, `ThreeDsInfo` |
| `Models.Payouts` | `CurrencyBalance`, `PayoutError` |
| `Models.Subscriptions` | `SubscriptionError`, `SubscriptionPaymentInfo`, `SubscriptionResponse` |

If you referenced one, you were holding a type the SDK never filled and never sent. The replacement is the
document-declared type the service actually uses — the table above pairs the obvious ones, and the service
signature names the rest.

**Two inherited properties per type on two models.** `ResultUserDetails` and
`AlternativePaymentCustomerDetails` no longer derive from `UserInfo`; they declare exactly what their own
schema declares. What disappeared:

| type | properties | why they could not work |
| --- | --- | --- |
| `ResultUserDetails` | `BrowserUserAgent`, `IpAddress` | response-side; the schema declares neither, so both were permanently `null` — indistinguishable from "the provider left it out" |
| `AlternativePaymentCustomerDetails` | `BrowserUserAgent`, `Patronym` | **request**-side; both were serialized, sent, and silently discarded by the provider |

Both schemas are flat in the document — no `allOf` — so the inheritance was the SDK's own invention, not a
composition the contract asked for.

### Added

- **`OrphanModelTypeTests`** — a public model must be reachable: used by the SDK, declared by the document,
  or named in a two-entry allow-list with its reason. Detection reads assembly *metadata*, not source text,
  because text cannot tell `public string? ApplePayToken` (a `string`) from a use of the type
  `ApplePayToken`. That distinction is exactly what hid seven of the thirty-seven.
- **`NoInheritedExtra_ShouldExistBeyondTheJustifiedList`** and **`EveryJustification_ShouldStillHold`** — the
  undeclared-property gate now covers inherited members too. Until now it checked only what a type declared
  itself, which made moving a field onto a base class a way past it.
- **README: "When a payment's state is unknown"** — the failure contract in the place callers read. Chiefly:
  `data_not_found` from `/info` is **not** proof a payment does not exist (verified live: four attempts over
  twelve seconds, all `data_not_found`, while the hosted checkout was open and working), so reading it that
  way and retrying with a fresh `external_id` charges the customer twice.

### Notes

`CustomerInfo` still inherits six properties its own schema does not declare, and that is deliberate: the
same C# type serves both `CustomerInfo` (6 fields) and `CustomerRequestUserDetails` (16), so the fields are
declared — by the second schema. The gate records this as a checkable claim rather than an exemption. Splitting
the type is tracked separately and will be breaking in its own right.

## [4.0.0] - 2026-07-31

`3.0.0` made the SDK agree with the published contract. This release makes it safe to operate.

The remaining defects came from a consumer-perspective audit, and none of them was a contract
divergence — the document says nothing about any of them. They are about what happens when a call
does not complete, when the provider ships a value this version has not seen, and when the natural
thing to write in C# is the wrong thing.

**Breaking.** Read `Changed` before upgrading.

### Added

- **`RozetkaPayTransportException`** — a request that failed before the provider's answer was read,
  carrying `IsTimeout`, `AttemptsDispatched` and `MayHaveReachedProvider`. That last is always `true`,
  deliberately: the SDK cannot prove a request did not arrive, and reporting otherwise would invite a
  caller to skip reconciliation on the strength of a guess.
- **`PaymentWebhook.EventKey`** — a deduplication key. The obvious choice was wrong: `Id` is the
  *payment* identifier, identical across every delivery for that payment, so deduplicating on it drops
  the refund notification for a payment already seen.
- **`RefundPaymentRequest.RefundEntirePayment`** — `[JsonIgnore]`, so the wire is unchanged. It exists
  so that "refund everything" has to be chosen rather than arrived at.

### Changed

- **A timeout is no longer retried, and no longer escapes the hierarchy.** It used to surface as a
  bare `TaskCanceledException`, so a caller writing the documented `catch (RozetkaPayException)` did
  not catch the one failure that leaves money in an unknown state. `RetryPolicy.Standard` then
  repeated it, turning one ambiguous payment creation into **four real POSTs**. A caller's own
  cancellation still arrives as their own `OperationCanceledException` carrying their own token.
- **An unknown enum token costs one field, not the response.** `ResponseCode` carries 184 values and
  the provider adds more; one unrecognised token used to make an entire reply unreadable — the payment
  had succeeded, the money had moved, and reading it back threw permanently. Nullable enums now yield
  `null`. Non-nullable ones stay strict: those are discriminators the SDK itself sets, where an
  unknown value means a bug rather than a provider release.
- **`CreatePaymentRequest.Mode` is `PaymentMode?`.** It was a non-nullable enum whose zero value is
  `Direct` — raw card acceptance, the PCI-scope flow — so a caller who forgot the field was requesting
  it silently, and `[Required]` on a value type cannot fail.
- **`Amount` is range-checked**; `[Required]` on a `decimal` was decorative and `0` or `-5` reached the
  gateway.
- **A null refund amount is a validation failure** unless the full refund is declared. `decimal?`
  arithmetic propagates null, so `(paid - refunded) / 2` yielded `null` and refunded everything — and
  before `3.0.0` those figures really were always `null`.
- **The provider's own message reaches the caller on every status.** It was parsed and discarded for
  401/403/404/500, so a log line read `"Resource not found"` while the text naming the actual problem
  sat only inside `RawBody`, which this SDK's documentation tells callers to scrub. `cf-ray` joins the
  request-id chain, because production sends neither header the SDK looked for.
- **An empty body on a `200` is an error**, not an object of defaults. Only `204` means no content.
  After `3.0.0` an all-null `PaymentOperationResult` is exactly what a successful hosted creation also
  looks like.
- **A body the SDK cannot read** fails inside `RozetkaPayException` with the raw body attached, rather
  than as a bare `JsonException` carrying no evidence.
- **The unauthenticated decline client is created on first use.** `RozetkaPayClient` is registered
  Scoped, so an eager one allocated a raw `HttpClient` — a fresh connection pool — per DI scope, that
  is per inbound request, and disposed it with sockets in `TIME_WAIT`.
- **Dates parse invariantly.** This exposed a real defect rather than being the tidy-up it looked
  like: `dd.MM.yyyy` was handled by the culture-sensitive branch only on a day-first machine, and the
  fallback used `AssumeUniversal` without `AdjustToUniversal`, so `SpecifyKind(Utc)` relabelled a
  local value instead of converting it. Every such date was wrong by the machine's UTC offset.

### Fixed — found by the pre-release audit

- **Alternative payments carrying products were rejected outright**, and by a fix from `3.0.0`.
  `Product` was taught to write `quantity` and the amounts as JSON strings because that is what
  `createPayment` declares — and `AlternativePaymentProduct` derived from it, while the document
  declares **the same field names with different types** there: `quantity` integer, `price` and the
  amounts numbers. Live: `"quantity":"1"` answers `400 invalid_request_body param: products.quantity`,
  `1` passes. The type is now declared from the document rather than derived; one shared base cannot
  serve two contracts that disagree.
- **Both refund operations** declare `PayPartsProduct` for their product list, not the
  payments-shaped `Product`.
- **Batch `customer.payment_method`** used the legacy flat model, sending `{"cc_token":"tok"}` where
  the provider expects `{"cc_token":{"token":"tok"}}` — every tokenized batch payment was
  off-contract.
- **`EventKey` did not include the operation's state**, so a pending or 3-D Secure delivery and the
  final one for the same operation shared a key. A consumer following the property's own advice would
  have rejected the final delivery and never marked the payment paid.
- **`IFinMonService.GetRulesAsync` took an `int`.** A Ukrainian RNOKPP encodes days since 31.12.1899
  in its first five digits, so for anyone born after October 1958 the ten-digit value exceeds
  `int.MaxValue` — the operation was unusable for most living recipients. It is a `long`.
- **Gift subscription** now uses the customer type the document requires, the only one able to carry
  the required `payment_method`.

### Removed

- **47 public types** nothing references and no component declares. `PaymentWebhook`,
  `ErrorResponse` and `TooManyRequestsResponse` stay: consumers deserialize into them, so the SDK not
  referencing them is the point.

### Migration

- Catch `RozetkaPayTransportException` where you previously caught `TaskCanceledException`, and read
  `AttemptsDispatched` before deciding whether to reconcile or retry.
- Set `Mode` explicitly; omitting it is now a validation error rather than a request for `direct`.
- A refund with no amount needs `RefundEntirePayment = true`.
- Deduplicate webhooks on `EventKey`, inside the same transaction as the state change it guards.

### Verification

- **1548 tests** pass on `net10.0`, 0 failed.
- Contract divergence baseline empty in all three groups.
- Integration pass against the live gateway through the public API: **0 SDK faults**.
- An independent pre-release audit ran the suite, resolved the live document, and attacked the new
  failure semantics with 27 adversarial probes. It returned **DO NOT SHIP** with six findings, all
  verified and fixed above; it also confirmed clean: every dispatched route declared, the refund
  guard unbypassable, cancellation identity preserved, the webhook signature matching an independent
  implementation of the published algorithm, and every enum member token-for-token.


## [3.0.0] - 2026-07-30

`2.0.0` shipped with 255 contract divergences that 1607 green tests could not see. This release
removes all of them, and closes the structural gap that let them hide.

**Why the tests missed it.** Every contract check matched an SDK type to a schema **of the same
name**, and silently skipped any type whose name had no counterpart. Whole model families written
against superseded documentation were therefore never compared to anything. Two further blind spots
compounded it: the operation manifest proved every *declared* operation was reachable but never the
converse, and the route check reflected over string constants, skipping any target built by
interpolation.

Two independent audits — one reading the document, one using the SDK as a consumer would — reached
the same conclusion from opposite directions.

**This is a breaking release, and larger than 2.0.0.** Read `Removed` and `Changed` before upgrading.

### Added

- **`OperationTypeMappingTests`** — every service method maps to a declared operation; every return
  type can receive what its response declares; every request type sends **exactly** what its body
  declares, both directions. The request rule is exact because both failure modes are silent: an
  undeclared field is discarded by the provider, and a declared one the model cannot express is a
  request the caller cannot make.
- **`DispatchedRouteTests`** — builds every service over a recording handler and compares the route
  that **actually left the process** against the document. This is what found two undeclared routes
  the static checks could not see, and corrected one the source reading got wrong in the opposite
  direction. It carries a coverage floor, because a gate that silently observes nothing passes
  forever — the first run reported zero dispatches and would otherwise have looked green.
- **`RequestJsonTypeTests`** — the sending direction is held to the declared JSON type.
- **`KnownContractDivergences.txt`** — the divergence baseline, exact in both directions: a new
  divergence fails because it is absent, and a fixed one fails because its line must be deleted in
  the commit that fixes it. It went 7 / 45 / 203 → **0 / 0 / 0**.
- **`CustomerWallet`** — the one model that had to be written rather than switched to.

### Removed

- **Seven operations dispatching routes the document does not declare** (EXP-430). Five duplicated
  operations that already worked, the declared equivalent sitting on a neighbouring overload; two
  were already `[Obsolete]`. All answered `404`.

  ```
  DELETE /api/customers/v1/{}/cards/{}          GET /api/customers/v1/{}/cards
  GET    /api/payparts/v1/operation/{}          GET /api/alternative-payments/v1/operation/{}
  GET    /api/alternative-payments/v1/{}/status GET /api/subscriptions/v1/subscriptions/customer/{}
  POST   /api/subscriptions/v1/subscriptions/{}/cancel
  ```

- **`CardLookupRequest` / `CardLookupResponse` / `AddCardToWalletRequest` /
  `AddCardToWalletResponse`** — none modelled anything the document declares.
- **`reason` and `external_refund_id` from all three refund bodies**, and `callback_url` from the
  PayParts resend. The gateway discards undeclared fields, so a caller who recorded a refund reason
  believed it was stored. It never was, and nothing said so.
- **`Product.sku`**, declared by nothing.

### Changed

- **Requests write the JSON type the document declares** (EXP-429). `Product.quantity`,
  `net_amount` and `vat_amount` are declared as strings; the SDK sent numbers, so **any payment
  carrying a product list was rejected**: `400 invalid_request_body param: products.quantity`, while
  the identical body with `"quantity":"2"` answers `200`. The properties stay `decimal?` and `int?`
  and a converter handles the wire — retyping them to `string` would hand every caller the
  formatting problem, culture bug included.
- **Twenty-five operations return the type the document declares.** `is_success` was unreachable on
  the four money operations — a caller could not answer *did this payment succeed?* — and
  `CheckoutUrl` was permanently `null` because the URL arrives under `action.value`. In all but one
  case the correct model **already existed in the SDK, unused**.
- **`AddCardToWalletAsync` takes a card token**, as the document declares. It previously invited a
  raw PAN and CVV at an endpoint that does not accept them.
- **`CardLookupAsync`** takes the checkout-shaped `CreateLookupRequest` and returns
  `PaymentOperationResult`.
- **The three refund bodies** gained the declared `currency`, `payload`, `products` and
  `callback_url`; `PayPartsResendCallbackRequest` gained `operation`, which decides *which* callback
  is resent.
- **`DeactivatePlanAsync` and `DeactivateAsync`** return `DefaultResponse` instead of discarding the
  provider's reply.

### Fixed

- The post-publish release check compared raw archive bytes against a package nuget.org
  repository-signs on ingestion (EXP-427). It was not merely wrong but **racy** — `1.0.0` passed only
  because it downloaded before signing propagated. It now compares payload entries.

### Migration

- Calls to the seven removed operations have no replacement; the routes do not exist. Use the
  declared equivalent named in `Removed`.
- Money operations return `PaymentOperationResult` / `PaymentStatusResult`. Read `IsSuccess`; the
  hosted URL is `Action.Value`, not `CheckoutUrl`.
- `AddCardToWalletAsync` takes `AddCustomerPaymentRequest` with a `cc_token`.
- `CardLookupAsync` takes `CreateLookupRequest` with `Mode` required.
- Subscription and plan reads return `Plan`, `Subscription`, `CreateSubscriptionResponse`,
  `List<SubscriptionPayment>`.
- `reason` and `external_refund_id` on refunds are gone; they were never sent anywhere the provider
  kept them.

### Verification

- **1526 tests** pass on `net10.0`, 0 failed.
- Divergence baseline empty in all three groups.
- Integration pass against the live gateway through the SDK's public API: **0 SDK faults**. Hosted
  payment with products, card lookup, PayParts banks, subscription plans, customer wallet and
  merchant status all succeed; three calls are refused by the gateway for account reasons
  (`data_not_found` on an unpaid checkout, `internal_error` on a nonexistent payout) and none is an
  SDK defect.


## [2.0.0] - 2026-07-30

A contract-correctness release. `1.0.0` was audited against RozetkaPay's live OpenAPI document and
against the gateway itself, and the audit found that the SDK's own contract tests compared it to a
**committed snapshot** and never to the published document. So 1480 green tests could not see any of
the drift that shipped.

Every defect below was already in `1.0.0`. Nothing here is a regression; it is the difference between
a suite that agrees with the implementation and one that agrees with the provider.

**This is a breaking release.** The removals are the point of it: a public method whose route returns
`404`, or a property the provider silently discards, is worse than a missing one, because the caller
learns at runtime, in production, against money. Read `Removed` and `Changed` before upgrading.

### Removed

- **Ten public operations whose routes the document does not declare (EXP-403).** They were held back
  a release and listed by name while it was still possible they were earlier spellings of operations
  now published elsewhere. They were then called against the live gateway with credentials that
  answer `200` on a declared control route: all ten answered `404`, and the POST-only ones answered
  `404` rather than `405`, which distinguishes an absent path from a wrong verb. Nothing replaces
  them, because nothing is there.

  | Removed member | Route |
  | --- | --- |
  | `IAlternativePaymentService.GetAvailableMethodsAsync` | `/api/alternative-payments/v1/methods` |
  | `IAlternativePaymentService.GetOperationsAsync` | `/api/alternative-payments/v1/operations` |
  | `IMerchantService.GetSettingsAsync` | `/api/merchant/v1/settings` |
  | `IMerchantService.UpdateSettingsAsync` | `/api/merchant/v1/settings` |
  | `IMerchantService.GetCommissionRatesAsync` | `/api/merchant/v1/commission-rates` |
  | `IPayPartsService.GetOperationsAsync` | `/api/payparts/v1/operations` |
  | `IPaymentService.ConfirmP2PAsync` | `/api/payments/v1/p2p/confirm` |
  | `IPaymentService.GetListAsync` | `/api/payments/v1/list` |
  | `IPayoutService.CreateAsync` | `/api/payouts/v1/new` |
  | `IPayoutService.GetListAsync` | `/api/payouts/v1/list` |
  | `IPayoutService.GetBalanceAsync` | `/api/payouts/v1/balance` |

  Fourteen request and response types existed only to feed them and went too. None is declared as a
  schema, so nothing that models a published contract was lost. `OffSpecRouteTests` now asserts an
  empty set, making it the standing proof that the SDK invents no route — verified by adding a probe
  constant, which fails the gate with the probe path named.

- **Twenty-six model properties no schema declares (EXP-422).** On a request body an undeclared
  property is a trap: the caller sets it, the SDK serializes it, the provider drops a field it does
  not declare, and the intent vanishes with no error anywhere. Every one sat beside the declared
  field it shadowed, under a name that read like the simpler way to do the same thing —
  `apple_pay_token` next to `apple_pay`, `card_number` next to `cc_number`, `card` next to
  `cc_token`, `category` next to `category_name`. The rest were fields of a neighbouring schema
  (`amount` and `currency` on `RecipientRequestUserDetails`, `patronym` on `PayPartsCustomer`),
  credentials the schema does not accept (`cvv` on `RecipientCCTokenRequestPaymentMethod`,
  `exp_month` and `exp_year` on `RecipientCCNumberRequestPaymentMethod`), or plain inventions
  (`user_info` on the customer object, `callback_url` on `ResendCallbackRequest`).

- **`Product.sku` and `Product.price` (EXP-422).** `sku` is declared by nothing.
  `AlternativePaymentProduct`, `PayPartsProduct`, `Plan` and `Subscription` each declare `price` and
  the base `Product` does not, so `price` moved to `AlternativePaymentProduct` where it belongs;
  carrying it on the base sent the field on every product shape the document says has no such
  property.

- **The `net9.0` target framework (EXP-425).** `net9.0` is STS and leaves support on `2026-11-10`.
  Removing a target framework later would be a breaking change; removing it now, before the package
  has consumers, is not. Nothing in the SDK used TFM-conditional compilation, so the second target
  was pure build and test duplication. The package now ships a single `lib/net10.0`.

- **Six fallback routes and the hidden retry that reached them (EXP-385).** A `404` silently
  triggered a second request to a different path. That turned one operation into two possible ones
  with no way for the caller to know which had run.

### Changed

- **`CustomerRequestPaymentMethod.Type` is `PaymentMethodType?` rather than `string?` (EXP-421).**
  The document declares it as a closed set of six values and makes it the discriminator: which
  sibling object becomes required depends on what it holds. A probe sent `"cc"` — not one of the six
  — and the request was built, validated, serialized and sent with no complaint. Nullable rather than
  a bare enum, deliberately: a non-nullable enum property defaults to its zero value, so a caller who
  omits the field would silently send `cc_token` instead of failing validation.

- **`PaymentService.BuildP2PRequest` signature (EXP-422).** Now
  `(amount, currency, externalId, customerEmail, recipientCardNumber, description = null)`. It sent
  `type: "card_number"`, which is not among the four values the schema's inline enum permits, so
  every request it built was invalid before it left the process. It also hardcoded
  `customer@example.com` to satisfy the `customer` field that `direct` mode requires — a fabricated
  address attached to a real transfer, with nothing to tell the caller. The expiry parameters are
  gone because `RecipientCCNumberRequestPaymentMethod` declares only `number`.

- **Enums serialize to the tokens the document publishes (EXP-384).** Two enums inherit 184 values
  through `allOf` and were modelled with three each. `JsonStringEnumMemberName` now carries the exact
  wire spelling and integer values are refused, so an unknown token fails loudly instead of
  deserializing to whatever member happens to sit at that ordinal.

- **Retries are gated on idempotency (EXP-385).** A non-idempotent operation is no longer retried.
  The provider's at-most-one-success guarantee is keyed on `external_id`, and retrying a request that
  does not carry one can charge twice.

- **Request bodies rebuilt from the published schemas** — create-payment and its dead twin (EXP-394),
  recurrent payment (EXP-392), card lookup (EXP-391), the three batch bodies (EXP-393), the
  PayParts order (EXP-396), the alternative-payment operation (EXP-395), plans and subscriptions
  (EXP-397), set-default-card, which was missing `option_id` and `type` (EXP-398). Response models
  now receive every field the schemas declare (EXP-401).

- **`Confirm` and `Cancel` carry a partial amount (EXP-386).** Both operations declare `amount` and
  `currency`; the SDK sent neither, so a partial capture or partial cancel was not expressible.

- **`DateTime` values convert to UTC instead of being relabelled (EXP-390).** A local time was
  stamped with `Z` rather than converted, which shifted every timestamp by the offset.

- **Metadata is carried everywhere the document declares it (EXP-399)**, with a limits attribute that
  names an offending key without ever quoting its value.

- **`Microsoft.Extensions.*` tracks the runtime's own release line (EXP-406).** Pinning an older line
  asked consumers to carry a second copy of assemblies their shared framework already provides.

- **Validation runs before the transport is touched (EXP-402).** The models carried 202 `[Required]`
  attributes and the SDK read none of them, which is worse than carrying none — a reader reasonably
  assumes a marked field is checked. Sixteen of those annotations contradicted the document and were
  corrected first, so enabling validation does not start rejecting requests the provider accepts.
  Violation messages name the field and the rule and never quote the value, because a validation
  message is a log line waiting to happen and a request body can carry a card number.

### Fixed

- **`GetBanksAsync` and `GetPlansAsync` expected a wrapper object where the API returns a bare array,
  and `finmon.recipient_ipn` was typed as a string where the document declares `integer` (EXP-419).**
  All three threw on the first real call. Found by calling the gateway, not by any test: nothing
  compared property **types** — only names and required-ness — so `PropertyTypeParityTests` now does.

- **Redirects are refused and clear-text endpoints rejected (EXP-383).** The SDK followed redirects
  with both secret headers attached, so a redirect to another origin handed them over; proved with a
  runtime probe showing both headers reaching a second host. The fix is enforced in the `BaseService`
  constructor rather than only on the DI path, because `new RozetkaPayClient(config)` bypassed it.

- **The SDK no longer writes to an `HttpClient` it does not own (EXP-388).** It set `BaseAddress` and
  `Timeout` on the caller's instance, mutating shared state; both are now snapshotted, and timeouts
  use a linked token that preserves the caller's own token identity.

- **`SdkSerializerOptions` is a single frozen shared instance (EXP-389)** rather than a new options
  object per call, which defeated `System.Text.Json`'s metadata cache on every request.

- **Adjudicated the suspected name collisions (EXP-400)**, each decided against the document rather
  than renamed en masse.

### Added

- **A drift job that checks the snapshot against the live document (EXP-387).** This is the gap that
  let everything above ship: the contract tests compared the SDK to a committed snapshot and never to
  what RozetkaPay publishes. `scripts/verify-openapi-drift.sh` downloads the document and fails CI on
  any semantic difference. Currently `59` paths, `67` operations.

- **`RequestBodyParityTests` (EXP-401, EXP-402)** — 15 request bodies compared in both directions,
  `[Required]` included.

- **`ModelFieldCoverageTests`** — no published schema may declare a field the SDK cannot receive.

- **`UndeclaredPropertyTests` (EXP-422)** — the opposite direction, which nothing checked. Scoped to
  properties a type declares itself; inherited extras are a different defect with a different remedy
  and are tracked separately. Twenty-seven exemptions remain, each naming its field and reason, and two
  tests keep the list honest: an entry must still be a real extra, and **none may name a schema a
  request body can reach**, so it structurally cannot hold a request-side extra.

- **`PublicApiSurfaceTests` and an approved surface file (EXP-404).** Nothing guarded the public
  surface, so removing or retyping a member went out in a green build. The baseline is the surface
  after this work rather than the one `1.0.0` published: comparing against `1.0.0` would report
  several hundred intentional differences and be switched off within a week, which is how gates die.

- **Release verification that proves the published package is the one that was built (EXP-405)** —
  artifact contract and byte-for-byte determinism from two different filesystem roots.

- **`PaymentMethodDiscriminatorTests`, `EnumWireTokenTests`, `RedirectSecurityTests`,
  `ConsumerHttpClientOwnershipTests`, `DateTimeConversionTests`, `PartialCaptureContractTests`,
  `MetadataContractTests`, `PropertyTypeParityTests`, `RequestValidationTests`.**

  Each gate was **proven to fail** rather than assumed to work. This matters more than it sounds:
  during the audit, six existing contract-test fixtures were found pinning the exact defect they were
  meant to catch, because they had been written from the implementation instead of from the document.

### Documentation

- **The contract documentation describes only what the tests prove (EXP-407).**
- **The sandbox host rejects RozetkaPay's published test credentials (EXP-424).** Against
  `api-epdev.rozetkapay.com` they answer `401`; against production the same pair authenticates and
  creates real hosted checkouts. A first run naturally pairs `Sandbox` with the only test credentials
  a developer can find, and that combination fails at authentication — which reads like a broken SDK
  and is not. The constant is unchanged: it is what the document publishes as the development server,
  and quietly repointing an environment named `Sandbox` at production would be a worse surprise.

### Migration

- Calls to any of the ten removed operations have no replacement. The routes do not exist; code
  calling them was already failing at runtime.
- `CustomerRequestPaymentMethod.Type = "cc_token"` becomes `PaymentMethodType.CCToken`. An off-enum
  string is now a compile error rather than a silent wire error.
- `BuildP2PRequest` needs the paying customer's email and no longer takes card expiry.
- Replace `apple_pay_token` / `google_pay_token` / `wallet_token` with the declared `ApplePay` /
  `GooglePay` / `Wallet` objects; `card_number` / `card_token` with `CcNumber` / `CcToken`; `card` /
  `recurrent_token` with `CcToken` / `RecurrentId`.
- Requests are now validated before dispatch. A body that violated its own annotations previously
  reached the provider and now throws `RozetkaPayValidationException`.
- Consumers on `net9.0` must move to `net10.0` or stay on `1.0.0`.

### Verification

- **1607 tests** pass, 0 failed, 1 skipped, on `net10.0`.
- Drift job green against the live document: `59` paths, `67` operations.
- Package artifact verification: one dependency group `net10.0`, exactly `lib/net10.0` dll and xml,
  no PDB in the primary package, exactly one PDB in the symbols package, Source Link `72` documents
  all under `/_/` pinned to the commit.
- Deterministic build: byte-identical by SHA-256 from two different filesystem roots.
- Live smoke against the gateway: **zero SDK faults** — a hosted payment created a real checkout URL,
  and every reply the gateway produced was readable. Direct-mode calls return
  `payment_settings_not_found`, which is the shared test account's configuration and not an SDK
  defect.

## [1.0.0] - 2026-07-29

### Added
- Enforceable repository and build conventions (EXP-340). Compiler and analyzer settings are no longer
  copy-pasted per project: a root `Directory.Build.props` declares `ImplicitUsings`, `Nullable`,
  `TreatWarningsAsErrors`, `EnableNETAnalyzers`, `AnalysisLevel` and `EnforceCodeStyleInBuild` once, MSBuild
  imports it into both `SYT.RozetkaPay` and `SYT.RozetkaPay.Tests` (verified by reading the *effective*
  property values back from each project, not by the presence of the file), and the two now-redundant
  `ImplicitUsings`/`Nullable` lines were removed from the project files. Because `TreatWarningsAsErrors` lives
  in the build and not only on the CI command line, a plain `dotnet build -c Release` **without**
  `-warnaserror` now fails on any warning — proven by a temporary `#warning` probe that breaks that exact
  build and by the clean rebuild after it is removed. Both workflows keep passing `-warnaserror` as a visible
  second belt. `AnalysisLevel` is deliberately `latest`, the level this code base satisfies with zero
  warnings, and **no `NoWarn`, `WarningsNotAsErrors`, analyzer package or suppression is introduced**;
  `latest-recommended` would add 66 pre-existing legacy API/perf diagnostics, which is a separate change.
  A root `.editorconfig` fixes UTF-8, LF, final newline, trimmed trailing whitespace and indentation, and
  states the C# style the code already uses, with every preference at `suggestion` or `silent` severity so it
  guides new code instead of rewriting existing files: `dotnet format ... analyzers` and
  `dotnet format ... style` both pass at `--severity warn`, no `.cs` file changed, and the known backlog of
  227 `dotnet format whitespace` diagnostics in 15 legacy files is documented as outstanding rather than
  hidden or silenced. `.gitignore` now really ignores `.claude/` — the previous `./.claude` spelling is not a
  gitignore pattern and matched nothing, so Claude Code worktrees showed up as untracked noise in every
  status — while the existing `.idea*`, `*.bak`, `artifacts*/`, `bin`/`obj`, `TestResults` and package-output
  rules are preserved. Enforcing all of it, `scripts/verify-repository-hygiene.sh` fails the build if Git ever
  starts tracking IDE/agent/build/package junk (enumerated NUL-safely via `git ls-files -z`, so paths with
  spaces cannot slip past) and probes `git check-ignore` in both directions — the junk shapes must be ignored
  and `.gitignore`, `.editorconfig`, `Directory.Build.props`, `.config/dotnet-tools.json`, `.github/**` and
  `scripts/**` must stay visible. The verifier is read-only (it never writes, stages or deletes, and ignored
  files in a working copy are explicitly not an error), takes no arguments, resolves the repository root
  itself so it runs from any directory, and is executed by both `ci.yml` and `release.yml` immediately after
  checkout, before restore, build and publish. Repository-maintainer concern only: no public API, runtime
  behaviour, package content or dependency group changes, and the EXP-338/EXP-339 determinism, package,
  Source Link and release guards are untouched.
- Release-grade, independently verifiable NuGet package artifacts (EXP-339). The package now carries an
  original local `128x128` `PackageIcon` (`assets/package-icon.png`, committed next to its
  `assets/package-icon.svg` source) instead of shipping without one; it is an own SDK mark — a payment-link
  glyph over an `SYT` wordmark — and not the RozetkaPay logo or any third-party asset. Source Link is no
  longer an unstated side effect of SDK defaults: `Deterministic`, `PublishRepositoryUrl` and
  `EmbedUntrackedSources` are pinned in the project file, and Source Link keeps coming from the built-in
  .NET SDK tooling, so **no `Microsoft.SourceLink.*` `PackageReference` is added and the published
  dependency groups (`net9.0`, `net10.0`) are unchanged**. Official builds run with
  `ContinuousIntegrationBuild=true` — set conditionally for `GITHUB_ACTIONS` in the project and passed
  explicitly by both workflows — so every embedded source path is normalized to `/_/*` and no runner or
  worktree filesystem root is published. Two executable gates enforce all of it:
  `scripts/verify-package-artifacts.sh` proves package contents rather than file names (exactly one
  `.nupkg` + one `.snupkg` of the same version, nuspec `id`/`icon`/`readme`/`license`, a
  `<repository type="git" …>` element whose `commit` equals the exact checked-out SHA, unchanged dependency
  groups, `lib/{net9.0,net10.0}` DLL and XML docs, no PDB in the primary package, a single root
  `package-icon.png` whose PNG `IHDR` really is `128x128` and under `1 MiB` and byte-identical to the
  committed asset, a `.snupkg` holding exactly the two PDBs, and — via the `sourcelink` tool pinned to
  `3.1.1` in `.config/dotnet-tools.json` — one Source Link mapping per PDB pointing at that exact commit
  with every document under `/_/` and its source downloaded and checksum-matched);
  `scripts/verify-deterministic-build.sh` proves reproducibility rather than an MSBuild property value by
  rebuilding the same commit in a throwaway detached `git worktree` under a different filesystem root and
  requiring identical SHA-256 for `SYT.RozetkaPay.dll`, `.pdb` and `.xml` on both frameworks. Both
  workflows run both gates in the same order, so a tag release cannot pass weaker artifact checks than a
  pull request, and the pre-existing exact tag/version gate still guards publish. The remote Source Link
  check is on by default; `--skip-remote-source-check` exists only for a local commit that is not pushed
  yet. `assets/package-icon.png` is reproducible from its SVG with:
  ```bash
  magick -size 1024x1024 xc:none -draw "
  scale 8,8
  fill '#12243F' stroke none
  path 'M31 5 H97 A26 26 0 0 1 123 31 V97 A26 26 0 0 1 97 123 H31 A26 26 0 0 1 5 97 V31 A26 26 0 0 1 31 5 Z'
  fill none stroke '#22D3EE' stroke-width 9 stroke-linecap round stroke-linejoin round
  path 'M58 35 H51 A13 13 0 0 0 51 61 H58'
  path 'M70 35 H77 A13 13 0 0 1 77 61 H70'
  path 'M54 48 H74'
  stroke '#FFFFFF' stroke-width 5
  path 'M47 85 A5.5 4.3 0 1 0 42 90 A5.5 4.3 0 1 1 37 95'
  path 'M55.5 82 L61.5 91 L67.5 82'
  path 'M61.5 91 V98'
  path 'M75.5 82 H91.5'
  path 'M83.5 82 V98'
  " -filter Lanczos -resize 128x128 -strip PNG32:assets/package-icon.png
  ```
- Executable contract coverage for **all `67` operations** of the pinned OpenAPI snapshot (EXP-337).
  `tests/SYT.RozetkaPay.Tests/TestInfrastructure/OpenApiOperationManifest.cs` holds one hand-written
  canonical row per published operation; `OpenApiOperationContractTests` compares that table with
  `docs/openapi.json` as exact sets on `(HTTP method, path template, operationId)` — so an operation
  added, removed, renamed, duplicated, or moved to another verb fails the build — and then invokes every
  row's canonical SDK method and asserts the request it produced. Per row: exact verb and concrete
  request target against a literal, single-pass percent-encoding of hostile caller values, body policy
  cross-checked against the document's `requestBody`, content type asserted on the wire, and authentication
  policy cross-checked against the document's operation-level `security`; `Basic` is decoded and compared as UTF-8
  `login:password`, optional headers present only when configured, and the caller's cancellation token
  observed at the transport. The suite cannot reach the network: the base address is in the reserved
  `.invalid` TLD and the transport never forwards. Rows call canonical members only — the `25` legacy
  compatibility routes are not counted as coverage. Test-only; no production code changed.
- Real HTTP-boundary coverage against ASP.NET Core / Kestrel on loopback (EXP-337), through a test-only
  `Microsoft.AspNetCore.App` framework reference — no new NuGet dependency, and no change to the
  package's published dependency set. `HttpBoundaryIntegrationTests` proves what an endpoint actually
  receives: `Basic` decoding as UTF-8 to exactly the configured non-ASCII placeholders with one
  separating colon, `X-ON-BEHALF-OF` / `X-CUSTOMER-AUTH` / user agent arriving verbatim, no credential
  anywhere in the request target, and — for `declinePaymentInstruction` — no credential-bearing header at
  all, a returned `Location`, and a reachable redirect target that records zero requests.
  `WebhookHttpBoundaryTests` drives the documented callback pipeline over real HTTP: raw bytes verified
  before deserialization or any side effect, and fail-closed `400` for a missing, malformed, mismatched,
  or duplicated signature header, a one-byte body mutation, and a semantically identical re-serialization.
  Expected signatures come from the independent reference vectors already pinned by
  `WebhookSignatureVerifierTests`, never from the verifier under test.
- Opt-in live sandbox smoke test (EXP-337): `SandboxSmokeTests` calls only `validateMerchantKeys`
  (`GET /api/merchants/v1/me`) through the supported DI/options route with
  `Environment = RozetkaPayEnvironment.Sandbox`, a bounded timeout, no retry, and no fallback to
  production. `SandboxFactAttribute` skips it unless both `ROZETKAPAY_SANDBOX_LOGIN` and
  `ROZETKAPAY_SANDBOX_PASSWORD` are set, with a reason that names the variables, states that no network
  call was made, and reveals neither which variable is missing nor any value. Absent credentials are
  never a silent pass and never fail an ordinary build. No mutating operation is ever called live, no
  scheduled workflow was added, and no CI secret was configured.
- Typed coverage for the ten operations the refreshed OpenAPI snapshot publishes and the previous one
  did not. All additive: no existing signature, route, verb, body, or response type changed.
- `ISubscriptionService.UpdatePaymentMethodAsync(subscriptionId, request, ct)` — official
  `UpdateSubscriptionPaymentMethod`:
  `PATCH /api/subscriptions/v1/subscriptions/{subscription_id}/payment-method`, with
  `UpdateSubscriptionPaymentMethodRequest`, `SubscriptionPaymentMethodUpdate`,
  `SubscriptionPaymentMethodUpdateType`, `SubscriptionRecurrentIdPaymentMethod`, and
  `UpdateSubscriptionPaymentMethodResponse` in `SYT.RozetkaPay.Models.Subscriptions`. The
  identifier is escaped once as one path segment and never logged; the configured `X-CUSTOMER-AUTH`
  header is honoured. The historical `SubscriptionPaymentMethod` is untouched, not repurposed.
- `IInStorePaymentService` / `InStorePaymentService` with the four official in-store (POS) operations:
  `CreateAsync`, `ConfirmAsync`, `RefundAsync` (`POST /api/in-store-payments/v1/{create,confirm,refund}`)
  and `GetInfoAsync` (`POST /api/in-store-payments/v1/info?external_id=…`). Request and response models
  live in `SYT.RozetkaPay.Models.InStorePayments`, including three distinct receipt types because the
  create, confirm, and refund receipts are three different official shapes. Amounts are strings in the
  smallest monetary unit and are carried verbatim; `InStorePaymentCurrency.Uah` serializes to the
  literal `"980"`.
- `IPartnerService` / `PartnerService` with the three official partner operations: `GetFeeDetailsAsync`,
  `GetMerchantStatusAsync`, and `GetTransactionDetailsAsync`, each with a no-query and an explicit-query
  overload. `PartnerFeeDetailsResponse`, `PartnerFeeDetails`, `PartnerTransactionDetailsListResponse`,
  `PartnerTransactionDetails`, `PartnerMerchantStatusOptions`, and `PartnerTransactionDetailsOptions`
  live in the new `SYT.RozetkaPay.Models.Partners` namespace. Merchant status reuses the existing
  `Models.Merchants.MerchantStatusResponse`, whose shape already matches the official response.
- `IPaymentInstructionService` / `PaymentInstructionService` with `CreateAsync`
  (`POST /api/payment-instructions/v1/new`) and `DeclineAsync`
  (`GET /api/payment-instructions/v1/decline`). `SYT.RozetkaPay.Models.PaymentInstructions` adds
  `CreatePaymentInstructionsRequest`, `PaymentInstructionPayer`, `PaymentInstructionOrder`,
  `PaymentInstructionsResult`, `PaymentInstruction`, `PaymentInstructionDeclineResult`,
  `PaymentInstructionProcessingType`, and `PaymentInstructionMethod`. Enum wire tokens
  (`cardpay`/`ppay`, `auth`/`purchase`) are pinned on the members instead of derived from the naming
  policy, which would emit `card_pay` and `p_pay`.
- `declinePaymentInstruction` is sent unauthenticated and its redirect is never followed. The official
  document declares it `security: []` and answers a bare `302` whose `Location` header is the result.
  Because `HttpClient` has no per-request redirect switch, the SDK uses a dedicated client whose primary
  handler sets `AllowAutoRedirect = false` and which carries no `Authorization`,
  `Proxy-Authorization`, `X-ON-BEHALF-OF`, or `X-CUSTOMER-AUTH` header. The SDK returns the `Location`
  without reading or fetching the target; deciding whether to navigate there — and validating it first —
  belongs to the caller. Platform TLS validation is unchanged: no certificate callback is installed.
- `IRozetkaPayClient` and `RozetkaPayClient` expose `InStorePayments`, `Partners`, and
  `PaymentInstructions`; `AddRozetkaPay` registers all three concrete services and interface aliases as
  scoped, resolving to the same instance, and adds one dedicated non-redirect named `HttpClient`
  (`RozetkaPay.PaymentInstructions.Decline`) sharing only the endpoint, timeout, and user agent.
- Test coverage for the above: `OpenApi59OperationTests`, `SubscriptionPaymentMethodUpdateTests`,
  `InStorePaymentServiceTests`, `PartnerServiceTests`, `PaymentInstructionServiceTests`, and
  `Exp354DisposalTests`, including a non-provider loopback test proving that the decline request carries
  no credential and that the redirect target receives nothing.
- Canonical members for three published operations that the SDK previously reached only through a
  legacy verb, path, body or response shape:
  - `ICustomerService.DeleteCustomerPaymentAsync(request, ct)` and
    `(externalId, request, ct)` — official `deleteCustomerPayment`:
    `DELETE /api/customers/v1/wallet` with an optional `external_id` query value, a JSON request body,
    and a typed `DeleteCustomerPaymentResult` response.
  - `ISubscriptionService.GetSubscriptionsAsync(ct)` and `(externalId, ct)` — official
    `getSubscriptions`: `GET /api/subscriptions/v1/subscriptions` with an optional `external_id` query
    value, reading the official root JSON array.
  - `ISubscriptionService.CancelCustomerSubscriptionAsync(subscriptionId, ct)` and
    `(subscriptionId, options, ct)` — official `CancelCustomerSubscription`:
    `DELETE /api/subscriptions/v1/subscriptions/{subscription_id}/cancel` with no request body and a
    typed `DefaultResponse`.
  The token-only overloads identify the customer through the configured `X-CUSTOMER-AUTH` header.
  Distinct names, not overloads of the old members, so no existing call becomes ambiguous.
- `CancelCustomerSubscriptionOptions` in `SYT.RozetkaPay.Models.Subscriptions`: the optional
  `external_id` and `refund` **query** parameters of the cancel operation. It is never serialized —
  the operation sends no body. `null` omits a parameter, an empty `ExternalId` is sent as
  `external_id=` for the provider to validate, `refund` is rendered lowercase and invariantly, and the
  query order is always `external_id` then `refund`.
- `RozetkaPayApiError` in `SYT.RozetkaPay.Exceptions` and `RozetkaPayException.ApiError`: structured
  details of a failed API call — HTTP status (`HttpStatusCode`), provider error code, request
  identifier, and the raw response body. Every exception raised from a non-success HTTP response
  carries one; manually constructed exceptions, transport failures, and responses the SDK cannot
  deserialize leave `ApiError` as `null`.
- The error code is exposed as a `string`, not as the `ResponseCode` enum, so a code the provider adds
  after this release survives unchanged instead of failing to deserialize or being mapped onto a wrong
  fallback value. A numeric code keeps its raw JSON text.
- The request identifier is resolved from the `X-Request-Id` response header, then `Request-Id`, then
  the payload `error_id`, then `error.error_id`. Header matching is case-insensitive and blank values
  are skipped.
- The raw response body is preserved verbatim, so a malformed, plain-text, or unmodelled payload stays
  inspectable and still yields the same status-specific exception instead of a parser error. The SDK
  never logs the raw body and never places it in `Exception.Message` or `Exception.ToString()`; the
  error log line carries only the HTTP status, the API code, and the request ID. Callers must treat the
  raw body as sensitive and scrub it before logging or storage.
- `RozetkaPayOptions` and `RozetkaPayEnvironment` in `SYT.RozetkaPay.Configuration`: typed
  settings bound from the `RozetkaPay` configuration section
  (`RozetkaPayOptions.SectionName`) and resolvable as `IOptions<RozetkaPayOptions>`.
- Sandbox/production switch: `Environment` selects the endpoint published by the official
  OpenAPI document — `RozetkaPayOptions.ProductionBaseUrl` (the default) or
  `RozetkaPayOptions.SandboxBaseUrl`. An explicit `BaseUrl` still overrides it.
- Validation through the options pipeline — DataAnnotations plus an
  `IValidateOptions<RozetkaPayOptions>` validator for the cross-field rules — checked with
  `ValidateOnStart()`, so a broken configuration throws `OptionsValidationException` while the
  host starts instead of during the first request. Failure messages name the configuration key
  and never contain credentials.
- `AddRozetkaPay(Action<RozetkaPayOptions>)` for configuring the SDK in code, without an
  `IConfiguration`.
- `IRozetkaPayWebhookSignatureVerifier` and `RozetkaPayWebhookSignatureVerifier` in
  `SYT.RozetkaPay.Security`, verifying the `X-ROZETKAPAY-SIGNATURE` header on incoming
  callbacks against the raw request body. Registered as a singleton by `AddRozetkaPay`.
  Missing, malformed, and incorrect signatures return `false` instead of throwing, and
  digests are compared in constant time.
- Tag-based [MinVer](https://github.com/adamralph/minver) versioning; the package
  version is derived from the `v*.*.*` release tag instead of a hardcoded value.
- Pull request build, test, and package verification (`Build & Test` workflow).
- Tag-triggered NuGet publishing and GitHub Releases (`Release NuGet` workflow).

### Changed
- The documented webhook receiver now requires **exactly one** `X-ROZETKAPAY-SIGNATURE` header value
  before verifying (EXP-337). The previous snippet used `FirstOrDefault()`, which silently picks one of
  several values and lets a sender append a header to choose which value is checked. `Verify` takes a
  single value and cannot see that a second one arrived, so this is the caller's check to make; the
  behaviour is now pinned by `WebhookHttpBoundaryTests`. Documentation only — no API change.
- README and `docs/API_COMPATIBILITY.md` now state precisely what is deterministic, what runs against a
  real local HTTP boundary, and what is live: `67/67` means 67 SDK operations produce the 67 requests the
  pinned document declares, verified without a network. The live sandbox check is one read-only merchant
  identity call, is opt-in, and is reported as skipped in CI because no sandbox secret is configured.
- The pinned OpenAPI snapshot `docs/openapi.json` is refreshed to the official document observed on
  `2026-07-25` — SHA-256 `98a9cf2a74b7df6edcaa17872d63f6bc9de96d77ca85a8adfb6a91af05c8e67a`, `59` paths,
  `67` operations, up from `49` paths and `57` operations. `OpenApi59OperationTests` hashes the committed
  file, so the snapshot cannot drift silently. Path coverage is `59/59` and a typed method exists for
  each pinned operation; this is a statement about the pinned document and **not** a claim that a live
  sandbox has answered all `67` operations.
- The two callback resend operations no longer share an operationId: the refreshed document names them
  `resendAlternativePaymentCallback` and `resendPayPartsCallback` instead of `resendCallback`. This is a
  documentation change only — both are unchanged on the wire, and the corresponding SDK methods were not
  renamed or rewired. No non-empty operationId in the pinned document is duplicated any more.
- `BaseService` gains an additive `PatchAsync` overload that takes a static log label, so a `PATCH` whose
  request target carries a caller identifier logs the route template instead. The existing overload keeps
  its previous logging behaviour.
- `BaseService` gains `PostWithoutBodyAsync`, for official `POST` operations that declare no request body.
  It leaves `HttpRequestMessage.Content` null rather than sending an invented `{}`, and reuses the
  existing retry, error-mapping, and deserialization behaviour.
- `BaseService.HandleErrorResponse` and `BaseService.ExecuteWithRetryAsync` changed from `private` to
  `protected` so an operation needing its own transport reuses the single status-to-exception switch and
  the single retry loop instead of duplicating either. Both additive; no status-to-exception mapping
  changed.
- `RozetkaPayClient.Dispose` also releases the decline client the payment-instruction service created
  internally. Disposal stays idempotent, and an externally supplied `HttpClient` is still not disposed.
- The safe-label `PatchAsync` overload and `PostWithoutBodyAsync` dispose their `HttpResponseMessage`
  deterministically, on the success path and on the path where the status-to-exception mapper throws. The
  pre-existing two-argument `PatchAsync` delegates to the new overload and inherits that fix; its signature,
  request target, body and logging are unchanged.
- **`AddRozetkaPay` now removes the built-in `IHttpClientFactory` HTTP logging from both of its named
  clients** (`RozetkaPay` and `RozetkaPay.PaymentInstructions.Decline`), so entries under
  `System.Net.Http.HttpClient.RozetkaPay.*` — `LogicalHandler` and `ClientHandler` alike — are no longer
  emitted for any SDK operation. This is a deliberate privacy fix, not a convenience change: that logging
  writes the request URI, and while `Microsoft.Extensions.Http` redacts the whole query to `?*` it does
  **not** redact path segments, so the `subscription_id` of the new payment-method update reached the log
  verbatim at Information level. Its header logging also redacts values in the rendered message only,
  leaving the real `Authorization` and `X-CUSTOMER-AUTH` values in the structured state at Trace level.
  Neither is configurable, so the loggers are removed with the supported `RemoveAllLoggers()` API.
- Removing those loggers does not change what the SDK's **own** service logging emits, and no SDK-wide
  guarantee about that logging is made here. The ten operations added in this entry log a static route
  template and, in the logging their tests capture under the default disabled retry policy, no caller
  identifier, credential, request body, response body, `RozetkaPayApiError.RawBody`, or decline `Location`.
  Every pre-existing operation logs exactly what it logged before and is **not** audited or claimed to be
  identifier- or content-safe: most log the real request target, several routes embed a caller identifier in
  it (`/api/customers/v1/{customerId}/cards/{cardId}`,
  `/api/alternative-payments/v1/operation/{externalId}`, `/api/payparts/v1/operation/{operationId}`, and the
  query strings of list operations), `PaymentService.ConfirmP2PAsync` logs the external ID and the amount,
  and the shared retry warning includes the transport exception message when retries are enabled. See the
  Logging section of the package README for the exact scope. Applications that need request-level HTTP
  telemetry should add their own `DelegatingHandler` or `IHttpClientLogger` that logs a redacted target.
  *(That legacy gap was subsequently closed by EXP-359 — see **Fixed**. The retry warning's exception message
  was removed by EXP-356.)*
- `DeletePaymentFromWalletAsync`, `GetCustomerSubscriptionsAsync` and `CancelAsync` are now
  `[Obsolete]` on both the interfaces and the implementations, each naming its canonical replacement.
  This is a compile-time warning only: their route, HTTP verb, request body and response type are
  unchanged on the wire, and every pre-existing signature stays binary compatible.
- `SubscriptionList` keeps its public class, `Subscriptions` property, property type and base type; an
  internal `JsonConverter<SubscriptionList>` now maps the official root JSON array onto it. Reads
  accept both the official array and the historical `{ "subscriptions": [...] }` wrapper; writes emit
  the official root array. An official `[]` yields an empty list, a wrapper carrying
  `"subscriptions": null` yields `null`, and an absent list is normalized to `[]` when serializing.
- Every DELETE request now rejects an already-cancelled `CancellationToken` before the retry loop and
  before `HttpClient` is invoked, so no DELETE reaches an `HttpMessageHandler` after the caller has
  cancelled. This previously depended on a `HttpClient` pre-dispatch check that behaves differently on
  `net9.0` and `net10.0`. DELETE was the first verb to get that guarantee and, at the time, the only one;
  EXP-357 (see **Fixed**) extends the same contract to every transport helper — including the JSON-body
  DELETE, whose guard used to run after the caller's body had already been serialized.
- The exception hierarchy and every pre-existing public exception constructor are unchanged:
  `RozetkaPayException`, `RozetkaPayAuthorizationException`, `RozetkaPayValidationException`,
  `RozetkaPayRateLimitException`, and `RozetkaPayNotFoundException` keep their parameterless,
  `(string)`, and `(string, Exception)` constructors, none is obsolete, and the status-to-exception
  mapping is untouched. `ApiError` is an added get-only property, and the structured construction path
  is internal, so `new RozetkaPayException("message", null)` stays unambiguous.
- The API error log line changed from `API error response received. StatusCode: … Message: …` to
  `RozetkaPay API error. StatusCode: … ApiCode: … RequestId: …`; the parsed provider message is no
  longer logged. Exception messages themselves are unchanged, and error-message parsing additionally
  accepts a nested `error.message`.
- A push to `main` no longer publishes a NuGet package; publishing now happens
  only when a version tag is pushed.
- `AddRozetkaPay` now builds its configuration snapshot from the validated options. The existing
  `RozetkaPayConfiguration`, `IConfiguration`, and login/password overloads, their
  registration-time failures, and every service lifetime are unchanged; the snapshot stays
  resolvable from DI. Settings that the previous check let through but the SDK cannot use — a
  `BaseUrl` that is well formed but not `http`/`https`, a non-positive `Timeout`, a
  whitespace-only login or password, an inconsistent retry policy — are now rejected while the
  host starts instead of failing on the first request.

### Removed
- **Breaking (pre-1.0):** `RozetkaPayOptions.ValidateSslCertificate` and
  `RozetkaPayConfiguration.ValidateSslCertificate` are removed. Both were dead: neither was ever read
  by a service, by the DI registration, or by any `HttpMessageHandler`, so setting either to `false`
  changed no TLS behaviour. The switch only ever promised something the SDK did not do, and removing
  the false promise is the point of this change — no certificate-validation feature was added, and no
  replacement setting exists. Code that assigned the property now fails to compile; delete the
  assignment.
- The removal was chosen over wiring the switch up. Connecting it would have meant putting a
  certificate-validation bypass into a payment SDK, and it could not have been applied consistently
  anyway: the SDK is constructed through a DI named `HttpClient`, an internally owned direct client, a
  caller-supplied `HttpClient`, and direct service construction, and a handler switch cannot reach an
  `HttpClient` a caller already built. Doing this before the stable 1.0 contract is the right
  compatibility boundary.
- TLS certificate validation is unchanged and always follows the platform or caller-supplied
  `HttpMessageHandler` policy. The SDK installs no certificate callback, never disables platform
  validation, and does not inspect or replace a caller's handler.
- `AddRozetkaPay(IConfiguration)` now throws `InvalidOperationException` when the removed
  `RozetkaPay:ValidateSslCertificate` key is still present, whatever its value, instead of letting the
  binder ignore it — a silently dropped key would leave an operator believing a TLS policy they
  configured is still in force. The message names the key, says why it was removed, and never contains
  its value or any neighbouring setting. **Migration: remove the key from your configuration.** Nothing
  else needs to change.
- To trust a certificate the platform does not: install the CA in the operating-system trust store
  (production), or build the `HttpClient` yourself with a handler narrowed to that one certificate and
  pass it to the SDK (local or test infrastructure). Production code must never install a trust-all
  callback such as `DangerousAcceptAnyServerCertificateValidator` or a
  `ServerCertificateCustomValidationCallback` that returns `true`: it disables authentication of the
  payment endpoint and exposes credentials and card data to interception.

### Fixed
- **A shared `HttpClient`'s default headers are no longer rewritten, and jitter no longer allocates a
  generator per retry delay** (EXP-341). Two independent defects on hot, shared state:
  - **`BaseService` wrote authentication onto the client.** Its constructor assigned
    `DefaultRequestHeaders.Authorization`, cleared and rebuilt `DefaultRequestHeaders.UserAgent`, and
    removed/re-added `X-ON-BEHALF-OF` and `X-CUSTOMER-AUTH`. `RozetkaPayClient` builds every service over
    **one** client and a consumer may own and share that client too, so the last service constructed decided
    what *every* service sent, construction order was observable, and the writes happened on state other
    requests were reading concurrently. A consumer's own `Authorization`, user agent or headers of those
    names were silently overwritten or, when the SDK had no value configured, silently **removed**.
  - **The credentials, user agent and optional headers are now request-scoped.** They are parsed and
    snapshotted once during construction — the user agent *and* both optional values through one throwaway
    scratch `HttpRequestMessage`, so the header grammar is still what validates them and an invalid user
    agent, or an `OnBehalfOf` / `CustomerAuth` value carrying an illegal CR/LF, still fails *while the
    service is constructed* rather than on the first call — and attached by a single private request factory
    that every authenticated transport uses: `GET`, JSON `POST`, `POST` accepting `204`/empty, `PATCH`,
    bodyless `POST`, `DELETE` with and without a JSON body. The factory runs **inside** the retry attempt, so
    each retry's fresh `HttpRequestMessage` carries the same values. Blank stays absent, as before.
    `BaseService` no longer reads, adds to or removes from the authenticated client's
    `DefaultRequestHeaders` at all, and `ApplyOptionalHeader` is gone. The SDK's one remaining look at a
    default header collection is deliberate and unchanged: `PaymentInstructionService` still *reads* a
    caller-supplied **decline** client's defaults in order to reject one that carries credentials (EXP-354),
    and still never writes to it.
  - **Precedence without duplication, and no rewriting of consumer state.** A header set on the request wins
    outright over a caller default of the same name — `HttpClient` merges defaults only for names the request
    does not already carry — so the wire carries exactly one `Authorization`, one `User-Agent` and one value
    of each configured optional header, proven per transport family and under concurrent calls from two
    services with different logins, user agents and optional values over one client. Conversely, when the SDK
    configuration names no optional value the caller's own default of that name is left in place and keeps
    flowing, and an unrelated caller default is untouched throughout. The caller's whole default header
    collection is compared against a full pre-construction snapshot before, during and after calls.
  - **Jitter reuses the runtime's shared random source.** `RetryPolicy.CalculateDelay` allocated a
    `new Random()` on every `ExponentialWithJitter` delay; it now uses the thread-safe process-wide
    `Random.Shared`. A warmed allocation-counting regression test measures 14.4 MB over 200 000 calculations
    on the old code against a 64 KB budget on the new one. No lock, `ThreadLocal<Random>`, cryptographic RNG
    or seed injection was added, and the formula, the ±25 % band and the non-negative clamp are unchanged.
  - **Unchanged:** public API, dependencies, target frameworks, retry semantics (enablement, attempt counts,
    retriable statuses and exceptions, `Retry-After` handling, cancellation, logging, error mapping), and the
    EXP-354 decline design — `DeclineAsync` stays unauthenticated on its own non-redirecting client, never
    goes through the authenticated request factory, still rejects caller-supplied decline clients that carry
    credential-bearing defaults, and still never mutates or disposes a caller-supplied one. Service
    construction does still set `BaseAddress` and `Timeout` on the supplied client; only the header
    collection is now left alone.
- **The SDK's own service logging no longer contains any caller value from the request target** (EXP-359).
  This closes the legacy gap that the EXP-354 entry under **Changed** correctly described as out of its scope:
  most pre-existing operations logged their **real** request target, so any identifier the caller put in a path
  or a query reached a sink at Information level — in the rendered message *and* in the structured `Endpoint` /
  `FallbackEndpoint` properties. Concretely, and measured on the base commit:
  `/api/customers/v1/<customerId>/cards/<cardId>`,
  `/api/alternative-payments/v1/operation/<externalId>`, `/api/payparts/v1/operation/<operationId>`,
  `/api/subscriptions/v1/subscriptions/<subscriptionId>[/payments|/cancel]`,
  `/api/subscriptions/v1/plans/<planId>`, and the full query string of every info, list and lookup operation.
  What changed:
  - **the no-label `BaseService` overloads fail closed.** Every transport overload that takes no separate log
    label now logs the constant `[redacted]` instead of the target it was given, and delegates to its
    label-aware counterpart. That covers `GetAsync`, `PostAsync`, `PostAsyncWithNoContent`, `PatchAsync`,
    `DeleteAsync` and the three `404` fallback wrappers. It protects **externally derived** services too: a
    dynamic target passed to a no-label helper can no longer become a log entry. A label is deliberately never
    derived from the target — a static route segment and a caller identifier are indistinguishable in an
    arbitrary path, so normalizing one would be a guess, and a wrong guess is the leak;
  - **additive label-aware overloads** complete the set: `PostAsync`, `PostAsyncWithNoContent`,
    `GetAsyncWithFallback`, `PostAsyncWithFallback` and `PostAsyncWithNoContentWithFallback` now each have a
    form taking an explicit static label. The fallback forms take one label per side, ordered
    *primary target, primary label, fallback target, fallback label*, so a label cannot drift onto the wrong
    request. No existing signature changed, nothing became obsolete, and the existing label-aware `GetAsync`,
    `PatchAsync`, `PostWithoutBodyAsync` and `DeleteAsync` overloads are untouched;
  - **every internal callsite passes a compile-time static route template** — all `91` transport-helper
    callsites across the 13 service implementation files, static routes included, so the fail-closed change
    could not silently downgrade a route that was already safe to log. A route carrying a caller identifier
    logs the template with the parameter name, e.g.
    `/api/subscriptions/v1/subscriptions/{subscription_id}/payments`; a route carrying query values logs the
    path with no query. No label is built by interpolation, concatenation, `Uri.EscapeDataString`,
    `RequestTargetEncoding.EscapePathSegment`, or from a request DTO;
  - **`PaymentService.ConfirmP2PAsync` no longer logs the external ID and the amount.** That statement was the
    one place a service logged a request-body value directly; it is removed, with no substitute message, since
    the transport helper already writes the safe route label. Both values are still sent in the body unchanged;
  - a fallback entry now reads `Primary endpoint <label> returned 404. Falling back to <label>.` and names no
    real target, still written after the cancellation guard.
  Coverage: `LegacyLoggingRedactionTests` plus
  `TestInfrastructure/LoggingRedactionTestInfrastructure.cs` — `64` tests on each of `net9.0` and `net10.0`,
  driving the real helpers through a test-only derived probe and the real services through an intercepting
  transport. They assert the rendered message, the structured state, the category **and** the scopes of every
  captured entry, against hostile markers in both raw and percent-encoded spelling; that each dynamic callsite
  still sends the expected value at the correct insertion point; that no SDK operation emits `[redacted]`; and
  that credentials, request bodies, success response bodies, provider error text,
  `RozetkaPayApiError.RawBody` and the decline `Location` are absent while the thrown exception keeps its
  mapped type and its raw body. No mocking package was added, no test opens a socket or resolves a name, and
  the SDK opens no logging scope.
  Unchanged: the public API (no interface, existing signature, overload resolution, cancellation-token default,
  DTO, exception constructor or DI registration was altered — the new overloads are additive); the wire (verb,
  request target, query order and escaping, request body, content type, response deserialization); the retry
  policy, repeat count and `Retry-After` handling; cancellation semantics, including the first-line
  pre-dispatch guard of `ExecuteWithRetryAsync` and the `ThrowIfCancellationRequested()` that opens each
  fallback `catch`; the exception hierarchy and error mapping, including `RozetkaPayApiError.RawBody`;
  request, response and content disposal; the `DeclineAsync` redirect semantics; the EXP-354
  `RemoveAllLoggers()` factory suppression; and the package dependencies and target frameworks. The retry
  warning and the API error log are separate, already-protected surfaces and were not touched — the error log
  deliberately keeps `StatusCode`, `ApiCode` and `RequestId`. Full per-operation audit:
  `src/SYT.RozetkaPay/docs/LOGGING_AUDIT.md`.
- **An already-cancelled `CancellationToken` now stops every HTTP helper before it does anything**
  (EXP-357). The two DELETE paths and the payment-instruction decline carried an explicit pre-dispatch
  guard; every other helper entered the shared retry executor and left the outcome to the runtime's own
  pre-dispatch check inside `HttpClient` — which is not a contract. It fires at different points on
  `net9.0` and `net10.0` and differs per verb, so the same cancelled call behaved differently depending on
  the framework and the operation. Measured on the base commit: an already-cancelled `GET`, bodiless
  `POST`, or `GET` fallback **dispatched the request and returned a successful result** on both target
  frameworks; a cancelled JSON `POST`, `POST` accepting `204`, `PATCH`, or `POST` fallback did the same on
  `net10.0` and on `net9.0` cancelled only *after* writing its `Making … request to …` log; and the
  JSON-body `DELETE` serialized the caller's body before reaching its own guard, because that helper
  serializes outside the attempt.
  Cancellation is now one contract owned by the SDK:
  - the check is the **first executable line** of `BaseService.ExecuteWithRetryAsync`, before
    `Configuration.RetryPolicy` is read and before any retry counter exists — so, for every helper whose
    attempt passes through the shared executor, it precedes helper logging, JSON serialization, request
    allocation, and every `HttpClient` and `HttpMessageHandler` call. It is repeated at the top of each
    later loop iteration, so a token cancelled while the previous attempt ran never enters the next one;
  - the JSON-body `DELETE` gained its own guard **before** `JsonSerializer.Serialize`, and the bodiless
    `DELETE` and decline guards stay where they are — both do work before reaching the executor;
  - the three `404` fallback wrappers (`GET`, `POST`, `POST` accepting `204`) check the token as the
    **first statement** of their `catch (RozetkaPayNotFoundException)`, so cancellation that becomes
    observable after a primary `404` prevents the fallback request *and* the "falling back" log line.
    `OperationCanceledException` is not caught there and still escapes unchanged;
  - the handler is invoked exactly **zero** times, a pre-cancelled call writes **no** log entry at all, and
    the caller's body is **never** serialized;
  - the exception carries the caller's **exact** `CancellationToken` — `ThrowIfCancellationRequested`, not
    a hand-built or tokenless exception — so callers can still distinguish their own cancellation from a
    timeout;
  - enabled and disabled `RetryPolicy` produce identical semantics, because the check precedes the policy
    read.
  Unchanged: the public API (no interface, signature, overload, cancellation-token default, DTO, exception
  constructor, `RetryPolicy` member, or DI registration was touched); timeout semantics — a timeout-like
  `TaskCanceledException` while the caller's token is still live is still a retriable transport failure and
  is never reported as caller cancellation; mid-flight cancellation, which still ends the operation after
  the one attempt already at the transport, with no retry, no fallback, and full per-attempt disposal; and
  every route, verb, body, content type, response mapping, and redirect for a token that is not cancelled.
  The methods remain `async`, so the exception surfaces on `await` — synchronous throw timing is not
  claimed and was not changed.
- **Configured retriable HTTP statuses are now actually retried** (EXP-356). `RetryPolicy` has always
  published `RetriableStatusCodes`, defaulting to `500`, `502`, `503`, `504`, `429`, `408`, and
  `RetryPolicy.ShouldRetry(HttpStatusCode)` has always reported those values correctly — but the retry loop
  only ever had a status-specific branch for `429`. Every other configured status escaped on the **first**
  attempt, and editing the set changed nothing for any status except `429`. A caller who enabled retries to
  survive a gateway blip got a single attempt and no indication that the setting was inert.
  The loop is now one decision instead of a chain of type-specific catches, and it reads the HTTP status from
  the response the SDK received (`RozetkaPayException.ApiError.StatusCode`) rather than from an exception type,
  message, or provider code:
  - all six default statuses repeat, and a custom set is honoured exactly as configured — additions
    (a `409`, or a `400`) and removals alike; an empty set retries no status at all, while transport failures
    still do;
  - an SDK exception carrying no `ApiError` never came from a response, so its class name alone no longer makes
    it retriable;
  - `MaxRetryAttempts` keeps its meaning — retries **after** the initial call — for exactly `1 + MaxRetryAttempts`
    requests; `Enabled = false` or `MaxRetryAttempts = 0` sends one;
  - a repeat is the same request: same verb, target, body bytes, content type, and authentication mode;
  - transport failures stay retriable — `HttpRequestException`, `SocketException`, and timeout-like
    `TaskCanceledException`, including one already wrapped in a `RozetkaPayException`. A `SocketException`
    raised directly by a handler is now retried too, which is what `RetryPolicy.ShouldRetry(Exception)` has
    always published.
- **Caller cancellation no longer buys another attempt** (EXP-356). A `TaskCanceledException` raised after the
  caller's token was cancelled was previously treated as a retriable timeout, so a cancelled operation could
  issue further requests. Cancellation is now checked before any retry decision: no delay is scheduled, no
  further attempt is made, and the `OperationCanceledException` propagates unwrapped. Cancelling **during** a
  retry delay aborts the wait without sending the next request. A timeout-like `TaskCanceledException` while
  the caller's token is still live remains retriable.
- **Exhausting the retry budget no longer erases the failure** (EXP-356). The loop used to end with
  `RozetkaPayException("Request failed after N attempts: …")`, discarding the status-specific exception type and
  the `RozetkaPayApiError` — status, provider code, request identifier, and raw body — that a caller needs for
  support correspondence. The final exception is now the one the last attempt produced, unchanged: an exhausted
  `429` is still `RozetkaPayRateLimitException`, an exhausted `500` still carries `Internal server error`, and
  the attached evidence is the **final** response's, with nothing merged in from earlier attempts.
- **`Retry-After` on a `429` now affects the wait** (EXP-356). The header was read only to compose the exception
  message and was ignored by the delay calculation. It is now parsed while the response is still open and drives
  the delay: delta-seconds as given, an HTTP-date converted to a relative delay, zero or a past date meaning
  retry immediately, and a positive value **capped by `MaxDelay`** so a mistaken or hostile header cannot park a
  request for hours. An absent or unparseable header is treated as no hint and uses the configured backoff —
  an invalid header never replaces `RozetkaPayRateLimitException` with a parser error. `Retry-After` is ignored
  on other statuses, and the exception message keeps the delta-seconds form and the historical `60` fallback it
  has always reported. The parsed value is carried internally; no public member was added.
- **The retry warning no longer renders the failure's message** (EXP-356). It interpolated
  `exception.Message`, whose content comes from the runtime or the provider and can carry provider text or a raw
  body. It now reports the retry number, the configured budget, the failure category as an exception type name,
  the HTTP status when the failure came from a response, and the computed delay in milliseconds — and the
  exception object is not passed to the logger at all, so a sink cannot expand it. The per-attempt error log
  already carried the safe identifiers and is unchanged.
- **Every retry attempt now owns and releases its own request and response** (EXP-356). `GetAsync`, `PostAsync`,
  and `PostAsyncWithNoContent` left the `HttpResponseMessage` — and on a real handler the connection behind it —
  to the finalizer, and built their request through the convenience `HttpClient` methods; the JSON-body
  `DeleteAsync` disposed its request but not its response. All four now build an explicit
  `HttpRequestMessage` inside the attempt and dispose request, request body, response, and response content on
  every path: success, a retried failure, exhaustion, a throwing body read, and cancellation. `PatchAsync`,
  `PostWithoutBodyAsync`, and the payment-instruction decline already did this and are unchanged. Wire
  behaviour is identical — same verb, target, body, content type, and headers.
- No public API changed for any of the above: `RetryPolicy` keeps every property and factory, the exception
  hierarchy and all public exception constructors are untouched, and the default policy is still **disabled**.
  Enabling retries repeats real requests, so a mutating financial operation can result in a second
  provider-side operation; the SDK does not claim exactly-once delivery. See **Retry policy** in the package
  `README.md` for the idempotency requirement.
- Operation-level parity defects against the pinned OpenAPI snapshot
  (`docs/openapi.json`, SHA-256 `309e61bf2185706c137f2d270d767b31777f7a4d09f2f2e0fb900fe36601cc44`,
  `49` paths / `57` operations):
  - `deleteCustomerPayment` was called as `DELETE /api/customers/v1/{customerId}/cards/{cardId}` with
    no request body and deserialized into `DeleteCardFromWalletResponse` instead of
    `DeleteCustomerPaymentResult`.
  - `getSubscriptions` was called as
    `GET /api/subscriptions/v1/subscriptions/customer/{customerId}` and deserialized into the
    `CustomerSubscriptionsResponse` wrapper instead of the official root array.
  - `CancelCustomerSubscription` was sent as a `POST` with an `external_id`/`reason`/`immediate` JSON
    body and returned no typed response, instead of a bodiless `DELETE` with optional `external_id`
    and `refund` query parameters returning `DefaultResponse`.
  No canonical call ever falls back to the legacy path or verb, on `404` or on any other failure: the
  request and response shapes genuinely differ, and `reason` and `immediate` have no honest mapping onto
  `refund`, so a fallback would conceal a parity error. A canonical `404` makes exactly one HTTP request.
  Retry behaviour was not changed by the parity work: with a retry policy enabled, the SDK may repeat the
  **same** canonical request target for the conditions its policy declares — corrected in the EXP-356 entry
  above — always as the same operation against the same target, and never as a different route or verb.
- `docs/API_COMPATIBILITY.md` no longer implies that path presence equals operation parity, and now
  reports the pinned snapshot and the live official document as two separate views.
- Every dynamic query value the SDK puts into a request URI is now percent-encoded as an
  individual value. A caller value can no longer add or overwrite a query parameter
  (`external_id=pay&status=success` stays one `external_id`), start a fragment (`#`), smuggle a
  raw `?`, or reach the path with `/`. This covers `PaymentService.GetInfoAsync`,
  `GetReceiptAsync`, and `GetListAsync`; `PayoutService.GetInfoAsync` and `GetListAsync`;
  `PayPartsService.GetOperationsAsync`; `AlternativePaymentService.GetOperationsAsync`; and the
  FinMon P2P pre-limits query. Endpoints that already encoded their values — the customer wallet
  endpoints, the payout account balance, and the PayParts/Alternative Payments info endpoints —
  are unchanged and are still encoded exactly once.
- Encoding semantics: a space becomes `%20` (never `+`), a literal `+` becomes `%2B`, and `&`,
  `=`, `?`, `#`, `/`, `%` become `%26`, `%3D`, `%3F`, `%23`, `%2F`, `%25`. Non-ASCII text is sent
  as UTF-8 percent-encoded octets. RFC 3986 unreserved characters stay readable, so request URIs
  built from ordinary IDs, dates, and pagination values are byte-for-byte what they were before.
- Callers pass raw values. The SDK encodes once, so a value that already looks encoded is treated
  as literal text: `already%2Fencoded` is sent as `already%252Fencoded`. Do not pre-encode.
- List filter dates and pagination are formatted with `CultureInfo.InvariantCulture`. Under a
  non-Gregorian ambient culture the `yyyy-MM-dd` filters previously rendered in that calendar —
  `th-TH` turned `2026-02-28` into `2569-02-28` — and a culture-specific negative sign could reach
  the query. Both now always render invariantly.
- A whitespace-only string filter is still sent, and now arrives as its encoded characters
  (`status=%20%20%20`) instead of being truncated to an empty value by URI canonicalization. Null
  and empty filters are still omitted, and query parameter names, ordering, and endpoint paths are
  unchanged.
- Every caller-controlled identifier the SDK puts into a request **path** is now percent-encoded as
  an individual path segment. Previously such an identifier was interpolated raw, so a `/` added a
  path segment, a `?` started a query, and a `#` started a fragment and silently discarded the rest
  of the identifier. Input that already looked like a percent escape was passed through as an escape
  rather than as data, so `already%2Fencoded` went on the wire unchanged and a server decodes it to
  `already/encoded`. Non-ASCII text, spaces, and a `%` that does not form a valid escape were already
  percent-encoded correctly by `System.Uri` and were never misrouted; they are covered by the new
  tests as regression protection, not as fixed defects. This covers
  `AlternativePaymentService.GetOperationInfoAsync` and
  `GetStatusAsync`; `PayPartsService.GetOperationInfoAsync`; `CustomerService.GetCustomerCardsAsync`
  and `DeletePaymentFromWalletAsync` (both segments encoded independently); and every
  `SubscriptionService` plan and subscription route — `GetPlanAsync`, `UpdatePlanAsync`,
  `DeactivatePlanAsync`, `GetAsync`, `UpdateAsync`, `DeactivateAsync`, `GetPaymentsAsync`,
  `CancelAsync`, and `GetCustomerSubscriptionsAsync`.
- This is the path-segment counterpart of the query-value encoding fixed earlier in this release.
  A path segment and a query value are separate contexts and are encoded at their own insertion
  points; the compatibility fallback paths that already encoded their segments are unchanged on the
  wire and are still encoded exactly once.
- Callers pass raw identifiers and must not pre-encode: `already%2Fencoded` is sent as
  `already%252Fencoded`. An identifier made only of RFC 3986 unreserved characters, such as
  `plan-123`, reaches the wire byte-for-byte unchanged.
- The identifiers `.` and `..` are now rejected with `ArgumentException` naming the offending
  parameter, before any request is sent. They cannot be carried through as data: `.` is an unreserved
  character that percent-encoding leaves alone, and `System.Uri` removes exact dot segments — also
  from the `%2E` spelling — while building the request, which would silently retarget the call
  (`GetPlanAsync(".")` requested `/api/subscriptions/v1/plans/`). A method with a path fallback
  rejects such an identifier before its primary query request.
- Endpoint paths, HTTP verbs, request bodies, primary/fallback order, the 404-only fallback trigger,
  public interfaces, and models are unchanged.

## [0.1.0-alpha.2] - 2026-02-28

### Fixed
- NuGet README maintainer image rendering.

## [0.1.0-alpha.1] - 2026-02-28

### Added
- Initial alpha SDK package.

[Unreleased]: https://github.com/i7aket/SYT.RozetkaPay/compare/v6.0.0...main
[6.0.0]: https://www.nuget.org/packages/SYT.RozetkaPay/6.0.0
[5.0.0]: https://www.nuget.org/packages/SYT.RozetkaPay/5.0.0
[4.0.0]: https://www.nuget.org/packages/SYT.RozetkaPay/4.0.0
[3.0.0]: https://www.nuget.org/packages/SYT.RozetkaPay/3.0.0
[2.0.0]: https://www.nuget.org/packages/SYT.RozetkaPay/2.0.0
[1.0.0]: https://www.nuget.org/packages/SYT.RozetkaPay/1.0.0
[0.1.0-alpha.2]: https://www.nuget.org/packages/SYT.RozetkaPay/0.1.0-alpha.2
[0.1.0-alpha.1]: https://www.nuget.org/packages/SYT.RozetkaPay/0.1.0-alpha.1
