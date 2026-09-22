# SNAPVERE Image Pipeline

Windows image pipeline odvaja capture acquisition, obradu piksela, anotacije, PNG encode i trajno objavljivanje datoteke. Zajednički `CaptureFrame` koristi BGRA8 piksele, eksplicitne dimenzije i stride te capture vrijeme i source identity.

## CaptureFrame granica

Svaki capture producer vraća `CaptureFrame`. Prije daljnje obrade frame se validira kako bi dimenzije, stride i duljina pixel buffera bili međusobno konzistentni. Workflows ponovno validiraju frame na važnim granicama umjesto da pretpostave da je native backend ili transformirani rezultat automatski ispravan.

Time crop, anotacije, encode i spremanje ostaju neovisni o konkretnom capture backendu.

## Crop

`CaptureFrameCropper` normalizira traženi pravokutnik, odbija praznu regiju i zahtijeva da cijeli crop ostane unutar izvornog framea. Destination stride računa se kao `width × 4` uz checked arithmetic.

Vidljivi BGRA redovi kopiraju se u tightly packed destination buffer. Novi frame zadržava izvorni capture timestamp i source identifier, dok dobiva novu veličinu, stride i pixel podatke odabrane regije.

## Anotacije

Windows Region editor podržava Pen, Line, Arrow, Box i Highlight nad odabranim frameom. Annotation state pripada lokalnom editing workflowu; acquisition sloj ne šalje piksele u cloud niti ih obrađuje udaljeni servis.

Tijekom crtanja izbjegava se nepotrebno kloniranje trenutne kolekcije točaka na svakom pointer-move događaju. Završno renderiranje radi nad validiranom geometrijom framea kako bi koordinate ostale unutar slike.

## PNG encode

`PngCaptureEncoder` iz BGRA framea zapisuje RGBA PNG stream. Piše PNG signature te `IHDR`, komprimirani `IDAT` i `IEND` chunkove s CRC-32 vrijednostima.

BGRA→RGBA pretvorba i DEFLATE kompresija CPU-bound su poslovi, zato se taj dio izvršava izvan WinUI threada preko `Task.Run`. Postojeći backing buffer `MemoryStream` objekta koristi se kroz `GetBuffer()` kako se ne bi stvarala dodatna puna kopija komprimiranog PNG buffera prije `IDAT` zapisa.

Svaki red prolazi kroz validirani frame contract, pretvara se u jedan ponovno korišten scanline i komprimira s `CompressionLevel.Fastest`. Cancellation se provjerava tijekom obrade redova i ponovno prije zapisa komprimiranih podataka u destination stream.

## Trajno lokalno spremanje

`CaptureFileWriter` određuje lokalni capture direktorij i u istom direktoriju izrađuje jedinstvenu privremenu datoteku. Encoder piše u temp file asinkronim I/O putem, a stream se flush-a prije završne objave.

Završni `File.Move` je commit boundary. Vidljivi naziv sada se dodjeljuje upravo na toj granici: ako druga snimka ili lokalni proces prije zauzmu isti naziv temeljen na timestampu, SNAPVERE prelazi na sljedeći deterministički sufiks i ponovno pokušava atomski move bez ponovnog PNG encodea. Cancellation se provjerava prije svakog pokušaja objave.

Budući da se temp i final file nalaze u istom direktoriju, dizajn ne prikazuje namjerno nedovršeni encode pod konačnim capture nazivom. Neuspjeli encode, flush, cancellation ili publication koriste best-effort cleanup, a greška cleanup-a ne smije zamijeniti izvornu capture grešku.

## Memorija i odzivnost

Screenshot obrada nužno koristi memoriju razmjerno dimenzijama slike. Veliki multi-monitor frameovi i velike anotirane regije zato mogu trošiti značajnu količinu memorije i kada sustav radi ispravno.

Aktualni hardening uključuje direktan BGRA zapis u Region/Window overlay bitmape, ranije oslobađanje zamrznutih Window Capture frameova, uklanjanje dodatnog PNG `ToArray()` copyja u clipboard putu te kompresiju izvan UI threada.

[Performanse i stabilnost](PERFORMANCE.md) opisuju stvarnu evidence granicu; ovi zahvati nisu fiksno jamstvo potrošnje RAM-a.

## Regresijski dokaz

Unit suite provjerava crop granice i kopiranje redova, annotation rendering, PNG dimenzije i dekodirane RGBA piksele, uspješnu atomsku objavu PNG-a, dvije istodobne snimke s istim timestampom bez kolizije naziva, cancellation bez finalne datoteke te cleanup oko zajedničkog writera.

Windows CI te testove izvršava na x64 test putu, uz odvojene x86 i ARM64 buildove. Package CI dodatno provjerava size budgete javnih executablea.

Povezano: [Architecture](../ARCHITECTURE.md), [Korisnički vodič](USER-GUIDE.md), [Window Capture](WINDOW-CAPTURE.md), [Performanse i stabilnost](PERFORMANCE.md), [QA matrica](QA-MATRIX.md).
