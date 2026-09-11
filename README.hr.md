# SNAPVERE

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/branding/readme/snapvere-logo-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="assets/branding/readme/snapvere-logo-light.svg">
  <img alt="SNAPVERE — Capture. Edit. Done." src="assets/branding/readme/snapvere-logo-light.svg" width="560">
</picture>

**Capture. Edit. Done. — brza, lokalna i privatna aplikacija za snimanje zaslona za Windows i Android koju razvija Brendigo.**

[English README](README.md) · [Službena stranica](https://snapvere.com) · [Podrška](mailto:info@snapvere.com) · [Developer: Brendigo](https://brendigo.com)

SNAPVERE 0.1.0 objedinjuje produkcijsku tray-first Windows aplikaciju i nativni Android companion u jednom validiranom release ugovoru. Objavljeni povijesni tagovi i asseti ostaju nepromjenjivi.

## Windows capture

| Akcija | Primarni unos | Alternativa |
| --- | --- | --- |
| Snimanje područja | lijevi klik tray ili **Print Screen** | `Ctrl+Shift+1` |
| Snimanje prozora | desni klik tray → Capture window | `Ctrl+Shift+2` |
| Snimanje zaslona | desni klik tray → Capture screen | `Ctrl+Shift+3` |
| Postavke / nedavne snimke | desni klik tray | — |
| Jezik / About / Exit | desni klik tray | — |

Region Capture nudi frozen-frame fizičku pixel selekciju, resize/pomicanje, Pen, Line, Arrow, Box i Highlight, četiri boje, Undo, Copy i Save. Window Capture koristi nativno otkrivanje top-level prozora i Windows.Graphics.Capture `CreateForWindow`; ne prelazi potajno na screen crop. Screen Capture preferira Windows.Graphics.Capture/Direct3D i zadržava resilient monitor fallback za očekivane probleme akvizicije.

PNG se prvo sigurno dovršava, a zatim se po potrebi otvara nativni Windows **Save As**. Odustajanje od Save As zadržava dovršenu snimku u `Pictures\SNAPVERE`. Detalji: [Spremanje](docs/hr/SAVE-LOCATION.md).

## Tray-first Windows UX

Normalni start zadržava SNAPVERE u Windows notification area umjesto otvaranja launcher dashboarda. Nativni tray host koristi `NOTIFYICON_VERSION_4`, ponovno postavlja verziju nakon Explorer/taskbar rekreacije, zadržava standardni tooltip i podržava pointer i keyboard activation. Region activation je debounced kako dupli notification event ne bi pokrenuo dvije snimke.

Implementirane preference su **Start SNAPVERE with Windows**, **Include cursor on capture** i **Language**. Recent Captures je lokalni i ograničen. Nema telemetry workera, cloud-upload klijenta, remote-command kanala ni automatskog updatera.

## Android 0.1.0

SNAPVERE za Android 10+ je nativna Java 17 aplikacija izgrađena na Android MediaProjection i MediaStore API-jima. Svaka snimka zahtijeva novo Android sustavsko dopuštenje. Consent token se ne cacheira niti ponovno koristi i SNAPVERE ne snima kontinuirano u pozadini.

Implementirani Android workflow uključuje:

- izričito full-screen snimanje;
- lokalni PNG u `Pictures/SNAPVERE`;
- validirane **Otvori / Podijeli / Izbriši** radnje za zadnju snimku;
- engleske i hrvatske resurse;
- korisnički pokrenute Web / Podrška / Privatnost / Uvjeti radnje;
- responzivno slaganje akcija, velike touch targete, system insets i namjerno tamni dizajn;
- bez računa, telemetrije, analyticsa, oglasa, cloud uploada i `INTERNET` permissiona.

### Android stabilnost 0.1.0

- 5 s Activity-hide i 7 s first-frame guard;
- provjera `Handler.post*` rezultata i `ImageReader.acquireLatestImage()`;
- kontrolirano rukovanje foreground-service initialization failureom;
- exception-safe MediaProjection / VirtualDisplay / ImageReader / HandlerThread cleanup po resursu;
- owner-aware capture lock release koji stale teardownu ne dopušta čišćenje tuđe aktivne sesije;
- `RGBA_8888` pixel-stride, row-stride, row-padding i buffer-size validacija prije bitmap kopiranja;
- `ByteBuffer.rewind()` prije kopiranja i deterministički Image/Bitmap cleanup redoslijed;
- conversion/provider/allocation failure ostaje unutar kontroliranog teardowna i prikazuje lokaliziranu recovery poruku.

Android CI pokreće `lintDebug`, `lintRelease`, JVM testove, debug/release build, signature/alignment i SHA-256. Za v0.1.0 javni `SNAPVERE.apk` namjerno koristi isti provjereni CI/debug-potpisani paketni put; privatni production keystore nije potreban.

Detalji: [Android arhitektura i QA](docs/hr/ANDROID.md) · [Android source/build vodič](android/README.md).

## Privatnost i sigurnost

Windows obrada snimki je lokalna. Desktop runtime nema first-party HTTP/socket klijent, WebView/WebView2 ni JavaScript execution path. Android manifest namjerno nema `android.permission.INTERNET`; cleartext i backup su isključeni, a MediaProjection servis nije exported.

0.1.0 zadržava stroge build/security kontrole:

- NuGet audit za direktne i tranzitivne ovisnosti od `low` severity nadalje;
- `NU1901`–`NU1904` kao build failure;
- GitHub Actions pinane na pune SHA vrijednosti;
- deterministic/analyzer .NET build;
- arhitekturne SHA-256 manifeste ugrađene u Portable host;
- transactional Portable cache rebuild za missing/modified/unexpected/reparse-point sadržaj;
- Android manifest privacy/version/service gate i lint warnings-as-errors;
- objavu samo uz exact asset ugovor i post-publication GitHub digest provjeru.

To nije tvrdnja da softver može biti zajamčeno bez svake ranjivosti. Vidi [Security policy](SECURITY.md), [0.1.0 sigurnost/performance](docs/hr/SECURITY-PERFORMANCE-0.1.0.md) i povijesni [0.0.9 izvještaj](docs/hr/SECURITY-PERFORMANCE-0.0.9.md).

## Performanse i stabilnost

SNAPVERE je event-driven dok miruje. Tray/hotkey hostovi koriste nativne message loopove bez pollinga, capture/D3D resursi stvaraju se za aktivni rad, sekundarni prozori su on-demand, Recent Captures je ograničen, a Android nema idle capture loop niti mrežni worker.

Ne obećava se fiksni CPU/RAM postotak jer OS, monitori/DPI, driveri, Android OEM, rezolucija i aktivni capture/editor rad mijenjaju potrošnju.

## Javni release ugovor 0.1.0

Valjano v0.1.0 GitHub izdanje sadrži **točno četiri javna asseta**:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE.apk
SNAPVERE-Android-Source.zip
```

`SNAPVERE-Setup.exe` i `SNAPVERE-Portable.exe` sadrže x86, x64 i ARM64 Windows payloade i automatski biraju kompatibilni payload. Minimalni Windows target ostaje Windows 10 1809/build 17763; WGC putovi zahtijevaju Windows 10 2004/build 19041 ili noviji.

`SNAPVERE.apk` je instalabilni Android CI/debug-potpisani paket iz validiranog 0.1.0 sourcea. Release put i dalje zahtijeva debug/release lint, JVM testove, uspješan debug/release build, provjeru APK potpisa, ZIP alignment i SHA-256. Paket se ne predstavlja kao Google Play/production-potpisan i ne zahtijeva privatne signing secrete.

Budući da javni 0.1.0 APK koristi CI/debug signing identitet, to nije podržani dugoročni production update lineage. Kasniji Android kanal s drugačijim stabilnim production ključem može zahtijevati deinstalaciju i novu instalaciju.

`SNAPVERE-Android-Source.zip` generira se izravno iz validiranog Git `android/` treea i ne sadrži generirani build output, Gradle cache niti signing materijal.

## Automatizirani QA

### Windows

GitHub Actions:

- restore s NuGet auditom;
- x64 build/test;
- x86 build i ARM64 cross-build;
- validacija sva tri native payloada i integrity manifesta;
- šest stvarno renderiranih površina: Region, Window, Tray, Options, Language i About;
- visual regression prema zadnjem zelenom `main` baselineu;
- universal Setup/Portable build;
- x64/x86 Setup+Portable lifecycle i tray-first testovi.

ARM64 na hosted x64 runneru ostaje cross-build/package dokaz, ne fizički ARM64 runtime test.

### Android

Android CI provjerava manifest privacy/service/version ugovor, SDK/tooling, debug/release lint, JVM testove, debug/release build, debug potpis/alignment i SHA-256. Release workflow dodatno provjerava javni CI/debug-potpisani APK, source arhivu, digest kroz Actions artifact transfer i sva četiri objavljena GitHub digesta.

Zeleni Android workflow je automatizirani build/package dokaz, ne tvrdnja o iscrpnom runtime testu na svakom fizičkom OEM uređaju.

## Android potpisivanje paketa za 0.1.0

Javni 0.1.0 APK namjerno slijedi CI/debug signing put i zato **ne zahtijeva privatne Android keystore secrete**. `apksigner` i ZIP-alignment provjere ostaju obavezne prije nego APK može doći do release joba.

Ovaj izbor omogućuje odmah instalabilan APK bez spremanja privatnog production ključa na GitHub. Ne treba ga tumačiti kao trajnu production/Play signing strategiju.

## Jezici

English je canonical default/fallback. Windows nudi 28 ugrađenih jezika uključujući hrvatski. Android trenutno ima namjenske engleske i hrvatske resurse i koristi standardni Android fallback za ostale locale.

Windows jezik sprema se lokalno u:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Nema translation API-ja ni background language network servisa.

## Arhitektura

Windows:

```text
Tray / Print Screen / hotkeys
    ↓
Skriveni WinUI runtime koordinator
    ↓
Region / Window / Screen workflow
    ↓
CaptureFrame (fizički BGRA8 pikseli)
    ↓
Crop / anotacija / PNG encode
    ↓
Lokalni PNG → opcionalni Save As / Clipboard
```

Android:

```text
Izričiti Capture tap
    ↓
MediaProjection dopuštenje
    ↓
Foreground mediaProjection servis
    ↓
Potvrda da je Activity skriven
    ↓
VirtualDisplay + ImageReader + bounded frame wait
    ↓
RGBA stride/buffer validacija
    ↓
MediaStore PNG → Otvori / Podijeli / Izbriši
```

## Dokumentacija

Engleska dokumentacija: [`docs/`](docs/). Hrvatska dokumentacija: [`docs/hr/`](docs/hr/).

Ključni dokumenti: [Arhitektura](docs/hr/ARCHITECTURE.md), [Tray UX](docs/hr/TRAY-UX.md), [Capture engine](docs/hr/CAPTURE-ENGINE.md), [Region Capture](docs/hr/REGION-CAPTURE.md), [Window Capture](docs/hr/WINDOW-CAPTURE.md), [Spremanje](docs/hr/SAVE-LOCATION.md), [Postavke](docs/hr/SETTINGS.md), [Instalacija](docs/hr/INSTALLATION.md), [Android](docs/hr/ANDROID.md), [0.1.0 sigurnost/performance](docs/hr/SECURITY-PERFORMANCE-0.1.0.md), [Branding](docs/hr/BRANDING.md) i [Image pipeline](docs/hr/IMAGE-PIPELINE.md).

## Tehnologija

- C# / .NET 10
- WinUI 3 / Windows App SDK 1.8 stable line
- Windows.Graphics.Capture + Direct3D 11
- Win32 / DWM / GDI interoperabilnost
- Java 17 / Android MediaProjection / MediaStore
- deterministic buildovi, nullable/analyzer enforcement i centralni NuGet management
- xUnit + JUnit 4 + GitHub Actions

## Dijagnostika

Windows startup log:

```text
%LOCALAPPDATA%\SNAPVERE\Logs\startup.log
```

Pikseli snimke ne zapisuju se namjerno u taj log.

## Licenca i vlasništvo

**SNAPVERE 0.0.7 i noviji distribuiraju se pod SNAPVERE Commercial Software License Agreement licencom u [`LICENSE`](LICENSE).** SNAPVERE je naziv proizvoda; Brendigo je developer i izdavač. Službena stranica: **snapvere.com**. Podrška: **info@snapvere.com**.

---

**SNAPVERE — Capture. Edit. Done.**  
Razvija i objavljuje **Brendigo** · [snapvere.com](https://snapvere.com) · [info@snapvere.com](mailto:info@snapvere.com) · [brendigo.com](https://brendigo.com)
