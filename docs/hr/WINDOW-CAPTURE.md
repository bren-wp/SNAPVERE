# SNAPVERE 0.1.2 Window Capture

Window Capture na Windowsu odvaja **odabir cilja** od **snimanja ciljnog prozora**. Picker najprije zamrzne prikaz radne površine i poredak prozora koji se mogu snimiti, a tek zatim prikazuje SNAPVERE overlay površine. Odabrani nativni prozor potom se snima kroz window capture servis i sprema kroz zajednički PNG pipeline.

## Otkrivanje prozora

`Win32WindowDiscovery` prolazi top-level prozore u nativnom Z-orderu. Iz popisa se uklanjaju nevidljivi prozori, prozori vlastitog SNAPVERE procesa, DWM-cloaked prozori, prozori bez smislenog naslova i ciljevi bez pozitivne površine.

Granice prozora prvenstveno se čitaju preko `DWMWA_EXTENDED_FRAME_BOUNDS`, uz `GetWindowRect` kao fallback kada DWM granice nisu dostupne. Koordinate su fizički pikseli i mogu biti negativne kada se monitor nalazi lijevo ili iznad primarnog monitora.

Hit-test koristi zamrznuti `WindowDescriptor` popis, umjesto ponovnog ispitivanja desktopa nakon što SNAPVERE postavi vlastite topmost overlaye. Time se odabir ne mijenja samo zato što je picker postao vidljiv.

## Zamrznuti multi-monitor picker

`WindowTargetPicker` najprije dohvaća aktivne monitore i popis prozora. Prije prikaza bilo kojeg overlaya snima po jedan cursor-free zamrznuti frame za svaki monitor.

Za svaki monitor izrađuje se `WindowTargetOverlayWindow`. Promjena hover cilja propagira se na sve overlaye kako bi isti prozor ostao vizualno označen kroz virtualni desktop. Odabir dovršava zajednički picker task, dok cancellation završava bez cilja. Svi overlayi zatvaraju se iz `finally` bloka neovisno o uspjehu, otkazivanju ili grešci.

Overlay preuzima vlasništvo nad svojim zamrznutim frameom. Nakon njihove izrade picker prazni vlastiti dictionary prije prikaza prozora, pa raw BGRA buffer pojedinog monitora može postati dostupan za GC čim ga taj overlay učita u bitmap, umjesto da svi monitor frameovi ostanu zadržani do završetka pickera.

## Snimanje odabranog prozora

Aktualni backend koristi `Windows.Graphics.Capture` (WGC). Window acquisition zahtijeva Windows 10 version 2004 / build 19041 ili noviji i dostupnu WGC podršku. Nulti native handle odbija se prije pokretanja nativnog capture rada.

Za svaki capture SNAPVERE izrađuje BGRA-kompatibilan D3D11 hardware device, `GraphicsCaptureItem` za odabrani `HWND`, free-threaded frame pool s dva buffera i capture session. Cursor se uključuje samo kada ga traži efektivna capture postavka.

Prvi frame mora stići unutar dvije sekunde. Cancellation korisnika ostaje cancellation korisnika, dok se interni first-frame timeout prijavljuje kao kontrolirani `TimeoutException`. Blank WGC frame se odbacuje i ne sprema kao valjana snimka.

Dobiveni frame kopira se u validirani `CaptureFrame` s BGRA pikselima i source metapodacima. Event handleri, WinRT objekti i D3D resursi oslobađaju se na svim izlaznim putovima.

Aktualna 0.1.2 implementacija ne tvrdi postojanje zasebnog legacy window backenda. Platform policy, protected content ili grafički stack mogu spriječiti uspješno snimanje pojedinog prozora.

## Spremanje i cancellation

`WindowCaptureWorkflow` provjerava cancellation prije poziva backenda, validira vraćeni frame i koristi zajednički `CaptureFileWriter`. Efektivna cursor postavka uključena je ako je traži poziv ili spremljena korisnička postavka.

Writer najprije kodira u jedinstvenu privremenu datoteku, radi flush, ponovno provjerava cancellation i tek zatim radi završni atomski `File.Move` na konačni PNG naziv. Neuspjeli ili otkazani capture zato namjerno ne izlaže djelomično zapisan PNG kao gotovu snimku.

Detalji: [Image Pipeline](IMAGE-PIPELINE.md).

## Regresijski dokaz

Unit testovi provjeravaju prosljeđivanje window discovery poziva, descriptor s negativnim koordinatama, PNG spremanje kroz zajednički writer i cancellation prije poziva capture backenda. Posebni hit-testing testovi provjeravaju odabir najvišeg odgovarajućeg prozora.

Windows CI dodatno renderira Window Capture overlay i uspoređuje ga s posljednjim uspješnim `main` visual baselineom. Universal package CI nakon toga provodi installed i Portable lifecycle na x64 i x86.

Ti gateovi smanjuju rizik regresije; nisu obećanje da se svaki third-party prozor, driver ili protected-content scenarij može snimiti.

Povezano: [Architecture](../ARCHITECTURE.md), [Image Pipeline](IMAGE-PIPELINE.md), [Performanse i stabilnost](PERFORMANCE.md), [QA matrica](QA-MATRIX.md).
