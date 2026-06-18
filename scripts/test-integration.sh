#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="Release"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration|-c) CONFIGURATION="$2"; shift 2 ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

echo "Running integration tests (${CONFIGURATION})"

dotnet test tests/McpServer.Integration.Tests \
  --configuration "${CONFIGURATION}" \
  -- \
  --no-progress \
  --ignore-exit-code "5;8"

echo ""
echo "Integration tests passed."
