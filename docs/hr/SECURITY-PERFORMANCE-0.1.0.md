# SNAPVERE 0.1.0 — sigurnost i performanse

Ovaj dokument bilježi kontrole i očekivane dokaze za SNAPVERE 0.1.0 Windows + Android release liniju. Ne tvrdi da bilo koju aplikaciju možemo dokazati potpuno bez grešaka ili ranjivosti.

## Local-first granica

Windows capture/editor obrada ostaje lokalna. Desktop runtime nema first-party HTTP/socket klijent, WebView/WebView2 ni JavaScript execution path. Android namjerno nema `android.permission.INTERNET`, ima isključen cleartext i backup te zadržava MediaProjection servis kao non-exported.

Web, support i legal poveznice pokreće korisnik i delegiraju se OS handlerima; ne stvaraju background SNAPVERE mrežni klijent.

## Windows hardening zadržan u 0.1.0

- NuGet audit pokriva direktne i tranzitivne pakete od `low` severity nadalje.
- `NU1901`–`NU1904` su build failure.
- Nullable/analyzer warning ostaje greška.
- Normalni CI checkout ne zadržava repository credentials.
- Build/release Actions pinani su na pune commit SHA vrijednosti.
- Setup/Portable path i extraction zaštite ostaju aktivne.
- Portable cache prije izvršavanja provjerava architecture-specific trusted SHA-256 manifest ugrađen u host.
- Missing, modified, unexpected ili reparse-point sadržaj uzrokuje transactional rebuild i ponovnu validaciju.
- Capture zapis koristi privremenu datoteku i atomic move; sekundarna cleanup greška ne smije zamijeniti izvorni capture rezultat.
- Capture History i preference putovi obrađuju `SecurityException`/policy failure na ograničenom filesystemu.

Aplikacija se ne predstavlja kao sandbox protiv proizvoljnog zlonamjernog koda koji već radi kao isti Windows korisnik.

## Android lifecycle hardening

0.1.0 dodatno učvršćuje single-capture MediaProjection sesiju:

- notification/foreground-service initialization failure završava kontroliranim capture failureom;
- provjerava se Handler scheduling za task-hide i first-frame rad;
- task-hide ostaje ograničen na pet sekundi;
- first-frame delivery ostaje ograničen na sedam sekundi;
- `ImageReader.acquireLatestImage()` ostaje zaštićen;
- dohvaćeni Image i Bitmap deterministički se oslobađaju prije završnog cleanupa;
- MediaProjection, callback, VirtualDisplay, ImageReader i HandlerThread cleanup ostaje exception-safe po resursu;
- globalni capture ownership oslobađa samo servisna instanca koja ga posjeduje;
- conversion/provider i allocation failure ostaju unutar capture teardowna.

## Android buffer sigurnost

Capture reader koristi `RGBA_8888`. Prije bitmap kopiranja SNAPVERE provjerava vidljivu širinu, pixel stride od točno 4 bajta, pozitivan i dovoljno velik row stride, whole-pixel row padding, overflow-safe aritmetiku, vraćen ByteBuffer položaj te dovoljan broj bajtova za `rowStride × height`.

JVM regresijski testovi pokrivaju tight/padded row, neispravne dimenzije, neočekivani pixel stride, djelomični padding i overflow.

## Android privacy ugovor

Automatizirana validacija pada ako se dogodi bilo koja regresija:

- pojavi se `android.permission.INTERNET`;
- uključi se cleartext;
- uključi se backup;
- ukloni se MediaProjection foreground-service permission/type;
- `CaptureService` postane exported;
- versionName/versionCode više nisu 0.1.0 / 10.

U 0.1.0 Android sourceu nema telemetrije, analytics SDK-a, oglasnog SDK-a, cloud uploadera, WebViewa niti remote-control kanala.

## Android potpisivanje paketa za v0.1.0

Javni `SNAPVERE.apk` namjerno koristi isti CI/debug-potpisani paketni put koji je već potvrđen Android CI-jem. Zbog toga v0.1.0 objava ne zahtijeva privatni production keystore niti repository signing secrete.

To **ne uklanja provjere potpisa**. Prije objave automatizacija i dalje zahtijeva:

- uspješan debug i release Android build;
- `lintDebug`, `lintRelease` i JVM testove;
- neprazan potpisani debug APK;
- uspješnu `apksigner` provjeru;
- ZIP-alignment provjeru;
- SHA-256 provjeru kroz artifact prijenos i završnu objavu.

APK je instalabilan, ali se ne predstavlja kao Google Play/production-potpisani paket. CI/debug signing identitet nije podržani trajni production update lineage. Kasniji Android kanal s drugačijim stabilnim production ključem može zahtijevati deinstalaciju i novu instalaciju.

## Integritet source arhive

`SNAPVERE-Android-Source.zip` nastaje kroz `git archive` iz točno validiranog release commita i njegova `android/` treea. Workflow provjerava očekivane Gradle/manifest/MainActivity putanje te odbija generirani `build/` ili `.gradle/` sadržaj. Signing materijal se ne nalazi u arhivi.

## Immutable release i four-asset ugovor

0.1.0 release workflow pokreće se posebnim `main` triggerom nakon zelenog source CI-a. Prije taga ponovno gradi i validira oba platformna deliverablea.

Immutable `v0.1.0` tag nastaje tek nakon svih pre-publication gateova. Postojeći tag mora se razriješiti na točno validirani commit.

Valjano v0.1.0 izdanje sadrži točno:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE.apk
SNAPVERE-Android-Source.zip
```

Workflow računa SHA-256 za sva četiri asseta i nakon objave uspoređuje GitHub digest s lokalno validiranim vrijednostima. Postojeći release asseti ne prepisuju se.

## Model performansi

SNAPVERE izbjegava nepotreban rezidentni rad dok miruje:

- Windows tray/hotkey hostovi blokiraju na nativnim message loopovima umjesto pollinga;
- capture/D3D resursi stvaraju se za capture rad;
- sekundarni WinUI prozori su on-demand;
- Recent Captures discovery je lokalni i ograničen;
- Portable cache integrity koristi sekvencijalni SHA-256 prolaz prema ugrađenom manifestu;
- Android stvara MediaProjection/ImageReader/VirtualDisplay samo za izričito odobrenu snimku;
- Android nema network worker, telemetry loop niti continuous recorder.

0.1.0 ne smanjuje postojeće lifecycle timeoutove ni visual-regression pragove radi prolaska CI-a.

## Automatizirani dokaz

### Windows

Release/CI gateovi uključuju audited restore, x64 build/test, x86 build, ARM64 cross-build, payload-integrity provjere, renderirani WinUI QA u normalnom CI putu, universal Setup/Portable packaging i x64/x86 lifecycle/tray-first probeove.

### Android

CI pokreće debug/release lint, JVM testove i debug/release build plus debug APK signature/alignment/digest. Release job dodatno provjerava javni CI/debug-potpisani APK, Android source arhivu i transfer hashove.

### Finalna objava

Objava se prihvaća tek nakon valjanih platformnih gateova i uspješne post-publication digest provjere sva četiri javna asseta.

## Granice dokaza

- ARM64 Windows na hosted x64 runneru je cross-build/package dokaz, ne fizički ARM64 runtime test.
- Android CI/release automatizacija nije iscrpni fizički test svakog OEM skina, zaslona, memory-pressure stanja ili permission managera.
- Javni 0.1.0 APK je CI/debug-potpisan, a ne production/Play signing tvrdnja.
- Sigurnosne kontrole smanjuju poznate klase rizika, ali nisu univerzalno jamstvo protiv nepoznatih ranjivosti.
