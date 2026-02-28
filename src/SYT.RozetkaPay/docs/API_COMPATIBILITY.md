# API Compatibility Matrix

## Scope

- SDK: `SYT.RozetkaPay` (`0.1.0-alpha.1`)
- API path family: `v1` (`/api/*/v1/*`)
- OpenAPI schema version: `3.0.3`

## Source of Truth

- Official public docs: `https://cdn.rozetkapay.com/public-docs/index.html`
- Local snapshot used by this repository: `docs/openapi.json`

## Last Verification

- Date: `2026-02-28`
- Result: local OpenAPI snapshot updated from official public docs and SDK service routes re-validated.
- Public OpenAPI path count: `49`
- SDK service coverage for OpenAPI paths: `49/49`
- Additional legacy compatibility routes in SDK: `25`

## Endpoint Coverage Status (SDK vs latest public OpenAPI)

All OpenAPI `v1` paths from `docs/openapi.json` are represented in SDK service layer methods.
For endpoints that changed route naming between API revisions, SDK keeps legacy fallbacks to avoid breaking existing integrations.

## Known Runtime Inconsistency (Observed in Integrations)

- Some API responses return numeric values as JSON strings (for example, `"100.00"` or `"2"`) instead of JSON numbers (`100.00`, `2`), despite OpenAPI declaring numeric types.
- This behavior was observed in integration testing and reported to RozetkaPay.
- As of `2026-02-28`, this is still reproducible on selected endpoints.
- SDK mitigation:
  - flexible decimal converters for `decimal` / `decimal?`
  - flexible integer converters for `int` / `int?` / `long` / `long?`
  - global `JsonNumberHandling.AllowReadingFromString` enabled in serializer options
  - converters are read-compatible with both formats, so SDK remains stable before and after potential API-side normalization.
