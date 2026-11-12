# Snapvere — Task Breakdown (developer)

Granularni, izvršni zadaci grupirani po fazama iz roadmapa. Svaki zadatak ima oznaku,
kratak opis i "Done when" kriterij. `[S]` = small (<½ dana), `[M]` = medium (½–2 dana),
`[L]` = large (2–5 dana).

Legenda statusa u skeletonu: ✅ postavljeno · 🟡 djelomično/stub · ⬜ nije započeto.

---

## FAZA 0 — Foundation

- **F0-1 `[S]` ✅ Repo & struktura** — monorepo layout (`apps/desktop`, `docs/`).
  *Done when:* repo se klonira, struktura postoji.
- **F0-2 `[M]` ✅ Tauri v2 scaffold** — `src-tauri` + Vite/React/TS frontend.
  *Done when:* `pnpm tauri dev` otvori prazan prozor.
- **F0-3 `[S]` ✅ Tailwind + dizajn tokeni** — Tailwind config, tema varijable, dark/light.
  *Done when:* tokeni dostupni, dark mode radi.
- **F0-4 `[S]` 🟡 Window router** — mount root po `window.label` (home/overlay/editor/settings).
  *Done when:* svaki prozor renderira svoj root.
- **F0-5 `[S]` ⬜ Lint/format/CI** — ESLint, Prettier, `cargo fmt`/`clippy`, GitHub Actions.
  *Done when:* CI prolazi na push.
- **F0-6 `[S]` 🟡 Tray skeleton** — tray ikona + osnovni menu.
  *Done when:* tray se pojavi, "Quit" radi.

## FAZA 1 — Capture core

- **F1-1 `[M]` 🟡 Capture monitora (xcap)** — `capture.rs`: svi monitori → `RgbaImage`.
  *Done when:* komanda vrati dimenzije svih monitora i frame.
- **F1-2 `[M]` ⬜ Frozen frame state** — drži snimke u app state; komanda `get_frozen_frame`.
  *Done when:* frontend dobije frame kao bytes/data-url.
- **F1-3 `[L]` 🟡 Overlay prozor** — transparentan, fullscreen, per-monitor, always-on-top.
  *Done when:* hotkey otvori dimmani overlay preko svih monitora.
- **F1-4 `[M]` 🟡 Drag-select** — pravokutna selekcija, live `W×H`, Esc odustaje.
  *Done when:* selekcija se crta i vraća koordinate.
- **F1-5 `[S]` ⬜ Magnifier loupe** — zoom kod kursora za precizan rub.
  *Done when:* loupe prati kursor.
- **F1-6 `[M]` 🟡 Crop & encode** — izreži regiju, encode PNG/JPG/WebP (`image`/`webp`).
  *Done when:* dobijemo bajtove u sva 3 formata.
- **F1-7 `[S]` 🟡 Copy to clipboard** — bitmap u clipboard.
  *Done when:* paste u drugu app daje sliku.
- **F1-8 `[M]` 🟡 Save to disk** — save folder + auto-ime + format iz postavki.
  *Done when:* datoteka nastane na disku.
- **F1-9 `[M]` 🟡 SQLite zapis** — `db.rs`: insert screenshot zapisa + thumbnail.
  *Done when:* svaki capture se vidi u bazi.
- **F1-10 `[M]` ⬜ Fullscreen & window capture** — akcije za cijeli ekran i aktivni prozor.
  *Done when:* obje akcije rade iz tray/hotkey.
- **F1-11 `[S]` 🟡 Global hotkey** — registracija PrintScreen → capture regije.
  *Done when:* hotkey radi globalno.

## FAZA 2 — Editor + Gallery (→ MVP)

### Editor
- **F2-1 `[L]` 🟡 Canvas engine** — slojevi: bitmap baza + vektorske anotacije (objekti).
  *Done when:* anotacije se renderiraju nad slikom.
- **F2-2 `[M]` 🟡 Tool: arrow** · **F2-3 `[M]` rectangle** · **F2-4 `[S]` line** ·
  **F2-5 `[M]` pen (freehand)** · **F2-6 `[S]` marker (highlight)**.
  *Done when:* svaki alat crta i ide na history.
- **F2-7 `[L]` 🟡 Tool: blur/redact** — selektiraj regiju → pixelate/gaussian, **destruktivno na exportu**.
  *Done when:* izlazna slika ima nepovratno bluran sadržaj.
- **F2-8 `[M]` 🟡 Tool: text** — klik za tekst, font/size/boja.
  *Done when:* tekst editabilan dok nije commitan.
- **F2-9 `[M]` 🟡 Undo/redo** — history stack svih operacija.
  *Done when:* Ctrl+Z/Ctrl+Y vraćaju/ponavljaju.
- **F2-10 `[S]` 🟡 Color picker + stroke size** — paleta + slider + eyedropper.
  *Done when:* mijenja aktivni alat.
- **F2-11 `[M]` ⬜ Crop u editoru** — re-crop nakon otvaranja.
  *Done when:* crop mijenja izlazni okvir.
- **F2-12 `[M]` 🟡 Export iz editora** — copy/save/(upload kasnije) iz editora.
  *Done when:* spljošteni izlaz s anotacijama.

### Gallery
- **F2-13 `[M]` 🟡 Grid + thumbnaili** — lazy-load, virtualizacija.
  *Done when:* stotine slika scrollaju glatko.
