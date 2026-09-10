# SNAPVERE

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/branding/readme/snapvere-logo-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="assets/branding/readme/snapvere-logo-light.svg">
  <img alt="SNAPVERE — Capture. Edit. Done." src="assets/branding/readme/snapvere-logo-light.svg" width="560">
</picture>

**Capture. Edit. Done. — brza, lokalna i privatna aplikacija za snimanje zaslona na Windowsu koju razvija Brendigo.**

[English README](README.md) · [Službena stranica proizvoda](https://snapvere.com) · [Developer: Brendigo](https://brendigo.com)

SNAPVERE je tray-first Windows aplikacija za brzo snimanje područja, prozora i zaslona, jednostavne anotacije i lokalno spremanje PNG datoteka. Primarni i zadani jezik je engleski, a hrvatski i više od 20 dodatnih jezika mogu se odabrati u aplikaciji.

> Aktualna razvojna linija cilja **0.0.7**. Objavljeni tagovi i izdanja ostaju nepromjenjivi; `v0.0.6` se ne prepisuje.

## Dizajn i UI

Vizuali u repozitoriju predstavljaju referentni ugovor prema kojem se izrađuje stvarni WinUI program. Tray flyout i Region editor usklađuju se s tim prikazima umjesto sa starim Capture Center dizajnom.

### Tray-first Region Capture

Lijevi klik na SNAPVERE ikonu u trayu ili tipka **Print Screen** odmah pokreću snimanje područja.

![SNAPVERE tray-first Region Capture referenca](docs/images/tray-first-region.svg)

### Brendirani tray izbornik

Desni klik otvara akcije za snimanje, lokalne datoteke, postavke, jezik, informacije o programu i izlaz.

![SNAPVERE tray izbornik](docs/images/tray-menu.svg)

### Region editor

Editor radi izravno preko zamrznutog prikaza zaslona. Ima fizičku pixel selekciju, osam ručki za promjenu veličine, vertikalni alatni panel i odvojeni Copy / Save / Close panel.

![SNAPVERE Region editor](docs/images/region-editor.svg)

SVG datoteke su održavane UI reference, a ne lažno predstavljeni Windows screenshotovi. Release QA pokreće stvarne Tray, Region, Window, Options i About WinUI površine. Stvarni PNG screenshotovi u dokumentaciju se dodaju samo kada su reproducibilno snimljeni iz stvarne aplikacije.

## Kontrole

| Akcija | Primarni unos | Alternativa |
| --- | --- | --- |
| Snimanje područja | lijevi klik tray ili **Print Screen** | `Ctrl+Shift+1` |
| Snimanje prozora | desni klik tray → Capture window | `Ctrl+Shift+2` |
| Snimanje zaslona | desni klik tray → Capture screen | `Ctrl+Shift+4` |
| Postavke / nedavne snimke | desni klik tray | — |
| Jezik | kontrola jezika u trayu ili Options | — |
| About / Exit | desni klik tray | — |

## Region Capture

Implementirano je: zamrznuti frame, fizička pixel selekcija, pomicanje i resize, osam ručki, prikaz dimenzija, Pen, Line, Arrow, Box i Highlight alati, četiri boje, Undo, `Ctrl+Z`, `Ctrl+C`, Copy, Save, Enter/dvostruki klik za spremanje i Esc za odustajanje. Anotacije se renderiraju u konačni PNG.

UI slijedi graphite/violet SNAPVERE referencu s vertikalnim toolbarom uz selekciju i zasebnim action barom ispod nje.

## Window Capture

SNAPVERE otkriva vidljive top-level Windows prozore prije prikaza overlayja, koristi DWM extended-frame granice, filtrira vlastite/tool/cloaked/nevidljive prozore, prikazuje DPI-aware picker i završno snima prozor kroz Windows.Graphics.Capture `CreateForWindow`. Window Capture ne prelazi potajno na običan screen crop.

## Screen Capture

Windows.Graphics.Capture + Direct3D 11 je preferirani put gdje je podržan. Za očekivane probleme pri monitor captureu postoji resilient GDI fallback. Capture/D3D resursi se stvaraju tek kada su potrebni i ne ostaju aktivni samo zato što SNAPVERE miruje u trayu.

## Jezici

English (`en`) je canonical default i fallback. Ugrađeni katalog trenutačno nudi 28 jezika, uključujući Hrvatski (`hr`), Deutsch, Français, Español, Italiano, Português, Nederlands, Polski, Čeština, Slovenčina, Slovenščina, Magyar, Română, Български, Ελληνικά, Svenska, Dansk, Norsk, Suomi, Eesti, Latviešu, Lietuvių, Українська, Türkçe, 日本語, 한국어 i 简体中文.

Odabrani jezik sprema se lokalno u:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Nema translation API-ja, mrežnih poziva, polling servisa niti dodatnog background threada za prijevode.

## Postavke i zadane opcije

Implementirane postavke:

- **Start SNAPVERE with Windows**;
- **Include cursor on capture**;
- **Language**.

Setup prema zadanim postavkama označava:

- Start menu prečac: **uključeno**;
- Desktop ikona: **uključeno**;
- Pokretanje SNAPVERE-a s Windowsima: **uključeno**;
- Pokretanje nakon instalacije: **uključeno** na završnom ekranu.

Sve opcionalne postavke korisnik može isključiti prije instalacije. Startup je per-user i pokreće tray-first aplikaciju bez velikog dashboard prozora.

## Universal packaging

Od **v0.0.7 nadalje** javni release ugovor sadrži točno dvije datoteke:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
```

Nema zasebnih x86/x64 downloadova i nema Demo executablea. Svaki javni host je napravljen tako da se može pokrenuti na 32-bit Windowsu te u sebi sadrži native application payloade za:

- x86 / x32 / 32-bit Windows;
- x64 / AMD64 Windows;
- ARM64 Windows.

Host automatski odabire kompatibilni payload. Korisnik ne mora znati treba li mu x86 ili x64 izdanje.

To **ne znači** podršku za svaki povijesni Windows. Minimalni application target ostaje Windows 10 version 1809 / build 17763, dok WGC capture putevi zahtijevaju Windows 10 version 2004 / build 19041 ili noviji.

### Uninstall kroz isti Setup

SNAPVERE ne instalira zaseban `uninstall.exe`. Windows Installed apps koristi instaliranu kopiju:

```text
SNAPVERE-Setup.exe --uninstall
```

Isti Setup upravlja instalacijom, nadogradnjom i uklanjanjem. Prije rekurzivnog brisanja provjerava installation marker, a screenshotove u `Pictures\SNAPVERE` ostavlja netaknute.

## Performanse i privatnost

SNAPVERE je projektiran za mali idle overhead:

- tray i hotkey put rade event-driven umjesto periodičnog polling-a;
- capture/D3D resursi su lazy;
- prijevodi su statični i lokalni;
- settings JSON je malen i zapisuje se atomically;
- recent captures se čitaju na zahtjev;
- nema obaveznog računa, telemetrije, cloud uploada niti analitike screenshotova.

Ne obećava se fiksna RAM/CPU brojka jer Windows verzija, DPI, broj monitora, driveri i aktivna capture sesija mijenjaju radni set. Cilj optimizacije je da idle SNAPVERE ne obavlja periodični posao bez potrebe.

## Automatizirani QA

GitHub Actions builda/testira x64 i x86 te cross-builda ARM64. Universal package gate dodatno provjerava:

- točno dva javna EXE outputa;
- sva tri native payloada sadrže `Snapvere.exe`;
- x64 i x86 Setup/Portable lifecycle;
- eksplicitno prihvaćanje komercijalne licence za silent Setup;
- zadani Desktop shortcut i startup registraciju;
- uninstall istim Setupom;
- tray-first startup;
- Region editor;
- Window picker;
- Tray / Options / About površine;
- unit testove za architecture selection, sigurnu ZIP ekstrakciju i language/settings fallback.

ARM64 se cross-builda i strukturno provjerava na hosted x64 runneru; to se ne predstavlja kao stvarni ARM64 runtime test.

## Arhitektura

```text
Tray / Print Screen / hotkeys
    ↓
Skriveni WinUI runtime coordinator
    ↓
Snapvere.Application workflows
    ├── Region / Screen → resilient monitor capture
    └── Window → geometric picker → WGC CreateForWindow
    ↓
CaptureFrame (physical BGRA8 pixels)
    ↓
Crop / annotation render / PNG encode
    ↓
Pictures\SNAPVERE ili Windows Clipboard
```

## Dokumentacija

Engleska dokumentacija nalazi se u [`docs/`](docs/), a hrvatska u [`docs/hr/`](docs/hr/).

## Tehnologija

- C# / .NET 10
- WinUI 3 / Windows App SDK 1.8 stable line
- Windows.Graphics.Capture + Direct3D 11
- Win32 / DWM / GDI interoperability
- deterministic buildovi, nullable/analyzer provjere i central NuGet management
- xUnit + GitHub Actions

## Dijagnostika

Lokalni startup log:

```text
%LOCALAPPDATA%\SNAPVERE\Logs\startup.log
```

Pixel sadržaj screenshotova ne zapisuje se u startup log.

## Licenca i vlasništvo

**SNAPVERE 0.0.7 i noviji distribuiraju se pod SNAPVERE Commercial Software License Agreementom iz [`LICENSE`](LICENSE).** SNAPVERE je brend proizvoda. Brendigo je developer i publisher. Službena stranica proizvoda je **snapvere.com**, a developerova stranica **brendigo.com**.

Ranije objavljene verzije ostaju pod licencom s kojom su bile distribuirane; današnja promjena licence ne može retroaktivno ukinuti ranije dodijeljena prava.

---

**SNAPVERE — Capture. Edit. Done.**  
Razvio i objavljuje **Brendigo** · [snapvere.com](https://snapvere.com) · [brendigo.com](https://brendigo.com)
