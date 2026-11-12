/* ─────────────────────────────────────────────────────────────
   Snapvere — /download/ (thank-you) stranica

   /download/?os=windows&from=hero
     → download krene sam nakon kratke pauze, GA dobije `file_download`
       (os, placement = odakle je klik došao, method = auto).
   /download/ bez ?os
     → samo gumb, ništa se ne skida samo od sebe (sigurno za linkove
       poput /download/#support i za posjetitelje iz tražilice).
   Mobitel / OS bez builda → bez auto-downloada, jasna poruka.
   ───────────────────────────────────────────────────────────── */
const CFG = window.SNAPVERE || { version: "", downloads: {}, support: {} };
const OS_LABEL = { windows: "Windows", mac: "macOS" };
const track = (name, params) => window.snapvereTrack && window.snapvereTrack(name, params);
const AUTO_DELAY_MS = 900;

const SUPPORT = {
  kofi: {
    label: "Buy me a coffee",
    sub: "on Ko-fi",
    icon: '<path d="M4 8h13v5a5 5 0 0 1-5 5H9a5 5 0 0 1-5-5V8Z"/><path d="M17 9h1.5a2.5 2.5 0 0 1 0 5H17"/><path d="M8 3v2M12 3v2"/>',
  },
  github: {
    label: "Sponsor",
    sub: "on GitHub",
    icon: '<path d="M12 20s-7-4.4-7-10a4 4 0 0 1 7-2.6A4 4 0 0 1 19 10c0 5.6-7 10-7 10Z"/>',
  },
  paypal: {
    label: "Donate",
    sub: "with PayPal",
    icon: '<rect x="3" y="6" width="18" height="13" rx="2"/><path d="M3 10h18M7 15h3"/>',
  },
};

function detectOS() {
  const p = (navigator.userAgentData?.platform || navigator.platform || "")
    .toLowerCase();
  const ua = navigator.userAgent.toLowerCase();
  if (/iphone|ipad|ipod|android/.test(ua)) return "mobile";
  if (p.includes("mac") || ua.includes("mac")) return "mac";
  if (p.includes("win") || ua.includes("win")) return "windows";
  return null;
}

const $ = (id) => document.getElementById(id);

function fileName(url) {
  return decodeURIComponent(url.split("/").pop() || "");
}

function eventParams(os, url, extra) {
  const name = fileName(url);
  return {
    os,
    app_version: CFG.version,
    file_name: name,
    file_extension: name.split(".").pop(),
    link_url: url,
    ...extra,
  };
}

// Gumb pokazuje ikonu, tekst i ime fajla za zadani OS.
function setButton(os) {
  const url = CFG.downloads[os] || "";
  const v = CFG.version ? ` · v${CFG.version}` : "";
  $("dl-btn").href = url;
  $("dl-btn-text").textContent = `Download for ${OS_LABEL[os]}`;
  $("dl-file").textContent = `${fileName(url)}${v}`;
  document.querySelectorAll("#dl-btn [data-icon]").forEach((svg) => {
    svg.hidden = svg.dataset.icon !== os;
  });
  return url;
}

function wireEditionLinks() {
  document.querySelectorAll("[data-download-edition]").forEach((el) => {
    const key = el.dataset.downloadEdition;
    const url = CFG.downloads[key];
    if (!url) { el.hidden = true; return; }
    el.href = url;
    el.addEventListener("click", () => {
      track("file_download", eventParams("windows", url, { placement: "edition-selector", method: "button" }));
    });
  });
}

