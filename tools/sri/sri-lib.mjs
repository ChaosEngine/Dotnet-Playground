import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import { Buffer } from "node:buffer";
import { execSync } from "node:child_process";

export const SCAN_EXTENSIONS = new Set([".cshtml", ".razor", ".html", ".htm"]);
export const DEFAULT_SCAN_ROOTS = [
  "DotnetPlayground.Web",
  "InkBall/src"
];
export const CDN_ALIASES = {
  qrcode_js: "qrcodejs"
};

/**
 * Normalizes a semantic version string to standard X.Y.Z format.
 * Removes leading 'v' prefix and preserves prerelease/build metadata.
 * @param {string|null} versionSpec - A version string with optional semver prefix or specifier (e.g., '^1.2.3', 'v1.2.3-alpha')
 * @returns {string|null} The normalized version (e.g., '1.2.3-alpha') or null if invalid
 */
export function normalizeVersionSpec(versionSpec) {
  if (!versionSpec || typeof versionSpec !== "string") {
    return null;
  }

  const trimmed = versionSpec.trim();
  const match = trimmed.match(/\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?/);
  return match ? match[0] : null;
}

/**
 * Removes leading 'v' prefix from version strings in CDN URLs.
 * @param {string|null} version - A version string that may start with 'v' (e.g., 'v1.2.3')
 * @returns {string|null} The version without 'v' prefix (e.g., '1.2.3') or null if invalid
 */
export function normalizeUrlVersion(version) {
  if (!version || typeof version !== "string") {
    return null;
  }

  return version.replace(/^v/i, "");
}

/**
 * Extracts package name, version, and provider from a CDN asset URL.
 * Supports jsDelivr, cdnjs, and unpkg CDN formats.
 * @param {string} assetUrl - A full CDN URL (e.g., 'https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css')
 * @returns {object} An object with {provider, packageName, urlVersion}; provider or packageName may be null if URL format unrecognized
 */
export function extractCdnMetadata(assetUrl) {
  try {
    const parsed = new URL(assetUrl);
    const host = parsed.hostname.toLowerCase();
    const pathname = parsed.pathname;

    if (host.includes("cdn.jsdelivr.net")) {
      const match = pathname.match(/^\/npm\/((?:@[^/]+\/)?[^@/]+)@([^/]+)(?:\/|$)/);
      if (!match) {
        return { provider: "jsdelivr", packageName: null, urlVersion: null };
      }

      return {
        provider: "jsdelivr",
        packageName: match[1],
        urlVersion: normalizeUrlVersion(match[2])
      };
    }

    if (host.includes("cdnjs.cloudflare.com")) {
      const match = pathname.match(/^\/ajax\/libs\/([^/]+)\/([^/]+)(?:\/|$)/);
      if (!match) {
        return { provider: "cdnjs", packageName: null, urlVersion: null };
      }

      return {
        provider: "cdnjs",
        packageName: match[1],
        urlVersion: normalizeUrlVersion(match[2])
      };
    }

    if (host.includes("unpkg.com")) {
      const match = pathname.match(/^\/((?:@[^/]+\/)?[^@/]+)@([^/]+)(?:\/|$)/);
      if (!match) {
        return { provider: "unpkg", packageName: null, urlVersion: null };
      }

      return {
        provider: "unpkg",
        packageName: match[1],
        urlVersion: normalizeUrlVersion(match[2])
      };
    }

    return {
      provider: "other",
      packageName: null,
      urlVersion: null
    };
  } catch {
    return {
      provider: "invalid-url",
      packageName: null,
      urlVersion: null
    };
  }
}

/**
 * Converts Razor-escaped scoped npm package names back to standard format.
 * In Razor, a literal '@' is written as '@@' in attributes; this reverses that escape.
 * @param {string|null} packageName - A package name, possibly with Razor escaping (e.g., '@@scope/package')
 * @returns {string|null} The normalized package name (e.g., '@scope/package') or the input if not escaped
 */
export function normalizeScopedPackageName(packageName) {
  if (!packageName || typeof packageName !== "string") {
    return packageName;
  }

  // Razor escapes a literal '@' as '@@', which can appear in scoped npm package URLs.
  return packageName.startsWith("@@") ? packageName.slice(1) : packageName;
}

/**
 * Maps a CDN package name to its local package.json equivalent using CDN_ALIASES.
 * First normalizes Razor escaping, then looks up any custom alias.
 * @param {string|null} rawPackageName - A package name from CDN metadata (may be Razor-escaped)
 * @returns {string|null} The mapped local package name, or the normalized input if no alias exists
 */
export function mapPackageName(rawPackageName) {
  if (!rawPackageName) {
    return null;
  }

  const normalized = normalizeScopedPackageName(rawPackageName);
  return CDN_ALIASES[normalized] || normalized;
}

function getLineNumberFromIndex(content, index) {
  const before = content.slice(0, index);
  return before.split("\n").length;
}

/**
 * Parses HTML/XML tag attributes into a key-value map.
 * Handles both double and single quoted attribute values.
 * @param {string} tagText - Raw HTML tag text (e.g., '<script src="..." integrity="...">')
 * @returns {Map<string, string>} Attribute names (lowercased) mapped to their values
 */
