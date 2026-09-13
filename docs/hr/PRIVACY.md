# SNAPVERE privatnost

Ovaj dokument opisuje implementirano first-party privacy ponašanje proizvoda **SNAPVERE 0.1.1**. Riječ je o tehničkoj dokumentaciji proizvoda, ne o zamjeni za pravnu obavijest prilagođenu pojedinoj jurisdikciji na službenoj web stranici.

## Temeljno načelo

SNAPVERE je dizajniran kao **local-first alat za snimanje zaslona**. Capture i encoding u aktualnim Windows, Android i browser implementacijama ne zahtijevaju SNAPVERE račun, first-party cloud upload, analytics pipeline ni oglasni SDK.

## Windows

Windows snimke nastaju i kodiraju se lokalno. Zadana lokacija je `Pictures\SNAPVERE`. Save As je eksplicitna korisnička akcija kroz nativni file picker.

Windows capture runtime nema first-party telemetry uploader ni automatski screenshot cloud-sync worker. Startup dijagnostika može se zapisivati u `%LOCALAPPDATA%\SNAPVERE\Logs\startup.log`; pikseli screenshotova ne zapisuju se namjerno u taj log.

Otvaranje, kopiranje, spremanje ili kasnije dijeljenje generirane datoteke kroz Windows ili drugu aplikaciju pod kontrolom je korisnika i može uključiti softver izvan SNAPVERE-a.

## Android

Android manifest namjerno **ne traži** `android.permission.INTERNET`. Aplikacija koristi Android MediaProjection za korisnički odobren jednokratni screen capture i MediaStore za lokalni PNG u `Pictures/SNAPVERE`.

Za svaku snimku Android prikazuje novo sustavsko MediaProjection odobrenje. SNAPVERE ne cacheira niti ponovno koristi consent token za tihe buduće capturee.

Aplikacija lokalno sprema samo lagane preference potrebne da identificira URI/naziv zadnje snimke za Open/Share/Delete UI. Ako URI više nije čitljiv, zastarjela referenca se uklanja.

Eksplicitne Website, Support, Privacy, Terms, Open i Share akcije mogu pokrenuti druge Android aplikacije. Nakon korisničkog odabira vanjske aplikacije vrijede pravila privatnosti te aplikacije.

Android backup i cleartext traffic isključeni su u aktualnom manifestu, a capture foreground service nije exported.

## Browser ekstenzije

Chrome, Edge, Opera i Firefox paketi obrađuju screenshot piksele lokalno. Ne sadrže first-party network uploader, analytics SDK, telemetry SDK, ad SDK ni remote runtime script.

Aktualni permission set je točno:

- `activeTab` — pristup aktivnom tabu koji je korisnik odabrao kada pokrene capture;
- `scripting` — injection lokalnog capture skripta za region/full-page workflow;
- `downloads` — pokretanje PNG download workflowa za visible/region capture;
- `storage` — lokalne postavke i ograničeno capture-session stanje.

Ekstenzije ne traže `<all_urls>` ni široki `host_permissions` grant. Zbog toga privilegirane browser stranice mogu ostati nedostupne za capture.

Full-page capture privremeno drži tile image objekte u content-script kontekstu, lokalno sastavlja finalnu sliku i oslobađa privremeno stanje kroz cleanup/watchdog put.

Browser-specifična policy dokumentacija za store pripremu nalazi se u [Browser Extension Privacy Policy](../../ekstenzije/PRIVACY.md).

## Podaci koji nisu potrebni za core capture

Implementirani proizvod za osnovni screenshot workflow ne zahtijeva SNAPVERE račun, payment identitet, advertising ID, kontakte, lokaciju, mikrofon, kameru ni first-party cloud storage.

## Vanjske usluge i linkovi

Aplikacija/dokumentacija može otvoriti `snapvere.com`, Brendigo, GitHub release ili korisničku email/browser/share aplikaciju. Posjet ili slanje podataka vanjskoj usluzi izlazi iz lokalne screenshot-processing granice i podliježe pravilima te usluge.

## Transparentnost potpisa i releasea

Javni Android v0.1.1 APK koristi CI/debug potpis i ne predstavlja se kao Google Play production-signed paket. Browser ZIP-ovi objavljeni su na GitHubu, ali se ne predstavljaju kao odobreni store listing dok stvarni vanjski publisher review/signing nije proveden.

## Sigurnosne prijave

Sumnju na ranjivost nemoj javno objavljivati prije čitanja [SECURITY.md](../../SECURITY.md). Za standardnu podršku koristi **info@snapvere.com**.
