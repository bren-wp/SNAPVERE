# SNAPVERE status proizvoda

Dokument je usklađen s javnim izdanjem **SNAPVERE 0.1.1** i post-release maintenance stanjem grane `main`.

## Javno izdanje

Službeni release: https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.1

v0.1.1 je objavljen s točno osam validiranih javnih asseta:

- `SNAPVERE-Setup.exe`
- `SNAPVERE-Portable.exe`
- `SNAPVERE.apk`
- `SNAPVERE-Android-Source.zip`
- `SNAPVERE-Chrome.zip`
- `SNAPVERE-Edge.zip`
- `SNAPVERE-Opera.zip`
- `SNAPVERE-Firefox.zip`

Release workflow provjerava lokalne SHA-256 vrijednosti i nakon objave ih uspoređuje s digestima koje prikazuje GitHub.

## Windows status

**Implementirano i objavljeno u release paketu:** tray-first pokretanje, region capture/editor, window capture, screen capture, postavke, recent-capture workflow, jezici, Setup, Portable i universal x86/x64/ARM64 payload pakiranje.

**Automatizirani dokaz:** x64 build/test, x86 build, ARM64 cross-build/package validacija, provjera native payloada, stvarno renderirani UI snapshotovi, PR visual comparison, Setup/Portable package contract, x64/x86 lifecycle i tray-first probeovi.

**Granica dokaza:** ARM64 se na hosted x64 CI-ju validira kao cross-build i zapakirani payload. To nije tvrdnja o iscrpnom runtime testu na fizičkom ARM64 računalu. Javni Windows EXE paketi ne predstavljaju se kao da imaju komercijalni Authenticode reputation/signing identitet.

## Android status

**Implementirano i objavljeno u release paketu:** nativna Java aplikacija za Android 10+, novo MediaProjection odobrenje za svaku snimku, lokalni MediaStore PNG, Open/Share/Delete za zadnju snimku, EN/HR resursi, privacy/about akcije te ograničeni cleanup/timeout guardovi.

**Automatizirani dokaz:** manifest privacy/service/version provjere, SDK/tooling, lintDebug/lintRelease, JVM unit test, debug/release build, APK signature/alignment, source archive validacija i release digest provjera.

**Granica dokaza:** javni `SNAPVERE.apk` koristi CI/debug potpis. Ne predstavlja se kao Google Play production-signed i ne tvrdi se da je objavljen na Google Playu.

## Browser ekstenzije

**Implementirano i objavljeno u release paketu:** Chrome, Edge, Opera i Firefox paketi s visible, region i bounded full-page captureom, EN/HR localeovima, lokalnim postavkama, lokalnom obradom i minimalnim permission ugovorom.

**Automatizirani dokaz:** manifest validacija, exact permission allow-list, source parity, dimenzije ikona, locale parity, syntax/policy, deterministički store vizuali, store metadata/privacy validacija, reproducibilno dvostruko pakiranje i SHA-256.

**Granica dokaza:** GitHub ZIP nije isto što i listing/potpis/odobrenje u Chrome Web Storeu, Microsoft Edge Add-onsu, Opera Add-onsu ili Mozilla Add-onsu. Vanjski publisher računi i store review/signing zaseban su proces.

## Privatnost

- Windows: screenshot obrada i first-party capture workflow lokalni su; capture runtime nema first-party telemetry/cloud-upload worker.
- Android: nema `android.permission.INTERNET`; backup i cleartext su isključeni; MediaProjection service nije exported.
- Browser: trenutni extension ugovor nema telemetry, analytics, ads SDK, remote runtime dependency, `<all_urls>` ni široke host permissione.

Vidi [Privatnost](PRIVACY.md) i [Security Policy](../../SECURITY.md).

## Status dokumentacije

Aktivna dokumentacija usklađuje se kroz [`product-version.json`](../../product-version.json) i `eng/validate-product-contract.py`. Product Contract CI provjerava current-version konzistentnost, Android EN/HR resource-key parity, browser version parity, točna imena release asseta i relativne Markdown linkove.

Povijesni release notes ostaju povijesni i smiju opisivati stare verzije i funkcije.

## Podrška

Prije prijave problema vidi [Rješavanje problema](TROUBLESHOOTING.md). U prijavu uključi platformu/verziju/arhitekturu, capture način i korake za reprodukciju.

Podrška: **info@snapvere.com**
