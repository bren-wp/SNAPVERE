# Postavke

SNAPVERE prikazuje samo postavke koje imaju stvarnu implementaciju i lokalnu persistence logiku.

## Start SNAPVERE with Windows

Per-user Windows startup opcija kontrolira `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\SNAPVERE`. Normalni SNAPVERE launch je tray-first. Isključivanje opcije briše vrijednost samo kada odgovara aktualnom SNAPVERE executableu, kako se ne bi obrisala nepovezana konfiguracija.

Setup ovu opciju uključuje po defaultu, ali korisnik je može isključiti prije instalacije.

## Include cursor on capture

Ova postavka određuje traži li capture workflow uključivanje pokazivača kada aktivni backend podržava cursor composition. Preference se koristi u stvarnim Region, Window i Screen workflowima.

## Language

English je zadani i fallback jezik. Language picker nudi 28 ugrađenih jezika, uključujući Hrvatski i više od 20 dodatnih jezika. Izbor jezika ne koristi mrežu ni translation servis.

## settings.json

Postavke se spremaju lokalno u:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Datoteka je malena i zapisuje se preko privremene datoteke + atomic move obrasca kako prekid procesa ne bi ostavio djelomično zapisan JSON. Neispravan JSON ili nepodržani language code vraća sigurne zadane vrijednosti umjesto rušenja aplikacije.

## Privatnost i performanse

Nema background file watchera, polling timera ni cloud synca za postavke. Vrijednosti se učitavaju na zahtjev i cacheiraju u procesu, a zapisuju samo kada ih korisnik promijeni.
