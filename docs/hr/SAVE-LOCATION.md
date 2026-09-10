# Odabir mjesta spremanja snimke

SNAPVERE omogućuje korisniku da za svaku dovršenu snimku **područja**, **prozora** ili **zaslona** odabere konačnu lokaciju spremanja.

## Tijek korištenja

1. SNAPVERE snimi odabrane piksele i dovrši PNG kodiranje kroz postojeći capture pipeline.
2. Potpuno dovršena PNG datoteka najprije se sigurno zapisuje u zadanu lokalnu mapu (`Slike\SNAPVERE`, odnosno Windows Pictures mapa) postojećim temp-file + atomic-move mehanizmom.
3. Nakon toga SNAPVERE otvara izvorni Windows dijalog **Spremi kao / Save As**, s već predloženim generiranim nazivom snimke i `.png` formatom.
4. Korisnik može zadržati predloženu lokaciju i naziv ili odabrati drugu mapu i naziv datoteke.
5. Ako se odabere druga putanja, SNAPVERE premješta već dovršenu PNG datoteku na odabranu lokaciju.

Ovaj redoslijed je namjeran: interaktivni dijalog za spremanje nikada ne smije ugroziti jedinu kopiju upravo snimljene slike.

## Ako korisnik odustane

Zatvaranje ili otkazivanje dijaloga **Spremi kao** ne briše snimku. Već dovršena PNG datoteka ostaje u zadanoj SNAPVERE mapi za snimke.

## Zamjena postojeće datoteke

Windows dijalog traži potvrdu prije zamjene postojeće datoteke. Nakon korisničke potvrde SNAPVERE kopira dovršenu snimku u kratku privremenu datoteku u odredišnoj mapi, a zatim atomarno zamjenjuje odabrano odredište.

Privremeni naziv ne sadrži puni korisnički naziv slike, nego koristi oblik `.snapvere-<guid>.tmp`. Tako i vrlo dugačak, ali valjan PNG naziv ne uzrokuje prekoračenje ograničenja duljine jedne datotečne komponente.

## Ponašanje kod pogreške

Ako premještanje ne uspije prije nego što je odredišna datoteka potpuno dovršena, izvorna snimka ostaje sačuvana u zadanoj mapi. Privremene datoteke brišu se po principu best effort.

Ako je nova odredišna datoteka već potpuno zapisana, ali Windows ne dopusti brisanje izvornika, SNAPVERE namjerno ostavlja izvornu datoteku kao sigurnosnu kopiju umjesto da riskira gubitak snimke.

## Format datoteke

Trenutni javni capture pipeline zapisuje PNG, pa Save As prihvaća samo `.png` odredišta. SNAPVERE ne radi skriveno pretvaranje formata samo zato što je korisnik upisao drugu ekstenziju.

## Kopiranje u međuspremnik

Ako se snimanje područja dovrši naredbom **Kopiraj**, Save As dijalog se ne otvara. Odabir lokacije koristi se za capture tokove koji završavaju spremanjem datoteke.

## Privatnost i potrošnja resursa

Funkcija radi potpuno lokalno. Ne uvodi mrežni pristup, telemetriju, upload servis, polling, timer, file watcher niti novi stalni background worker. Windows picker nastaje samo nakon dovršene snimke i postoji samo dok korisnik bira lokaciju.

## Arhitektura

- `CaptureCenterWindow` koordinira završetak spremanja za područje, prozor i zaslon te poziva picker.
- `CaptureSaveLocationService` je tanki Windows App SDK sloj za native Save As dijalog.
- `CaptureRelocationService` sadrži UI-neovisni i testabilni ugovor sigurnog premještanja datoteke.
- Postojeći capture workflowi i `CaptureFileWriter` i dalje upravljaju snimanjem, PNG kodiranjem, zadanom lokacijom i atomarnim stvaranjem prve dovršene PNG datoteke.

Odvajanjem premještanja od samog snimanja promjene u Save As UX-u ne utječu na geometriju snimanja, anotacije, pokazivač miša, PNG encoder ili tray-first runtime ugovor.
