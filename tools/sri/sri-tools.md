# SRI Tools Guide

This document explains how to use the local SRI tooling added to this repository.

## Purpose

The SRI checker keeps CDN references safe and consistent by validating:

- Integrity hash values in script and link tags
- CDN URL version tokens against package.json versions
- Reachability of each CDN URL

It helps prevent drift when dependency versions change.

## What is scanned

The checker scans these roots:

- DotnetPlayground.Web
- InkBall/src

Supported file extensions:

- .cshtml
- .razor
- .html
- .htm

Only HTTP/HTTPS tags with an integrity attribute are validated.

## Commands

Run from repository root.

Node 20+ or Bun 1.3+ is recommended.

### Validate

```bash
npm run sri:check
```

```bash
bun run sri:check:bun
```

Behavior:

- Exits with code 0 on success
- Exits with code 2 for validation failures (mapping/version/hash)
- Exits with code 3 for network failures

### Update integrity values

```bash
npm run sri:update
```

```bash
bun run sri:update:bun
```

Behavior:

- Recomputes integrity from CDN bytes
- Replaces only integrity attribute values in-place
- Keeps other tag attributes/order unchanged

### Run parser/unit tests

```bash
npm run sri:test
```

```bash
bun run sri:test:bun
```

## Targeted modes

### Update only selected packages

```bash
node tools/sri/sri-check.mjs --update --only bootstrap-table,i18next
```

```bash
bun tools/sri/sri-check.mjs --update --only bootstrap-table,i18next
```

### Update only dependencies changed from HEAD package.json

```bash
node tools/sri/sri-check.mjs --update --changed
```

```bash
bun tools/sri/sri-check.mjs --update --changed
```

### Allow network failures locally

```bash
node tools/sri/sri-check.mjs --allow-network-failures
```

```bash
bun tools/sri/sri-check.mjs --allow-network-failures
```

Use this only for local troubleshooting. CI should run without this flag.

## Output categories

The checker groups findings by severity:

- Missing mapping
  - CDN package name could not be mapped to package.json key
- Version mismatch
  - URL version does not match package.json version
- Hash mismatch
  - Computed hash does not match declared integrity value
- Unreachable URL
  - URL fetch failed after retry

## Retry policy

CDN fetches use 3 attempts with exponential backoff:

- 500 ms
- 1000 ms
- 2000 ms

## Typical workflows

### After dependency bumps

1. Update package versions
2. Run npm run sri:check
3. If hashes mismatch, run npm run sri:update
4. Re-run npm run sri:check
5. Commit the integrity updates

### Before opening a PR

1. Run npm run sri:test
2. Run npm run sri:check

## CI integration

Main workflow runs the checker before dotnet build/test:

- setup-node
- npm ci
- npm run sri:check

This fails fast on SRI drift.

## Tool files

- tools/sri/sri-check.mjs
- tools/sri/sri-lib.mjs
- tools/sri/sri-lib.test.mjs

## Notes about Razor scoped packages

Razor can escape scoped package names using @@ in markup (for example @@microsoft/signalr). The checker handles this correctly:

- Keeps source URLs unchanged in files
- Normalizes fetch URL internally to validate content/hash
