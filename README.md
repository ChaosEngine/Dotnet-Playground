# .NET Playground

[![Build & Test](https://github.com/ChaosEngine/Dotnet-Playground/workflows/build%20and%20test/badge.svg)](https://github.com/ChaosEngine/Dotnet-Playground/actions)

An ASP.NET Core web application showcasing modern .NET patterns, integrations, and feature examples. Hosted live at https://haos.hopto.org/dotnet/

## Features

- **InkBall Game** — Real-time multiplayer drawing game using SignalR with WebSocket support
- **Blogging System** — Create, edit, and manage blog posts and comments
- **Hash Generator** — Compute and search MD5/SHA256 hashes with caching
- **Identity Management** — User authentication with ASP.NET Core Identity + OAuth providers (Google, Facebook, GitHub, Twitter)
- **Admin UI** — IdentityManager2 integration for user/role management at `/dotnet/idm`
- **Multi-Database Support** — Switch between SQLite, SQL Server, PostgreSQL, MySQL, Oracle, or MongoDB via configuration
- **Distributed Caching** — SQL Server or MySQL session cache backends
- **Background Tasks** — YouTube upload integration with file watching
- **End-to-End Tests** — Playwright test suite with pre-configured test users
- **Asset Pipeline** — Gulp-based compilation for JS/CSS/translations with webpack bundling

## Quick Start

### Prerequisites
- .NET SDK
- Node.js (bun/pnpm/npm) for asset pipeline
- Docker (optional, for database services)

### Setup

```bash
# Clone with submodules (InkBall, IdentityManager2, Caching-MySQL)
git clone https://github.com/ChaosEngine/Dotnet-Playground.git
cd Dotnet-Playground
git submodule update --init --recursive

# Restore .NET dependencies
dotnet restore

# Install Node dependencies
bun install  # or: npm install / pnpm install

# Build assets
bun x gulp  # or: npm run gulp

# Run the app (listens on https://localhost:4553/dotnet/)
dotnet run --project DotnetPlayground.Web
```

### Run Tests

```bash
# Unit and integration tests
dotnet test

# Playwright e2e tests (requires app running)
cd e2e
npm run test  # or: bunx playwright test
```

## Configuration

Edit `DotnetPlayground.Web/appsettings.json` or use user-secrets:

```bash
dotnet user-secrets set "DBKind" "sqlite"  # sqlite|sqlserver|postgres|mysql|oracle|mongodb
dotnet user-secrets set "Authentication:Google:ClientId" "your-client-id"
```

**Key Settings:**
- `AppRootPath` — All routes run under `/dotnet/` path
- `DBKind` — Database provider (default: sqlite)
- `Authentication:*` — OAuth credentials for external login providers
- `YouTubeAPI` — YouTube upload integration (optional)

## Project Structure

```
├── DotnetPlayground.Web/       # Main ASP.NET Core app
├── DotnetPlayground.Tests/     # Unit & integration tests
├── e2e/                        # Playwright end-to-end tests
├── InkBall/                    # Game module (submodule)
├── IdentityManager2/           # Identity admin UI (submodule)
├── Caching-MySQL/              # MySQL distributed cache (submodule)
└── gulpfile.mjs                # Asset build pipeline
```

## Architecture Highlights

- **Database Contexts:** Two DbContexts (`BloggingContext` + `GamesContext`) with auto-migration on DEBUG startup
- **Provider Abstraction:** `ContextFactory` wires up database/cache based on `DBKind` build constant
- **Modular Design:** InkBall game, IdentityManager2 admin, and Caching-MySQL are git submodules
- **SignalR Integration:** Real-time game updates via MessagePack or JSON protocol
- **Background Tasks:** `BackgroundTaskQueue` service for async operations (video processing, etc.)
- **Path Isolation:** All middleware and routes scoped under `/dotnet` via `app.Map()` for multi-tenancy readiness

## Development

### Build Configurations
- **Debug** — All database providers enabled (INCLUDE_ORACLE, INCLUDE_MONGODB, INCLUDE_SQLSERVER, INCLUDE_POSTGRES, INCLUDE_MYSQL)
- **Release** — MySQL only (production-optimized)
- **Oracle** — Oracle provider only

### Asset Pipeline
Uses Gulp + Webpack for bundling JavaScript (including workers) and SCSS compilation. Run after any JS/CSS changes:

```bash
bun x gulp
```

### Test Users (Debug Only)
Seeded automatically in DEBUG builds:
- `Playwright1@test.domain.com` / `Playwright1!`
- `Playwright2@test.domain.com` / `Playwright2!`

## Deployment

```bash
# Publish (triggers gulp build internally)
dotnet publish -c Release -r win-x64 --self-contained

# Or use Docker
docker build -t dotnet-playground .
docker-compose up
```

## License

MIT — See LICENSE file for details

## Contributing

Issues and pull requests welcome! Please refer to [.github/copilot-instructions.md](.github/copilot-instructions.md) for development guidelines.