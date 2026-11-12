# Snapvere — Product Specification & Development Plan

> Verzija dokumenta: 1.0 · Status: aktivni nacrt · Platforma: Windows-first (macOS/Linux kasnije)

---

## 1. Kratko objašnjenje proizvoda

**Snapvere** je desktop alat za brzo hvatanje ekrana usmjeren na tri stvari koje stari alati
rade loše: **brzinu**, **anotaciju** i **privatnost**.

Korisnik pritisne globalni hotkey, ekran se zatamni transparentnim overlayem, mišem označi
pravokutnik, i odmah dobije floating toolbar za brzo uređivanje (strelice, okviri, tekst, blur).
Rezultat može kopirati u clipboard, spremiti lokalno (PNG/JPG/WebP) ili podijeliti preko
**privatnog, sigurnog linka** s opcionalnim istekom i lozinkom.

Sve uhvaćeno čuva se u **lokalnoj galeriji** s pretragom po datumu, imenu i tagovima — bez
obaveznog clouda. Cloud je opcija, ne uvjet.

**Pozicioniranje:** _"Brzina brzog screenshot alata, ali s privatnošću kakvu zaslužuješ 2026."_

**Ciljani korisnici:** developeri, dizajneri, support timovi, QA, svatko tko dijeli screenshotove
i ne želi da završe na javnoj, indeksiranoj galeriji.

**Ključni diferencijatori:**
1. Privatno po defaultu + high-entropy linkovi + istek + lozinka
2. Blur/redact kao **primarni**, istaknuti alat (ne sakriven)
3. Lokalna galerija s pretragom — vlasništvo nad vlastitim podacima
4. Premium, minimalistički UI; native brzina (Tauri/Rust)
5. Upozorenje prije uploada ako slika možda sadrži osjetljive podatke

---

## 2. Popis svih funkcija

### Capture
1. Capture označene regije (drag-to-select overlay)
2. Capture cijelog ekrana (svi monitori ili odabrani)
3. Capture aktivnog prozora
4. Capture s odgodom (delay 3s/5s/10s) — *napredno*
5. Multi-monitor podrška (DPI-aware)
6. Re-capture / ponovi zadnju regiju — *napredno*

### Output
7. Kopiraj u clipboard (bitmap)
8. Spremi lokalno kao **PNG / JPG / WebP** (odabir kvalitete za JPG/WebP)
9. Auto-imenovanje po predlošku (`SNAPVERE_{date}_{time}_{seq}`)
10. Drag-out slike iz editora u druge aplikacije — *napredno*

### Anotacija (Quick Editor)
11. Strelica (arrow)
12. Pravokutnik (rectangle) — ispunjen/obrub
13. Linija (line)
14. Elipsa/krug — *napredno*
15. Freehand pen (olovka)
16. Highlight marker (poluprozirni)
17. **Blur / redact** osjetljive regije (pixelate ili gaussian)
18. Tekst (font, veličina, boja)
19. Numerirani koraci (1, 2, 3 badge-evi) — *napredno*
20. Crop / re-crop nakon capture
21. Undo / redo (puni history stack)
22. Color picker (paleta + eyedropper)
23. Stroke size (debljina poteza)
24. Brisanje/odabir pojedinog elementa (object model, ne raster) — *napredno*

### Sharing & Cloud (Faza 3)
25. Upload na vlastiti backend
26. Generiranje **privatnog** share linka (high-entropy ID)
27. Opcionalni istek linka: **1h / 1d / 7d / nikad**
28. Opcionalna **lozinka** za zaštićeni link
29. Brisanje uploadanog screenshota (revoke linka)
30. Upozorenje prije uploada ako je detektiran mogući osjetljivi sadržaj
31. Kopiranje share linka u clipboard nakon uploada
32. Pregled vlastitih uploada (privatna lista, nikad javna galerija)

### Lokalna galerija & organizacija
33. Lokalna povijest svih screenshotova
34. Pretraga po datumu / imenu / tagu
35. Tagiranje screenshotova
36. Filtriranje (samo uploadani, samo lokalni, po formatu)
37. Brzi pregled (lightbox) + kopiraj/dijeli/obriši iz galerije
38. Bulk akcije (obriši više, export više) — *napredno*

