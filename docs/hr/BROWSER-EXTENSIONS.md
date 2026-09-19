# SNAPVERE browser ekstenzije

Podržani su Chrome, Edge, Opera i Firefox. Dostupne su radnje za vidljivo područje, odabrano područje i ograničeno snimanje cijele stranice.

Ekstenzije traže samo `activeTab`, `scripting`, `downloads`, `downloads.open` i `storage`, bez širokog host pristupa. `downloads.open` koristi se isključivo nakon korisničkog klika na **Otvori** za dovršenu SNAPVERE snimku u Nedavnim snimkama. Brand je zaključan na SNAPVERE.

Kod cijele stranice svaki dekodirani tile odmah se crta u jedan ograničeni canvas i zatim oslobađa, čime se smanjuje vršno korištenje memorije. Promjena aktivne kartice ili fokusiranog browser prozora prekida snimanje kako se rad ne bi nastavio nad pogrešnim ciljem. Ownership se ponovno provjerava prema početnom ID-u kartice/prozora i zadnje fokusiranom browser prozoru prije Region/Full Page script injectiona te prije page-mutating pripreme, scrollanja, skrivanja floating elemenata i assembly granica; Region overlay nastao neposredno prije promjene ownershipa uklanja se token-scoped cleanup porukom.

## Ownership poruka

Background runtime odvaja korisničke capture naredbe od callbackova aktivne capture sesije. `CAPTURE_VISIBLE`, `CAPTURE_REGION` i `CAPTURE_FULL` prihvaćaju se samo iz SNAPVERE extension stranica, a ne iz injektiranog content scripta kartice. Callbackovi region sesije prihvaćaju se samo od vlasničke kartice i dodatno moraju odgovarati capture tokenu, ID-u kartice i ID-u prozora. Odgovori prema extension UI-ju koriste ograničene error ključeve umjesto raw internih exception poruka.

## Tipkovnički prečaci

Zadani browser prečaci su `Ctrl+Shift+1` (područje), `Ctrl+Shift+2` (vidljivo područje) i `Ctrl+Shift+3` (cijela stranica). Na macOS-u zadane kombinacije su `Command+Shift+1`, `Command+Shift+2` i `Command+Shift+7`; cijela stranica namjerno izbjegava sistemski rezervirani `Shift+Command+3` screenshot prečac. Korisnik ih može promijeniti kroz browser sučelje za prečace ekstenzija.
