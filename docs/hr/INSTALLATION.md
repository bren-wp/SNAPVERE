# Instalacija i universal packaging

Od SNAPVERE 0.0.7 nadalje javni release sadrži samo dvije datoteke:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
```

Korisnik ne bira zaseban x86, x64 ili ARM64 download. Svaki host ugrađuje native application payloade za x86, x64 i ARM64 te automatski odabire kompatibilan payload za aktualni Windows.

## Podržane arhitekture

- x86 / x32 / 32-bit Windows
- x64 / AMD64 Windows
- ARM64 Windows

Universal host je namjerno građen kao x86-compatible executable kako bi se mogao pokrenuti i na 32-bit Windowsu. Na x64 i ARM64 sustavima zatim pokreće odgovarajući native child payload.

## Verzija Windowsa

Architecture support nije isto što i podrška za svaki stari Windows. Minimalni application target je Windows 10 version 1809 / build 17763. WGC ovisni capture putevi zahtijevaju Windows 10 version 2004 / build 19041 ili noviji.

## Setup zadane opcije

Interactive Setup prema zadanim postavkama uključuje:

- Start menu shortcut — uključeno
- Desktop icon — uključeno
- Start SNAPVERE with Windows — uključeno

Korisnik svaku od tih opcija može isključiti prije instalacije. Silent install koristi iste zadane vrijednosti nakon eksplicitnog prihvaćanja licence.

## Komercijalna licenca

Setup prikazuje SNAPVERE Commercial Software License Agreement. Silent instalacija bez `--accept-license` mora završiti s nenultim rezultatom i ne smije zaobići prihvaćanje licence.

## Instalacijska lokacija

Zadani per-user put je:

```text
%LOCALAPPDATA%\Programs\SNAPVERE
```

Setup ne zahtijeva zaseban admin-only Program Files install kako bi osnovni per-user workflow radio.

## Startup

Kada je opcija uključena, Setup zapisuje per-user Windows Run vrijednost koja pokazuje na stabilni instalirani `Snapvere.exe`. Normalni launch aplikacije je tray-first, pa startup ne treba otvarati veliki dashboard.

## Uninstall

Ne postoji zaseban `uninstall.exe`. Windows Installed apps poziva:

```text
SNAPVERE-Setup.exe --uninstall
```

Isti Setup binary upravlja install/update/remove lifecycleom. Prije rekurzivnog brisanja provjerava `.snapvere-installation` marker kako pogrešan registry put ne bi uzrokovao brisanje proizvoljne mape.

Uninstall uklanja instalacijske datoteke, shortcutove, Installed apps registraciju i startup registraciju samo kada ona pripada toj instalaciji. Screenshotovi u `Pictures\SNAPVERE` ostaju sačuvani.

## Portable

`SNAPVERE-Portable.exe` ekstrahira odgovarajući embedded payload u kontrolirani lokalni cache i pokreće ga. Startup registration iz Portable builda mora koristiti stabilni Portable launcher path, nikada privremeni extracted child path.

## Integritet i QA

Release pipeline provjerava da javna release mapa sadrži točno dva EXE-a, da sva tri native payloada sadrže `Snapvere.exe`, da x64/x86 lifecycle prolazi kroz Setup i Portable te da ARM64 payload uspješno cross-builda i prolazi strukturnu validaciju.
