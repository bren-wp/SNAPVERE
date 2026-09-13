# SNAPVERE

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/branding/readme/snapvere-logo-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="assets/branding/readme/snapvere-logo-light.svg">
  <img alt="SNAPVERE — Capture. Edit. Done." src="assets/branding/readme/snapvere-logo-light.svg" width="560">
</picture>

## Snimi ono što ti treba. Zadrži kontrolu nad svojim sadržajem.

**SNAPVERE je brz, local-first alat za screenshotove na Windowsu, Androidu i modernim browserima — za korisnike koji žele kvalitetan capture bez obaveznog računa, analytics buke i automatskog cloud workflowa.**

Na Windowsu odaberi precizno područje i anotiraj ga. Na Androidu napravi jednokratnu sistemski odobrenu snimku. U browseru spremi vidljivo područje, odabranu regiju ili cijelu stranicu unutar sigurnih granica. Core capture ostaje na tvom uređaju.

**Aktualno izdanje: SNAPVERE 0.1.1**  
[Preuzmi v0.1.1](https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.1) · [English README](README.md) · [Korisnički vodič](docs/hr/USER-GUIDE.md) · [Službena stranica](https://snapvere.com) · [Podrška](mailto:info@snapvere.com)

### Preuzmi SNAPVERE

| Platforma | Preporučeni download | Što dobivaš |
| --- | --- | --- |
| **Windows** | **[SNAPVERE-Setup.exe](https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Setup.exe)** | Instalirana tray-first aplikacija s x86/x64/ARM64 payloadima |
| **Windows Portable** | **[SNAPVERE-Portable.exe](https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Portable.exe)** | Jedan portable host bez klasične instalacije |
| **Android 10+** | **[SNAPVERE.apk](https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE.apk)** | Nativna jednokratna MediaProjection capture aplikacija |
| **Chrome** | **[SNAPVERE-Chrome.zip](https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Chrome.zip)** | Visible, region i bounded full-page capture |
| **Edge** | **[SNAPVERE-Edge.zip](https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Edge.zip)** | Chromium MV3 extension paket |
| **Opera** | **[SNAPVERE-Opera.zip](https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Opera.zip)** | Chromium MV3 extension paket |
| **Firefox** | **[SNAPVERE-Firefox.zip](https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Firefox.zip)** | Firefox-compatible MV3 WebExtension paket |
| Android source | [SNAPVERE-Android-Source.zip](https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Android-Source.zip) | Validirani tracked Android source tree |

> Browser ZIP paketi objavljeni su na GitHubu za ručnu/developer-mode instalaciju. **Ne predstavljaju se kao odobreni listing** u Chrome Web Storeu, Edge Add-onsu, Opera Add-onsu ili Mozilla Add-onsu dok stvarni vanjski publisher/review proces ne bude završen. Javni Android APK transparentno je **CI/debug-signed**, a ne Google Play production-signed.

---

## Zašto SNAPVERE?

### Brz kada ga trebaš, neprimjetan kada ga ne trebaš

Windows aplikacija je tray-first. Može živjeti u notification area bez stalnog dashboarda na ekranu. Capture je dostupan iz traya ili globalnih prečaca.

### Više od običnog Print Screena

Windows donosi region, window i screen workflow te frozen-frame region editor s alatima **Pen, Line, Arrow, Box, Highlight, bojama, Undo, Copy i Save**. Browser dodaje visible, selected-region i bounded full-page capture. Android nudi fokusirani jednokratni full-screen workflow.

### Local-first po dizajnu

Za core capture nije potreban SNAPVERE račun. Android aplikacija namjerno **nema `INTERNET` permission**. Browser ekstenzije nemaju široki `<all_urls>` permission ni telemetry/analytics/cloud-upload runtime. Windows capture i PNG encoding odvijaju se lokalno.

### Jedan proizvod, tri okruženja

Desktop aplikacija služi za brzo snimanje i anotacije, Android companion za nativni mobilni capture, a browser ekstenzija za sadržaj unutar web stranice.

### Release proces koji se može provjeriti

Projekt validira architecture payloade, testira Windows lifecycle, pokreće Android lint/test/build, provjerava browser permission/source parity, reproducibilno pakira browser ZIP-ove i nakon objave verificira SHA-256 digeste release asseta.

---

## Windows — snimanje bez prekidanja rada

SNAPVERE za Windows je **.NET 10 / WinUI 3 tray-first aplikacija**.

| Akcija | Primarni unos | Alternativa |
| --- | --- | --- |
| Region Capture | lijevi klik tray ili **Print Screen** | `Ctrl+Shift+1` |
| Window Capture | tray → Capture window | `Ctrl+Shift+2` |
| Screen Capture | tray → Capture screen | `Ctrl+Shift+3` |
| Postavke / recent captures | desni klik tray | — |
| Jezik / About / Exit | desni klik tray | — |

### Region capture koji se može uređivati

Region Capture radi na zamrznutom physical-pixel frameu. Odaberi, pomakni ili promijeni veličinu regije, zatim lokalno anotiraj Pen, Line, Arrow, Box ili Highlight alatom. Rezultat kopiraj ili spremi kao PNG.

### Pravi window workflow

Window Capture koristi nativno otkrivanje top-level prozora i Windows.Graphics.Capture kada je podržan; ne predstavlja obični desktop crop kao pravi window capture.

### Resilient screen capture

Screen Capture preferira Windows.Graphics.Capture/Direct3D i ima monitor fallback za očekivane acquisition probleme. Multi-monitor i DPI conversion logika pokrivena je desktop testovima.

### Lokalne datoteke pod tvojom kontrolom

Gotove snimke po defaultu završavaju u:

```text
Pictures\SNAPVERE
```

File writer koristi temporary-file + final move pristup kako prekinuti encode ne bi izgledao kao gotov PNG. Save As koristi nativni picker; odustajanje ne briše već dovršenu lokalnu snimku.

### Universal javni paketi

`SNAPVERE-Setup.exe` i `SNAPVERE-Portable.exe` sadrže validirane x86, x64 i ARM64 application payloade. Minimalni target je Windows 10 1809/build 17763; WGC putovi traže Windows 10 2004/build 19041 ili noviji.

[Windows arhitektura](docs/hr/ARCHITECTURE.md) · [Capture engine](docs/hr/CAPTURE-ENGINE.md) · [Instalacija](docs/hr/INSTALLATION.md)

---

## Android — jedan tap, jedno odobrenje, jedna lokalna snimka

SNAPVERE za Android 10+ je nativna Java 17 aplikacija na **MediaProjection + MediaStore** API-jima.

Svaki capture započinje novim Android system-consent dijalogom. SNAPVERE ne cacheira projection token za tihe buduće snimke. Nakon odobrenja aplikacija svoj task pomiče iza prethodnog sadržaja i sprema jedan PNG u:

```text
Pictures/SNAPVERE
```

Zadnju čitljivu snimku možeš **otvoriti, podijeliti ili izbrisati**. Zastarjele MediaStore reference uklanjaju se umjesto prikaza nepostojeće snimke.

### Android privacy posture

- nema `android.permission.INTERNET`;
- nema first-party telemetry ni analytics SDK-a;
- nema advertising SDK-a;
- nema automatskog cloud uploadera;
- backup je isključen;
- cleartext traffic je isključen;
- MediaProjection foreground service nije exported;
- novo sistemsko odobrenje za svaki capture.

Verzija: **0.1.1** / `versionCode 11`.

[Android vodič](docs/hr/ANDROID.md) · [Android source/build](android/README.md) · [Privatnost](docs/hr/PRIVACY.md)

---

## Browser ekstenzije — snimi stranicu, ne svoju privatnost

SNAPVERE 0.1.1 uključuje standalone pakete za **Chrome, Edge, Operu i Firefox**.

Odaberi jednu od tri akcije:

- **Capture visible area** — sprema trenutni viewport;
- **Select region** — povuci preko točno željenog područja;
- **Capture full page** — bounded kontrolirani scrolling + lokalni stitching.

Aktualni permission contract namjerno je malen:

```text
activeTab
scripting
downloads
storage
```

Nema `<all_urls>` ni širokog `host_permissions` granta. Source nema telemetry, analytics, oglase, cloud uploader ni remote runtime dependency.

Full-page capture ima izričite tile/canvas/pixel limite kako ne bi nekontrolirano trošio memoriju. Dinamične stranice, video/canvas, sticky elementi i cross-origin frameovi i dalje mogu odstupati od statičkog dokumenta — to ograničenje dokumentiramo otvoreno.

[Browser vodič](docs/hr/BROWSER-EXTENSIONS.md) · [Source vodič](ekstenzije/README.md) · [Extension Privacy](ekstenzije/PRIVACY.md)

---

## Privatnost koju je lako razumjeti

Aktualni SNAPVERE core capture model je local-first:

- **Windows:** lokalni capture/encoding; capture runtime nema first-party screenshot cloud-sync ni telemetry uploader.
- **Android:** nema Internet permission; screenshot se sprema kroz lokalni MediaStore.
- **Browser:** screenshot se obrađuje lokalno; traži se samo minimalni permission set iznad.

Vanjske akcije ostaju eksplicitne. Ako odabereš Android Share, Save As u sinkroniziranu mapu ili vanjski web/email client, odredišna aplikacija/usluga ima vlastita pravila.

Pročitaj [detaljnu privatnost](docs/hr/PRIVACY.md) i [Security Policy](SECURITY.md).

---

## Kvaliteta kroz provjere, ne kroz prazna obećanja

Nijedan ozbiljan softver ne može pošteno garantirati da nikada neće imati grešku. SNAPVERE umjesto toga koristi više automatiziranih gateova koji regresije čine vidljivima prije izdanja.

### Windows CI

- NuGet vulnerability audit i analyzer enforcement;
- x64 build + xUnit testovi;
- x86 build i ARM64 cross-build/package validacija;
- native payload/integrity provjera;
- šest stvarno renderiranih UI površina;
- PR visual comparison prema uspješnom `main` baselineu;
- universal Setup + Portable build;
- exact javni package contract;
- x64/x86 Setup/Portable lifecycle i tray-first probeovi.

### Android CI

- manifest privacy/service/version provjere;
- SDK/tooling;
- `lintDebug` + `lintRelease`;
- JVM unit testovi;
- debug + release build;
- APK signature/alignment;
- validirana Android source arhiva.

### Browser CI

- MV3 manifest i exact permission validacija;
- Chrome/Edge/Opera source parity i normalizirane Firefox razlike;
- EN/HR locale parity;
- icon hash/dimenzije;
- source syntax i forbidden-pattern policy;
- deterministički store vizuali;
- store metadata/privacy provjera;
- dva neovisna ZIP builda koji moraju biti byte-for-byte jednaki.

### Product Contract CI

[`product-version.json`](product-version.json) je canonical aktivni version contract. Poseban validator provjerava Windows, Android i browser verzije, Android EN/HR resource parity, aktivnu dokumentaciju, exact release asset names i relativne Markdown linkove.

Vidi [QA matricu](docs/hr/QA-MATRIX.md) i [Status proizvoda](docs/hr/PRODUCT-STATUS.md).

---

## Integritet izdanja — v0.1.1

Javni v0.1.1 release sadrži **točno osam SNAPVERE asseta**:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE.apk
SNAPVERE-Android-Source.zip
SNAPVERE-Chrome.zip
SNAPVERE-Edge.zip
SNAPVERE-Opera.zip
SNAPVERE-Firefox.zip
```

Release workflow gradi/validira platform payloade, računa SHA-256, validira Windows runtime/package ugovor, kreira/provjerava release tag, objavljuje samo odobrene datoteke i zatim uspoređuje objavljeni GitHub digest svakog asseta s lokalno validiranom vrijednošću.

Povijesna izdanja ostaju povijesna: **v0.1.0 se ne prepisuje niti mu se retroaktivno dodaju browser paketi.**

[Release Notes 0.1.1](RELEASE_NOTES_0.1.1.md) · [Versioning & Releases](docs/hr/VERSIONING-RELEASES.md)

---

## Jezici

Engleski je canonical fallback. Windows aplikacija nudi **28 ugrađenih jezičnih izbora**, uključujući hrvatski. Android i browser ekstenzije imaju namjenske EN/HR resurse. Runtime jezici ne zahtijevaju translation API.

---

## Dokumentacija

Za početak koristi [središte dokumentacije](docs/hr/README.md).

Preporučeno:

- [Korisnički vodič](docs/hr/USER-GUIDE.md)
- [Instalacija](docs/hr/INSTALLATION.md)
- [Rješavanje problema](docs/hr/TROUBLESHOOTING.md)
- [Privatnost](docs/hr/PRIVACY.md)
- [Status proizvoda](docs/hr/PRODUCT-STATUS.md)
- [QA matrica](docs/hr/QA-MATRIX.md)
- [Arhitektura](docs/hr/ARCHITECTURE.md)
- [Android](docs/hr/ANDROID.md)
- [Browser ekstenzije](docs/hr/BROWSER-EXTENSIONS.md)
- [Security & Performance 0.1.1](docs/hr/SECURITY-PERFORMANCE-0.1.1.md)
- [Verzioniranje i izdanja](docs/hr/VERSIONING-RELEASES.md)

---

## Tehnologija

C# / .NET 10 · WinUI 3 · Windows App SDK 1.8 · Windows.Graphics.Capture · Direct3D 11 · Win32/DWM/GDI interop · Java 17 · Android MediaProjection/MediaStore · Manifest V3/WebExtensions · xUnit · JUnit 4 · GitHub Actions

---

## Licenca, developer i podrška

**SNAPVERE 0.0.7 i noviji distribuiraju se pod SNAPVERE Commercial Software License Agreement licencom u [`LICENSE`](LICENSE).** SNAPVERE je proizvodni brand. **Brendigo** je developer i publisher.

Službena stranica: **https://snapvere.com**  
Podrška: **info@snapvere.com**  
Developer: **https://brendigo.com**

---

### Želiš capture bez nepotrebnog clutera?

**[Preuzmi SNAPVERE 0.1.1](https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.1)** i odaberi paket za svoju platformu.

**SNAPVERE — Capture. Edit. Done.**
