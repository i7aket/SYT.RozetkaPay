#!/usr/bin/env bash
#
# Proves that the repository itself stays clean: that no IDE, agent, build or
# package junk has become *tracked*, and that the ignore contract which keeps it
# untracked is actually in force.
#
# Reading .gitignore and finding a line there would prove nothing - a leading
# './', a stray comment marker or a pattern in the wrong section silently stops
# matching, which is exactly the defect this script exists to catch. So every
# claim here is checked against Git itself: the tracked set comes from
# 'git ls-files -z', and each ignore rule is probed with
# 'git check-ignore --no-index'.
#
# Usage:
#   scripts/verify-repository-hygiene.sh
#
# Takes no arguments and runs from any working directory; the repository root is
# resolved with git. Strictly read-only: it never writes, deletes, stages or
# checks out anything, and it never inspects the environment. Local ignored
# files on disk (.idea/, bin/, obj/, artifacts-check/, .claude/) are normal
# developer state and are deliberately *not* an error - 'git status' is not a
# criterion here, only what Git tracks is.
#
# Exit codes:
#   0  repository hygiene verified
#   1  a hygiene rule is violated
#   2  wrong usage

set -euo pipefail

readonly REQUIRED_CONFIG_FILES=(
    'Directory.Build.props'
    '.editorconfig'
)

# Representative paths that must be ignored. One per rule of the ignore
# contract, using the concrete shapes that have actually shown up in this
# repository's working copies.
readonly MUST_BE_IGNORED=(
    '.claude/worktrees/probe'
    '.idea/probe.xml'
    '.idea.backup.20990101/probe.xml'
    '.vscode/settings.json'
    'SYT.RozetkaPay.sln.bak'
    'artifacts-check/probe.nupkg'
    'artifacts/probe.snupkg'
    'src/SYT.RozetkaPay/bin/Release/probe.dll'
    'src/SYT.RozetkaPay/obj/probe.g.cs'
    'TestResults/probe.trx'
    'src/SYT.RozetkaPay/probe.user'
    '.DS_Store'
)

# Paths that carry real repository configuration and must therefore stay
# visible to Git. An over-broad ignore pattern is as much a defect as a missing
# one: it would make a required file impossible to commit.
readonly MUST_NOT_BE_IGNORED=(
    '.config/dotnet-tools.json'
    '.github/workflows/ci.yml'
    '.github/workflows/release.yml'
    'Directory.Build.props'
    '.editorconfig'
    '.gitignore'
    'assets/package-icon.png'
    'scripts/verify-package-artifacts.sh'
    'scripts/verify-deterministic-build.sh'
    'scripts/verify-repository-hygiene.sh'
)

usage() {
    cat >&2 <<'USAGE'
Usage: scripts/verify-repository-hygiene.sh

Verifies that no IDE/agent/build/package junk is tracked by Git and that the
.gitignore contract is in force. Takes no arguments.
USAGE
}

fail() {
    printf 'ERROR: %s\n' "$*" >&2
    exit 1
}

