#!/usr/bin/env bash
# ============================================================
# E2E Test Environment Setup
# Single command to: start Docker SQL, create DB, seed data
#
# Usage:
#   ./tests/e2e/setup-e2e.sh          # start fresh
#   ./tests/e2e/setup-e2e.sh --reset  # tear down and restart
#   ./tests/e2e/setup-e2e.sh --down   # tear down only
# ============================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
COMPOSE_FILE="$SCRIPT_DIR/docker-compose.yml"
CONTAINER="mssql-e2e-test"
SA_PASSWORD="YourStrong@Passw0rd1"
CONFIG_PATH="$SCRIPT_DIR/TestCompareConfig.xlsx"

RED='\033[0;31m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
DIM='\033[2m'
RESET='\033[0m'

log()  { echo -e "${CYAN}▸${RESET} $1"; }
ok()   { echo -e "${GREEN}✓${RESET} $1"; }
fail() { echo -e "${RED}✗${RESET} $1"; exit 1; }
dim()  { echo -e "${DIM}  $1${RESET}"; }

run_sql() {
    docker exec "$CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$SA_PASSWORD" -C -b \
        -i "/e2e/$1" 2>&1
}

# --down: tear down only
if [[ "${1:-}" == "--down" ]]; then
    log "Stopping E2E environment..."
    docker compose -f "$COMPOSE_FILE" down -v 2>/dev/null || true
    ok "Environment stopped"
    exit 0
fi

# --reset: tear down then continue to setup
if [[ "${1:-}" == "--reset" ]]; then
    log "Resetting E2E environment..."
    docker compose -f "$COMPOSE_FILE" down -v 2>/dev/null || true
    ok "Old environment removed"
fi

echo ""
echo -e "${CYAN}═══════════════════════════════════════════════${RESET}"
echo -e "${CYAN}  MsSqlRecordsCompare — E2E Test Setup${RESET}"
echo -e "${CYAN}═══════════════════════════════════════════════${RESET}"
echo ""

# Step 0: Build project and generate test config workbook
log "Building project..."
BUILD_OUTPUT=$(dotnet build "$PROJECT_ROOT" --nologo 2>&1)
if echo "$BUILD_OUTPUT" | grep -q "Build succeeded"; then
    ok "Project built"
else
    echo "$BUILD_OUTPUT" | grep -iE "(error |FAILED)" | grep -v "NU1900"
    fail "Build failed"
fi

log "Generating test config workbook..."
dotnet test "$PROJECT_ROOT" \
    --filter "FullyQualifiedName=MsSqlRecordsCompare.Tests.E2E.E2EConfigGenerator.GenerateTestConfigWorkbook" \
    --no-build --nologo 2>&1 | grep -E "(Passed|Failed)" | head -1
if [[ -f "$CONFIG_PATH" ]]; then
    ok "TestCompareConfig.xlsx generated"
else
    fail "Failed to generate TestCompareConfig.xlsx"
fi

# Step 1: Start Docker container
log "Starting SQL Server 2022 container..."
docker compose -f "$COMPOSE_FILE" up -d 2>&1 || true

# Step 2: Wait for SQL Server to be ready
log "Waiting for SQL Server to be ready..."
RETRIES=30
until docker exec "$CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" -C -Q "SELECT 1" -b &>/dev/null; do
    RETRIES=$((RETRIES - 1))
    if [[ $RETRIES -le 0 ]]; then
        fail "SQL Server did not become ready in time"
    fi
    sleep 2
    dim "waiting... ($RETRIES attempts remaining)"
done
ok "SQL Server is ready"

# Step 3: Create schema
log "Creating database schema..."
OUTPUT=$(run_sql "01-schema.sql")
if echo "$OUTPUT" | grep -q "Schema created successfully"; then
    ok "Database TestCompareDB created with 7 tables"
else
    echo "$OUTPUT"
    fail "Schema creation failed"
fi

# Step 4: Seed scenarios
for SCRIPT in \
    "02-seed-scenario1-perfect-match.sql" \
    "02-seed-scenario2-known-mismatches.sql" \
    "02-seed-scenario3-row-count-diff.sql" \
    "02-seed-scenario4-null-handling.sql" \
    "02-seed-scenario5-complex-queries-dupes.sql"; do

    SCENARIO=$(echo "$SCRIPT" | sed 's/02-seed-//' | sed 's/\.sql//' | tr '-' ' ')
    log "Seeding: $SCENARIO..."
    OUTPUT=$(run_sql "$SCRIPT")
    if echo "$OUTPUT" | grep -qi "seeded"; then
        ok "Seeded: $SCENARIO"
    else
        echo "$OUTPUT"
        fail "Seeding failed: $SCRIPT"
    fi
done

# Step 5: Verify
log "Verifying row counts..."
VERIFY_OUTPUT=$(run_sql "03-verify.sql")
TOTAL_ROWS=$(echo "$VERIFY_OUTPUT" | grep -c "OLD-\|NEW-" || true)
if [[ "$TOTAL_ROWS" -gt 0 ]]; then
    ok "Verification complete ($TOTAL_ROWS record entries across all tables)"
else
    echo "$VERIFY_OUTPUT"
    fail "Verification returned no data"
fi

# Summary
echo ""
echo -e "${CYAN}═══════════════════════════════════════════════${RESET}"
echo -e "${GREEN}  E2E environment is ready!${RESET}"
echo -e "${CYAN}═══════════════════════════════════════════════${RESET}"
echo ""
echo "  Server:   localhost,11433"
echo "  User:     sa"
echo "  Password: $SA_PASSWORD"
echo "  Database: TestCompareDB"
echo ""
echo "  Run all scenarios:"
echo -e "  ${DIM}dotnet run --project src/MsSqlRecordsCompare.CLI -- \\"
echo "    --config tests/e2e/TestCompareConfig.xlsx \\"
echo "    --table-set TestOrders \\"
echo "    --server \"localhost,11433\" \\"
echo "    --user sa \\"
echo -e "    --password \"$SA_PASSWORD\"${RESET}"
echo ""
echo "  Run single scenario:"
echo -e "  ${DIM}dotnet run --project src/MsSqlRecordsCompare.CLI -- \\"
echo "    --config tests/e2e/TestCompareConfig.xlsx \\"
echo "    --table-set TestOrders \\"
echo "    --scenario Perfect-Match \\"
echo "    --server \"localhost,11433\" \\"
echo "    --user sa \\"
echo -e "    --password \"$SA_PASSWORD\"${RESET}"
echo ""
echo "  Tear down:"
echo -e "  ${DIM}./tests/e2e/setup-e2e.sh --down${RESET}"
echo ""
