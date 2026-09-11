# SNAPVERE Android — arhitektura, UI i QA

## Ugovor proizvoda

SNAPVERE za Android je nativna, lokalno usmjerena aplikacija za snimanje zaslona. Ne traži korisnički račun i ne prenosi piksele snimki na mrežu. Svako snimanje započinje izričitom radnjom korisnika i novim Android MediaProjection dijalogom za dopuštenje.

Podržana osnova:

- Android 10+ / API 29+
- compileSdk / targetSdk 36
- Java 17
- Android Gradle Plugin 8.10.1
- Gradle 8.11.1

## Korisnički tijek

1. Korisnik otvara SNAPVERE.
2. Tamna početna površina prikazuje stanje spremnosti i zadnju lokalnu snimku, ako je i dalje čitljiva.
3. Korisnik dodiruje **Snimi zaslon**.
4. Android prikazuje sustavski MediaProjection dijalog.
5. Nakon dopuštenja SNAPVERE pokreće mediaProjection foreground service dok je Activity još u prvom planu.
6. SNAPVERE poziva `moveTaskToBack(true)`.
7. `MainActivity.onStop()` potvrđuje da SNAPVERE više nije vidljiv.
8. Tek tada servis stvara VirtualDisplay i ImageReader.
9. Prvi dovršeni frame se validira, pretvara iz RGBA image plane podataka u obrezani ARGB bitmap i sprema kao PNG kroz MediaStore.
10. URI i naziv zadnje snimke čuvaju se u privatnom SharedPreferences spremištu radi Open/Share/Delete radnji.
11. Projection/display/reader/thread resursi se oslobađaju i foreground service se zaustavlja.

Ograničeni timeout od pet sekundi sprječava da handoff ostane beskonačno aktivan. Nakon stvaranja VirtualDisplaya zaseban timeout od sedam sekundi za isporuku framea sprječava da zastoj ImageReadera ili drivera ostavi foreground service i globalni capture lock aktivnima. Nijedan timeout ne pokreće novu snimku niti zaobilazi Android dopuštenje.

Android 14+ zahtijeva novo korisničko dopuštenje za svaku MediaProjection capture sesiju i jedan `createVirtualDisplay()` poziv po MediaProjection instanci. SNAPVERE ne cacheira niti ponovno koristi consent token; svaka snimka dobiva novu projection instancu. Registriran je i `MediaProjection.Callback.onStop()` za kontrolirano oslobađanje resursa kada Android prekine projekciju.

## Konkurentnost i gašenje resursa

`CaptureService` koristi procesni single-active-capture guard. Drugi slučajni start ne može preklopiti postojeću MediaProjection sesiju.

Servis koristi atomsko stanje dovršetka i početka snimanja. Cleanup je idempotentan, a svaki platformski resurs oslobađa se neovisno. Ako vendor/API cleanup poziv baci iznimku, čišćenje preostalih resursa i globalnog capture ownershipa ipak se nastavlja. Neočekivano uništenje servisa bilježi i best-effort šalje lokalni status pogreške.

Deterministički se obrađuju:

- task-hide i frame-delivery timeout callbackovi
- MediaProjection callback
- MediaProjection
- VirtualDisplay
- ImageReader i dohvaćeni Image
- handler callbackovi
- HandlerThread
- foreground notification/service ownership
- procesni capture ownership

`ImageReader.acquireLatestImage()` je zaštićen jer Android može baciti iznimku pri iscrpljenom redu ili određenim producer/format mismatch slučajevima. Svaka uspješno dohvaćena slika zatvara se i kada konverzija ili spremanje ne uspije.

## Validacija capture buffera

Prije alokacije padded bitmapa SNAPVERE provjerava:

- da je vidljiva širina pozitivna;
- da je pixel stride pozitivan;
- da row stride sadrži najmanje potreban broj bajtova za vidljivi red;
- da aritmetika vidljivog reda ne prelijeva `int`;
- da izračun padded širine ne prelijeva.

Čisti `CaptureBufferLayout` helper pokriven je JVM unit testovima za tight row, padded row, neispravne dimenzije/stride i overflow slučajeve.

## Spremanje

Snimke se kroz Android MediaStore spremaju kao `image/png` u:

```text
Pictures/SNAPVERE
```

Aplikacija ne traži široka storage dopuštenja. Provjerava se završetak MediaStore pending stanja; ako finalizacija ne uspije, brisanje nepotpune stavke je best-effort i ne smije sakriti izvornu grešku spremanja.

UI zadnje snimke provjerava je li spremljeni URI još čitljiv prije nego omogući Otvori, Podijeli ili Izbriši. Zastarjeli URI uklanja se iz privatnih postavki. MediaStore/provider pogreške prikazuju se korisniku umjesto da ruše proces.

## Tamni dizajn i responzivni UI

Android distribucija dijeli ključnu SNAPVERE dark paletu s Windows distribucijom. Canonical desktop tokeni definirani su u `src/Snapvere.App/App.xaml`, a Android ih preslikava u `android/app/src/main/res/values/colors.xml`.

| Token | Vrijednost |
| --- | --- |
| Canvas | `#0B0D12` |
| Surface | `#12151C` |
| Raised surface | `#181C25` |
| Border | `#2A3140` |
| Primarni tekst | `#F6F7FB` |
| Sekundarni tekst | `#98A2B3` |
| Muted tekst | `#727C90` |
| Primarni accent | `#7C6CFF` |
| Success | `#45D6A2` |

