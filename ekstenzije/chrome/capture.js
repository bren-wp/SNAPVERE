(() => {
  "use strict";

  if (globalThis.__snapvereCaptureInjected) return;
  globalThis.__snapvereCaptureInjected = true;

  const MIN_REGION_SIZE = 8;
  const FULL_WATCHDOG_MS = 90_000;
  const MAX_CANVAS_DIMENSION = 32767;
  const MAX_TOTAL_PIXELS = 60_000_000;
  const MAX_STORED_TILES = 60;

  let regionState = null;
  let fullState = null;

  function localized(key, fallback) {
    try {
      return chrome.i18n.getMessage(key) || fallback;
    } catch {
      return fallback;
    }
  }

  function validToken(value) {
    return typeof value === "string" && value.length >= 16 && value.length <= 128;
  }

  function safeError(error) {
    return error && typeof error.message === "string" ? error.message : "Capture failed.";
  }

  function sendRuntime(message) {
    return new Promise((resolve, reject) => {
      try {
        chrome.runtime.sendMessage(message, (response) => {
          const error = chrome.runtime.lastError;
          if (error) reject(new Error(error.message));
          else resolve(response);
        });
      } catch (error) {
        reject(error);
      }
    });
  }

  function clamp(value, min, max) {
    return Math.min(max, Math.max(min, value));
  }

  function loadImage(dataUrl) {
    return new Promise((resolve, reject) => {
      if (typeof dataUrl !== "string" || !dataUrl.startsWith("data:image/png;base64,")) {
        reject(new Error("Invalid PNG data."));
        return;
      }

      const image = new Image();
      image.onload = () => resolve(image);
      image.onerror = () => reject(new Error("Unable to decode captured PNG."));
      image.src = dataUrl;
    });
  }

  function canvasToBlob(canvas) {
    return new Promise((resolve, reject) => {
      canvas.toBlob((blob) => {
        if (blob) resolve(blob);
        else reject(new Error("Unable to encode PNG."));
      }, "image/png");
    });
  }

  function addOverlayStyle() {
    if (document.getElementById("snapvere-capture-style")) return;

    const style = document.createElement("style");
    style.id = "snapvere-capture-style";
    style.textContent = `
      #snapvere-region-overlay {
        position: fixed !important;
        inset: 0 !important;
        z-index: 2147483647 !important;
        cursor: crosshair !important;
        background: rgba(6, 8, 18, .48) !important;
        user-select: none !important;
        touch-action: none !important;
      }
      #snapvere-region-overlay * { box-sizing: border-box !important; }
      #snapvere-region-selection {
        position: absolute !important;
        display: none;
        border: 2px solid #a774ff !important;
        background: rgba(122, 82, 255, .12) !important;
        box-shadow: 0 0 0 99999px rgba(5, 7, 16, .38), 0 0 0 1px rgba(255,255,255,.2) inset !important;
        pointer-events: none !important;
      }
      #snapvere-region-hint {
        position: absolute !important;
        left: 50% !important;
        top: 18px !important;
        transform: translateX(-50%) !important;
        max-width: calc(100vw - 32px) !important;
        padding: 10px 14px !important;
        border: 1px solid rgba(170, 132, 255, .42) !important;
        border-radius: 12px !important;
        background: rgba(15, 17, 31, .94) !important;
        color: #f6f2ff !important;
        font: 600 13px/1.35 system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif !important;
        letter-spacing: .01em !important;
        box-shadow: 0 12px 32px rgba(0,0,0,.32) !important;
        pointer-events: none !important;
        text-align: center !important;
      }
    `;
    (document.head || document.documentElement).appendChild(style);
  }

  function removeOverlayStyle() {
    const style = document.getElementById("snapvere-capture-style");
    if (style) style.remove();
  }

  function cleanupRegion() {
    if (!regionState) {
      removeOverlayStyle();
      return;
    }

    const { root, onPointerDown, onPointerMove, onPointerUp, onKeyDown } = regionState;
    root.removeEventListener("pointerdown", onPointerDown, true);
    root.removeEventListener("pointermove", onPointerMove, true);
    root.removeEventListener("pointerup", onPointerUp, true);
    window.removeEventListener("keydown", onKeyDown, true);
    if (root.isConnected) root.remove();
    regionState = null;
    removeOverlayStyle();
  }

  async function notifyRegionCancelled(token) {
    try {
      await sendRuntime({ type: "REGION_CANCELLED", token });
    } catch {
      // The local overlay is already cleaned even if the background is unavailable.
    }
  }

  function startRegion(token) {
    if (!validToken(token)) throw new Error("Invalid region capture token.");
    cleanupRegion();
    addOverlayStyle();

    const root = document.createElement("div");
    root.id = "snapvere-region-overlay";
    root.setAttribute("role", "presentation");

    const selection = document.createElement("div");
    selection.id = "snapvere-region-selection";

    const hint = document.createElement("div");
    hint.id = "snapvere-region-hint";
    hint.textContent = localized("regionInstruction", "Drag to select an area. Press Esc to cancel.");

    root.append(selection, hint);
    document.documentElement.appendChild(root);

    let dragging = false;
    let startX = 0;
    let startY = 0;
    let pointerId = null;

    const draw = (x, y) => {
      const left = Math.min(startX, x);
      const top = Math.min(startY, y);
      const width = Math.abs(x - startX);
      const height = Math.abs(y - startY);
      selection.style.display = "block";
      selection.style.left = `${left}px`;
      selection.style.top = `${top}px`;
      selection.style.width = `${width}px`;
      selection.style.height = `${height}px`;
      return { x: left, y: top, width, height };
    };

    const onPointerDown = (event) => {
      if (event.button !== 0 || dragging) return;
      event.preventDefault();
      event.stopPropagation();
      dragging = true;
      pointerId = event.pointerId;
      startX = clamp(event.clientX, 0, window.innerWidth);
      startY = clamp(event.clientY, 0, window.innerHeight);
      try {
        root.setPointerCapture(pointerId);
      } catch {
        // Pointer capture is best-effort; listeners still track movement on the overlay.
      }
      draw(startX, startY);
    };

    const onPointerMove = (event) => {
      if (!dragging || (pointerId !== null && event.pointerId !== pointerId)) return;
      event.preventDefault();
      event.stopPropagation();
      draw(
        clamp(event.clientX, 0, window.innerWidth),
        clamp(event.clientY, 0, window.innerHeight)
      );
    };

    const onPointerUp = (event) => {
      if (!dragging || (pointerId !== null && event.pointerId !== pointerId)) return;
      event.preventDefault();
      event.stopPropagation();

      const rect = draw(
        clamp(event.clientX, 0, window.innerWidth),
        clamp(event.clientY, 0, window.innerHeight)
      );
      dragging = false;

      try {
        root.releasePointerCapture(pointerId);
      } catch {
        // No-op.
      }
      pointerId = null;

      if (rect.width < MIN_REGION_SIZE || rect.height < MIN_REGION_SIZE) {
        hint.textContent = localized("regionTooSmall", "Select a larger area.");
        selection.style.display = "none";
        return;
      }

      cleanupRegion();
      void sendRuntime({
        type: "REGION_SELECTED",
        token,
        rect
      }).catch(() => undefined);
    };

    const onKeyDown = (event) => {
      if (event.key !== "Escape") return;
      event.preventDefault();
      event.stopPropagation();
      cleanupRegion();
      void notifyRegionCancelled(token);
    };

    regionState = { root, onPointerDown, onPointerMove, onPointerUp, onKeyDown };
    root.addEventListener("pointerdown", onPointerDown, true);
    root.addEventListener("pointermove", onPointerMove, true);
    root.addEventListener("pointerup", onPointerUp, true);
    window.addEventListener("keydown", onKeyDown, true);
  }

  async function cropRegion(token, rect, dataUrl) {
    if (!validToken(token)) throw new Error("Invalid region capture token.");
    if (!rect || ![rect.x, rect.y, rect.width, rect.height].every(Number.isFinite)) {
      throw new Error("Invalid region geometry.");
    }

    const image = await loadImage(dataUrl);
    const scaleX = image.naturalWidth / Math.max(1, window.innerWidth);
    const scaleY = image.naturalHeight / Math.max(1, window.innerHeight);

    const sourceX = Math.max(0, Math.floor(rect.x * scaleX));
    const sourceY = Math.max(0, Math.floor(rect.y * scaleY));
    const sourceWidth = Math.min(image.naturalWidth - sourceX, Math.max(1, Math.round(rect.width * scaleX)));
    const sourceHeight = Math.min(image.naturalHeight - sourceY, Math.max(1, Math.round(rect.height * scaleY)));

    if (
      sourceWidth <= 0 ||
      sourceHeight <= 0 ||
      sourceWidth > MAX_CANVAS_DIMENSION ||
      sourceHeight > MAX_CANVAS_DIMENSION ||
      sourceWidth * sourceHeight > MAX_TOTAL_PIXELS
    ) {
      throw new Error("Selected region exceeds safe canvas limits.");
    }

    const canvas = document.createElement("canvas");
    canvas.width = sourceWidth;
    canvas.height = sourceHeight;
    const context = canvas.getContext("2d", { alpha: false });
    if (!context) throw new Error("2D canvas is unavailable.");

    context.drawImage(
      image,
      sourceX,
      sourceY,
      sourceWidth,
      sourceHeight,
      0,
      0,
      sourceWidth,
      sourceHeight
    );
    return canvas.toDataURL("image/png");
  }

  function fullDimensions() {
    const root = document.documentElement;
    const body = document.body;
    const widths = [
      root.scrollWidth,
      root.offsetWidth,
      root.clientWidth,
      body ? body.scrollWidth : 0,
      body ? body.offsetWidth : 0
    ];
    const heights = [
      root.scrollHeight,
      root.offsetHeight,
      root.clientHeight,
      body ? body.scrollHeight : 0,
      body ? body.offsetHeight : 0
    ];

    return {
      totalWidth: Math.max(...widths),
      totalHeight: Math.max(...heights),
      viewportWidth: window.innerWidth,
      viewportHeight: window.innerHeight,
      devicePixelRatio: window.devicePixelRatio || 1
    };
  }

  function identifyFloatingElements() {
    const items = [];
    const elements = document.body ? document.body.querySelectorAll("*") : [];
    let scanned = 0;
    for (const element of elements) {
      scanned += 1;
      if (scanned > 5000) break;
      const style = getComputedStyle(element);
      if (style.position !== "fixed" && style.position !== "sticky") continue;
      if (style.display === "none" || style.visibility === "hidden" || Number(style.opacity) === 0) continue;

      const rect = element.getBoundingClientRect();
      if (rect.width <= 0 || rect.height <= 0) continue;
      items.push({
        element,
        visibility: element.style.visibility
      });
      if (items.length >= 250) break;
    }
    return items;
  }

  function scheduleFullWatchdog() {
    if (!fullState) return;
    if (fullState.watchdog) clearTimeout(fullState.watchdog);
    fullState.watchdog = setTimeout(() => {
      cleanupFull();
    }, FULL_WATCHDOG_MS);
  }

  function ensureFullToken(token) {
    if (!fullState || fullState.token !== token) {
      throw new Error("Full-page capture session is stale.");
    }
    scheduleFullWatchdog();
  }

  function cleanupFull() {
    if (!fullState) return;

    if (fullState.watchdog) clearTimeout(fullState.watchdog);
    for (const item of fullState.floating) {
      if (item.element && item.element.isConnected) {
        item.element.style.visibility = item.visibility;
      }
    }

    document.documentElement.style.scrollBehavior = fullState.rootScrollBehavior;
    if (document.body) document.body.style.scrollBehavior = fullState.bodyScrollBehavior;

    const x = fullState.originalX;
    const y = fullState.originalY;
    const tiles = fullState.tiles;
    fullState = null;

    for (const tile of tiles) {
      if (tile.image) tile.image.src = "";
    }
    window.scrollTo(x, y);
  }

  function prepFull(token) {
    if (!validToken(token)) throw new Error("Invalid full-page capture token.");
    cleanupFull();

    const dimensions = fullDimensions();
    if (
      !Number.isFinite(dimensions.totalWidth) ||
      !Number.isFinite(dimensions.totalHeight) ||
      dimensions.totalWidth <= 0 ||
      dimensions.totalHeight <= 0 ||
      dimensions.viewportWidth <= 0 ||
      dimensions.viewportHeight <= 0
    ) {
      throw new Error("Page dimensions are invalid.");
    }

    fullState = {
      token,
      originalX: window.scrollX,
      originalY: window.scrollY,
      rootScrollBehavior: document.documentElement.style.scrollBehavior,
      bodyScrollBehavior: document.body ? document.body.style.scrollBehavior : "",
      floating: identifyFloatingElements(),
      floatingHidden: false,
      tiles: [],
      viewportWidth: dimensions.viewportWidth,
      viewportHeight: dimensions.viewportHeight,
      totalWidth: dimensions.totalWidth,
      totalHeight: dimensions.totalHeight,
      watchdog: null
    };

    document.documentElement.style.scrollBehavior = "auto";
    if (document.body) document.body.style.scrollBehavior = "auto";
    scheduleFullWatchdog();
    return dimensions;
  }

  function afterPaint(delay = 70) {
    return new Promise((resolve) => {
      requestAnimationFrame(() => {
        requestAnimationFrame(() => setTimeout(resolve, delay));
      });
    });
  }

  async function fullScroll(token, x, y) {
    ensureFullToken(token);
    if (!Number.isFinite(x) || !Number.isFinite(y)) throw new Error("Invalid scroll target.");
    window.scrollTo({
      left: clamp(x, 0, Math.max(0, fullState.totalWidth - fullState.viewportWidth)),
      top: clamp(y, 0, Math.max(0, fullState.totalHeight - fullState.viewportHeight)),
      behavior: "auto"
    });
    await afterPaint();
    ensureFullToken(token);
    return { x: window.scrollX, y: window.scrollY };
  }

  function hideFloating(token) {
    ensureFullToken(token);
    if (fullState.floatingHidden) return;
    for (const item of fullState.floating) {
      if (item.element && item.element.isConnected) {
        item.element.style.visibility = "hidden";
      }
    }
    fullState.floatingHidden = true;
  }

  async function storeTile(token, dataUrl, x, y) {
    ensureFullToken(token);
    if (!Number.isFinite(x) || !Number.isFinite(y)) throw new Error("Invalid tile position.");
    if (fullState.tiles.length >= MAX_STORED_TILES) {
      throw new Error("Full-page capture exceeded the tile limit.");
    }

    const image = await loadImage(dataUrl);
    ensureFullToken(token);
    fullState.tiles.push({ image, x, y });
    return { count: fullState.tiles.length };
  }

  async function assembleFull(token, filename) {
    ensureFullToken(token);
    if (typeof filename !== "string" || !filename.toLowerCase().endsWith(".png") || filename.length > 128) {
      throw new Error("Invalid download filename.");
    }
    if (fullState.tiles.length === 0) throw new Error("No full-page capture tiles were collected.");

    const first = fullState.tiles[0].image;
    const scaleX = first.naturalWidth / Math.max(1, fullState.viewportWidth);
    const scaleY = first.naturalHeight / Math.max(1, fullState.viewportHeight);
    const width = Math.ceil(fullState.totalWidth * scaleX);
    const height = Math.ceil(fullState.totalHeight * scaleY);

    if (
      width <= 0 ||
      height <= 0 ||
      width > MAX_CANVAS_DIMENSION ||
      height > MAX_CANVAS_DIMENSION ||
      width * height > MAX_TOTAL_PIXELS
    ) {
      throw new Error("Full-page capture exceeds safe canvas limits.");
    }

    const canvas = document.createElement("canvas");
    canvas.width = width;
    canvas.height = height;
    const context = canvas.getContext("2d", { alpha: false });
    if (!context) throw new Error("2D canvas is unavailable.");

    context.fillStyle = "#ffffff";
    context.fillRect(0, 0, width, height);

    for (const tile of fullState.tiles) {
      const destinationX = Math.round(tile.x * scaleX);
      const destinationY = Math.round(tile.y * scaleY);
      context.drawImage(tile.image, destinationX, destinationY);
    }

    const blob = await canvasToBlob(canvas);
    const url = URL.createObjectURL(blob);
    try {
      const link = document.createElement("a");
      link.href = url;
      link.download = filename;
      link.rel = "noopener";
      link.style.display = "none";
      document.documentElement.appendChild(link);
      link.click();
      link.remove();
    } finally {
      setTimeout(() => URL.revokeObjectURL(url), 15_000);
      canvas.width = 1;
      canvas.height = 1;
      cleanupFull();
    }
  }

  async function handle(message) {
    if (!message || typeof message.type !== "string") {
      throw new Error("Invalid capture message.");
    }

    switch (message.type) {
      case "REGION_START":
        startRegion(message.token);
        return {};
      case "REGION_CROP":
        return { dataUrl: await cropRegion(message.token, message.rect, message.dataUrl) };
      case "FULL_PREP":
        return prepFull(message.token);
      case "FULL_SCROLL":
        return fullScroll(message.token, Number(message.x), Number(message.y));
      case "FULL_HIDE_FLOATING":
        hideFloating(message.token);
        return {};
      case "FULL_STORE_TILE":
        return storeTile(message.token, message.dataUrl, Number(message.x), Number(message.y));
      case "FULL_ASSEMBLE":
        await assembleFull(message.token, message.filename);
        return {};
      case "FULL_CLEANUP":
        if (fullState && fullState.token === message.token) cleanupFull();
        return {};
      default:
        throw new Error("Unsupported capture message.");
    }
  }

  chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
    const accepted = new Set([
      "REGION_START",
      "REGION_CROP",
      "FULL_PREP",
      "FULL_SCROLL",
      "FULL_HIDE_FLOATING",
      "FULL_STORE_TILE",
      "FULL_ASSEMBLE",
      "FULL_CLEANUP"
    ]);
    if (!message || !accepted.has(message.type)) return false;

    Promise.resolve(handle(message))
      .then((result) => sendResponse({ ok: true, ...(result || {}) }))
      .catch((error) => sendResponse({
        ok: false,
        errorKey: "captureFailed",
        message: safeError(error)
      }));
    return true;
  });
})();