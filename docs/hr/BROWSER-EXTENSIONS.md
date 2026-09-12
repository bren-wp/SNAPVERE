# Browser ekstenzije

SNAPVERE 0.1.1 uključuje službene javne browser-extension pakete za Chrome, Edge, Operu i Firefox u mapi [`ekstenzije/`](../../ekstenzije/). Browser kod razvijen je nakon v0.1.0, a v0.1.1 je prvo izdanje koje ga uključuje u javni GitHub Release ugovor.

## Release verzija i asseti

Sva četiri manifesta i kanonski store listing koriste verziju **0.1.1**.

v0.1.1 GitHub Release sadrži:

```text
SNAPVERE-Chrome.zip
SNAPVERE-Edge.zip
SNAPVERE-Opera.zip
SNAPVERE-Firefox.zip
```

Ta četiri browser paketa dio su novog osmo-assetnog v0.1.1 release ugovora. Povijesni v0.1.0 ostaje nepromijenjen sa svojim originalnim Windows/Android assetima.

## Arhitektura

Chromium varijante koriste Manifest V3 i `background.service_worker`. Firefox koristi Manifest V3 s Firefox-kompatibilnim `background.scripts`. Sve varijante dijele isti capture kod, popup/options UI, lokalizacijske ključeve i local-first model privatnosti.

Browser paketi nemaju vanjske runtime ovisnosti.

### Vidljivo područje

Background kontekst zaključava jednu bounded capture operaciju, snima viewport aktivnog taba browser screenshot API-jem i sprema PNG preko downloads API-ja.

### Odabrano područje

Prije umetanja lokalnog region selectora background zapisuje capture token, tab/window identitet i vrijeme početka u `storage.local`, pa workflow ne ovisi samo o RAM stanju MV3 service workera dok korisnik bira područje.

Content helper uklanja overlay prije stvarne snimke. Viewport se lokalno reže u canvasu. Escape prekida postupak, a pointer/keyboard listeneri i overlay DOM elementi čiste se u završnim putovima.

### Cijela stranica

Full-page capture:

1. sprema početnu scroll poziciju;
2. mjeri dokument i viewport;
3. otkriva vidljive fixed/sticky elemente;
4. računa ograničenu mrežu viewport tileova;
5. kontrolirano scrolla uz čekanje rendera;
6. snima i prenosi svaki tile content helperu;
7. nakon prvog tilea skriva floating elemente da se ne ponavljaju;
8. slaže tileove u ograničeni završni canvas;
9. lokalno preuzima PNG;
10. vraća floating elemente i početnu scroll poziciju.

Sigurnosni limiti prekidaju snimanje ako stranica prelazi podržanu canvas dimenziju, broj piksela ili broj tileova. Content-side watchdog vraća stanje stranice ako orkestracija neočekivano nestane.

## Sigurnosne kontrole

Prihvaćaju se samo izričito definirani tipovi poruka. Kod završetka region capturea provjeravaju se token i identitet taba/prozora. Tekst se ne umeće preko `innerHTML`; lokalizirane poruke/statusi koriste `textContent`.

Nema `<all_urls>` niti širokog `host_permissions` unosa i ekstenzija ne pokušava zaobići browser-protected stranice.

## Privatnost

Nema telemetrije, analyticsa, oglasnog SDK-a, cloud uploada, automatskog slanja snimki, remote runtime koda ni background mrežnog klijenta. Postavke i aktivni capture metapodaci ostaju u lokalnoj browser pohrani.

Javna politika privatnosti je [`ekstenzije/PRIVACY.md`](../../ekstenzije/PRIVACY.md).

## Lokalizacija

English je default/fallback. Svaki distributivni browser paket sadrži English i Hrvatski locale katalog.

## Reproducibilno pakiranje

`ekstenzije/tools/package-extensions.sh` izrađuje četiri release ZIP-a iz staged sourcea. Normalizira timestampove datoteka/mapa na prijenosni ZIP epoch, sortira putanje i koristi `zip -X` za uklanjanje nepotrebnih host metapodataka. Uz pakete se generira `SHA256SUMS.txt`.

CI i 0.1.1 release workflow pokreću pakiranje dvaput u odvojenim direktorijima. Odgovarajući ZIP-ovi i checksum manifesti moraju biti byte-for-byte identični prije objave.

## Kontrola pariteta između browsera

`ekstenzije/tools/verify-extension-parity.mjs` sprječava tiho razilaženje provjeravajući:

- identične putanje i SHA-256 sadržaj zajedničkog runtime sourcea u Chromeu, Edgeu, Operi i Firefoxu;
- identične Chrome/Edge/Opera manifeste;
- samo očekivane background/Gecko razlike u Firefoxu;
- usklađene verzije manifesta;
- točne dimenzije i identične byteove ikona;
- izostanak inline script/style blokova, inline event handlera i remote HTML runtime resursa.

Ove provjere nadopunjuju validator permissiona, localea, source-policy pravila i manifesta.

## Spremnost za browser storeove

`ekstenzije/store/listing.json` je kanonski strojno čitljiv store ugovor s EN/HR listing tekstom, single-purpose izjavom, obrazloženjem permissiona, local-first privacy/data-practice izjavama, točnim ZIP imenima i referencama na store assete.

`ekstenzije/store/reviewer-notes.md` daje vanjskim reviewerima deterministične funkcionalne korake i dokumentira protected-page ponašanje, upotrebu permissiona, network/privacy ponašanje, full-page limite i Firefox source/signing granicu.

`ekstenzije/tools/generate-store-assets.py` deterministički generira listing screenshotove i promo tileove. Generirani PNG-ovi ostaju CI artefakti, a ne commitani binarni source.

`ekstenzije/tools/validate-store-readiness.mjs` validira store materijal prema stvarnim manifestima, uključujući verziju, allow-list od četiri permissiona, izostanak širokih host permissiona, local-first privacy zastavice, EN/HR metapodatke, točne nazive paketa, potrebne asset reference i PNG dimenzije.

Sama store objava je vanjski proces. Autentificirani publisher pristup, store-side submission, certifikacija/review i Firefox signing ne mogu se zaključiti iz GitHub releasea ili zelenog repozitorijskog CI-ja.

## Release i QA granica

`.github/workflows/extensions-ci.yml` provjerava syntax, manifest, permission, locale, cross-browser parity, source-policy, store-readiness, generirane assete, determinističko pakiranje, SHA-256 i sadržaj paketa.

`.github/workflows/release-0.1.1.yml` ponavlja browser validaciju i reproducibilno pakiranje na točnom release commitu, prenosi četiri ZIP-a u finalni release job, ponovno računa hashove i nakon objave uspoređuje GitHub digeste.

Zeleni workflow je automatizirani static/package dokaz; nije tvrdnja da je svaki browser build/web aplikacija ručno testirana niti da je vanjski store odobrio ekstenziju.

Za development load, pakiranje, privatnost, store-submission materijal i poznata ograničenja vidi [`ekstenzije/README.md`](../../ekstenzije/README.md).
