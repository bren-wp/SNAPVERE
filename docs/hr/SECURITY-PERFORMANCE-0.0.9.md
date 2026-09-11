# SNAPVERE 0.0.9 — sigurnost, stabilnost i performanse

Ovaj dokument bilježi hardening za v0.0.9 i granice onoga što repozitorij i CI stvarno dokazuju.

## Sigurnosna površina

Statički pregled v0.0.9 desktop koda nije pronašao WebView/WebView2, HTML renderer, izvršavanje JavaScripta niti first-party HTTP/socket klijent. Klasični browser XSS zato nije primjenjiva napadna površina u ovoj verziji. To nije tvrdnja da je bilo koji softver bez mogućih propusta.

Vanjska web i mail odredišta u prozoru O programu definirana su u proizvodu i otvaraju se samo nakon izričitog klika korisnika. Samo pokretanje SNAPVERE-a ili otvaranje About prozora ne preuzima te stranice unaprijed.

SNAPVERE v0.0.9 nema remote command kanal, automatski updater, cloud-upload klijent, telemetry klijent niti mrežni licensing klijent.

## Dependency i build-chain hardening

- NuGet audit je izričito uključen za direktne i tranzitivne pakete.
- Audit počinje od `low` severity razine.
- `NU1901`, `NU1902`, `NU1903` i `NU1904` tretiraju se kao build greške pa poznata ranjivost blokira CI i release.
- GitHub Actions u CI-u pinani su na pune commit SHA vrijednosti umjesto mutable major tagova.
- Obični CI checkout ne zadržava repository credentials.
- Dependabot tjedno prati NuGet i GitHub Actions ovisnosti.

Ove mjere smanjuju dependency i CI supply-chain rizik, ali nisu zamjena za Authenticode potpisivanje finalnih Windows binarnih datoteka.

## Package i filesystem hardening

Embedded ZIP ekstrakcija i dalje odbija apsolutne putanje i parent traversal, canonicalizira ciljnu putanju, ograničava broj zapisa i ukupnu raspakiranu veličinu, odbija duplicirana odredišta datoteka te zapisuje kroz staging datoteku prije zamjene.

v0.0.9 koristi ograničeno staging ime:

```text
.snapvere-<guid>.tmp
```

Više se ne produžuje odredišni basename dodatnim suffixom, pa valjano dugo NTFS ime ne postaje nevaljano samo zbog privremenog imena.

Portable host više ne smatra user-writable `%TEMP%` extraction cache pouzdanim samo zato što postoje `.ready` i `Snapvere.exe`. CI prije izgradnje universal Portable hosta generira integrity manifest za svaku arhitekturu iz točno objavljenog payloada. Mali manifest ugrađuje se uz pripadajući payload ZIP i bilježi očekivanu relativnu putanju, veličinu i SHA-256 digest svake datoteke. Prije pokretanja cached aplikacije Portable odbija reparse pointove, nestale ili neočekivane datoteke te svaku datoteku čija se veličina ili SHA-256 ne podudara s trusted embedded manifestom. Nevaljan cache ponovno se transakcijski gradi iz embedded payloada i još jednom provjerava prije izvršavanja.

Prva implementacija uspoređivala je cached datoteke izravno s drugim decompression prolazom kroz veliki embedded ZIP. Stvarni package lifecycle CI pokazao je da takav dizajn na hosted runneru može prijeći postojeći 60-sekundni Portable startup-probe prozor. Zato nije povećan timeout niti oslabljen test: popravljen je uzrok tako da cache provjera radi jedan sekvencijalni hash prolaz kroz raspakirane datoteke i uspoređuje ih s embedded manifestom, bez redundantne ZIP dekompresije.

Zajednički path-boundary helper sada tretira i samu zaštićenu mapu i njezine potomke kao dio granice, a odbija parent i sibling putanje koje samo dijele isti tekstualni prefiks. Setup ga koristi pri zaštiti Windows system direktorija i identifikaciji vlastitih instaliranih datoteka/procesa.

## Sigurnije spremanje snimki

Funkcija odabira mjesta spremanja u 0.0.9 prvo dovrši PNG na sigurnoj zadanoj lokaciji pa tek zatim otvara odabir konačnog odredišta. Odustajanje zato ne gubi snimku.

