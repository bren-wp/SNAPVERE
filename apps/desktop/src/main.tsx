import React from "react";
import ReactDOM from "react-dom/client";
import "./styles.css";

import Home from "./windows/Home";
import Overlay from "./windows/Overlay";
import Editor from "./windows/Editor";
import Settings from "./windows/Settings";
import RecBar from "./windows/RecBar";
import RecDraw from "./windows/RecDraw";
import RecFrame from "./windows/RecFrame";

/**
 * Window router.
 * Svaki Tauri prozor učitava `index.html` s hashom koji određuje koji root mountamo:
 *   #overlay?mon=<id>   → selekcija regije
 *   #editor?p=<base64>  → annotation editor
 *   #recbar             → HUD traka dok snimanje traje
 *   #recdraw            → prozirni sloj za crtanje preko snimke
 *   #recframe           → okvir: zatamni sve oko snimanog područja
 *   #settings           → postavke
 *   (default)           → home / galerija
 */
function resolveWindow() {
  const hash = window.location.hash.replace(/^#/, "");
  const [kind, query] = hash.split("?");
  return { kind: kind || "home", params: new URLSearchParams(query ?? "") };
}

// Primijeni spremljenu temu što ranije (izbjegne bljesak).
const theme = localStorage.getItem("snapvere-theme") ?? "dark";
document.documentElement.setAttribute("data-theme", theme);

const { kind, params } = resolveWindow();

let node: React.ReactNode;
switch (kind) {
  case "overlay":
    document.body.classList.add("overlay");
    node = <Overlay />;
    break;
  case "editor":
    node = <Editor pathB64={params.get("p") ?? ""} />;
    break;
  case "recbar":
    document.body.classList.add("hud");
    node = <RecBar />;
    break;
  case "recdraw":
    document.body.classList.add("hud");
    node = <RecDraw />;
    break;
  case "recframe":
    document.body.classList.add("hud");
    node = <RecFrame />;
    break;
  case "settings":
    node = <Settings />;
    break;
  default:
    node = <Home />;
}

ReactDOM.createRoot(document.getElementById("root")!).render(node);
