(() => {
  "use strict";

  const LOCK_KEY = "snapvereActiveCapture";
  const SETTINGS_KEY = "snapvereSettings";
  const LOCK_TTL_MS = 5 * 60 * 1000;
  const MAX_TILES = 60;
  const MAX_CANVAS_DIMENSION = 32767;
  const MAX_TOTAL_PIXELS = 60_000_000;
  const MAX_REGION_DATA_URL = 64 * 1024 * 1024;
  let startQueue = Promise.resolve();

  class SnapvereError extends Error {
    constructor(key, message) {
      super(message || key);
      this.name = "SnapvereError";
      this.key = key;
    }
  }

  function runtimeError() {
    return chrome.runtime.lastError ? new Error(chrome.runtime.lastError.message) : null;
  }

  function invoke(api, method, ...args) {
    return new Promise((resolve, reject) => {
      let settled = false;
      const callback = (value) => {
        if (settled) return;
        settled = true;
        const error = runtimeError();
        if (error) reject(error);
        else resolve(value);
      };

      try {
        const maybePromise = api[method](...args, callback);
        if (maybePromise && typeof maybePromise.then === "function") {
          maybePromise.then((value) => {
            if (!settled) {
              settled = true;
              resolve(value);
            }
          }, (error) => {
            if (!settled) {
              settled = true;
              reject(error);
            }
          });
        }
      } catch (error) {
        if (!settled) {
          settled = true;
          reject(error);
        }
      }
    });
  }

  const storageGet = (keys) => invoke(chrome.storage.local, "get", keys);
  const storageSet = (items) => invoke(chrome.storage.local, "set", items);
  const storageRemove = (keys) => invoke(chrome.storage.local, "remove", keys);

  function serializeStart(task) {
    const run = startQueue.then(task, task);
    startQueue = run.catch(() => undefined);
    return run;
  }

  function makeToken() {
    if (globalThis.crypto && typeof globalThis.crypto.randomUUID === "function") {
      return globalThis.crypto.randomUUID();
    }
    const bytes = new Uint8Array(16);
    globalThis.crypto.getRandomValues(bytes);
    return Array.from(bytes, (value) => value.toString(16).padStart(2, "0")).join("");
  }

  async function getActiveTab() {
    const tabs = await invoke(chrome.tabs, "query", { active: true, currentWindow: true });
    const tab = Array.isArray(tabs) ? tabs[0] : null;
    if (!tab || !Number.isInteger(tab.id) || !Number.isInteger(tab.windowId)) {
      throw new SnapvereError("captureFailed", "No active browser tab is available.");
    }
    return tab;
  }

  async function getLock() {
    const values = await storageGet(LOCK_KEY);
    const lock = values ? values[LOCK_KEY] : null;
    if (!lock || typeof lock !== "object") return null;
    if (!Number.isFinite(lock.startedAt) || Date.now() - lock.startedAt > LOCK_TTL_MS) {
      await storageRemove(LOCK_KEY);
      return null;
    }
    return lock;
  }

  async function acquireLock(kind, tab) {
    const existing = await getLock();
    if (existing) {
      throw new SnapvereError("captureBusy", "Another SNAPVERE capture is already active.");
    }

    const lock = {
      token: makeToken(),
      kind,
      tabId: tab.id,
      windowId: tab.windowId,
      startedAt: Date.now()
    };
    await storageSet({ [LOCK_KEY]: lock });
    return lock;
  }

  async function releaseLock(token) {
    const current = await getLock();
    if (current && current.token === token) {
      await storageRemove(LOCK_KEY);
    }
  }

  async function readSettings() {
    const values = await storageGet(SETTINGS_KEY);
    const raw = values && values[SETTINGS_KEY] && typeof values[SETTINGS_KEY] === "object"
      ? values[SETTINGS_KEY]
      : {};

    const prefixCandidate = typeof raw.filenamePrefix === "string" ? raw.filenamePrefix.trim() : "SNAPVERE";
    const filenamePrefix = sanitizePrefix(prefixCandidate || "SNAPVERE");
    return {
      filenamePrefix,
      saveAs: raw.saveAs === true
    };
  }

  function sanitizePrefix(value) {
    const safe = String(value)
      .normalize("NFKC")
      .replace(/[<>:"/\\|?*\u0000-\u001F]/g, "-")
      .replace(/\s+/g, " ")
      .trim()
      .slice(0, 48);
    return safe || "SNAPVERE";
  }

  function timestamp() {
    const now = new Date();
    const pad = (value) => String(value).padStart(2, "0");
    return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}_${pad(now.getHours())}-${pad(now.getMinutes())}-${pad(now.getSeconds())}`;
  }

  async function buildFilename(kind) {
    const settings = await readSettings();
    return {
      filename: `${settings.filenamePrefix}-${kind}-${timestamp()}.png`,
      saveAs: settings.saveAs
    };
  }

  async function downloadDataUrl(dataUrl, kind) {
    if (typeof dataUrl !== "string" || !dataUrl.startsWith("data:image/png;base64,")) {
      throw new SnapvereError("captureFailed", "Capture did not produce a PNG data URL.");
    }

    const { filename, saveAs } = await buildFilename(kind);
    const id = await invoke(chrome.downloads, "download", {
      url: dataUrl,
      filename,
      saveAs,
      conflictAction: "uniquify"
    });
    if (!Number.isInteger(id)) {
      throw new SnapvereError("captureFailed", "Browser did not accept the PNG download.");
    }
    return filename;
  }

  async function captureVisible(windowId) {
    try {
      return await invoke(chrome.tabs, "captureVisibleTab", windowId, { format: "png" });
    } catch (error) {
      throw new SnapvereError("unsupportedPage", error && error.message);
    }
  }

  async function ensureCaptureScript(tabId) {
    try {
      await invoke(chrome.scripting, "executeScript", {
        target: { tabId },
        files: ["capture.js"]
      });
    } catch (error) {
      throw new SnapvereError("unsupportedPage", error && error.message);
    }
  }

  async function sendTab(tabId, message) {
    try {
      const response = await invoke(chrome.tabs, "sendMessage", tabId, message);
      if (!response || response.ok !== true) {
        const key = response && typeof response.errorKey === "string" ? response.errorKey : "captureFailed";
        throw new SnapvereError(key, response && response.message);
      }
      return response;
    } catch (error) {
      if (error instanceof SnapvereError) throw error;
      throw new SnapvereError("captureFailed", error && error.message);
    }
  }

  function positions(total, viewport) {
    if (!Number.isFinite(total) || !Number.isFinite(viewport) || total <= 0 || viewport <= 0) {
      throw new SnapvereError("captureFailed", "Invalid page dimensions.");
    }
    if (total <= viewport) return [0];

    const values = [];
    const max = Math.max(0, total - viewport);
    for (let value = 0; value < max; value += viewport) {
      values.push(value);
      if (values.length > MAX_TILES) {
        throw new SnapvereError("fullPageTooLarge", "Page requires too many capture tiles.");
      }
    }
    if (values[values.length - 1] !== max) values.push(max);
    return values;
  }

  async function startVisibleCapture() {
    const tab = await getActiveTab();
    const lock = await acquireLock("visible", tab);
    try {
      const dataUrl = await captureVisible(tab.windowId);
      const filename = await downloadDataUrl(dataUrl, "visible");
      return { ok: true, filename };
    } finally {
      await releaseLock(lock.token);
    }
  }

  async function startRegionCapture() {
    const tab = await getActiveTab();
    const lock = await acquireLock("region", tab);
    try {
      await ensureCaptureScript(tab.id);
      await sendTab(tab.id, { type: "REGION_START", token: lock.token });
      return { ok: true, pending: true };
    } catch (error) {
      await releaseLock(lock.token);
      throw error;
    }
  }

  async function startFullPageCapture() {
    const tab = await getActiveTab();
    const lock = await acquireLock("full", tab);
    let prepared = false;

    try {
      await ensureCaptureScript(tab.id);
      const prep = await sendTab(tab.id, { type: "FULL_PREP", token: lock.token });
      prepared = true;

      const totalWidth = Number(prep.totalWidth);
      const totalHeight = Number(prep.totalHeight);
      const viewportWidth = Number(prep.viewportWidth);
      const viewportHeight = Number(prep.viewportHeight);
      const dpr = Math.max(1, Math.min(4, Number(prep.devicePixelRatio) || 1));

      const pixelWidth = Math.ceil(totalWidth * dpr);
      const pixelHeight = Math.ceil(totalHeight * dpr);
      if (
        pixelWidth > MAX_CANVAS_DIMENSION ||
        pixelHeight > MAX_CANVAS_DIMENSION ||
        pixelWidth * pixelHeight > MAX_TOTAL_PIXELS
      ) {
        throw new SnapvereError("fullPageTooLarge", "Page exceeds the safe full-page canvas limit.");
      }

      const xs = positions(totalWidth, viewportWidth);
      const ys = positions(totalHeight, viewportHeight);
      if (xs.length * ys.length > MAX_TILES) {
        throw new SnapvereError("fullPageTooLarge", "Page requires too many capture tiles.");
      }

      let index = 0;
      for (const y of ys) {
        for (const x of xs) {
          const scrolled = await sendTab(tab.id, {
            type: "FULL_SCROLL",
            token: lock.token,
            x,
            y
          });

          const dataUrl = await captureVisible(tab.windowId);
          await sendTab(tab.id, {
            type: "FULL_STORE_TILE",
            token: lock.token,
            dataUrl,
            x: Number(scrolled.x) || 0,
            y: Number(scrolled.y) || 0
          });

          index += 1;
          if (index === 1 && xs.length * ys.length > 1) {
            await sendTab(tab.id, { type: "FULL_HIDE_FLOATING", token: lock.token });
          }
        }
      }

      const { filename } = await buildFilename("full-page");
      await sendTab(tab.id, {
        type: "FULL_ASSEMBLE",
        token: lock.token,
        filename
      });
      return { ok: true, filename };
    } finally {
      if (prepared) {
        try {
          await sendTab(tab.id, { type: "FULL_CLEANUP", token: lock.token });
        } catch {
          // The content script also has its own watchdog/cleanup path.
        }
      }
      await releaseLock(lock.token);
    }
  }

  async function handleRegionSelected(message, sender) {
    const tabId = sender && sender.tab && sender.tab.id;
    const windowId = sender && sender.tab && sender.tab.windowId;
    if (!Number.isInteger(tabId) || !Number.isInteger(windowId)) {
      throw new SnapvereError("captureFailed", "Region result did not originate from a browser tab.");
    }

    const lock = await getLock();
    if (
      !lock ||
      lock.kind !== "region" ||
      lock.token !== message.token ||
      lock.tabId !== tabId ||
      lock.windowId !== windowId
    ) {
      throw new SnapvereError("captureFailed", "Region capture session is stale.");
    }

    try {
      const rect = message.rect;
      if (
        !rect ||
        ![rect.x, rect.y, rect.width, rect.height].every(Number.isFinite) ||
        rect.width < 8 ||
        rect.height < 8
      ) {
        throw new SnapvereError("regionTooSmall", "Selected region is too small.");
      }

      const dataUrl = await captureVisible(windowId);
      const cropped = await sendTab(tabId, {
        type: "REGION_CROP",
        token: lock.token,
        rect,
        dataUrl
      });

      if (
        typeof cropped.dataUrl !== "string" ||
        cropped.dataUrl.length > MAX_REGION_DATA_URL ||
        !cropped.dataUrl.startsWith("data:image/png;base64,")
      ) {
        throw new SnapvereError("captureFailed", "Region crop result is invalid or too large.");
      }

      const filename = await downloadDataUrl(cropped.dataUrl, "region");
      return { ok: true, filename };
    } finally {
      await releaseLock(lock.token);
    }
  }

  async function handleRegionCancelled(message, sender) {
    const tabId = sender && sender.tab && sender.tab.id;
    const lock = await getLock();
    if (lock && lock.kind === "region" && lock.token === message.token && lock.tabId === tabId) {
      await releaseLock(lock.token);
    }
    return { ok: true, cancelled: true };
  }

  async function dispatch(message, sender) {
    if (!message || typeof message.type !== "string") {
      throw new SnapvereError("captureFailed", "Invalid extension message.");
    }

    switch (message.type) {
      case "CAPTURE_VISIBLE":
        return serializeStart(startVisibleCapture);
      case "CAPTURE_FULL":
        return serializeStart(startFullPageCapture);
      case "CAPTURE_REGION":
        return serializeStart(startRegionCapture);
      case "REGION_SELECTED":
        return handleRegionSelected(message, sender);
      case "REGION_CANCELLED":
        return handleRegionCancelled(message, sender);
      default:
        throw new SnapvereError("captureFailed", "Unsupported extension message.");
    }
  }

  chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    Promise.resolve(dispatch(message, sender))
      .then((result) => sendResponse(result))
      .catch((error) => {
        const errorKey = error instanceof SnapvereError ? error.key : "captureFailed";
        sendResponse({
          ok: false,
          errorKey,
          message: error && typeof error.message === "string" ? error.message : errorKey
        });
      });
    return true;
  });
})();