Relokacija koristi destination-local staging, asinkrono kopiranje, završnu atomsku zamjenu, dopušta samo PNG odredišta i provjerava Windows file identity. Izvor se briše samo kada Windows pozitivno potvrdi da su source i destination različite datoteke. Alias ili nejasni identity slučaj zadržava source kao recovery kopiju.

## UI i UX ispravnost

- Region Capture: Print Screen i `Ctrl+Shift+1`.
- Window Capture: `Ctrl+Shift+2`.
- Full Screen Capture: `Ctrl+Shift+3` u v0.0.9.
- Scrolling Capture nema rezerviran globalni hotkey dok stvarni workflow nije implementiran.
- About prikazuje službenu stranicu, support e-mail, Privacy i Terms odredišta.
- About sadržaj je vertikalno scrollable kako Windows text scaling ne bi sakrio support/legal kontrole.
- Ako Windows nema registriran mail klijent, support akcija može lokalno kopirati `info@snapvere.com` u clipboard.

## Performanse i idle resursi

Tray i global-hotkey host koriste blokirajući Win32 `GetMessage` loop. Aplikacija ne koristi periodični polling timer za te ulaze.

Capture i Direct3D resursi stvaraju se za stvarni capture rad umjesto da stalno ostanu aktivni samo zato što program miruje u trayu. Sekundarni prozori stvaraju se na zahtjev.

Recent-capture enumeracija ostaje ograničena i pokreće se na zahtjev. v0.0.9 uklanja nepotrebni eksplicitni metadata refresh za svaki pronađeni PNG te tolerira očekivane I/O/access race situacije bez file watchera, baze ili resident cachea.

Portable cache provjera dodaje foreground rad pri pokretanju javnog Portable executablea jer se cached payload datoteke SHA-256 provjeravaju prije izvršavanja. Validator ih čita sekvencijalno i ne dekomprimira ponovno embedded ZIP samo radi ponovnog dobivanja očekivanih bajtova. Ne objavljuje se fiksna tvrdnja o vremenu pokretanja; package lifecycle CI ostaje regression gate, a stvarno launch vrijeme i dalje ovisi o storageu i endpoint-security uvjetima.

Projekt namjerno ne objavljuje izmišljene RAM ili CPU postotke. Working set i CPU ovise o Windows verziji, DPI-ju, broju monitora, driverima i aktivnoj capture/editor sesiji. Performance ugovor je arhitekturni: nema nepotrebnog periodičnog idle loopa, telemetry workera, language/network workera niti stalno aktivnog capture GPU pipelinea.

## Integritet visual QA-a

Prije v0.0.9 otkriven je provenance race: hosted runner desktop mogao je biti snimljen umjesto SNAPVERE Region overlaya i zatim postati successful-main baseline. v0.0.9 hardena probe tako da je fallback vezan uz native window contract SNAPVERE overlaya, transient render timing se retrya, a manifest zapisuje provenance metadata.

Visual thresholdi se ne spuštaju kako bi se problem sakrio. Prije prihvaćanja v0.0.9 mora postojati čist `main` baseline.

## Release validacija

v0.0.9 smije se objaviti tek kada sam release source prođe:

- audited dependency restore;
- x64 build i unit testove;
- x86 build;
- ARM64 cross-build;
- validaciju native payloada;
- stvarni rendered UI capture;
- točan two-file universal package contract;
- x64 i x86 Setup/Portable lifecycle i tray-first provjere;
- exact immutable release-tag provjeru;
- post-publication provjeru da GitHub Release sadrži samo dva očekivana executablea.

ARM64 provjera na hosted x64 runneru ostaje cross-build/package dokaz, a ne tvrdnja o stvarnom ARM64 hardware runtime testu.

## Preostala trust granica

v0.0.9 se ne opisuje kao Authenticode-signed dok stvarni signing certifikat i verification korak ne postoje. SHA-256 digest potvrđuje identitet bajtova, ali samostalno ne potvrđuje publishera.

SNAPVERE također nije sandbox protiv proizvoljnog zlonamjernog koda koji već radi kao isti Windows korisnik. Odgovornost proizvoda je ne uvoditi vlastiti dodatni remote execution put, nesigurnu package ekstrakciju, destruktivnu path grešku ili skriveni mrežni kanal.
