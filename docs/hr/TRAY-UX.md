# Tray UX

SNAPVERE je tray-first aplikacija. Normalni startup ne otvara veliki dashboard. Native tray ikona i globalni hotkey hostovi ostaju glavni ulaz u capture workflowe.

## Lijevi klik

Lijevi klik na tray ikonu odmah pokreće Region Capture. Nema međukoraka ni launcher prozora.

## Desni klik

Desni klik otvara kompaktni graphite/violet SNAPVERE flyout prema `docs/images/tray-menu.svg`. Izbornik sadrži Region, Window i Screen capture, Open capture folder, Options/recent captures, Language, About i Exit.

## Vizualni ugovor

Flyout koristi 418×540 referentnu geometriju, tamni graphite surface, violet primary accent, cyan secondary detalje i zajednički SNAPVERE brand mark. Ikone koriste Windows Segoe Fluent Icons gdje je primjereno, bez emoji zamjena.

## Tipkovnica i accessibility

Esc zatvara flyout. Interaktivne kontrole dobivaju automation names. Shortcut tekst se prikazuje uz capture akcije kada je relevantan.

## Fokus i zatvaranje

Flyout je always-on-top samo dok je otvoren i zatvara se pri deaktivaciji ili nakon odabrane naredbe. Ne ostaje skriven kao dodatni rezidentni prozor.

## Performanse

Tray host je native event-driven servis. Nema timer polling-a za detekciju klikova. Sam WinUI flyout nastaje tek nakon desnog klika i uništava se nakon zatvaranja.
