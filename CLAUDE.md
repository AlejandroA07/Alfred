@AGENTS.md

## Claude-specific notes

- Use the project skills when they apply: `migrate` (EF Core migrations per module), `db` (read-only Postgres inspection), `security-checklist` (endpoint review), `retro` (weekly harness compost).
- Use the globally installed planning skills explicitly when requested, including `$wayfinder` for multi-session decision maps.
- Before finishing any change, run the `node scripts/verify.mjs` gate from `AGENTS.md`; use `code-review` and `security-checklist` when their scopes apply.
- The app can be driven end-to-end via the Playwright MCP server once it's running locally.
