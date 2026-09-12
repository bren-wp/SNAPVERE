# Browser ekstenzije

SNAPVERE sadrži post-v0.1.0 razvoj browser ekstenzija za Chrome, Edge, Operu i Firefox u mapi [`ekstenzije/`](../../ekstenzije/).

## Arhitektura

Chromium varijante koriste Manifest V3 i `background.service_worker`. Firefox ostaje Manifest V3 WebExtension, ali koristi Firefox-kompatibilni `background.scripts`. Sve varijante dijele isti capture kod, popup/options UI, lokalizacijske ključeve i local-first model privatnosti.

Browser paket nema vanjske runtime ovisnosti.

### Vidljivo područje

Background kontekst zaključava jednu capture operaciju, snima viewport aktivnog taba browser screenshot API-jem i lokalno sprema PNG preko downloads API-ja.

### Odabrano područje

Prije umetanja lokalnog region selectora background zapisuje capture token, tab/window identitet i vrijeme početka u `storage.local`. Time region workflow ne ovisi samo o RAM stanju MV3 service workera dok korisnik bira područje.

Content helper uklanja overlay prije stvarne snimke. Viewport se zatim lokalno reže u canvasu. Escape prekida postupak, a pointer/keyboard listeneri i svi overlay DOM elementi čiste se u završnim putovima.

### Cijela stranica

Full-page capture:

1. sprema početnu scroll poziciju;
2. mjeri dokument i viewport;
3. otkriva vidljive fixed/sticky elemente;
4. računa ograničenu mrežu viewport tileova;
5. kontrolirano scrolla uz čekanje rendera;
6. snima i prenosi svaki tile content helperu;
7. nakon prvog tilea skriva floating elemente kako se ne bi ponavljali;
8. slaže tileove u ograničeni završni canvas;
9. lokalno preuzima PNG;
10. vraća floating elemente i početnu scroll poziciju.

Sigurnosni limiti prekidaju snimanje kontroliranom greškom ako stranica prelazi maksimalnu dimenziju canvasa, broj piksela ili broj tileova.

Content-side watchdog vraća stanje stranice ako orkestracija neočekivano nestane.

## Sigurnost

Prihvaćaju se samo izričito definirani tipovi poruka. Kod završetka region capturea provjeravaju se token i identitet taba/prozora. Tekst se ne umeće preko `innerHTML`; statusi i lokalizirane poruke koriste `textContent`.

Nema `<all_urls>` dozvole niti pokušaja zaobilaženja browser-protected stranica.

## Privatnost

Nema telemetrije, analyticsa, oglasnog SDK-a, cloud uploada, automatskog slanja snimki ni background mrežnog klijenta. Postavke i aktivni capture metapodaci ostaju u lokalnoj browser pohrani.

Verzionirana javna politika privatnosti browser ekstenzija nalazi se u [`ekstenzije/PRIVACY.md`](../../ekstenzije/PRIVACY.md).

## Lokalizacija

English je default/fallback. Svaka distributivna browser mapa sadrži zasebne English i Hrvatski locale kataloge.

## Reproducibilno pakiranje

`ekstenzije/tools/package-extensions.sh` izrađuje četiri ZIP paketa iz zasebno staged izvora. Skripta normalizira timestampove staged datoteka i mapa na prijenosni ZIP epoch, sortira putanje u arhivi te koristi `zip -X` kako bi uklonila nepotrebne host metapodatke. Uz ZIP-ove se generira `SHA256SUMS.txt`.

CI pokreće isti packaging helper dvaput u dva odvojena izlazna direktorija. Svaki odgovarajući ZIP mora biti byte-for-byte identičan, a oba SHA-256 manifesta moraju biti ista prije uploada CI artefakta.

## Kontrola pariteta između browsera

`ekstenzije/tools/verify-extension-parity.mjs` sprječava tiho razilaženje browser varijanti. Provjerava da:

- sve zajedničke runtime datoteke imaju iste putanje i SHA-256 sadržaj u Chromeu, Edgeu, Operi i Firefoxu;
- Chrome, Edge i Opera imaju identične manifeste;
- Firefox se od Chromium manifesta razlikuje samo u očekivanom background/Gecko dijelu;
- verzija ostaje usklađena u sva četiri manifesta;
- PNG ikone imaju točne dimenzije 16/32/48/128 i identične byteove u svim browserima;
- popup/options HTML nema inline script/style blokove, inline event handlere ni udaljene runtime resurse.

Ove provjere nadopunjuju postojeći validator permissiona, localea, source-policy pravila i manifesta.

## Spremnost za objavu u browser storeovima

`ekstenzije/store/listing.json` je kanonski strojno čitljiv store ugovor. Usklađuje EN/HR listing tekst, izjavu o jedinoj namjeni ekstenzije, obrazloženja permissiona, local-first privacy/data-practice izjave, točne nazive ZIP paketa i reference na store vizuale za Chrome Web Store, Microsoft Edge Add-ons, Opera Add-ons i Mozilla Add-ons.

`ekstenzije/store/reviewer-notes.md` vanjskom revieweru daje determinističan funkcionalni testni postupak te dokumentira očekivano ponašanje na zaštićenim browser stranicama, upotrebu permissiona, mrežno/privacy ponašanje, full-page limite i Firefox granicu vezanu uz izvorni kod i signing. `ekstenzije/store/README.md` opisuje završne ručne korake objave i povezuje službenu dokumentaciju publisher portala.

`ekstenzije/tools/generate-store-assets.py` deterministički generira tri listing screenshota 1280×800, dva Opera screenshota 612×408, mali promo tile 440×280 i veliki/marquee promo tile 1400×560 iz SNAPVERE tamno-ljubičastog UI modela. Generirani PNG-ovi su CI artefakti, a ne commitani binarni izvori.

`ekstenzije/tools/validate-store-readiness.mjs` provjerava store materijal prema stvarnim manifestima ekstenzija. Između ostalog zahtijeva usklađenu verziju, točan allow-list od četiri permissiona, izostanak širokih host permissiona, local-first privacy zastavice, potpune EN/HR metapodatke, točne nazive paketa, obavezne asset putanje i točne PNG dimenzije.

Extension CI zato proizvodi dva odvojena artefakta: četiri deterministička ZIP-a ekstenzija sa SHA-256 manifestom te zaseban `snapvere-browser-store-kit-<sha>` paket s politikom privatnosti, kanonskim listing metapodacima, reviewer bilješkama i generiranim store vizualima.

Sama objava u storeu ostaje vanjska granica. Autentificirani publisher pristup, store-side submission, certifikacija/review i Firefox signing ne mogu se zaključiti iz zelenog repozitorijskog CI-ja i ne smiju se prikazivati kao završeni dok ih odgovarajući store stvarno ne potvrdi.

## Granica QA tvrdnji

`extensions-ci.yml` daje syntax, manifest, permission, locale, cross-browser parity, source-policy, store-readiness, provjeru dimenzija generiranih vizuala, reproducible packaging, SHA-256 i package-content validaciju. Zeleni CI nije tvrdnja da su sve četiri ekstenzije ručno testirane u stvarnom browser GUI-ju ili odobrene u vanjskim storeovima.

Za development load, pakiranje, store-submission materijal i poznata ograničenja vidi [`ekstenzije/README.md`](../../ekstenzije/README.md).
