# SNAPVERE Android — arhitektura, UI i QA

## Ugovor proizvoda

SNAPVERE za Android je nativna, lokalno usmjerena aplikacija za snimanje zaslona. Ne traži korisnički račun i ne prenosi piksele snimki. Svako snimanje započinje izričitom radnjom korisnika i novim Android MediaProjection dopuštenjem.

Aktualna linija izdanja je **0.1.0** (`versionCode 10`). Podržana osnova:

- Android 10+ / API 29+
- compileSdk / targetSdk 36
- Java 17
- Android Gradle Plugin 8.10.1
- Gradle 8.11.1

## Korisnički tijek

1. Korisnik otvara SNAPVERE.
2. Tamna početna površina prikazuje stanje spremnosti i zadnju čitljivu lokalnu snimku.
3. Korisnik dodiruje **Snimi zaslon**.
4. Android prikazuje sustavski MediaProjection dijalog.
5. Nakon dopuštenja SNAPVERE pokreće `mediaProjection` foreground servis dok je Activity još u prvom planu.
6. SNAPVERE poziva `moveTaskToBack(true)`.
7. `MainActivity.onStop()` potvrđuje da SNAPVERE više nije vidljiv.
8. Tek tada servis stvara VirtualDisplay i ImageReader.
9. Prvi dovršeni frame validira se, pretvara u vidljivi ARGB bitmap i sprema kao PNG kroz MediaStore.
10. URI i naziv zadnje snimke čuvaju se u privatnom SharedPreferences spremištu za Otvori/Podijeli/Izbriši.
11. Image, projection, display, reader, callback i thread resursi oslobađaju se, a foreground servis se zaustavlja.

Timeout od pet sekundi ograničava Activity-to-background handoff, a zaseban timeout od sedam sekundi ograničava isporuku prvog framea nakon VirtualDisplaya. Nijedan timeout ne pokreće novu snimku niti zaobilazi korisničko dopuštenje.

Android 14+ zahtijeva novo dopuštenje za svaku MediaProjection sesiju. SNAPVERE ne cacheira niti ponovno koristi consent token i registrira `MediaProjection.Callback.onStop()` za kontrolirano gašenje.

## Učvršćivanje lifecyclea u 0.1.0

Servis platformske/provider pogreške pretvara u kontrolirani capture failure kad god je to praktično moguće.

- Pogreška inicijalizacije notification channela bilježi se i obrađuje pri startu capturea umjesto namjernog izlijetanja iz `onCreate()`.
- Provjerava se prihvaća li Handler task-hide/frame posao; odbijeni posao ne smije ostaviti capture ownership aktivnim.
- MediaProjection, VirtualDisplay i frame acquisition failure završavaju lokaliziranom recovery porukom.
- `ImageReader.acquireLatestImage()` ostaje zaštićen.
- Dohvaćeni `Image` zatvara se prije konačnog završetka service cleanupa.
- Conversion/provider i allocation problemi, uključujući `OutOfMemoryError`, ostaju unutar kontroliranog capture teardowna.
- Procesni capture guard može osloboditi samo servisna instanca koja ga posjeduje, čime stale teardown ne može očistiti ownership druge aktivne sesije.
- Cleanup je i dalje idempotentan i svaki se Android resurs oslobađa neovisno.

## Validacija capture buffera

ImageReader koristi `PixelFormat.RGBA_8888`. Prije bitmap alokacije i kopiranja SNAPVERE provjerava:

- pozitivnu vidljivu širinu;
- RGBA pixel stride od točno 4 bajta;
- pozitivan row stride dovoljno velik za vidljivi red;
- da je row padding poravnat na cijeli RGBA pixel;
- da aritmetika reda i padded širine ne prelijeva dopušten raspon;
- da se ByteBuffer vrati na početnu poziciju prije kopiranja;
- da buffer stvarno sadrži najmanje `rowStride × height` deklariranih bajtova.

`CaptureBufferLayoutTest` pokriva tight/padded redove, neispravne dimenzije, neočekivani pixel stride, djelomični padding i overflow.

## Spremanje

Snimke se kroz Android MediaStore spremaju kao `image/png` u:

```text
Pictures/SNAPVERE
```

Aplikacija ne traži široko storage dopuštenje. Provjerava se MediaStore pending finalizacija; ako pisanje ili finalizacija ne uspije, cleanup nepotpune stavke je best-effort i ne smije sakriti izvornu pogrešku.

UI zadnje snimke provjerava je li spremljeni URI i dalje čitljiv prije nego omogući Otvori, Podijeli ili Izbriši. Zastarjeli lokalni metapodaci uklanjaju se umjesto prikaza neispravnih akcija.

## Tamni dizajn i responzivni UX

Android dijeli SNAPVERE tamni vizualni identitet s Windows aplikacijom. OEM `forceDark` je isključen jer aplikacija već ima namjerno dizajniranu tamnu paletu.

Početna površina sadrži SNAPVERE identitet, primarnu Capture karticu, accessibility-aware status, Zadnju snimku s Otvori / Podijeli / Izbriši akcijama, Privatno po dizajnu karticu, About/support/legal akcije i footer s verzijom/platformom.

