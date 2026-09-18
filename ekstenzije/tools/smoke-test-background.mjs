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
  const deferredRemovals = [];
  const deferredCaptures = [];
  let storageRemoveCount = 0;
  const activeTabSequence = Array.isArray(options.activeTabSequence) && options.activeTabSequence.length > 0
    ? [...options.activeTabSequence]
    : [7];
  let activeTabQueryIndex = 0;
  let messageListener = null;
  let commandListener = null;
  let tabRemovedListener = null;
  let tabUpdatedListener = null;

  const chrome = {
    runtime: {
      id: "snapvere-test-extension",
      lastError: null,
      onMessage: { addListener(listener) { messageListener = listener; } }
    },
    commands: {
      onCommand: { addListener(listener) { commandListener = listener; } }
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
          storageRemoveCount += 1;
          const requested = Array.isArray(keys) ? [...keys] : [keys];
          const apply = () => {
            for (const key of requested) delete storage[key];
            callback();
          };
          if (options.deferFirstStorageRemove === true && storageRemoveCount === 1) {
            deferredRemovals.push(apply);
            return;
          }
          apply();
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
      captureVisibleTab(windowId, captureOptions, callback) {
        assert.equal(windowId, 3);
        assert.equal(captureOptions?.format, 'png');
        captures.push(windowId);
        if (options.deferCapture === true) {
          deferredCaptures.push(() => callback(PNG));
          return;
        }
        callback(PNG);
      },
      sendMessage(_tabId, message, callback) {
        if (message?.type === 'FULL_PREP') {
          callback({ ok: true, totalWidth: 100, totalHeight: 100, viewportWidth: 100, viewportHeight: 100, devicePixelRatio: 1 });
          return;
        }
        if (message?.type === 'FULL_SCROLL') {
          callback({ ok: true, x: message.x, y: message.y });
          return;
        }
        if (message?.type === 'REGION_CROP') {
          callback({ ok: true, dataUrl: PNG });
          return;
        }
        callback({ ok: true });
      },
      onRemoved: { addListener(listener) { tabRemovedListener = listener; } },
      onUpdated: { addListener(listener) { tabUpdatedListener = listener; } }
    },
    scripting: { executeScript(_details, callback) { callback([]); } },
    downloads: {
      download(downloadOptions, callback) {
        downloads.push(structuredClone(downloadOptions));
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
      assert.equal(typeof messageListener, 'function');
      return messageListener;
    },
    command(command) {
      assert.equal(typeof commandListener, 'function');
      return commandListener(command);
    },
    removeTab(tabId) {
      assert.equal(typeof tabRemovedListener, 'function');
      tabRemovedListener(tabId, { windowId: 3, isWindowClosing: false });
    },
    updateTab(tabId, changeInfo) {
      assert.equal(typeof tabUpdatedListener, 'function');
      tabUpdatedListener(tabId, changeInfo, { id: tabId, windowId: 3 });
    },
    resolveDeferredRemove() {
      const operation = deferredRemovals.shift();
      assert.equal(typeof operation, 'function');
      operation();
    },
    resolveCapture() {
      const operation = deferredCaptures.shift();
      assert.equal(typeof operation, 'function');
      operation();
    }
  };
}

function send(listener, message, sender = { id: "snapvere-test-extension" }) {
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
    assert.equal(keepAlive, true);
  });
}

const flush = () => new Promise((resolve) => setImmediate(resolve));