function render() {
  wireEditionLinks();
  const q = new URLSearchParams(location.search);
  const wanted = OS_LABEL[q.get("os")] ? q.get("os") : null;
  const from = (q.get("from") || "direct").slice(0, 24);
  const device = detectOS();
  const v = CFG.version ? `v${CFG.version} ` : "";

  const card = $("dl-card");
  const title = $("dl-title");
  const sub = $("dl-sub");
  const note = $("dl-note");
  const started = () => {
    card.dataset.state = "started";
    title.textContent = "Thanks for downloading!";
  };

  // Traženi OS bez builda (zasad macOS) → poruka + Windows kao alternativa.
  const target = wanted || (device === "mac" ? "mac" : "windows");
  if (!CFG.downloads[target]) {
    card.dataset.state = "soon";
    title.textContent = `Snapvere for ${OS_LABEL[target]} is in testing`;
    sub.textContent =
      "It lands here as soon as it’s cleared for release. The Windows build is ready today.";
    const winUrl = setButton("windows");
    note.innerHTML =
      'Want a heads-up? <a href="https://github.com/bren-wp/Snapvere/releases" target="_blank" rel="noopener">Watch the releases on GitHub</a>.';
    track("download_unavailable", { os: target, placement: from });
    $("dl-btn").addEventListener("click", () => {
      track("file_download", eventParams("windows", winUrl, { placement: from, method: "button" }));
      started();
    });
    return;
  }

  const url = setButton(target);
  // Auto-download samo kad je posjetitelj kliknuo gumb za taj OS i nije na mobitelu
  // ni na očito drugom OS-u (Mac korisnik koji klikne Windows dobije gumb, ne fajl).
  const auto = wanted === target && device !== "mobile" && (device === target || device === null);

  if (auto) {
    card.dataset.state = "starting";
    title.textContent = "Your download is starting…";
    sub.textContent = `Snapvere ${v}for ${OS_LABEL[target]} — free, no account, no ads.`;
    note.innerHTML = 'Didn’t start? <a id="dl-retry" href="">Download it directly</a>.';
    $("dl-retry").href = url;
    // ponovni klik (link ili veliki gumb) = retry, ne novi download u statistici
    [$("dl-retry"), $("dl-btn")].forEach((el) =>
      el.addEventListener("click", () => track("download_retry", { os: target, placement: from }))
    );

    track("file_download", eventParams(target, url, { placement: from, method: "auto" }));
    setTimeout(() => {
      window.location.assign(url);
      started();
    }, AUTO_DELAY_MS);
  } else {
    card.dataset.state = "ready";
    if (device === "mobile") {
      sub.textContent = `Snapvere is a desktop app. Open this page on your ${OS_LABEL[target]} computer to install it.`;
    } else if (wanted && device && device !== target) {
      sub.textContent = `This looks like a ${OS_LABEL[device]} computer — the installer below is for ${OS_LABEL[target]}.`;
    }
    $("dl-btn").addEventListener("click", () => {
      track("file_download", eventParams(target, url, { placement: from, method: "button" }));
      started();
    });
  }
}

/* ── donacije: iscrtaj samo gumbe koji imaju link ────────────── */
function renderSupport() {
  const links = CFG.support || {};
  const keys = Object.keys(SUPPORT).filter((k) => links[k]);
  const box = $("support");
  const list = $("support-links");
  if (!box || !list || !keys.length) return;

  list.innerHTML = keys
    .map(
      (k, i) => `
      <a class="btn ${i === 0 ? "btn--primary" : "btn--ghost"} btn--lg" data-platform="${k}"
         href="${encodeURI(links[k])}" target="_blank" rel="noopener">
        <svg class="btn__icon btn__icon--line" viewBox="0 0 24 24" aria-hidden="true">${SUPPORT[k].icon}</svg>
        <span>${SUPPORT[k].label}<small>${SUPPORT[k].sub}</small></span>
      </a>`
    )
    .join("");
  list.addEventListener("click", (e) => {
    const a = e.target.closest("[data-platform]");
    if (a) track("donate_click", { platform: a.dataset.platform });
  });
  box.hidden = false;
}

function wireNav() {
  const nav = $("nav");
  if (!nav) return;
  const onScroll = () => nav.classList.toggle("is-scrolled", window.scrollY > 12);
  onScroll();
  window.addEventListener("scroll", onScroll, { passive: true });
}

document.addEventListener("DOMContentLoaded", () => {
  render();
  renderSupport();
  wireNav();
  document.querySelectorAll("[data-support-link]").forEach((el) => {
    el.hidden = !Object.values(CFG.support || {}).some(Boolean);
  });
  const y = $("year");
  if (y) y.textContent = new Date().getFullYear();
});
