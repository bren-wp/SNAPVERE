# Arhitektura SNAPVERE-a

## Status

Aktualno javno izdanje je **SNAPVERE 0.1.1**. Proizvod ima tri namjerno odvojene runtime površine:

- tray-first Windows aplikaciju za capture;
- nativni Android 10+ companion;
- standalone browser ekstenzije za Chrome, Edge, Operu i Firefox.

Platforme dijele identitet proizvoda, local-first načela privatnosti, version/release governance i dokumentaciju, ali se **ne predstavljaju** kao jedan zajednički cross-platform codebase. Svaki runtime koristi nativni capture i lifecycle model svoje platforme.

Objavljeni povijesni tagovi/releaseovi ostaju nepromjenjivi. Post-release maintenance na `main` ne pomiče niti prepisuje `v0.1.1`.

## Ciljevi dizajna

SNAPVERE daje prioritet niskoj capture latenciji, fizičkoj pixel točnosti, mixed-DPI ispravnosti, ograničenom ownershipu resursa, determinističkom cleanupu, local-first obradi, minimalnim permissionima, predvidljivom startup/lifecycle ponašanju i maloj razumljivoj dependency površini.

Aktivni cross-platform version contract je [`product-version.json`](../../product-version.json). Product Contract CI provjerava da Windows, Android, browser manifesti/store metadata i aktivna dokumentacija ostanu usklađeni s 0.1.1.

# Windows arhitektura

## Slojevi projekta

### Snapvere.App

WinUI 3 composition root i presentation layer.

- `App` upravlja dependency injectionom, UI-thread routingom, runtime/visual probeovima i životnim vijekom prozora.
- `CaptureCenterWindow` je skriveni capture coordinator, ne normalni launcher dashboard.
- `TrayMenuWindow`, `OptionsWindow`, `LanguagePickerWindow` i `AboutWindow` nastaju na zahtjev.
- `RegionCaptureWindow` upravlja interaktivnom region selekcijom i anotacijama.
- `WindowTargetPicker` koordinira DPI-aware target overlaye.

Programatski WinUI treeovi ostaju preferirani za sekundarne prozore jer ih CI/runtime probeovi mogu materijalizirati bez dodatnih XAML resource-load ovisnosti.

### Snapvere.Application

UI-neovisni workflowi i lokalna persistencija:

- `RegionCaptureWorkflow`
- `WindowCaptureWorkflow`
- `ScreenCaptureWorkflow`
- `CaptureFileWriter`
- `CaptureHistoryService`
- `CapturePreferencesService`

Preference i capture history ostaju lokalni. Aktualna linija zadržava filesystem-policy/security handling u preference loadu, history enumeraciji i temporary-file cleanupu kako sekundarne lokalne I/O greške ne bi nepotrebno prekinule UI tok.

### Snapvere.Domain

Capture geometrija i value objecti u fizičkim pikselima bez UI ovisnosti.

### Snapvere.Capture

Windows acquisition i desktop integracija:

- monitor/window discovery;
- DPI i virtual-desktop geometrija;
- global hotkey host;
- Windows.Graphics.Capture / D3D11;
- GDI monitor compatibility backend;
- native window Z-order filtering i targeting.

### Snapvere.Imaging

Deterministički BGRA8 crop, annotation rendering i PNG encoding.

### Snapvere.Packaging / Snapvere.Setup / Snapvere.Portable

Guarded embedded-payload obrada, architecture selection, per-user Setup lifecycle, same-Setup uninstall i Portable extraction/launch. Portable reusable cache provjerava se prema trusted architecture-specific SHA-256 manifestima ugrađenima u host prije izvršavanja.

### Snapvere.Shared

Male zajedničke komponente poput lokalizacije i process-local language statea.

## Tray-first startup

```text
Snapvere.exe
    ↓
startup dijagnostika + DI
    ↓
skriveni CaptureCenterWindow coordinator
    ↓
Win32 global-hotkey host
    ↓
Win32 notification-area icon host
    ↓
coordinator ostaje skriven dok aplikacija ostaje rezidentna
```

