# SNAPVERE branding

SNAPVERE je naziv i identitet proizvoda. Brendigo je developer i publisher.

## Službeni identitet

- Product brand: **SNAPVERE**
- Tagline: **Capture. Edit. Done.**
- Developer / publisher: **Brendigo**
- Official product website: **https://snapvere.com**
- Developer website: **https://brendigo.com**

## Vizualni jezik

Primarni UI je tamni graphite sustav s violet/indigo primary akcentom i umjerenim cyan detaljima. Referentne površine moraju djelovati kao jedan proizvod: tray menu, Region editor, Window picker, Options, Language, About i Setup.

## Logo i ikone

Canonical vector asseti nalaze se u `assets/branding/`. SNAPVERE brand mark treba koristiti isti oblik i gradijent kroz README, Setup i aplikaciju. Za funkcijske ikone koristi se konzistentan Fluent stil; emoji se ne koriste kao zamjena za UI ikone.

## README slike i visual QA

`docs/images/` sadrži održavane UI reference. One služe kao dizajnerski ugovor prema kojem se implementira stvarni app UI. Ne smiju se predstavljati kao stvarni Windows screenshotovi ako nisu snimljene iz izvršene aplikacije.

CI odvojeno pokreće stvarni x64 WinUI build i snima šest renderiranih PNG površina: Region, Window, Tray, Options, Language i About. Vizualno prazan ili neočekivano malen frame ruši visual-QA gate. PNG-ovi i manifest s dimenzijama, veličinom i SHA-256 sažetkom spremaju se kao kratkotrajni GitHub Actions artefakt.

Stvarni screenshot koji se trajno dodaje u dokumentaciju mora nastati reproducibilnim pokretanjem stvarnog WinUI builda. Postojanje CI artefakta ne znači da se održavani SVG reference mogu nazivati runtime screenshotovima.

Time README ne prikazuje dizajn koji aplikacija ne može reproducirati.

## Komercijalni identitet

Od v0.0.7 SNAPVERE se distribuira pod SNAPVERE Commercial Software License Agreementom. Aktualni About i Setup ne smiju prikazivati stare MPL oznake. Povijesne verzije ostaju pod licencom s kojom su već objavljene.

## Naming

U user-facing copyju koristi se `SNAPVERE`, a ne razvojni nazivi projekata ili namespaceovi. `Brendigo` se koristi kada se navodi developer/publisher. `snapvere.com` je službena product stranica; `brendigo.com` developer stranica.
