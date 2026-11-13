import test from "node:test";
import assert from "node:assert/strict";
import { DEFAULTS, normalizeSettings } from "../src/windows/settings/preferences.ts";

test("settings recover safely from corrupt or incomplete persisted data", () => {
  assert.deepEqual(normalizeSettings(null), DEFAULTS);
  const out = normalizeSettings({
    theme: "invalid", format: "exe", recordFormat: "avi",
    quality: Number.NaN, recordQuality: 500, recordFps: -1,
    recordMaxSeconds: 99, recordMic: "true", saveFolder: "a\u0000b",
  });
  assert.equal(out.theme, DEFAULTS.theme);
  assert.equal(out.format, "png");
  assert.equal(out.recordFormat, "mp4");
  assert.equal(out.quality, DEFAULTS.quality);
  assert.equal(out.recordQuality, 100);
  assert.equal(out.recordFps, DEFAULTS.recordFps);
  assert.equal(out.recordMaxSeconds, DEFAULTS.recordMaxSeconds);
  assert.equal(out.recordMic, DEFAULTS.recordMic);
  assert.equal(out.saveFolder, DEFAULTS.saveFolder);
});

test("settings keep explicitly chosen values within supported ranges", () => {
  const out = normalizeSettings({
    theme: "light", format: "webp", recordFormat: "gif",
    quality: 20, recordQuality: 66, recordFps: 60,
    recordMaxSeconds: 7200, recordMic: true, recordCursor: false,
    saveFolder: "D:/Videos/Snapvere",
  });
  assert.deepEqual(
    [out.theme, out.format, out.recordFormat, out.quality, out.recordQuality, out.recordFps,
      out.recordMaxSeconds, out.recordMic, out.recordCursor, out.saveFolder],
    ["light", "webp", "gif", 50, 66, 60, 7200, true, false, "D:/Videos/Snapvere"],
  );
});
