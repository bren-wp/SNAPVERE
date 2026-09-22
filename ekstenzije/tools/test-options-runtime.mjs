import assert from "node:assert/strict";
import fs from "node:fs";
import vm from "node:vm";

class FakeClassList {
  constructor() { this.values = new Set(); }
  toggle(name, force) {
    if (force) this.values.add(name);
    else this.values.delete(name);
  }
}

class FakeElement {
  constructor(id = "") {
    this.id = id;
    this.dataset = {};
    this.attributes = new Map();
    this.listeners = new Map();
    this.children = [];
    this.classList = new FakeClassList();
    this.className = "";
    this.textContent = "";
    this.hidden = false;
    this.disabled = false;
    this.checked = false;
    this.tabIndex = 0;
    this.title = "";
    this.isConnected = true;
  }

  setAttribute(name, value) { this.attributes.set(name, String(value)); }
  getAttribute(name) { return this.attributes.get(name) || null; }
  addEventListener(type, handler) {
    const handlers = this.listeners.get(type) || [];
    handlers.push(handler);
    this.listeners.set(type, handlers);
  }
  dispatch(type, event = {}) {
    for (const handler of this.listeners.get(type) || []) handler({ currentTarget: this, preventDefault() {}, ...event });
  }
  append(...nodes) { this.children.push(...nodes); }
  appendChild(node) { this.children.push(node); return node; }
  replaceChildren(...nodes) { this.children = [...nodes]; }
  focus() {}
  querySelector(selector) {
    return selector === 'button[type="submit"]' ? this.submitButton || null : null;
  }
}

function makeItem(id, filename) {
  return {
    id,
    state: "complete",
    exists: true,
    filename: `C:\\Users\\Test\\Downloads\\${filename}`,
    startTime: "2026-09-18T17:00:00Z",
    fileSize: 4096
  };
}

const ids = [
  "settings-form", "save-as", "settings-status", "recent-status", "recent-list",
  "refresh-recent", "open-downloads-folder", "settings-panel", "recent-panel",
  "settings-tab", "recent-tab"
];
const elements = Object.fromEntries(ids.map((id) => [id, new FakeElement(id)]));
elements["settings-tab"].dataset.panel = "settings-panel";
elements["recent-tab"].dataset.panel = "recent-panel";
elements["settings-form"].submitButton = new FakeElement("save-settings");

const searchCallbacks = [];
const openCallbacks = [];
const timers = [];
let openCalls = 0;
let folderCalls = 0;

const chrome = {
  runtime: { lastError: null },
  i18n: {
    getMessage: (key) => key,
    getUILanguage: () => "en-US"
  },
  storage: {
    local: {
      get: (_key, callback) => callback({}),
      set: (_value, callback) => callback()
    }
  },
  downloads: {
    search: (_query, callback) => { searchCallbacks.push(callback); },
    open: (_id, callback) => { openCalls += 1; openCallbacks.push(callback); },
    showDefaultFolder: () => { folderCalls += 1; }
  }
};

const document = {
  documentElement: { lang: "" },
  getElementById: (id) => elements[id] || null,
  querySelectorAll: (selector) => {
    if (selector === "[data-panel]") return [elements["settings-tab"], elements["recent-tab"]];
    return [];
  },
  createElement: () => new FakeElement()
};

const context = {
  chrome,
  document,
  history: { replaceState() {} },
  location: { hash: "#recent" },
  Intl,
  Date,
  Number,
  Array,
  Promise,
  setTimeout: (callback, delay) => {
    timers.push({ callback, delay });
    return timers.length;
  },
  clearTimeout
};
vm.createContext(context);
const source = fs.readFileSync("ekstenzije/chrome/options.js", "utf8");
vm.runInContext(source, context, { filename: "options.js" });

const flush = () => new Promise((resolve) => setImmediate(resolve));

assert.equal(searchCallbacks.length, 1, "opening Recent should start one search");
elements["settings-tab"].dispatch("click");
elements["recent-tab"].dispatch("click");
assert.equal(searchCallbacks.length, 2, "returning to Recent should start a fresh search");

searchCallbacks[1]([makeItem(2, "SNAPVERE-visible-new.png")]);
await flush();
assert.equal(elements["recent-list"].children.length, 1);
assert.equal(elements["recent-list"].children[0].children[0].children[0].textContent, "SNAPVERE-visible-new.png");

searchCallbacks[0]([makeItem(1, "SNAPVERE-visible-stale.png")]);
await flush();
assert.equal(
  elements["recent-list"].children[0].children[0].children[0].textContent,
  "SNAPVERE-visible-new.png",
  "a stale earlier search must not overwrite the newest Recent result"
);

const openButton = elements["recent-list"].children[0].children[1];
openButton.dispatch("click");
openButton.dispatch("click");
assert.equal(
  searchCallbacks.length,
  3,
  "Open must perform exactly one click-time download revalidation while the button is busy"
);
assert.equal(openCalls, 0, "Open must not run before click-time revalidation succeeds");
assert.equal(openButton.disabled, true);

searchCallbacks[2]([makeItem(2, "SNAPVERE-visible-new.png")]);
await flush();
assert.equal(openCalls, 1, "a still-valid capture should open after revalidation");
openCallbacks.shift()();
await flush();
assert.equal(openButton.disabled, false);

elements["refresh-recent"].dispatch("click");
assert.equal(searchCallbacks.length, 4, "Refresh should request a new Recent snapshot");
searchCallbacks[3]([makeItem(3, "SNAPVERE-region-stale.png")]);
await flush();

const staleOpenButton = elements["recent-list"].children[0].children[1];
staleOpenButton.dispatch("click");
assert.equal(searchCallbacks.length, 5, "stale Open should revalidate the selected download");
searchCallbacks[4]([]);
await flush();
assert.equal(openCalls, 1, "a missing capture must not reach downloads.open");
assert.equal(
  searchCallbacks.length,
  6,
  "a stale capture should trigger a fresh Recent load so the stale row can disappear"
);
searchCallbacks[5]([]);
await flush();
await flush();
assert.equal(elements["recent-list"].children.length, 1);
assert.equal(elements["recent-list"].children[0].textContent, "noRecentCaptures");
assert.equal(elements["recent-status"].textContent, "openCaptureFailed");
assert.equal(elements["recent-status"].className, "status error");
assert.equal(staleOpenButton.disabled, false);

elements["open-downloads-folder"].dispatch("click");
elements["open-downloads-folder"].dispatch("click");
assert.equal(folderCalls, 1, "Open downloads folder must suppress duplicate activation during the cooldown");
await flush();
assert.equal(elements["open-downloads-folder"].disabled, true);
assert.equal(timers.length, 1);
assert.equal(timers[0].delay, 500);
timers.shift().callback();
assert.equal(elements["open-downloads-folder"].disabled, false);

console.log("SNAPVERE options runtime race/double-activation tests passed.");
