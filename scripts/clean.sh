#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"

echo "Cleaning build artifacts"

rm -rf "${REPO_ROOT}/artifacts"
dotnet clean Solution.slnx --nologo --verbosity quiet 2>/dev/null || true

echo "Clean complete."
