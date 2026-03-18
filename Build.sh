#!/usr/bin/env bash
# Build.sh — publish notify for one or more target profiles
# Run from the solution root (Teams-Notify/).
#
# Usage:
#   ./Build.sh                           # all profiles
#   ./Build.sh win-x64                   # single profile
#   ./Build.sh win-x64 linux-x64         # multiple profiles

set -euo pipefail

SOLUTION_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/"
PROJECT_PATH="src/Notify/Notify.csproj"

DEFAULT_PROFILES=(
    "win-x64"
    "linux-x64"
    "linux-arm64"
    "osx-x64"
    "osx-arm64"
)

if [ "$#" -gt 0 ]; then
    TARGET_PROFILES=("$@")
else
    TARGET_PROFILES=("${DEFAULT_PROFILES[@]}")
fi

FAILED=()

for profile in "${TARGET_PROFILES[@]}"; do
    out_dir="${SOLUTION_DIR}artifacts/${profile}"
    if [ -d "$out_dir" ]; then
        echo "    Cleaning $out_dir ..."
        rm -rf "$out_dir"
    fi

    echo ""
    echo "==> Publishing $profile ..."
    if dotnet publish "$PROJECT_PATH" -p:PublishProfile="$profile" -p:SolutionDir="$SOLUTION_DIR"; then
        echo "OK: $profile"
    else
        echo "FAILED: $profile"
        FAILED+=("$profile")
    fi
done

echo ""
if [ "${#FAILED[@]}" -eq 0 ]; then
    echo "All profiles built successfully."
else
    echo "Failed profiles: ${FAILED[*]}"
    exit 1
fi
