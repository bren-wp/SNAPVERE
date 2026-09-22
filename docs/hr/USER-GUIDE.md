# SNAPVERE korisnički vodič

SNAPVERE je lokalno usmjeren alat za snimanje sadržaja na Windowsu i u modernim preglednicima. Aktualno javno izdanje je v0.1.13.

## Windows

Aplikacija radi prvenstveno iz područja obavijesti.

| Radnja | Prečac |
| --- | --- |
| Snimanje područja | Print Screen ili Ctrl+Shift+1 |
| Snimanje prozora | Ctrl+Shift+2 |
| Snimanje zaslona | Ctrl+Shift+3 |
| Snimanje zaslona kao video | Start/Stop u tray izborniku |

Odabrano područje može se označiti olovkom, linijom, strelicom, okvirom ili markerom te kopirati ili spremiti kao PNG.

Snimanje zaslona snima primarni Windows zaslon lokalno u H.264 MP4 preko Windows.Graphics.Capture. Pokretanje i zaustavljanje obavlja se iz tray izbornika, koristi postojeću postavku uključivanja pokazivača miša i prikazuje aktivno stanje snimanja. Početna implementacija je samo video: mikrofon i sistemski zvuk još nisu navedeni kao podržani.

Snimke zaslona i dovršene videosnimke zadano se spremaju u `Pictures\SNAPVERE`. Dovršeni MP4 objavljuje se tek nakon završetka enkodiranja; privremene datoteke ne predstavljaju dovršene snimke.

## Browser ekstenzije

Chrome, Edge, Opera i Firefox nude snimanje vidljivog područja, odabranog područja i ograničeno snimanje cijele stranice. Naziv proizvoda i prefiks datoteke fiksno su **SNAPVERE**.

Snimanje videa trenutačno je Windows funkcija u validaciji. Browser ekstenzije ne navode podršku za videosnimanje.

## Privatnost

Osnovna obrada snimki i videosnimki odvija se lokalno bez obaveznog računa, automatskog slanja u oblak, analitike ili telemetrije.

Povezano: [Privatnost](PRIVACY.md), [QA matrica](QA-MATRIX.md) i [Rješavanje problema](TROUBLESHOOTING.md).
