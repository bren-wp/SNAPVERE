# SNAPVERE dokumentacija

Aktualna održavana linija proizvoda je **SNAPVERE 0.1.19** za Windows te Chrome, Edge, Operu i Firefox.

Ova dokumentacija opisuje produkcijske funkcije, tehničke granice i provjere koje stvarno izvodi CI. Aktualne javne binarne datoteke su v0.1.19, dok kasnije promjene na `main` mogu sadržavati izričito dokumentiran neobjavljeni reliability/security/UX hardening. Povijesne činjenice o starijim izdanjima ostaju u release dokumentaciji.

## Počnite ovdje

- [Korisnički vodič](USER-GUIDE.md) — capture, anotacije, copy/save i svakodnevni rad.
- [Instalacija](INSTALLATION.md) — Windows Setup/Portable i browser paketi.
- [Performanse i stabilnost](PERFORMANCE.md) — memorija, capture hardening, lifecycle i regresijski gateovi.
- [Rješavanje problema](TROUBLESHOOTING.md) — dijagnostika i kontrolirani recovery.
- [Status proizvoda](PRODUCT-STATUS.md) — što se održava i što CI rezultat stvarno dokazuje.

## Windows internals

- [Window Capture](WINDOW-CAPTURE.md) — native discovery, zamrznuti multi-monitor picker i WGC acquisition.
- [Image Pipeline](IMAGE-PIPELINE.md) — frame validation, crop/anotacije, PNG encode i atomsko spremanje.
- [Tray i lifecycle](TRAY-LIFECYCLE.md) — single-instance, native tray host, Explorer recovery i Setup/Portable lifecycle dokaz.
- [Korisnički vodič](USER-GUIDE.md) — Region Capture i lokalne Windows/browser postavke u jednom praktičnom vodiču.
- [QA matrica](QA-MATRIX.md) — automatizirani dokazi i njihove granice.
- [Engleska arhitektura](../ARCHITECTURE.md) — capture engine, multi-monitor/DPI, persistence i browser tehničke granice.

## Browser i privatnost

- [Browser ekstenzije](BROWSER-EXTENSIONS.md)
- [Privatnost](PRIVACY.md)
- [Security Policy](../../SECURITY.md)

## Brand i release proces

- [Branding](../BRANDING.md)
- [Versioning and Releases](../VERSIONING-RELEASES.md)

Službena stranica: https://snapvere.com  
Podrška: info@snapvere.com  
Aktualno izdanje: https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.19
