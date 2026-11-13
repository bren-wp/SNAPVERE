import test from "node:test";
import assert from "node:assert/strict";
import { PLAYBACK_SPEEDS, readVolume, normalizeVolume } from "../src/lib/playerPreferences.ts";

test("volume preference rejects invalid or corrupt local data", () => {
  assert.deepEqual(normalizeVolume(null), { volume: 1, muted: false });
  assert.deepEqual(normalizeVolume({ volume: Number.NaN, muted: "true" }), { volume: 1, muted: false });
  assert.deepEqual(normalizeVolume({ volume: 9, muted: true }), { volume: 1, muted: true });
  assert.deepEqual(normalizeVolume({ volume: -4, muted: false }), { volume: 0, muted: false });
  assert.deepEqual(readVolume({ getItem: () => "broken json" }), { volume: 1, muted: false });
  assert.deepEqual(readVolume({ getItem: () => '{"volume":0.4,"muted":true}' }), { volume: 0.4, muted: true });
});

test("playback speeds are unique and within supported safe range", () => {
  assert.equal(new Set(PLAYBACK_SPEEDS).size, PLAYBACK_SPEEDS.length);
  assert.ok(PLAYBACK_SPEEDS.includes(1));
  for (const speed of PLAYBACK_SPEEDS) assert.ok(speed >= 0.5 && speed <= 2);
});