### Sustav & postavke
39. Globalni hotkeys (konfigurabilni po akciji)
40. Settings panel: hotkeys, save folder, default format, kvaliteta, privatnost
41. System tray ikona (quick menu: capture, gallery, settings, quit)
42. Auto-start s Windowsom (opcionalno)
43. Default ponašanje nakon capture (otvori editor / kopiraj / spremi)
44. Tema: dark / light / system
45. Onboarding na prvom pokretanju — *napredno*

---

## 3. User flow

### 3.1 Glavni flow — capture regije
```
Hotkey (PrintScreen)
   │
   ▼
Rust uhvati frame svih monitora (xcap) → drži u memoriji (frozen snapshot)
   │
   ▼
Otvori transparentni fullscreen overlay (per-monitor), prikaže frozen snapshot + dimming
   │
   ▼
Korisnik povuče pravokutnik
   │   (live dimenzije, magnifier, snap na rubove)
   ▼
Otpuštanje miša → selekcija fiksirana
   │
   ▼
Floating toolbar uz selekciju:
   [ Copy ] [ Save ] [ Edit ] [ Upload ] [ Blur ] [ ✕ ]
   │
   ├─ Copy   → bitmap u clipboard → toast → zatvori overlay
   ├─ Save   → spremi u save folder (default format) → zapis u galeriju → toast
   ├─ Edit   → otvori Editor prozor s crop-om regije
   ├─ Upload → (Faza 3) sensitive-check → upload → link u clipboard
   └─ Esc    → odustani, zatvori overlay
```

### 3.2 Editor flow
```
Editor prozor (slika učitana)
   │
   ▼
Lijevo: paleta alata (select, arrow, rect, line, pen, marker, blur, text, crop)
Vrh: kontekstne opcije (boja, stroke, font) · Undo/Redo
   │
   ▼
Korisnik anotira (svaka akcija ide na history stack)
   │
   ▼
Akcije: [Copy] [Save as…] [Upload] [Pin] 
   │
   ▼
Spremanje → galerija ažurirana, prozor se zatvara (ili ostaje po postavci)
```

### 3.3 Upload flow (Faza 3)
```
Klik Upload
   │
   ▼
Lokalna sensitive-data heuristika (OCR/regex za email, karticu, token) — opcionalno
   │
   ├─ Detektirano? → modal: "Slika možda sadrži osjetljive podatke. Blur prije uploada?"
   │                         [ Blur sad ] [ Upload svejedno ] [ Odustani ]
   ▼
Odabir opcija: istek (1h/1d/7d/never), lozinka (opcionalno)
   │
   ▼
POST /api/v1/screenshots (multipart) → backend sprema u S3 (privatno) + DB zapis
   │
   ▼
Backend vrati { id, slug (high-entropy), url, expiresAt }
   │
   ▼
Link u clipboard + toast "Privatni link kopiran" · zapis povezan s lokalnim screenshotom
```

### 3.4 Prvo pokretanje
```
Install → tray ikona → onboarding prozor:
  1. Odaberi hotkey
  2. Odaberi save folder
  3. Default format
  4. (opc.) Auto-start s Windowsom
  5. (opc.) Login za cloud sharing — može se preskočiti
→ Spremno. Pritisni hotkey za prvi screenshot.
```

---

## 4. UI/UX opis

**Dizajn jezik:** minimalistički, "premium tool" — tamna baza, jedan naglašeni akcent,
zaobljeni rubovi, suptilne sjene, glatke mikro-animacije (120–180ms). Nikakav vizualni
identitet postojećih alata; vlastiti brand.

**Brand (prijedlog, promjenjivo):**
- Akcent: električno-plava/cyan `#3B82F6 → #06B6D4` gradijent za primarne akcije
- Neutralno: `zinc` skala (900 baza, 100 tekst u dark modu)
- Font: Inter (UI) / JetBrains Mono (dimenzije, hex kodovi)
- Radius: `xl` (12px) na karticama, `lg` (8px) na gumbima
- Ikone: jedan konzistentan stroke set (npr. Lucide), 1.75px stroke

### Prozori
1. **Overlay (capture)** — fullscreen, transparentan, frozen snapshot + 40% dimming.
   Selekcija je "rupa" punog sjaja u zatamnjenju. Live badge s `W×H px`. Magnifier (zoom
   loupe) kod kursora za precizan rub. Crosshair vodilice. Floating toolbar se pojavljuje
   uz donji rub selekcije (ili gornji ako nema mjesta).

