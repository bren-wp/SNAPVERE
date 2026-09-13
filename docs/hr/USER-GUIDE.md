# SNAPVERE korisnički vodič

Ovaj vodič opisuje korisničke workflowe u **SNAPVERE 0.1.1** za Windows, Android i četiri browser paketa.

## Odaberi odgovarajući SNAPVERE paket

| Platforma | Najbolje za | Javni paket |
| --- | --- | --- |
| Windows | Brzo tray-first snimanje, uređivanje područja, prozor i zaslon | `SNAPVERE-Setup.exe` ili `SNAPVERE-Portable.exe` |
| Android 10+ | Jednokratna full-screen snimka spremljena u sustavnu Pictures kolekciju | `SNAPVERE.apk` |
| Chrome | Vidljivo područje, cijela web stranica i odabrano područje | `SNAPVERE-Chrome.zip` |
| Edge | Vidljivo područje, cijela web stranica i odabrano područje | `SNAPVERE-Edge.zip` |
| Opera | Vidljivo područje, cijela web stranica i odabrano područje | `SNAPVERE-Opera.zip` |
| Firefox | Vidljivo područje, cijela web stranica i odabrano područje | `SNAPVERE-Firefox.zip` |

Aktualno izdanje: https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.1

## Windows

### Prvo pokretanje

Instaliraj `SNAPVERE-Setup.exe` ili pokreni `SNAPVERE-Portable.exe`. SNAPVERE je tray-first: normalno pokretanje zadržava aplikaciju u Windows notification area umjesto otvaranja stalnog dashboarda.

Ako Windows sakrije tray ikonu, otvori overflow područje i po želji je prikvači.

### Snimanje područja

Koristi **Print Screen**, `Ctrl+Shift+1` ili lijevi klik na tray ikonu. SNAPVERE zamrzava uhvaćeni desktop frame i otvara editor područja.

1. Povuci za odabir željenog područja.
2. Po potrebi pomakni ili promijeni veličinu selekcije.
3. Koristi Pen, Line, Arrow, Box ili Highlight za lokalne anotacije.
4. Po potrebi koristi Undo.
5. Odaberi Copy ili Save.

Gotovi PNG nastaje lokalno. Zadana mapa je `Pictures\SNAPVERE`. Ako koristiš Save As, odustajanje od dijaloga ne briše već dovršenu lokalnu snimku.

### Snimanje prozora

Koristi `Ctrl+Shift+2` ili tray izbornik. SNAPVERE pronalazi odgovarajuće nativne top-level prozore i koristi poseban window-capture workflow, bez tihog pretvaranja u obični desktop crop.

Zaštićeni, minimizirani, hardware-overlay ili na drugi način nedostupni prozori mogu ostati ograničeni pravilima Windowsa ili same aplikacije.

### Snimanje zaslona

Koristi `Ctrl+Shift+3` ili tray izbornik. Preferirani put koristi Windows.Graphics.Capture kada je dostupan, uz fallback za očekivane probleme akvizicije monitora.

### Postavke i povijest

Tray izbornik sadrži postavke, nedavne snimke, odabir jezika, About i Exit. Windows izdanje sadrži 28 ugrađenih jezičnih izbora; engleski je canonical fallback.

### Setup ili Portable

- **Setup** instalira SNAPVERE za standardnu desktop upotrebu i ima validirani uninstall lifecycle.
- **Portable** je jedna javna izvršna datoteka koja sadrži validirane x86, x64 i ARM64 payloadove te bira odgovarajuću arhitekturu pri pokretanju.

## Android 10+

### Snimi zaslon

1. Otvori SNAPVERE.
2. Dodirni **Capture screen**.
3. Odobri Android sustavski MediaProjection dijalog.
4. SNAPVERE pomiče svoj task iza prethodno vidljivog sadržaja.
5. Jednokratna snimka sprema se kroz MediaStore u `Pictures/SNAPVERE`.

Za svaku snimku traži se novo sustavsko odobrenje. SNAPVERE ne cacheira niti potajno ponovno koristi MediaProjection consent token.

### Akcije zadnje snimke

Aplikacija pamti zadnji čitljivi capture URI i nudi:

- **Otvori** — otvara PNG u instaliranom pregledniku slika;
- **Podijeli** — šalje PNG kroz Android chooser;
- **Izbriši** — nakon potvrde briše zadnju snimku.

Ako MediaStore URI postane zastario ili nečitljiv, SNAPVERE uklanja zastarjelu referencu umjesto da je prikazuje kao važeću snimku.

### Privatnost

Android aplikacija nema `INTERNET` permission. Ne sadrži first-party analytics, telemetry, advertising ni cloud-upload worker. Sustavske Share/Open akcije pokreće korisnik i mogu predati podatke aplikaciji koju korisnik sam odabere.

Javni v0.1.1 APK koristi CI/debug potpis. Instalabilan je, ali se ne predstavlja kao Google Play production-signed paket.

## Browser ekstenzije

### Instalacija iz GitHub paketa

Dok vanjski browser storeovi nisu stvarno objavljeni i odobreni, javni ZIP-ovi su source/distribution paketi za ručnu developer-mode instalaciju. Vidi [Instalaciju](INSTALLATION.md) za korake po pregledniku.

### Snimanje vidljivog područja

Otvori SNAPVERE popup i odaberi **Capture visible area**. Trenutni viewport snima se kao PNG i predaje browser download workflowu.

### Snimanje odabranog područja

Odaberi **Select region**, povuci preko željenog dijela stranice i otpusti. **Esc** prekida odabir. Crop se obrađuje lokalno prije preuzimanja.

### Snimanje cijele stranice

Odaberi **Capture full page**. SNAPVERE mjeri dokument, kontrolirano scrolla kroz ograničeni broj pozicija, snima tileove, nakon prvog tilea skriva ograničen broj fixed/sticky elemenata, lokalno sastavlja sliku i vraća stanje stranice.

Zbog sigurnosnih granica odbijaju se stranice koje prelaze tile/canvas/pixel limite. Vrlo dinamične stranice, video, canvas sadržaj i neki cross-origin frameovi mogu dati rezultat drukčiji od statičkog dokumenta.

### Browser postavke

Options stranica omogućuje lokalni filename prefix i opciju “ask where to save” za visible i region capture. Full-page rezultat preuzima se lokalno iz content-script konteksta jer se finalna spojena slika ondje i generira.

## Imenovanje datoteka

Windows koristi `SNAPVERE_yyyy-MM-dd_HHmmss.png` uz numerički nastavak kada je potreban. Android dodaje milisekunde. Browser koristi konfigurirani prefix, vrstu capturea i timestamp.

## Ako nešto ne radi

Kreni od [Rješavanja problema](TROUBLESHOOTING.md). Za sigurnost i privatnost vidi [Privatnost](PRIVACY.md), a za stvarni opseg automatiziranih provjera [QA matricu](QA-MATRIX.md).

Podrška: **info@snapvere.com**
