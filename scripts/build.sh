#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="Release"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration|-c) CONFIGURATION="$2"; shift 2 ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

echo "Building solution (${CONFIGURATION})"

dotnet build Solution.slnx \
  --configuration "${CONFIGURATION}" \
  --nologo \
  -warnaserror

echo ""
echo "Build succeeded."
