# Multi-monitor ponašanje

SNAPVERE koristi Windows virtual-desktop koordinate i podržava monitore koji se nalaze lijevo ili iznad primarnog monitora, uključujući negativne koordinate.

## DPI

Svaki monitor može imati vlastiti DPI. Interni capture rectangle ostaje u fizičkim pikselima, dok WinUI overlay koristi logičke jedinice samo za prikaz. Konverzije se rade po monitoru kako bi selection, highlight i finalni crop ostali usklađeni.

## Region Capture

Trenutni Region Capture radi nad frozen frameom jednog monitora. Koordinirani cross-monitor Region freeze nije prikazan kao gotova funkcija dok ne bude implementiran i release-testiran.

## Window Capture

Window picker može prikazati koordinirani highlight za prozor koji prelazi granice monitora. Window descriptor se i dalje odnosi na jedan stvarni HWND i finalni capture ide kroz WGC `CreateForWindow`.

## Screen Capture

Trenutni Screen Capture fokusira se na implementirani monitor workflow. UI za automatski monitor-under-cursor i all-monitors capture neće se prikazivati dok takvi putevi nisu potpuno implementirani.

## QA

DPI transformacije, selection geometrija i negativne koordinate pokrivene su unit/runtime provjerama gdje je to moguće na hosted runneru. Stvarni fizički multi-monitor hardware scenarij treba dodatno potvrditi na odgovarajućem Windows uređaju prije tvrdnje o potpunoj hardverskoj pokrivenosti.
