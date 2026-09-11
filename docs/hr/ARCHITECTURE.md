# Arhitektura SNAPVERE-a

## Status

Aktualna release linija je **0.1.0**. SNAPVERE se sastoji od tray-first Windows aplikacije za snimanje zaslona i zasebnog nativnog Android 10+ companiona. Objavljeni povijesni tagovi/releaseovi ostaju nepromjenjivi i kasniji razvoj ih ne prepisuje.

Windows normalni launch stvara skriveni WinUI capture coordinator, global-hotkey host i notification-area host te ostaje tray-first. Region, Window i Screen Capture su implementirani. Tray, Options/Recent Captures, Language i About sekundarne su površine koje nastaju na zahtjev.

Android implementira izričito korisnički odobreno full-screen snimanje kroz MediaProjection i MediaStore. Nije port Windows top-level-window capture enginea i ne navodi Windows-only funkcije koje Android platforma ne pruža.

## Ciljevi dizajna

SNAPVERE daje prioritet niskoj capture latenciji, fizičkoj pixel točnosti, mixed-DPI ispravnosti, determinističkom native cleanupu, local-first privatnosti, bounded failure ponašanju, predvidljivom tray/service startupu i maloj razumljivoj dependency površini.

## Windows slojevi

### Snapvere.App

WinUI 3 composition root i presentation layer.

- `App` upravlja dependency injectionom, UI-thread routingom, runtime/visual probeovima i životnim vijekom prozora.
- `CaptureCenterWindow` ostaje skriveni capture coordinator, a ne normalni launcher UI.
- `TrayMenuWindow`, `OptionsWindow`, `LanguagePickerWindow` i `AboutWindow` nastaju na zahtjev.
- `RegionCaptureWindow` upravlja interaktivnom Region selekcijom i anotacijama.
- `WindowTargetPicker` koordinira DPI-aware target overlaye.

Programatski WinUI treeovi ostaju preferirani za sekundarne prozore jer ih package/runtime probeovi pouzdano materijaliziraju bez dodatnih XAML resource-load ovisnosti.

### Snapvere.Application

UI-neovisni Windows workflowi i lokalna persistencija:

- `RegionCaptureWorkflow`
- `WindowCaptureWorkflow`
- `ScreenCaptureWorkflow`
- `CaptureFileWriter`
- `CaptureHistoryService`
- `CapturePreferencesService`

Preference i capture history ostaju lokalni. 0.1.0 dodatno zadržava filesystem policy/security failure unutar load/enumeration/temp-cleanup putova tako da sekundarna lokalna I/O greška ne ruši UI tok.

### Snapvere.Domain

Modeli i geometrija capturea u fizičkim pikselima bez UI ovisnosti.

### Snapvere.Capture

Windows acquisition i desktop integration:

- monitor/window discovery;
- DPI i virtual-desktop geometrija;
- global hotkey host;
- Windows.Graphics.Capture / D3D11;
- GDI monitor compatibility backend;
- native window Z-order filtering i targeting.

### Snapvere.Imaging

Deterministički BGRA8 crop, annotation rendering i PNG encoding.

### Snapvere.Packaging / Snapvere.Setup / Snapvere.Portable

Guarded embedded-payload obrada, architecture selection, per-user Setup lifecycle, same-Setup uninstall i Portable extraction/launch. Portable reusable cache provjerava se prema trusted architecture-specific SHA-256 manifestu ugrađenom u host prije izvršavanja.

### Snapvere.Shared

Male zajedničke komponente poput lokalizacije i process-local language statea.

