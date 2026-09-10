# Instalacija i universal packaging

Od SNAPVERE **0.0.7** nadalje javni release sadrži samo dvije datoteke:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
```

Korisnik ne bira zaseban x86, x64 ili ARM64 download. Svaki host ugrađuje native application payloade za x86, x64 i ARM64 te automatski odabire kompatibilan payload za aktualni Windows. Trenutačno objavljeno izdanje je **v0.0.8**; kasnije promjene na `main` nisu novo izdanje dok zasebni release proces ne objavi novi tag i assete.

## Podržane arhitekture

- x86 / x32 / 32-bit Windows
- x64 / AMD64 Windows
- ARM64 Windows

Universal host je namjerno građen kao x86-compatible executable kako bi se mogao pokrenuti i na 32-bit Windowsu. Na x64 i ARM64 sustavima zatim pokreće odgovarajući native child payload.

## Verzija Windowsa

Architecture support nije isto što i podrška za svaki stari Windows. Minimalni application target je Windows 10 version 1809 / build 17763. WGC ovisni capture putevi zahtijevaju Windows 10 version 2004 / build 19041 ili noviji.

## Tray-first startup

Normalno pokretanje inicijalizira skriveni capture coordinator, globalne hotkeye i notification-area ikonu bez otvaranja starog Capture Center prozora.

- lijevi klik tray → Region Capture;
- desni klik tray → brendirani quick-actions flyout;
- Print Screen → Region Capture kada je registracija dostupna;
- `Ctrl+Shift+1` → Region fallback;
- `Ctrl+Shift+2` → Window Capture;
- `Ctrl+Shift+4` → Screen Capture.

Options, Language, Recent Captures i About stvaraju se samo kada ih korisnik zatraži.

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

Portable koristi stabilni originalni `SNAPVERE-Portable.exe` launcher path za ovu postavku, a ne privremeni child executable unutar extraction cachea.

## Uninstall

Ne postoji zaseban `uninstall.exe`. Windows Installed apps poziva:

```text
SNAPVERE-Setup.exe --uninstall
```

Isti Setup binary upravlja install/update/remove lifecycleom. Prije rekurzivnog brisanja provjerava installation marker kako pogrešan registry put ne bi uzrokovao brisanje proizvoljne mape.

Uninstall uklanja instalacijske datoteke, shortcutove, Installed apps registraciju i startup registraciju samo kada ona pripada toj instalaciji. Screenshotovi u `Pictures\SNAPVERE` ostaju sačuvani.

## Portable

`SNAPVERE-Portable.exe` ekstrahira odgovarajući embedded payload u kontrolirani lokalni cache i pokreće ga. Startup registration iz Portable builda mora koristiti stabilni Portable launcher path, nikada privremeni extracted child path.

## Integritet i QA

CI i release pipeline provjeravaju:

1. x64 build i unit testove;
2. x86 build;
3. ARM64 cross-build;
4. da sva tri native payload arhiva sadrže root `Snapvere.exe`;
5. da javni package direktorij sadrži točno `SNAPVERE-Setup.exe` i `SNAPVERE-Portable.exe`;
6. obavezno prihvaćanje komercijalne licence za silent Setup;
7. instalaciju, Desktop shortcut, Start-with-Windows registraciju i Installed apps metadata;
8. tray-first startup;
9. materijalizaciju Region, Window, Tray, Options, Language i About WinUI površina;
10. šest stvarno renderiranih x64 PNG površina — Region, Window, Tray, Options, Language i About — pri čemu prazan ili neočekivano malen frame ruši CI;
11. visual-QA manifest s dimenzijama, veličinom i SHA-256 sažetkom svakog PNG-a te upload tog manifesta i PNG-ova kao GitHub Actions artefakta;
12. stvarni x64/x86 universal Setup i Portable lifecycle;
13. uninstall istim Setupom i uklanjanje pripadajućih instalacijskih artefakata;
14. očuvanje korisničkih screenshotova izvan uninstall scopea.

ARM64 se cross-builda i package-validira na hosted x64 Windows runneru; to se ne predstavlja kao stvarni ARM64 hardware runtime test. SHA-256 služi za provjeru integriteta i nije Authenticode potpis.

## Dijagnostika i postavke

Startup log ostaje lokalno u:

```text
%LOCALAPPDATA%\SNAPVERE\Logs\startup.log
```

Postavke se spremaju lokalno u:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Screenshot pixeli ne zapisuju se u startup log.

## Licenca i identitet proizvoda

SNAPVERE 0.0.7 i noviji koriste komercijalnu licencu iz root `LICENSE` datoteke. Proizvod je **SNAPVERE**, developer/publisher je **Brendigo**, službena stranica proizvoda je **https://snapvere.com**, a developerova stranica **https://brendigo.com**.

Povijesna izdanja ostaju pod uvjetima licence distribuirane s tim izdanjima.