async function runVariant(browser) {
  const source = fs.readFileSync(path.join(root, browser, 'background.js'), 'utf8');

  {
    const runtime = createRuntime({
      snapvereSettings: { filenamePrefix: 'OTHER/BRAND', saveAs: true }
    });
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });
    const response = await send(runtime.listener, { type: 'CAPTURE_VISIBLE' });
    assert.equal(response.ok, true);
    assert.equal(runtime.downloads.length, 1);
    assert.equal(runtime.downloads[0].saveAs, true);
    assert.equal(runtime.downloads[0].conflictAction, 'uniquify');
    assert.match(runtime.downloads[0].filename, /^SNAPVERE-visible-\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}\.png$/);
    assert.doesNotMatch(runtime.downloads[0].filename, /OTHER|BRAND/i);
    assert.equal(runtime.storage.snapvereActiveCapture, undefined);
  }

  {
    const runtime = createRuntime();
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });
    const result = await runtime.command('capture-visible');
    assert.equal(result.ok, true);
    assert.equal(runtime.downloads.length, 1);
    assert.match(runtime.downloads[0].filename, /^SNAPVERE-visible-/);
    assert.equal(runtime.storage.snapvereActiveCapture, undefined);
  }

  {
    const runtime = createRuntime();
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });
    const result = await runtime.command('capture-region');
    assert.equal(result.ok, true);
    assert.equal(result.pending, true);
    assert.equal(runtime.storage.snapvereActiveCapture?.kind, 'region');
    runtime.removeTab(7);
    await flush();
    assert.equal(runtime.storage.snapvereActiveCapture, undefined);
  }

  {
    const runtime = createRuntime();
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });
    const result = await runtime.command('capture-full-page');
    assert.equal(result.ok, true);
    assert.match(result.filename, /^SNAPVERE-full-page-/);
    assert.equal(runtime.captures.length, 1);
    assert.equal(runtime.storage.snapvereActiveCapture, undefined);
  }

  {
    const runtime = createRuntime();
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });
    const result = runtime.command('unknown-command');
    assert.equal(result, undefined);
    assert.equal(runtime.downloads.length, 0);
    assert.equal(runtime.storage.snapvereActiveCapture, undefined);
  }

  {
    const runtime = createRuntime({}, { activeTabSequence: [7, 9] });
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });
    const response = await send(runtime.listener, { type: 'CAPTURE_VISIBLE' });
    assert.equal(response.ok, false);
    assert.equal(response.errorKey, 'captureTabChanged');
    assert.equal(runtime.captures.length, 0);
    assert.equal(runtime.downloads.length, 0);
    assert.equal(runtime.storage.snapvereActiveCapture, undefined);
  }

  {
    const runtime = createRuntime({}, { activeTabSequence: [7, 7, 9] });
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });
    const response = await send(runtime.listener, { type: 'CAPTURE_VISIBLE' });
    assert.equal(response.ok, false);
    assert.equal(response.errorKey, 'captureTabChanged');
    assert.equal(runtime.captures.length, 1);
    assert.equal(runtime.downloads.length, 0);
    assert.equal(runtime.storage.snapvereActiveCapture, undefined);
  }

  {
    const runtime = createRuntime({
      snapvereActiveCapture: { token: 'existing', kind: 'visible', tabId: 7, windowId: 3, startedAt: Date.now() }
    });
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });
    const response = await send(runtime.listener, { type: 'CAPTURE_VISIBLE' });
    assert.equal(response.ok, false);
    assert.equal(response.errorKey, 'captureBusy');
    assert.equal(runtime.downloads.length, 0);
  }

  {
    const runtime = createRuntime({}, { activeTabSequence: [7, 9] });
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });
    const response = await send(runtime.listener, { type: 'CAPTURE_FULL' });
    assert.equal(response.ok, false);
    assert.equal(response.errorKey, 'captureTabChanged');
    assert.equal(runtime.captures.length, 0);
    assert.equal(runtime.storage.snapvereActiveCapture, undefined);
  }

  {
    const runtime = createRuntime();
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });
    const response = await send(runtime.listener, { type: 'CAPTURE_REGION' });
    assert.equal(response.ok, true);
    assert.equal(response.pending, true);
    assert.equal(runtime.storage.snapvereActiveCapture?.kind, 'region');
    runtime.removeTab(7);
    await flush();
    assert.equal(runtime.storage.snapvereActiveCapture, undefined);
    const visible = await send(runtime.listener, { type: 'CAPTURE_VISIBLE' });
    assert.equal(visible.ok, true);
  }

  {
    const runtime = createRuntime();
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });
    const response = await send(runtime.listener, { type: 'CAPTURE_REGION' });
    assert.equal(response.ok, true);
    runtime.updateTab(7, { status: 'loading', url: 'https://example.test/next' });
    await flush();
    assert.equal(runtime.storage.snapvereActiveCapture, undefined);
  }

  {
    const runtime = createRuntime({
      snapvereActiveCapture: {
        token: 'stale-region-token',
        kind: 'region',
        tabId: 7,
        windowId: 3,
        startedAt: Date.now() - (6 * 60 * 1000)
      }
    }, {
      deferFirstStorageRemove: true,
      deferCapture: true
    });
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });

    runtime.updateTab(7, { status: 'loading', url: 'https://example.test/reload' });
    await flush();

    const visiblePromise = send(runtime.listener, { type: 'CAPTURE_VISIBLE' });
    await flush();

    runtime.resolveDeferredRemove();
    await flush();

    assert.equal(
      runtime.storage.snapvereActiveCapture?.kind,
      'visible',
      'stale lock cleanup must not delete a newer capture lock'
    );

    runtime.resolveCapture();
    const visible = await visiblePromise;
    assert.equal(visible.ok, true);
    assert.equal(runtime.storage.snapvereActiveCapture, undefined);
  }

  {
    const runtime = createRuntime({}, { activeTabSequence: [7, 9] });
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });
    const pending = await send(runtime.listener, { type: 'CAPTURE_REGION' });
    assert.equal(pending.ok, true);
    const token = runtime.storage.snapvereActiveCapture?.token;
    const selected = await send(
      runtime.listener,
      { type: 'REGION_SELECTED', token, rect: { x: 10, y: 12, width: 80, height: 60 } },
      { id: "snapvere-test-extension", tab: { id: 7, windowId: 3 } }
    );
    assert.equal(selected.ok, false);
    assert.equal(selected.errorKey, 'captureTabChanged');
    assert.equal(runtime.captures.length, 0);
    assert.equal(runtime.downloads.length, 0);
    assert.equal(runtime.storage.snapvereActiveCapture, undefined);
  }

  {
    const runtime = createRuntime();
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });
    const response = await send(
      runtime.listener,
      { type: 'CAPTURE_VISIBLE' },
      { id: 'snapvere-test-extension', tab: { id: 7, windowId: 3 } }
    );
    assert.equal(response.ok, false);
    assert.equal(response.errorKey, 'captureFailed');
    assert.equal(runtime.captures.length, 0);
    assert.equal(runtime.downloads.length, 0);
    assert.equal(runtime.storage.snapvereActiveCapture, undefined);
  }

  {
    const runtime = createRuntime();
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });
    const response = await send(
      runtime.listener,
      { type: 'CAPTURE_VISIBLE' },
      { id: 'different-extension' }
    );
    assert.equal(response.ok, false);
    assert.equal(response.errorKey, 'captureFailed');
    assert.equal(runtime.captures.length, 0);
    assert.equal(runtime.downloads.length, 0);
  }

  {
    const runtime = createRuntime();
    vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });
    const response = await send(runtime.listener, { type: 'UNKNOWN_MESSAGE' });
    assert.equal(response.ok, false);
    assert.equal(response.errorKey, 'captureFailed');
  }

  console.log(`${browser}: background runtime smoke tests passed`);
}

for (const browser of browsers) {
  await runVariant(browser);
}

console.log('SNAPVERE browser background behavioral smoke tests passed for all variants.');
