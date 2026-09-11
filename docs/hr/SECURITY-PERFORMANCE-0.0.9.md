# SNAPVERE 0.0.9 — sigurnost, stabilnost i performanse

Ovaj dokument bilježi hardening za v0.0.9 na Windowsu i Androidu te granice onoga što repozitorij i CI stvarno dokazuju.

## Sigurnosna površina

Statički pregled v0.0.9 desktop koda nije pronašao WebView/WebView2, HTML renderer, izvršavanje JavaScripta niti first-party HTTP/socket klijent. Klasični browser XSS zato nije primjenjiva napadna površina u ovoj verziji. To nije tvrdnja da je bilo koji softver bez mogućih propusta.

Vanjska web i mail odredišta definirana su u proizvodu i otvaraju se samo nakon izričite radnje korisnika. Samo pokretanje SNAPVERE-a ili otvaranje About prozora ne preuzima te destinacije unaprijed.

Android aplikacija također nema first-party HTTP klijent i namjerno ne deklarira `android.permission.INTERNET`. Website, Support, Privacy i Terms akcije delegiraju se Android browser/mail handlerima nakon korisničke radnje. SNAPVERE v0.0.9 nema remote command kanal, automatski updater, cloud-upload klijent, telemetry klijent niti mrežni licensing klijent.

## Dependency i build-chain hardening

- NuGet audit je izričito uključen za direktne i tranzitivne pakete.
- Audit počinje od `low` severity razine.
- `NU1901`, `NU1902`, `NU1903` i `NU1904` su build greške pa poznata ranjivost blokira CI i release.
- GitHub Actions pinani su na pune commit SHA vrijednosti umjesto mutable major tagova.
- Obični CI checkout ne zadržava repository credentials.
- Dependabot tjedno prati NuGet i GitHub Actions ovisnosti.
- Android CI pokreće debug i release lint s warnings-as-errors, JVM unit testove, debug/release build, APK signature provjeru, ZIP alignment i SHA-256 artifacta.
- Android CI pada ako se doda `INTERNET` permission ili oslabi non-exported MediaProjection foreground-service/privacy ugovor.

Ove mjere smanjuju dependency i CI supply-chain rizik, ali nisu zamjena za Authenticode potpis finalnih Windows binarija niti za privatno upravljani produkcijski Android signing key.

## Package i filesystem hardening

Embedded ZIP ekstrakcija i dalje odbija apsolutne putanje i parent traversal, canonicalizira ciljnu putanju, ograničava broj zapisa i ukupnu raspakiranu veličinu, odbija duplicirana odredišta te zapisuje kroz staging datoteku prije zamjene.

v0.0.9 koristi ograničeno staging ime:

```text
.snapvere-<guid>.tmp
```

Odredišni basename više se ne proširuje dodatnim staging suffixom, pa valjano dugo NTFS ime ne postaje nevaljano zbog internog privremenog imena.

Portable host više ne smatra user-writable `%TEMP%` extraction cache pouzdanim samo zato što postoje `.ready` i `Snapvere.exe`. CI generira integrity manifest za svaku arhitekturu iz točno objavljenog payloada. Embedded manifest bilježi očekivanu relativnu putanju, veličinu i SHA-256 digest svake datoteke. Prije pokretanja cached aplikacije Portable odbija reparse pointove, nestale/neočekivane datoteke i svaki sadržaj čija se veličina ili SHA-256 ne podudara s trusted manifestom. Nevaljan cache ponovno se transakcijski gradi i provjerava prije izvršavanja.

Prva implementacija radila je drugi decompression prolaz kroz veliki embedded ZIP. Stvarni package lifecycle CI pokazao je da to može prijeći postojeći 60-sekundni Portable startup-probe prozor. Timeout nije povećan niti je test oslabljen; uzrok je popravljen tako da provjera radi jedan sekvencijalni SHA-256 prolaz kroz raspakirane datoteke prema embedded manifestu.

Zajednički path-boundary helper pravilno tretira samu zaštićenu mapu i njezine potomke, a odbija parent i sibling putanje koje samo dijele tekstualni prefiks. Setup ga koristi pri zaštiti Windows system putanja i identifikaciji vlastite instalacije.

