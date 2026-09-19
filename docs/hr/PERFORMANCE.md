# SNAPVERE Performanse i stabilnost

SNAPVERE je optimiziran za kratke capture operacije umjesto za stalno aktivan desktop proces. Fokus je zato na niskom idle trošku, manjem peak RAM-u tijekom snimanja, predvidljivom cleanupu i regresijskim provjerama.

## Windows memorija

Region Capture zapisuje zamrznute BGRA retke izravno u WinUI bitmap bez dodatne pune packed-frame kopije. Clipboard PNG put koristi postojeći komprimirani buffer, a draft anotacije više ne moraju stvarati novu kopiju cijelog popisa točaka za svaki pointer-move događaj.

Window Capture koristi isti direct-bitmap pristup za svaki monitor overlay. Nakon što je zamrznuta slika učitana, raw `CaptureFrame` referenca se oslobađa. Picker također uklanja vlastite reference nakon prijenosa ownershipa overlayima.

Packed 3840×2160 BGRA frame zauzima približno 31.6 MiB, pa uklanjanje jedne redundantne pune kopije po monitoru značajno smanjuje vršnu managed memoriju na multi-monitor sustavima.

## Capture pipeline

- PNG kompresija se izvodi izvan WinUI threada;
- datoteke se zapisuju kroz staging prije završnog premještanja;
- dimenzije, stride i duljina buffera validiraju se prije obrade;
- multi-monitor geometrija koristi nativne bounds vrijednosti i DPI pretvorbu;
- browser full-page capture ima eksplicitne limite i oslobađa tile resurse nakon crtanja;
- browser capture ponovno provjerava vlasništvo aktivnog taba prije završetka.

## Idle ponašanje

Windows aplikacija je tray-first. Popis nedavnih snimki čita se kada korisnik otvori odgovarajuće sučelje, bez stalnog filesystem watchera. QA polling petlje koriste se samo u CI probe putovima, ne u normalnom radu aplikacije.

## Stabilnost

SNAPVERE ima kontrolirane failure putove za capture, filesystem, tray, hotkey, packaging i UI-host operacije. Cilj nije obećanje da platforma nikada neće pogriješiti, nego ograničen rad, eksplicitni cleanup i jasna regresijska evidencija.

## Regresijski gateovi

CI za 0.1.2 provjerava x64 build i unit testove, x86 i ARM64 buildove, renderirane WinUI površine, visual baseline usporedbu, universal Setup/Portable, package-size budget, x64/x86 lifecycle, browser runtime/permission/parity provjere, reproducibilno pakiranje, Product Contract CI i CodeQL.

Povezano: [QA matrica](QA-MATRIX.md), [Status proizvoda](PRODUCT-STATUS.md), [Engleska arhitektura](../ARCHITECTURE.md) i [Multi-monitor](../MULTI-MONITOR.md).
