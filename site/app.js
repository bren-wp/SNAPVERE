/* ─────────────────────────────────────────────────────────────
   Snapvere landing — interaktivnost

   Download linkovi, verzija, GA i donacije žive u config.js.
   Gumbi s [data-os] vode na /download/?os=…&from=… (thank-you stranica):
   ona pokrene download, pošalje GA event i ponudi donaciju.
   `data-from` = mjesto gumba na stranici (hero / cta / footer) za GA.
   ───────────────────────────────────────────────────────────── */
const CFG = window.SNAPVERE || { version: "", downloads: {}, support: {} };
const OS_LABEL = { windows: "Windows", mac: "macOS" };
const track = (name, params) => window.snapvereTrack && window.snapvereTrack(name, params);

/* ── detekcija operativnog sustava posjetitelja ──────────────── */
function detectOS() {
  const p = (navigator.userAgentData?.platform || navigator.platform || "")
    .toLowerCase();
  const ua = navigator.userAgent.toLowerCase();
  if (/iphone|ipad|ipod|android/.test(ua)) return null; // mobitel → ne nameći ništa
  if (p.includes("mac") || ua.includes("mac")) return "mac";
  if (p.includes("win") || ua.includes("win")) return "windows";
  return null; // Linux / nepoznato
}

/* ── poveži download gumbe ───────────────────────────────────── */
function wireDownloads() {
  const os = detectOS();
  const ready = (target) => Boolean(CFG.downloads[target]);

  document.querySelectorAll("[data-os]").forEach((el) => {
    const target = el.dataset.os;
    const from = el.dataset.from || "page";

    if (ready(target)) {
      el.setAttribute("href", `/download/?os=${target}&from=${from}`);
    } else {
      // build još nije objavljen → blagi feedback + mjerimo potražnju
      el.addEventListener("click", (e) => {
        e.preventDefault();
        flashHint(`The ${OS_LABEL[target]} build is still in testing. Windows is ready now.`);
        track("download_unavailable", { os: target, placement: from });
      });
    }

    // istakni gumb za posjetiteljev OS — samo ako se stvarno može skinuti
    if (os === target && ready(os) && el.classList.contains("btn")) {
      el.setAttribute("data-recommended", "");
    }
  });

  // stiša "drugi" gumb da preporučeni dominira
  if (os && ready(os)) {
    document
      .querySelectorAll(`.btn[data-os]:not([data-os="${os}"])`)
      .forEach((el) => el.classList.replace("btn--primary", "btn--ghost"));
    document
      .querySelectorAll(`.btn[data-os="${os}"]`)
      .forEach((el) => el.classList.replace("btn--ghost", "btn--primary"));
  }

  const hint = document.getElementById("os-hint");
  if (hint) {
    const v = CFG.version ? `v${CFG.version} · ` : "";
    if (os && ready(os)) {
      hint.textContent = `Detected: ${OS_LABEL[os]} · ${v}free, no account`;
    } else if (os === "mac") {
      hint.textContent = "The macOS build is in testing — Windows 10/11 is ready now.";
    } else {
      hint.textContent = `${v}Windows 10/11, 64-bit · free, no account`;
    }
  }
}

let hintTimer;
function flashHint(msg) {
  const hint = document.getElementById("os-hint");
  if (!hint) return;
  hint.textContent = msg;
  hint.style.color = "#3d9bff";
  clearTimeout(hintTimer);
  hintTimer = setTimeout(() => {
    hint.style.color = "";
  }, 2600);
}

/* ── link „Support Snapvere" samo ako postoji ijedan link za donaciju ── */
function wireSupport() {
  const any = Object.values(CFG.support || {}).some(Boolean);
  document.querySelectorAll("[data-support-link]").forEach((el) => {
    el.hidden = !any;
  });
}

/* ── reveal na scroll ────────────────────────────────────────── */
function wireReveal() {
  const els = document.querySelectorAll(".reveal");
  if (!("IntersectionObserver" in window)) {
    els.forEach((el) => el.classList.add("is-in"));
    return;
  }
  const io = new IntersectionObserver(
    (entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          entry.target.classList.add("is-in");
          io.unobserve(entry.target);
        }
      });
    },
    { threshold: 0.12, rootMargin: "0px 0px -8% 0px" }
  );
  els.forEach((el) => io.observe(el));
}

/* ── nav dobiva pozadinu nakon scrolla ───────────────────────── */
function wireNav() {
  const nav = document.getElementById("nav");
  if (!nav) return;
  const onScroll = () => nav.classList.toggle("is-scrolled", window.scrollY > 12);
  onScroll();
  window.addEventListener("scroll", onScroll, { passive: true });
}

/* ── FAQ: koja pitanja ljudi otvaraju (signal što nije jasno) ── */
function wireFaq() {
  document.querySelectorAll(".faq__item").forEach((item) => {
    item.addEventListener("toggle", () => {
      if (!item.open) return;
      const q = item.querySelector("summary")?.textContent.trim() || "";
      track("faq_open", { question: q.slice(0, 100) });
    });
  });
}

/* ── init ────────────────────────────────────────────────────── */
document.addEventListener("DOMContentLoaded", () => {
  wireDownloads();
  wireSupport();
  wireReveal();
  wireNav();
  wireFaq();
  const y = document.getElementById("year");
  if (y) y.textContent = new Date().getFullYear();
});