## Sigurnije spremanje snimki

Windows save-location workflow prvo dovršava PNG na sigurnoj zadanoj lokaciji i tek zatim traži konačno odredište. Odustajanje zato ne gubi snimku.

Relokacija koristi destination-local staging, asinkrono kopiranje, završni atomic replacement, PNG-only validaciju i Windows file-identity provjere. Source se briše samo kada Windows pozitivno potvrdi da su source i destination različite datoteke. Alias ili nesigurni identity slučaj zadržava source kao recovery kopiju.

## Windows tray hardening

Nativni notification-area host koristi moderni `NOTIFYICON_VERSION_4` callback ugovor. Nakon svakog uspješnog dodavanja ikone, uključujući Explorer/taskbar recreation, host poziva `NIM_SETVERSION`. `NIF_SHOWTIP` zadržava standardni tooltip pod version-4 protokolom.

Version-4 callback event čita se iz low worda `lParam`. Podržani su keyboard select i keyboard context-menu eventi uz postojeći pointer input. Region Capture debounce ostaje aktivan kako duplicate shell notifikacije ne bi pokrenule preklapajuće capture workflowe.

## Android capture lifecycle hardening

Svaka Android snimka ostaje eksplicitno korisnički odobrena kroz novu MediaProjection sesiju. Consent podaci se ne cacheiraju niti ponovno koriste.

Dvije ograničene failure zaštite sprječavaju beskonačni ownership:

- pet sekundi za Activity → background handoff;
- sedam sekundi za isporuku prvog framea nakon stvaranja VirtualDisplaya.

Ako Android ili graphics driver ne isporuči frame, foreground capture service završava s vidljivom pogreškom i čisti sesiju umjesto da capture ostane trajno aktivan.

Cleanup je idempotentan i odvija se po resursu. Timeouti, handler callbackovi, VirtualDisplay, ImageReader, MediaProjection callback, MediaProjection, dohvaćeni Image i HandlerThread čiste se neovisno. RuntimeException jednog platformskog cleanup poziva ne sprječava čišćenje kasnijih resursa niti oslobađanje globalnog capture ownershipa.

`ImageReader.acquireLatestImage()` je zaštićen. Svaki dohvaćeni `Image` zatvara se i kada bitmap konverzija ili spremanje ne uspije. MediaStore cleanup nepotpunog PNG-a je best-effort i ne smije sakriti izvornu persistence grešku.

Prije padded-bitmap alokacije `CaptureBufferLayout` provjerava width, pixel stride, minimalni broj bajtova reda, row stride i integer overflow. JVM testovi pokrivaju tight row, padded row, nevaljane dimenzije/stride i overflow aritmetiku.

## Android UI/UX robusnost

Capture, Otvori, Podijeli, Izbriši, Web, Podrška, Privatnost i Uvjeti imaju eksplicitni failure handling za nedostupne system servise, stale MediaStore URI-je, nedostajuće intent handlere i provider greške. Problem se prikazuje na lokalnoj status površini umjesto da pobjegne kao Activity-level crash.

Parovi akcija automatski se slažu vertikalno na uskim ekranima ili pri font scaleu 1,25x+. Aplikacija ostaje scrollable, poštuje system-bar insete, zadržava minimalnu visinu akcija od 52 dp i isključuje OEM `forceDark` jer SNAPVERE već ima namjerno dizajniranu dark paletu.

## Performanse i idle resursi

Windows tray i global-hotkey host koriste blokirajući Win32 `GetMessage` loop bez periodičnog app pollinga. Capture i Direct3D resursi stvaraju se za stvarni capture rad, ne samo zato što aplikacija miruje u trayu. Sekundarni prozori stvaraju se na zahtjev.

Recent-capture enumeracija ostaje ograničena i on-demand. v0.0.9 uklanja nepotreban eksplicitni metadata refresh za svaki PNG i tolerira očekivane directory access/disappearance race situacije bez file watchera, baze ili resident cachea.

Portable cache provjera dodaje foreground rad pri pokretanju jer se cached payload datoteke SHA-256 provjeravaju prije izvršavanja. Validator ih čita sekvencijalno i ne dekomprimira ponovno embedded ZIP samo radi derivacije očekivanih bajtova. Ne objavljuje se fiksno launch vrijeme; lifecycle CI ostaje regression gate.

