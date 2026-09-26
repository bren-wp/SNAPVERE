import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import vm from "node:vm";

const browsers = ["chrome", "edge", "opera", "firefox"];
const PNG = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAAB";
const TOKEN = "0123456789abcdef";

function createRuntime() {
  let listener = null;
  const pendingImages = [];
  const drawCalls = [];

  class FakeImage {
    constructor() {
      this.onload = null;
      this.onerror = null;
      this.naturalWidth = 200;
      this.naturalHeight = 100;
      this._src = "";
    }

    set src(value) {
      this._src = value;
      if (value) pendingImages.push(this);
    }

    get src() {
      return this._src;
    }
  }

  const documentElement = {
    style: {},
    appendChild() {}
  };

  const document = {
    documentElement,
    body: null,
    getElementById() { return null; },
    createElement(tag) {
      if (tag !== "canvas") {
        return {
          id: "",
          style: {},
          setAttribute() {},
          append() {},
          remove() {},
          addEventListener() {},
          removeEventListener() {},
          isConnected: true
        };
      }

      return {
        width: 0,
        height: 0,
        getContext() {
          return {
            drawImage(...args) { drawCalls.push(args); }
          };
        },
        toDataURL() { return PNG; },
        toBlob(callback) { callback(new Blob(["png"], { type: "image/png" })); }
      };
    }
  };

  const windowObject = {
    innerWidth: 100,
    innerHeight: 50,
    scrollX: 0,
    scrollY: 0,
    devicePixelRatio: 2,
    addEventListener() {},
    removeEventListener() {},
    scrollTo() {}
  };

  const chrome = {
    i18n: { getMessage: () => "" },
    runtime: {
      lastError: null,
      sendMessage(_message, callback) { callback({ ok: true }); },
      onMessage: {
        addListener(value) { listener = value; }
      }
    }
  };

  const context = vm.createContext({
    chrome,
    document,
    window: windowObject,
    Image: FakeImage,
    URL: {
      createObjectURL: () => "blob:snapvere-test",
      revokeObjectURL() {}
    },
    Blob,
    Promise,
    Number,
    Math,
    Set,
    Error,
    Uint8Array,
    setTimeout,
    clearTimeout,
    requestAnimationFrame: (callback) => callback()
  });

  return {
    context,
    window: windowObject,
    pendingImages,
    drawCalls,
    get listener() {
      assert.equal(typeof listener, "function");
      return listener;
    }
  };
}

function send(listener, message) {
  return new Promise((resolve) => {
    const keepAlive = listener(message, {}, resolve);
    assert.equal(keepAlive, true);
  });
}

for (const browser of browsers) {
  const runtime = createRuntime();
  const source = fs.readFileSync(path.join("ekstenzije", browser, "capture.js"), "utf8");
  vm.runInContext(source, runtime.context, { filename: `${browser}/capture.js` });

  const validPromise = send(runtime.listener, {
    type: "REGION_CROP",
    token: TOKEN,
    rect: { x: 10, y: 5, width: 20, height: 10 },
    dataUrl: PNG,
    viewportWidth: 100,
    viewportHeight: 50
  });
  assert.equal(runtime.pendingImages.length, 1);
  runtime.pendingImages.shift().onload();
  const valid = await validPromise;
  assert.equal(valid.ok, true);
  assert.equal(valid.dataUrl, PNG);
  assert.equal(runtime.drawCalls.length, 1);

  runtime.window.innerWidth = 100;
  runtime.window.innerHeight = 50;
  const resizedPromise = send(runtime.listener, {
    type: "REGION_CROP",
    token: TOKEN,
    rect: { x: 10, y: 5, width: 20, height: 10 },
    dataUrl: PNG,
    viewportWidth: 100,
    viewportHeight: 50
  });
  assert.equal(runtime.pendingImages.length, 1);
  runtime.window.innerWidth = 90;
  runtime.pendingImages.shift().onload();
  const resized = await resizedPromise;
  assert.equal(resized.ok, false);
  assert.equal(resized.errorKey, "captureFailed");
  assert.match(resized.message, /viewport changed/i);

  console.log(`${browser}: capture runtime viewport tests passed`);
}

console.log("SNAPVERE capture runtime tests passed for all variants.");
