/* eslint-disable jsdoc/require-param-description, jsdoc/require-param-type, jsdoc/require-returns */

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
 *
 * @param versionSpec
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
 *
 * @param version
 */
export function normalizeUrlVersion(version) {
  if (!version || typeof version !== "string") {
    return null;
  }

  return version.replace(/^v/i, "");
}

/**
 *
 * @param assetUrl
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
 *
 * @param packageName
 */
export function normalizeScopedPackageName(packageName) {
  if (!packageName || typeof packageName !== "string") {
    return packageName;
  }

  // Razor escapes a literal '@' as '@@', which can appear in scoped npm package URLs.
  return packageName.startsWith("@@") ? packageName.slice(1) : packageName;
}

/**
 *
 * @param rawPackageName
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
 *
 * @param tagText
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
 *
 * @param content
 * @param relativePath
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
      if (child.name === "bin" || child.name === "obj" || child.name === ".git") {
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
 *
 * @param repoRoot
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
 *
 * @param integrity
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
 *
 * @param buffer
 * @param algorithm
 */
export function computeIntegrity(buffer, algorithm) {
  const digest = crypto.createHash(algorithm).update(buffer).digest("base64");
  return `${algorithm}-${digest}`;
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

/**
 *
 * @param url
 * @param attempts
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
 *
 * @param repoRoot
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
 *
 * @param repoRoot
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
 *
 * @param url
 * @param oldVersion
 * @param newVersion
 */
export function substituteUrlVersion(url, oldVersion, newVersion) {
  // Handles jsdelivr/unpkg (@VERSION/) and cdnjs (/VERSION/) URL patterns.
  return url
    .replace(`@${oldVersion}/`, `@${newVersion}/`)
    .replace(`/${oldVersion}/`, `/${newVersion}/`);
}

/**
 *
 * @param content
 * @param updates
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

