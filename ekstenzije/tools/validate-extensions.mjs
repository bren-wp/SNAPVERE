import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(here, "..", "..");
const extensionsRoot = path.join(repoRoot, "ekstenzije");
const browsers = ["chrome", "edge", "opera", "firefox"];
const allowedPermissions = new Set(["activeTab", "scripting", "downloads", "downloads.open", "storage"]);
const expectedCommands = Object.freeze({
  "capture-region": {
    default: "Ctrl+Shift+1",
    mac: "Command+Shift+1",
    description: "__MSG_commandCaptureRegion__"
  },
  "capture-visible": {
    default: "Ctrl+Shift+2",
    mac: "Command+Shift+2",
    description: "__MSG_commandCaptureVisible__"
  },
  "capture-full-page": {
    default: "Ctrl+Shift+3",
    mac: "Command+Shift+7",
    description: "__MSG_commandCaptureFullPage__"
  }
});
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
    if (messages.extensionName?.message !== "SNAPVERE") {
      fail(`${path.basename(browserDir)} ${locale} extensionName must remain SNAPVERE.`);
    }
  }
}

function validateLocalizationReferences(browser, browserDir) {
  const messages = readJson(path.join(browserDir, "_locales/en/messages.json"));
  const availableKeys = new Set(Object.keys(messages));
  const files = [
    ["manifest.json", [/__MSG_([A-Za-z0-9_]+)__/g]],
    ["popup.html", [/\bdata-i18n(?:-aria-label)?=["']([A-Za-z0-9_]+)["']/g]],
    ["options.html", [/\bdata-i18n(?:-aria-label)?=["']([A-Za-z0-9_]+)["']/g]],
    ["popup.js", [
      /\b(?:t|setStatus)\(\s*["']([A-Za-z0-9_]+)["']/g,
      /chrome\.i18n\.getMessage\(\s*["']([A-Za-z0-9_]+)["']/g
    ]],
    ["options.js", [
      /\bt\(\s*["']([A-Za-z0-9_]+)["']/g,
      /chrome\.i18n\.getMessage\(\s*["']([A-Za-z0-9_]+)["']/g
    ]],
    ["background.js", [
      /\blocalizedMessage\(\s*["']([A-Za-z0-9_]+)["']/g,
      /new\s+SnapvereError\(\s*["']([A-Za-z0-9_]+)["']/g,
      /chrome\.i18n\.getMessage\(\s*["']([A-Za-z0-9_]+)["']/g
    ]],
    ["capture.js", [
      /chrome\.i18n\.getMessage\(\s*["']([A-Za-z0-9_]+)["']/g
    ]]
  ];

  for (const [relative, patterns] of files) {
    const text = fs.readFileSync(path.join(browserDir, relative), "utf8");
    for (const pattern of patterns) {
      for (const match of text.matchAll(pattern)) {
        const key = match[1];
        if (!availableKeys.has(key)) {
          fail(`${browser}/${relative} references missing locale key: ${key}`);
        }
      }
    }
  }
}

function validateCommandContract(browser, manifest) {
  const commands = manifest.commands && typeof manifest.commands === "object" ? manifest.commands : {};
  const actualIds = Object.keys(commands).sort();
  const expectedIds = Object.keys(expectedCommands).sort();
  if (JSON.stringify(actualIds) !== JSON.stringify(expectedIds)) {
    fail(`${browser} must declare exactly the three SNAPVERE capture commands.`);
  }

  const defaults = new Set();
  const macKeys = new Set();
  for (const [commandId, expected] of Object.entries(expectedCommands)) {
    const command = commands[commandId];
    if (!command || typeof command !== "object") fail(`${browser} is missing command: ${commandId}`);
    if (command.description !== expected.description) {
      fail(`${browser} command ${commandId} must use ${expected.description}.`);
    }
    if (command.suggested_key?.default !== expected.default) {
      fail(`${browser} command ${commandId} default shortcut must be ${expected.default}.`);
    }
    if (command.suggested_key?.mac !== expected.mac) {
      fail(`${browser} command ${commandId} mac shortcut must be ${expected.mac}.`);
    }
    if (defaults.has(expected.default) || macKeys.has(expected.mac)) {
      fail(`${browser} command shortcuts must be unique.`);
    }
    defaults.add(expected.default);
    macKeys.add(expected.mac);
  }
}

function validateManifest(browser, browserDir) {
  const manifest = readJson(path.join(browserDir, "manifest.json"));
  if (manifest.manifest_version !== 3) fail(`${browser} must use Manifest V3.`);
  if (!/^\d+\.\d+\.\d+(?:\.\d+)?$/.test(String(manifest.version || ""))) {
    fail(`${browser} has invalid version format.`);
  }
  if (manifest.name !== "__MSG_extensionName__") fail(`${browser} manifest name must use the locked SNAPVERE locale key.`);
  if (manifest.default_locale !== "en") fail(`${browser} default_locale must be en.`);

  const permissions = Array.isArray(manifest.permissions) ? manifest.permissions : [];
  if (permissions.length !== allowedPermissions.size) {
    fail(`${browser} permission count changed.`);
  }
  for (const permission of permissions) {
    if (!allowedPermissions.has(permission)) fail(`${browser} requests disallowed permission: ${permission}`);
  }
  for (const required of allowedPermissions) {
    if (!permissions.includes(required)) fail(`${browser} is missing required permission: ${required}`);
  }

  const hostPermissions = Array.isArray(manifest.host_permissions) ? manifest.host_permissions : [];
  if (hostPermissions.length !== 0) fail(`${browser} must not declare host_permissions.`);

  validateCommandContract(browser, manifest);

  const background = manifest.background || {};
  if (browser === "firefox") {
    if (!Array.isArray(background.scripts) || !background.scripts.includes("background.js")) {
      fail("Firefox must use background.scripts with background.js.");
    }
    if ("service_worker" in background) fail("Firefox manifest must not declare background.service_worker.");
    if (!manifest.browser_specific_settings?.gecko?.id) fail("Firefox manifest is missing Gecko extension id.");
  } else {
    if (background.service_worker !== "background.js") fail(`${browser} must use background.service_worker.`);
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
    if (!fs.existsSync(path.join(browserDir, ref))) fail(`${browser} references missing file: ${ref}`);
  }
}

function validateBrandLock(browser, browserDir) {
  const background = fs.readFileSync(path.join(browserDir, "background.js"), "utf8");
  const optionsHtml = fs.readFileSync(path.join(browserDir, "options.html"), "utf8");
  const optionsJs = fs.readFileSync(path.join(browserDir, "options.js"), "utf8");
  const popupHtml = fs.readFileSync(path.join(browserDir, "popup.html"), "utf8");

  if (!/const\s+FILE_PREFIX\s*=\s*["']SNAPVERE["']/.test(background)) {
    fail(`${browser}/background.js must hard-lock the SNAPVERE filename prefix.`);
  }
  if (!/`\$\{FILE_PREFIX\}-\$\{kind\}-\$\{timestamp\(\)\}\.png`/.test(background)) {
    fail(`${browser}/background.js must build capture filenames from the locked SNAPVERE prefix.`);
  }

  for (const [name, text] of [["background.js", background], ["options.html", optionsHtml], ["options.js", optionsJs]]) {
    if (/filenamePrefix|filename-prefix|fileNamePrefix|invalidPrefix/.test(text)) {
      fail(`${browser}/${name} must not expose or consume configurable branding/filename prefixes.`);
    }
  }

  if (!/<div class="wordmark">SNAPVERE<\/div>/.test(optionsHtml) ||
      !/<div class="wordmark">SNAPVERE<\/div>/.test(popupHtml)) {
    fail(`${browser} visible extension surfaces must retain the SNAPVERE wordmark.`);
  }

  const visibleHtml = `${optionsHtml}\n${popupHtml}`;
  if (/\b(?:dev|debug|todo|placeholder)\b/i.test(visibleHtml)) {
    fail(`${browser} visible extension UI contains development-only wording.`);
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

    if (relative === "popup.css") {
      if (/min-width:\s*(?:3[4-9]\d|[4-9]\d{2,})px/i.test(text)) {
        fail(`${browser}/popup.css must not force a desktop-width popup on narrow viewports.`);
      }
      if (!/@media\s*\(max-width:\s*320px\)/i.test(text)) {
        fail(`${browser}/popup.css must keep the narrow-popup responsive breakpoint.`);
      }
      if (!/@media\s*\(max-width:\s*280px\)/i.test(text) ||
          !/grid-template-columns:\s*26px\s+minmax\(0,\s*1fr\)/i.test(text)) {
        fail(`${browser}/popup.css must keep the extra-narrow popup reflow.`);
      }
      if (!/overflow-wrap:\s*anywhere/i.test(text)) {
        fail(`${browser}/popup.css must allow long localized/status copy to wrap on narrow viewports.`);
      }
    }

    if (relative === "options.css") {
      for (const breakpoint of [540, 420, 320]) {
        if (!new RegExp(`@media\\s*\\(max-width:\\s*${breakpoint}px\\)`, "i").test(text)) {
          fail(`${browser}/options.css must keep the ${breakpoint}px responsive breakpoint.`);
        }
      }
      if (!/\.tabs\s*\{[^}]*grid-template-columns:\s*1fr/i.test(text)) {
        fail(`${browser}/options.css must stack tabs in compact mode.`);
      }
    }

    if (relative === "capture.js") {
      const postEncodeSessionGuard = /const\s+blob\s*=\s*await\s+canvasToBlob\(canvas\);\s*ensureFullToken\(token\);/;
      if (!postEncodeSessionGuard.test(text)) {
        fail(`${browser}/capture.js must revalidate the full-page token after async canvas encoding.`);
      }
      if (!/REGION_SELECTED[\s\S]*?\.then\s*\(\(response\)\s*=>[\s\S]*?showRegionError/.test(text)) {
        fail(`${browser}/capture.js must surface asynchronous region capture failures after the popup closes.`);
      }
      if (/fullState\.tiles\.push|tiles\s*:\s*\[\]/.test(text)) {
        fail(`${browser}/capture.js must not retain decoded full-page tile images in an unbounded array.`);
      }
      if (!/fullState\.context\.drawImage\(image/.test(text) || !/image\.src\s*=\s*["']["']/.test(text)) {
        fail(`${browser}/capture.js must draw full-page tiles incrementally and release decoded images.`);
      }
    }
  }
}

for (const browser of browsers) {
  const browserDir = path.join(extensionsRoot, browser);
  if (!fs.existsSync(browserDir) || !fs.statSync(browserDir).isDirectory()) {
    fail(`Missing browser extension directory: ${browser}`);
  }

  for (const relative of requiredFiles) {
    if (!fs.existsSync(path.join(browserDir, relative))) fail(`${browser} is missing required file: ${relative}`);
  }

  validateManifest(browser, browserDir);
  validateMessages(browserDir);
  validateLocalizationReferences(browser, browserDir);
  validateBrandLock(browser, browserDir);
  validateSource(browser, browserDir);
  console.log(`Validated ${browser}.`);
}

console.log("SNAPVERE browser extension validation passed: branding locked, permissions bounded, localization references verified, command shortcuts locked, capture memory lifecycle enforced.");