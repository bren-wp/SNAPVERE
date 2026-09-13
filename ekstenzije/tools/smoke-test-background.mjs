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

function createRuntime(initialStorage = {}) {
  const storage = structuredClone(initialStorage);
  const downloads = [];
  let messageListener = null;

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
        callback([{ id: 7, windowId: 3 }]);
      },
      captureVisibleTab(windowId, options, callback) {
        assert.equal(windowId, 3);
        assert.deepEqual(options, { format: 'png' });
        callback(PNG);
      },
      sendMessage(_tabId, _message, callback) {
        callback({ ok: true });
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
    get listener() {
      assert.equal(typeof messageListener, 'function', 'background.js must register a runtime message listener');
      return messageListener;
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
    assert.equal(response.ok, true, `${browser}: visible capture should succeed`);
    assert.equal(runtime.downloads.length, 1, `${browser}: exactly one download should start`);
    assert.equal(runtime.downloads[0].url, PNG, `${browser}: PNG data URL must reach downloads API`);
    assert.equal(runtime.downloads[0].saveAs, true, `${browser}: saveAs setting must be respected`);
    assert.equal(runtime.downloads[0].conflictAction, 'uniquify');
    assert.match(runtime.downloads[0].filename, /^SNAP-VERE-visible-\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}\.png$/);
    assert.equal(runtime.storage.snapvereActiveCapture, undefined, `${browser}: capture lock must be released`);
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