## Windows tray-first startup

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
coordinator ostaje skriven dok proces ostaje aktivan
```

Native tray/hotkey threadovi ne mijenjaju WinUI kontrole izravno. Naredbe se prebacuju na WinUI `DispatcherQueue`. Tray host koristi `NOTIFYICON_VERSION_4`, obrađuje Explorer/taskbar rekreaciju te pointer i keyboard activation.

## Windows capture pipelineovi

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

WGC/D3D resursi stvaraju se lazy za aktivni capture. Caller cancellation i neočekivane programerske greške ne skrivaju se iza nepovezanog fallback rada.

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

Picker ne koristi `WindowFromPoint` nakon stvaranja SNAPVERE overlayja, čime se sprječava self-selection.

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

Preview, selection i output koriste isti frozen frame.

## Windows koordinate i frame ugovor

Virtual-desktop koordinate mogu biti negativne. Display geometrija koristi fizičke bounds/effective DPI. WinUI logičke pointer pozicije prolaze eksplicitnu DPI konverziju prije capture geometrije.

`CaptureFrame` je validirani BGRA8 podatak s fizičkim dimenzijama, strideom, UTC timestampom i source identifikatorom. Prazne dimenzije, neispravan stride i premalen buffer odbijaju se prije downstream obrade.

## Windows lokalno stanje

Preference su u:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Zapis koristi privremenu datoteku i atomic move. Neispravno/nečitljivo stanje vraća sigurne zadane vrijednosti. Lokalizacija je statična i in-process; English je canonical fallback. Windows startup registracija je per-user i njome upravlja `StartupRegistrationService`.

## Android arhitektura

Android source živi pod `android/` i namjerno je odvojen od WinUI/.NET aplikacijskog sloja.

Glavne komponente:

- `MainActivity` — responzivni native home UI, Capture/Otvori/Podijeli/Izbriši i support/legal akcije;
- `CaptureService` — foreground `mediaProjection` servis koji posjeduje jednu odobrenu capture sesiju;
- `CaptureBufferLayout` — čista validirana RGBA row-layout aritmetika;
- Android resources — tamna tema, engleski/hrvatski stringovi i native ikone.

Android manifest zadržava `INTERNET` absent, cleartext disabled, backup disabled i `CaptureService` non-exported s `foregroundServiceType="mediaProjection"`.

## Android capture pipeline

```text
izričiti Capture tap
    ↓
Android MediaProjection dopuštenje
    ↓
start foreground CaptureServicea
    ↓
Activity se premješta iza ciljanog zaslona
    ↓
MainActivity.onStop() potvrđuje skriveno stanje
    ↓
VirtualDisplay + RGBA_8888 ImageReader
    ↓
bounded first-frame wait
    ↓
pixel stride / row stride / padding / buffer-length validacija
    ↓
bitmap konverzija
    ↓
MediaStore PNG → Pictures/SNAPVERE
    ↓
