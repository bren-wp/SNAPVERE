# Window Capture

Window Capture snima stvarni top-level Windows prozor, a ne proizvoljni crop zaslona.

## Discovery

Prije prikaza picker overlayja SNAPVERE enumerira vidljive top-level prozore i bilježi njihov Z-order. Filtriraju se vlastiti SNAPVERE prozori, tool windows, nevidljivi i cloaked prozori. Za granice se koriste DWM extended-frame bounds kada su dostupne.

## Picker

Za svaki monitor nastaje frozen DPI-aware overlay. Hit testing je geometrijski nad unaprijed poznatim window descriptorima, tako da picker overlay ne ulazi u vlastiti target popis. Prozori koji prelaze više monitora mogu dobiti koordinirani highlight.

## Finalni capture

Odabrani HWND se predaje `WindowsGraphicsCaptureService` kroz `CreateForWindow`. Ako taj WGC put nije dostupan ili nije podržan, workflow prijavljuje problem; ne prelazi potajno na screen crop.

## Kontrole

Pomicanje pokazivača označava kandidat. Lijevi klik potvrđuje snimanje, a Esc odustaje. DPI i negativne virtual-desktop koordinate obrađuju se eksplicitno.

## Privatnost

Popis prozora, naslovi i capture pixel podaci koriste se lokalno za izvršavanje workflowa. Trenutni proizvod nema obavezni cloud upload niti telemetry pipeline za screenshot sadržaj.
