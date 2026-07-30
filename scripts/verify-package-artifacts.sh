#!/usr/bin/env bash
#
# Verifies the SYT.RozetkaPay NuGet artifacts produced by 'dotnet pack'.
#
# This is the single verifier shared by the PR/main CI workflow and the tag
# release workflow, so a release cannot pass looser checks than a pull request.
# It inspects archive *contents*, never just file names.
#
# Usage:
#   scripts/verify-package-artifacts.sh <artifact-dir> <expected-commit-sha> [--skip-remote-source-check]
#
# Arguments:
#   <artifact-dir>          Directory holding exactly one .nupkg and one .snupkg.
#   <expected-commit-sha>   Full 40-hex commit the packages must be stamped with,
#                           e.g. "$(git rev-parse HEAD)". Always pass the SHA of
#                           the exact checkout that was built - never a branch or
#                           tag name.
#
# Options:
#   --skip-remote-source-check
#           Skip 'dotnet sourcelink test', which downloads every source document
#           from raw.githubusercontent.com and compares checksums. Use this ONLY
#           for a local commit that has not been pushed yet: the sources cannot
#           exist on the remote, so the download must fail. Every structural
#           check still runs. CI and release builds must NOT pass this flag.
#
# Examples:
#   # CI / release (commit is pushed; full verification including remote sources)
#   scripts/verify-package-artifacts.sh ./artifacts "$(git rev-parse HEAD)"
#
#   # Local, commit not pushed yet (structural checks only)
#   scripts/verify-package-artifacts.sh ./artifacts "$(git rev-parse HEAD)" --skip-remote-source-check

set -euo pipefail

readonly EXPECTED_PACKAGE_ID='SYT.RozetkaPay'
readonly EXPECTED_ICON_ENTRY='package-icon.png'
readonly EXPECTED_ICON_WIDTH=128
readonly EXPECTED_ICON_HEIGHT=128
readonly MAX_ICON_BYTES=$((1024 * 1024))
readonly EXPECTED_REPOSITORY_URL='https://github.com/i7aket/SYT.RozetkaPay'
readonly EXPECTED_TFMS='net10.0'
readonly EXPECTED_RAW_HOST='https://raw.githubusercontent.com/i7aket/SYT.RozetkaPay'

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# Declared and assigned separately: 'readonly x="$(...)"' would make readonly's
# own exit status the command status and mask a failing subshell (SC2155).
repo_root="$(cd -- "${script_dir}/.." && pwd)"
readonly repo_root

tmp_dir=''

cleanup() {
    local status=$?
    if [ -n "${tmp_dir}" ] && [ -d "${tmp_dir}" ]; then
        rm -rf -- "${tmp_dir}"
    fi
    return "${status}"
}
trap cleanup EXIT

# Self-contained help text. Deliberately not scraped out of the header comment
# with a line-range sed: the range silently drifts whenever the header changes
# and can spill live shell code into the help output.
usage() {
    cat <<'USAGE'
Verifies the SYT.RozetkaPay NuGet artifacts produced by 'dotnet pack'.

This is the single verifier shared by the PR/main CI workflow and the tag
release workflow, so a release cannot pass looser checks than a pull request.
It inspects archive contents, never just file names.

Usage:
  scripts/verify-package-artifacts.sh <artifact-dir> <expected-commit-sha> [--skip-remote-source-check]

Arguments:
  <artifact-dir>          Directory holding exactly one .nupkg and one .snupkg.
  <expected-commit-sha>   Full 40-hex commit the packages must be stamped with,
                          e.g. "$(git rev-parse HEAD)". Always pass the SHA of
                          the exact checkout that was built - never a branch or
                          tag name.

Options:
  --skip-remote-source-check
          Skip 'dotnet sourcelink test', which downloads every source document
          from raw.githubusercontent.com and compares checksums. Use this ONLY
          for a local commit that has not been pushed yet: the sources cannot
          exist on the remote, so the download must fail. Every structural
          check still runs. CI and release builds must NOT pass this flag.
  -h, --help
          Print this help and exit.

Examples:
  # CI / release (commit is pushed; full verification including remote sources)
  scripts/verify-package-artifacts.sh ./artifacts "$(git rev-parse HEAD)"

  # Local, commit not pushed yet (structural checks only)
  scripts/verify-package-artifacts.sh ./artifacts "$(git rev-parse HEAD)" --skip-remote-source-check
USAGE
}

