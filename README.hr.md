# SNAPVERE

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/branding/readme/snapvere-logo-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="assets/branding/readme/snapvere-logo-light.svg">
  <img alt="SNAPVERE — Capture. Edit. Done." src="assets/branding/readme/snapvere-logo-light.svg" width="560">
</picture>

**Capture. Edit. Done. — brza, lokalna i privatna aplikacija za snimanje zaslona za Windows i Android koju razvija Brendigo.**

[English README](README.md) · [Službena stranica](https://snapvere.com) · [Podrška](mailto:info@snapvere.com) · [Developer: Brendigo](https://brendigo.com)

SNAPVERE je tray-first Windows aplikacija za brzo snimanje područja, prozora i zaslona, lagane anotacije i lokalno PNG spremanje. Android 10+ companion koristi službeni MediaProjection model i isti local-first privacy ugovor.

> **v0.0.9 je aktualni release milestone u završnoj validaciji.** Objavljeni povijesni tagovi, releaseovi i asseti ne prepisuju se naknadnim razvojem.

## Windows kontrole

| Akcija | Primarni unos | Alternativa |
| --- | --- | --- |
| Snimanje područja | lijevi klik tray ili **Print Screen** | `Ctrl+Shift+1` |
| Snimanje prozora | desni klik tray → Capture window | `Ctrl+Shift+2` |
| Snimanje zaslona | desni klik tray → Capture screen | `Ctrl+Shift+3` |
| Postavke / nedavne snimke | desni klik tray | — |
| Jezik | kontrola jezika u trayu ili Options | — |
| About / Exit | desni klik tray | — |

Scrolling Capture ne rezervira globalni shortcut dok workflow stvarno nije implementiran.

## Spremanje snimki

Region, Window i Screen snimke prvo se sigurno dovršavaju kao PNG u standardnoj SNAPVERE mapi. Nakon uspješnog spremanja SNAPVERE može otvoriti izvorni Windows **Save As** picker kako bi korisnik odabrao konačnu mapu i naziv.

Odustajanje od pickera zadržava dovršenu PNG datoteku u `Pictures\SNAPVERE`. Premještanje koristi asinkronu kopiju, privremenu datoteku na odredištu, završni atomic replacement, PNG-only validaciju i Windows file-identity provjere. Alias, hard-link ili nesigurni identity slučajevi zadržavaju recovery kopiju izvora. Clipboard-only Copy ostaje bez Save As koraka.

Detalji: [Save location](docs/hr/SAVE-LOCATION.md).

## Region Capture

Implementirani workflow uključuje zamrznuti frame, fizičku pixel selekciju, pomicanje/resize, osam ručki, live dimenzije, Pen, Line, Arrow, Box i Highlight alate, četiri boje, Undo, `Ctrl+Z`, `Ctrl+C`, Copy, Save, Enter/dvostruki klik za spremanje i Esc za odustajanje. Anotacije se renderiraju u konačni PNG.

## Window Capture

SNAPVERE otkriva vidljive top-level prozore prije prikaza overlayja, koristi DWM extended-frame granice, filtrira SNAPVERE/tool/cloaked/nevidljive prozore, prikazuje DPI-aware picker i završno snima prozor kroz Windows.Graphics.Capture `CreateForWindow`. Window Capture ne prelazi potajno na običan screen crop.

## Screen Capture

Windows.Graphics.Capture + Direct3D 11 je preferirani backend gdje je podržan. Za očekivane probleme pri monitor captureu postoji resilient GDI fallback. Capture/D3D resursi stvaraju se samo kada su potrebni i ne ostaju aktivni dok aplikacija miruje u trayu.

## Pouzdanost i pristupačnost Windows traya

Nativni notification-area host u v0.0.9 koristi moderni `NOTIFYICON_VERSION_4` callback ugovor. Nakon svakog uspješnog dodavanja ikone, uključujući ponovno stvaranje taskbara/Explorera, SNAPVERE postavlja verziju protokola. Standardni tooltip ostaje uključen putem `NIF_SHOWTIP`, callback event čita se iz low worda `lParam`, a podržane su i keyboard select/context-menu notifikacije. Region Capture i dalje koristi debounce kako dupli activation event ne bi pokrenuo dva capture workflowa.

## Android

SNAPVERE za Android 10+ je nativna aplikacija za korisnički odobreno snimanje cijelog zaslona. Nema korisnički račun, telemetriju, analytics SDK, cloud upload niti `INTERNET` permission. PNG se sprema kroz MediaStore u `Pictures/SNAPVERE`.

Svaka snimka dobiva novi Android MediaProjection consent. SNAPVERE ne cacheira niti ponovno koristi consent token. Activity se nakon odobrenja premješta iza ciljane aplikacije, a VirtualDisplay se stvara tek nakon potvrde `MainActivity.onStop()`.

### Stabilnost capture lifecyclea

v0.0.9 dodaje dvije eksplicitne granice neuspjeha:

- 5 sekundi za Activity → background handoff;
- 7 sekundi za prvi frame nakon stvaranja VirtualDisplaya.

Ako ImageReader/driver ne isporuči frame, sesija završava kontrolirano umjesto da foreground servis i globalni capture lock ostanu trajno aktivni. Cleanup je idempotentan i odvija se po resursu: pogreška pri oslobađanju jednog MediaProjection/VirtualDisplay/ImageReader objekta ne prekida čišćenje ostalih resursa.

`ImageReader.acquireLatestImage()` je zaštićen od runtime iznimki, svaki dohvaćeni `Image` se zatvara, a prije alokacije padded bitmapa provjeravaju se width, pixel stride, row stride i integer overflow. `CaptureBufferLayout` ima direktne JVM unit testove.

### Android UI/UX

Android koristi istu tamnu SNAPVERE paletu kao Windows: canvas `#0B0D12`, surface `#12151C`, raised surface `#181C25`, border `#2A3140`, primarni tekst `#F6F7FB`, sekundarni `#98A2B3`, violet accent `#7C6CFF` i success `#45D6A2`.

OEM `forceDark` je onemogućen jer aplikacija već ima vlastiti dark theme. Parovi akcijskih gumba automatski prelaze u vertikalni raspored na uskim zaslonima ili pri font scaleu 1,25x+, touch targeti ostaju najmanje 52 dp, sadržaj poštuje system-bar insete i stranica ostaje scrollable.

Capture, Otvori, Podijeli, Izbriši, Web, Podrška, Privatnost i Uvjeti imaju kontrolirane failure stateove. MediaStore/provider/intent/clipboard problem prikazuje status korisniku umjesto da sruši Activity. Support prvo pokušava `mailto:info@snapvere.com`, a zatim lokalni clipboard fallback.

Engleski i hrvatski status/recovery stringovi održavaju se zajedno.

### Android CI

Za svaku Android promjenu CI provjerava:

1. manifest privacy/service ugovor — `INTERNET` je zabranjen, cleartext i backup ostaju isključeni, a MediaProjection servis nije exported;
2. `lintDebug` i `lintRelease` uz warnings-as-errors;
3. `testDebugUnitTest`, uključujući capture-buffer testove;
4. debug build;
5. minificirani/shrunk release build;
6. debug APK potpis kroz `apksigner`;
7. ZIP alignment;
8. SHA-256;
9. Actions artifact vezan uz točan source SHA.

CI APK je debug-potpisan development/internal build. Produkcijski Play/release potpis zahtijeva zasebno čuvani privatni signing key; repozitorij ne izmišlja produkcijski potpis niti sprema signing secret.

Android nema isti opći top-level-window capture primitive kao Windows. Zato se Windows-style Window Capture ne prikazuje kao Android funkcija. Region selection/annotation parity ostaju zasebne mogućnosti dok stvarno nisu implementirane i device-testirane.

Detalji: [Android aplikacija](docs/hr/ANDROID.md) · [Android source/build vodič](android/README.md).

## About, podrška i pravne poveznice

Korisnički pokrenute destinacije:

- `https://snapvere.com`
- `info@snapvere.com`
- `https://snapvere.com/privacy`
- `https://snapvere.com/terms`
- `https://brendigo.com`

Desktop aplikacija ih ne prefetchira. Ako Windows nema mail handler, Podrška kopira `info@snapvere.com` u lokalni clipboard.

## Jezici

English (`en`) je canonical default i fallback. Ugrađeni katalog nudi 28 jezika, uključujući hrvatski (`hr`). Ako odabrani jezik nema prijevod pojedinog stringa, koristi se canonical English fallback umjesto prikaza resource ključa.

Odabrani jezik sprema se lokalno u:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Nema translation API-ja, mrežnih poziva, polling servisa ni dodatnog background threada za prijevod.

## Postavke

Implementirane preference uključuju **Start SNAPVERE with Windows**, **Include cursor on capture** i **Language**. Windows startup je per-user i pokreće isti tray-first program bez dashboard prozora.

## Sigurnosni hardening v0.0.9

- NuGet audit uključuje direktne i tranzitivne ovisnosti od `low` severity nadalje.
- `NU1901`–`NU1904` su build failure.
- GitHub Actions su pinani na pune commit SHA vrijednosti.
- Normalni CI checkout ne ostavlja repository credentials.
- Dependabot prati NuGet i Actions ovisnosti.
- Setup path-boundary validacija pravilno tretira zaštićeni direktorij i njegove poddirektorije.
- ZIP ekstrakcija odbija absolute/traversal putanje, duplicate destination, prevelik entry count i expanded-size te koristi bounded random staging nazive.
- Portable prije izvršavanja reusable `%TEMP%` cachea provjerava arhitekturni SHA-256 manifest; missing/modified/unexpected/reparse-point sadržaj uzrokuje transactional rebuild i ponovnu validaciju.
- Prvi direct-ZIP integrity dizajn odbijen je u lifecycle CI-u zbog startup regresije; timeout nije povećan, nego je runtime provjera prebačena na jedan sekvencijalni SHA-256 prolaz.
- Android capture hardening sprječava beskonačno aktivnu projekciju nakon frame timeouta ili cleanup iznimke.
- Nema telemetry, cloud-upload, remote-command ni automatic-update kanala u v0.0.9.

Trenutačni desktop kod nema WebView/WebView2 ni HTML/JavaScript runtime površinu, pa browser-style XSS nije aktualna aplikacijska površina. SNAPVERE nije sandbox protiv proizvoljnog zlonamjernog koda koji već radi kao isti Windows korisnik.

Detalji: [Security policy](SECURITY.md) · [v0.0.9 sigurnost i performanse](docs/hr/SECURITY-PERFORMANCE-0.0.9.md).

## Performanse i stabilnost

- tray i global-hotkey hostovi blokiraju na Win32 message loopovima bez periodičnog app pollinga;
- capture/D3D resursi stvaraju se na zahtjev;
- sekundarni prozori nastaju samo kada su potrebni;
- Recent Captures enumeracija je lokalna i ograničena te uklanja nepotreban metadata refresh;
- Portable integrity provjera čita cache jednom sekvencijalno za SHA-256 i ne dekomprimira ponovno payload samo radi usporedbe;
- Android capture ima bounded handoff/frame wait i per-resource cleanup bez idle workera;
- nema telemetry workera, file watchera ni background network pollinga.

Ne obećava se fiksna CPU/RAM brojka jer Windows verzija, DPI, monitori, driveri, Android OEM ponašanje i aktivni capture/editor mijenjaju radni set.

## Universal Windows packaging

Javni Windows release ugovor sadrži točno dvije datoteke:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
```

Svaki host sadrži native payloade za x86, x64 i ARM64 i automatski bira kompatibilni payload. Minimalni Windows application target ostaje Windows 10 version 1809 / build 17763; WGC capture put zahtijeva Windows 10 version 2004 / build 19041 ili noviji.

SNAPVERE namjerno nema zaseban `uninstall.exe`. Windows Installed apps koristi instalirani `SNAPVERE-Setup.exe --uninstall`; isti Setup provjerava installation marker prije uklanjanja i ne briše korisničke screenshotove.

Android CI artifact nije dio ovog exact-two-file Windows release ugovora.

## Automatizirani QA

Windows CI builda/testira x64 i x86 te cross-builda ARM64. Universal package gate provjerava sva tri native payloada i njihove integrity manifeste, šest stvarno renderiranih WinUI površina, exact two-file package contract te x64/x86 Setup/Portable lifecycle i tray-first ponašanje.

Visual-QA površine: Region, Window, Tray, Options, Language i About. Thresholdi se ne spuštaju radi skrivanja regresije.

Android CI zasebno provjerava manifest ugovor, debug/release lint, JVM unit testove, debug/release build, potpis, alignment i SHA-256 artifacta.

ARM64 dokaz na hosted x64 runneru je cross-build/package dokaz, ne stvarni ARM64 hardware runtime. Zeleni Android CI je build/lint/unit/package dokaz, ne tvrdnja da je svaki OEM uređaj fizički testiran.

## Release 0.0.9 disciplina

Objava se pokreće tek nakon finalnog zelenog source/PR/main CI-a. Release workflow ponovno gradi i validira source prije taga. Immutable `v0.0.9` lookup razlikuje samo stvarni HTTP 404 kao “tag ne postoji”; druge GitHub API greške prekidaju objavu. Annotated-tag objekt i tag ref također moraju biti uspješno stvoreni i imati valjani SHA prije objave.

GitHub Release nakon toga smije sadržavati samo `SNAPVERE-Setup.exe` i `SNAPVERE-Portable.exe`, a zadnji korak uspoređuje GitHub asset SHA-256 digest s lokalno izračunatim release digestom.

## Dokumentacija

Engleska dokumentacija: [`docs/`](docs/). Hrvatska dokumentacija: [`docs/hr/`](docs/hr/).

Ključni dokumenti: [Arhitektura](docs/hr/ARCHITECTURE.md), [Tray UX](docs/hr/TRAY-UX.md), [Capture engine](docs/hr/CAPTURE-ENGINE.md), [Region Capture](docs/hr/REGION-CAPTURE.md), [Window Capture](docs/hr/WINDOW-CAPTURE.md), [Save location](docs/hr/SAVE-LOCATION.md), [Postavke](docs/hr/SETTINGS.md), [Sigurnost/performance 0.0.9](docs/hr/SECURITY-PERFORMANCE-0.0.9.md), [Instalacija](docs/hr/INSTALLATION.md), [Branding](docs/hr/BRANDING.md), [Image pipeline](docs/hr/IMAGE-PIPELINE.md) i [Android](docs/hr/ANDROID.md).

## Tehnologija

- C# / .NET 10
- WinUI 3 / Windows App SDK 1.8 stable line
- Windows.Graphics.Capture + Direct3D 11
- Win32 / DWM / GDI interop
- Java 17 / nativni Android API / MediaProjection / MediaStore
- deterministic buildovi, nullable/analyzer enforcement i centralni NuGet management
- xUnit + JUnit 4 + GitHub Actions

## Diagnostics

Windows startup diagnostics:

```text
%LOCALAPPDATA%\SNAPVERE\Logs\startup.log
```

Screenshot pikseli namjerno se ne zapisuju u startup log.

## Licenca i vlasništvo

**SNAPVERE 0.0.7 i noviji distribuira se pod SNAPVERE Commercial Software License Agreement ugovorom u [`LICENSE`](LICENSE).** SNAPVERE je proizvodni brand. Brendigo je developer i publisher. Službena stranica je **snapvere.com**, a podrška **info@snapvere.com**.

---

**SNAPVERE — Capture. Edit. Done.**  
Razvija i objavljuje **Brendigo** · [snapvere.com](https://snapvere.com) · [info@snapvere.com](mailto:info@snapvere.com) · [brendigo.com](https://brendigo.com)
