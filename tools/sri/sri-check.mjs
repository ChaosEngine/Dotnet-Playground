#!/usr/bin/env node

/* eslint-disable no-console */

import fs from "node:fs";
import path from "node:path";
import process from "node:process";

import {
  normalizeVersionSpec,
  extractCdnMetadata,
  mapPackageName,
  collectRazorIntegrityEntries,
  parseIntegrityTokens,
  computeIntegrity,
  fetchBytesWithRetry,
  loadPackageVersionMap,
  detectChangedPackagesFromHead,
  substituteUrlVersion,
  updateIntegrityInContent
} from "./sri-lib.mjs";

const EXIT_OK = 0, EXIT_VALIDATION = 2, EXIT_NETWORK = 3;

function parseArgs(argv) {
  const options = {
    update: false,
    allowNetworkFailures: false,
    onlyPackages: new Set(),
    changedOnly: false
  };

  for (let index = 0; index < argv.length; index += 1) {
    const arg = argv[index];

    if (arg === "--update") {
      options.update = true;
      continue;
    }

    if (arg === "--allow-network-failures") {
      options.allowNetworkFailures = true;
      continue;
    }

    if (arg === "--changed") {
      options.changedOnly = true;
      continue;
    }

    if (arg === "--only") {
      const next = argv[index + 1];
      if (!next) {
        throw new Error("Expected value after --only");
      }

      for (const packageName of next.split(",")) {
        const trimmed = packageName.trim();
        if (trimmed) {
          options.onlyPackages.add(trimmed);
        }
      }

      index += 1;
      continue;
    }

    throw new Error(`Unknown argument: ${arg}`);
  }

  return options;
}

function formatLocation(entry) {
  return `${entry.relativePath}:${entry.line}`;
}

function normalizeUrlForFetch(url) {
  // Razor escapes '@' as '@@' in attributes; CDNs expect a single '@'.
  return String(url).replace("/npm/@@", "/npm/@");
}

function printGroup(title, items) {
  if (items.length === 0) {
    return;
  }

  console.log(`\n${title} (${items.length})`);
  for (const item of items) {
    console.log(`- ${item}`);
  }
}

