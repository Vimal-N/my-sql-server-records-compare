#!/usr/bin/env bash
# Package Age Verification
# ========================
# Checks that all NuGet packages in Directory.Packages.props have been
# published for at least N days. Guards against supply chain attacks.
#
# Usage:
#   ./build/check-package-age.sh          # default: 10 days
#   ./build/check-package-age.sh 14       # custom: 14 days
#
# Bypass: Add entries to build/package-age-bypass.txt
#   Format: PackageId|Version|Reason

set -euo pipefail

MIN_DAYS=${1:-10}
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
PROPS_FILE="$ROOT_DIR/Directory.Packages.props"
BYPASS_FILE="$SCRIPT_DIR/package-age-bypass.txt"
FAILURES=0
CHECKED=0

echo "Package Age Check — minimum $MIN_DAYS days"
echo "=================================================="

if [ ! -f "$PROPS_FILE" ]; then
    echo "ERROR: Directory.Packages.props not found"
    exit 1
fi

# Load bypass entries into a flat string for lookup
BYPASS_LIST=""
if [ -f "$BYPASS_FILE" ]; then
    while IFS= read -r line || [ -n "$line" ]; do
        line=$(echo "$line" | sed 's/^[[:space:]]*//' | sed 's/[[:space:]]*$//')
        [[ "$line" =~ ^#.*$ ]] && continue
        [[ -z "$line" ]] && continue
        BYPASS_LIST="${BYPASS_LIST}${line}
"
    done < "$BYPASS_FILE"
fi

is_bypassed() {
    local pkg_id="$1"
    local pkg_version="$2"
    local search=$(echo "${pkg_id}|${pkg_version}" | tr '[:upper:]' '[:lower:]')
    echo "$BYPASS_LIST" | while IFS='|' read -r bid bver breason; do
        bid=$(echo "$bid" | tr '[:upper:]' '[:lower:]' | sed 's/^[[:space:]]*//' | sed 's/[[:space:]]*$//')
        bver=$(echo "$bver" | tr '[:upper:]' '[:lower:]' | sed 's/^[[:space:]]*//' | sed 's/[[:space:]]*$//')
        if [ "${bid}|${bver}" = "$search" ]; then
            echo "${breason:-no reason given}"
            return
        fi
    done
}

# Parse PackageVersion entries from Directory.Packages.props
while IFS= read -r line; do
    if echo "$line" | grep -q 'Include='; then
        PKG_ID=$(echo "$line" | sed 's/.*Include="\([^"]*\)".*/\1/')
        PKG_VERSION=$(echo "$line" | sed 's/.*Version="\([^"]*\)".*/\1/')
        CHECKED=$((CHECKED + 1))

        # Check bypass
        BYPASS_REASON=$(is_bypassed "$PKG_ID" "$PKG_VERSION")
        if [ -n "$BYPASS_REASON" ]; then
            printf "  \033[33mBYPASS\033[0m  %s %s — %s\n" "$PKG_ID" "$PKG_VERSION" "$BYPASS_REASON"
            continue
        fi

        # Query NuGet registration API
        PKG_LOWER=$(echo "$PKG_ID" | tr '[:upper:]' '[:lower:]')
        VER_LOWER=$(echo "$PKG_VERSION" | tr '[:upper:]' '[:lower:]')
        URL="https://api.nuget.org/v3/registration5-gz-semver2/${PKG_LOWER}/${VER_LOWER}.json"

        BODY=$(curl -s --compressed "$URL" 2>/dev/null || echo "")

        if [ -z "$BODY" ] || echo "$BODY" | grep -q '"statusCode"'; then
            printf "  \033[33mWARN\033[0m    %s %s — could not fetch metadata\n" "$PKG_ID" "$PKG_VERSION"
            continue
        fi

        # Extract published date
        PUBLISHED=$(echo "$BODY" | grep -o '"published":"[^"]*"' | head -1 | sed 's/"published":"//;s/"//')

        if [ -z "$PUBLISHED" ]; then
            printf "  \033[33mWARN\033[0m    %s %s — no publish date found\n" "$PKG_ID" "$PKG_VERSION"
            continue
        fi

        # Calculate age in days (handle both macOS and Linux date)
        PUBLISH_DATE="${PUBLISHED%%T*}"
        if date -j -f "%Y-%m-%d" "$PUBLISH_DATE" "+%s" >/dev/null 2>&1; then
            # macOS
            PUBLISH_EPOCH=$(date -j -f "%Y-%m-%d" "$PUBLISH_DATE" "+%s")
        else
            # Linux
            PUBLISH_EPOCH=$(date -d "$PUBLISH_DATE" "+%s")
        fi
        NOW_EPOCH=$(date "+%s")
        AGE_DAYS=$(( (NOW_EPOCH - PUBLISH_EPOCH) / 86400 ))

        if [ "$AGE_DAYS" -lt "$MIN_DAYS" ]; then
            printf "  \033[31mFAIL\033[0m    %s %s — published %d days ago (minimum: %d)\n" "$PKG_ID" "$PKG_VERSION" "$AGE_DAYS" "$MIN_DAYS"
            FAILURES=$((FAILURES + 1))
        else
            printf "  \033[32mOK\033[0m      %s %s — published %d days ago\n" "$PKG_ID" "$PKG_VERSION" "$AGE_DAYS"
        fi
    fi
done < <(grep "PackageVersion" "$PROPS_FILE")

echo "=================================================="

if [ "$FAILURES" -gt 0 ]; then
    printf "\033[31mFAILED: %d package(s) do not meet the %d-day age requirement.\033[0m\n" "$FAILURES" "$MIN_DAYS"
    echo ""
    echo "To bypass (e.g., urgent security fix), add to build/package-age-bypass.txt:"
    echo "  PackageId|Version|Reason"
    exit 1
else
    printf "\033[32mPASSED: All %d packages meet the %d-day age requirement.\033[0m\n" "$CHECKED" "$MIN_DAYS"
fi
