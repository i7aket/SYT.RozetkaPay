#!/usr/bin/env bash
#
# Compares the committed OpenAPI snapshot against the document RozetkaPay publishes.
#
# Why this exists: every contract test in this repository asserts against the snapshot. That makes
# the suite a check that the SDK matches what someone once downloaded, not what the provider serves
# today. Release 1.0.0 shipped green with enums outside the published token set, request bodies
# missing required fields, and two response-code schemas the SDK modelled with three values out of
# 184 — none of which any test could have seen. This job is the missing half: it checks the
# snapshot itself.
#
# The comparison is semantic rather than textual. Key order and whitespace are not contract, so a
# byte diff would cry wolf on a reformat and teach everyone to ignore the job. What is contract is
# the set of operations, the shape of every schema, and the exact enum tokens.
set -euo pipefail

LIVE_URL="${ROZETKAPAY_OPENAPI_URL:-https://docs.rozetkapay.com/openapi.json}"
SNAPSHOT="$(cd "$(dirname "$0")/.." && pwd)/src/SYT.RozetkaPay/docs/openapi.json"
LIVE="$(mktemp)"
trap 'rm -f "$LIVE"' EXIT

echo "Fetching ${LIVE_URL}"
curl --fail --silent --show-error --location --max-time 60 --retry 2 -o "$LIVE" "$LIVE_URL"

python3 - "$SNAPSHOT" "$LIVE" <<'PY'
import difflib
import json
import sys

snapshot_path, live_path = sys.argv[1:3]
with open(snapshot_path, encoding="utf-8") as handle:
    snapshot = json.load(handle)
with open(live_path, encoding="utf-8") as handle:
    live = json.load(handle)

METHODS = ("get", "post", "put", "patch", "delete", "head", "options")


def operations(document):
    return {
        (method.upper(), path, operation.get("operationId"))
        for path, item in document.get("paths", {}).items()
        for method, operation in item.items()
        if method in METHODS
    }


def canonical(document):
    return json.dumps(document, sort_keys=True, ensure_ascii=False, indent=1).splitlines()


problems = []

for operation in sorted(operations(live) - operations(snapshot)):
    problems.append(f"published upstream, absent from the snapshot: {operation}")
for operation in sorted(operations(snapshot) - operations(live)):
    problems.append(f"in the snapshot, no longer published: {operation}")

diff = list(difflib.unified_diff(canonical(snapshot), canonical(live), "snapshot", "live", n=2, lineterm=""))
if diff:
    problems.append(f"schema drift, {len(diff)} diff lines:")
    problems.extend(diff[:200])
    if len(diff) > 200:
        problems.append(f"... {len(diff) - 200} further diff lines suppressed")

if problems:
    print("OpenAPI drift detected.\n")
    print("\n".join(problems))
    print(
        "\nRefresh src/SYT.RozetkaPay/docs/openapi.json from the published document, reconcile the "
        "SDK with what changed, and re-run. Do not refresh the snapshot alone: the contract tests "
        "read their expectations from it, so a silent refresh would make them agree with the drift."
    )
    sys.exit(1)

print(
    f"Snapshot matches the published document: {len(live.get('paths', {}))} paths, "
    f"{len(operations(live))} operations."
)
PY
