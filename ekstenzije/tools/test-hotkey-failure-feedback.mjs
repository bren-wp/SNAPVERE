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

function loadMessages(browser, locale) {
  return JSON.parse(fs.readFileSync(path.join(root, browser, '_locales', locale, 'messages.json'), 'utf8'));
}

function createRuntime(messages, scenario = {}) {
  const storage = structuredClone(scenario.storage || {});
  const activeTabSequence = scenario.activeTabSequence || [7, 7, 7, 7];
  let activeTabIndex = 0;
  let commandListener = null;
  const warnings = [];
  const timers = [];
  const actionState = {
    badgeTexts: [],
    badgeColors: [],
    titles: []
  };

  const chrome = {
    runtime: {
      lastError: null,
      onMessage: { addListener() {} }
    },
    i18n: {
      getMessage(key) {
        return messages[key]?.message || '';
      }
    },
    action: {
      setBadgeText(details, callback) {
        actionState.badgeTexts.push(details.text);
        callback();
      },
      setBadgeBackgroundColor(details, callback) {
        actionState.badgeColors.push(details.color);
        callback();
      },
      setTitle(details, callback) {
        actionState.titles.push(details.title);
        callback();
      }
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
          for (const key of Array.isArray(keys) ? keys : [keys]) delete storage[key];
          callback();
        }
      }
    },
    tabs: {
      query(_query, callback) {
        const index = Math.min(activeTabIndex, activeTabSequence.length - 1);
        const tabId = activeTabSequence[index];
        activeTabIndex += 1;
        callback([{ id: tabId, windowId: 3 }]);
      },
      captureVisibleTab(_windowId, _options, callback) {
        if (scenario.captureVisibleError) {
          chrome.runtime.lastError = { message: scenario.captureVisibleError };
          try { callback(); } finally { chrome.runtime.lastError = null; }
          return;
        }
        callback(PNG);
      },
      sendMessage(_tabId, message, callback) {
        if (message?.type === 'FULL_PREP') {
          callback({
            ok: true,
            totalWidth: scenario.fullPageTooLarge ? 40000 : 100,
            totalHeight: 100,
            viewportWidth: 100,
            viewportHeight: 100,
            devicePixelRatio: 1
          });
          return;
        }
        callback({ ok: true, x: message?.x || 0, y: message?.y || 0 });
      },
      onRemoved: { addListener() {} },
      onUpdated: { addListener() {} }
    },
    scripting: {
      executeScript(_details, callback) {
        if (scenario.scriptError) {
          chrome.runtime.lastError = { message: scenario.scriptError };
          try { callback(); } finally { chrome.runtime.lastError = null; }
          return;
        }
        callback([]);
      }
    },
    downloads: {
      download(_options, callback) {
        callback(scenario.invalidDownload ? null : 101);
      }
    }
  };

  const context = vm.createContext({
    chrome,
    crypto: webcrypto,
    Promise,
    Date,
    Uint8Array,
    structuredClone,
    console: {
      warn(...args) { warnings.push(args); }
    },
    setTimeout(callback, delay) {
      timers.push({ callback, delay });
      return timers.length;
    }
  });

  return {
    context,
    storage,
    warnings,
    timers,
    actionState,
    command(command) {
      assert.equal(typeof commandListener, 'function');
      return commandListener(command);
    }
  };
}

async function exerciseFailure(browser, locale, expectedKey, command, scenario = {}) {
  const messages = loadMessages(browser, locale);
  const source = fs.readFileSync(path.join(root, browser, 'background.js'), 'utf8');
  const runtime = createRuntime(messages, scenario);
  vm.runInContext(source, runtime.context, { filename: `${browser}/background.js` });

  const result = await runtime.command(command);
  assert.deepEqual(result, { ok: false, errorKey: expectedKey });
  assert.equal(runtime.actionState.badgeTexts.at(-1), '!');
  assert.equal(runtime.actionState.titles.at(-1), messages[expectedKey].message);
  assert.equal(runtime.actionState.badgeColors.at(-1), '#B3261E');
  assert.equal(runtime.timers.length, 1);
  assert.equal(runtime.timers[0].delay, 5000);
  assert.equal(runtime.warnings.length, 1);
  assert.equal(runtime.warnings[0].length, 1);
  assert.equal(runtime.warnings[0][0], `SNAPVERE command ${command} failed: ${expectedKey}`);

  const serialized = JSON.stringify({ result, actionState: runtime.actionState, warnings: runtime.warnings });
  assert.doesNotMatch(serialized, /RAW_INTERNAL_EXTENSION_FAILURE/);

  runtime.timers[0].callback();
  await Promise.resolve();
  await Promise.resolve();
  assert.equal(runtime.actionState.badgeTexts.at(-1), '');
  assert.equal(runtime.actionState.titles.at(-1), messages.actionTitle.message);
}

for (const browser of browsers) {
  await exerciseFailure(browser, 'en', 'captureBusy', 'capture-visible', {
    storage: {
      snapvereActiveCapture: {
        token: 'existing',
        kind: 'visible',
        tabId: 7,
        windowId: 3,
        startedAt: Date.now()
      }
    }
  });

  await exerciseFailure(browser, 'en', 'unsupportedPage', 'capture-region', {
    scriptError: 'RAW_INTERNAL_EXTENSION_FAILURE'
  });

  await exerciseFailure(browser, 'en', 'captureTabChanged', 'capture-visible', {
    activeTabSequence: [7, 9]
  });

  await exerciseFailure(browser, 'en', 'fullPageTooLarge', 'capture-full-page', {
    fullPageTooLarge: true
  });

  await exerciseFailure(browser, 'en', 'captureFailed', 'capture-visible', {
    invalidDownload: true
  });

  await exerciseFailure(browser, 'hr', 'captureBusy', 'capture-visible', {
    storage: {
      snapvereActiveCapture: {
        token: 'existing',
        kind: 'visible',
        tabId: 7,
        windowId: 3,
        startedAt: Date.now()
      }
    }
  });

  console.log(`${browser}: hotkey failure feedback tests passed`);
}

console.log('SNAPVERE hotkey failure feedback is sanitized and localized across all browser variants.');
