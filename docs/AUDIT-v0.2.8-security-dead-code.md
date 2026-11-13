# SNAPVERE v0.2.8 — sigurnost, stabilnost, performanse i dead-code audit

Datum pregleda: 13.11.2026. Područje: repozitorijska **Tauri 2 / Rust** aplikacija, React/TypeScript GUI, lokalna SQLite galerija, statički web u `site/` te Windows NSIS/MSI i GitHub Actions. **Javno postavljeni snapvere.com trenutno služi zasebno izdanje .NET / WinUI 3 v0.0.8.** Ne smije se poistovjetiti s Tauri izdanjem niti automatski zamijeniti postojećim paketima.

## Provjereni statički i automatizirani kontrolni mehanizmi

| Područje | Kontrola | Trenutačni rezultat ili granica |
|---|---|---|
| TypeScript | `tsc --noEmit`, `tsc && vite build`; Node unit testovi postavki/paginacije | Pokretanje u quality i release workflowu; zadnji commit mora ponovno proći |
| Rust | `cargo fmt --all -- --check`, `cargo test --locked`, `cargo clippy --all-targets --locked -- -D warnings` | Windows CI je mjerodavan; puni build i testovi za najnoviji commit još nisu certificirani |
| Ovisnosti | `npm ci`, `npm audit --audit-level=high`, zaključani Cargo.lock | npm audit automatiziran; **RustSec/cargo-audit i SCA svih tranzitivnih ovisnosti ostaju otvoreni** |
| IPC | `tools/check_ipc_contract.py` | Svaki `invoke` iz tipiziranog frontend wrappera mora postojati u Rust registryju; statička provjera ne dokazuje runtime uspjeh |
| Javni linkovi | `tools/check_public_distribution.py`, `tools/check_website.py` | GitHub domene ne smiju biti u javnim UI datotekama aplikacije ili weba; preuzimanja moraju biti first-party |
| Vizualna provjera | `tools/check_brand_tokens.py`, Playwright capture i visual comparison | Dijele se brand tokeni i originalne snimke renderiranog frontenda; to NIJE 1:1 fizički Windows QA |
| Licence | `bundle.licenseFile`, komercijalni RTF, `/license/`, Settings/About | Konfigurirana EULA za NSIS i MSI; stvarni klik/odbijanje i uninstall/upgrade na Windowsu još moraju biti provjereni |

## Konkretni ispravci u PR-u #18

1. Ispravljeno odsijecanje negativnih koordinata monitora, spremanje u izabranom formatu, SQLite literalna pretraga i referencijalni integritet u memorijskom fallbacku.
2. Novi zapis snimke upisuje se s `create_new`; djelomične datoteke/minijature čiste se na neuspjeh. Postojeća videosnimka ne briše se ako upis metapodataka ne uspije. Save As prvo zapisuje u novu privremenu datoteku pa tek onda preimenuje.
3. PNG IPC u nativnom sloju provjerava potpis, dimenzije i gornju granicu broja piksela/veličine base64 poruke. Clipboard kopiranje ima ograničenje veličine datoteke i slike.
4. PNG encoder sada obrađuje posuđene RGBA podatke bez kopiranja cijelog framea. SQLite upit s tagovima koristi `EXISTS` umjesto množenja rezultata joinom. Paginacija galerije i pretrage je ograničena i stabilna.
5. Zatvaranje tijekom snimanja čeka stvarnu finalizaciju do zadanog timeouta umjesto slijepih 600 ms. Ograničenje ne može spriječiti driver deadlock, ali smanjuje rizik prekinutog MP4/GIF zapisa.
6. Nepotrebni Tauri runtime plugini `tauri-plugin-fs` i `tauri-plugin-os` više nisu registrirani, a `fs:default` i `os:default` uklonjeni su iz webview capabilities. Preostale deklaracije Cargo ovisnosti treba ukloniti tek uz kontroliranu regeneraciju lockfilea.
7. Komercijalni installer EULA ne opoziva prava ranije objavljenog MIT izvornog koda. Pravni pregled komercijalnih uvjeta preporučen je prije javne produkcijske distribucije.

