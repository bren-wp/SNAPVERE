# Visual QA stvarno renderiranog UI-a

SNAPVERE tretira stvarno renderirani Windows UI kao CI-testiranu površinu proizvoda. Visual-QA put koristi pravi x64 `Snapvere.exe` koji je proizveo workflow; SVG ilustracije i generirani mockupovi ne prihvaćaju se kao runtime screenshotovi.

## Površine koje se snimaju

`eng/Capture-SnapvereVisualQa.ps1` pokreće namjenske probe modove i snima točno šest stvarno renderiranih PNG datoteka:

- `region-capture.png`
- `window-capture.png`
- `tray-menu.png`
- `options.png`
- `language.png`
- `about.png`

Capture skripta također zapisuje `manifest.json` s renderiranim dimenzijama, veličinom PNG datoteke i SHA-256 sažetkom za svaku površinu. Ako frame nedostaje, vizualno je prazan ili je neočekivano malen, workflow pada prije nastavka packaginga.

## Regression baseline za pull request

CI za pull request koristi najnoviji uspješni `main` push koji još ima neistekli `snapvere-visual-qa-<sha>` artefakt. Workflow taj artefakt preuzima izravno kroz GitHub Actions i uspoređuje ga s aktualnim PR snapshotovima pomoću `eng/Compare-SnapvereVisualQa.ps1`.

Comparator prvo provjerava ugovor od točno šest PNG datoteka i pokrivenost u manifestu. Zatim uspoređuje svaku odgovarajuću površinu nakon normalizacije obiju slika na fiksnu sample mrežu. Gate bilježi:

- baseline i aktualne dimenzije;
- omjer promjene širine i visine;
- omjer veličine PNG datoteka;
- normaliziranu prosječnu RGB razliku;
- omjer značajno promijenjenih sample pixela;
- pass/fail razloge za svaku površinu.

Generirani `comparison.json` uploada se zajedno s aktualnim stvarno renderiranim PNG datotekama i manifestom.

## Zadana regression pravila

Zadana pravila namjerno toleriraju male razlike u anti-aliasingu i renderiranju GitHub hosted runnera, ali odbijaju velike nenamjerne promjene:

| Provjera | Zadana granica |
| --- | ---: |
| promjena širine | 8% |
| promjena visine | 8% |
| normalizirana prosječna RGB razlika | 0.18 |
| značajno promijenjeni sample pixeli | 70% |
| prag značajnog sample pixela | 0.20 |
| omjer veličine PNG datoteke | 0.35–3.0 |
| normalizirana sample mreža | 64×64 |

Ove vrijednosti su sigurnosna granica protiv regresije, a ne ocjena kvalitete dizajna. Spacing, tipografija, veličina ikona, rounded corners i alignment i dalje se provjeravaju pregledom stvarnog uploadanog PNG artefakta. Nemoj slabiti granice samo zato da bi PR postao zelen; prvo utvrdi je li promjena namjerna, rezultat runner varijacije ili stvarna UI regresija.

## Zadržavanje baseline artefakta

Aktualni visual-QA artefakti zadržavaju se 90 dana. PR CI pregledava nedavne uspješne `main` runove i uzima prvi odgovarajući artefakt koji nije istekao. Ako valjani renderirani baseline ne postoji, regression korak jasno pada umjesto da tiho preskoči usporedbu.

## Što zeleni gate stvarno potvrđuje

Zeleni visual-QA regression korak potvrđuje da:

- je x64 aplikacija na GitHub Windows runneru renderirala svih šest obveznih površina;
- screenshotovi nisu prazni i zadovoljavaju artefaktni ugovor;
- aktualni PR ostaje unutar konfiguriranih granica vizualnog odstupanja u odnosu na uspješni `main` runtime snapshot;
- je proizveden strojno čitljiv comparison report.

Ne potvrđuje fizičko ARM64 runtime ponašanje, svaki mixed-DPI raspored, svaki grafički driver, svaku Windows temu/konfiguraciju niti pixel-perfect jednakost s ljudskim dizajn referencama. ARM64 se i dalje cross-builda i strukturno validira na hosted x64 runneru dok se izričito ne uvede pravi ARM64 runtime job.

## Pregled namjerne UI promjene

Kod namjerne vizualne izmjene:

1. pregledaj PR `snapvere-visual-qa-<sha>` artefakt;
2. usporedi pogođeni stvarni PNG s aktualnom SNAPVERE dizajn referencom;
3. provjeri da promjena ne izlaže placeholder kontrole ili neimplementirane funkcije;
4. regression pravila ostavi nepromijenjena osim ako postoji dokumentiran tehnički razlog za izmjenu;
5. mergeaj tek kada finalni PR head i svi CI jobovi budu zeleni.

Nakon mergea, sljedeći uspješni `main` visual artefakt prirodno postaje baseline za sljedeće pull requestove.
