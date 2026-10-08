# SNAPVERE 0.1.31 privatnost

SNAPVERE je local-first. Osnovna obrada snimki na Windowsu i u browser ekstenzijama ne zahtijeva SNAPVERE račun, automatski cloud servis, first-party analitiku ni capture telemetriju.

## Windows

Pikseli snimke obrađuju se lokalno. PNG/MP4 datoteke spremaju se samo kroz capture workflow koji je pokrenuo korisnik.

Startup dijagnostika je lokalna, ograničena veličinom i rotira se. Prije zapisa exception teksta SNAPVERE redaktira apsolutne Windows/UNC putanje, HTTP/HTTPS/file URI vrijednosti, e-mail adrese i česte credential/token obrasce. Korisničke poruke o grešci ne prikazuju sirovi exception tekst ni lokalne dijagnostičke putanje. Dijagnostika nije namijenjena spremanju piksela snimki, sadržaja međuspremnika, vjerodajnica ili privatnog signing materijala.

## Browseri

Browser ekstenzije lokalno obrađuju screenshot piksele i koriste browser download workflow za spremljene PNG datoteke. Ne traže široki host pristup i nemaju first-party mrežni klijent, analytics SDK, oglasni SDK ni automatski uploader snimki.

Trajni browser storage koristi se za korisničke postavke. Kratkotrajni metadata podaci vlasništva capture sesije (nasumični token, identifikatori taba/prozora i vrijeme početka) čuvaju se u session-scoped extension storageu kada ga preglednik podržava, uz local-storage fallback samo radi kompatibilnosti. Nedavne snimke dohvaćaju ograničeni skup SNAPVERE kandidata iz browser download API-ja te prije prikaza ili otvaranja lokalno ponovno provjeravaju naziv i stanje zapisa.

Vanjske aplikacije, browser sync funkcije ili sinkronizirane mape koje korisnik odabere imaju vlastito privacy ponašanje.

Podrška: info@snapvere.com