export function parseIntegrityTagAttributes(tagText) {
  const attrRegex = /\b([a-zA-Z_:][\w:.-]*)\s*=\s*("([^"]*)"|'([^']*)')/g;
  const attributes = new Map();
  let match = attrRegex.exec(tagText);

  while (match) {
    const name = match[1].toLowerCase();
    const value = match[3] ?? match[4] ?? "";
    attributes.set(name, value);
    match = attrRegex.exec(tagText);
  }

  return attributes;
}

/**
 * Scans markup content for <script> and <link> tags with integrity attributes.
 * Returns metadata for each tag: location, tag type, URL, integrity value, and position info.
 * @param {string} content - HTML/Razor markup content
 * @param {string} relativePath - Relative file path for diagnostic purposes
 * @returns {Array<object>} Array of integrity entries with {relativePath, line, tagName, url, integrity, tagText, tagStart, tagEnd}
 */
export function parseIntegrityEntriesFromContent(content, relativePath) {
  const entries = [];
  const tagRegex = /<(script|link)\b[\s\S]*?>/gi;
  let tagMatch = tagRegex.exec(content);

  while (tagMatch) {
    const tagName = tagMatch[1].toLowerCase();
    const tagText = tagMatch[0];
    const tagStart = tagMatch.index;
    const tagEnd = tagStart + tagText.length;
    const attributes = parseIntegrityTagAttributes(tagText);

    if (attributes.has("integrity")) {
      const assetUrl = attributes.get("src") || attributes.get("href");
      const integrity = attributes.get("integrity");

      if (assetUrl && /^https?:\/\//i.test(assetUrl)) {
        entries.push({
          relativePath,
          line: getLineNumberFromIndex(content, tagStart),
          tagName,
          url: assetUrl,
          integrity,
          tagText,
          tagStart,
          tagEnd
        });
      }
    }

    tagMatch = tagRegex.exec(content);
  }

  return entries;
}

function collectFilesRecursive(rootDir, predicate) {
  const output = [];
  const stack = [rootDir];

  while (stack.length > 0) {
    const current = stack.pop();
    const children = fs.readdirSync(current, { withFileTypes: true });

    for (const child of children) {
      if (child.name === "bin" || child.name === "obj" || child.name === ".git"
        || child.name === "node_modules") {
        continue;
      }

      const fullPath = path.join(current, child.name);
      if (child.isDirectory()) {
        stack.push(fullPath);
        continue;
      }

      if (predicate(fullPath)) {
        output.push(fullPath);
      }
    }
  }

  return output;
}

/**
 * Recursively scans the repo for all Razor/HTML files and collects integrity entries from each.
 * Searches DEFAULT_SCAN_ROOTS and ignores build/cache directories (bin, obj, .git, node_modules).
 * @param {string} repoRoot - Absolute path to the repository root
 * @returns {Array<object>} Array of all integrity entries found across all scanned files
 */
export function collectRazorIntegrityEntries(repoRoot) {
  const entries = [];

  for (const scanRoot of DEFAULT_SCAN_ROOTS) {
    const absoluteRoot = path.join(repoRoot, scanRoot);
    if (!fs.existsSync(absoluteRoot)) {
      continue;
    }

    const files = collectFilesRecursive(absoluteRoot, (fullPath) => SCAN_EXTENSIONS.has(path.extname(fullPath).toLowerCase()));

    for (const filePath of files) {
      const content = fs.readFileSync(filePath, "utf8");
      const relativePath = path.relative(repoRoot, filePath).replace(/\\/g, "/");
      entries.push(...parseIntegrityEntriesFromContent(content, relativePath));
    }
  }

  return entries;
}

/**
 * Parses an integrity attribute value into structured algorithm-digest tokens.
 * Supports multiple space-separated tokens (e.g., 'sha384-xxx sha512-yyy').
 * @param {string} integrity - Integrity attribute value (e.g., 'sha384-abc123def456')
 * @returns {Array<object>} Array of {raw, algorithm, digest} objects; empty if malformed
 */
export function parseIntegrityTokens(integrity) {
  const tokens = String(integrity)
    .trim()
    .split(/\s+/)
    .filter(Boolean)
    .map((token) => {
      const separator = token.indexOf("-");
      if (separator <= 0) {
        return null;
      }

      const algorithm = token.slice(0, separator).toLowerCase();
      const digest = token.slice(separator + 1);
      if (!algorithm || !digest) {
        return null;
      }

      return { raw: token, algorithm, digest };
    })
    .filter(Boolean);

  return tokens;
}

/**
 * Computes an SRI integrity token for a buffer using the specified hash algorithm.
 * Returns the token in standard SRI format: 'algorithm-base64digest'.
 * @param {Buffer} buffer - Raw bytes to hash
 * @param {string} algorithm - Hash algorithm name (e.g., 'sha384', 'sha512')
 * @returns {string} SRI token (e.g., 'sha384-abc123def456...')
 */
