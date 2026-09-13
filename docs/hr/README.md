# SNAPVERE dokumentacija — Hrvatski

Ova mapa je hrvatsko središte dokumentacije za aktualnu proizvodnu liniju **SNAPVERE 0.1.1**. Opisuje stvarno implementiranu Windows aplikaciju, nativnu Android aplikaciju, browser ekstenzije, pakiranje, privatnost, QA dokaze i release proces.

Za pregled proizvoda i preuzimanja kreni od [glavnog hrvatskog README-a](../../README.hr.md). Engleska dokumentacija indeksirana je u [`docs/README.md`](../README.md).

## Počni ovdje

- [Korisnički vodič](USER-GUIDE.md) — svakodnevni Windows, Android i browser workflowi.
- [Instalacija](INSTALLATION.md) — Setup, Portable, APK i ručna instalacija browser ekstenzija.
- [Rješavanje problema](TROUBLESHOOTING.md) — česti problemi, ograničenja i provjere.
- [Status proizvoda](PRODUCT-STATUS.md) — što je objavljeno, validirano i što još ovisi o vanjskim storeovima.
- [Privatnost](PRIVACY.md) — local-first ponašanje po platformama.
- [QA matrica](QA-MATRIX.md) — što CI stvarno dokazuje, a što ne.
- [Verzioniranje i izdanja](VERSIONING-RELEASES.md) — canonical version contract i pravila nepromjenjivih izdanja.

## Windows aplikacija

- [Arhitektura](ARCHITECTURE.md)
- [Capture engine](CAPTURE-ENGINE.md)
- [Region Capture](REGION-CAPTURE.md)
- [Window Capture](WINDOW-CAPTURE.md)
- [Multi-monitor i DPI](MULTI-MONITOR.md)
- [Image pipeline](IMAGE-PIPELINE.md)
- [Tray UX](TRAY-UX.md)
- [Postavke](SETTINGS.md)
- [Visual QA](VISUAL-QA.md)

## Android

- [Android arhitektura i QA](ANDROID.md)
- [Android source/build vodič](../../android/README.md)

## Browser ekstenzije

- [Browser ekstenzije](BROWSER-EXTENSIONS.md)
- [Extension source vodič](../../ekstenzije/README.md)
- [Privacy policy browser ekstenzija](../../ekstenzije/PRIVACY.md)

## Release, sigurnost i branding

- [SNAPVERE 0.1.1 Security & Performance](SECURITY-PERFORMANCE-0.1.1.md)
- [Security Policy](../../SECURITY.md)
- [Branding](BRANDING.md)
- [About i support linkovi](ABOUT-SUPPORT-LINKS.md)
- [Release Notes 0.1.1](../../RELEASE_NOTES_0.1.1.md)
- [Changelog](../../CHANGELOG.md)

## Canonical izvor verzije

Strojno čitljivi izvor istine je [`product-version.json`](../../product-version.json). `eng/validate-product-contract.py` i **Product Contract CI** provjeravaju da aktivna dokumentacija, .NET verzija, Android verzija, browser manifesti/store metadata i javni ugovor od osam release asseta ostanu međusobno usklađeni.

Povijesni release notes ostaju povijesni zapisi. Ne prepisuju se tako da starija izdanja izgledaju kao da su sadržavala današnje funkcije.

## Podrška

Službena stranica proizvoda: **https://snapvere.com**  
Podrška: **info@snapvere.com**  
Developer i publisher: **Brendigo** — https://brendigo.com