Android nema idle capture worker. MediaProjection, ImageReader i capture-thread resursi postoje samo tijekom korisnički odobrene sesije, a bounded timeouti sprječavaju da stalled capture postane neželjeni background rad.

Projekt namjerno ne objavljuje izmišljene RAM ili CPU postotke. Working set i CPU ovise o Windows verziji, DPI-ju, broju monitora, driveru, Android uređaju/OEM ponašanju i aktivnoj capture/editor sesiji.

## Integritet visual QA-a

Prije v0.0.9 otkriven je provenance race: hosted runner desktop mogao je biti snimljen umjesto SNAPVERE Region overlaya i zatim postati successful-main baseline. v0.0.9 hardena probe tako da je fallback vezan uz native window contract SNAPVERE overlaya, transient render timing se retrya, a manifest zapisuje provenance metadata.

Visual thresholdi se ne spuštaju kako bi se problem sakrio. Prije finalnog prihvaćanja releasea mora postojati čist `main` baseline.

## Integritet release automatizacije

Prvi v0.0.9 publication run prošao je source validaciju, buildove, testove, payload/integrity-manifest provjere, šest rendered UI površina, exact two-file packaging i x64/x86 lifecycle provjere, ali je stao prije objave. Uzrok je bio tag lookup koji je očekivao da će `gh api` za nepostojeći ref baciti PowerShell exception. `gh api` je zapravo vratio nonzero exit code i HTTP 404 tekst, što je stari kod pogrešno protumačio kao postojeći ali prazan ref.

Ispravljeni workflow je fail-closed:

- `gh` exit code se provjerava izričito;
- samo stvarni `HTTP 404` znači “tag ne postoji”;
- svaka druga lookup greška prekida objavu;
- postojeći tag/ref mora imati upotrebljiv SHA;
- annotated-tag resolution ima vlastitu API exit-code provjeru;
- stvaranje annotated taga i tag refa provjerava exit code i vraćeni SHA prije nastavka.

Prvi neuspjeli pokušaj nije objavio GitHub Release. Definitivni release mora se ponovno pokrenuti iz finalno validiranog sourcea nakon ovog popravka.

## Release validacija

v0.0.9 Windows release smije se objaviti tek kada sam release source prođe:

- audited dependency restore;
- x64 build i unit testove;
- x86 build;
- ARM64 cross-build;
- native payload + integrity-manifest validaciju;
- šest stvarno renderiranih UI površina;
- exact two-file universal package contract;
- x64 i x86 Setup/Portable lifecycle i tray-first provjere;
- immutable tag provjeru/stvaranje;
- objavu točno `SNAPVERE-Setup.exe` i `SNAPVERE-Portable.exe`;
- post-publication provjeru GitHub asset digestova.

Android source promjene zasebno moraju proći manifest privacy/service ugovor, debug/release lint, JVM testove, debug/release build, debug APK signature, ZIP alignment i SHA-256 artifacta za točnu finalnu source reviziju.

ARM64 provjera na hosted x64 runneru ostaje cross-build/package dokaz, ne stvarni ARM64 hardware runtime. Zeleni Android CI je build/lint/unit/package dokaz, a ne tvrdnja o fizičkom runtime testu svakog OEM/device spoja.

## Preostala trust granica

v0.0.9 Windows binarije ne opisujemo kao Authenticode-signed dok stvarni signing certifikat i verification korak ne postoje. SHA-256 digest potvrđuje identitet bajtova, ali samostalno ne autentificira publishera.

Android Actions APK je debug-potpisan i ne predstavlja se kao produkcijski Play Store/release-signed paket. Produkcijski signing zahtijeva zasebno upravljani privatni ključ koji se ne smije commitati u repozitorij.

SNAPVERE nije sandbox protiv proizvoljnog zlonamjernog koda koji već radi kao isti OS korisnik. Odgovornost proizvoda je ne uvoditi vlastiti dodatni remote execution put, nesigurnu package ekstrakciju, destruktivnu path grešku, stuck capture ownership ili skriveni mrežni kanal.