## Dead-code provjera i što se NE smije naslijepo ukloniti

- Strogi TypeScript i Rust Clippy provjeravaju neiskorištene simbole, greške i dio nedostižnog koda; `tools/check_ipc_contract.py` kontrolira da UI wrapper ne sadrži nepostojeće backend komande.
- `src-tauri/examples/captest.rs` predstavlja ručni dijagnostički primjer za capture; ne učitava se kao produkcijska funkcija. Zadržati ga dok ne postoje pouzdani fizički višemonitorski testovi.
- Polja `remote_id`, `remote_slug`, `remote_url` i `remote_expires_at` u SQLite shemi ne predstavljaju funkcionalno aktivnu cloud opciju. Njihovo brisanje bez migracije ugrozilo bi kompatibilnost postojećih baza; ostaju do planske migracije.
- Sadržaj `docs/screenshots` i `site/screenshots` ima dokumentacijsku/marketinšku ulogu; nije dokaz hardverskog rada. Ne brisati ga bez provjere referenci.
- Ovaj pregled nije dokaz da svaka dinamička grana, platform-specific FFI putanja ili korisnička kombinacija nema neiskorištenog koda. Za konačnu potvrdu potrebni su instrumentirani Windows testovi i runtime coverage.

## Otvoreni rizici / obavezni QA prije stabilnog izdanja

**Visoki prioritet:** izdanje još nije kodno potpisano; potrebno je provjeriti instalaciju, odbijanje/prihvaćanje EULA-a i nadogradnju iz stvarnih Windows paketa, te ispravnost snimanja s mikrofonom i sistemskim zvukom na fizičkim Windows 10/11 uređajima. Nema jamstva „0 padova“ bez dugotrajnih stress/soak i driver-specific testova.

**Provenance:** repo `site/config.js` navodi izdanje 0.2.4, dok binarne SHA-256 vrijednosti u `site/files` nisu potvrđene kao iste službenim javnim GitHub 0.2.4 assetima. Ne prepisivati postojeća javna preuzimanja dok se ne potvrde izvor i kompatibilnost.

**Sigurnost koja još zahtijeva rad:** cjelovita provjera RustSec advisories, stroži CSP websitea uz očuvanje consent-only analitike, pregled kontroliranih Save As putanja prema trust-boundary modelu i testiranje svih FFI rubova. Nema potvrđenog external pentesta.

**UI/UX:** zajednički brand tokeni nisu dokaz 1:1 pikselske podudarnosti; referentni screenshotovi i usporedba moraju biti verzijski i OS specifični. Posebno provjeriti Windows zoom 100–400%, mixed-DPI, više monitora, overlay, editor, HUD, video controls, sve postavke i responsive web na mobilnom/tablet/desktop prikazu.

## Preporučene ručne regresije

NSIS i MSI: prikaz pune licence → odbijanje ne instalira → prihvaćanje instalira → program se pokreće → nova verzija čuva lokalnu galeriju, hotkey i postavke → uninstall uklanja aplikaciju uz eksplicitnu odluku o korisničkim podacima. Snimanje: start/stop/pause/cancel, sistemski/mikrofonski zvuk, MP4/GIF, kontrola kursora, minimalno/maksimalno područje, promjena monitora, Quit dok enkoder finalizira. Screenshot: kopiranje, PNG/JPEG/WebP Save As, pun disk, read-only mapa, postojeća datoteka, ogromna slika i oštećen PNG. Galerija: 500+ datoteka, pretraga tagova, promjena filtera, brisanje i paginacija; bez duplih ili izgubljenih rezultata.

**Izdanje v0.2.8 ostaje preview kandidat unutar otvorenog PR-a #18 dok svi CI gateovi i fizički testovi ne prođu.**
