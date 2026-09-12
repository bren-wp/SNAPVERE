import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(here, "..", "..");
const extensionsRoot = path.join(repoRoot, "ekstenzije");
const browsers = ["chrome", "edge", "opera", "firefox"];
const allowedPermissions = new Set(["activeTab", "scripting", "downloads", "storage"]);
const requiredFiles = [
  "manifest.json",
  "background.js",
  "popup.html",
  "popup.css",
  "popup.js",
  "capture.js",
  "options.html",
  "options.css",
  "options.js",
  "_locales/en/messages.json",
  "_locales/hr/messages.json",
  "icons/icon-16.png",
  "icons/icon-32.png",
  "icons/icon-48.png",
  "icons/icon-128.png"
];

function fail(message) {
  throw new Error(message);
}

function readJson(file) {
  return JSON.parse(fs.readFileSync(file, "utf8"));
}

function walk(dir) {
  const out = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const absolute = path.join(dir, entry.name);
    if (entry.isDirectory()) out.push(...walk(absolute));
    else out.push(absolute);
  }
  return out;
}

function validateMessages(browserDir) {
  const en = readJson(path.join(browserDir, "_locales/en/messages.json"));
  const hr = readJson(path.join(browserDir, "_locales/hr/messages.json"));
  const enKeys = Object.keys(en).sort();
  const hrKeys = Object.keys(hr).sort();

  if (JSON.stringify(enKeys) !== JSON.stringify(hrKeys)) {
    fail(`${path.basename(browserDir)} locale keys differ between EN and HR.`);
  }
  for (const [locale, messages] of [["en", en], ["hr", hr]]) {
    for (const [key, value] of Object.entries(messages)) {
      if (!value || typeof value.message !== "string" || value.message.trim() === "") {
        fail(`${path.basename(browserDir)} ${locale} locale has invalid message: ${key}`);
      }
    }
  }
}

function validateManifest(browser, browserDir) {
  const manifest = readJson(path.join(browserDir, "manifest.json"));
  if (manifest.manifest_version !== 3) fail(`${browser} must use Manifest V3.`);
  if (!/^\d+\.\d+\.\d+(?:\.\d+)?$/.test(String(manifest.version || ""))) {
    fail(`${browser} has invalid version format.`);
  }
  if (manifest.default_locale !== "en") fail(`${browser} default_locale must be en.`);

  const permissions = Array.isArray(manifest.permissions) ? manifest.permissions : [];
  for (const permission of permissions) {
    if (!allowedPermissions.has(permission)) fail(`${browser} requests disallowed permission: ${permission}`);
  }
  for (const required of allowedPermissions) {
    if (!permissions.includes(required)) fail(`${browser} is missing required permission: ${required}`);
  }

  const hostPermissions = Array.isArray(manifest.host_permissions) ? manifest.host_permissions : [];
  if (hostPermissions.length !== 0) fail(`${browser} must not declare host_permissions.`);

  const background = manifest.background || {};
  if (browser === "firefox") {
    if (!Array.isArray(background.scripts) || !background.scripts.includes("background.js")) {
      fail("Firefox must use background.scripts with background.js.");
    }
    if ("service_worker" in background) fail("Firefox manifest must not declare background.service_worker.");
    if (!manifest.browser_specific_settings?.gecko?.id) fail("Firefox manifest is missing Gecko extension id.");
  } else {
    if (background.service_worker !== "background.js") {
      fail(`${browser} must use background.service_worker.`);
    }
    if ("scripts" in background) fail(`${browser} must not declare background.scripts.`);
  }

  const references = new Set([
    background.service_worker,
    ...(Array.isArray(background.scripts) ? background.scripts : []),
    manifest.action?.default_popup,
    manifest.options_page,
    ...Object.values(manifest.icons || {}),
    ...Object.values(manifest.action?.default_icon || {})
  ].filter(Boolean));

  for (const ref of references) {
    if (!fs.existsSync(path.join(browserDir, ref))) {
      fail(`${browser} references missing file: ${ref}`);
    }
  }
}

function validateSource(browser, browserDir) {
  const forbiddenNameFragments = ["node_modules", ".cache", "__pycache__", ".DS_Store", ".map"];
  const sourcePattern = /\.(?:js|mjs|html|css)$/i;
  const files = walk(browserDir);

  for (const absolute of files) {
    const relative = path.relative(browserDir, absolute).replaceAll(path.sep, "/");
    if (forbiddenNameFragments.some((fragment) => relative.includes(fragment))) {
      fail(`${browser} includes development/cache artifact: ${relative}`);
    }
    if (!sourcePattern.test(relative)) continue;

    const text = fs.readFileSync(absolute, "utf8");
    const checks = [
      [/\bhttp:\/\//i, "http://"],
      [/<all_urls>/i, "<all_urls>"],
      [/\beval\s*\(/, "eval("],
      [/\bnew\s+Function\s*\(/, "new Function("],
      [/<script[^>]+src=["']https?:\/\//i, "remote script"],
      [/\b(?:google-analytics|googletagmanager|mixpanel|segment\.io|sentry\.io)\b/i, "analytics/telemetry endpoint"]
    ];
    for (const [pattern, label] of checks) {
      if (pattern.test(text)) fail(`${browser}/${relative} contains forbidden ${label}.`);
    }
  }
}

for (const browser of browsers) {
  const browserDir = path.join(extensionsRoot, browser);
  if (!fs.existsSync(browserDir) || !fs.statSync(browserDir).isDirectory()) {
    fail(`Missing browser extension directory: ${browser}`);
  }

  for (const relative of requiredFiles) {
    if (!fs.existsSync(path.join(browserDir, relative))) {
      fail(`${browser} is missing required file: ${relative}`);
    }
  }

  validateManifest(browser, browserDir);
  validateMessages(browserDir);
  validateSource(browser, browserDir);
  console.log(`Validated ${browser}.`);
}

console.log("SNAPVERE browser extension validation passed.");
