# SNAPVERE QA matrica

Ova matrica opisuje automatizirane dokaze za proizvodnu liniju **SNAPVERE 0.1.1**. Zeleni workflow je dokaz za ovdje navedene provjere; nije tvrdnja da je ručno testirana svaka kombinacija uređaja, drivera, browser stranice ili Android OEM-a.

| Područje | Automatizirane provjere | Granica dokaza |
| --- | --- | --- |
| Windows source | NuGet audit, analyzers, x64 build, xUnit testovi | Ne zamjenjuje sve stvarne Windows konfiguracije |
| Windows x86 | Restore/build, package payload, Setup/Portable lifecycle | Runtime dokaz je na hosted Windows runneru |
| Windows x64 | Build, testovi, payload, Setup/Portable lifecycle, tray-first probeovi | Hosted runner ne predstavlja svaki driver/display stack |
| Windows ARM64 | Restore/build i payload/package validacija | Cross-build dokaz; nije fizički ARM64 runtime test |
| Windows UI | Šest stvarno renderiranih UI površina, PR-to-main visual comparison | Screenshot comparison nije potpuni accessibility/usability test |
| Packaging | Architecture payload, integrity manifesti, two-file javni ugovor | Ne osigurava komercijalni code-signing reputation |
| Android manifest | Verzija, service/export, privacy i permission ugovor | Ne pokriva sve OEM policy razlike |
| Android source | lintDebug, lintRelease, JVM unit testovi | JVM test ne emulira puni MediaProjection runtime |
| Android APK | Debug/release build, signature, zip alignment | Javni APK je CI/debug-signed, ne Google Play production-signed |
| Android source ZIP | Provjera tracked source arhive i zabrana cache/build outputa | Ne predstavlja app-store objavu |
| Browser manifesti | MV3 ugovor, točni permissioni, bez host_permissions | Browser policy se može promijeniti van repoa |
| Browser source | JavaScript syntax, forbidden-pattern policy, EN/HR locale parity | Statičke provjere nisu potpuna GUI sesija |
| Browser parity | Chromium byte parity, normalizirane Firefox razlike, icon hash/dimenzije | Browser enginei i dalje mogu imati razlike |
| Browser packaging | Dva neovisna prolaza, byte-for-byte usporedba, SHA-256 | Reproducibilnost unutar istog CI okruženja |
| Browser store kit | Listing/privacy/permission metadata i deterministički vizuali | Store approval/signing ostaje vanjski proces |
| Product contract | Windows/Android/browser version parity, docs/release ugovor, Markdown linkovi | Povijesni dokumenti smiju sadržavati stare verzije |
| Release | Točno osam javnih asseta i lokalni SHA-256 | Ne implicira third-party store objavu |
| Nakon objave | GitHub asset names/digests prema lokalno validiranim datotekama | Provjerava integritet GitHub releasea u trenutku objave |

## Windows workflow

Windows CI provjerava aplikaciju kroz x86, x64 i ARM64 payload generiranje te zatim gradi universal Setup/Portable hostove. PR workflow uspoređuje renderirani UI s posljednjim uspješnim `main` baselineom kada je baseline dostupan. Lifecycle testovi provjeravaju extraction/install ponašanje i tray-first ugovor na x64/x86 hosted Windows runnerima.

## Android workflow

Android CI provjerava SDK/tooling, privacy/service/version ugovor, oba lint moda, JVM testove i debug/release build. Public release namjerno kopira validirani debug-signed APK u `SNAPVERE.apk`, provjerava potpis/alignment i zapisuje digest. Ta signing granica se dokumentira, ne skriva.

## Browser workflow

Extension CI provjerava source/manifest policy, parity i store metadata, ponovno generira determinističke store vizuale i dva puta pakira sve četiri varijante. Odgovarajući ZIP-ovi moraju biti byte-for-byte jednaki i proći SHA-256 prije uploada.

## Product Contract CI

`eng/validate-product-contract.py` koristi [`product-version.json`](../../product-version.json) kao aktivni product contract. Provjerava:

- .NET product/assembly/file verziju;
- Android versionCode/versionName/minSdk/targetSdk/compileSdk;
- Android English/Croatian string-key parity;
- Chrome/Edge/Opera/Firefox manifest verziju;
- browser store listing verziju i deklarirani publication status;
- exact v0.1.1 ugovor od osam release asseta;
- aktivne README release linkove i assete;
- aktualne documentation indexe;
- relativne Markdown linkove aktivne dokumentacije.

## Ručna provjera je i dalje vrijedna

Prije budućeg izdanja korisno je stvarno testirati:

- fizički ARM64 Windows uređaj;
- mixed-DPI/multi-monitor i neuobičajene GPU konfiguracije;
- više Android OEM-a/display načina;
- aktualne stabilne verzije sva četiri browsera na reprezentativnim dinamičnim stranicama;
- accessibility tipkovnicom, screen readerom i velikim fontovima;
- vanjske store installation/signing flowove kada publisher računi budu dostupni.

Automatizacija smanjuje regresije; nije opravdanje za tvrdnju o univerzalnoj kompatibilnosti ili nula grešaka.
