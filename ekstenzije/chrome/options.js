(() => {
  "use strict";

  const SETTINGS_KEY = "snapvereSettings";
  const RECENT_LIMIT = 10;
  const form = document.getElementById("settings-form");
  const saveAsInput = document.getElementById("save-as");
  const settingsStatus = document.getElementById("settings-status");
  const recentStatus = document.getElementById("recent-status");
  const recentList = document.getElementById("recent-list");
  const refreshRecent = document.getElementById("refresh-recent");
  const openDownloadsFolder = document.getElementById("open-downloads-folder");
  const tabs = Array.from(document.querySelectorAll("[data-panel]"));

  function t(key, substitutions) {
    return chrome.i18n.getMessage(key, substitutions) || key;
  }

  function localize() {
    document.documentElement.lang = chrome.i18n.getUILanguage().toLowerCase().startsWith("hr") ? "hr" : "en";
    for (const node of document.querySelectorAll("[data-i18n]")) {
      node.textContent = t(node.getAttribute("data-i18n"));
    }
    for (const node of document.querySelectorAll("[data-i18n-aria-label]")) {
      node.setAttribute("aria-label", t(node.getAttribute("data-i18n-aria-label")));
    }
  }

  function runtimeError() {
    return chrome.runtime.lastError ? new Error(chrome.runtime.lastError.message) : null;
  }

  function getLocal(key) {
    return new Promise((resolve, reject) => {
      chrome.storage.local.get(key, (value) => {
        const error = runtimeError();
        if (error) reject(error);
        else resolve(value || {});
      });
    });
  }

  function setLocal(value) {
    return new Promise((resolve, reject) => {
      chrome.storage.local.set(value, () => {
        const error = runtimeError();
        if (error) reject(error);
        else resolve();
      });
    });
  }

  function searchDownloads(query) {
    return new Promise((resolve, reject) => {
      chrome.downloads.search(query, (items) => {
        const error = runtimeError();
        if (error) reject(error);
        else resolve(Array.isArray(items) ? items : []);
      });
    });
  }

  function setStatus(node, message, error = false) {
    node.textContent = message;
    node.className = `status${error ? " error" : ""}`;
  }

  async function loadSettings() {
    const values = await getLocal(SETTINGS_KEY);
    const settings = values[SETTINGS_KEY] || {};
    saveAsInput.checked = settings.saveAs === true;
  }

  function isSnapvereCapture(item) {
    if (
      !item ||
      !Number.isInteger(item.id) ||
      item.state !== "complete" ||
      item.exists === false ||
      typeof item.filename !== "string"
    ) {
      return false;
    }
    const fileName = item.filename.split(/[\\/]/).pop() || "";
    return /^SNAPVERE-(?:visible|region|full-page)-.+\.png$/i.test(fileName);
  }

  function formatBytes(value) {
    if (!Number.isFinite(value) || value <= 0) return "";
    if (value < 1024 * 1024) return `${Math.max(1, Math.round(value / 1024))} KB`;
    return `${(value / (1024 * 1024)).toFixed(1)} MB`;
  }

  function captureMeta(item) {
    const parts = [];
    if (item.startTime) {
      const date = new Date(item.startTime);
      if (!Number.isNaN(date.getTime())) {
        parts.push(new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(date));
      }
    }
    const size = formatBytes(Number(item.fileSize || item.totalBytes));
    if (size) parts.push(size);
    return parts.join(" · ");
  }

  function openDownload(item) {
    try {
      const result = chrome.downloads.open(item.id);
      if (result && typeof result.catch === "function") {
        result.catch(() => setStatus(recentStatus, t("openCaptureFailed"), true));
      }
    } catch {
      setStatus(recentStatus, t("openCaptureFailed"), true);
    }
  }

  function openDefaultDownloadsFolder() {
    try {
      const result = chrome.downloads.showDefaultFolder();
      if (result && typeof result.catch === "function") {
        result.catch(() => setStatus(recentStatus, t("openDownloadsFolderFailed"), true));
      }
    } catch {
      setStatus(recentStatus, t("openDownloadsFolderFailed"), true);
    }
  }

  function renderRecent(items) {
    recentList.replaceChildren();
    if (items.length === 0) {
      const empty = document.createElement("div");
      empty.className = "empty";
      empty.textContent = t("noRecentCaptures");
      recentList.appendChild(empty);
      return;
    }

    for (const item of items) {
      const row = document.createElement("div");
      row.className = "recent-item";

      const copy = document.createElement("div");
      copy.className = "recent-copy";
      const name = document.createElement("div");
      name.className = "recent-name";
      name.textContent = item.filename.split(/[\\/]/).pop() || t("recentCapture");
      name.title = name.textContent;
      const meta = document.createElement("div");
      meta.className = "recent-meta";
      meta.textContent = captureMeta(item);
      copy.append(name, meta);

      const open = document.createElement("button");
      open.type = "button";
      open.className = "recent-open";
      open.textContent = t("openCapture");
      open.addEventListener("click", () => openDownload(item));

      row.append(copy, open);
      recentList.appendChild(row);
    }
  }

  async function loadRecent() {
    refreshRecent.disabled = true;
    setStatus(recentStatus, t("loadingRecent"));
    try {
      const items = await searchDownloads({ orderBy: ["-startTime"], limit: 100 });
      const recent = items.filter(isSnapvereCapture).slice(0, RECENT_LIMIT);
      renderRecent(recent);
      setStatus(recentStatus, recent.length ? t("recentReady") : "");
    } catch {
      recentList.replaceChildren();
      setStatus(recentStatus, t("recentLoadFailed"), true);
    } finally {
      refreshRecent.disabled = false;
    }
  }

  function showPanel(panelId, updateHash = true, focusTab = false) {
    let activeTab = null;
    for (const tab of tabs) {
      const active = tab.dataset.panel === panelId;
      tab.classList.toggle("active", active);
      tab.setAttribute("aria-selected", active ? "true" : "false");
      tab.tabIndex = active ? 0 : -1;
      if (active) activeTab = tab;
      const panel = document.getElementById(tab.dataset.panel);
      if (panel) panel.hidden = !active;
    }

    if (updateHash) {
      history.replaceState(null, "", panelId === "recent-panel" ? "#recent" : "#settings");
    }
    if (focusTab && activeTab) activeTab.focus();
    if (panelId === "recent-panel") void loadRecent();
  }

  function handleTabKeydown(event) {
    const currentIndex = tabs.indexOf(event.currentTarget);
    if (currentIndex < 0 || tabs.length === 0) return;

    let targetIndex = null;
    switch (event.key) {
      case "ArrowRight":
        targetIndex = (currentIndex + 1) % tabs.length;
        break;
      case "ArrowLeft":
        targetIndex = (currentIndex - 1 + tabs.length) % tabs.length;
        break;
      case "Home":
        targetIndex = 0;
        break;
      case "End":
        targetIndex = tabs.length - 1;
        break;
      default:
        return;
    }

    event.preventDefault();
    showPanel(tabs[targetIndex].dataset.panel, true, true);
  }

  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    setStatus(settingsStatus, "");
    try {
      await setLocal({ [SETTINGS_KEY]: { saveAs: saveAsInput.checked === true } });
      setStatus(settingsStatus, t("settingsSaved"));
    } catch {
      setStatus(settingsStatus, t("settingsSaveFailed"), true);
    }
  });

  for (const tab of tabs) {
    tab.addEventListener("click", () => showPanel(tab.dataset.panel));
    tab.addEventListener("keydown", handleTabKeydown);
  }
  refreshRecent.addEventListener("click", () => void loadRecent());
  openDownloadsFolder.addEventListener("click", openDefaultDownloadsFolder);

  localize();
  loadSettings().catch(() => setStatus(settingsStatus, t("settingsLoadFailed"), true));
  showPanel(location.hash === "#recent" ? "recent-panel" : "settings-panel", false);
})();
