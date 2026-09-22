# SNAPVERE korisnički vodič

SNAPVERE je lokalno usmjeren alat za snimanje sadržaja na Windowsu i u modernim preglednicima. Aktualno javno izdanje je v0.1.12.

## Windows

Aplikacija radi prvenstveno iz područja obavijesti.

| Radnja | Prečac |
| --- | --- |
| Snimanje područja | Print Screen ili Ctrl+Shift+1 |
| Snimanje prozora | Ctrl+Shift+2 |
| Snimanje zaslona | Ctrl+Shift+3 |
| Snimanje zaslona kao video | Start/Stop u tray izborniku |

### Snimanje područja

Snimanje područja zamrzava odabrani zaslon prije prikaza editora, pa se sadržaj ispod odabira ne može pomicati. Povuci za stvaranje područja, zatim ga pomakni ili promijeni veličinu unutar zamrznutog framea. Dostupni su olovka, linija, strelica, okvir, marker, odabir boje i Undo.

**Kopiraj** renderira odabrano područje u Windows međuspremnik, a **Spremi** zapisuje PNG lokalno. `Enter` sprema, a `Esc` odustaje. Geometrija je ograničena na zamrznuti frame i prazni/nevaljani odabiri odbijaju se prije enkodiranja.

### Snimanje zaslona kao video

Snimanje zaslona snima primarni Windows zaslon lokalno u H.264 MP4 preko Windows.Graphics.Capture. Pokretanje i zaustavljanje obavlja se iz tray izbornika, koristi postojeću postavku uključivanja pokazivača miša i prikazuje aktivno stanje snimanja. Početna implementacija je samo video: mikrofon i sistemski zvuk još nisu navedeni kao podržani.

Snimke zaslona i dovršene videosnimke zadano se spremaju u `Pictures\SNAPVERE`. Dovršeni MP4 objavljuje se tek nakon završetka enkodiranja; privremene datoteke ne predstavljaju dovršene snimke.

### Windows postavke

Postavke prikazuju samo stvarno implementirane lokalne opcije: pokretanje SNAPVERE-a s Windowsima, uključivanje pokazivača u podržane načine snimanja i odabir jezika sučelja. Preference se spremaju samo za trenutačni Windows račun u `%LOCALAPPDATA%\SNAPVERE`. Nedavne snimke čitaju se iz lokalne mape bez baze podataka ili cloud povijesti.

## Browser ekstenzije

Chrome, Edge, Opera i Firefox nude snimanje vidljivog područja, odabranog područja i ograničeno snimanje cijele stranice. Browser postavke mogu odrediti treba li preglednik pitati gdje spremiti podržane visible/region snimke. Naziv proizvoda, wordmark i prefiks datoteke fiksno su **SNAPVERE** i nisu korisnički promjenjivi.

Browser region workflow koristi token vezan uz pojedinu snimku. Nakon zatvaranja popupa page overlay dovršava zahtjev i prikazuje lokaliziranu privremenu grešku ako background crop/download ne uspije. Nijedna postavka ne uključuje telemetriju, automatski cloud upload ili udaljeni runtime kod.

Snimanje videa trenutačno je Windows funkcija u validaciji. Browser ekstenzije ne navode podršku za videosnimanje.

## Privatnost

Osnovna obrada snimki i videosnimki odvija se lokalno bez obaveznog računa, automatskog slanja u oblak, analitike ili telemetrije.

Povezano: [Privatnost](PRIVACY.md), [QA matrica](QA-MATRIX.md) i [Rješavanje problema](TROUBLESHOOTING.md).
