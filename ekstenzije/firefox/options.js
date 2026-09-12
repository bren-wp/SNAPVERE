(() => {
  "use strict";

  const SETTINGS_KEY = "snapvereSettings";
  const form = document.getElementById("settings-form");
  const prefixInput = document.getElementById("filename-prefix");
  const saveAsInput = document.getElementById("save-as");
  const status = document.getElementById("status");

  function t(key, fallback = "") {
    return chrome.i18n.getMessage(key) || fallback || key;
  }

  function localize() {
    document.documentElement.lang = chrome.i18n.getUILanguage().toLowerCase().startsWith("hr") ? "hr" : "en";
    for (const node of document.querySelectorAll("[data-i18n]")) {
      node.textContent = t(node.getAttribute("data-i18n"));
    }
  }

  function sanitizePrefix(value) {
    return String(value)
      .normalize("NFKC")
      .replace(/[<>:"/\\|?*\u0000-\u001F]/g, "-")
      .replace(/\s+/g, " ")
      .trim()
      .slice(0, 48);
  }

  function getLocal(key) {
    return new Promise((resolve, reject) => {
      chrome.storage.local.get(key, (value) => {
        const error = chrome.runtime.lastError;
        if (error) reject(new Error(error.message));
        else resolve(value || {});
      });
    });
  }

  function setLocal(value) {
    return new Promise((resolve, reject) => {
      chrome.storage.local.set(value, () => {
        const error = chrome.runtime.lastError;
        if (error) reject(new Error(error.message));
        else resolve();
      });
    });
  }

  async function load() {
    const values = await getLocal(SETTINGS_KEY);
    const settings = values[SETTINGS_KEY] || {};
    prefixInput.value = typeof settings.filenamePrefix === "string" && settings.filenamePrefix.trim()
      ? settings.filenamePrefix
      : "SNAPVERE";
    saveAsInput.checked = settings.saveAs === true;
  }

  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    status.className = "";
    const filenamePrefix = sanitizePrefix(prefixInput.value);
    if (!filenamePrefix) {
      status.textContent = t("invalidPrefix");
      status.className = "error";
      prefixInput.focus();
      return;
    }

    try {
      await setLocal({
        [SETTINGS_KEY]: {
          filenamePrefix,
          saveAs: saveAsInput.checked === true
        }
      });
      prefixInput.value = filenamePrefix;
      status.textContent = t("settingsSaved");
    } catch {
      status.textContent = t("captureFailed");
      status.className = "error";
    }
  });

  localize();
  load().catch(() => {
    status.textContent = t("captureFailed");
    status.className = "error";
  });
})();