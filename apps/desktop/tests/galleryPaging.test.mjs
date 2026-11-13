import test from "node:test";
import assert from "node:assert/strict";
import { mergeUniqueById } from "../src/lib/galleryPaging.ts";

test("append new pages without replaying an overlapping capture", () => {
  const current = [{ id: "new" }, { id: "middle" }];
  const incoming = [{ id: "middle" }, { id: "old" }];
  assert.deepEqual(mergeUniqueById(current, incoming).map((item) => item.id), [
    "new", "middle", "old",
  ]);
  assert.deepEqual(current.map((item) => item.id), ["new", "middle"]);
});

test("deduplicates duplicate IDs inside the same incoming page", () => {
  assert.deepEqual(
    mergeUniqueById([], [{ id: "one" }, { id: "one" }, { id: "two" }]),
    [{ id: "one" }, { id: "two" }],
  );
});