OEM `forceDark` je izričito isključen kako sustav ne bi ponovno transformirao već dizajniranu tamnu paletu.

UI hijerarhija:

- zaglavlje sa SNAPVERE ikonom, taglineom i Android/local bedžom
- naglašena Capture kartica
- accessibility-aware status površina
- Latest Capture kartica s validiranim Otvori / Podijeli / Izbriši radnjama
- Private by Design kartica
- About/support/legal kartica
- footer s verzijom i platformom

Stranica je vertikalno pomična i poštuje system-bar insete. Telefoni koriste kompaktniji horizontalni razmak, a tablet layout 48 dp horizontalnog paddinga. Parovi akcijskih gumba automatski se slažu vertikalno na uskim ekranima ili kada je Android font scale 1,25x ili veći, čime se izbjegavaju odrezani natpisi i premali touch targeti. Gumbi zadržavaju minimalnu visinu 52 dp, ripple feedback i jasno disabled stanje.

## Pouzdanost svih akcija

- **Snimi zaslon** obrađuje nedostupan MediaProjection servis i launcher failure bez trajno onemogućenog gumba.
- **Otvori** i **Podijeli** ponovno validiraju MediaStore URI neposredno prije predaje Androidu.
- **Izbriši** prvo traži potvrdu, podnosi stale/provider pogreške i ne ruši aplikaciju zbog neispravnog URI-ja.
- **Web**, **Privatnost** i **Uvjeti** delegiraju se vanjskom pregledniku i prikazuju lokalnu pogrešku ako handler nije dostupan.
- **Podrška** prvo pokušava `mailto:`, zatim kopira `info@snapvere.com`; ako ni clipboard nije dostupan, prikazuje se kontrolirana lokalna pogreška.
- Registracija i odjava lokalnog capture status receivera zaštićene su od lifecycle rubnih slučajeva.

Nijedna od ovih radnji ne dodaje `INTERNET` dopuštenje niti first-party mrežni klijent. Vanjski URL otvara se samo nakon izričite korisničke radnje.

## Lokalizacija

Engleski je zadani resource set. Hrvatski je dostupan u `values-hr`. Novi statusi i recovery poruke održavaju se u oba skupa resursa. Android koristi standardni fallback za druge locale.

## Sigurnosni ugovor manifesta

CI pada ako Android manifest dobije `android.permission.INTERNET`. CI dodatno provjerava:

- deklaraciju `FOREGROUND_SERVICE_MEDIA_PROJECTION`;
- da `CaptureService` ostaje `android:exported="false"`;
- da `CaptureService` ostaje `android:foregroundServiceType="mediaProjection"`;
- da je cleartext promet onemogućen;
- da je app backup onemogućen.

U ovom Android milestoneu nema telemetrije, analytics SDK-a, oglasnog SDK-a, cloud upload klijenta, WebViewa ni remote-command kanala.

## GitHub Actions dokaz

`.github/workflows/android-ci.yml` je izvor istine za build. Za svaki Android PR i Android promjenu na `main` CI:

1. provjerava manifest privacy/service ugovor;
2. postavlja JDK 17 i Gradle 8.11.1;
3. provjerava Android SDK 36 / Build Tools 35.0.0;
4. pokreće `clean lintDebug lintRelease testDebugUnitTest assembleDebug assembleRelease`;
5. lint warninge tretira kao greške;
6. izvršava lokalne JVM unit testove, uključujući capture-buffer validaciju;
7. gradi debug i minificirani/shrunk release variant;
8. provjerava debug APK s `apksigner`;
9. provjerava alignment s `zipalign`;
10. izračunava SHA-256;
11. prenosi APK i digest kao 30-dnevni Actions artifact.

Naziv artifacta:

```text
snapvere-android-apk-<commit-sha>
```

Sadržaj artifacta:

```text
SNAPVERE-Android-0.0.9-debug.apk
SNAPVERE-Android-0.0.9-debug.apk.sha256
```

Generirani APK-ovi namjerno se ne commitaju u source tree.

## Potpisivanje

CI APK je debug-potpisan za razvojnu/internu distribuciju i može se instalirati radi testiranja. Ne predstavlja se kao produkcijski Play Store/release-signed paket. Produkcijski potpis zahtijeva zasebno upravljan privatni release key; signing secret se ne smije commitati u repozitorij.

## Granice dokaza

Zeleni Android CI dokazuje kompilaciju sourcea, debug/release lint, JVM unit testove, debug/release build, debug APK potpis, alignment i stvaranje artifacta. Sam po sebi ne dokazuje interakciju na fizičkom uređaju za svaki OEM/Android. Device/emulator runtime QA navodi se zasebno samo kada je stvarno izveden.

Finalni PR dokaz mora pripadati finalnoj source reviziji koja se mergea. Zeleni run sa starijom bazom ostaje koristan povijesni dokaz, ali nije završni dokaz za merge.

## Granica platformske jednakosti

Android aplikacija je dovršena za implementirani full-screen MediaProjection workflow. Android nema isti opći top-level-window capture primitive koji SNAPVERE koristi na Windowsu, zato se Windows-style Window Capture ne prikazuje kao implementiran na Androidu. Region selection i annotation jednakost odvojene su funkcije i neće se lažno navoditi kao prisutne dok nisu implementirane i testirane na uređaju.
