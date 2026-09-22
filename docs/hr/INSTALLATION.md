# Instalacija SNAPVERE-a

Aktualno javno izdanje: **v0.1.14**. Objavljeni tagovi i asseti su immutable; kasniji hardening na `main` grani nije retroaktivni dio tog izdanja.

## Windows Setup

Preuzmi `SNAPVERE-Setup.exe` iz aktualnog izdanja i pokreni ga normalno. Universal paket sadrži validirane x86, x64 i ARM64 aplikacijske payloade i automatski bira kompatibilnu arhitekturu.

Interaktivni installer:

- prikazuje ugrađenu komercijalnu licencu kao 1. korak i traži prihvat prije nastavka,
- prikazuje lokaciju instalacije, prečace i Windows startup opciju odvojeno kao 2. korak,
- zadano instalira po korisniku u `%LOCALAPPDATA%\Programs\SNAPVERE`,
- omogućuje odabir druge mape bez slučajnog `SNAPVERE\SNAPVERE` dupliranja,
- izdvaja payload u staging prije objave,
- odbija instalaciju izravno u root diska, Windows sistemsku mapu i kroz reparse-point/symlink putanju,
- odbija zamijeniti neprazni custom target ako nije validirana postojeća SNAPVERE instalacija,
- čuva prethodnu validiranu instalaciju ako upgrade ne može završiti,
- registrira uninstall podatke za Windows Installed Apps,
- može izraditi Start menu/Desktop prečace i uključiti pokretanje s Windowsima za trenutnog korisnika,
- ne dopušta zatvaranje čarobnjaka dok aktivna file operacija još završava,
- korisniku prikazuje sanitizirane install/uninstall greške bez raw exception detalja,
- zadržava Installed Apps podatke, startup registraciju i SNAPVERE prečace ako brisanje datoteka ne uspije, tako da se blokirani uninstall može popraviti ili ponoviti bez neregistrirane instalacije,
- zahtijeva pozitivan parent PID za interni deferred-cleanup workflow, čeka taj proces kada još postoji i serijalizira cleanup s normalnim Setup operacijama kako odgođeni uninstall ne bi ušao u race s novim install/repair postupkom.

Tihi per-user install podržava:

```text
SNAPVERE-Setup.exe --silent --accept-license
```

Tihi uninstall podržava:

```text
SNAPVERE-Setup.exe --uninstall --silent
```

Tihi install bez `--accept-license` završava bez instalacije.

## Windows Portable

`SNAPVERE-Portable.exe` je portable opcija. Prije ponovne uporabe provjerava ugrađeni payload i ne zahtijeva klasičnu instalaciju.

## Browser ekstenzije

Aktualno izdanje sadrži:

- `SNAPVERE-Chrome.zip`
- `SNAPVERE-Edge.zip`
- `SNAPVERE-Opera.zip`
- `SNAPVERE-Firefox.zip`

ZIP paketi služe za ručnu instalaciju. Objavu u službenim trgovinama ne treba smatrati završenom dok stvarni vanjski listing nije objavljen.

Aktualno izdanje: https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.14
