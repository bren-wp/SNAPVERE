import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import { webcrypto } from 'node:crypto';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '..');
const browsers = ['chrome', 'edge', 'opera', 'firefox'];
const PNG = 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAAB';

function createRuntime(initialStorage = {}, options = {}) {
  const storage = structuredClone(initialStorage);
  const downloads = [];
  const captures = [];
  const activeTabSequence = Array.isArray(options.activeTabSequence) && options.activeTabSequence.length > 0
    ? [...options.activeTabSequence]
    : [7];
  let activeTabQueryIndex = 0;
  let messageListener = null;
  let tabRemovedListener = null;
  let tabUpdatedListener = null;

  const chrome = {
    runtime: {
      lastError: null,
      onMessage: {
        addListener(listener) {
          messageListener = listener;
        }
      }
    },
    storage: {
      local: {
        get(keys, callback) {
          const requested = Array.isArray(keys) ? keys : [keys];
          const result = {};
          for (const key of requested) {
            if (Object.hasOwn(storage, key)) result[key] = structuredClone(storage[key]);
          }
          callback(result);
        },
        set(items, callback) {
          Object.assign(storage, structuredClone(items));
          callback();
        },
        remove(keys, callback) {
          for (const key of Array.isArray(keys) ? keys : [keys]) delete storage[key];
          callback();
        }
      }
    },
    tabs: {
      query(_query, callback) {
        const index = Math.min(activeTabQueryIndex, activeTabSequence.length - 1);
        const tabId = activeTabSequence[index];
        activeTabQueryIndex += 1;
        callback([{ id: tabId, windowId: 3 }]);
      },
      captureVisibleTab(windowId, options, callback) {
        assert.equal(windowId, 3);
        assert.equal(options && options.format, 'png');
        captures.push(windowId);
        callback(PNG);
      },
      sendMessage(_tabId, message, callback) {
        if (message && message.type === 'FULL_PREP') {
          callback({
            ok: true,
            totalWidth: 100,
            totalHeight: 100,
            viewportWidth: 100,
            viewportHeight: 100,
            devicePixelRatio: 1
          });
          return;
        }
        if (message && message.type === 'FULL_SCROLL') {
          callback({ ok: true, x: message.x, y: message.y });
          return;
        }
        if (message && message.type === 'REGION_CROP') {
          callback({ ok: true, dataUrl: PNG });
          return;
        }
        callback({ ok: true });
      },
      onRemoved: {
        addListener(listener) {
          tabRemovedListener = listener;
        }
      },
      onUpdated: {
        addListener(listener) {
          tabUpdatedListener = listener;
        }
      }
    },
    scripting: {
      executeScript(_details, callback) {
        callback([]);
      }
    },
    downloads: {
      download(options, callback) {
        downloads.push(structuredClone(options));
        callback(101);
      }
    }
  };

  const context = vm.createContext({
    chrome,
    crypto: webcrypto,
    console,
    Promise,
    Date,
    Uint8Array,
    structuredClone
  });

  return {
    context,
    storage,
    downloads,
    captures,
    get listener() {
      assert.equal(typeof messageListener, 'function', 'background.js must register a runtime message listener');
      return messageListener;
    },
    removeTab(tabId) {
      assert.equal(typeof tabRemovedListener, 'function', 'background.js must observe tab removal');
      tabRemovedListener(tabId, { windowId: 3, isWindowClosing: false });
    },
    updateTab(tabId, changeInfo) {
      assert.equal(typeof tabUpdatedListener, 'function', 'background.js must observe tab navigation');
      tabUpdatedListener(tabId, changeInfo, { id: tabId, windowId: 3 });
    }
  };
}

function send(listener, message, sender = {}) {
  return new Promise((resolve, reject) => {
    let settled = false;
    const timeout = setTimeout(() => {
      if (!settled) reject(new Error(`Background response timed out for ${message.type}`));
    }, 1500);

    const keepAlive = listener(message, sender, (response) => {
      settled = true;
      clearTimeout(timeout);
      resolve(response);
    });
    assert.equal(keepAlive, true, 'background listener must keep the async response channel alive');
  });
}

function flushBackgroundTasks() {
  return new Promise((resolve) => setImmediate(resolve));
}

