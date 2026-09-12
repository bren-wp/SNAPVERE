import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const extensionsRoot = path.resolve(here, "..");
const browsers = ["chrome", "edge", "opera", "firefox"];
const chromiumBrowsers = ["chrome", "edge", "opera"];
const iconSizes = [16, 32, 48, 128];

function fail(message) {
  throw new Error(message);
}

function readJson(file) {
  return JSON.parse(fs.readFileSync(file, "utf8"));
}

function walkFiles(root, current = root) {
  const files = [];
  for (const entry of fs.readdirSync(current, { withFileTypes: true })) {
    const absolute = path.join(current, entry.name);
    if (entry.isDirectory()) files.push(...walkFiles(root, absolute));
    else files.push(path.relative(root, absolute).replaceAll(path.sep, "/"));
  }
  return files.sort();
}

function sha256(file) {
  return crypto.createHash("sha256").update(fs.readFileSync(file)).digest("hex");
}

function assertEqual(actual, expected, message) {
  if (actual !== expected) fail(`${message}\nExpected: ${expected}\nActual:   ${actual}`);
}

function assertJsonEqual(actual, expected, message) {
  assertEqual(JSON.stringify(actual), JSON.stringify(expected), message);
}

function pngDimensions(file) {
  const data = fs.readFileSync(file);
  const signature = "89504e470d0a1a0a";
  if (data.length < 24 || data.subarray(0, 8).toString("hex") !== signature) {
    fail(`${file} is not a valid PNG file.`);
  }
  return {
    width: data.readUInt32BE(16),
    height: data.readUInt32BE(20)
  };
}

function normalizeManifest(manifest) {
  const normalized = structuredClone(manifest);
  delete normalized.background;
  delete normalized.browser_specific_settings;
  return normalized;
}

function validateSharedTreeParity() {
  const chromeDir = path.join(extensionsRoot, "chrome");
  const chromeFiles = walkFiles(chromeDir).filter((file) => file !== "manifest.json");

  for (const browser of browsers.filter((item) => item !== "chrome")) {
    const browserDir = path.join(extensionsRoot, browser);
    const browserFiles = walkFiles(browserDir).filter((file) => file !== "manifest.json");
    assertJsonEqual(browserFiles, chromeFiles, `${browser} shared file set diverged from chrome.`);

    for (const relative of chromeFiles) {
      const expected = sha256(path.join(chromeDir, relative));
      const actual = sha256(path.join(browserDir, relative));
      assertEqual(actual, expected, `${browser}/${relative} diverged from chrome/${relative}.`);
    }
  }
}

function validateManifestParity() {
  const manifests = Object.fromEntries(
    browsers.map((browser) => [browser, readJson(path.join(extensionsRoot, browser, "manifest.json"))])
  );

  for (const browser of chromiumBrowsers.slice(1)) {
    assertJsonEqual(manifests[browser], manifests.chrome, `${browser} manifest diverged from Chrome manifest.`);
  }

  assertJsonEqual(
    normalizeManifest(manifests.firefox),
    normalizeManifest(manifests.chrome),
    "Firefox manifest contains an unexpected semantic difference from Chromium manifests."
  );

  const versions = new Set(browsers.map((browser) => manifests[browser].version));
  if (versions.size !== 1) fail("Browser extension versions are not aligned across all manifests.");
}

function validateIconDimensionsAndParity() {
  for (const size of iconSizes) {
    const relative = `icons/icon-${size}.png`;
    const chromeFile = path.join(extensionsRoot, "chrome", relative);
    const expectedHash = sha256(chromeFile);
    const dimensions = pngDimensions(chromeFile);

    if (dimensions.width !== size || dimensions.height !== size) {
      fail(`chrome/${relative} must be ${size}x${size}, found ${dimensions.width}x${dimensions.height}.`);
    }

    for (const browser of browsers.slice(1)) {
      const file = path.join(extensionsRoot, browser, relative);
      const browserDimensions = pngDimensions(file);
      if (browserDimensions.width !== size || browserDimensions.height !== size) {
        fail(`${browser}/${relative} must be ${size}x${size}, found ${browserDimensions.width}x${browserDimensions.height}.`);
      }
      assertEqual(sha256(file), expectedHash, `${browser}/${relative} differs from the canonical Chrome icon.`);
    }
  }
}

function validateHtmlEntrypoints() {
  for (const browser of browsers) {
    for (const page of ["popup.html", "options.html"]) {
      const file = path.join(extensionsRoot, browser, page);
      const html = fs.readFileSync(file, "utf8");

      if (/<script\b[^>]*\bsrc=["']https?:\/\//i.test(html) || /<link\b[^>]*\bhref=["']https?:\/\//i.test(html)) {
        fail(`${browser}/${page} references a remote runtime resource.`);
      }
      if (/\bon[a-z]+\s*=/i.test(html)) {
        fail(`${browser}/${page} contains an inline event handler.`);
      }
      if (/<script\b(?![^>]*\bsrc=)[^>]*>/i.test(html)) {
        fail(`${browser}/${page} contains an inline script block.`);
      }
      if (/<style\b/i.test(html)) {
        fail(`${browser}/${page} contains an inline style block.`);
      }
    }
  }
}

validateSharedTreeParity();
validateManifestParity();
validateIconDimensionsAndParity();
validateHtmlEntrypoints();

console.log("SNAPVERE browser extension parity checks passed.");
