# Arhitektura SNAPVERE-a

SNAPVERE 0.0.7 koristi tray-first arhitekturu. Vidljivi dashboard nije dio normalnog startup toka. Aplikacija drži mali WinUI runtime coordinator, native tray icon i globalne hotkeye, dok se capture prozori stvaraju samo kada su potrebni.

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

`Snapvere.Domain` sadrži osnovne modele i geometriju bez UI ovisnosti. `Snapvere.Shared` sadrži lagane zajedničke komponente poput lokalizacijskog kataloga. `Snapvere.Packaging` sadrži zajedničku logiku za universal Setup i Portable hostove kako se ekstrakcija, cache i architecture izbor ne bi duplicirali.

## Startup i idle ponašanje

Normalni launch je tray-first. Tray i globalni hotkey hostovi rade event-driven preko Windows poruka; nema periodičnog timer polling-a. WGC/D3D capture resursi nisu inicijalizirani samo zato što aplikacija miruje u trayu.

## Capture putevi

Region i Screen capture koriste `ResilientScreenCaptureService`: preferira Windows.Graphics.Capture, a za očekivane monitor-acquisition probleme može koristiti GDI fallback. Window Capture koristi stvarni `CreateForWindow` WGC put i ne zamjenjuje neuspjeh običnim cropom zaslona.

## Koordinate i DPI

Interni capture modeli koriste fizičke piksele. WinUI overlay koristi logičke DPI jedinice samo za prikaz, uz eksplicitnu konverziju. Time selection i finalni PNG ostaju vezani uz stvarni frozen frame.

## Lifecycle

Privremeni capture prozori i teški frame resursi nastaju na zahtjev i zatvaraju se nakon workflowa. Options, Language i About prozori također se stvaraju samo kada ih korisnik otvori.

## Packaging

`SNAPVERE-Setup.exe` i `SNAPVERE-Portable.exe` su jedina dva javna release asseta od v0.0.7. Svaki host ugrađuje x86, x64 i ARM64 application payload te odabire kompatibilni payload prema Windows arhitekturi.

## Sigurnosni principi

ZIP payload se ekstrahira uz zaštitu od path traversal napada. Setup prije rekurzivnog uklanjanja provjerava vlastiti installation marker. Startup i uninstall registracije su per-user. Postavke se zapisuju atomically u lokalni `settings.json`.
