# Tray UX

SNAPVERE je tray-first aplikacija. Normalni startup ne otvara veliki dashboard. Native tray ikona i globalni hotkey hostovi ostaju glavni ulaz u capture workflowe, dok je skriveni WinUI runtime/capture coordinator implementation detail.

## Notification-area protokol

v0.0.9 koristi moderni `NOTIFYICON_VERSION_4` callback ugovor. Nakon svakog uspješnog `NIM_ADD`, uključujući ponovno dodavanje ikone nakon Explorer/taskbar recreationa, SNAPVERE poziva `NIM_SETVERSION`. Ako inicijalno postavljanje version-4 protokola ne uspije, upravo dodana ikona se uklanja i tray startup pada umjesto da aplikacija tiho nastavi s nejasnim callback ugovorom.

`NIF_SHOWTIP` ostaje uključen kako bi standardni SNAPVERE tooltip ostao vidljiv pod version-4 protokolom. Event se dekodira iz low worda `lParam`, a eksplicitno se obrađuju pointer i keyboard activation događaji.

## Lijevi klik i keyboard activation

Lijevi klik na tray ikonu odmah pokreće Region Capture. Isto vrijedi za version-4 `NIN_SELECT` i `NIN_KEYSELECT` aktivaciju.

```text
WM_LBUTTONUP / NIN_SELECT / NIN_KEYSELECT
  → TrayCommand.RegionCapture
  → App DispatcherQueue
  → Region capture workflow
```

Nema međukoraka ni launcher prozora. Double-click i duplicate shell activation eventovi prolaze kroz debounce kako ne bi proizveli dvije Region Capture akcije.

## Desni klik i keyboard context menu

Desni klik ili version-4 `WM_CONTEXTMENU` otvara kompaktni graphite/violet SNAPVERE flyout. Native tray message thread ne izrađuje WinUI kontrole izravno. Podigne `TrayCommand.ShowMenu`, a aplikacija naredbu prenosi na WinUI `DispatcherQueue` prije stvaranja user-facing površine.

Aktualni flyout prati `docs/images/tray-menu.svg` referencu: 418×540, tamni graphite surface, violet primary accent, cyan secondary detalje i zajednički SNAPVERE brand mark.

Akcije:

- Capture region — Print Screen / `Ctrl+Shift+1`;
- Capture window — `Ctrl+Shift+2`;
- Capture screen — `Ctrl+Shift+3`;
- Open capture folder;
- Options & recent captures;
- Language;
- About SNAPVERE;
- Exit.

Language je stvarna funkcija. Flyout čita spremljeni language code pri stvaranju i otvara on-demand `LanguagePickerWindow` kroz application coordinator.

Flyout se zatvara pri gubitku aktivacije, nakon odabrane akcije ili na Esc. Ne ostaje kao dodatni rezidentni prozor.

## Vlasništvo naredbi

`Win32TrayIconService` posjeduje samo native integration odgovornosti:

- add/update/remove/version negotiation notification ikone;
- dekodiranje pointer i keyboard tray callbackova;
- podizanje command eventa;
- Explorer/taskbar recreation recovery;
- lifetime native ikone i window handlea.

WinUI kontrole ne smiju se stvarati ili mijenjati na native tray message threadu. `App` je vlasnik user-facing command dispatcha na WinUI threadu. Subscriber iznimka ne smije srušiti native tray message loop.

## Glavni prečaci

| Akcija | Primarni unos | Alternativa |
| --- | --- | --- |
| Region Capture | tray lijevi klik ili Print Screen | `Ctrl+Shift+1` |
| Window Capture | tray desni klik → Capture window | `Ctrl+Shift+2` |
| Screen Capture | tray desni klik → Capture screen | `Ctrl+Shift+3` |
| Quick actions | tray desni klik / keyboard context menu | — |
| Language | tray language akcija ili Options | — |

Print Screen može biti nedostupan kada ga Windows ili druga aplikacija već posjeduje; neovisni Region fallback ostaje dostupan.

## Sekundarni prozori

### Options / Preferences

Otvara stvarni `OptionsWindow` s implementiranim lokalnim preferencama i Recent Captures bez vraćanja Capture Centera kao vidljivog launchera.

### Language

Otvara jedan on-demand language picker. English je canonical default/fallback, a hrvatski i više od 20 dodatnih jezika dostupni su iz ugrađenog kataloga. Izbor se zapisuje lokalno bez resident timera, watchera, network workera ili translation servisa.

### Recent captures

Koristi lokalni filesystem-backed history i stvarne open/refresh/folder akcije.

### About

Prikazuje komercijalni SNAPVERE About surface s Brendigo developer/publisher identitetom, službenim `snapvere.com` / `brendigo.com` destinacijama, support e-mailom te Privacy/Terms akcijama.

## Runtime i visual QA

`SNAPVERE_TRAY_STARTUP_PROBE=1` ili `--tray-startup-probe` provjerava tray-first inicijalizaciju i emitira:

```text
SNAPVERE 0.0.X TRAY_READY
```

Marker se doseže tek kada su global hotkey i tray host inicijalizirani dok runtime coordinator ostaje skriven.

`SNAPVERE_SECONDARY_UI_PROBE=1` ili `--secondary-ui-probe` stvarno materijalizira:

```text
Tray
  → Options
  → Language
  → About
  → SECONDARY_UI_READY
```

Installed i Portable x64/x86 lifecycle zahtijevaju da taj probe prođe.

`eng/Capture-SnapvereVisualQa.ps1` u x64 CI-u dodatno snima stvarno renderirane Region, Window, Tray, Options, Language i About površine. Prazan ili neočekivano malen frame ruši CI. Manifest bilježi provenance, dimenzije, veličinu i SHA-256, PR se uspoređuje s posljednjim čistim successful-main baselineom, a rezultat se sprema kao Actions artifact. Visual thresholdi se ne spuštaju radi skrivanja regresije.

## Stabilnost i performanse

- WGC/D3D se ne inicijalizira samo radi tray residencyja;
- WinUI kontrole se ne diraju s native tray threada;
- nema periodičnog pollinga za tray, lokalizaciju ili recent-capture stanje;
- ne prikazuju se dekorativne kontrole bez stvarne akcije;
- double-click ne smije interferirati sa single-click Region Capture ugovorom;
- `NOTIFYICON_VERSION_4` se pregovara nakon svakog add/re-add ciklusa;
- version-4 callback event čita se iz dokumentiranog low-word polja;
- native handleovi imaju determinističko vlasništvo i Explorer recovery;
- manual launch i Windows startup koriste isti tray-first executable ugovor;
- Tray/Options/Language/About prozori stvaraju se samo na zahtjev i oslobađaju pri zatvaranju.

## Accessibility i vizualni smjer

Flyout koristi Windows-native Segoe/Fluent ikone bez emoji zamjena, automation names na interaktivnim kontrolama, Esc zatvaranje i keyboard notification-area activation/context-menu događaje. Shape, border i state promjene nadopunjuju boju. Čitljivost i high-contrast ponašanje važniji su od dekorativnog glowa.

Brand detalji: `docs/hr/BRANDING.md`.
