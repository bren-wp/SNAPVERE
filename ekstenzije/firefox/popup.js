(() => {
  "use strict";

  const status = document.getElementById("status");
  const buttons = Array.from(document.querySelectorAll("[data-action]"));
  const settingsButton = document.getElementById("settings");

  function t(key, fallback = "") {
    return chrome.i18n.getMessage(key) || fallback || key;
  }

  function localize() {
    document.documentElement.lang = chrome.i18n.getUILanguage().toLowerCase().startsWith("hr") ? "hr" : "en";
    for (const node of document.querySelectorAll("[data-i18n]")) {
      const key = node.getAttribute("data-i18n");
      node.textContent = t(key);
    }
    for (const node of document.querySelectorAll("[data-i18n-aria-label]")) {
      node.setAttribute("aria-label", t(node.getAttribute("data-i18n-aria-label")));
    }
  }

  function setBusy(busy) {
    for (const button of buttons) button.disabled = busy;
    settingsButton.disabled = busy;
  }

  function setStatus(key, kind = "") {
    status.textContent = t(key);
    status.className = `status${kind ? ` ${kind}` : ""}`;
  }

  function runtimeError() {
    return chrome.runtime.lastError ? new Error(chrome.runtime.lastError.message) : null;
  }

  function send(message) {
    return new Promise((resolve, reject) => {
      try {
        chrome.runtime.sendMessage(message, (response) => {
          const error = runtimeError();
          if (error) reject(error);
          else resolve(response);
        });
      } catch (error) {
        reject(error);
      }
    });
  }

  function progressKey(action) {
    if (action === "CAPTURE_VISIBLE") return "capturingVisible";
    if (action === "CAPTURE_FULL") return "capturingFull";
    return "selectRegion";
  }

  async function runCapture(action) {
    setBusy(true);
    setStatus(progressKey(action));

    try {
      const response = await send({ type: action });
      if (!response || response.ok !== true) {
        const errorKey = response && typeof response.errorKey === "string" ? response.errorKey : "captureFailed";
        setStatus(errorKey, "error");
        return;
      }

      if (response.pending) {
        setStatus("selectRegion");
        window.close();
        return;
      }

      setStatus("saved", "success");
    } catch {
      setStatus("captureFailed", "error");
    } finally {
      setBusy(false);
    }
  }

  for (const button of buttons) {
    button.addEventListener("click", () => {
      const action = button.getAttribute("data-action");
      if (action) void runCapture(action);
    });
  }

  settingsButton.addEventListener("click", () => {
    chrome.runtime.openOptionsPage();
  });

  localize();
  setStatus("ready");
})();