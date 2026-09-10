# Tray UX

SNAPVERE je tray-first aplikacija. Normalni startup ne otvara veliki dashboard. Native tray ikona i globalni hotkey hostovi ostaju glavni ulaz u capture workflowe, dok je skriveni WinUI runtime/capture coordinator samo implementation detail.

## Lijevi klik

Lijevi klik na tray ikonu odmah pokreće Region Capture. Nema međukoraka ni launcher prozora. Double-click handling je debounced kako ne bi proizveo duplu Region Capture akciju povrh single-click ugovora.

## Desni klik

Desni klik otvara kompaktni graphite/violet SNAPVERE flyout prema `docs/images/tray-menu.svg`. Izbornik sadrži Region, Window i Screen capture, Open capture folder, Options/recent captures, Language, About i Exit.

Native tray message thread ne izrađuje WinUI kontrole izravno. Podigne `TrayCommand`, a aplikacija naredbu prenosi na WinUI `DispatcherQueue` prije otvaranja flyouta ili capture površine.

## Vizualni ugovor

Flyout koristi 418×540 referentnu geometriju, tamni graphite surface, violet primary accent, cyan secondary detalje i zajednički SNAPVERE brand mark. Ikone koriste Windows Segoe Fluent Icons gdje je primjereno, bez emoji zamjena.

## Tipkovnica i accessibility

Esc zatvara flyout. Interaktivne kontrole dobivaju automation names. Shortcut tekst se prikazuje uz capture akcije kada je relevantan.

Glavni capture prečaci su:

- Print Screen ili `Ctrl+Shift+1` — Region Capture;
- `Ctrl+Shift+2` — Window Capture;
- `Ctrl+Shift+4` — Screen Capture.

## Fokus i zatvaranje

Flyout je always-on-top samo dok je otvoren i zatvara se pri deaktivaciji ili nakon odabrane naredbe. Ne ostaje skriven kao dodatni rezidentni prozor.

Options, Language i About također se stvaraju samo na zahtjev i oslobađaju nakon zatvaranja.

## Runtime i visual QA

`TRAY_READY` probe potvrđuje da su hotkey i tray hostovi inicijalizirani dok Capture Center ostaje skriven.

`SECONDARY_UI_READY` redom materijalizira:

```text
Tray
  → Options
  → Language
  → About
  → SECONDARY_UI_READY
```

Installed i Portable x64/x86 lifecycle zahtijevaju da taj probe prođe.

Uz marker probe, `eng/Capture-SnapvereVisualQa.ps1` u x64 CI-u snima stvarno renderirani `tray-menu.png` zajedno s Region, Window, Options, Language i About površinama. Vizualno prazan ili neočekivano malen PNG ruši CI. Manifest bilježi naslov, dimenzije, veličinu datoteke i SHA-256, a rezultat se sprema kao kratkotrajni GitHub Actions artefakt.

## Performanse

Tray host je native event-driven servis. Nema timer polling-a za detekciju klikova. Sam WinUI flyout nastaje tek nakon desnog klika i uništava se nakon zatvaranja. WGC/D3D capture resursi ne inicijaliziraju se samo zbog tray residencyja, a lokalizacija ne uvodi mrežni worker ni polling servis.
