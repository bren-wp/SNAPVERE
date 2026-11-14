import test from "node:test";
import assert from "node:assert/strict";
import { clampRect, applyResize } from "../src/lib/selectionGeometry.ts";

test("selection clamps negative, oversized and nonfinite geometry", () => {
  assert.deepEqual(clampRect({x:-20,y:-10,w:200,h:90},{width:100,height:70}),
    {x:0,y:0,w:100,h:70});
  assert.deepEqual(clampRect({x:Number.NaN,y:5,w:0,h:0},{width:10,height:8}),
    {x:0,y:0,w:10,h:8});
});

test("northwest resize clamps to minimum and preserves right/bottom anchors", () => {
  const before = {x:10,y:20,w:100,h:70};
  assert.deepEqual(applyResize("nw",before,150,150,{width:400,height:300}),
    {x:98,y:78,w:12,h:12});
  assert.deepEqual(applyResize("se",before,30,20,{width:400,height:300}),
    {x:10,y:20,w:130,h:90});
});

test("keyboard nudging clamps to viewport edges", () => {
  const source = {x:10,y:10,w:70,h:70};
  assert.deepEqual(clampRect({...source,x:110,y:110},{width:100,height:100}),
    {x:30,y:30,w:70,h:70});
});
