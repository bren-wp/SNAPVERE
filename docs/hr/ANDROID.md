# SNAPVERE Android — arhitektura, UI i QA

## Ugovor proizvoda

SNAPVERE za Android je nativna local-first aplikacija za snimanje zaslona. Ne traži korisnički račun i ne prenosi piksele snimki. Svako snimanje počinje izričitom radnjom korisnika i novim Android MediaProjection dopuštenjem.

Aktualna release linija: **0.1.1** (`versionCode 11`). Podržana osnova:

- Android 10+ / API 29+;
- compileSdk / targetSdk 36;
- Java 17;
- Android Gradle Plugin 8.10.1;
- Gradle 8.11.1.

## Korisnički tijek

1. Korisnik otvara SNAPVERE.
2. Tamna početna površina prikazuje spremnost i zadnju čitljivu lokalnu snimku.
3. Korisnik dodiruje **Snimi zaslon**.
4. Android prikazuje MediaProjection sustavski dijalog.
5. Nakon dopuštenja SNAPVERE pokreće `mediaProjection` foreground servis dok je Activity još u prvom planu.
6. SNAPVERE poziva `moveTaskToBack(true)`.
7. `MainActivity.onStop()` potvrđuje da aplikacija više nije vidljiva.
8. Tek tada servis stvara VirtualDisplay i ImageReader.
9. Prvi frame validira se, pretvara iz RGBA ravnine u ARGB bitmap i sprema kao PNG kroz MediaStore.
10. URI/naziv zadnje snimke čuva se u privatnom SharedPreferences spremištu za Otvori/Podijeli/Izbriši.
11. Image, projection, display, reader, callback i thread resursi se oslobađaju i foreground servis staje.

Pet-sekundni task-hide timeout i sedam-sekundni first-frame timeout ograničavaju zaglavljene capture putove. Nijedan timeout ne pokreće novu snimku niti zaobilazi Android dopuštenje.

Android 14+ traži novo dopuštenje za svaku MediaProjection sesiju. SNAPVERE ne cacheira/reusea consent token i registrira `MediaProjection.Callback.onStop()` za kontrolirani teardown.

## Lifecycle hardening zadržan u 0.1.1

0.1.1 zadržava učvršćeni capture/service model uveden u 0.1.0:

- notification/service initialization failure pretvara se u kontrolirani capture failure;
- Handler scheduling failure ne može beskonačno zadržati capture ownership;
- MediaProjection, VirtualDisplay i frame acquisition failure završavaju lokaliziranom recovery porukom;
- `ImageReader.acquireLatestImage()` ostaje zaštićen;
- dohvaćeni `Image` zatvara se prije konačnog session cleanupa;
- bitmap conversion sadrži allocation/provider probleme, uključujući `OutOfMemoryError`;
- samo vlasnik servisne sesije može osloboditi process-local capture ownership;
- cleanup je idempotentan i svaki Android resurs oslobađa se neovisno.

## Validacija capture buffera

ImageReader koristi `PixelFormat.RGBA_8888`. Prije bitmap alokacije/kopiranja SNAPVERE provjerava:

- pozitivnu vidljivu širinu;
- točno 4-bajtni RGBA pixel stride;
- pozitivan row stride dovoljno velik za vidljivi red;
- full-pixel poravnanje row paddinga;
- overflow-safe aritmetiku padded širine/reda;
- `ByteBuffer.rewind()` prije kopiranja;
- najmanje `rowStride × height` dostupnih bajtova.

`CaptureBufferLayout` JVM testovi pokrivaju tight/padded redove, neispravne dimenzije, neočekivani pixel stride, partial padding i overflow.

## Spremanje i UI

Snimke se kroz MediaStore spremaju kao `image/png` u:

```text
Pictures/SNAPVERE
```

Nema širokog storage permissiona. Pending-state finalizacija provjerava se, a cleanup problem ne može sakriti izvornu save grešku.

UI zadnje snimke ponovno provjerava URI prije Otvori, Podijeli ili Izbriši. Stale metapodaci uklanjaju se umjesto prikaza neispravnih akcija.

UI koristi SNAPVERE tamnu paletu, isključuje OEM `forceDark`, poštuje system-bar insete i ostaje scrollable. Parovi akcija slažu se vertikalno na uskim ekranima ili pri font scaleu 1,25x+. Gumbi zadržavaju najmanje 52 dp touch height.

## Privacy/security ugovor manifesta

CI/release pada ako manifest doda `android.permission.INTERNET`. Dodatno zahtijeva:

- `FOREGROUND_SERVICE_MEDIA_PROJECTION`;
- `CaptureService` s `android:exported="false"`;
- `CaptureService` s `android:foregroundServiceType="mediaProjection"`;
- isključen cleartext;
- isključen app backup;
- `versionName 0.1.1` / `versionCode 11`.

Nema telemetrije, analytics SDK-a, oglasnog SDK-a, cloud-upload klijenta, WebViewa ni remote-command kanala.

## Razvojni CI APK

`.github/workflows/android-ci.yml` pokreće:

```text
clean lintDebug lintRelease testDebugUnitTest assembleDebug assembleRelease
```

Lint warning je greška. CI provjerava privacy/service/version ugovor, SDK 36 / Build Tools, debug APK potpis, ZIP alignment i SHA-256 te prenosi verzionirani 0.1.1 debug APK unutar `snapvere-android-ci-apk-<commit-sha>`.

## Javni Android paket za 0.1.1

Javni `SNAPVERE.apk` koristi validirani CI/debug-potpisani paketni put. Release **ne tvrdi** da koristi privatni production keystore ili Google Play signing identitet.

Release workflow zahtijeva:

- privacy/service/version validaciju;
- `lintDebug` i `lintRelease`;
- JVM unit testove;
- uspješan debug i release build;
- potpisan, neprazan debug APK;
- `apksigner` provjeru;
- ZIP alignment;
- SHA-256 provjeru kroz Actions transfer i GitHub objavu.

APK je instalabilan, ali njegov signing identitet nije trajni production upgrade ugovor. Budući kanal s drugim stabilnim production ključem može zahtijevati deinstalaciju i novu instalaciju.

Iz točnog validiranog Git treea workflow stvara i:

```text
SNAPVERE-Android-Source.zip
```

Arhiva sadrži tracked `android/` source/configuration i isključuje build output, Gradle cache i signing materijal.

## Kombinirani v0.1.1 public asset ugovor

Android doprinosi dvije datoteke u osmo-assetni v0.1.1 release:

```text
SNAPVERE.apk
SNAPVERE-Android-Source.zip
```

Isti GitHub Release sadrži Windows Setup/Portable te Chrome/Edge/Opera/Firefox ZIP-ove. SHA-256 provjerava se tijekom artifact transfera, ponovno računa za finalne assete i uspoređuje s GitHub objavljenim digestima.

Povijesni v0.1.0 ostaje nepromijenjen sa svojim originalnim četvero-assetnim ugovorom.

## Granice dokaza

Zeleni Android CI dokazuje kompilaciju, lint, JVM testove, debug/release buildove, debug APK potpis/alignment i generiranje artefakta. Zeleni v0.1.1 release dodatno dokazuje strukturu source arhive, cross-job digest transfer i finalne GitHub asset digest provjere.

To nije iscrpni fizički test svakog OEM uređaja, Android skina, rezolucije ili permission implementacije.

## Granica platformske jednakosti

Android 0.1.1 je dovršen za implementirani full-screen MediaProjection workflow. Windows-style top-level Window Capture i desktop Region annotation editor ne predstavljaju se kao Android funkcije dok zasebno ne budu implementirane i device-tested.
