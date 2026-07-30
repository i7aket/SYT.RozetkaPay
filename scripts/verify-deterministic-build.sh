#!/usr/bin/env bash
#
# Proves the build is reproducible by building the *same commit* from two
# different filesystem roots and comparing the resulting assemblies.
#
# Asserting the value of the 'Deterministic' MSBuild property would prove
# nothing: it is on by default and says only what the build was asked to do.
# This script instead builds HEAD twice - once in this checkout, once in a
# throwaway detached git worktree under a mktemp directory - and requires the
# SHA-256 of SYT.RozetkaPay.dll, .pdb and .xml to match for every target
# framework. Different roots are the point: a non-normalized source path would
# change the PDB and make the hashes diverge.
#
# Usage:
#   scripts/verify-deterministic-build.sh
#
# Runs from any working directory; the repository root is resolved with git.
# Never commits, pushes, tags, or touches any worktree other than the temporary
# one it creates. Uses no destructive git command: no reset, no clean, no
# checkout of tracked files.

set -euo pipefail

readonly ASSEMBLY_NAME='SYT.RozetkaPay'
readonly SOLUTION='SYT.RozetkaPay.sln'
readonly PROJECT_REL='src/SYT.RozetkaPay'
readonly TFMS=(net10.0)
readonly COMPARED_EXTENSIONS=(dll pdb xml)

tmp_root=''
worktree_b=''
repo_root=''

cleanup() {
    # Preserve the real exit status: this trap must never turn a failure green.
    local status=$?

    if [ -n "${worktree_b}" ] && [ -n "${tmp_root}" ] && [ -d "${worktree_b}" ]; then
        # Refuse to remove anything that is not inside our own mktemp directory,
        # so this can never delete the feature worktree, the main checkout, or
        # another agent's worktree.
        case "${worktree_b}" in
            "${tmp_root}"/*)
                git -C "${repo_root}" worktree remove --force "${worktree_b}" >/dev/null 2>&1 ||
                    rm -rf -- "${worktree_b}"
                ;;
            *)
                printf 'WARNING: refusing to remove unexpected worktree path: %s\n' "${worktree_b}" >&2
                ;;
        esac
    fi

    if [ -n "${tmp_root}" ] && [ -d "${tmp_root}" ]; then
        rm -rf -- "${tmp_root}"
    fi

    if [ -n "${repo_root}" ]; then
        git -C "${repo_root}" worktree prune >/dev/null 2>&1 || true
    fi

    return "${status}"
}
trap cleanup EXIT

fail() {
    printf 'ERROR: %s\n' "$*" >&2
    exit 1
}

sha256_of() {
    if command -v sha256sum >/dev/null 2>&1; then
        sha256sum < "$1" | awk '{ print $1 }'
    else
        shasum -a 256 < "$1" | awk '{ print $1 }'
    fi
}

build_release() {
    # The exact same command both workflows run, so what this script compares is
    # what CI and the release actually ship.
    local root="$1"
    ( cd "${root}" &&
      dotnet restore "${SOLUTION}" &&
      dotnet build "${SOLUTION}" \
          -c Release \
          --no-restore \
          -warnaserror \
          -p:ContinuousIntegrationBuild=true )
}

# ---------------------------------------------------------------------------
# 1. Preconditions
# ---------------------------------------------------------------------------

if [ "$#" -ne 0 ]; then
    fail "This script takes no arguments (got $#). Usage: scripts/verify-deterministic-build.sh"
fi

repo_root="$(git rev-parse --show-toplevel)" ||
    fail "Not inside a git repository."

commit="$(git -C "${repo_root}" rev-parse HEAD)" ||
    fail "Repository has no commit at HEAD; commit before proving determinism."

if ! git -C "${repo_root}" diff --quiet HEAD --; then
    fail "Tracked files differ from HEAD (${commit}).
The second checkout is created from the commit, so uncommitted tracked changes
would be compared against a build that does not contain them. Commit or stash
them first. Untracked files are fine and are left alone."
fi

printf 'Deterministic build verification\n'
printf '  repository : %s\n' "${repo_root}"
printf '  commit     : %s\n' "${commit}"

# ---------------------------------------------------------------------------
# 2. Checkout A - this worktree
# ---------------------------------------------------------------------------

printf '\n[1/3] Building checkout A (%s)\n' "${repo_root}"
if ! build_release "${repo_root}" > /dev/null; then
    fail "Release build failed in checkout A (${repo_root})."
fi

# ---------------------------------------------------------------------------
# 3. Checkout B - throwaway detached worktree at the same commit
# ---------------------------------------------------------------------------

tmp_root="$(mktemp -d)"
worktree_b="${tmp_root}/checkout-b"

printf '[2/3] Building checkout B (%s)\n' "${worktree_b}"
git -C "${repo_root}" worktree add --detach "${worktree_b}" "${commit}" >/dev/null ||
    fail "Could not create a temporary detached worktree at ${worktree_b}."

worktree_b_commit="$(git -C "${worktree_b}" rev-parse HEAD)"
[ "${worktree_b_commit}" = "${commit}" ] ||
    fail "Temporary worktree is at ${worktree_b_commit}, expected ${commit}."

if ! build_release "${worktree_b}" > /dev/null; then
    fail "Release build failed in checkout B (${worktree_b})."
fi

# ---------------------------------------------------------------------------
# 4. Compare
# ---------------------------------------------------------------------------

printf '[3/3] Comparing build outputs\n\n'

mismatches=0
compared=0

for tfm in "${TFMS[@]}"; do
    for ext in "${COMPARED_EXTENSIONS[@]}"; do
        rel="${PROJECT_REL}/bin/Release/${tfm}/${ASSEMBLY_NAME}.${ext}"
        file_a="${repo_root}/${rel}"
        file_b="${worktree_b}/${rel}"

        [ -f "${file_a}" ] || fail "Missing build output in checkout A: ${rel}"
        [ -f "${file_b}" ] || fail "Missing build output in checkout B: ${rel}"

        hash_a="$(sha256_of "${file_a}")"
        hash_b="$(sha256_of "${file_b}")"
        compared=$((compared + 1))

        if [ "${hash_a}" = "${hash_b}" ]; then
            printf '  MATCH    %-8s %-3s %s\n' "${tfm}" "${ext}" "${hash_a}"
        else
            mismatches=$((mismatches + 1))
            printf '  MISMATCH %-8s %-3s\n' "${tfm}" "${ext}" >&2
            printf '    checkout A (%s): %s\n' "${repo_root}" "${hash_a}" >&2
            printf '    checkout B (%s): %s\n' "${worktree_b}" "${hash_b}" >&2
        fi
    done
done

if [ "${mismatches}" -ne 0 ]; then
    fail "${mismatches} of ${compared} artifact(s) differ between the two checkout roots.
The build of ${commit} is not reproducible: something in it depends on the
filesystem path it was built from, or on non-deterministic build input."
fi

cat <<SUMMARY

Deterministic build verification PASSED
  commit            : ${commit}
  checkout A        : ${repo_root}
  checkout B        : ${worktree_b} (temporary, removed on exit)
  target frameworks : ${TFMS[*]}
  artifacts compared: ${compared} (${ASSEMBLY_NAME}.{${COMPARED_EXTENSIONS[0]},${COMPARED_EXTENSIONS[1]},${COMPARED_EXTENSIONS[2]}} per framework)
  result            : byte-identical by SHA-256 from two different roots
SUMMARY
