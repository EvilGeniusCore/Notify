#!/usr/bin/env bash
# teams-notify smoke test
# Loads credentials from test.env, then runs three checks:
#   1. list       — verifies auth and read permissions
#   2. send --dry-run — verifies team/channel resolution without posting
#   3. send       — delivers a real test message to the configured channel

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$ROOT/test.env"
BINARY="$ROOT/teams-notify"

# ── Preflight ──────────────────────────────────────────────────────────────────

if [[ ! -f "$ENV_FILE" ]]; then
    echo "ERROR: test.env not found." >&2
    echo "Copy test.env.template to test.env and fill in the values." >&2
    exit 1
fi

if [[ ! -f "$BINARY" ]]; then
    echo "ERROR: teams-notify binary not found in this folder." >&2
    echo "Download the release zip for your platform from the releases page," >&2
    echo "extract the binary here, run 'chmod +x teams-notify', then re-run this script." >&2
    exit 1
fi

if [[ ! -x "$BINARY" ]]; then
    echo "ERROR: teams-notify is not executable. Run: chmod +x teams-notify" >&2
    exit 1
fi

# ── Load test.env ──────────────────────────────────────────────────────────────

while IFS='=' read -r key value; do
    key="${key%%$'\r'}"
    key="${key#"${key%%[![:space:]]*}"}"
    [[ -z "$key" || "$key" == \#* ]] && continue
    value="${value%%$'\r'}"
    value="${value#"${value%%[![:space:]]*}"}"
    [[ -z "$value" ]] && continue
    export "$key=$value"
done < "$ENV_FILE"

# ── Helpers ────────────────────────────────────────────────────────────────────

PASS=0
FAIL=0

run_test() {
    local label="$1"
    shift
    echo ""
    echo "[$label]"
    if "$@"; then
        echo "  PASS"
        PASS=$((PASS + 1))
    else
        local code=$?
        echo "  FAIL — exit code $code"
        FAIL=$((FAIL + 1))
        exit $code
    fi
}

# ── Tests ──────────────────────────────────────────────────────────────────────

echo "teams-notify smoke test"
echo "Binary: $BINARY"
echo "Env:    $ENV_FILE"

run_test "1. List teams (auth + read permissions)" \
    "$BINARY" list

run_test "2. Send dry-run (team/channel resolution)" \
    "$BINARY" send --message "teams-notify dry-run test" --dry-run

HOSTNAME_VAL="$(hostname)"
TIMESTAMP="$(date '+%Y-%m-%d %H:%M:%S')"

run_test "3. Send real message" \
    "$BINARY" send --message "teams-notify smoke test — $HOSTNAME_VAL — $TIMESTAMP"

# ── Summary ────────────────────────────────────────────────────────────────────

echo ""
if [[ $FAIL -eq 0 ]]; then
    echo "Results: $PASS passed, $FAIL failed"
else
    echo "Results: $PASS passed, $FAIL failed" >&2
    exit 1
fi
