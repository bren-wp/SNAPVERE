(() => {
  "use strict";

  const SETTINGS_KEY = "snapvereSettings";
  const RECENT_LIMIT = 10;
  const form = document.getElementById("settings-form");
  const saveAsInput = document.getElementById("save-as");
  const settingsSubmit = form.querySelector('button[type="submit"]');
  const settingsStatus = document.getElementById("settings-status");
  const recentStatus = document.getElementById("recent-status");
  const recentList = document.getElementById("recent-list");
  const refreshRecent = document.getElementById("refresh-recent");
  const openDownloadsFolder = document.getElementById("open-downloads-folder");
  const tabs = Array.from(document.querySelectorAll("[data-panel]"));
  let recentLoadGeneration = 0;
  let activePanelId = null;

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

  async function invokeWithoutCallback(api, method, ...args) {
    const result = api[method](...args);
    return result && typeof result.then === "function" ? await result : result;
  }

  async function getLocal(key) {
    return (await invoke(chrome.storage.local, "get", key)) || {};
  }

  function setLocal(value) {
    return invoke(chrome.storage.local, "set", value);
  }

  async function searchDownloads(query) {
    const items = await invoke(chrome.downloads, "search", query);
    return Array.isArray(items) ? items : [];
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

  function setSettingsInteractive(interactive) {
    saveAsInput.disabled = !interactive;
    if (settingsSubmit) settingsSubmit.disabled = !interactive;
    form.setAttribute("aria-busy", interactive ? "false" : "true");
  }

  async function initializeSettings() {
    setSettingsInteractive(false);
    setStatus(settingsStatus, t("loadingSettings"));
    let loaded = false;
    try {
      await loadSettings();
      loaded = true;
      setStatus(settingsStatus, "");
    } catch {
      setStatus(settingsStatus, t("settingsLoadFailed"), true);
    } finally {
      // Never allow a failed initial read to be overwritten by the default
      // checkbox state. Reloading the Options page gives storage another
      // chance to recover without mutating existing settings.
      setSettingsInteractive(loaded);
    }
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

  async function openDownload(item, button) {
    if (!button || button.disabled) return;
    button.disabled = true;
    setStatus(recentStatus, "");
    try {
      const matches = await searchDownloads({ id: item.id });
      const current = matches.find((candidate) => candidate.id === item.id);
      if (!isSnapvereCapture(current)) {
        await loadRecent();
        setStatus(recentStatus, t("openCaptureFailed"), true);
        return;
      }

      await invoke(chrome.downloads, "open", current.id);
    } catch {
      setStatus(recentStatus, t("openCaptureFailed"), true);
    } finally {
      if (button.isConnected) button.disabled = false;
    }
  }

  async function openDefaultDownloadsFolder() {
    if (openDownloadsFolder.disabled) return;
    openDownloadsFolder.disabled = true;
    setStatus(recentStatus, "");
    try {
      await invokeWithoutCallback(chrome.downloads, "showDefaultFolder");
    } catch {
      setStatus(recentStatus, t("openDownloadsFolderFailed"), true);
    } finally {
      setTimeout(() => {
        openDownloadsFolder.disabled = false;
      }, 500);
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
      open.addEventListener("click", () => void openDownload(item, open));

      row.append(copy, open);
      recentList.appendChild(row);
    }
  }

  async function loadRecent() {
    const generation = ++recentLoadGeneration;
    refreshRecent.disabled = true;
    recentList.setAttribute("aria-busy", "true");
    setStatus(recentStatus, t("loadingRecent"));
    try {
      const items = await searchDownloads({ orderBy: ["-startTime"], limit: 100 });
      if (generation !== recentLoadGeneration || activePanelId !== "recent-panel") return;
      const recent = items.filter(isSnapvereCapture).slice(0, RECENT_LIMIT);
      renderRecent(recent);
      setStatus(recentStatus, recent.length ? t("recentReady") : "");
    } catch {
      if (generation !== recentLoadGeneration || activePanelId !== "recent-panel") return;
      recentList.replaceChildren();
      setStatus(recentStatus, t("recentLoadFailed"), true);
    } finally {
      if (generation === recentLoadGeneration) {
        refreshRecent.disabled = false;
        recentList.setAttribute("aria-busy", "false");
      }
    }
  }

  function showPanel(panelId, updateHash = true, focusTab = false) {
    const previousPanelId = activePanelId;
    activePanelId = panelId;
    if (previousPanelId === "recent-panel" && panelId !== "recent-panel") {
      recentLoadGeneration += 1;
      refreshRecent.disabled = false;
      recentList.setAttribute("aria-busy", "false");
    }

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
    if (panelId === "recent-panel" && previousPanelId !== "recent-panel") void loadRecent();
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
    if (!settingsSubmit || settingsSubmit.disabled) return;

    const saveAs = saveAsInput.checked === true;
    setSettingsInteractive(false);
    setStatus(settingsStatus, "");
    try {
      await setLocal({ [SETTINGS_KEY]: { saveAs } });
      setStatus(settingsStatus, t("settingsSaved"));
    } catch {
      setStatus(settingsStatus, t("settingsSaveFailed"), true);
    } finally {
      setSettingsInteractive(true);
    }
  });

  for (const tab of tabs) {
    tab.addEventListener("click", () => showPanel(tab.dataset.panel));
    tab.addEventListener("keydown", handleTabKeydown);
  }
  refreshRecent.addEventListener("click", () => void loadRecent());
  openDownloadsFolder.addEventListener("click", () => void openDefaultDownloadsFolder());

  localize();
  void initializeSettings();
  showPanel(location.hash === "#recent" ? "recent-panel" : "settings-panel", false);
})();
