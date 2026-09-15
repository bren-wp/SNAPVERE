# SNAPVERE

**Snimi. Uredi. Gotovo.**

SNAPVERE 0.1.1 je brz, lokalno usmjeren alat za snimke zaslona za **Windows, Chrome, Edge, Operu i Firefox**. Osnovni capture workflow ne traži račun, first-party analitiku ni automatski prijenos snimki u oblak.

Aktualno izdanje: https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.1

## Preuzimanja

| Platforma | Paket |
| --- | --- |
| Windows Setup | `SNAPVERE-Setup.exe` |
| Windows Portable | `SNAPVERE-Portable.exe` |
| Chrome | `SNAPVERE-Chrome.zip` |
| Edge | `SNAPVERE-Edge.zip` |
| Opera | `SNAPVERE-Opera.zip` |
| Firefox | `SNAPVERE-Firefox.zip` |

Aktualni ugovor proizvoda sadrži samo ovih šest paketa. Browser ZIP paketi namijenjeni su ručnoj instalaciji; odobrenje u vanjskim trgovinama ne tvrdi se dok stvarni listing nije objavljen.

## Windows

Windows aplikacija radi prvenstveno iz područja obavijesti i podržava region, window i screen capture, frozen-frame odabir, Pen/Line/Arrow/Box/Highlight anotacije, Copy, lokalni PNG Save, lokalne postavke i x86/x64/ARM64 universal pakiranje.

Snimke se zadano spremaju u `Pictures\SNAPVERE`. PNG zapis koristi privremenu datoteku i završni move kako prekinuti encode ne bi izgledao kao gotova snimka.

## Browser ekstenzije

Chrome, Edge, Opera i Firefox nude snimanje vidljivog područja, odabranog područja i ograničeno snimanje cijele stranice. Dozvole su točno `activeTab`, `scripting`, `downloads` i `storage`, bez širokog host pristupa.

Naziv proizvoda, wordmark i prefiks spremljenih datoteka fiksno su **SNAPVERE** i ne mogu se mijenjati u postavkama. Kod full-page snimanja svaki dekodirani tile odmah se crta u jedan ograničeni canvas i zatim oslobađa radi manjeg vršnog korištenja RAM-a.

## Stabilnost i performanse

Capture stanje i memorijski limiti su ograničeni, Windows encode radi izvan WinUI threada, a CI provjerava x64/x86/ARM64 buildove, universal Setup/Portable lifecycle, package-size budget, browser runtime, dozvole, brand lock, cross-browser paritet i reproducibilno pakiranje.

Nijedan ozbiljan program ne može vjerodostojno obećati da platforma ili driver nikad neće pogriješiti. SNAPVERE zato koristi kontrolirani error handling, cleanup resursa i regresijske gateove.

## Privatnost

Osnovna obrada snimki je lokalna. SNAPVERE ne zahtijeva račun za capture i capture runtime nema first-party telemetriju snimki ni automatski cloud upload.

Dokumentacija: [Korisnički vodič](docs/hr/USER-GUIDE.md), [Privatnost](docs/hr/PRIVACY.md), [Rješavanje problema](docs/hr/TROUBLESHOOTING.md), [Status](docs/hr/PRODUCT-STATUS.md) i [QA matrica](docs/hr/QA-MATRIX.md).

Službena stranica: https://snapvere.com  
Podrška: info@snapvere.com  
Izdavač: https://brendigo.com

Povijesne činjenice o izdanjima ostaju u [RELEASES.md](RELEASES.md); aktualna dokumentacija opisuje održavani Windows/browser proizvod.
