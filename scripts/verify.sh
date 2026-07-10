#!/bin/bash
# Deterministic verification gate — the final vote on any change.
# Green here = done; anything else = not done. No self-assessment overrides this.
set -euo pipefail
cd "$(dirname "$0")/.."

echo "[verify] 1/4 backend build"
dotnet build --configuration Release --no-incremental

echo "[verify] 2/4 format/lint gates"
dotnet format whitespace --verify-no-changes --no-restore
dotnet format style --verify-no-changes --severity info --no-restore
(cd web && pnpm run lint)

echo "[verify] 3/4 backend tests (same as CI)"
dotnet test --configuration Release --no-build

echo "[verify] 4/4 frontend build"
(cd web && pnpm run build)

echo "[verify] ALL GREEN"