Stranica je vertikalno pomična, poštuje system-bar insete i koristi širi padding na tablet-class širinama. Parovi akcija slažu se vertikalno na uskim ekranima ili kada Android font scale dosegne 1,25x. Gumbi zadržavaju najmanje 52 dp dodirne visine i jasno enabled/disabled stanje.

0.1.0 dodatno usklađuje engleske i hrvatske capture/privacy/recovery poruke tako da korisnik dobije jasnu radnju za oporavak, a ne sirovi provider exception tekst.

## Pouzdanost akcija

- **Snimi zaslon** podnosi nedostupan MediaProjection/service/launcher put bez namjernog trajnog blokiranja primarne akcije.
- **Otvori** i **Podijeli** ponovno provjeravaju MediaStore URI prije delegiranja Androidu.
- **Izbriši** prvo traži potvrdu i obrađuje stale/provider pogreške.
- **Web**, **Privatnost** i **Uvjeti** su isključivo korisnički pokrenuti vanjski intenti s vidljivim failure stanjem.
- **Podrška** prvo koristi `mailto:`, zatim kopiranje `info@snapvere.com`; nedostupan handler/servis ostaje kontroliran.
- registracija/odjava capture-result receivera zaštićena je od lifecycle rubnih slučajeva.

Ove akcije ne dodaju `INTERNET` dopuštenje niti first-party mrežni klijent.

## Sigurnosni ugovor manifesta

CI pada ako se u manifest doda `android.permission.INTERNET`. Dodatno provjerava `FOREGROUND_SERVICE_MEDIA_PROJECTION`, non-exported `CaptureService`, `foregroundServiceType="mediaProjection"`, isključen cleartext, isključen backup te versionName/versionCode 0.1.0.

U ovoj Android liniji nema telemetrije, analytics SDK-a, oglasnog SDK-a, cloud-upload klijenta, WebViewa ni remote-command kanala.

## Razvojni CI APK

`.github/workflows/android-ci.yml` pokreće:

```text
clean lintDebug lintRelease testDebugUnitTest assembleDebug assembleRelease
```

Lint warning je greška. CI provjerava privacy/service/version ugovor, SDK 36 / Build Tools, debug APK potpis i ZIP alignment, računa SHA-256 te prenosi:

```text
SNAPVERE-Android-0.1.0-debug.apk
SNAPVERE-Android-0.1.0-debug.apk.sha256
```

u Actions artifactu `snapvere-android-ci-apk-<commit-sha>`.

## Javni Android paket za 0.1.0

Za v0.1.0 javni `SNAPVERE.apk` namjerno koristi isti CI/debug-potpisani paketni put koji je već potvrđen Android CI-jem. Privatni production keystore i repository signing secreti nisu potrebni.

Release workflow i dalje gradi **debug i release varijantu** te zahtijeva:

- privacy/service/version validaciju;
- `lintDebug` i `lintRelease`;
- JVM unit testove;
- uspješan debug i release build;
- potpisan, neprazan debug APK;
- `apksigner` provjeru;
- ZIP alignment provjeru;
- SHA-256 provjeru kroz Actions prijenos i nakon javne objave.

Provjereni debug-potpisani paket objavljuje se kao:

```text
SNAPVERE.apk
```

APK je instalabilan, ali se **ne predstavlja kao Google Play/production-potpisan paket**. Njegov signing identitet nije podržani dugoročni production upgrade ugovor. Ako kasniji Android kanal koristi drugačiji stabilni production ključ, Android može zahtijevati deinstalaciju i novu instalaciju prije instaliranja drugačije potpisanog paketa.

Iz istog validiranog Git commita stvara se i:

```text
SNAPVERE-Android-Source.zip
```

Source ZIP sadrži samo praćeni `android/` source/configuration. Ne uključuje generirani `build/`, Gradle cache ni signing materijal.

## Javni asset ugovor 0.1.0

Valjano GitHub izdanje sadrži točno:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE.apk
SNAPVERE-Android-Source.zip
```

SHA-256 se provjerava kroz prijenos Android Actions artifacta, zatim ponovno računa za sva četiri finalna asseta i uspoređuje s GitHub digestom nakon objave.

## Granice dokaza

Zeleni Android CI dokazuje kompilaciju, debug/release lint, JVM testove, debug/release build, debug APK potpis/alignment i artifact. Zeleni 0.1.0 release job dodatno dokazuje potpis/alignment javnog CI/debug-potpisanog APK-a, strukturu source arhive, Windows package lifecycle provjere i release-asset digest provjeru. To nije tvrdnja o iscrpnom runtime testu na svakom fizičkom OEM uređaju.

## Granica platformske jednakosti

Android 0.1.0 dovršen je za implementirani full-screen MediaProjection workflow. Android nema isti opći top-level-window capture primitive koji SNAPVERE koristi na Windowsu, stoga se Windows-style Window Capture ne navodi kao Android funkcija. Region-selection/annotation paritet ostaje zasebna buduća mogućnost dok stvarno ne bude implementirana i device-tested.
