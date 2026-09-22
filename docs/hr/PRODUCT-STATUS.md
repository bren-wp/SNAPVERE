# SNAPVERE 0.1.16 Status proizvoda

Aktualne održavane površine proizvoda su **Windows** i **browser ekstenzije**.

## Windows

Produkcijska implementacija uključuje tray-first startup, Region/Window/Screen capture, lokalno snimanje primarnog zaslona kao video, frozen-frame odabir, lokalne anotacije, clipboard, PNG i MP4 persistence workflow, lokalne postavke, nedavne snimke, dijagnostiku te x86/x64/ARM64 aplikacijske payloade u universal Setup i Portable paketima.

SNAPVERE 0.1.16 zadržava local-first snimanje primarnog Windows zaslona preko Windows.Graphics.Capture uz H.264 MP4 enkodiranje i čini stanje snimanja jasnim: Postavke imaju odvojene kompaktne Start i Stop kontrole, a aktivno snimanje mali borderless kontroler s proteklim vremenom i jednom namjenskom Stop akcijom. Setup odvaja prihvat licence od instalacijskih opcija i nakon uspješnog install/uninstall postupka izlazi iz busy lifecyclea prije gumba Završi. Responsive high-DPI ponašanje ostaje zadržano kroz Tray, Settings, Language, About, capture feedback i Setup. Početni način snimanja je samo video; sistemski zvuk i mikrofon nisu navedeni kao podržani.

## Browseri

Chrome, Edge, Opera i Firefox održavaju visible-area, selected-region i bounded full-page capture. SNAPVERE brand je zaključan. Validirani permission contract je `activeTab`, `scripting`, `downloads`, `downloads.open` i `storage`; `downloads.open` koristi se samo za izričitu Recent > Otvori radnju, nakon što click-time revalidacija potvrdi da odabrani download i dalje postoji, dovršen je i još odgovara SNAPVERE capture ugovoru. Široki host pristup nije dio održavanog dizajna. SNAPVERE 0.1.16 dodatno provjerava sender i active-tab ownership prije privilegiranih capture/download radnji. SNAPVERE 0.1.16 dodatno učvršćuje capture-lock ownership, async redoslijed Recent rezultata, zaštitu od dvostrukih akcija, kompatibilnost browser API poziva i responsive/reduced-motion ponašanje te premješta macOS Full Page prečac sa sistemski rezervirane kombinacije Command+Shift+3.

## Paketi

Aktualni 0.1.16 ugovor sadrži `SNAPVERE-Setup.exe`, `SNAPVERE-Portable.exe`, `SNAPVERE-Chrome.zip`, `SNAPVERE-Edge.zip`, `SNAPVERE-Opera.zip` i `SNAPVERE-Firefox.zip`.

## Kvaliteta

CI provjerava Windows buildove/testove, renderirani WinUI visual QA, package-size budget i x64/x86 lifecycle. Browser CI provjerava runtime ponašanje, dozvole, locale, brand lock, cross-browser paritet i determinističko pakiranje. Product Contract CI i CodeQL izvode se zasebno.

To je jaka regresijska evidencija, ali nije obećanje da svaka platforma, driver ili preglednik nikada ne može imati specifičan problem.

Javno izdanje je **v0.1.16**. Kasniji neobjavljeni hardening na `main` grani predstavlja izvorni kod dok se buduća verzija izričito ne zapakira i objavi.

Povezano: [Performanse i stabilnost](PERFORMANCE.md), [QA matrica](QA-MATRIX.md) i https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.16
