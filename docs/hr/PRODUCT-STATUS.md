# SNAPVERE 0.1.32 Status proizvoda

Aktualne održavane površine proizvoda su **Windows** i **browser ekstenzije**.

## Windows

Produkcijska implementacija uključuje tray-first startup, Region/Window/Screen capture, lokalno snimanje primarnog zaslona kao video, frozen-frame odabir, lokalne anotacije, clipboard, PNG i MP4 persistence workflow, lokalne postavke, nedavne snimke, dijagnostiku te x86/x64/ARM64 aplikacijske payloade u universal Setup i Portable paketima te standardnu x64 MSI distribuciju.

SNAPVERE 0.1.32 je produkcijsko branding i usability izdanje. Zajednički `SnapvereBrand` sloj zaključava dostavljeni premium identitet na svim vidljivim Windows površinama: Obsidian `#070912`, Surface `#111526`, Slate `#161B2E`, Violet `#7655F6`, Lavender `#A48BFF`, Ice `#80E1E5`, viewfinder+munja znak, kompaktni spacing i ujednačenu hijerarhiju akcija. Tray naglašava četiri glavne capture/recording radnje, dok su sekundarni alati prebačeni u kompaktne footer akcije. Postavke koriste brandirani lijevi rail, Region/Window overlayi isti chrome, a recording/sekundarni prozori i Setup isti vizualni sustav.

Normalni startup ostaje tray-first i skriven; v0.1.32 ne uvodi blokirajući splash ekran ni stalni dashboard. Screen recording i dalje snima samo video primarnog zaslona, zato se mikrofon i sistemski audio ne prikazuju kao implementirane funkcije. Postojeći diagnostic redaction i local-first privacy mehanizmi ostaju aktivni.

## Browseri

Chrome, Edge, Opera i Firefox održavaju visible-area, selected-region i bounded full-page capture. Sva četiri browsera sada koriste isti dostavljeni 16/32/48/128 PNG icon set, premium popup hijerarhiju i premium Options raspored. Popup prikazuje samo tri stvarno podržana capture načina te izravan pristup Nedavnim snimkama i Postavkama. Cross-browser source paritet ostaje obavezan.

Validirani permission contract i dalje je `activeTab`, `scripting`, `downloads`, `downloads.open` i `storage`; široki host pristup nije dio održavanog dizajna. Postojeće privacy granice ostaju aktivne: session-scoped metadata capture vlasništva koristi se gdje je podržan, trajni local storage ostaje za postavke kada je moguće, Recent dohvat je ograničen na SNAPVERE kandidate, a click-time download revalidacija te sender/tab ownership provjere ostaju obavezne.

GitHub ZIP paketi ne predstavljaju se kao odobreni store listing dok stvarna vanjska objava nije potvrđena.

## Paketi

Aktualni 0.1.32 ugovor sadrži `SNAPVERE-Setup.exe`, `SNAPVERE-Setup.msi`, `SNAPVERE-Portable.exe`, `SNAPVERE-Chrome.zip`, `SNAPVERE-Edge.zip`, `SNAPVERE-Opera.zip` i `SNAPVERE-Firefox.zip`.

## Kvaliteta

CI provjerava Windows buildove/testove, renderirani WinUI visual QA, package-size budget, x64/x86 Setup/Portable lifecycle te MSI database/install/repair/upgrade/uninstall lifecycle QA. Browser CI provjerava runtime ponašanje, dozvole, locale, brand lock, cross-browser paritet i determinističko pakiranje. Windows UI contract zaključava premium palette/layout fragmente, a browser validator zajedničke premium CSS tokene i popup hijerarhiju. Product Contract CI i CodeQL izvode se zasebno.

To je regresijska evidencija, ali nije obećanje da svaka platforma, driver ili preglednik nikada ne može imati specifičan problem.

Release-prep cilj je **v0.1.32**. Verzija se ne smatra objavljenom dok nakon zelenog mergea ne završi GitHub release workflow.

Povezano: [Performanse i stabilnost](PERFORMANCE.md), [QA matrica](QA-MATRIX.md) i [Povijest izdanja](../../RELEASES.md).
