//! Selection/annotation palette and pure cursor/font helpers.
import { MousePointer2, ArrowUpRight, Square, Circle, Minus, PenTool, Highlighter, EyeOff, Type, ListOrdered } from "lucide-react";
import type { EditorTool } from "../types";
import type { ResizeDirection as Dir } from "../selectionGeometry";

export const TOOLS: { id: EditorTool; icon: typeof Square; label: string }[] = [
  { id: "select", icon: MousePointer2, label: "Move / resize frame" },
  { id: "arrow", icon: ArrowUpRight, label: "Arrow" },
  { id: "rect", icon: Square, label: "Rectangle" },
  { id: "ellipse", icon: Circle, label: "Ellipse" },
  { id: "line", icon: Minus, label: "Line" },
  { id: "pen", icon: PenTool, label: "Pen" },
  { id: "marker", icon: Highlighter, label: "Marker" },
  { id: "step", icon: ListOrdered, label: "Numbered step" },
  { id: "blur", icon: EyeOff, label: "Blur / redact" },
  { id: "text", icon: Type, label: "Text" },
];

export const SWATCHES = [
  "#FF453A", "#FF9F0A", "#FFD60A", "#30D158", "#40C8E0",
  "#09BFD9", "#BF5AF2", "#FF375F", "#FFFFFF", "#1C1C1E",
];

let counter = 0;
export const uid = () => `a${++counter}`;
export const textFont = (size: number) => Math.round(size * 3.5 + 8);
export const cursorFor = (d: Dir) =>
  d === "nw" || d === "se" ? "nwse-resize" : d === "ne" || d === "sw" ? "nesw-resize" : d === "n" || d === "s" ? "ns-resize" : "ew-resize";

