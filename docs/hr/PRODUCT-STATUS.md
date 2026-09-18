# SNAPVERE 0.1.4 Status proizvoda

Aktualne održavane površine proizvoda su **Windows** i **browser ekstenzije**.

## Windows

Produkcijska implementacija uključuje tray-first startup, Region/Window/Screen capture, frozen-frame odabir, lokalne anotacije, clipboard i PNG save workflow, lokalne postavke, nedavne snimke, dijagnostiku te x86/x64/ARM64 aplikacijske payloade u universal Setup i Portable paketima.

Najnoviji capture-path hardening uklanja redundantne full-frame staging alokacije iz Region i Window prikaza te oslobađa raw frozen monitor buffere nakon što je odgovarajući UI bitmap spreman. Post-v0.1.4 `main` dodatno objavljuje PNG datoteke uz collision-safe dodjelu naziva na commit granici, kontrolira očekivane Settings/Recent shell i lokalne security greške, vraća jezični odabir na stvarno spremljenu vrijednost nakon neuspjelog zapisa te drži tray fallback unutar Windows virtualnog desktopa.

## Browseri

Chrome, Edge, Opera i Firefox održavaju visible-area, selected-region i bounded full-page capture. SNAPVERE brand je zaključan. Validirani permission contract je `activeTab`, `scripting`, `downloads`, `downloads.open` i `storage`; `downloads.open` koristi se samo za izričitu Recent > Otvori radnju. Široki host pristup nije dio održavanog dizajna. Post-v0.1.4 `main` dodatno učvršćuje capture-lock ownership, async redoslijed Recent rezultata, zaštitu od dvostrukih akcija, kompatibilnost browser API poziva i responsive/reduced-motion ponašanje.

## Paketi

Aktualni 0.1.4 ugovor sadrži `SNAPVERE-Setup.exe`, `SNAPVERE-Portable.exe`, `SNAPVERE-Chrome.zip`, `SNAPVERE-Edge.zip`, `SNAPVERE-Opera.zip` i `SNAPVERE-Firefox.zip`.

## Kvaliteta

CI provjerava Windows buildove/testove, renderirani WinUI visual QA, package-size budget i x64/x86 lifecycle. Browser CI provjerava runtime ponašanje, dozvole, locale, brand lock, cross-browser paritet i determinističko pakiranje. Product Contract CI i CodeQL izvode se zasebno.

To je jaka regresijska evidencija, ali nije obećanje da svaka platforma, driver ili preglednik nikada ne može imati specifičan problem.

Javno izdanje ostaje **v0.1.4**. Neobjavljeni hardening na `main` grani predstavlja izvorni kod dok se buduća verzija izričito ne zapakira i objavi.

Povezano: [Performanse i stabilnost](PERFORMANCE.md), [QA matrica](QA-MATRIX.md) i https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.4
