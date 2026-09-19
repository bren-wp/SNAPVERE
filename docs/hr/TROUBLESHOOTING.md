# SNAPVERE — rješavanje problema

Aktualno javno izdanje: **v0.1.6**. Grana `main` može sadržavati kasniji neobjavljeni reliability i security hardening.

## Windows snimanje ne radi

Pokušaj ponovno nakon zatvaranja zaštićenog ili full-screen sadržaja. SNAPVERE preferira moderni Windows capture put i za očekivane acquisition greške može koristiti kompatibilni monitor fallback. Zaštićeni sadržaj može namjerno ostati prazan.

## Globalni prečac ne radi

Druga aplikacija možda koristi isti globalni prečac. Pokreni snimanje iz tray izbornika i provjeri Windows startup/shortcut stanje.

## Spremanje snimke ili Region kopiranje ne uspijeva

Ako spremanje ne uspije, SNAPVERE razlikuje blokiranu mapu snimki, puni uređaj za pohranu i drugi lokalni kvar zapisa PNG datoteke. Provjeri dozvole za Pictures/SNAPVERE, slobodan prostor i pokušaj ponovno. Region clipboard greške ostaju odvojene od file persistencea. SNAPVERE koristi staged zapisivanje i atomic final move pa prekinuti PNG encode nije prikazan kao dovršena snimka. Tehnički filesystem exception tekst ostaje u lokalnoj dijagnostici umjesto u recovery poruci.

## Portable se ne pokreće

Zatvori sve pokrenute SNAPVERE procese i ponovno pokreni Portable izvršnu datoteku. Ako SNAPVERE prijavi problem validacije paketa ili cachea, preuzmi svježu kopiju iz službenog izdanja. Kod filesystem grešaka provjeri može li Windows pisati u privremenu mapu i local application-data mapu trenutnog korisnika te ima li dovoljno slobodnog prostora.

Portable startup dijalog namjerno prikazuje sanitiziranu kategoriju greške umjesto sirovog runtime exception teksta. Tehnički detalji ostaju lokalno u `%LOCALAPPDATA%\SNAPVERE\Logs\startup.log`; SNAPVERE taj log ne šalje automatski.

## Browser snimanje ne radi

Provjeri radi li se o običnoj web stranici. Privilegirane browser stranice mogu blokirati script injection ili screenshot API. Ako se aktivna kartica promijeni tijekom snimanja, SNAPVERE odbacuje frame umjesto spremanja sadržaja pogrešne kartice.

## Full Page je odbijen

Stranica je prešla ograničeni tile/canvas/pixel budget. To je zaštita stabilnosti, a ne skrivena background greška.

Windows startup dijagnostika, kada je potrebna, ostaje lokalno pod `%LOCALAPPDATA%\SNAPVERE\Logs`.
