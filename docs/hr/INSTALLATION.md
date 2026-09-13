# Instalacija i release paketi

Ovaj vodič opisuje aktualne javne pakete **SNAPVERE 0.1.1**. Službeni release: https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.1

## Javni release ugovor v0.1.1

Valjani v0.1.1 GitHub Release sadrži točno osam SNAPVERE asseta:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE.apk
SNAPVERE-Android-Source.zip
SNAPVERE-Chrome.zip
SNAPVERE-Edge.zip
SNAPVERE-Opera.zip
SNAPVERE-Firefox.zip
```

Povijesna izdanja zadržavaju vlastite povijesne asset ugovore. v0.1.0 ostaje originalni release s četiri Windows/Android asseta i ne mijenja se retroaktivno.

## Windows arhitekture

`SNAPVERE-Setup.exe` i `SNAPVERE-Portable.exe` universal su javni hostovi s native application payloadima za:

- x86 / x32 / 32-bit Windows;
- x64 / AMD64 Windows;
- ARM64 Windows.

Host automatski prepoznaje Windows arhitekturu i bira kompatibilni payload. Korisnik ne mora birati zaseban architecture download.

Minimalni target je Windows 10 1809 / build 17763. Windows.Graphics.Capture ovisni putevi zahtijevaju Windows 10 2004 / build 19041 ili noviji.

## Windows tray-first startup

Normalni launch inicijalizira capture coordination, hotkeye i notification-area ikonu bez stalnog dashboarda.

- lijevi klik tray → Region Capture;
- desni klik tray → quick actions;
- **Print Screen** → Region Capture kada je dostupan;
- `Ctrl+Shift+1` → Region Capture fallback;
- `Ctrl+Shift+2` → Window Capture;
- `Ctrl+Shift+3` → Screen Capture.

Postavke, jezici, recent captures i About otvaraju se samo kada ih korisnik zatraži.

## Windows Setup

Download: https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Setup.exe

Zadani per-user install direktorij:

```text
%LOCALAPPDATA%\Programs\SNAPVERE
```

Interactive Setup zahtijeva prihvaćanje **SNAPVERE Commercial Software License Agreement**. Opcionalni defaulti su opt-out:

- Start menu shortcut — uključeno;
- Desktop icon — uključeno;
- Start SNAPVERE with Windows — uključeno.

Instalirani maintenance binary:

```text
%LOCALAPPDATA%\Programs\SNAPVERE\SNAPVERE-Setup.exe
```

Isti binary upravlja install/update/remove lifecycleom.

### Silent install

```text
SNAPVERE-Setup.exe --silent --accept-license
```

Bez `--accept-license` silent instalacija završava s kodom `2`.

### Start with Windows

Per-user startup registracija:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\SNAPVERE
```

### Uninstall istim Setup binaryjem

Windows Installed apps poziva:

```text
SNAPVERE-Setup.exe --uninstall
```

Tihi uninstall:

```text
SNAPVERE-Setup.exe --uninstall --silent
```

SNAPVERE ne instalira zaseban `uninstall.exe`, `uninstaller.exe` ni `unins*.exe`.

Prije rekurzivnog brisanja install direktorija Setup provjerava installation marker i očekivane datoteke. Korisničke snimke ostaju izvan instalacije u:

```text
Pictures\SNAPVERE
```

## Windows Portable

Download: https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Portable.exe

Portable host ugrađuje sve podržane native payloade i ekstrahira samo kompatibilnu arhitekturu u kontrolirani cache. Zaštite uključuju:

- bez Installed apps registracije;
- bez prisilnih Start menu/Desktop shortcutova;
- self-contained application payload;
- archive traversal/absolute-target rejection;
- bounded extraction i duplicate-destination rejection;
- architecture-specific SHA-256 integrity manifeste;
- path/length/hash validaciju prije cached executiona;
- reparse-point i unexpected-file rejection;
- transactional invalid-cache rebuild/revalidation;
- version/architecture cache reuse tek nakon validacije;
- launcher-preparation mutex i lokalnu startup dijagnostiku.

Ako korisnik eksplicitno uključi startup, Portable registracija pokazuje na originalni Portable launcher, a ne versioned child u extraction cacheu.

## Android APK

Download: https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE.apk

Zahtjevi i identitet:

- Android 10 / API 29 ili noviji;
- `versionName 0.1.1` / `versionCode 11`;
- nativna Java 17 aplikacija;
- bez `android.permission.INTERNET`;
- novo Android MediaProjection odobrenje za svaki capture.