async function runVariant(browser) {
  const source = fs.readFileSync(path.join(root, browser, 'background.js'), 'utf8');

  {
    const runtime = createRuntime({
      snapvereSettings: {
        filenamePrefix: 'SNAP/VERE',
        saveAs: true
      }
    });
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });

    const response = await send(runtime.listener, { type: 'CAPTURE_VISIBLE' });
    assert.equal(response.ok, true, `${browser}: visible capture should succeed; response=${JSON.stringify(response)}`);
    assert.equal(runtime.downloads.length, 1, `${browser}: exactly one download should start`);
    assert.equal(runtime.downloads[0].url, PNG, `${browser}: PNG data URL must reach downloads API`);
    assert.equal(runtime.downloads[0].saveAs, true, `${browser}: saveAs setting must be respected`);
    assert.equal(runtime.downloads[0].conflictAction, 'uniquify');
    assert.match(runtime.downloads[0].filename, /^SNAP-VERE-visible-\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}\.png$/);
    assert.equal(runtime.storage.snapvereActiveCapture, undefined, `${browser}: capture lock must be released`);
  }

  {
    const runtime = createRuntime({}, { activeTabSequence: [7, 9] });
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });

    const response = await send(runtime.listener, { type: 'CAPTURE_VISIBLE' });
    assert.equal(response.ok, false, `${browser}: visible capture must fail if the active tab changes before frame capture`);
    assert.equal(response.errorKey, 'captureTabChanged');
    assert.equal(runtime.captures.length, 0, `${browser}: changed-tab visible capture must not capture another tab`);
    assert.equal(runtime.downloads.length, 0, `${browser}: changed-tab visible capture must not download`);
    assert.equal(runtime.storage.snapvereActiveCapture, undefined, `${browser}: changed-tab visible capture must release the lock`);
  }

  {
    const runtime = createRuntime({}, { activeTabSequence: [7, 7, 9] });
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });

    const response = await send(runtime.listener, { type: 'CAPTURE_VISIBLE' });
    assert.equal(response.ok, false, `${browser}: visible capture must discard a frame if activation changes during captureVisibleTab`);
    assert.equal(response.errorKey, 'captureTabChanged');
    assert.equal(runtime.captures.length, 1, `${browser}: race test must acquire exactly one frame before post-validation rejects it`);
    assert.equal(runtime.downloads.length, 0, `${browser}: post-validation failure must never download the raced frame`);
    assert.equal(runtime.storage.snapvereActiveCapture, undefined, `${browser}: raced visible capture must release the lock`);
  }

  {
    const runtime = createRuntime({
      snapvereActiveCapture: {
        token: 'existing',
        kind: 'visible',
        tabId: 7,
        windowId: 3,
        startedAt: Date.now()
      }
    });
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });

    const response = await send(runtime.listener, { type: 'CAPTURE_VISIBLE' });
    assert.equal(response.ok, false, `${browser}: concurrent capture must be rejected`);
    assert.equal(response.errorKey, 'captureBusy');
    assert.equal(runtime.downloads.length, 0, `${browser}: busy capture must not download`);
  }

  {
    const runtime = createRuntime({}, { activeTabSequence: [7, 9] });
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });

    const response = await send(runtime.listener, { type: 'CAPTURE_FULL' });
    assert.equal(response.ok, false, `${browser}: full-page capture must stop if another tab becomes active`);
    assert.equal(response.errorKey, 'captureTabChanged');
    assert.equal(runtime.captures.length, 0, `${browser}: full-page capture must not capture a frame from another tab`);
    assert.equal(runtime.storage.snapvereActiveCapture, undefined, `${browser}: changed-tab full-page capture must release the lock`);
  }

  {
    const runtime = createRuntime({}, { activeTabSequence: [7, 7, 9] });
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });

    const response = await send(runtime.listener, { type: 'CAPTURE_FULL' });
    assert.equal(response.ok, false, `${browser}: full-page capture must discard a tile if activation changes during captureVisibleTab`);
    assert.equal(response.errorKey, 'captureTabChanged');
    assert.equal(runtime.captures.length, 1, `${browser}: full-page race test must acquire one frame before rejecting it`);
    assert.equal(runtime.downloads.length, 0, `${browser}: raced full-page frame must not produce a download`);
    assert.equal(runtime.storage.snapvereActiveCapture, undefined, `${browser}: raced full-page capture must release the lock`);
  }

  {
    const runtime = createRuntime();
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });

    const response = await send(runtime.listener, { type: 'CAPTURE_REGION' });
    assert.equal(response.ok, true, `${browser}: region capture should enter pending state`);
    assert.equal(response.pending, true);
    assert.equal(runtime.storage.snapvereActiveCapture?.kind, 'region');

    runtime.removeTab(7);
    await flushBackgroundTasks();
    assert.equal(runtime.storage.snapvereActiveCapture, undefined, `${browser}: closing a tab must release its pending region lock`);

    const visible = await send(runtime.listener, { type: 'CAPTURE_VISIBLE' });
    assert.equal(visible.ok, true, `${browser}: a new capture must be allowed after tab-close cleanup`);
  }

  {
    const runtime = createRuntime();
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });

    const response = await send(runtime.listener, { type: 'CAPTURE_REGION' });
    assert.equal(response.ok, true, `${browser}: region capture should enter pending state before navigation`);
    assert.equal(runtime.storage.snapvereActiveCapture?.kind, 'region');

    runtime.updateTab(7, { status: 'loading', url: 'https://example.test/next' });
    await flushBackgroundTasks();
    assert.equal(runtime.storage.snapvereActiveCapture, undefined, `${browser}: navigation must release a pending region lock`);
  }

  {
    const runtime = createRuntime({}, { activeTabSequence: [7, 9] });
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });

    const pending = await send(runtime.listener, { type: 'CAPTURE_REGION' });
    assert.equal(pending.ok, true, `${browser}: region capture should enter pending state before tab-switch validation`);
    const token = runtime.storage.snapvereActiveCapture?.token;
    assert.equal(typeof token, 'string');

    const selected = await send(
      runtime.listener,
      {
        type: 'REGION_SELECTED',
        token,
        rect: { x: 10, y: 12, width: 80, height: 60 }
      },
      { tab: { id: 7, windowId: 3 } }
    );
    assert.equal(selected.ok, false, `${browser}: region capture must fail if another tab becomes active before frame capture`);
    assert.equal(selected.errorKey, 'captureTabChanged');
    assert.equal(runtime.captures.length, 0, `${browser}: changed-tab region capture must not capture another tab`);
    assert.equal(runtime.downloads.length, 0, `${browser}: changed-tab region capture must not download`);
    assert.equal(runtime.storage.snapvereActiveCapture, undefined, `${browser}: changed-tab region capture must release the lock`);
  }

  {
    const runtime = createRuntime({}, { activeTabSequence: [7, 7, 9] });
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });

    const pending = await send(runtime.listener, { type: 'CAPTURE_REGION' });
    assert.equal(pending.ok, true, `${browser}: region race test should enter pending state`);
    const token = runtime.storage.snapvereActiveCapture?.token;
    assert.equal(typeof token, 'string');

    const selected = await send(
      runtime.listener,
      {
        type: 'REGION_SELECTED',
        token,
        rect: { x: 10, y: 12, width: 80, height: 60 }
      },
      { tab: { id: 7, windowId: 3 } }
    );
    assert.equal(selected.ok, false, `${browser}: region capture must discard a frame if activation changes during captureVisibleTab`);
    assert.equal(selected.errorKey, 'captureTabChanged');
    assert.equal(runtime.captures.length, 1, `${browser}: region race test must acquire one frame before post-validation rejects it`);
    assert.equal(runtime.downloads.length, 0, `${browser}: raced region frame must not download`);
    assert.equal(runtime.storage.snapvereActiveCapture, undefined, `${browser}: raced region capture must release the lock`);
  }

  {
    const runtime = createRuntime();
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });
    const response = await send(runtime.listener, { type: 'UNKNOWN_MESSAGE' });
    assert.equal(response.ok, false, `${browser}: unknown message must fail safely`);
    assert.equal(response.errorKey, 'captureFailed');
  }

  console.log(`${browser}: background runtime smoke tests passed`);
}

for (const browser of browsers) {
  await runVariant(browser);
}

console.log('SNAPVERE browser background behavioral smoke tests passed for all variants.');
