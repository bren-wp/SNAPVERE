# SNAPVERE 0.1.1 — sigurnost, privatnost i performanse

Ovaj dokument bilježi automatizirane security/privacy/performance dokaze za SNAPVERE 0.1.1 release liniju. Nadopunjuje, a ne zamjenjuje [`SECURITY.md`](../../SECURITY.md).

## Opseg izdanja

v0.1.1 sadrži Windows, Android i browser-extension pakete. Valjani javni GitHub Release ima točno osam datoteka: dva universal Windows executablea, Android APK i source arhivu te četiri browser ZIP paketa.

Povijesni v0.1.0 asseti ostaju nepromijenjeni i 0.1.1 release proces ih ne prepisuje.

## Windows kontrole

- .NET restore koristi NuGet audit za direktne i tranzitivne ovisnosti od `low` severity nadalje.
- `NU1901`–`NU1904` tretiraju se kao build greške.
- Nullable analiza i .NET analyzer ostaju uključeni, a warnings se tretiraju kao errors.
- Release payloadi grade se zasebno za x86, x64 i ARM64.
- Svaki native payload dobiva SHA-256 integrity manifest.
- Universal Setup i Portable grade se iz validiranih payloada.
- Portable cache provjerava se prema ugrađenom pouzdanom integrity metapodatku prije pokretanja.
- x64 i x86 Setup/Portable lifecycle i tray-first runtime probeovi moraju proći prije kreiranja release taga.
- Normalni CI snima stvarno renderirane Region, Window, Tray, Options, Language i About površine i radi PR visual-regression usporedbu prema zelenom `main` baselineu.

ARM64 ostaje cross-build/package dokaz na hosted x64 runneru i ne predstavlja se kao fizički ARM64 runtime test.

## Android kontrole

- Android verzija je `0.1.1` / `versionCode 11`.
- `android.permission.INTERNET` je zabranjen release ugovorom.
- Cleartext promet i application backup su isključeni.
- `CaptureService` ostaje non-exported i ograničen na `mediaProjection` foreground-service tip.
- Svaka snimka traži novo MediaProjection dopuštenje; token se ne koristi za kontinuirano/background snimanje.
- Release automatizacija pokreće `lintDebug`, `lintRelease`, JVM testove, debug build i release build.
- Javni APK prolazi `apksigner`, ZIP-alignment i SHA-256 provjere.
- Javni v0.1.1 APK koristi validirani CI/debug signing identitet i ne predstavlja se kao Google Play/production-potpisan.
- Android source ZIP generira se iz točnog tracked release treea i provjerava se da nema build/cache sadržaja.

## Browser-extension kontrole

Chrome, Edge, Opera i Firefox paketi koriste verziju 0.1.1 i dijele isti local-first capture runtime osim očekivanih Firefox manifest background/Gecko metapodataka.

Release validacija zahtijeva:

- točne dozvole: `activeTab`, `scripting`, `downloads`, `storage`;
- bez `<all_urls>` i bez širokog `host_permissions`;
- bez remote runtime skripti, telemetry endpointa, `eval` ili `new Function`;
- EN/HR locale parity;
- postojeće runtime assete i točne 16/32/48/128 dimenzije ikona;
- byte-identičan zajednički source kroz browser varijante;
- determinističko ZIP pakiranje u dva neovisna prolaza;
- byte-for-byte reproducibilnost i SHA-256 provjeru;
- čiste pakete bez uobičajenih build/cache/source-map artefakata;
- store metadata/privacy deklaracije usklađene sa stvarnim manifestima;
- determinističko generiranje listing/promo slika.

Pikseli snimke obrađuju se lokalno u ekstenziji. Ekstenzija nema cloud uploader, analytics, oglasni SDK, account sustav niti background mrežni klijent.

## Integritet releasea

v0.1.1 release workflow ne kreira `v0.1.1` tag dok Android, browser i Windows release gateovi ne prođu.

Prije objave zahtijeva točno:

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

SHA-256 računa se za svaki finalni asset. Nakon GitHub Release objave GitHub digest svakog asseta uspoređuje se s lokalno validiranim hashom. Postojeći release asseti ne zamjenjuju se potajno.

## Model performansi

SNAPVERE namjerno izbjegava stalni capture/network polling:

- Windows tray/hotkey obrada je event-driven;
- Direct3D/capture resursi stvaraju se za aktivni rad umjesto stalnog rada;
- Android nema idle screen-recording loop niti background network worker;
- browser-extension capture kod kreće nakon eksplicitne korisničke akcije, a full-page rad ograničen je tile/canvas/pixel limitima.

Ne obećava se fiksni CPU/RAM postotak. Potrošnja ovisi o dimenzijama zaslona/stranice, broju monitora i DPI-ju, grafičkom driveru, browser engineu, Android OEM implementaciji i aktivnom capture/edit workloadu.

## Granica dokaza

Zeleni CI/release dokazuje samo source/build/test/package/signature/digest provjere koje automatizacija stvarno izvršava. To nije jamstvo da softver nema nijednu ranjivost ili grešku niti iscrpni runtime test svakog Windows računala, Android uređaja/OEM-a, browser builda ili web aplikacije.

Vanjska objava/review u browser storeovima nije dio GitHub Release pipelinea i ne smije se zaključiti samo iz uspješne v0.1.1 GitHub objave.
