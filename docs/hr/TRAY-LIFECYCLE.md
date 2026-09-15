# SNAPVERE 0.1.1 Tray i lifecycle

Windows aplikacija radi kao **tray-first** proces. Normalan launch drži SNAPVERE spremnim za capture bez stalno otvorenog dashboarda, dok globalni prečaci i notification-area host služe kao glavni ulazi u capture workflow.

## Single-instance granica

`SingleInstanceGuard` se pokreće kroz module initializer prije normalnog WinUI startupa. Preuzima named per-user desktop mutex i drži ga tijekom cijelog životnog vijeka procesa.

Ako drugi normalni SNAPVERE proces već drži mutex, novi proces izlazi s uspješnim statusom prije pokretanja tray i hotkey hostova. Ako je prethodni proces pao i ostavio abandoned mutex, novi launch prihvaća vlasništvo koje je `WaitOne` već preuzeo, pa stale stanje ne može trajno blokirati aplikaciju.

Posebni CI automation probe launch-evi izuzeti su iz produkcijskog singleton guarda kako bi visual i lifecycle provjere mogle deterministički pokrenuti tražene površine. Izuzeće vrijedi samo za eksplicitne probe environment varijable i command-line switch-eve koje koristi repozitorijska validacija.

Portable launcher prije skupog payload verification/extraction koraka provjerava isti desktop-instance identity. Child aplikacija i dalje ostaje konačni race-safe vlasnik mutexa.

## Native tray host

`Win32TrayIconService` koristi poseban background thread i message-only Win32 prozor. `Start()` čeka da native inicijalizacija uspije ili vrati startup exception, pa aplikacija ne nastavlja tiho nakon neuspjelog podizanja tray hosta.

Tray ikona koristi `Shell_NotifyIcon` s notification protocol version 4. Left click, double click ili keyboard selection pokreću Region Capture. Right click/context-menu poziv otvara SNAPVERE WinUI tray menu.

Brzi uzastopni Region Capture klikovi debounceaju se. Exception u UI subscriberu zadržava se unutar command dispatcha kako ne bi srušio native tray message loop.

## Explorer recovery i lokalizacija

Tray host registrira `TaskbarCreated`. Nakon ponovnog pokretanja Explorera SNAPVERE pokušava ponovno dodati notification icon. Taj recovery je best effort; neuspjeh ponovnog dodavanja ikone ne bi trebao ugasiti proces, a globalni hotkeys mogu ostati aktivni.

Kada korisnik promijeni SNAPVERE jezik, native loop prima privatnu refresh poruku i osvježava lokalizirani tray tooltip bez restartanja aplikacije.

## Shutdown i cleanup nativnih resursa

`Dispose()` uklanja language event subscription, šalje `WM_CLOSE` message-only prozoru i po potrebi ograničeno čeka završetak tray threada. Uništenje prozora šalje quit message native loopu.

Cleanup uklanja notification icon, uništava generirani icon handle, message-only prozor i privremeno registriranu window class. Native bitmap handleovi korišteni pri izradi SNAPVERE tray ikone oslobađaju se nakon `CreateIconIndirect`.

## Installed i Portable lifecycle dokaz

Tray-first lifecycle test radi nad instaliranom aplikacijom i Portable paketom za x64 i x86.

Za Setup provjerava materializaciju setup UI-a, radi silent install, potvrđuje da installed app postoji, izvršava tray initialization probe te normalni launch koji mora ostati aktivan bez vidljivog glavnog prozora. Zatim pokreće drugu instancu: duplicate mora završiti uspješno, primarni proces mora ostati aktivan i u sustavu smije ostati samo jedan SNAPVERE app proces. Na kraju se uninstall izvršava kroz instalirani Setup executable.

Za Portable se izvršava isti tray probe, pokreće se Portable launcher i zahtijeva se da launcher završi nakon pokretanja tray-only child procesa. Test potvrđuje točno jedan SNAPVERE app proces, odsutnost glavnog prozora i odbijanje drugog Portable launch-a bez stvaranja duplikata.

Puni package lifecycle gate zasebno provjerava universal Setup/Portable contract i zapisuje arhitekturne completion markere tek nakon završetka x64 i x86 lifecycle skripti. CI zahtijeva te markere umjesto zaključivanja uspjeha iz dvosmislenog PowerShell process statea.

## Korisničko ponašanje

Tray-first rad namjerno se razlikuje od aplikacije koja pri svakom launchu otvara dashboard. Capture se pokreće iz traya ili globalnim prečacima; sekundarne površine poput Options, Language i About otvaraju se samo kada ih korisnik zatraži.

CI dokazuje deklarirane startup/lifecycle scenarije na svojim Windows runnerima. To nije jamstvo da Explorer, third-party shell software, security alat ili svaka Windows konfiguracija nikad ne može utjecati na notification-area ponašanje.

Povezano: [Instalacija](INSTALLATION.md), [Postavke](SETTINGS.md), [Performanse i stabilnost](PERFORMANCE.md), [Architecture](../ARCHITECTURE.md), [QA matrica](QA-MATRIX.md).