- **F2-14 `[M]` 🟡 Pretraga & filteri** — FTS po imenu/tagu + filter po datumu/formatu/uploadu.
  *Done when:* instant filter rezultata.
- **F2-15 `[S]` ⬜ Tagiranje** — dodaj/ukloni tagove iz galerije i editora.
  *Done when:* tag perzistira i pretraživ je.
- **F2-16 `[S]` 🟡 Lightbox + quick akcije** — pregled, copy, save-as, delete.
  *Done when:* akcije rade iz lightboxa.

### Settings / system
- **F2-17 `[M]` 🟡 Settings panel** — tabovi General/Hotkeys/Capture/Save/Privacy/About.
  *Done when:* promjene perzistiraju (store).
- **F2-18 `[M]` 🟡 Hotkey rebinding** — capture polje + re-registracija u Rustu.
  *Done when:* novi hotkey odmah radi.
- **F2-19 `[S]` 🟡 Autostart** — `tauri-plugin-autostart` toggle.
  *Done when:* app se diže s Windowsom kad je uključeno.
- **F2-20 `[M]` ⬜ MVP packaging** — `.msi`/`.exe`, ikona, metapodaci, smoke test.
  *Done when:* instaler radi na čistom Windowsu → **MVP RELEASE**.

## FAZA 3 — Cloud & Sharing (backend)

- **F3-1 `[M]` ⬜ NestJS scaffold** — moduli Auth/Users/Screenshots/Links/Storage; config; Docker.
- **F3-2 `[M]` ⬜ PostgreSQL + migracije** — shema iz spec §6.2 (Prisma/TypeORM).
- **F3-3 `[M]` ⬜ Storage modul (S3/MinIO)** — presigned PUT/GET, privatni bucket.
- **F3-4 `[M]` ⬜ Sharp ingest pipeline** — thumbnail + EXIF strip pri uploadu.
- **F3-5 `[L]` ⬜ Auth (magic-link)** — JWT + refresh, rate-limit; Google OAuth opcionalno.
- **F3-6 `[M]` ⬜ Upload API** — `/presign` + `/screenshots` registracija + slug generacija (≥128 bit).
- **F3-7 `[M]` ⬜ Link view API** — `/s/:slug`, `/unlock`, `/image` (presigned, noindex headeri).
- **F3-8 `[M]` ⬜ Istek + cron čišćenje** — BullMQ/cron briše istekle (DB + storage).
- **F3-9 `[M]` ⬜ Password linkovi** — argon2id hash, unlock token, rate-limit.
- **F3-10 `[M]` ⬜ Revoke/delete** — soft delete + brisanje objekta; 404 nakon revoke.
- **F3-11 `[L]` ⬜ Desktop upload klijent** — upload flow, opcije isteka/lozinke, link u clipboard.
- **F3-12 `[M]` ⬜ Lista uploada (desktop)** — privatni pregled vlastitih linkova + manage.

## FAZA 4 — Privacy++ & Pro

- **F4-1 `[L]` ⬜ Sensitive-data detekcija** — lokalni regex+OCR (email/karticu/IBAN/JWT) → blur prijedlog.
- **F4-2 `[M]` ⬜ Upozorenje prije uploada** — modal s "Blur sad / Upload svejedno".
- **F4-3 `[M]` ⬜ Lokalna enkripcija galerije** — opcionalna lozinka za lokalne podatke.
- **F4-4 `[M]` ⬜ Editor pro** — numerirani koraci, elipsa, object select/edit, drag-out.
- **F4-5 `[M]` ⬜ Galerija pro** — bulk delete/export, napredni filteri.

## FAZA 5 — Cross-platform & Polish

- **F5-1 `[L]` ⬜ macOS build** — capture/permissions (screen recording), tray, packaging.
- **F5-2 `[L]` ⬜ Linux build** — X11/Wayland capture, packaging (AppImage/deb).
- **F5-3 `[M]` ⬜ Onboarding** — first-run wizard.
- **F5-4 `[M]` ⬜ Auto-update** — Tauri updater + potpisivanje.
- **F5-5 `[M]` ⬜ Performance pass** — capture latencija, memorija frozen frames, startup.

## FAZA 6 — Beta → 1.0

- **F6-1 `[M]` ⬜ Opt-in telemetrija** — anonimni crash/usage (privacy-friendly).
- **F6-2 `[M]` ⬜ Dokumentacija + site** — docs, privacy policy, landing.
- **F6-3 `[M]` ⬜ QA & stabilizacija** — bug bash, edge-case multi-monitor/DPI.
- **F6-4 `[S]` ⬜ Launch** — release kanali, changelog, verzioniranje.

---

## Predloženi redoslijed rada (kritični put do MVP-a)
```
F0-2 → F0-4 → F1-1 → F1-2 → F1-3 → F1-4 → F1-6 → F1-7/F1-8 → F1-9
   → F2-1 → (F2-2…F2-8) → F2-9 → F2-12 → F2-13 → F2-14 → F2-17 → F2-20 (MVP)
```

## Dependency napomene
- Editor (F2-1) ovisi o crop/encode (F1-6).
- Galerija (F2-13/14) ovisi o SQLite zapisu (F1-9).
- Sav cloud (Faza 3) ovisi o stabilnom lokalnom modelu screenshota (Faza 1–2).
- Sensitive detekcija (F4-1) je preduvjet za smisleno upozorenje prije uploada (F4-2),
  ali F4-2 može krenuti s ručnim blurom dok detekcija ne sazri.
