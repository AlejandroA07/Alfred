#!/bin/bash
# Deterministic verification gate — the final vote on any change.
# Green here = done; anything else = not done. No self-assessment overrides this.
set -euo pipefail
cd "$(dirname "$0")/.."

echo "[verify] 1/5 restore locked dependencies (same as CI)"
dotnet restore --locked-mode

echo "[verify] 2/5 backend build"
dotnet build --configuration Release --no-incremental --no-restore

echo "[verify] 3/5 format/lint gates"
dotnet format whitespace --verify-no-changes --no-restore
dotnet format style --verify-no-changes --severity info --no-restore
(cd web && pnpm run lint)

echo "[verify] 4/5 backend tests (same as CI)"
dotnet test --configuration Release --no-build

echo "[verify] 5/5 frontend build"
(cd web && pnpm run build)

echo "[verify] ALL GREEN"
