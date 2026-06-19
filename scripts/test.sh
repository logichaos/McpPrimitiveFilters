#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="Release"
FAILED=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration|-c) CONFIGURATION="$2"; shift 2 ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

echo "Running all tests (${CONFIGURATION})"

echo ""
echo "Unit tests..."
if dotnet test \
  tests/McpPrimitiveFilters.Unit.Tests \
  tests/McpServer.Unit.Tests \
  --configuration "${CONFIGURATION}" \
  -- \
  --no-progress \
  --ignore-exit-code "5;8"; then
  echo "  Unit tests passed."
else
  echo "  Unit tests FAILED."
  FAILED=1
fi

echo ""
echo "Integration tests..."
if dotnet test tests/McpServer.Integration.Tests \
  --configuration "${CONFIGURATION}" \
  -- \
  --no-progress \
  --ignore-exit-code "5;8"; then
  echo "  Integration tests passed."
else
  echo "  Integration tests FAILED."
  FAILED=1
fi

echo ""
if [[ $FAILED -eq 0 ]]; then
  echo "All tests passed."
else
  echo "One or more test suites failed."
  exit 1
fi
