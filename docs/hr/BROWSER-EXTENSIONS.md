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

## Lokalizacija

English je default/fallback. Svaka distributivna browser mapa sadrži zasebne English i Hrvatski locale kataloge.

## Granica QA tvrdnji

`extensions-ci.yml` daje syntax, manifest, permission, locale, source-policy i package validaciju. Zeleni CI nije tvrdnja da su sve četiri ekstenzije ručno testirane u stvarnom browser GUI-ju.

Za development load, pakiranje i poznata ograničenja vidi [`ekstenzije/README.md`](../../ekstenzije/README.md).