2. **Editor** — čist canvas u sredini, lijevo vertikalna paleta alata (ikone, tooltipovi,
   keyboard hint), gore tanak kontekstni bar (boja swatch, stroke slider, font dropdown,
   undo/redo), dolje desno primarne akcije. Zoom/pan. Sve anotacije su **objekti** (mogu se
   selektirati i mijenjati), ne odmah rasterizirane.

3. **Gallery / Home** — grid thumbnaila (lazy-load), search bar gore (instant filter),
   lijevi sidebar s filterima (Svi / Lokalni / Uploadani / po tagu / po datumu). Hover na
   kartici → quick akcije (copy, share, delete, tag). Klik → lightbox.

4. **Settings** — lijevi tabovi: General, Hotkeys, Capture, Save, Privacy, Account, About.
   Hotkey capture polje (klikni pa pritisni kombinaciju). Sve promjene auto-spremane.

5. **Tray menu** — Capture Region · Capture Full Screen · Capture Window · ──— · Open Gallery ·
   Settings · ──— · Quit.

**Accessibility:** puna tipkovnička navigacija, ARIA na editor alatima, fokus prsteni,
poštivanje `prefers-reduced-motion`, kontrast ≥ WCAG AA.

**Mikrointerakcije:** toast notifikacije (ne blokirajuće), copy → kratki "✓ Kopirano",
spinner samo za upload, optimistični UI za save.

---

## 5. Tehnička arhitektura

### 5.1 Visoka razina
```
┌────────────────────────────────────────────────────────────┐
│                      Snapvere DESKTOP                         │
│                                                              │
│  ┌──────────────────────┐      ┌──────────────────────────┐ │
│  │  Frontend (WebView)   │ IPC  │   Rust core (tauri)      │ │
│  │  React + TS + Tailwind│◄────►│                          │ │
│  │                       │invoke│  • capture (xcap)         │ │
│  │  • Overlay window     │ /emit│  • global shortcuts       │ │
│  │  • Editor (canvas)    │      │  • tray + autostart       │ │
│  │  • Gallery / Home     │      │  • SQLite (rusqlite)      │ │
│  │  • Settings           │      │  • file IO (image crate)  │ │
│  └──────────────────────┘      │  • upload client (Faza 3) │ │
│                                 └────────────┬─────────────┘ │
└──────────────────────────────────────────────┼──────────────┘
                                                │ HTTPS (Faza 3)
                                                ▼
┌────────────────────────────────────────────────────────────┐
│                    Snapvere BACKEND (Faza 3)                  │
│  NestJS API  ──►  PostgreSQL (metapodaci, korisnici, linkovi)│
│      │                                                        │
│      └──►  S3-compatible storage (slike, privatno, presigned) │
│      └──►  Sharp (thumbnaili, transcode, EXIF strip)          │
└────────────────────────────────────────────────────────────┘
```

### 5.2 Desktop — moduli

**Rust core (`src-tauri/src/`):**
| Modul | Odgovornost |
|---|---|
| `lib.rs` | App setup, registracija plugina, tray, shortcuts, invoke handler |
| `capture.rs` | Hvatanje monitora/regije/prozora preko `xcap`, encode preko `image` |
| `commands.rs` | Tauri komande pozvane iz frontenda (capture, save, copy, db ops) |
| `db.rs` | SQLite shema + CRUD za povijest, tagove, upload zapise |
| `models.rs` | Serde strukture (Screenshot, Tag, Settings, …) |
| `settings.rs` | Učitavanje/spremanje postavki (tauri-plugin-store) |
| `clipboard.rs` | Pisanje bitmapa u clipboard |
| `tray.rs` | System tray menu i handleri |
| `shortcuts.rs` | Registracija/izmjena globalnih hotkeya |

**Frontend (`src/`):**
| Dio | Odgovornost |
|---|---|
| `main.tsx` | Window router — mounta root po `getCurrentWindow().label` |
| `windows/Overlay` | Selekcija regije, magnifier, floating toolbar |
| `windows/Editor` | Canvas, alati, history, render anotacija |
| `windows/Home` | Galerija + pretraga + filteri |
| `windows/Settings` | Tabovi postavki |
| `lib/ipc.ts` | Typed wrappere oko `invoke()` |
| `lib/canvas/` | Crtanje, alat engine, blur, export |
| `store/` | Zustand store (settings, gallery cache, editor state) |