export function computeIntegrity(buffer, algorithm) {
  const digest = crypto.createHash(algorithm).update(buffer).digest("base64");
  return `${algorithm}-${digest}`;
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

/**
 * Fetches URL content with exponential backoff retry on network failure.
 * Throws an error if all attempts fail; retries add 500ms * 2^(attempt-1) delay between tries.
 * @param {string} url - Full URL to fetch
 * @param {number} attempts - Maximum fetch attempts (default: 3)
 * @returns {Promise<Buffer>} The response body as a Buffer
 * @throws {Error} If all attempts fail
 */
export async function fetchBytesWithRetry(url, attempts = 3) {
  let lastError = null;

  for (let attempt = 1; attempt <= attempts; attempt += 1) {
    try {
      const response = await fetch(url);
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const arrayBuffer = await response.arrayBuffer();
      return Buffer.from(arrayBuffer);
    } catch (error) {
      lastError = error;
      if (attempt < attempts) {
        const backoffMs = 500 * Math.pow(2, attempt - 1);
        await sleep(backoffMs);
      }
    }
  }

  throw lastError;
}

/**
 * Loads and merges all dependencies and devDependencies from package.json into a single version map.
 * @param {string} repoRoot - Absolute path to the repository root
 * @returns {object} A map of {packageName: versionSpec}
 */
export function loadPackageVersionMap(repoRoot) {
  const packageJsonPath = path.join(repoRoot, "package.json");
  const packageJson = JSON.parse(fs.readFileSync(packageJsonPath, "utf8"));
  return {
    ...(packageJson.dependencies || {}),
    ...(packageJson.devDependencies || {})
  };
}

/**
 * Detects which packages have changed versions between HEAD and the current working directory.
 * Uses 'git show HEAD:package.json' to compare versions; returns null if git fails.
 * @param {string} repoRoot - Absolute path to the repository root
 * @returns {Set<string>|null} A Set of package names with changed versions, or null if git command fails
 */
export function detectChangedPackagesFromHead(repoRoot) {
  try {
    const headPackageJson = execSync("git show HEAD:package.json", {
      cwd: repoRoot,
      stdio: ["ignore", "pipe", "pipe"],
      encoding: "utf8"
    });

    const currentPackageJson = fs.readFileSync(path.join(repoRoot, "package.json"), "utf8");
    const oldPkg = JSON.parse(headPackageJson);
    const newPkg = JSON.parse(currentPackageJson);

    const oldVersions = {
      ...(oldPkg.dependencies || {}),
      ...(oldPkg.devDependencies || {})
    };
    const newVersions = {
      ...(newPkg.dependencies || {}),
      ...(newPkg.devDependencies || {})
    };

    const changed = new Set();
    for (const [name, version] of Object.entries(newVersions)) {
      if (oldVersions[name] !== version) {
        changed.add(name);
      }
    }

    return changed;
  } catch {
    return null;
  }
}

/**
 * Replaces a version segment in a CDN URL with a new version.
 * Handles both jsDelivr/unpkg format (@version/) and cdnjs format (/version/).
 * @param {string} url - Full CDN URL (e.g., 'https://cdn.jsdelivr.net/npm/bootstrap@5.0.0/...')
 * @param {string} oldVersion - Current version in the URL (e.g., '5.0.0')
 * @param {string} newVersion - New version to substitute (e.g., '5.3.0')
 * @returns {string} The URL with version replaced
 */
export function substituteUrlVersion(url, oldVersion, newVersion) {
  // Handles jsdelivr/unpkg (@VERSION/) and cdnjs (/VERSION/) URL patterns.
  return url
    .replace(`@${oldVersion}/`, `@${newVersion}/`)
    .replace(`/${oldVersion}/`, `/${newVersion}/`);
}

/**
 * Applies a batch of integrity and/or URL updates to markup content.
 * Processes updates in reverse document order (highest tagStart first) to preserve indices.
 * @param {string} content - HTML/Razor markup content
 * @param {Array<object>} updates - Array of {tagStart, tagEnd, newIntegrity, newUrl?} objects
 * @returns {string} The updated content with all changes applied
 */
export function updateIntegrityInContent(content, updates) {
  if (!updates.length) {
    return content;
  }

  const sorted = [...updates].sort((a, b) => b.tagStart - a.tagStart);
  let updatedContent = content;

  for (const item of sorted) {
    const originalTag = updatedContent.slice(item.tagStart, item.tagEnd);
    let replacedTag = originalTag.replace(/(\bintegrity\s*=\s*["'])[^"']+(["'])/i, `$1${item.newIntegrity}$2`);
    if (item.newUrl) {
      replacedTag = replacedTag
        .replace(/(\bsrc\s*=\s*["'])[^"']+(["'])/i, `$1${item.newUrl}$2`)
        .replace(/(\bhref\s*=\s*["'])[^"']+(["'])/i, `$1${item.newUrl}$2`);
    }
    updatedContent = updatedContent.slice(0, item.tagStart) + replacedTag + updatedContent.slice(item.tagEnd);
  }

  return updatedContent;
}

