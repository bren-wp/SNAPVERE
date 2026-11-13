import test from "node:test";
import assert from "node:assert/strict";
import {
  normalizeCapturePreferences,
  formatFromSavePath,
} from "../src/lib/capturePreferences.ts";

test("new screenshot exports use safe defaults", () => {
  assert.deepEqual(normalizeCapturePreferences(null, null), {
    format: "png",
    quality: 92,
  });
  assert.deepEqual(normalizeCapturePreferences("gif", Infinity), {
    format: "png",
    quality: 92,
  });
});

test("stored settings preserve valid formats and clamp invalid qualities", () => {
  assert.deepEqual(normalizeCapturePreferences("webp", 82), {
    format: "webp",
    quality: 82,
  });
  assert.deepEqual(normalizeCapturePreferences("jpg", 999), {
    format: "jpg",
    quality: 100,
  });
  assert.deepEqual(normalizeCapturePreferences("png", -12), {
    format: "png",
    quality: 1,
  });
  assert.equal(normalizeCapturePreferences("png", NaN).quality, 92);
});

test("save as uses the filename's actual image extension, including JPEG", () => {
  assert.equal(formatFromSavePath("C:\\Pictures\\Screenshot.JPEG"), "jpg");
  assert.equal(formatFromSavePath("capture.jpg"), "jpg");
  assert.equal(formatFromSavePath("folder/capture.png"), "png");
  assert.equal(formatFromSavePath("capture.webp"), "webp");
  assert.equal(formatFromSavePath("capture.exe"), null);
  assert.equal(formatFromSavePath("capture"), null);
  assert.equal(formatFromSavePath("capture.png.exe"), null);
});