Javni v0.1.1 APK koristi validirani **CI/debug signing identitet**. Instalabilan je, ali se ne predstavlja kao Google Play/production-signed. Buduća verzija s drugim production signing identitetom može zahtijevati uninstall/reinstall umjesto in-place updatea.

Pri instalaciji izvan app storea Android može zahtijevati eksplicitno dopuštenje za odabrani izvor. SNAPVERE ne pokušava zaobići package-installation policy.

Release gate provjerava APK signature, ZIP alignment i SHA-256 integritet.

## Android source paket

Download: https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Android-Source.zip

Source ZIP generira se iz validiranog tracked `android/` treea putem `git archive`. Release gate provjerava očekivane Gradle, manifest i application source putanje te odbija generirani `build/` / `.gradle/` cache. Privatni signing materijal nije dio arhive.

## Chrome ručna instalacija

Paket: https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Chrome.zip

1. Raspakiraj ZIP u stabilnu lokalnu mapu.
2. Otvori `chrome://extensions`.
3. Uključi **Developer mode**.
4. Odaberi **Load unpacked**.
5. Odaberi raspakiranu mapu koja sadrži `manifest.json`.

## Microsoft Edge ručna instalacija

Paket: https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Edge.zip

1. Raspakiraj ZIP.
2. Otvori `edge://extensions`.
3. Uključi **Developer mode**.
4. Odaberi **Load unpacked**.
5. Odaberi raspakiranu extension mapu.

## Opera ručna instalacija

Paket: https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Opera.zip

1. Raspakiraj ZIP.
2. Otvori `opera://extensions`.
3. Uključi developer mode ako to aktualni Opera UI zahtijeva.
4. Odaberi opciju za učitavanje unpacked ekstenzije.
5. Odaberi raspakiranu extension mapu.

## Firefox privremena/development instalacija

Paket: https://github.com/bren-wp/SNAPVERE/releases/download/v0.1.1/SNAPVERE-Firefox.zip

Za development/test raspakiraj ZIP, otvori `about:debugging`, odaberi **This Firefox**, zatim **Load Temporary Add-on** i odaberi raspakirani `manifest.json`.

Firefox store distribucija ima zaseban AMO review/signing proces. GitHub ZIP se ne predstavlja kao AMO-signed ili store-approved paket.

## Browser store status

Četiri browser ZIP-a službeni su v0.1.1 GitHub release asseti, ali GitHub objava **ne znači** objavu/odobrenje u Chrome Web Storeu, Edge Add-onsu, Opera Add-onsu ili Mozilla Add-onsu. Ti kanali zahtijevaju autentificirane publisher račune i vanjski review/signing.

## Release validacija

Prije v0.1.1 objave automatizacija je zahtijevala:

### Windows

1. audited restore/build/test;
2. x86/x64/ARM64 payload generiranje;
3. architecture integrity manifeste;
4. native payload validaciju;
5. universal Setup/Portable build;
6. exact javni package contract;
7. x64/x86 Setup+Portable lifecycle i tray-first probeove.

### Android

1. privacy/service/version contract;
2. `lintDebug` i `lintRelease`;
3. JVM unit testove;
4. debug/release variant build;
5. APK signature i ZIP-alignment provjeru;
6. source ZIP validaciju;
7. SHA-256 transfer validaciju.

### Browser ekstenzije

1. MV3 manifest/permission/source validaciju;
2. cross-browser parity;
3. EN/HR locale provjere;
4. store metadata/privacy provjeru;
5. reproducibilno dvostruko pakiranje;
6. exact četiri ZIP imena i SHA-256.

### Objava

Finalni release direktorij morao je sadržavati točno osam datoteka s početka ovog vodiča. Release workflow zatim je kreirao/provjerio `v0.1.1`, objavio GitHub Release i usporedio GitHub digest svakog asseta s lokalno validiranim SHA-256.

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

## Dalje

- [Korisnički vodič](USER-GUIDE.md)
- [Rješavanje problema](TROUBLESHOOTING.md)
- [Privatnost](PRIVACY.md)
- [QA matrica](QA-MATRIX.md)
- [Verzioniranje i izdanja](VERSIONING-RELEASES.md)

SNAPVERE razvija i objavljuje **Brendigo**. Službena stranica: https://snapvere.com · Podrška: **info@snapvere.com**.