Otvori / Podijeli / Izbriši
```

Svaka snimka koristi novi consent token/projection instance. Petsekundni task-hide guard i sedam-sekundni first-frame guard ograničavaju ownership. Handler scheduling i image acquisition se provjeravaju. Teardown je idempotentan/per-resource, a capture ownership service-instance-aware kako stale teardown ne bi očistio drugu aktivnu sesiju.

RGBA put zahtijeva 4-byte pixel stride, dovoljan row stride, whole-pixel padding, overflow-safe aritmetiku i dovoljno ByteBuffer bajtova za `rowStride × height`. JVM unit testovi pokrivaju valjane i nevaljane layoute.

## Android UI/UX granica

Android dijeli SNAPVERE tamni identitet, ali koristi native Android layout ponašanje. Površina je vertikalno scrollable, system-inset aware, zadržava najmanje 52 dp touch targete i slaže paired actions vertikalno na uskim zaslonima ili kada je font scale >= 1,25x.

System/provider problemi za Capture/Otvori/Podijeli/Izbriši/Web/Podrška/Privatnost/Uvjeti prelaze u vidljiv recovery status gdje je moguće, umjesto da raw internal exception izađe iz Activityja.

## Životni vijek resursa

Teški capture resursi na obje platforme stvaraju se na zahtjev. Windows ne drži full-resolution frame/D3D capture resurse samo radi tray rezidentnosti. Android stvara MediaProjection, VirtualDisplay, ImageReader i capture thread samo za izričito odobrenu sesiju i gasi ih nakon uspjeha ili failurea.

Nijedna platforma ne dodaje telemetry worker, cloud-upload worker niti continuous capture loop.

## Runtime i visual QA

Windows tehnički probeovi uključuju `READY`, `TRAY_READY`, `REGION_OVERLAY_READY`, `WINDOW_OVERLAY_READY`, `SECONDARY_UI_READY` i normal-launch survival. Visual QA snima šest stvarno renderiranih x64 površina:

```text
region-capture.png
window-capture.png
tray-menu.png
options.png
language.png
about.png
```

CI odbija prazan/neočekivano mali output i zapisuje dimenzije, veličine i SHA-256.

Android CI provjerava manifest privacy/version/service ugovor, `lintDebug`, `lintRelease`, JVM testove, debug/release build, debug APK signature/alignment i SHA-256. v0.1.0 publication put dodatno ponovno provjerava CI/debug APK potpis i alignment, validira Android source arhivu i prenosi SHA-256 vrijednosti do finalne objave. Ne tvrdi se produkcijski/Google Play signing identitet.

## Packaging i javni release

### Windows

`SNAPVERE-Setup.exe` i `SNAPVERE-Portable.exe` ugrađuju x86, x64 i ARM64 payloade te automatski biraju kompatibilnu arhitekturu. Setup upravlja per-user install/update/uninstall lifecycleom. Portable koristi versioned cache s bounded/path-safe extractionom, integrity validacijom, mutexom i child-startup provjerom.

Hosted x64 CI izvršava universal x64/x86 package lifecycle. ARM64 je cross-build/package validacija, ne fizički ARM64 runtime dokaz.

### Android

Javni v0.1.0 `SNAPVERE.apk` je validirani CI/debug-potpisani APK. Nastaje iz istog Android sourcea koji prolazi i release-variant lint/build provjeru, a objava provjerava njegov potpis, ZIP alignment i SHA-256 identitet. v0.1.0 ne zahtijeva niti ugrađuje privatni produkcijski Android keystore i ne predstavlja se kao produkcijski/Play-potpisan.

Budući produkcijski potpisani Android kanal mora koristiti namjerno upravljani stabilni signing identitet. Ako se taj identitet razlikuje od v0.1.0 CI/debug identiteta, Android može zahtijevati uninstall/reinstall umjesto in-place nadogradnje; kompatibilnost potpisa ne smije se pretpostaviti.

`SNAPVERE-Android-Source.zip` generira se izravno iz točno validiranog `android/` Git treea i ne sadrži generirani build/cache output ni signing secret.

### v0.1.0 javni asseti

Finalno GitHub izdanje mora sadržavati točno:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE.apk
SNAPVERE-Android-Source.zip
```

Za sva četiri asseta računa se lokalni SHA-256 i nakon objave provjerava GitHub digest prije prihvaćanja izdanja.

## Sigurnosna i privacy granica

Screenshot pixeli, clipboard sadržaj i korisničke datoteke nisu rutinski diagnostic payload. Capture je local-first. Package extraction je path-constrained/size-bounded. Uninstall čuva `Pictures\SNAPVERE`. Android nema first-party network capture path.

Vidi `SECURITY.md`, `docs/hr/SECURITY-PERFORMANCE-0.1.0.md` i `docs/hr/ANDROID.md`.

## Namjerno odgođeno

Windows:

- coordinated cross-monitor Region composition;
- text, blur/pixelate i numbered-step anotacije;
- scrolling capture;
- prošireni History/favorites/pin-to-screen;
- OCR;
- automatic updater;
- Authenticode signing.

Android:

- Windows-style arbitrary top-level Window Capture;
- Region-selection/annotation paritet s desktop editorom.

Odgođene funkcije ne ulaze u production UI/dokumentaciju dok stvarno nisu implementirane i release-gated.
