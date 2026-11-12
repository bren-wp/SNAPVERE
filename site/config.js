/* ─────────────────────────────────────────────────────────────
   Snapvere sajt — SVE postavke na jednom mjestu.
   Ovo je jedini fajl koji treba dirati kod nove verzije,
   GA4 ID-a ili linkova za donacije.
   ───────────────────────────────────────────────────────────── */
window.SNAPVERE = {
  // Prikazuje se na download stranici. Uskladi s tauri.conf.json kod releasea.
  version: "0.2.4",

  // Prazan string ("") => gumb pokaže „coming soon" umjesto downloada.
  // `latest/download` uvijek vodi na zadnji objavljeni GitHub Release.
  downloads: {
    windows:
      "",
    mac: "", // .dmg se gradi u CI-u, ali čeka potvrdu da radi na Macu
  },

  // Google Analytics 4 Measurement ID, oblik "G-XXXXXXXXXX".
  // Prazno => GA se ne učitava i cookie banner se ne prikazuje.
  gaId: "",

  // Linkovi za donacije na /download/ stranici i u footeru.
  // Prazno => taj gumb se ne prikazuje; sve prazno => cijeli blok skriven.
  support: {
    kofi: "", // npr. "https://ko-fi.com/snapvere"
    github: "", // npr. "https://github.com/sponsors/bren-wp"
    paypal: "", // npr. "https://paypal.me/snapvere"
  },
};