Native tray/hotkey threadovi ne mijenjaju WinUI kontrole izravno. Naredbe se marshalla na WinUI `DispatcherQueue`. Tray host pregovara `NOTIFYICON_VERSION_4`, obrađuje Explorer/taskbar rekreaciju i podržava pointer/keyboard activation.

## Capture pipelineovi

### Region / Screen

```text
naredba
    ↓
ResilientScreenCaptureService
    ├── Windows.Graphics.Capture + D3D11
    └── očekivani monitor-acquisition failure → GDI fallback
    ↓
CaptureFrame (fizički BGRA8)
    ↓
opcionalni crop + annotation render
    ↓
CaptureFileWriter / Clipboard
```

WGC/D3D resursi stvaraju se lazy samo za capture rad. Caller cancellation i neočekivane programerske greške ne skrivaju se iza nepovezanog fallback ponašanja.

### Window

```text
Window naredba / Ctrl+Shift+2
    ↓
snapshot eligible top-level prozora u Z-orderu
    ↓
freeze zaslona prije overlayja
    ↓
DPI-aware picker overlayi
    ↓
geometrijski hit-test prema frozen snapshotu
    ↓
WindowsGraphicsCaptureService.CreateForWindow
    ↓
lokalni PNG
```

Picker ne ovisi o `WindowFromPoint` nakon što SNAPVERE overlay postoji, čime se sprječava self-selection.

### Region editor

```text
tray / Print Screen / Ctrl+Shift+1
    ↓
frozen frame
    ↓
fizička pixel selekcija / move / osam resize ručki
    ↓
Pen / Line / Arrow / Box / Highlight
    ↓
render anotacija u odabrani frame
    ↓
Copy ili Save
```

Preview, selection i output proizlaze iz istog frozen framea.

## Koordinate i frame ugovor

Virtual-desktop koordinate mogu biti negativne. Display geometrija koristi fizičke bounds/effective DPI. WinUI logičke pointer pozicije prolaze eksplicitnu DPI konverziju prije capture geometrije.

`CaptureFrame` je validirani BGRA8 podatak s fizičkim dimenzijama, strideom, UTC timestampom i source identifikatorom. Prazne dimenzije, neispravan stride i premalen buffer odbijaju se prije downstream obrade.

## Lokalno stanje

Preference su u:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Zapis koristi privremenu datoteku + atomic move. Neispravno ili nečitljivo stanje vraća sigurne zadane vrijednosti. Lokalizacija je statična i in-process; engleski je canonical fallback. Windows startup registracija je per-user i njome upravlja `StartupRegistrationService`.

# Android arhitektura

Android source živi pod `android/` i namjerno je odvojen od WinUI/.NET granice.

Glavne komponente:

- `MainActivity` — responzivni nativni home UI, eksplicitne Capture/Open/Share/Delete i support/legal akcije;
- `CaptureService` — foreground `mediaProjection` servis koji posjeduje jednu odobrenu capture sesiju;
- `CaptureBufferLayout` — čista validirana RGBA row-layout aritmetika;
- Android resources — tamna tema, English/Croatian stringovi i lokalne vector/icon assete.

Android manifest namjerno zadržava `INTERNET` odsutan, cleartext isključen, backup isključen i `CaptureService` non-exported s `foregroundServiceType="mediaProjection"`.

## Android capture pipeline

```text
eksplicitni Capture tap
    ↓
Android MediaProjection consent
    ↓
start foreground CaptureServicea
    ↓
Activity se pomiče iza ciljnog sadržaja
    ↓
MainActivity.onStop() potvrđuje hidden stanje
    ↓
VirtualDisplay + RGBA_8888 ImageReader
    ↓
bounded first-frame wait
    ↓
validacija pixel stridea / row stridea / paddinga / buffer lengtha
    ↓
bitmap konverzija
    ↓
MediaStore PNG → Pictures/SNAPVERE
    ↓
Open / Share / Delete
```

