# Copilot Instructions

## Start Here
- Initialize submodules before build: `git submodule update --init --recursive`.
- Main app: [DotnetPlayground.Web](../DotnetPlayground.Web). Additional modules: [InkBall](../InkBall), [IdentityManager2](../IdentityManager2), [Caching-MySQL](../Caching-MySQL).
- Canonical project overview and environment setup: [README.md](../README.md).

## High-Value Commands
- Build: `dotnet build` (or workspace task `build`).
- Test: `dotnet test` (or workspace task `test`).
- Lint JS: `npm run lint`.
- Asset pipeline: `npm install` (runs `postinstall`) then `npm run gulp` (or `bun x gulp`).
- CDN/SRI validation: `npm run sri:check`.
- CDN/SRI rewrite: `npm run sri:update`.
- E2E tests: `npm run test:playwright`.

## Routing And Path Rules
- App is mounted under `/dotnet`; do not introduce root-level routes by accident.
- Keep cookie paths and redirects aligned with `AppRootPath` in [DotnetPlayground.Web/appsettings.json](../DotnetPlayground.Web/appsettings.json).
- Request pipeline and endpoint mapping are under `app.Map("/dotnet", ...)` in [DotnetPlayground.Web/Startup.cs](../DotnetPlayground.Web/Startup.cs).

## Data And Providers
- Provider selection is driven by `DBKind` in config and wiring in [DotnetPlayground.Web/Helpers/ContextFactory.cs](../DotnetPlayground.Web/Helpers/ContextFactory.cs).
- `BloggingContext` (app + Identity) and InkBall game context are both configured there.
- Debug builds include all provider symbols; Release is MySQL-focused. Verify symbols in [DotnetPlayground.Web/DotnetPlayground.Web.csproj](../DotnetPlayground.Web/DotnetPlayground.Web.csproj) before adding provider-specific code.
- Distributed cache falls back to in-memory unless `DBKind` is SQL Server or MySQL.

## Tests And Seed Data
- In DEBUG, startup applies migrations and seeds Playwright users in [DotnetPlayground.Web/Helpers/ContextFactory.cs](../DotnetPlayground.Web/Helpers/ContextFactory.cs).
- Playwright config uses `https://localhost:4553/dotnet/` and launches the app via `dotnet run --project DotnetPlayground.Web/`; see [playwright.config.js](../playwright.config.js).
- Test user fixtures and storage states are in [e2e/TwoUsersFixtures.js](../e2e/TwoUsersFixtures.js) and [e2e/storageStates](../e2e/storageStates).
- If auth-state files are stale, delete `e2e/storageStates/*.json` and rerun Playwright.

## Assets And Frontend Workflow
- Bundles/translations are generated into both main app and InkBall module webroots via [gulpfile.mjs](../gulpfile.mjs).
- `dotnet publish` depends on the gulp production pipeline; do not skip asset generation when changing frontend files.
- For CDN package bumps in Razor/HTML, run SRI tooling in [tools/sri](../tools/sri) instead of manual edits.

## Pitfalls To Avoid
- Non-development environments rely on forwarded headers handling in [DotnetPlayground.Web/Startup.cs](../DotnetPlayground.Web/Startup.cs); keep proxy behavior intact.
- IdentityManager2 is mounted under `/dotnet/idm`; changes to auth/routing should verify this path still works.
- Background YouTube upload queue is optional and can no-op when secrets are absent; check config before debugging that path.