async function main() {
  const repoRoot = process.cwd();
  const args = parseArgs(process.argv.slice(2));
  const packageVersions = loadPackageVersionMap(repoRoot);
  const entries = collectRazorIntegrityEntries(repoRoot);

  const changedPackages = args.changedOnly ? detectChangedPackagesFromHead(repoRoot) : null;
  if (args.changedOnly && !changedPackages) {
    console.warn("Warning: unable to determine changed packages from HEAD, scanning all references.");
  }

  const findings = {
    missingMapping: [],
    versionMismatch: [],
    hashMismatch: [],
    unreachable: []
  };

  const updatesByFile = new Map();
  const downloadCache = new Map();
  const filteredEntries = [];

  for (const entry of entries) {
    const metadata = extractCdnMetadata(entry.url);
    const mappedPackage = mapPackageName(metadata.packageName);

    entry.metadata = metadata;
    entry.packageName = mappedPackage;

    if (args.onlyPackages.size > 0 && (!mappedPackage || !args.onlyPackages.has(mappedPackage))) {
      continue;
    }

    if (changedPackages && changedPackages.size > 0 && mappedPackage && !changedPackages.has(mappedPackage)) {
      continue;
    }

    filteredEntries.push(entry);
  }

  for (const entry of filteredEntries) {
    const location = formatLocation(entry);
    const packageName = entry.packageName;

    if (!packageName) {
      findings.missingMapping.push(`${location} -> ${entry.url}`);
      continue;
    }

    const declaredVersion = normalizeVersionSpec(packageVersions[packageName]);
    const urlVersion = entry.metadata.urlVersion;
    if (!declaredVersion) {
      findings.missingMapping.push(`${location} -> ${packageName} missing from package.json`);
      continue;
    }

    const hasVersionMismatch = urlVersion && declaredVersion !== urlVersion;

    if (hasVersionMismatch && args.update) {
      // Fetch the new-version URL (from package.json), not the stale one in the file.
      const newUrl = substituteUrlVersion(entry.url, urlVersion, declaredVersion);
      const targetFetchUrl = normalizeUrlForFetch(newUrl);

      if (!downloadCache.has(targetFetchUrl)) {
        downloadCache.set(targetFetchUrl, { status: "pending" });
        try {
          const buffer = await fetchBytesWithRetry(targetFetchUrl, 3);
          downloadCache.set(targetFetchUrl, { status: "ok", buffer });
        } catch (error) {
          downloadCache.set(targetFetchUrl, { status: "error", message: error.message || String(error) });
        }
      }

      const result = downloadCache.get(targetFetchUrl);
      if (result.status === "error") {
        findings.unreachable.push(`${location} -> ${newUrl} (${result.message})`);
        continue;
      }

      const tokens = parseIntegrityTokens(entry.integrity);
      const algorithm = tokens.length > 0 ? tokens[0].algorithm : "sha256";
      const newIntegrity = computeIntegrity(result.buffer, algorithm);

      const updates = updatesByFile.get(entry.relativePath) || [];
      updates.push({ tagStart: entry.tagStart, tagEnd: entry.tagEnd, newIntegrity, newUrl });
      updatesByFile.set(entry.relativePath, updates);
    } else {
      if (hasVersionMismatch) {
        findings.versionMismatch.push(`${location} -> ${packageName} package.json=${declaredVersion}, url=${urlVersion}`);
      }

      const fetchUrl = normalizeUrlForFetch(entry.url);
      if (!downloadCache.has(fetchUrl)) {
        downloadCache.set(fetchUrl, { status: "pending" });
        try {
          const buffer = await fetchBytesWithRetry(fetchUrl, 3);
          downloadCache.set(fetchUrl, { status: "ok", buffer });
        } catch (error) {
          downloadCache.set(fetchUrl, { status: "error", message: error.message || String(error) });
        }
      }

      const result = downloadCache.get(fetchUrl);
      if (result.status === "error") {
        findings.unreachable.push(`${location} -> ${entry.url} (${result.message})`);
        continue;
      }

      const tokens = parseIntegrityTokens(entry.integrity);
      if (tokens.length === 0) {
        findings.hashMismatch.push(`${location} -> malformed integrity value '${entry.integrity}'`);
        continue;
      }

      const tokenMatches = tokens.some((token) => {
        const calculated = computeIntegrity(result.buffer, token.algorithm);
        return calculated === token.raw;
      });

      if (!tokenMatches) {
        const preferredAlgorithm = tokens[0].algorithm;
        const expectedIntegrity = computeIntegrity(result.buffer, preferredAlgorithm);
        findings.hashMismatch.push(`${location} -> expected ${expectedIntegrity}, found ${entry.integrity}`);

        if (args.update) {
          const updates = updatesByFile.get(entry.relativePath) || [];
          updates.push({ tagStart: entry.tagStart, tagEnd: entry.tagEnd, newIntegrity: expectedIntegrity });
          updatesByFile.set(entry.relativePath, updates);
        }
      }
    }
  }

  if (args.update && updatesByFile.size > 0) {
    for (const [relativePath, updates] of updatesByFile.entries()) {
      const absolutePath = path.join(repoRoot, relativePath);
      const content = fs.readFileSync(absolutePath, "utf8");
      const updatedContent = updateIntegrityInContent(content, updates);
      fs.writeFileSync(absolutePath, updatedContent, "utf8");
    }
  }

  console.log(`Scanned entries: ${filteredEntries.length}`);
  printGroup("Missing mapping", findings.missingMapping);
  printGroup("Version mismatch", findings.versionMismatch);
  printGroup("Hash mismatch", findings.hashMismatch);
  printGroup("Unreachable URL", findings.unreachable);

  const hasValidationFailures = findings.missingMapping.length > 0 || findings.versionMismatch.length > 0 || findings.hashMismatch.length > 0;
  const hasNetworkFailures = findings.unreachable.length > 0;

  if (hasValidationFailures) {
    process.exitCode = EXIT_VALIDATION;
    return;
  }

  if (hasNetworkFailures && !args.allowNetworkFailures) {
    process.exitCode = EXIT_NETWORK;
    return;
  }

  if (hasNetworkFailures && args.allowNetworkFailures) {
    console.warn("Warning: network failures were downgraded by --allow-network-failures.");
  }

  process.exitCode = EXIT_OK;
}

main().catch((error) => {
  console.error(error.message || error);
  process.exitCode = 1;
});