Svaka snimka koristi novi consent token/projection instance. Pet-sekundni task-hide guard i sedam-sekundni first-frame guard ograničavaju ownership. Handler scheduling i image acquisition se provjeravaju. Resource teardown je idempotentan/per-resource, a capture ownership service-instance-aware kako stale teardown ne bi očistio drugu aktivnu sesiju.

RGBA put zahtijeva 4-byte pixel stride, dovoljan row stride, whole-pixel padding, overflow-safe aritmetiku i dovoljno ByteBuffer bajtova za `rowStride × height`. JVM testovi pokrivaju valjane i nevaljane layoute.

## Android UI/UX granica

Android dijeli SNAPVERE tamni identitet, ali koristi native Android layout ponašanje. Površina je vertikalno scrollable, system-inset aware, zadržava najmanje 52 dp touch targete i slaže paired actions vertikalno na uskim zaslonima ili kada je font scale >= 1,25x.

System/provider failure za Capture/Open/Share/Delete/Website/Support/Privacy/Terms prelazi u vidljiv recovery status gdje je moguće, umjesto izlaganja raw internal exception teksta.

# Browser-extension arhitektura

Browser source živi pod `ekstenzije/` u četiri self-contained varijante:

```text
ekstenzije/chrome
ekstenzije/edge
ekstenzije/opera
ekstenzije/firefox
```

Chrome, Edge i Opera koriste Manifest V3 service-worker background. Firefox koristi Firefox-kompatibilni MV3 `background.scripts`. Zajednički runtime source namjerno ostaje byte-identical među varijantama osim očekivane manifest/Gecko metadata razlike.

Glavni runtime dijelovi:

- `background.js` — capture orchestration, durable capture lock, visible capture, region/full-page koordinacija i download iniciranje;
- `capture.js` — page overlay/selection, full-page tile collection/stitching, lokalni crop/Blob output i restore page statea;
- `popup.*` — tri korisničke capture akcije i status UI;
- `options.*` — lokalni filename-prefix i save-location preference;
- `_locales/en` + `_locales/hr` — dedicated locale katalozi;
- lokalne 16/32/48/128 PNG ikone.

## Browser capture tokovi

### Visible area

```text
popup zahtjev
    ↓
background uzima bounded storage-backed capture lock
    ↓
active tab/window validacija
    ↓
browser captureVisibleTab API
    ↓
lokalni PNG download
    ↓
release capture locka
```

### Region

```text
popup zahtjev
    ↓
spremi token + tab/window identitet
    ↓
inject lokalni capture.js
    ↓
korisnik odabere regiju / Esc prekida
    ↓
overlay se ukloni prije screenshota
    ↓
viewport screenshot + lokalni crop
    ↓
PNG download + cleanup
```

### Full page

Content helper mjeri dokument, gradi bounded viewport tile plan, kontrolirano scrolla uz render settle, privremeno skriva ograničen broj fixed/sticky elemenata nakon prvog tilea, prima lokalne screenshot tileove, sastavlja ih u bounded canvas, lokalno preuzima Blob i vraća page state.

Eksplicitni limiti ograničavaju broj tileova, canvas dimenziju i ukupan broj piksela. Watchdog vraća page state ako orkestracija neočekivano nestane.

## Browser permission granica

Aktualni extension contract traži točno:

```text
activeTab
scripting
downloads
storage
```

Nema `<all_urls>` ni širokog `host_permissions` granta. Privilegirane/interne browser stranice mogu ostati nedostupne za capture i vraćaju se kao kontrolirane unsupported-page greške.

# Životni vijek resursa i local-first granica

Teški capture resursi na svim runtimeovima stvaraju se na zahtjev:

- Windows ne drži full-resolution/D3D capture resurse samo radi tray rezidentnosti;
- Android stvara projection/display/reader/thread resurse samo za eksplicitno odobrenu sesiju;
- browser full-page tile/session state postoji samo za bounded capture i čisti se kroz success/error/watchdog tokove.

