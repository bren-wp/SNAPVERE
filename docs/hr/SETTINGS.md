# Postavke

Aktualna javna linija **SNAPVERE 0.1.1** prikazuje samo postavke koje imaju stvarnu implementaciju i lokalnu persistence logiku. Options / Preferences je sekundarna površina koja se otvara na zahtjev i ne vraća stari Capture Center kao normalni startup prozor.

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
