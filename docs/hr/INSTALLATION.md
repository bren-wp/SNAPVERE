# Instalacija i release paketi

## Javni release ugovor 0.1.0

Valjano SNAPVERE **0.1.0** GitHub izdanje sadrži točno četiri korisnička asseta:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE.apk
SNAPVERE-Android-Source.zip
```

Dva Windows hosta su universal x86/x64/ARM64 launcheri. Android APK je release build potpisan stabilnim SNAPVERE Android release identitetom. Android source ZIP generira se iz točno validiranog Git treea.

Povijesna izdanja zadržavaju vlastite povijesne asset ugovore i ne prepisuju se.

## Windows arhitekture

`SNAPVERE-Setup.exe` i `SNAPVERE-Portable.exe` ugrađuju native application payloade za:

- x86 / x32 / 32-bit Windows;
- x64 / AMD64 Windows;
- ARM64 Windows.

Host automatski prepoznaje Windows arhitekturu i bira kompatibilni native payload. Korisnik ne bira poseban architecture download.

Minimalni application target ostaje Windows 10 version 1809 / build 17763. WGC ovisni capture putevi zahtijevaju Windows 10 version 2004 / build 19041 ili noviji.

## Tray-first startup

Normalni Windows launch inicijalizira capture coordinator, globalne hotkeye i notification-area ikonu bez launcher dashboarda.

- lijevi klik tray → Region Capture;
- desni klik tray → quick actions;
- Print Screen → Region Capture kada je dostupno;
- `Ctrl+Shift+1` → Region Capture fallback;
- `Ctrl+Shift+2` → Window Capture;
- `Ctrl+Shift+3` → Screen Capture.

Options, Language, Recent Captures i About stvaraju se samo kada ih korisnik zatraži.

## Windows Setup

Zadani install direktorij:

```text
%LOCALAPPDATA%\Programs\SNAPVERE
```

Instalacija je per-user i za osnovni workflow ne zahtijeva Program Files administraciju.

Interactive Setup zahtijeva prihvaćanje **SNAPVERE Commercial Software License Agreement**. Opcionalne zadane postavke su opt-out:

- Start menu shortcut — uključeno;
- Desktop icon — uključeno;
- Start SNAPVERE with Windows — uključeno.

Instalirani maintenance binary je:

```text
%LOCALAPPDATA%\Programs\SNAPVERE\SNAPVERE-Setup.exe
```

Isti binary upravlja install/update/remove lifecycleom.

### Silent install

```text
SNAPVERE-Setup.exe --silent --accept-license
```

Silent install bez `--accept-license` završava s kodom `2`.

## Start with Windows

Per-user startup registracija:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\SNAPVERE
```

Pokazuje na stabilni SNAPVERE launcher. Portable koristi originalni Portable launcher path, a ne versioned child unutar extraction cachea.

## Uninstall istim Setupom

Windows Installed apps poziva:

```text
SNAPVERE-Setup.exe --uninstall
```

Tihi uninstall:

```text
SNAPVERE-Setup.exe --uninstall --silent
```

SNAPVERE ne instalira zaseban `uninstall.exe`, `uninstaller.exe` niti `unins*.exe`.

Prije rekurzivnog brisanja install direktorija Setup validira SNAPVERE installation marker i očekivane datoteke. Uninstall uklanja aplikaciju, Setup shortcutove, Installed apps metadata i startup registraciju samo kada pripada validiranoj instalaciji.

Korisničke snimke ostaju izvan install direktorija u:

```text
Pictures\SNAPVERE
```

## Windows Portable

`SNAPVERE-Portable.exe` ugrađuje sve podržane native payloade i ekstrahira samo kompatibilni u kontrolirani Portable cache.

Portable zaštite uključuju:

- bez Installed apps registracije;
- bez prisilnih Start menu/Desktop shortcutova;
- self-contained application payload;
- odbijanje archive traversal/absolute targeta;
- bounded extraction i odbijanje duplicate destinationa;
- trusted architecture-specific SHA-256 manifest ugrađen u host;
- provjeru očekivanih putanja, veličina i hashova prije cached executiona;
- odbijanje reparse-point i unexpected-file sadržaja;
- transactional rebuild/revalidation nevaljanog reusable cachea;
- version/architecture reuse tek nakon validacije;
- launcher-preparation mutex i lokalnu startup dijagnostiku.

## Instalacija Android APK-a

`SNAPVERE.apk` cilja Android 10 / API 29 ili noviji. Nastaje iz minificiranog/shrunk release varianta i prihvaća se za javno izdanje tek nakon `zipalign` i `apksigner` provjere.

Android paket namjerno ne traži `INTERNET` permission. Snimanje zaslona zahtijeva Android sustavsko MediaProjection odobrenje za svaku capture sesiju.

Kod instalacije izvan app storea Android može zahtijevati da korisnik izričito dopusti instalaciju iz odabranog izvora. SNAPVERE ne pokušava zaobići Android package-installation policy.

Budući Android update mora biti potpisan istim stabilnim release identitetom kao instalirani javni APK. Zato release workflow nikada ne zamjenjuje nedostajući release key ephemeral CI debug ključem.

## Android source paket

`SNAPVERE-Android-Source.zip` generira se iz praćenog `android/` treea validiranog release commita putem `git archive`.

Release gate provjerava očekivane Gradle, manifest i MainActivity putanje te odbija generirani `build/` i `.gradle/` cache sadržaj. Privatni signing materijal nije dio arhive.

## Release validacija

Prije objave `v0.1.0` automatizacija zahtijeva:

### Windows

1. audited x64 restore/build/test;
2. x86 build;
3. ARM64 cross-build;
4. root `Snapvere.exe` u sva tri native payloada;
5. valjane architecture integrity manifeste;
6. šest stvarno renderiranih WinUI površina;
7. universal Setup i Portable build;
8. x64/x86 Setup+Portable lifecycle i tray-first probeove;
9. uninstall cleanup uz očuvanje korisničkih snimki.

### Android

1. privacy/service/version manifest ugovor;
2. `lintDebug` i `lintRelease` uz warnings-as-errors;
3. JVM unit testove;
4. debug i minificirani release build;
5. stabilni release signing materijal izvan Git sourcea;
6. ZIP alignment i kriptografsku APK-signature provjeru;
7. strukturno valjan Android source ZIP;
8. SHA-256 transfer provjeru iz Android joba u finalni release job.

### Objava

Finalni release direktorij mora sadržavati točno:

```text
SNAPVERE-Android-Source.zip
SNAPVERE-Portable.exe
SNAPVERE-Setup.exe
SNAPVERE.apk
```

Tek tada smije nastati immutable `v0.1.0` tag. Nakon objave GitHub SHA-256 digest svakog asseta mora odgovarati lokalno validiranom digestu.

ARM64 Windows dokaz na hosted x64 runneru je cross-build/package validacija, ne fizički ARM64 runtime. Android automatizacija nije tvrdnja o iscrpnom testu na svakom fizičkom OEM uređaju.

## Dijagnostika

Windows startup log:

```text
%LOCALAPPDATA%\SNAPVERE\Logs\startup.log
```

Windows postavke:

```text
%LOCALAPPDATA%\SNAPVERE\settings.json
```

Screenshot pikseli ne zapisuju se namjerno u startup log.

## Licenca i identitet proizvoda

SNAPVERE 0.0.7 i noviji koriste komercijalnu licencu iz root `LICENSE` datoteke. Proizvod: **SNAPVERE**. Developer/publisher: **Brendigo**. Službena stranica: **https://snapvere.com**. Developer stranica: **https://brendigo.com**.

Povijesna izdanja ostaju pod uvjetima koji su isporučeni s tim verzijama.
