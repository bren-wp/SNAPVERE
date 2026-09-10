# Region Capture

Region Capture je primarni SNAPVERE workflow. Pokreće se lijevim klikom na tray ikonu, tipkom Print Screen ili `Ctrl+Shift+1` fallback prečacem.

## Tok

1. SNAPVERE dohvaća frozen frame monitora.
2. Preko framea se otvara borderless DPI-aware overlay.
3. Korisnik povlačenjem definira fizički pixel rectangle.
4. Selekcija se može pomicati i mijenjati preko osam ručki.
5. Po potrebi se dodaju anotacije.
6. Copy šalje finalni rezultat u Windows Clipboard, a Save sprema PNG.

## UI ugovor

Region editor prati `docs/images/region-editor.svg`: violet selection border, osam resize ručki, badge dimenzija, vertikalni tool rail desno ili s druge strane ako nema mjesta te zaseban Copy / Save / Close action bar ispod selekcije. Palete se automatski repositioniraju da ostanu unutar work area i da ne prekrivaju ključni selection badge.

## Alati

Implementirani su Move, Pen, Line, Arrow, Box, Highlight, četiri boje i Undo. `Ctrl+Z` vraća zadnju anotaciju, `Ctrl+C` kopira odabir, Enter sprema, Esc prvo zatvara aktivni alat ili odustaje od capturea.

## Točnost

Selekcija se vodi u fizičkim pikselima. WinUI položaji se pretvaraju u fizičke koordinate prema DPI-ju monitora. Finalni crop i anotacije renderiraju se iz istog frozen framea kako se sadržaj ispod kursora ne bi promijenio između odabira i spremanja.

## Performanse

Frozen frame i annotation visuals postoje samo tijekom aktivnog Region Capture workflowa. Po zatvaranju prozora reference se oslobađaju i tray proces se vraća u idle event-driven stanje.
