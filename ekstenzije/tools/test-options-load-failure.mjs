import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import vm from "node:vm";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, "..");
const browsers = ["chrome", "edge", "opera", "firefox"];

class FakeClassList { toggle() {} }
class FakeElement {
  constructor(id = "") {
    this.id = id; this.dataset = {}; this.attributes = new Map(); this.listeners = new Map();
    this.children = []; this.classList = new FakeClassList(); this.className = ""; this.textContent = "";
    this.hidden = false; this.disabled = false; this.checked = false; this.tabIndex = 0; this.isConnected = true;
  }
  setAttribute(name, value) { this.attributes.set(name, String(value)); }
  getAttribute(name) { return this.attributes.get(name) || null; }
  addEventListener(type, handler) {
    const handlers = this.listeners.get(type) || [];
    handlers.push(handler); this.listeners.set(type, handlers);
  }
  dispatch(type, event = {}) {
    for (const handler of this.listeners.get(type) || []) {
      handler({ currentTarget: this, preventDefault() {}, ...event });
    }
  }
  append(...nodes) { this.children.push(...nodes); }
  replaceChildren(...nodes) { this.children = [...nodes]; }
  focus() {}
  querySelector(selector) {
    return selector === 'button[type="submit"]' ? this.submitButton || null : null;
  }
}
const flush = () => new Promise((resolve) => setImmediate(resolve));

for (const browser of browsers) {
  const ids = [
    "settings-form", "save-as", "settings-status", "recent-status", "recent-list",
    "refresh-recent", "open-downloads-folder", "settings-panel", "recent-panel",
    "settings-tab", "recent-tab"
  ];
  const elements = Object.fromEntries(ids.map((id) => [id, new FakeElement(id)]));
  elements["settings-tab"].dataset.panel = "settings-panel";
  elements["recent-tab"].dataset.panel = "recent-panel";
  elements["settings-form"].submitButton = new FakeElement("save-settings");

  let storageWrites = 0;
  const chrome = {
    runtime: { lastError: null },
    i18n: { getMessage: (key) => key, getUILanguage: () => "en-US" },
    storage: {
      local: {
        get: (_key, callback) => {
          chrome.runtime.lastError = { message: "simulated settings read failure" };
          callback({});
          chrome.runtime.lastError = null;
        },
        set: (_value, callback) => { storageWrites += 1; callback(); }
      }
    },
    downloads: {
      search: (_query, callback) => callback([]),
      open: (_id, callback) => callback(),
      showDefaultFolder: () => {}
    }
  };

  const document = {
    documentElement: { lang: "" },
    getElementById: (id) => elements[id] || null,
    querySelectorAll: (selector) =>
      selector === "[data-panel]" ? [elements["settings-tab"], elements["recent-tab"]] : [],
    createElement: () => new FakeElement()
  };

  const context = {
    chrome, document, history: { replaceState() {} }, location: { hash: "#settings" },
    Intl, Date, Number, Array, Promise, setTimeout, clearTimeout
  };
  vm.createContext(context);
  const source = fs.readFileSync(path.join(root, browser, "options.js"), "utf8");
  vm.runInContext(source, context, { filename: `${browser}/options.js` });
  await flush();

  const submit = elements["settings-form"].submitButton;
  assert.equal(elements["save-as"].disabled, true, `${browser}: Save As must stay disabled after settings load failure`);
  assert.equal(submit.disabled, true, `${browser}: Save must stay disabled after settings load failure`);
  assert.equal(elements["settings-form"].getAttribute("aria-busy"), "true", `${browser}: failed settings form must remain non-interactive`);
  assert.equal(elements["settings-status"].textContent, "settingsLoadFailed", `${browser}: load failure must remain visible`);
  assert.equal(elements["settings-status"].className, "status error", `${browser}: load failure must use error state`);

  elements["save-as"].checked = true;
  elements["settings-form"].dispatch("submit");
  await flush();
  assert.equal(storageWrites, 0, `${browser}: failed initial read must never be overwritten`);
}
console.log("SNAPVERE options initial-load failure containment tests passed for all browser variants.");
