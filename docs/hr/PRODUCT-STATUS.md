# SNAPVERE 0.1.29 Status proizvoda

Aktualne održavane površine proizvoda su **Windows** i **browser ekstenzije**.

## Windows

Produkcijska implementacija uključuje tray-first startup, Region/Window/Screen capture, lokalno snimanje primarnog zaslona kao video, frozen-frame odabir, lokalne anotacije, clipboard, PNG i MP4 persistence workflow, lokalne postavke, nedavne snimke, dijagnostiku te x86/x64/ARM64 aplikacijske payloade u universal Setup i Portable paketima te standardnu x64 MSI distribuciju.

SNAPVERE 0.1.29 dodaje responsive chrome hardening Region i Window capture overlayima. Region guidance/status površine sada se ograničavaju prema stvarnoj širini overlaya, wrapaju lokalizirani tekst i smanjuju vertikalni margin na nižim work-area površinama. Window Capture jednako ograničava karticu s uputom i labelu odabranog prozora, pa dugi naslovi i lokalizirani tekst više ne izlaze iz uskih/high-DPI monitora. Postojeći recording finalization integrity, responsive sekundarni prozori, Setup lifecycle i local-first capture semantika ostaju nepromijenjeni. Početni način snimanja je samo video; sistemski zvuk i mikrofon nisu navedeni kao podržani.

## Browseri

Chrome, Edge, Opera i Firefox održavaju visible-area, selected-region i bounded full-page capture. SNAPVERE brand je zaključan. Validirani permission contract je `activeTab`, `scripting`, `downloads`, `downloads.open` i `storage`; `downloads.open` koristi se samo za izričitu Recent > Otvori radnju, nakon što click-time revalidacija potvrdi da odabrani download i dalje postoji, dovršen je i još odgovara SNAPVERE capture ugovoru. Široki host pristup nije dio održavanog dizajna. Browser runtime i dalje provjerava sender i active-tab ownership prije privilegiranih capture/download radnji te zadržava capture-lock hardening, async redoslijed Recent rezultata, zaštitu od dvostrukih akcija, kompatibilnost browser API poziva i responsive/reduced-motion ponašanje. macOS Full Page prečac ostaje izvan sistemski rezervirane kombinacije Command+Shift+3.

Region capture sada prenosi dimenzije viewporta iz trenutka odabira kroz background-to-crop pipeline i prekida rad ako se viewport promijeni prije lokalnog PNG cropa, umjesto spremanja geometrijski pogrešnog izreza. Postavke također zaključavaju Save As zajedno s gumbom Spremi dok traje zapis u browser-local storage, čime prikazano stanje ostaje usklađeno sa spremljenim.

## Paketi

Aktualni 0.1.29 ugovor sadrži `SNAPVERE-Setup.exe`, `SNAPVERE-Setup.msi`, `SNAPVERE-Portable.exe`, `SNAPVERE-Chrome.zip`, `SNAPVERE-Edge.zip`, `SNAPVERE-Opera.zip` i `SNAPVERE-Firefox.zip`.

## Kvaliteta

CI provjerava Windows buildove/testove, renderirani WinUI visual QA, package-size budget, x64/x86 Setup/Portable lifecycle te MSI database/install/repair/upgrade/uninstall lifecycle QA. Browser CI provjerava runtime ponašanje, dozvole, locale, brand lock, cross-browser paritet i determinističko pakiranje. Product Contract CI i CodeQL izvode se zasebno.

To je jaka regresijska evidencija, ali nije obećanje da svaka platforma, driver ili preglednik nikada ne može imati specifičan problem.

Javno izdanje je **v0.1.29**. Kasniji neobjavljeni hardening na `main` grani predstavlja izvorni kod dok se buduća verzija izričito ne zapakira i objavi.

Povezano: [Performanse i stabilnost](PERFORMANCE.md), [QA matrica](QA-MATRIX.md) i https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.29
