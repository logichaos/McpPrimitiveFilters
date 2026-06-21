#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="Release"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration|-c) CONFIGURATION="$2"; shift 2 ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

PROJECT="src/McpPrimitiveFilters/McpPrimitiveFilters.csproj"
OUTPUT_DIR="artifacts/package/release"

VERSION=$(git describe --tags --abbrev=0 2>/dev/null | sed 's/^v//')
if [[ -z "${VERSION}" ]]; then
  echo "Error: no git tag found to derive package version." >&2
  exit 1
fi

echo "Building NuGet package (${CONFIGURATION})"
echo "Project: ${PROJECT}"
echo "Version: ${VERSION}"

dotnet pack "${PROJECT}" \
  --configuration "${CONFIGURATION}" \
  --nologo \
  -p:PackageVersion="${VERSION}"

echo ""
echo "Package created."

PACKAGE_PATH=$(ls -1t "${OUTPUT_DIR}"/*.nupkg 2>/dev/null | head -1)
if [[ -n "${PACKAGE_PATH}" ]]; then
  echo "  $(basename "${PACKAGE_PATH}")"
fi
