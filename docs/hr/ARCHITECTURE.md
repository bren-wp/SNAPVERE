# Arhitektura SNAPVERE-a

Aktualni `main` je post-**v0.0.8** hardening. Najnovije objavljeno izdanje je v0.0.8; kasnije promjene na `main` nisu novo izdanje dok zasebni release workflow ne objavi novi tag i assete. Objavljeni releaseovi ne prepisuju se naknadnim razvojem.

SNAPVERE koristi tray-first arhitekturu. Vidljivi dashboard nije dio normalnog startup toka. Aplikacija drži mali skriveni WinUI runtime/capture coordinator, native tray icon i globalne hotkeye, dok se capture i sekundarni prozori stvaraju samo kada su potrebni.

## Slojevi

```text
Tray / Print Screen / hotkeys
    ↓
Snapvere.App
    ↓
Snapvere.Application workflows
    ↓
Snapvere.Capture acquisition
    ↓
Snapvere.Imaging obrada
    ↓
CaptureFileWriter / Clipboard
```

`Snapvere.Domain` sadrži osnovne modele i geometriju bez UI ovisnosti. `Snapvere.Shared` sadrži lagane zajedničke komponente poput built-in lokalizacijskog kataloga i process-local language statea. `Snapvere.Packaging` sadrži zajedničku logiku za universal Setup i Portable hostove kako se ekstrakcija, cache i architecture izbor ne bi duplicirali.

## Startup i idle ponašanje

Normalni launch je tray-first. Tray i globalni hotkey hostovi rade event-driven preko Windows poruka; nema periodičnog timer polling-a. WGC/D3D capture resursi nisu inicijalizirani samo zato što aplikacija miruje u trayu.

Native tray thread ne manipulira WinUI kontrolama izravno. Naredba se prenosi na WinUI `DispatcherQueue`, koji zatim otvara capture ili sekundarnu površinu.

## Capture putevi

Region i Screen capture koriste `ResilientScreenCaptureService`: preferira Windows.Graphics.Capture, a za očekivane monitor-acquisition probleme može koristiti GDI fallback. Window Capture koristi stvarni `CreateForWindow` WGC put i ne zamjenjuje neuspjeh običnim cropom zaslona.

Region Capture radi nad jednim frozen frameom primarnog zaslona, koristi fizičku pixel geometriju i renderira implementirane anotacije u konačni rezultat. Window picker prije prikaza overlayja snima geometrijski/Z-order snapshot kandidata kako SNAPVERE overlay ne bi postao cilj.

## Koordinate i DPI

Interni capture modeli koriste fizičke piksele. WinUI overlay koristi logičke DPI jedinice samo za prikaz, uz eksplicitnu konverziju kroz DPI transformer. Time selection i finalni PNG ostaju vezani uz stvarni frozen frame.

Window picker može koristiti odvojeni overlay po monitoru. Coordinated cross-monitor Region Capture i dalje je namjerno odgođen dok ne bude implementiran i release-gated.

## Postavke i lokalizacija

`CapturePreferencesService` sprema implementirane preference lokalno u:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Trenutačno se spremaju cursor preference i language code. Zapis koristi privremenu datoteku i atomic replacement/move. Neispravan JSON ili nepodržani jezik vraća sigurne zadane vrijednosti.

`SnapvereLocalization` koristi statični ugrađeni katalog. English je canonical fallback, a Hrvatski ima zasebne stringove za aktualne capture i sekundarne UI površine. Nema mrežnog translation servisa, file watchera ni background pollinga.

## Lifecycle

Privremeni capture prozori i teški frame resursi nastaju na zahtjev i zatvaraju se nakon workflowa. Tray flyout, Options, Language i About također se stvaraju samo kada ih korisnik otvori.

## Packaging

Od v0.0.7 `SNAPVERE-Setup.exe` i `SNAPVERE-Portable.exe` jedina su dva javna release asseta. Svaki universal host građen je kao x86-compatible executable, ugrađuje x86, x64 i ARM64 application payload te automatski odabire kompatibilni native payload prema Windows arhitekturi.

Setup je per-user i isti `SNAPVERE-Setup.exe` upravlja install/update/remove lifecycleom. Portable koristi versioned lokalni cache, bounded/path-safe extraction, preparation mutex i stabilni originalni Portable launcher path za opcionalni Windows startup.

Hosted x64 CI stvarno izvršava universal Setup/Portable lifecycle za x64 i x86 payload selection. ARM64 se cross-builda i package-validira, ali to nije predstavljeno kao stvarni ARM64 hardware runtime test.

## Runtime i visual QA

Marker probeovi odvajaju tehničku konstrukciju od normalnog startup ugovora:

1. `READY` — tehnički aktivira skriveni coordinator;
2. `TRAY_READY` — potvrđuje tray/hotkey startup bez vidljivog Capture Centera;
3. `REGION_OVERLAY_READY` — potvrđuje Region editor;
4. `WINDOW_OVERLAY_READY` — potvrđuje Window picker;
5. `SECONDARY_UI_READY` — redom materijalizira Tray, Options, Language i About;
6. normal-launch survival — potvrđuje da installed/Portable proces ostaje živ u tray-first načinu.

Dodatni x64 visual-QA harness pokreće stvarne probeove i snima šest renderiranih PNG-ova: Region, Window, Tray, Options, Language i About. Prazan ili neočekivano malen frame ruši CI. `manifest.json` zapisuje naslov, dimenzije, veličinu i SHA-256 svakog PNG-a, a GitHub Actions sprema cijeli set kao kratkotrajni artefakt.

## Sigurnosni principi

ZIP payload se ekstrahira uz zaštitu od path traversal napada i bounded extraction pravila. Setup prije rekurzivnog uklanjanja provjerava vlastiti installation marker. Startup i uninstall registracije su per-user. Postavke se zapisuju atomically. Screenshot pixeli nisu rutinski startup-log payload, nema telemetry/cloud-upload dependencyja, a uninstall ne uklanja `Pictures\SNAPVERE` korisničke snimke.
