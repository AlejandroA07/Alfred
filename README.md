# Alfred

Personal life-admin app: finances, reminders, purchases, calendar and more — with an assistant layer on top.

## Stack

- **Backend:** ASP.NET Core (.NET 10), modular monolith, EF Core + PostgreSQL
- **Frontend:** React 19 + TypeScript + Vite, installable PWA
- **Infrastructure:** Docker Compose (PostgreSQL 17)

## Getting started

```bash
# 1. Database
docker compose up -d

# 2. API (http://localhost:5037 — applies migrations on startup in Development)
dotnet run --project src/Alfred.Api --launch-profile http

# 3. Frontend dev server (http://localhost:5173, proxies /api to the API)
cd web && pnpm install && pnpm run dev
```

## Development

```bash
dotnet build                      # backend build
dotnet test                       # backend tests (incl. architecture tests)
dotnet format                     # code formatting
cd web && pnpm run lint            # frontend lint
cd web && pnpm run build           # frontend production build
scripts/verify.sh                 # everything CI checks, in one command
```

The solution uses NuGet lock files (`--locked-mode` in CI) and central package management (`Directory.Packages.props`). The frontend uses **pnpm** (v11): dependency install scripts are blocked by default and `minimumReleaseAge` (3 days) refuses freshly published package versions as supply-chain protection.

## Structure

```
src/Alfred.Api                  API host (endpoints, SPA serving, composition)
src/Alfred.SharedKernel         shared abstractions
src/Alfred.Modules.*            domain modules (module boundaries enforced by tests)
tests/Alfred.Tests              xUnit test suite
web/                            React PWA
```
