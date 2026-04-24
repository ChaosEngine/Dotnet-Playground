import test from "node:test";
import assert from "node:assert/strict";
import {
  extractCdnMetadata,
  mapPackageName,
  normalizeVersionSpec,
  parseIntegrityEntriesFromContent,
  parseIntegrityTokens,
  updateIntegrityInContent
} from "./sri-lib.mjs";

test("extracts jsdelivr package and version", () => {
  const metadata = extractCdnMetadata("https://cdn.jsdelivr.net/npm/bootstrap-table@1.27.3/dist/bootstrap-table.min.js");
  assert.equal(metadata.provider, "jsdelivr");
  assert.equal(metadata.packageName, "bootstrap-table");
  assert.equal(metadata.urlVersion, "1.27.3");
});

test("extracts cdnjs package and version with query string", () => {
  const metadata = extractCdnMetadata("https://cdnjs.cloudflare.com/ajax/libs/html2canvas/1.4.1/html2canvas.min.js?cache=1");
  assert.equal(metadata.provider, "cdnjs");
  assert.equal(metadata.packageName, "html2canvas");
  assert.equal(metadata.urlVersion, "1.4.1");
});

test("normalizes Razor-escaped scoped package names", () => {
  const metadata = extractCdnMetadata("https://cdn.jsdelivr.net/npm/@@microsoft/signalr@10.0.0/dist/browser/signalr.min.js");
  assert.equal(metadata.provider, "jsdelivr");
  assert.equal(metadata.packageName, "@@microsoft/signalr");
  assert.equal(mapPackageName(metadata.packageName), "@microsoft/signalr");
});

test("normalizes semver with range prefixes", () => {
  assert.equal(normalizeVersionSpec("^1.2.3"), "1.2.3");
  assert.equal(normalizeVersionSpec("~4.5.6"), "4.5.6");
  assert.equal(normalizeVersionSpec("workspace:*"), null);
});

test("parses multiline script tag with integrity", () => {
  const content = [
    "<script src=\"https://cdn.jsdelivr.net/npm/chance@1.1.13/dist/chance.min.js\"",
    "  integrity=\"sha256-OLDHASH==\" crossorigin=\"anonymous\"></script>"
  ].join("\n");

  const entries = parseIntegrityEntriesFromContent(content, "fixture.cshtml");
  assert.equal(entries.length, 1);
  assert.equal(entries[0].line, 1);
  assert.equal(entries[0].integrity, "sha256-OLDHASH==");
});

test("parses integrity tokens", () => {
  const tokens = parseIntegrityTokens("sha256-abc= sha384-def=");
  assert.equal(tokens.length, 2);
  assert.equal(tokens[0].algorithm, "sha256");
  assert.equal(tokens[1].algorithm, "sha384");
});

test("updates integrity value in-place", () => {
  const content = "<script src=\"https://cdn.jsdelivr.net/npm/x@1.0.0/a.js\" integrity=\"sha256-old\" crossorigin=\"anonymous\"></script>";
  const entries = parseIntegrityEntriesFromContent(content, "fixture.cshtml");

  const output = updateIntegrityInContent(content, [
    {
      tagStart: entries[0].tagStart,
      tagEnd: entries[0].tagEnd,
      newIntegrity: "sha256-new"
    }
  ]);

  assert.match(output, /integrity="sha256-new"/);
});