fail() {
    printf 'ERROR: %s\n' "$*" >&2
    exit 1
}

note() {
    printf '  %s\n' "$*"
}

# Prints the number of archive entries exactly equal to $2. awk keeps the exit
# status at 0 for a zero count, so a missing entry surfaces as a failed
# comparison rather than as a swallowed grep exit code.
zip_entry_count() {
    unzip -Z1 "$1" | awk -v want="$2" '$0 == want { n++ } END { print n + 0 }'
}

# Prints the number of archive entries whose name matches the awk regex in $2.
zip_entry_match_count() {
    unzip -Z1 "$1" | awk -v re="$2" '$0 ~ re { n++ } END { print n + 0 }'
}

# Prints the text of a single-occurrence nuspec element, e.g. <id>…</id>.
nuspec_element() {
    sed -n "s:.*<$2>\([^<]*\)</$2>.*:\1:p" < "$1"
}

sha256_of() {
    if command -v sha256sum >/dev/null 2>&1; then
        sha256sum < "$1" | awk '{ print $1 }'
    else
        shasum -a 256 < "$1" | awk '{ print $1 }'
    fi
}

# ---------------------------------------------------------------------------
# Arguments
# ---------------------------------------------------------------------------

artifact_dir=''
expected_sha=''
skip_remote_source_check=0
positional_count=0

for arg in "$@"; do
    case "${arg}" in
        --skip-remote-source-check)
            skip_remote_source_check=1
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        -*)
            printf 'ERROR: unknown option: %s\n\n' "${arg}" >&2
            usage >&2
            exit 2
            ;;
        *)
            positional_count=$((positional_count + 1))
            case "${positional_count}" in
                1) artifact_dir="${arg}" ;;
                2) expected_sha="${arg}" ;;
                *)
                    printf 'ERROR: unexpected extra argument: %s\n\n' "${arg}" >&2
                    usage >&2
                    exit 2
                    ;;
            esac
            ;;
    esac
done

if [ "${positional_count}" -ne 2 ]; then
    printf 'ERROR: expected exactly 2 positional arguments, got %s.\n\n' "${positional_count}" >&2
    usage >&2
    exit 2
fi

if [ ! -d "${artifact_dir}" ]; then
    fail "Artifact directory does not exist: ${artifact_dir}"
fi

if ! printf '%s' "${expected_sha}" | grep -Eq '^[0-9a-f]{40}$'; then
    fail "Expected commit must be a full lowercase 40-hex SHA, got: '${expected_sha}'." \
         "Pass \"\$(git rev-parse HEAD)\" for the exact checkout that was built."
fi

artifact_dir="$(cd -- "${artifact_dir}" && pwd)"
tmp_dir="$(mktemp -d)"

printf 'Verifying package artifacts in: %s\n' "${artifact_dir}"
printf 'Expected repository commit:     %s\n\n' "${expected_sha}"

# ---------------------------------------------------------------------------
# 1. Package counts, names and versions
# ---------------------------------------------------------------------------

printf '[1/6] Package counts and names\n'

