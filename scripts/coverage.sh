#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="Release"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUTPUT="$ROOT/artifacts/coverage"
REPORT="$OUTPUT/report"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration|-c) CONFIGURATION="$2"; shift 2 ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

rm -rf "$OUTPUT"
mkdir -p "$OUTPUT"

echo "Collecting unit test coverage (${CONFIGURATION})"
dotnet coverage collect \
    --settings "$ROOT/scripts/coverage.config" \
    --output "$OUTPUT/unit-tests.coverage" \
    dotnet test \
      "$ROOT/tests/McpPrimitiveFilters.Unit.Tests" \
      "$ROOT/tests/McpServer.Unit.Tests" \
      --configuration "${CONFIGURATION}"

echo ""
echo "Collecting integration test coverage"
dotnet coverage collect \
    --settings "$ROOT/scripts/coverage.config" \
    --output "$OUTPUT/integration-tests.coverage" \
    dotnet test "$ROOT/tests/McpServer.Integration.Tests" \
      --configuration "${CONFIGURATION}"

echo ""
echo "Merging coverage reports"
dotnet coverage merge \
    --output "$OUTPUT/merged.coverage" \
    "$OUTPUT/unit-tests.coverage" \
    "$OUTPUT/integration-tests.coverage"

echo ""
echo "Generating coverage report"
dotnet coverage report \
    --output "$REPORT" \
    --report-formats html \
    "$OUTPUT/merged.coverage"

echo ""
echo "Coverage report: $REPORT/index.html"