**Plugini (Tauri v2):** `global-shortcut`, `autostart`, `store`, `sql` (ili rusqlite direktno),
`fs`, `dialog`, `clipboard-manager`, `os`, `notification`, `opener`.

### 5.3 Capture pipeline (detaljno)
```
1. Hotkey → Rust handler
2. xcap::Monitor::all() → za svaki monitor capture_image() → RgbaImage
3. Spremi frozen frames u app state (HashMap<monitor_id, image>)
4. Za svaki monitor: WebviewWindowBuilder → transparent, fullscreen, always-on-top,
   skip-taskbar, pozicioniran na taj monitor. URL: index.html#overlay?mon={id}
5. Frontend overlay traži frozen frame kao data-URL (invoke get_frozen_frame)
   ILI Rust renderira dimming i šalje samo selekciju natrag (manje memorije)
6. Korisnik selektira → frontend šalje {monitor_id, x, y, w, h} natrag
7. Rust izreže regiju iz frozen frame → RgbaImage
8. Ovisno o akciji: encode (PNG/JPG/WebP) → clipboard / disk / editor / upload
9. Zatvori sve overlay prozore, oslobodi frozen frames
```
> Optimizacija: za velike ekrane šalji frozen frame kao binarni IPC (Tauri v2 podržava
> `Response` s bajtovima) umjesto base64 data-URL-a.

### 5.4 Backend (Faza 3, NestJS)
- Moduli: `AuthModule`, `ScreenshotsModule`, `LinksModule`, `UsersModule`, `StorageModule`.
- Slike nikad ne idu kroz public bucket — **presigned URL** za upload i za pregled.
- `StorageModule` apstrahira S3 (MinIO lokalno, AWS/Backblaze/R2 u produkciji).
- Sharp generira thumbnail + strip EXIF/GPS metapodataka pri ingestiji.
- BullMQ (Redis) za async poslove: thumbnaili, čišćenje isteklih linkova (cron).

---

## 6. Database schema

### 6.1 Lokalno (SQLite, na desktopu)

```sql
-- Lokalna povijest screenshotova
CREATE TABLE screenshots (
    id            TEXT PRIMARY KEY,            -- UUID v4
    file_path     TEXT NOT NULL,               -- apsolutna putanja do slike
    thumb_path    TEXT,                        -- putanja do thumbnaila
    name          TEXT NOT NULL,               -- prikazno ime
    format        TEXT NOT NULL,               -- 'png' | 'jpg' | 'webp'
    width         INTEGER NOT NULL,
    height        INTEGER NOT NULL,
    size_bytes    INTEGER NOT NULL,
    source        TEXT NOT NULL,               -- 'region' | 'fullscreen' | 'window'
    created_at    INTEGER NOT NULL,            -- unix ms
    -- upload veza (nullable dok nije uploadano)
    remote_id     TEXT,                        -- id na backendu
    remote_slug   TEXT,                        -- high-entropy slug
    remote_url    TEXT,
    remote_expires_at INTEGER,                 -- unix ms ili NULL = never
    is_uploaded   INTEGER NOT NULL DEFAULT 0
);
CREATE INDEX idx_screenshots_created ON screenshots(created_at DESC);
CREATE INDEX idx_screenshots_name    ON screenshots(name);

CREATE TABLE tags (
    id    INTEGER PRIMARY KEY AUTOINCREMENT,
    name  TEXT NOT NULL UNIQUE
);

CREATE TABLE screenshot_tags (
    screenshot_id TEXT NOT NULL REFERENCES screenshots(id) ON DELETE CASCADE,
    tag_id        INTEGER NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
    PRIMARY KEY (screenshot_id, tag_id)
);

-- Full-text pretraga (ime + tagovi denormalizirano)
CREATE VIRTUAL TABLE screenshots_fts USING fts5(
    id UNINDEXED, name, tags
);

-- Postavke (key-value); alternativno tauri-plugin-store JSON
CREATE TABLE settings (
    key   TEXT PRIMARY KEY,
    value TEXT NOT NULL                        -- JSON-encoded
);
```

### 6.2 Backend (PostgreSQL, Faza 3)

