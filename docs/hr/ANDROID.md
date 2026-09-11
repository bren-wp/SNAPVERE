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
9. Prvi dovršeni frame pretvara se iz RGBA image plane podataka u obrezani ARGB bitmap i sprema kao PNG kroz MediaStore.
10. URI i naziv zadnje snimke čuvaju se u privatnom SharedPreferences spremištu radi Open/Share/Delete radnji.
11. Projection/display/reader/thread resursi se oslobađaju i foreground service se zaustavlja.

Ograničeni timeout od pet sekundi služi samo kao zaštita od greške. Nikad sam ne pokreće snimanje.

## Konkurentnost i gašenje resursa

`CaptureService` koristi procesni single-active-capture guard. Drugi slučajni start ne može preklopiti postojeću MediaProjection sesiju.

Servis koristi atomsko stanje dovršetka i početka snimanja. Ako Android uništi servis dok capture work još traje, cleanup se po mogućnosti serializira kroz capture handler. Vlasništvo sesije oslobađa se tek nakon cleanupa kako nova sesija ne bi preklopila gašenje prethodne.

Deterministički se oslobađaju:

- MediaProjection callback
- MediaProjection
- VirtualDisplay
- ImageReader
- handler callbackovi
- HandlerThread
- foreground notification/service ownership

## Spremanje

Snimke se kroz Android MediaStore spremaju kao `image/png` u:

```text
Pictures/SNAPVERE
```

Aplikacija ne traži široka storage dopuštenja. Provjerava se završetak MediaStore pending stanja; ako finalizacija ne uspije, nepotpuna stavka se briše.

UI zadnje snimke provjerava je li spremljeni URI još čitljiv prije nego omogući Open, Share ili Delete. Zastarjeli URI uklanja se iz privatnih postavki umjesto da ostanu neispravni gumbi.

## Tamni dizajn sustav

Android distribucija sada dijeli ključnu SNAPVERE dark paletu s Windows distribucijom umjesto zasebnog vizualnog identiteta. Canonical desktop tokeni definirani su u `src/Snapvere.App/App.xaml`, a Android ih preslikava u `android/app/src/main/res/values/colors.xml`.

Zajednički ključni tokeni:

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

Android-specific strong-border, strong-accent, warning i destructive tokeni nadograđuju tu osnovu bez mijenjanja zajedničkog identiteta proizvoda.

`MainActivity` koristi resource tokene umjesto hardkodirane nepovezane palete. UI hijerarhija:

- zaglavlje s SNAPVERE ikonom, taglineom i Android/local bedžom
- naglašena Capture kartica
- accessibility-aware status površina
- Latest Capture kartica s validiranim Otvori / Podijeli / Izbriši radnjama
- Private by Design kartica
- About/support/legal kartica
- footer s verzijom i platformom

Stranica je vertikalno pomična i poštuje system-bar insete. Telefoni koriste kompaktniji horizontalni razmak, a tablet-class layout koristi 48 dp horizontalnog paddinga. Interaktivni gumbi imaju minimalnu visinu 52 dp, nativni ripple feedback i eksplicitno disabled stanje bez dodavanja teškog UI frameworka.

## Radnje nad zadnjom snimkom

### Otvori

MediaStore URI predaje se Android pregledniku slika putem `ACTION_VIEW` uz privremeno read dopuštenje.

### Podijeli

PNG se predaje Android share sheetu putem `ACTION_SEND` uz privremeno read dopuštenje. Dijeljenje uvijek pokreće korisnik.

### Izbriši

Prikazuje se nativni dijalog za potvrdu. Aplikacija briše samo spremljeni URI zadnje SNAPVERE snimke. Ako URI više ne postoji, zastarjela lokalna referenca se čisti.

## Web, podrška i pravne poveznice

Odredišta se otvaraju samo nakon korisničke radnje:

- web: `https://snapvere.com`
- podrška: `mailto:info@snapvere.com`
- privatnost: `https://snapvere.com/privacy`
- uvjeti: `https://snapvere.com/terms`

Aplikacija ih ne dohvaća unaprijed i nema `INTERNET` dopuštenje. Android HTTP(S) poveznice predaje vanjskom pregledniku. Ako nema mail aplikacije za `mailto:`, adresa podrške kopira se u lokalni međuspremnik i korisniku se prikazuje vidljiva poruka.

## Lokalizacija

Engleski je zadani resource set. Hrvatski je dostupan u `values-hr`. Android koristi standardni fallback kada uređaj koristi drugi locale.

## Sigurnosni ugovor manifesta

CI pada ako Android manifest dobije `android.permission.INTERNET`. CI dodatno provjerava:

- deklaraciju `FOREGROUND_SERVICE_MEDIA_PROJECTION`
- da `CaptureService` ostaje `android:exported="false"`
- da `CaptureService` ostaje `android:foregroundServiceType="mediaProjection"`
- da je cleartext promet onemogućen
- da je app backup onemogućen

U ovom Android milestoneu nema telemetrije, analytics SDK-a, oglasnog SDK-a, cloud upload klijenta, WebViewa ni remote-command kanala.

## GitHub Actions dokaz

`.github/workflows/android-ci.yml` je izvor istine za build. Za svaki Android PR i Android promjenu na `main` CI:

1. provjerava manifest privacy/service ugovor
2. postavlja JDK 17 i Gradle 8.11.1
3. provjerava Android SDK 36 / Build Tools 35.0.0
4. pokreće `clean lintDebug assembleDebug assembleRelease`
5. lint warninge tretira kao greške
6. provjerava debug APK s `apksigner`
7. provjerava alignment s `zipalign`
8. izračunava SHA-256
9. prenosi APK i digest kao 30-dnevni Actions artifact

Naziv artifacta:

```text
snapvere-android-apk-<commit-sha>
```

Sadržaj artifacta:

```text
SNAPVERE-Android-0.0.9-debug.apk
SNAPVERE-Android-0.0.9-debug.apk.sha256
```

Generirani APK-ovi namjerno se ne commitaju u source tree kako promjena izvornog koda ne bi ostavila zastarjeli binarij pokraj novog sourcea.

## Potpisivanje

CI APK je debug-potpisan za razvojnu/internu distribuciju i može se instalirati radi testiranja. Ne predstavlja se kao produkcijski Play Store/release-signed paket. Produkcijski potpis zahtijeva zasebno upravljan privatni release key; signing secret se ne smije commitati u repozitorij.

## Granice dokaza

Zeleni Android CI dokazuje kompilaciju sourcea, Android lint, debug/release build, debug APK potpis, alignment i stvaranje artifacta. Sam po sebi ne dokazuje interakciju na fizičkom uređaju za svaki OEM/Android. Device/emulator runtime QA navodi se odvojeno samo kada je stvarno izveden.

## Granica platformske jednakosti

Android aplikacija je dovršena za implementirani full-screen MediaProjection workflow. Android nema isti top-level-window capture primitive koji SNAPVERE koristi na Windowsu, zato se Windows-style Window Capture ne prikazuje kao implementiran na Androidu. Region selection i annotation jednakost ostaju buduće funkcije dok se stvarno ne implementiraju i device-testiraju.
