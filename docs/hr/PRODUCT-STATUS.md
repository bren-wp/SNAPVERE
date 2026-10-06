# SNAPVERE 0.1.30 Status proizvoda

Aktualne održavane površine proizvoda su **Windows** i **browser ekstenzije**.

## Windows

Produkcijska implementacija uključuje tray-first startup, Region/Window/Screen capture, lokalno snimanje primarnog zaslona kao video, frozen-frame odabir, lokalne anotacije, clipboard, PNG i MP4 persistence workflow, lokalne postavke, nedavne snimke, dijagnostiku te x86/x64/ARM64 aplikacijske payloade u universal Setup i Portable paketima te standardnu x64 MSI distribuciju.

SNAPVERE 0.1.30 fokusira se na privacy/security hardening. Windows i Portable startup dijagnostika sada prije lokalnog zapisa prolazi kroz ograničeni redactor za lokalne/UNC putanje, URL/file URI vrijednosti, e-mail adrese i česte credential/token obrasce, uz zadržavanje tipa exceptiona i HRESULT evidencije. Postojeći capture, recording, responsive overlay i Setup lifecycle ostaju nepromijenjeni. Početni način snimanja je samo video; sistemski zvuk i mikrofon nisu navedeni kao podržani.

## Browseri

Chrome, Edge, Opera i Firefox održavaju visible-area, selected-region i bounded full-page capture. SNAPVERE brand je zaključan. Validirani permission contract je `activeTab`, `scripting`, `downloads`, `downloads.open` i `storage`; `downloads.open` koristi se samo za izričitu Recent > Otvori radnju, nakon što click-time revalidacija potvrdi da odabrani download i dalje postoji, dovršen je i još odgovara SNAPVERE capture ugovoru. Široki host pristup nije dio održavanog dizajna. Browser runtime i dalje provjerava sender i active-tab ownership prije privilegiranih capture/download radnji te zadržava capture-lock hardening, async redoslijed Recent rezultata, zaštitu od dvostrukih akcija, kompatibilnost browser API poziva i responsive/reduced-motion ponašanje. macOS Full Page prečac ostaje izvan sistemski rezervirane kombinacije Command+Shift+3.

Region capture i dalje prenosi dimenzije viewporta iz trenutka odabira kroz background-to-crop pipeline i prekida rad ako se geometrija promijeni. U 0.1.30 metadata vlasništva aktivne capture sesije prelazi u session-scoped extension storage kada je dostupan, dok trajni local storage ostaje za postavke; Nedavne snimke dodatno dohvaćaju samo ograničeni SNAPVERE kandidatni skup prije lokalnog filtriranja i otvaranja.

## Paketi

Aktualni 0.1.30 ugovor sadrži `SNAPVERE-Setup.exe`, `SNAPVERE-Setup.msi`, `SNAPVERE-Portable.exe`, `SNAPVERE-Chrome.zip`, `SNAPVERE-Edge.zip`, `SNAPVERE-Opera.zip` i `SNAPVERE-Firefox.zip`.

## Kvaliteta

CI provjerava Windows buildove/testove, renderirani WinUI visual QA, package-size budget, x64/x86 Setup/Portable lifecycle te MSI database/install/repair/upgrade/uninstall lifecycle QA. Browser CI provjerava runtime ponašanje, dozvole, locale, brand lock, cross-browser paritet i determinističko pakiranje. Product Contract CI i CodeQL izvode se zasebno.

To je jaka regresijska evidencija, ali nije obećanje da svaka platforma, driver ili preglednik nikada ne može imati specifičan problem.

Javno izdanje je **v0.1.30**. Kasniji neobjavljeni hardening na `main` grani predstavlja izvorni kod dok se buduća verzija izričito ne zapakira i objavi.

Povezano: [Performanse i stabilnost](PERFORMANCE.md), [QA matrica](QA-MATRIX.md) i https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.30