# Returns the reason a tracked path is forbidden, or nothing if it is fine.
# Written as one case statement so the rule set is readable in one screen; '*'
# matches '/' in a case pattern, which is what makes the '*/bin/*' style rules
# work at any depth.
forbidden_reason() {
    case "$1" in
        .DS_Store|*/.DS_Store)
            printf 'macOS Finder metadata' ;;
        .idea|.idea/*|.idea.*)
            printf 'JetBrains IDE state' ;;
        .claude|.claude/*)
            printf 'Claude Code agent state' ;;
        .vscode|.vscode/*)
            printf 'Visual Studio Code state' ;;
        *.suo|*.user|*.userosscache|*.sln.docstates)
            printf 'IDE user-specific file' ;;
        bin/*|*/bin/*|obj/*|*/obj/*)
            printf 'build output' ;;
        TestResults/*|*/TestResults/*|TestResults|*/TestResults)
            printf 'test result output' ;;
        artifacts/*|artifacts-check/*)
            printf 'package artifact output' ;;
        *.nupkg|*.snupkg)
            printf 'package artifact' ;;
        *.bak|*.tmp|*.temp|*.log)
            printf 'temporary or backup file' ;;
        *)
            : ;;
    esac
}

# ---------------------------------------------------------------------------
# 1. Preconditions
# ---------------------------------------------------------------------------

if [ "$#" -ne 0 ]; then
    printf 'ERROR: This script takes no arguments (got %s).\n' "$#" >&2
    usage
    exit 2
fi

# Resolved from the script's own location first, so the verifier checks the
# repository it belongs to no matter which directory it is invoked from; the
# current directory is only a fallback for an exotic invocation (e.g. through a
# copy on PATH).
script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" > /dev/null 2>&1 && pwd)"
repo_root="$(git -C "${script_dir}" rev-parse --show-toplevel 2> /dev/null)" || repo_root=''

if [ -z "${repo_root}" ]; then
    repo_root="$(git rev-parse --show-toplevel 2> /dev/null)" || repo_root=''
fi

[ -n "${repo_root}" ] ||
    fail "Not inside a git repository (looked from ${script_dir} and the current directory)."

printf 'Repository hygiene verification\n'
printf '  repository : %s\n' "${repo_root}"

# ---------------------------------------------------------------------------
# 2. Required repository configuration
# ---------------------------------------------------------------------------

missing_config=()
for config in "${REQUIRED_CONFIG_FILES[@]}"; do
    if [ ! -s "${repo_root}/${config}" ]; then
        missing_config+=("${config}")
    fi
done

if [ "${#missing_config[@]}" -ne 0 ]; then
    printf 'ERROR: Missing or empty required repository configuration:\n' >&2
    for config in "${missing_config[@]}"; do
        printf '  %s\n' "${config}" >&2
    done
    fail "Common build properties and the code-style contract must exist and be non-empty."
fi

# ---------------------------------------------------------------------------
# 3. No forbidden tracked path
# ---------------------------------------------------------------------------

tracked_count=0
forbidden_paths=()
forbidden_reasons=()

# -z plus 'read -d ""' is the only enumeration that survives spaces, newlines
# and non-ASCII bytes in a tracked path.
while IFS= read -r -d '' tracked_path; do
    tracked_count=$((tracked_count + 1))
    reason="$(forbidden_reason "${tracked_path}")"
    if [ -n "${reason}" ]; then
        forbidden_paths+=("${tracked_path}")
        forbidden_reasons+=("${reason}")
    fi
done < <(git -C "${repo_root}" ls-files -z)

if [ "${#forbidden_paths[@]}" -ne 0 ]; then
    printf 'ERROR: Git tracks %s forbidden path(s):\n' "${#forbidden_paths[@]}" >&2
    for index in "${!forbidden_paths[@]}"; do
        printf '  [%s] %s\n' "${forbidden_reasons[index]}" "${forbidden_paths[index]}" >&2
    done
    fail "Remove them from the index (git rm --cached) and keep them ignored."
fi

# ---------------------------------------------------------------------------
# 4. The ignore contract is really in force
# ---------------------------------------------------------------------------

not_ignored=()
for probe in "${MUST_BE_IGNORED[@]}"; do
    if ! git -C "${repo_root}" check-ignore -q --no-index -- "${probe}"; then
        not_ignored+=("${probe}")
    fi
done

if [ "${#not_ignored[@]}" -ne 0 ]; then
    printf 'ERROR: %s path(s) that must be ignored are not:\n' "${#not_ignored[@]}" >&2
    for probe in "${not_ignored[@]}"; do
        printf '  %s\n' "${probe}" >&2
    done
    fail "Fix .gitignore: these shapes would show up as untracked noise and could be committed by accident."
fi

wrongly_ignored=()
for probe in "${MUST_NOT_BE_IGNORED[@]}"; do
    if git -C "${repo_root}" check-ignore -q --no-index -- "${probe}"; then
        wrongly_ignored+=("${probe}")
    fi
done

if [ "${#wrongly_ignored[@]}" -ne 0 ]; then
    printf 'ERROR: %s required repository path(s) are ignored:\n' "${#wrongly_ignored[@]}" >&2
    for probe in "${wrongly_ignored[@]}"; do
        printf '  %s\n' "${probe}" >&2
    done
    fail "An over-broad .gitignore pattern hides repository configuration from Git."
fi

# ---------------------------------------------------------------------------
# 5. Summary
# ---------------------------------------------------------------------------

cat <<SUMMARY

Repository hygiene verification PASSED
  repository        : ${repo_root}
  tracked files     : ${tracked_count} (no IDE/agent/build/package junk)
  required config   : ${REQUIRED_CONFIG_FILES[*]} present and non-empty
  ignored probes    : ${#MUST_BE_IGNORED[@]} forbidden shapes confirmed ignored
  visible probes    : ${#MUST_NOT_BE_IGNORED[@]} required paths confirmed not ignored
SUMMARY
