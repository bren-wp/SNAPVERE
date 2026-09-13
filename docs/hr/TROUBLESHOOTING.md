# SNAPVERE rješavanje problema

Ovaj vodič vrijedi za aktualnu proizvodnu liniju **SNAPVERE 0.1.1**. Kreni od odjeljka za platformu/paket koji koristiš.

## Prije dijagnostike

1. Provjeri da je paket preuzet sa službenog v0.1.1 izdanja: https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.1
2. Provjeri odgovara li naziv paketa tvojoj platformi.
3. Jednom ponovno pokreni aplikaciju ili browser prije promjene sistemskih postavki.
4. Nemoj isključivati sigurnosne mehanizme OS-a ili browsera samo da bi capture radio.

## Windows

### Nakon pokretanja se ne otvara prozor

To je normalno tray-first ponašanje. Provjeri Windows notification area i overflow. SNAPVERE nije zamišljen kao aplikacija sa stalno otvorenim launcher prozorom.

Ako se proces potpuno zatvori, provjeri:

```text
%LOCALAPPDATA%\SNAPVERE\Logs\startup.log
```

Startup log je namijenjen dijagnostičkom stanju, ne pikselima screenshotova.

### Print Screen ili hotkey ne radi

Druga aplikacija može zauzeti isti globalni hotkey. Pokreni istu capture akciju iz tray izbornika. Ako akcija iz izbornika radi, a hotkey ne, ukloni konflikt u drugoj aplikaciji ili promijeni SNAPVERE postavke.

### Određeni prozor nije moguće snimiti

Neki prozori su zaštićeni, minimizirani, koriste ograničene overlaye ili nestanu tijekom selekcije. Dovedi ciljni prozor u foreground i ostavi ga vidljivim. SNAPVERE ne tvrdi da može zaobići zaštitu sadržaja.

### Problem na monitorima s različitim DPI/scaling postavkama

SNAPVERE ima physical-pixel/DPI i virtual-desktop obradu, ali rijetke kombinacije drivera i skaliranja i dalje mogu otkriti platform-specific problem. Ponovi test s ciljnim monitorom kao primary i zapiši scaling svih monitora za support prijavu.

### Windows upozorava na Setup/Portable

Projekt ne tvrdi da javni Windows izvršni paketi imaju komercijalni Authenticode reputation/signing identitet. Provjeri da je datoteka preuzeta sa službenog GitHub releasea i usporedi SHA-256 digest koji prikazuje GitHub. Nemoj globalno gasiti Defender ili SmartScreen.

### Portable cache/extraction problem

Zatvori sve SNAPVERE procese i ponovno pokreni Portable. Host validira ugrađeni architecture payload i po potrebi transakcijski obnavlja cache. Ako problem ostane, pošalji startup log te Windows build i arhitekturu.

## Android

### Nakon capture gumba nema slike

Svaka snimka traži novo Android MediaProjection odobrenje. Ako se sustavski dijalog odbije ili zatvori, capture se ne izvršava. Pokreni novi capture i odobri zahtjev.

### Aplikacija javlja da se nije mogla sakriti

Capture servis namjerno čeka da SNAPVERE task više nije vidljiv kako aplikacija ne bi snimila samu sebe. Ako se task ne može pomaknuti iza prethodnog sadržaja, capture se otkazuje umjesto spremanja SNAPVERE ekrana.

### Capture timeout

Postoje ograničeni task-hide i first-frame timeouti. OEM grafika, display način ili sustavske restrikcije mogu spriječiti dolazak valjanog framea. Ponovi s mirnim foreground ekranom. Ako je problem ponovljiv, navedi model uređaja, Android verziju i display način.

### Otvori / Podijeli / Izbriši je onemogućeno

Akcije zahtijevaju da zadnji spremljeni MediaStore URI i dalje bude čitljiv. Ako je Android ili druga aplikacija uklonila sliku, SNAPVERE uklanja zastarjelu latest-capture referencu.

### APK se ne može instalirati preko drugog builda

Javni v0.1.1 APK koristi validirani CI/debug potpis. Paket s drugim signing identitetom nije moguće instalirati kao in-place update. Deinstalacija uklanja aplikacijski paket/postavke; screenshotovi koji su već spremljeni u sustavnoj Pictures kolekciji zasebne su MediaStore datoteke.

### Mreža i privatnost

Android manifest namjerno nema `android.permission.INTERNET`. Website, Support, Privacy, Terms, Open i Share korisnički su pokrenute akcije koje Android prosljeđuje odabranoj aplikaciji.

## Browser ekstenzije

### Nije moguće snimiti browser-internal stranicu

Browseri ograničavaju injection/capture na internim i privilegiranim stranicama kao što su settings, extension storeovi i neki ugrađeni preglednici. SNAPVERE vraća kontroliranu unsupported-page grešku i ne traži široki host access da bi zaobišao ta pravila.

### Full-page kaže da je stranica prevelika

Ekstenzija namjerno ograničava broj tileova, canvas dimenzije i ukupan broj piksela radi sprječavanja nekontrolirane potrošnje memorije. Koristi manju region ili visible snimku.

### Full-page duplicira ili preskoči sticky/dinamični sadržaj

Full-page radi kontrolirani scrolling i stitching. Animacije, lazy loading, video, canvas, sticky elementi i cross-origin frameovi mogu se mijenjati tijekom capturea. Stabiliziraj stranicu kada je moguće ili koristi visible/region capture.

### “Ask where to save” ne utječe na full-page

U 0.1.1 ta opcija namjerno vrijedi za visible i region capture. Full-page stitching završava unutar content-script konteksta i lokalno pokreće preuzimanje generiranog Bloba.

### Region selection se ne pokreće

Injection je blokiran na privilegiranim stranicama. Na običnoj web stranici napravi reload i pokušaj ponovno. Reload je koristan i ako je ekstenzija ažurirana dok je stranica već bila otvorena.

### Upozorenje o ručnoj instalaciji

GitHub browser ZIP paketi nisu dokaz da su ekstenzije odobrene u Chrome Web Storeu, Edge Add-onsu, Opera Add-onsu ili Mozilla Add-onsu. Dok vanjski storeovi stvarno ne budu objavljeni, koristi dokumentirani developer-mode/manual install put ako želiš testirati pakete.

## Što poslati podršci

- SNAPVERE verziju (`0.1.1` za aktualni javni release);
- platformu, OS/browser verziju i arhitekturu;
- točan capture način;
- točan error/status tekst;
- korake za reprodukciju;
- ponavlja li se nakon restarta;
- Windows startup log kada je relevantan;
- broj monitora i scaling za desktop probleme;
- Android model i verziju za Android problem;
- vrstu stranice (obična ili privilegirana) za browser problem.

Nemoj slati osjetljive screenshotove osim ako su stvarno potrebni i svjesno ih odlučiš podijeliti.

Podrška: **info@snapvere.com**
