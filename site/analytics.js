/* ─────────────────────────────────────────────────────────────
   Snapvere sajt — Google Analytics 4 uz privolu (GDPR, Consent Mode v2)

   • GA se NE učitava dok posjetitelj ne klikne „Accept" (basic consent
     mode) — nema kolačića ni pingova prije pristanka.
   • Oglasni signali (ad_storage / ad_user_data / ad_personalization) su
     uvijek odbijeni: sajt nema oglasa.
   • Bez `gaId` u config.js nema ni GA ni bannera.
   • window.snapvereTrack(ime, parametri) je sigurno zvati uvijek: prije odluke
     se eventi drže u redu, nakon „Decline" se odbacuju.
   • Elementi s [data-consent-open] (footer „Cookie settings") ponovno
     otvaraju banner.
   ───────────────────────────────────────────────────────────── */
(function () {
  "use strict";

  var ID = (window.SNAPVERE && window.SNAPVERE.gaId) || "";
  var KEY = "snapvere-consent"; // "granted" | "denied"
  var state = readChoice();
  var queue = [];
  var loaded = false;
  var banner = null;

  window.dataLayer = window.dataLayer || [];
  function gtag() {
    window.dataLayer.push(arguments);
  }

  function readChoice() {
    try {
      return localStorage.getItem(KEY);
    } catch (e) {
      return null; // privatni prozor / blokirana pohrana → pitaj svaki put
    }
  }
  function saveChoice(v) {
    try {
      localStorage.setItem(KEY, v);
    } catch (e) {}
  }

  function load() {
    if (loaded || !ID) return;
    loaded = true;
    gtag("consent", "default", {
      ad_storage: "denied",
      ad_user_data: "denied",
      ad_personalization: "denied",
      analytics_storage: "granted",
    });
    var s = document.createElement("script");
    s.async = true;
    s.src = "https://www.googletagmanager.com/gtag/js?id=" + encodeURIComponent(ID);
    document.head.appendChild(s);
    gtag("js", new Date());
    gtag("config", ID);
    queue.splice(0).forEach(function (e) {
      gtag("event", e[0], e[1]);
    });
  }

  // Povlačenje privole: zaustavi GA i obriši njegove kolačiće (_ga, _ga_XXXX).
  function revoke() {
    if (loaded) gtag("consent", "update", { analytics_storage: "denied" });
    var host = location.hostname;
    document.cookie.split(";").forEach(function (c) {
      var name = c.split("=")[0].trim();
      if (name !== "_ga" && name.indexOf("_ga_") !== 0) return;
      [host, "." + host].forEach(function (d) {
        document.cookie = name + "=; Max-Age=0; path=/; domain=" + d;
      });
      document.cookie = name + "=; Max-Age=0; path=/";
    });
  }

  function decide(choice) {
    var was = state;
    state = choice;
    saveChoice(choice);
    hideBanner();
    if (choice === "granted") {
      load();
    } else {
      queue.length = 0;
      if (was === "granted") revoke();
    }
  }

  function showBanner() {
    if (!ID) return;
    if (!banner) {
      banner = document.createElement("div");
      banner.className = "consent";
      banner.setAttribute("role", "dialog");
      banner.setAttribute("aria-label", "Cookie consent");
      banner.innerHTML =
        '<p class="consent__text"><strong>Mind if we count?</strong> ' +
        "We&rsquo;d like to use Google Analytics cookies to see how many people " +
        "visit and download Snapvere. No ads, no selling data, and the app " +
        'itself never phones home. <a href="/privacy/">Privacy policy</a></p>' +
        '<div class="consent__actions">' +
        '<button type="button" class="btn btn--ghost btn--sm" data-choice="denied">Decline</button>' +
        '<button type="button" class="btn btn--primary btn--sm" data-choice="granted">Accept</button>' +
        "</div>";
      banner.addEventListener("click", function (e) {
        var btn = e.target.closest("[data-choice]");
        if (btn) decide(btn.getAttribute("data-choice"));
      });
      document.body.appendChild(banner);
    }
    banner.hidden = false;
    requestAnimationFrame(function () {
      banner.classList.add("is-in");
    });
  }

  function hideBanner() {
    if (banner) {
      banner.classList.remove("is-in");
      banner.hidden = true;
    }
  }

  window.snapvereTrack = function (name, params) {
    if (!ID) return;
    if (state === "granted") {
      load();
      gtag("event", name, params || {});
    } else if (state !== "denied") {
      queue.push([name, params || {}]);
    }
  };

  if (ID && state === "granted") load();

  function init() {
    if (!ID) return;
    if (state !== "granted" && state !== "denied") showBanner();
    document.querySelectorAll("[data-consent-open]").forEach(function (el) {
      el.hidden = false;
      el.addEventListener("click", function (e) {
        e.preventDefault();
        showBanner();
      });
    });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
