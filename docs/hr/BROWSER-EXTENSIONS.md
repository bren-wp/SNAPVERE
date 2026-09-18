# SNAPVERE browser ekstenzije

Podržani su Chrome, Edge, Opera i Firefox. Dostupne su radnje za vidljivo područje, odabrano područje i ograničeno snimanje cijele stranice.

Ekstenzije traže samo `activeTab`, `scripting`, `downloads`, `downloads.open` i `storage`, bez širokog host pristupa. `downloads.open` koristi se isključivo nakon korisničkog klika na **Otvori** za dovršenu SNAPVERE snimku u Nedavnim snimkama. Brand je zaključan na SNAPVERE.

Kod cijele stranice svaki dekodirani tile odmah se crta u jedan ograničeni canvas i zatim oslobađa, čime se smanjuje vršno korištenje memorije. Promjena aktivne kartice prekida snimanje kako se ne bi spremio sadržaj pogrešne kartice.

## Tipkovnički prečaci

Zadani browser prečaci su `Ctrl+Shift+1` (područje), `Ctrl+Shift+2` (vidljivo područje) i `Ctrl+Shift+3` (cijela stranica). Na macOS-u zadane kombinacije su `Command+Shift+1`, `Command+Shift+2` i `Command+Shift+7`; cijela stranica namjerno izbjegava sistemski rezervirani `Shift+Command+3` screenshot prečac. Korisnik ih može promijeniti kroz browser sučelje za prečace ekstenzija.
