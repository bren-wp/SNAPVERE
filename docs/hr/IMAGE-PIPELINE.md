# Image pipeline

SNAPVERE obrađuje capture podatke lokalno u memoriji i sprema konačne rezultate kao PNG.

## CaptureFrame

Capture sloj proizvodi `CaptureFrame` s fizičkim BGRA8 pixel podacima, dimenzijama i stride informacijom. Ostali slojevi ne trebaju poznavati native WGC/GDI detalje.

## Crop

Region workflow iz frozen framea izdvaja fizički rectangle koji je korisnik odabrao. Bounds se normaliziraju i clampaju na izvorni frame kako ne bi nastao out-of-range read.

## Anotacije

Pen, Line, Arrow, Rectangle i Highlight čuvaju se kao lokalne geometrijske anotacije u koordinatama selekcije. Prije spremanja ili kopiranja renderiraju se u izlazni frame, pa nisu samo privremeni UI overlay.

## PNG

`PngCaptureEncoder` kodira završni frame u PNG. File writer koristi lokalno spremanje i izbjegava ostavljanje djelomično zapisanih outputa kada operacija ne uspije.

## Clipboard

Copy workflow kodira rezultat u memorijski PNG stream i predaje ga Windows Clipboardu. Nema mrežne faze.

## Memorija

Veliki frame bufferi nastaju samo tijekom aktivnog capturea. Kod ne uvodi globalni screenshot cache u tray procesu. Cilj je da se memorija vezana uz capture oslobodi nakon završetka workflowa.