shopt -s nullglob
nupkgs=()
for candidate in "${artifact_dir}"/*.nupkg; do
    # *.snupkg does not match *.nupkg, but be explicit rather than rely on it.
    case "${candidate}" in
        *.snupkg) continue ;;
    esac
    nupkgs+=("${candidate}")
done
snupkgs=("${artifact_dir}"/*.snupkg)
shopt -u nullglob

if [ "${#nupkgs[@]}" -ne 1 ]; then
    fail "Expected exactly one .nupkg (excluding .snupkg), found ${#nupkgs[@]}."
fi
if [ "${#snupkgs[@]}" -ne 1 ]; then
    fail "Expected exactly one .snupkg, found ${#snupkgs[@]}."
fi

nupkg="${nupkgs[0]}"
snupkg="${snupkgs[0]}"

[ -s "${nupkg}" ] || fail "Primary package is empty: ${nupkg}"
[ -s "${snupkg}" ] || fail "Symbol package is empty: ${snupkg}"

nupkg_name="$(basename -- "${nupkg}")"
snupkg_name="$(basename -- "${snupkg}")"

case "${nupkg_name}" in
    "${EXPECTED_PACKAGE_ID}."*) ;;
    *) fail "Primary package name '${nupkg_name}' does not start with '${EXPECTED_PACKAGE_ID}.'." ;;
esac
case "${snupkg_name}" in
    "${EXPECTED_PACKAGE_ID}."*) ;;
    *) fail "Symbol package name '${snupkg_name}' does not start with '${EXPECTED_PACKAGE_ID}.'." ;;
esac

nupkg_file_version="${nupkg_name#"${EXPECTED_PACKAGE_ID}".}"
nupkg_file_version="${nupkg_file_version%.nupkg}"
snupkg_file_version="${snupkg_name#"${EXPECTED_PACKAGE_ID}".}"
snupkg_file_version="${snupkg_file_version%.snupkg}"

if [ -z "${nupkg_file_version}" ]; then
    fail "Could not derive a version from primary package name '${nupkg_name}'."
fi
if [ "${nupkg_file_version}" != "${snupkg_file_version}" ]; then
    fail "Primary package version '${nupkg_file_version}' and symbol package version '${snupkg_file_version}' differ."
fi

note "primary: ${nupkg_name}"
note "symbols: ${snupkg_name}"
note "version: ${nupkg_file_version}"

# ---------------------------------------------------------------------------
# 2. Primary nuspec contract
# ---------------------------------------------------------------------------

printf '[2/6] Primary nuspec metadata\n'

nuspec_entry="$(unzip -Z1 "${nupkg}" | awk '/\.nuspec$/ { n++; e = $0 } END { if (n == 1) print e }')"
if [ -z "${nuspec_entry}" ]; then
    fail "Primary package does not contain exactly one .nuspec entry."
fi

nuspec_file="${tmp_dir}/primary.nuspec"
unzip -p "${nupkg}" "${nuspec_entry}" > "${nuspec_file}"
[ -s "${nuspec_file}" ] || fail "Extracted nuspec is empty: ${nuspec_entry}"

nuspec_id="$(nuspec_element "${nuspec_file}" id)"
nuspec_version="$(nuspec_element "${nuspec_file}" version)"
nuspec_icon="$(nuspec_element "${nuspec_file}" icon)"
nuspec_readme="$(nuspec_element "${nuspec_file}" readme)"

[ "${nuspec_id}" = "${EXPECTED_PACKAGE_ID}" ] ||
    fail "nuspec <id> is '${nuspec_id:-missing}', expected '${EXPECTED_PACKAGE_ID}'."
[ "${nuspec_version}" = "${nupkg_file_version}" ] ||
    fail "nuspec <version> '${nuspec_version:-missing}' does not match package file version '${nupkg_file_version}'."
[ "${nuspec_icon}" = "${EXPECTED_ICON_ENTRY}" ] ||
    fail "nuspec <icon> is '${nuspec_icon:-missing}', expected '${EXPECTED_ICON_ENTRY}'."
[ "${nuspec_readme}" = 'README.md' ] ||
    fail "nuspec <readme> is '${nuspec_readme:-missing}', expected 'README.md'. README metadata must not regress."

if ! grep -Fq '<license type="expression">MIT</license>' < "${nuspec_file}"; then
    fail "nuspec is missing '<license type=\"expression\">MIT</license>'. License metadata must not regress."
fi

# The repository element carries the provenance the whole task is about. Assert
# each attribute separately: 'dotnet pack' also emits a 'branch' attribute whose
# value legitimately differs between a PR build and a tag build.
repository_line="$(awk '/<repository /{ n++; print } END { if (n != 1) exit 1 }' < "${nuspec_file}")" ||
    fail "nuspec must contain exactly one <repository> element."

for required in \
    'type="git"' \
    "url=\"${EXPECTED_REPOSITORY_URL}\"" \
    "commit=\"${expected_sha}\"" ; do
    if ! printf '%s' "${repository_line}" | grep -Fq -- "${required}"; then
        fail "nuspec <repository> is missing ${required}. Actual element:${repository_line}"
    fi
done

# Dependency groups must be unchanged by this task: exactly the two TFMs, and no
# new build-only package (a SourceLink PackageReference would show up here).
actual_tfms="$(
    tr '<' '\n' < "${nuspec_file}" |
        awk 'match($0, /targetFramework="[^"]*"/) { print substr($0, RSTART + 17, RLENGTH - 18) }' |
        sort |
        paste -sd, -
)"
[ "${actual_tfms}" = "${EXPECTED_TFMS}" ] ||
    fail "nuspec dependency groups are '${actual_tfms:-none}', expected exactly '${EXPECTED_TFMS}'."

note "id/version: ${nuspec_id} ${nuspec_version}"
note "icon: ${nuspec_icon}"
note "repository commit: ${expected_sha}"
note "dependency groups: ${actual_tfms}"

# ---------------------------------------------------------------------------
# 3. Primary package payload
# ---------------------------------------------------------------------------

printf '[3/6] Primary package payload\n'

for tfm in net10.0; do
    for ext in dll xml; do
        entry="lib/${tfm}/${EXPECTED_PACKAGE_ID}.${ext}"
        count="$(zip_entry_count "${nupkg}" "${entry}")"
        [ "${count}" -eq 1 ] ||
            fail "Primary package must contain exactly one '${entry}', found ${count}."
    done
done

pdb_count="$(zip_entry_match_count "${nupkg}" '\.pdb$')"
[ "${pdb_count}" -eq 0 ] ||
    fail "Primary package contains ${pdb_count} .pdb entr(y|ies); symbols must ship only in the .snupkg."

icon_count="$(zip_entry_count "${nupkg}" "${EXPECTED_ICON_ENTRY}")"
[ "${icon_count}" -eq 1 ] ||
    fail "Primary package must contain exactly one root '${EXPECTED_ICON_ENTRY}' entry, found ${icon_count}."

note "lib/net10.0 dll and xml present"
note "no .pdb in primary package"

# ---------------------------------------------------------------------------
# 4. Packed icon bitmap
# ---------------------------------------------------------------------------

printf '[4/6] Packed icon bitmap\n'

icon_file="${tmp_dir}/${EXPECTED_ICON_ENTRY}"
unzip -p "${nupkg}" "${EXPECTED_ICON_ENTRY}" > "${icon_file}"
[ -s "${icon_file}" ] || fail "Extracted icon is empty: ${EXPECTED_ICON_ENTRY}"

icon_bytes="$(wc -c < "${icon_file}" | tr -d '[:space:]')"
[ "${icon_bytes}" -lt "${MAX_ICON_BYTES}" ] ||
    fail "Packed icon is ${icon_bytes} bytes, which is not below the ${MAX_ICON_BYTES} byte (1 MiB) limit."

# Parse the PNG signature and IHDR straight out of the first 24 bytes so the
# check needs no image library at runtime:
#   bytes  0-7   PNG signature
#   bytes  8-11  IHDR chunk length (13)
#   bytes 12-15  "IHDR"
#   bytes 16-19  width  (big endian)
#   bytes 20-23  height (big endian)
icon_head="$(od -An -tx1 -N 24 < "${icon_file}" | tr -d ' \n')"
[ "${#icon_head}" -eq 48 ] || fail "Packed icon is shorter than a PNG header (24 bytes)."

[ "${icon_head:0:16}" = '89504e470d0a1a0a' ] ||
    fail "Packed icon does not start with the PNG signature (got 0x${icon_head:0:16})."
[ "${icon_head:24:8}" = '49484452' ] ||
    fail "Packed icon's first chunk is not IHDR."

icon_width=$((16#${icon_head:32:8}))
icon_height=$((16#${icon_head:40:8}))

[ "${icon_width}" -eq "${EXPECTED_ICON_WIDTH}" ] && [ "${icon_height}" -eq "${EXPECTED_ICON_HEIGHT}" ] ||
    fail "Packed icon is ${icon_width}x${icon_height}, expected ${EXPECTED_ICON_WIDTH}x${EXPECTED_ICON_HEIGHT}."

# The committed asset and the packed copy must be the same file. A missing
# committed asset is a hard failure, not a skipped comparison: treating it as
# optional would let the packed icon go unchecked exactly when the repository
# lost the source of truth it is supposed to be compared against.
committed_icon="${repo_root}/assets/${EXPECTED_ICON_ENTRY}"
if [ ! -f "${committed_icon}" ]; then
    fail "Committed icon asset is missing: ${committed_icon}
The packed icon cannot be proven to be the reviewed asset without it."
fi
if [ ! -s "${committed_icon}" ]; then
    fail "Committed icon asset is empty: ${committed_icon}"
fi

packed_hash="$(sha256_of "${icon_file}")"
committed_hash="$(sha256_of "${committed_icon}")"
[ "${packed_hash}" = "${committed_hash}" ] ||
    fail "Packed icon (${packed_hash}) differs from committed assets/${EXPECTED_ICON_ENTRY} (${committed_hash})."
note "matches committed assets/${EXPECTED_ICON_ENTRY}"

note "PNG ${icon_width}x${icon_height}, ${icon_bytes} bytes"

# ---------------------------------------------------------------------------
# 5. Symbol package contract
# ---------------------------------------------------------------------------

printf '[5/6] Symbol package contract\n'

expected_pdbs="lib/net10.0/${EXPECTED_PACKAGE_ID}.pdb"

actual_lib_entries="$(unzip -Z1 "${snupkg}" | awk '/^lib\// { print }' | sort)"
if [ "${actual_lib_entries}" != "${expected_pdbs}" ]; then
    fail "Symbol package lib/ entries are not exactly the expected PDB.
Expected:
${expected_pdbs}
Actual:
${actual_lib_entries:-none}"
fi

for forbidden in '\.dll$' '\.png$' '\.xml$'; do
    # [Content_Types].xml is OPC packaging metadata, not payload, so scope the
    # extension rules to the payload tree.
    count="$(zip_entry_match_count "${snupkg}" "^lib/.*${forbidden}")"
    [ "${count}" -eq 0 ] ||
        fail "Symbol package contains ${count} lib/ entr(y|ies) matching '${forbidden}'; it must ship PDBs only."
done

# Nothing may ride along outside the PDB and the standard OPC metadata:
# no assembly, no icon, no README, no extra directory.
unexpected="$(
    unzip -Z1 "${snupkg}" |
        awk '
            $0 ~ /^lib\/net10\.0\/SYT\.RozetkaPay\.pdb$/ { next }
            $0 == "[Content_Types].xml" { next }
            $0 == "_rels/.rels" { next }
            $0 ~ /^SYT\.RozetkaPay\.nuspec$/ { next }
            $0 ~ /^package\/services\/metadata\/core-properties\/[0-9a-f]+\.psmdcp$/ { next }
            { print }
        '
)"
if [ -n "${unexpected}" ]; then
    fail "Symbol package contains unexpected entries beyond the PDB and OPC metadata:
${unexpected}"
fi

note "exactly lib/net10.0 PDB, no dll/icon/extra payload"

# ---------------------------------------------------------------------------
# 6. Source Link metadata in both PDBs
# ---------------------------------------------------------------------------

printf '[6/6] Source Link metadata\n'

expected_json="{\"documents\":{\"/_/*\":\"${EXPECTED_RAW_HOST}/${expected_sha}/*\"}}"

if [ "${skip_remote_source_check}" -eq 1 ]; then
    remote_status='SKIPPED (--skip-remote-source-check; local unpushed commit)'
else
    remote_status='PASSED (every source document downloaded and checksum-matched)'
fi

for tfm in net10.0; do
    pdb_entry="lib/${tfm}/${EXPECTED_PACKAGE_ID}.pdb"
    pdb_file="${tmp_dir}/${tfm}.pdb"
    unzip -p "${snupkg}" "${pdb_entry}" > "${pdb_file}"
    [ -s "${pdb_file}" ] || fail "Extracted PDB is empty: ${pdb_entry}"

    # 'dotnet sourcelink' is a repository-local tool, so resolve it against the
    # manifest at <repo>/.config/dotnet-tools.json regardless of the caller's cwd.
    actual_json="$(cd -- "${repo_root}" && dotnet sourcelink print-json "${pdb_file}" | tr -d ' \n\r')"

    if [ "${actual_json}" != "${expected_json}" ]; then
        fail "${tfm}: Source Link JSON does not match the expected single mapping.
Expected: ${expected_json}
Actual:   ${actual_json}"
    fi

    documents_file="${tmp_dir}/${tfm}.documents"
    (cd -- "${repo_root}" && dotnet sourcelink print-documents "${pdb_file}") > "${documents_file}"

    document_count="$(awk 'NF { n++ } END { print n + 0 }' < "${documents_file}")"
    [ "${document_count}" -gt 0 ] || fail "${tfm}: PDB reports no source documents."

    # print-documents emits "<checksum> <algorithm> <language> <path>". Compare
    # the path field only - matching against the whole line would let the
    # hexadecimal checksum satisfy a '^/_/' test and make this check vacuous.
    unnormalized="$(
        awk '{ path = $4; for (i = 5; i <= NF; i++) path = path " " $i;
               if (path !~ /^\/_\//) print path }' < "${documents_file}"
    )"
    if [ -n "${unnormalized}" ]; then
        fail "${tfm}: PDB has source documents that are not normalized under '/_/':
${unnormalized}"
    fi

    # Belt and braces: no machine-, runner- or worktree-specific root anywhere.
    leaked="$(grep -nE '/Volumes/|/Users/|/home/[^/]+/|[A-Za-z]:\\\\|\.claude/worktrees' < "${documents_file}" || true)"
    if [ -n "${leaked}" ]; then
        fail "${tfm}: PDB leaks a machine-specific source root:
${leaked}"
    fi

    if [ "${skip_remote_source_check}" -eq 0 ]; then
        # No '|| true', no pipe: a checksum mismatch or a missing source document
        # must fail this script.
        if ! (cd -- "${repo_root}" && dotnet sourcelink test "${pdb_file}"); then
            fail "${tfm}: 'dotnet sourcelink test' failed. Source for commit ${expected_sha} could not be downloaded and checksum-verified."
        fi
    fi

    note "${tfm}: ${document_count} documents, all under /_/, mapping pinned to ${expected_sha}"
done

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

cat <<SUMMARY

Package artifact verification PASSED
  primary package  : ${nupkg}
  symbols package  : ${snupkg}
  package version  : ${nuspec_version}
  package icon     : ${EXPECTED_ICON_ENTRY} (PNG ${icon_width}x${icon_height}, ${icon_bytes} bytes, limit ${MAX_ICON_BYTES})
  repository commit: ${expected_sha}
  target frameworks: net10.0
  source link remote verification: ${remote_status}
SUMMARY
