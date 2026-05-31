#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUTPUT="$ROOT/artifacts/coverage"
REPORT="$OUTPUT/report"

rm -rf "$OUTPUT"
mkdir -p "$OUTPUT"

echo "=== Collecting unit test coverage ==="
dotnet coverage collect \
    --settings "$ROOT/scripts/coverage.config" \
    --output "$OUTPUT/unit-tests.coverage" \
    dotnet test "$ROOT/tests/McpServer.Unit.Tests"

echo "=== Collecting integration test coverage ==="
dotnet coverage collect \
    --settings "$ROOT/scripts/coverage.config" \
    --output "$OUTPUT/integration-tests.coverage" \
    dotnet test "$ROOT/tests/McpServer.Integration.Tests"

echo "=== Merging coverage reports ==="
dotnet coverage merge \
    --output "$OUTPUT/merged.coverage" \
    "$OUTPUT/unit-tests.coverage" \
    "$OUTPUT/integration-tests.coverage"

echo "=== Generating coverage report ==="
dotnet coverage report \
    --output "$REPORT" \
    --report-formats html \
    "$OUTPUT/merged.coverage"

echo ""
echo "✓ Coverage report: $REPORT/index.html"
