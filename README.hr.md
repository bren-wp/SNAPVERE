# SNAPVERE

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/branding/readme/snapvere-logo-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="assets/branding/readme/snapvere-logo-light.svg">
  <img alt="SNAPVERE — Capture. Edit. Done." src="assets/branding/readme/snapvere-logo-light.svg" width="560">
</picture>

**Capture. Edit. Done. — lokalno i privatno snimanje zaslona za Windows, Android i moderne preglednike koje razvija Brendigo.**

[English README](README.md) · [Službena stranica](https://snapvere.com) · [Podrška](mailto:info@snapvere.com) · [Developer: Brendigo](https://brendigo.com)

Trenutna verzija: **SNAPVERE 0.1.1**.

0.1.1 zadržava validiranu tray-first Windows aplikaciju i nativni Android companion iz linije 0.1.0 te prvi put uključuje Chrome, Edge, Opera i Firefox ekstenzije u službeni javni GitHub Release ugovor. Već objavljeni povijesni tagovi i release asseti ostaju nepromijenjeni.

## Windows

SNAPVERE za Windows je tray-first .NET 10 / WinUI 3 aplikacija. Normalni start zadržava aplikaciju u notification area umjesto otvaranja launcher dashboarda.

| Akcija | Primarni unos | Alternativa |
| --- | --- | --- |
| Snimanje područja | lijevi klik tray ili **Print Screen** | `Ctrl+Shift+1` |
| Snimanje prozora | desni klik tray → Capture window | `Ctrl+Shift+2` |
| Snimanje zaslona | desni klik tray → Capture screen | `Ctrl+Shift+3` |
| Postavke / nedavne snimke | desni klik tray | — |
| Jezik / About / Exit | desni klik tray | — |

Region Capture koristi frozen-frame fizičku pixel selekciju, resize/pomicanje, Pen, Line, Arrow, Box i Highlight alate, boje, Undo, Copy i Save. Window Capture koristi nativno otkrivanje top-level prozora i Windows.Graphics.Capture, a ne skriveni screen crop. Screen Capture preferira Windows.Graphics.Capture/Direct3D uz fallback za očekivane acquisition probleme.

PNG se dovršava lokalno prije opcionalnog premještanja kroz nativni **Save As**. Odustajanje od Save As zadržava gotovu sliku u `Pictures\SNAPVERE`.

Javni Windows paketi su universal hostovi s x86, x64 i ARM64 payloadima:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
```

Minimalni Windows target je Windows 10 1809/build 17763. WGC putovi zahtijevaju Windows 10 2004/build 19041 ili noviji.

## Android 0.1.1

SNAPVERE za Android 10+ je nativna Java 17 aplikacija na MediaProjection i MediaStore API-jima.

Verzija: **0.1.1** / `versionCode 11`.

Svaka snimka zahtijeva novo Android sustavsko dopuštenje. Consent token se ne cacheira niti ponovno koristi i SNAPVERE ne snima kontinuirano u pozadini.

Android workflow uključuje:

- eksplicitno full-screen snimanje;
- lokalni PNG u `Pictures/SNAPVERE`;
- validirane **Otvori / Podijeli / Izbriši** akcije za zadnju snimku;
- engleske i hrvatske resurse;
- korisnički pokrenute Web / Podrška / Privatnost / Uvjeti akcije;
- responzivne kontrole, velike touch targete i system-inset handling;
- bez računa, telemetrije, analyticsa, oglasa, cloud uploada i `INTERNET` permissiona.

Capture put zadržava bounded Activity-hide/first-frame guardove, owner-aware capture ownership, exception-safe MediaProjection/VirtualDisplay/ImageReader cleanup te RGBA stride/buffer validaciju.

Javni `SNAPVERE.apk` za v0.1.1 i dalje koristi provjereni CI/debug signing identitet. Paket je instalabilan, ali se **ne predstavlja kao Google Play/production-potpisan**. Budući Android kanal s drugim production potpisom može zahtijevati deinstalaciju i novu instalaciju.

Release također sadrži `SNAPVERE-Android-Source.zip`, generiran izravno iz validiranog Android Git treea bez build cachea i signing materijala.

Vidi [Android arhitekturu i QA](docs/hr/ANDROID.md) i [Android source/build vodič](android/README.md).

## Browser ekstenzije 0.1.1

v0.1.1 je prvo javno SNAPVERE izdanje s browser paketima za:

- Google Chrome;
- Microsoft Edge;
- Operu;
- Mozilla Firefox.

Ekstenzije podržavaju snimanje vidljivog područja, bounded full-page snimanje kontroliranim scrollanjem/stitchanjem i pravokutno odabrano područje. Pikseli snimke ostaju lokalni i spremaju se kao PNG.

Chromium varijante koriste Manifest V3 service worker, a Firefox kompatibilni Manifest V3 WebExtension background model. Sve varijante traže samo:

- `activeTab`;
- `scripting`;
- `downloads`;
- `storage`.

Nema `<all_urls>` niti širokog host permissiona. Source ne sadrži telemetriju, analytics, oglasni SDK, cloud uploader ni remote runtime dependency.

Javni browser asseti:

```text
SNAPVERE-Chrome.zip
SNAPVERE-Edge.zip
SNAPVERE-Opera.zip
SNAPVERE-Firefox.zip
```

Paketi se reproducibilno generiraju u dva neovisna prolaza i moraju biti byte-for-byte identični prije objave. Store metadata i privacy deklaracije validiraju se prema stvarnim manifestima. Vidi [Browser ekstenzije](docs/hr/BROWSER-EXTENSIONS.md), [vodič za source](ekstenzije/README.md) i [browser privacy policy](ekstenzije/PRIVACY.md).

GitHub Release **ne znači** da je ekstenzija objavljena ili odobrena u Chrome Web Storeu, Edge Add-onsu, Opera Add-onsu ili Mozilla Add-onsu. Ti kanali zahtijevaju zasebne autentificirane publisher račune i njihov review/certification/signing proces.

## Javni release ugovor v0.1.1

Valjano v0.1.1 GitHub izdanje sadrži **točno osam javnih asseta**:

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

Release workflow računa SHA-256 za svaki asset, kreira nepromjenjivi `v0.1.1` tag tek nakon svih validacijskih gateova, objavljuje samo odobrena imena i zatim uspoređuje GitHub digest svakog objavljenog asseta s lokalno validiranom vrijednošću.

Povijesni **v0.1.0** ostaje nepromijenjen sa svoja originalna četiri Windows/Android asseta. Browser ZIP-ovi se ne dodaju retroaktivno u v0.1.0.

## Privatnost i sigurnost

SNAPVERE je local-first:

- Windows obrada snimki je lokalna i desktop runtime nema first-party telemetry/cloud-upload worker;
- Android namjerno nema `android.permission.INTERNET`, cleartext i backup su isključeni, a MediaProjection servis nije exported;
- browser ekstenzije ne šalju screenshotove izvan uređaja i ne traže široki pristup web stranicama.

Build/release kontrole uključuju:

- NuGet audit direktnih i tranzitivnih ovisnosti od `low` severity nadalje;
- `NU1901`–`NU1904` kao build failure;
- nullable/analyzer enforcement i deterministic .NET build;
- GitHub Actions pinane na puni SHA;
- arhitekturne SHA-256 manifeste u Portable hostu;
- transactional Portable cache validaciju/rebuild;
- Android privacy/version/service/lint/signature/alignment gateove;
- browser manifest/permission/source/parity/store-metadata provjere;
- exact release-asset i post-publication digest provjeru.

Vidi [Security Policy](SECURITY.md) i [0.1.1 sigurnost/performance](docs/hr/SECURITY-PERFORMANCE-0.1.1.md).

## Automatizirani QA

### Windows

GitHub Actions:

- restore s NuGet auditom;
- x64 build/test;
- x86 build i ARM64 cross-build;
- validacija native payloada i integrity manifesta;
- šest stvarno renderiranih UI površina;
- PR visual comparison prema zadnjem zelenom `main` baselineu;
- universal Setup/Portable build;
- validacija javnog package ugovora;
- x64/x86 Setup+Portable lifecycle i tray-first probeovi.

ARM64 na hosted x64 runneru ostaje cross-build/package dokaz, ne fizički ARM64 runtime test.

### Android

Android CI/release provjerava privacy/service/version manifest, SDK/tooling, `lintDebug`, `lintRelease`, JVM unit testove, debug/release buildove, potpis/alignment javnog APK-a, strukturu source arhive te SHA-256 tijekom artifact transfera i objave.

Zeleni Android workflow je build/package dokaz, ne iscrpni fizički OEM/device runtime test.

### Browseri

Browser CI/release provjerava sve četiri varijante, source parity, točan permission allow-list, locale, dimenzije ikona, source syntax/policy, store metadata/privacy deklaracije, determinističke store vizuale, reproducibilne ZIP-ove, čistoću paketa i SHA-256 integritet.

Zeleni browser workflow je static/package dokaz, ne iscrpno ručno GUI testiranje svakog browser builda ili web aplikacije.

## Performanse i stabilnost

SNAPVERE je event-driven dok miruje. Windows tray/hotkey hostovi koriste nativne message loopove umjesto pollinga, capture/D3D resursi stvaraju se samo za aktivni capture. Android nema idle capture loop ni network worker. Browser ekstenzije aktiviraju capture kod samo nakon korisničke akcije.

Ne obećava se fiksni CPU/RAM postotak jer OS/browser verzija, monitori/DPI, driveri, Android OEM, složenost stranice, rezolucija i aktivni capture/editor rad utječu na potrošnju.

## Jezici

English je canonical default/fallback. Windows nudi 28 ugrađenih jezika uključujući hrvatski. Android i browser ekstenzije imaju namjenske engleske i hrvatske resurse.

Nema translation API-ja ni background language network servisa.

## Dokumentacija

Engleska dokumentacija: [`docs/`](docs/). Hrvatska dokumentacija: [`docs/hr/`](docs/hr/).

Ključni dokumenti:

- [Arhitektura](docs/hr/ARCHITECTURE.md)
- [Capture engine](docs/hr/CAPTURE-ENGINE.md)
- [Region Capture](docs/hr/REGION-CAPTURE.md)
- [Window Capture](docs/hr/WINDOW-CAPTURE.md)
- [Instalacija](docs/hr/INSTALLATION.md)
- [Android](docs/hr/ANDROID.md)
- [Browser ekstenzije](docs/hr/BROWSER-EXTENSIONS.md)
- [0.1.1 sigurnost i performanse](docs/hr/SECURITY-PERFORMANCE-0.1.1.md)
- [Branding](docs/hr/BRANDING.md)
- [Release Notes 0.1.1](RELEASE_NOTES_0.1.1.md)

## Tehnologija

- C# / .NET 10
- WinUI 3 / Windows App SDK 1.8 stable line
- Windows.Graphics.Capture + Direct3D 11
- Win32 / DWM / GDI interoperabilnost
- Java 17 / Android MediaProjection / MediaStore
- Manifest V3 / WebExtensions
- deterministic buildovi i reproducibilno browser pakiranje
- xUnit + JUnit 4 + GitHub Actions

## Dijagnostika

Windows startup log:

```text
%LOCALAPPDATA%\SNAPVERE\Logs\startup.log
```

Pikseli snimke ne zapisuju se namjerno u taj log.

## Licenca i vlasništvo

**SNAPVERE 0.0.7 i noviji distribuiraju se pod SNAPVERE Commercial Software License Agreement licencom u [`LICENSE`](LICENSE).** SNAPVERE je naziv proizvoda; Brendigo je developer i izdavač.

Službena stranica: **snapvere.com** · Podrška: **info@snapvere.com**.

---

**SNAPVERE — Capture. Edit. Done.**