Nijedna platforma u aktualnom product contractu ne dodaje first-party telemetry worker, automatski screenshot cloud-upload worker niti continuous background capture loop.

# QA arhitektura

## Windows

Tehnički probeovi uključuju `READY`, `TRAY_READY`, `REGION_OVERLAY_READY`, `WINDOW_OVERLAY_READY`, `SECONDARY_UI_READY` i normal-launch survival. CI renderira šest x64 UI površina i validira x86/x64/ARM64 payload construction plus Setup/Portable x64/x86 lifecycle.

ARM64 dokaz na hosted x64 CI-ju je cross-build/package dokaz, ne fizički ARM64 runtime test.

## Android

CI provjerava manifest privacy/version/service ugovor, `lintDebug`, `lintRelease`, JVM testove, debug/release build, APK signature/alignment i SHA-256. Javni 0.1.1 APK transparentno ostaje CI/debug-signed i ne predstavlja se kao Google Play production-signed.

## Browser ekstenzije

Browser CI provjerava manifest/permission/source policy, cross-browser parity, EN/HR locale, icon dimenzije, behavioral background smoke tokove, store-readiness metadata i reproducibilno dvostruko pakiranje uz SHA-256.

Behavioral VM smoke test izvršava stvarni `background.js` state machine, ali se ne predstavlja kao iscrpno browser GUI testiranje.

## Product contract

Product Contract CI cross-checka platform verzije, Android EN/HR resource keyeve, browser metadata, release asset names, current-documentation verziju i relativne Markdown linkove.

# Packaging i javni release

## Windows

`SNAPVERE-Setup.exe` i `SNAPVERE-Portable.exe` ugrađuju x86, x64 i ARM64 native payloade te automatski biraju kompatibilnu arhitekturu. Setup upravlja per-user install/update/uninstall lifecycleom. Portable koristi versioned cache s bounded/path-safe extractionom, integrity validacijom, mutex zaštitom i child-startup provjerom.

## Android

Javni v0.1.1 `SNAPVERE.apk` je validirani CI/debug-signed APK. Release također objavljuje `SNAPVERE-Android-Source.zip`, generiran iz validiranog tracked Android treea bez build/cache/signing secreta.

## Browser ekstenzije

v0.1.1 release objavljuje determinističke ZIP-ove za Chrome, Edge, Operu i Firefox. GitHub release objava odvojena je od vanjskog browser-store review/signing procesa i ne znači store approval.

## v0.1.1 javni asseti

Javni GitHub Release sadrži točno:

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

Za sve finalne assete izračunat je lokalni SHA-256 i nakon objave provjeren GitHub digest u release workflowu.

# Sigurnosna i privacy granica

Screenshot pixeli, clipboard sadržaj i korisničke datoteke nisu rutinski diagnostic payload. Capture je local-first. Windows package extraction je path-constrained i integrity-checked. Uninstall čuva `Pictures\SNAPVERE`. Android nema first-party Internet permission. Browser ekstenzije nemaju široki host permission niti first-party telemetry/cloud-upload runtime.

Vidi [Security Policy](../../SECURITY.md), [Privatnost](PRIVACY.md), [Security & Performance 0.1.1](SECURITY-PERFORMANCE-0.1.1.md), [Android](ANDROID.md), [Browser ekstenzije](BROWSER-EXTENSIONS.md) i [QA matricu](QA-MATRIX.md).

# Namjerno odgođeno

Windows:

- coordinated cross-monitor Region composition;
- text, blur/pixelate i numbered-step anotacije;
- scrolling capture;
- prošireni history/favorites/pin-to-screen;
- OCR;
- automatic updater;
- Authenticode signing.

Android:

- Windows-style arbitrary top-level Window Capture;
- Region-selection/annotation paritet s desktop editorom.

Browser distribucija:

- vanjska Chrome Web Store / Edge Add-ons / Opera Add-ons / Mozilla Add-ons objava i signing dok autentificirani publisher workflowi nisu dostupni.

Odgođene funkcije ne ulaze u production UI/marketing dok stvarno nisu implementirane i validirane.