```sql
CREATE TABLE users (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email         CITEXT UNIQUE NOT NULL,
    display_name  TEXT,
    avatar_url    TEXT,
    auth_provider TEXT NOT NULL DEFAULT 'email',  -- 'email' | 'google'
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE screenshots (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id      UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    slug          TEXT UNIQUE NOT NULL,           -- high-entropy, base62, ~22 znaka
    storage_key   TEXT NOT NULL,                  -- S3 ključ (privatno)
    thumb_key     TEXT,
    format        TEXT NOT NULL,
    width         INTEGER NOT NULL,
    height        INTEGER NOT NULL,
    size_bytes    BIGINT NOT NULL,
    -- privatnost
    visibility    TEXT NOT NULL DEFAULT 'private',  -- 'private' | 'unlisted' (nikad 'public' default)
    password_hash TEXT,                            -- argon2id, NULL ako nema
    expires_at    TIMESTAMPTZ,                     -- NULL = never
    -- audit
    view_count    INTEGER NOT NULL DEFAULT 0,
    last_viewed_at TIMESTAMPTZ,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at    TIMESTAMPTZ                       -- soft delete / revoke
);
CREATE INDEX idx_ss_owner   ON screenshots(owner_id, created_at DESC);
CREATE INDEX idx_ss_expires ON screenshots(expires_at) WHERE expires_at IS NOT NULL;
CREATE UNIQUE INDEX idx_ss_slug ON screenshots(slug) WHERE deleted_at IS NULL;

CREATE TABLE access_logs (              -- minimalno, za rate-limit i abuse, ne za tracking
    id            BIGSERIAL PRIMARY KEY,
    screenshot_id UUID REFERENCES screenshots(id) ON DELETE CASCADE,
    ip_hash       TEXT,                  -- hashiran IP (privatnost), TTL retencija
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE auth_tokens (             -- magic-link / refresh
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     UUID REFERENCES users(id) ON DELETE CASCADE,
    token_hash  TEXT NOT NULL,
    type        TEXT NOT NULL,         -- 'magic' | 'refresh'
    expires_at  TIMESTAMPTZ NOT NULL,
    used_at     TIMESTAMPTZ,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

---

## 7. API routes (Backend, Faza 3)

Bazni prefix: `/api/v1`. Auth preko `Authorization: Bearer <jwt>`. Svi odgovori JSON.

### Auth
| Metoda | Ruta | Opis |
|---|---|---|
| `POST` | `/auth/magic-link` | Pošalji magic-link na email |
| `POST` | `/auth/verify` | Razmijeni magic token za JWT (+ refresh) |
| `POST` | `/auth/google` | Google OAuth code → JWT (opcionalno) |
| `POST` | `/auth/refresh` | Refresh token → novi access token |
| `POST` | `/auth/logout` | Invalidiraj refresh token |
| `GET`  | `/me` | Trenutni korisnik |

### Screenshots (uploadi)
| Metoda | Ruta | Opis |
|---|---|---|
| `POST` | `/screenshots/presign` | Zatraži presigned PUT URL (vrati `uploadUrl`, `storageKey`) |
| `POST` | `/screenshots` | Registriraj upload: `{ storageKey, format, w, h, expiresIn, password? }` → `{ id, slug, url, expiresAt }` |
| `GET`  | `/screenshots` | Lista **vlastitih** uploada (paginated, privatno) |
| `GET`  | `/screenshots/:id` | Detalji vlastitog uploada |
| `PATCH`| `/screenshots/:id` | Izmijeni istek / lozinku / ime |
| `DELETE`| `/screenshots/:id` | Revoke + obriši iz storage-a |

### Javni pristup linku (view)
| Metoda | Ruta | Opis |
|---|---|---|
| `GET`  | `/s/:slug` | Metapodaci linka (ili 404 ako istekao/obrisan). Ako password-protected → `{ requiresPassword: true }` |
| `POST` | `/s/:slug/unlock` | `{ password }` → kratkotrajni signed view token |
| `GET`  | `/s/:slug/image` | Presigned redirect na sliku (zahtijeva view token ako je zaštićeno) |

> **Sigurnosna pravila ruta:**
> - `/s/:slug` i image ruta vraćaju `X-Robots-Tag: noindex, noarchive, nofollow`.
> - Slika se nikad ne servira iz public bucketa — uvijek presigned, kratak TTL (npr. 60s).
> - Rate-limit na `/s/:slug/unlock` (brute-force lozinke) — npr. 5/min po IP hashu.
> - Istekli/obrisani linkovi vraćaju jednak 404 (ne otkrivaju je li ikad postojao).

### Health/meta
| Metoda | Ruta | Opis |
|---|---|---|
| `GET` | `/health` | Liveness/readiness |
| `GET` | `/version` | API verzija |

---

## 8. Security / Privacy plan

**Načelo: privacy by default, privacy by design.** Korisnik je vlasnik podataka; cloud je opt-in.

### Privatnost uploada
- **Privatno po defaultu.** Ne postoji "public" visibility kao default; čak ni "unlisted"
  nije pogodiv jer slug je high-entropy.
- **High-entropy slug:** ≥ 128 bita entropije, base62 (~22 znaka), generiran CSPRNG-om
  (`getrandom`/`crypto`). Nije sekvencijalan, nije pogodiv.
- **Istek linka:** 1h / 1d / 7d / never. Cron job briše istekle (storage + DB), presigned
  pristup prestaje vrijediti.
- **Lozinka:** opcionalna, argon2id hash, nikad u plaintextu, rate-limited unlock.
- **Revoke:** korisnik briše upload → soft delete + brisanje objekta iz storage-a; link
  odmah vraća 404.
- **Bez javne galerije.** Lista uploada vidljiva je samo vlasniku, iza auth.
- **Bez indeksiranja:** `X-Robots-Tag: noindex`, `robots.txt` deny, nema sitemapa, nema OG
  preview slike koja bi procurila sadržaj na social platforme (ili eksplicitni opt-in).

### Sensitive-data zaštita
- **Blur/redact je primarni alat** u editoru i u overlay toolbaru.
- **Redact je destruktivan na exportu:** bluranu regiju rasteriziramo u izlaznu sliku tako da
  se original ne može rekonstruirati (ne CSS blur preko originalnih piksela!).
- **Upozorenje prije uploada:** opcionalna lokalna heuristika (regex + lagani OCR) koja
  detektira email/karticu/JWT/IBAN i nudi blur prije slanja. Sve lokalno, ništa se ne šalje
  radi detekcije.
- **EXIF/metadata strip:** pri spremanju i pri uploadu uklanjamo metapodatke.

### Transport & storage
- TLS 1.2+ svuda; HSTS.
- Slike u privatnom S3 bucketu; pristup samo preko kratkotrajnih presigned URL-ova.
- Enkripcija u mirovanju (S3 SSE) i u prijenosu.
- IP-ovi u logovima hashirani; kratka retencija; logovi samo za abuse/rate-limit, ne tracking.

### Auth & app
- JWT access (kratak TTL) + rotirajući refresh token (hashiran u DB).
- Argon2id za lozinke linkova; magic-link tokeni jednokratni, kratkog vijeka.
- CSRF/CORS pravila; striktni CSP na web view stranicama.
- Rate-limiting na auth i unlock rutama.
- Desktop: Tauri capabilities/allowlist — frontend smije zvati samo definirane komande;
  bez `nodeIntegration` rizika (nema Node u web viewu).

### Local
- Lokalna baza i slike u korisničkom app-data direktoriju; opcija "ne pamti povijest".
- Opcionalno: lokalna enkripcija galerije lozinkom (napredno).
- "Obriši sve lokalne podatke" u Settings → Privacy.

### Compliance/etika
- Jasna privacy policy; GDPR-friendly (export/brisanje podataka).
- Bez trećih-strana trackera u desktop aplikaciji ni na view stranicama.

---

## 9. MVP verzija

**Cilj MVP-a: potpuno koristan, offline, privatan capture+anotacija+galerija alat za Windows.**
Nema backenda — dokazuje core vrijednost i brzinu.

**U scopeu (MVP):**
- Capture: regija, fullscreen, aktivni prozor (multi-monitor)
- Output: copy u clipboard, save PNG/JPG/WebP, auto-imenovanje
- Overlay: dimming, drag-select, dimenzije, floating toolbar
- Editor (osnovno): arrow, rectangle, line, pen, marker, **blur/redact**, text, undo/redo,
  color picker, stroke size, crop
- Lokalna galerija: lista + thumbnaili + pretraga (datum/ime/tag) + obriši/kopiraj
- Settings: hotkeys, save folder, default format, tema
- System tray + auto-start opcija
- Dark/light tema

**Izvan scopea (MVP):** sve cloud/share/upload, login, password linkovi, OCR sensitive
detekcija (samo ručni blur), numerirani koraci, drag-out, bulk akcije.

**Definicija gotovog MVP-a:** od pritiska hotkeya do spremljene anotirane slike < 5s; sve
radi offline; galerija pretraživa; instalacijski `.msi`.

---

## 10. Napredna verzija

Sve iz MVP-a + cloud sloj + power-user funkcije:

- **Cloud sharing (Faza 3):** upload, high-entropy privatni linkovi, istek (1h/1d/7d/never),
  password linkovi, revoke, lista uploada, sensitive-data upozorenje.
- **Auth:** email magic-link, Google OAuth opcionalno, sinkronizacija postavki.
- **Editor pro:** numerirani koraci, elipsa, object-level edit/select, drag-out slike,
  re-capture, delayed capture.
- **Galerija pro:** bulk akcije, napredni filteri, export više, lokalna enkripcija.
- **Sensitive detekcija:** lokalni OCR (regex za email/karticu/IBAN/JWT) + prijedlog blura.
- **Cross-platform:** macOS i Linux build.
- **Integracije:** "Otvori u…", webhook nakon uploada, CLI companion.
- **Team (dugoročno):** dijeljeni prostori, role, retention politike za organizacije.

---

## 11. Roadmap po fazama

| Faza | Naziv | Trajanje (orijent.) | Isporuka |
|---|---|---|---|
| **0** | Foundation | 1 tj. | Repo, Tauri v2 scaffold, CI, lint/format, window router, tray skeleton |
| **1** | Capture core | 2 tj. | Hotkey → overlay → selekcija → copy/save; multi-monitor; SQLite zapis |
| **2** | Editor + Gallery | 3 tj. | Anotacije (arrow/rect/line/pen/marker/blur/text), undo/redo, crop; galerija + pretraga; Settings; **MVP release (Windows .msi)** |
| **3** | Cloud & Sharing | 4 tj. | NestJS backend, S3, PostgreSQL; upload, privatni linkovi, istek, password, revoke; auth (magic-link) |
| **4** | Privacy++ & Pro | 3 tj. | Sensitive-data detekcija, EXIF strip, lokalna enkripcija, editor pro alati, bulk galerija |
| **5** | Cross-platform & Polish | 3 tj. | macOS/Linux build, onboarding, integracije, auto-update, performance pass |
| **6** | Beta → 1.0 | 2 tj. | Stabilizacija, telemetrija (opt-in), dokumentacija, marketing site, launch |

> Trajanja su za 1–2 developera; paralelizacija backenda i desktopa skraćuje Faze 3–4.

---

## Dodatak A — Glavne tehničke odluke (ADR sažetak)

| Odluka | Izbor | Razlog |
|---|---|---|
| Desktop framework | **Tauri v2 + Rust** | Mali binary, brzina, sigurnost, niska RAM — pristaje privacy/premium pozicioniranju |
| UI | React + TS + Tailwind | Brz razvoj, tipovi, dizajn sustav |
| Capture | `xcap` crate | Cross-platform monitor/window capture u Rustu |
| Lokalna baza | SQLite (rusqlite/tauri-plugin-sql) | Embedded, pretraga (FTS5), bez servera |
| Slike (Rust) | `image` + `webp`/`mozjpeg` | Encode PNG/JPG/WebP, crop, blur |
| Backend | NestJS | Strukturiran, modularan, TS dijeljen s frontom |
| Storage | S3-compatible (MinIO/R2) | Presigned, privatno, jeftino skalira |
| Baza (cloud) | PostgreSQL | Pouzdano, CITEXT, indeksi, partial index za istek |
| Slug | base62, ≥128 bit CSPRNG | Nepogodiv, privatan po defaultu |

## Dodatak B — Otvorena pitanja
- Default hotkey: `PrintScreen` zna biti zauzet OS-om / drugim alatima → fallback `Ctrl+Shift+1`?
- Naplatni model: free lokalno + plaćeni cloud (storage kvote / retention)?
- Self-host opcija backenda za enterprise (Docker compose)?
