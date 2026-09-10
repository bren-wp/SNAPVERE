# Postavke

Aktualni post-v0.0.8 `main` prikazuje samo postavke koje imaju stvarnu implementaciju i lokalnu persistence logiku. Options / Preferences je sekundarna površina koja se otvara na zahtjev i ne vraća stari Capture Center kao normalni startup prozor.

## Start SNAPVERE with Windows

Per-user Windows startup opcija kontrolira `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\SNAPVERE`. Normalni SNAPVERE launch je tray-first. Isključivanje opcije briše vrijednost samo kada odgovara aktualnom SNAPVERE executableu, kako se ne bi obrisala nepovezana konfiguracija.

Setup ovu opciju uključuje po defaultu, ali korisnik je može isključiti prije instalacije.

## Include cursor on capture

Ova postavka određuje traži li capture workflow uključivanje pokazivača kada aktivni backend podržava cursor composition. Preference se koristi u stvarnim Region, Window i Screen workflowima.

## Language

English (`en`) je zadani i canonical fallback jezik. Language picker nudi 28 ugrađenih jezika, uključujući Hrvatski (`hr`) i više od 20 dodatnih jezika. Izbor jezika ne koristi mrežu ni translation servis.

Aktualne Region/Window capture površine te Tray/Options/Language/About sekundarni UI dohvaćaju korisnički tekst kroz zajednički `SnapvereLocalization` katalog. Hrvatski ima zasebne prijevode za aktualne lokalizirane površine. Ako drugi odabrani jezik nema određeni prijevod, prikazuje se canonical English tekst umjesto nepoznatog resource ključa.

## settings.json

Postavke se spremaju lokalno u:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Datoteka je malena i zapisuje se preko privremene datoteke + atomic move obrasca kako prekid procesa ne bi ostavio djelomično zapisan JSON. Neispravan JSON ili nepodržani language code vraća sigurne zadane vrijednosti umjesto rušenja aplikacije.

## Recent Captures

Options površina prikazuje ograničeni popis stvarnih datoteka u lokalnoj capture mapi. Implementirane su stvarne akcije za osvježavanje popisa, otvaranje capture mape i otvaranje pojedinačne snimke kroz Windows shell association. Ne postoji lažna remote history baza.

## Privatnost i performanse

Nema background file watchera, polling timera ni cloud synca za postavke. Vrijednosti se učitavaju na zahtjev i cacheiraju u procesu, a zapisuju samo kada ih korisnik promijeni.

Lokalizacija također ostaje statična i event-driven: nema mrežnog translation API-ja, telemetry dependencyja ni background language workera.

## QA

Unit testovi pokrivaju settings persistence, atomic temp-file cleanup, corrupt JSON fallback, normalization podržanih jezika, fallback nepodržanih jezika, zasebne hrvatske secondary-UI prijevode i formatirane localization placeholdere.

Runtime QA kroz `SECONDARY_UI_READY` materijalizira Tray, Options, Language i About na instaliranim i Portable x64/x86 paketnim putovima. Dodatni x64 visual-QA gate snima stvarno renderirane Options, Language i About PNG-ove zajedno s Region, Window i Tray površinama, odbija vizualno prazne/neočekivano male frameove te sprema manifest s dimenzijama, veličinama datoteka i SHA-256 sažecima.
