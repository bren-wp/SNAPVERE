# SNAPVERE QA matrica

Ova matrica opisuje automatiziranu regresijsku evidenciju za aktivno održavani Windows/browser proizvod. Aktualno javno izdanje je v0.1.10, dok `main` može sadržavati kasniji neobjavljeni hardening. Zeleni gate znači da je provjereni ugovor prošao na tom commitu; ne predstavlja apsolutno jamstvo platformskog ponašanja.

## Windows gateovi

- .NET restore s vulnerability auditom.
- x64 build i unit-test suite, uključujući paralelno PNG spremanje dviju snimki s istim timestampom, stale/recent/foreign/locked capture-temp cleanup granice, tipiziranu access-denied/storage-full/write-failure persistence klasifikaciju s invalid-target integration slučajem, provjeru da Portable startup poruke ne izlažu umetnute privatne putanje iz exception teksta i bounded provjeru local-shell failure taksonomije.
- x86 build i ARM64 cross-build.
- provjera native payload strukture.
- stvarni renderirani WinUI snapshotovi za Region, Window, Tray, Options, Language i About.
- PR visual usporedba s posljednjim uspješnim `main` baselineom.
- universal Setup/Portable build.
- točan two-file Windows package contract.
- package-size regression budget.
- x64 i x86 Setup/Portable lifecycle completion.
- locked-file uninstall failure injection koji zahtijeva da Installed Apps/startup/prečac metadata ostane dok file cleanup stvarno ne uspije, nakon čega slijede repair i uspješan uninstall.
- provjera odbijanja deferred cleanupa s neispravnim parent PID-om i Setup-mutex contentiona, uz zahtjev da instalacijski ugovor ostane nepromijenjen.
- tray-first launch provjera unutar lifecycle probea, uključujući dokaz da drugi Installed ili Portable launch aktivira postojeći proces bez stvaranja duplikata te unit-testirani bounded Explorer tray-recovery backoff.
- capture/shutdown lifecycle serializacija dokazuje da aktivno snimanje blokira Exit-driven dispose servisa do završetka te da prihvaćeni shutdown blokira nova snimanja.
- testovi screen-recording policyja provjeravaju parne H.264 dimenzije, 8K granicu izvora, bitrate granice i odbijanje zero-frame/zero-byte izlaza; MP4 writer testovi pokrivaju atomic publish, collision-safe nazive, cleanup privremenih datoteka i zabranu objave nakon greške.
- responsive Setup policy testovi provjeravaju sidebar breakpoint, compact širine i horizontal-fit invariant; browser validator zaključava popup/options compact breakpointove za sva četiri browsera.
- installer safety regresijski testovi za exact marker matching, normalizaciju instalacijske mape, validaciju normalnog directory chaina i ownership install targeta (missing/empty/owned/unowned/invalid-marker/marker-only).

## Browser gateovi

- Manifest V3 i točna permission validacija.
- zaključan SNAPVERE naziv, wordmark i filename prefix.
- Chrome/Edge/Opera paritet i dokumentirane Firefox razlike.
- EN/HR locale paritet.
- runtime background smoke testovi, uključujući owner-safe stale-lock cleanup interleaving i provjeru sender/active-tab ownershipa za poruke ekstenzije.
- active-tab i last-focused-window ownership provjere prije i nakon pixel capturea te behavioral coverage da se Region/Full Page page-mutating side effecti odbijaju ili token-cleanupaju kada se aktivna kartica ili fokusirani browser prozor promijeni.
- bounded full-page memory ponašanje.
- Settings/Recent behavioral testovi za odbacivanje zastarjelih async rezultata i zaštitu od dvostrukih Open/folder akcija.
- točan `downloads.open` permission za izričitu Recent > Otvori radnju uz trajnu zabranu širokog host pristupa.
- responsive, disabled-state i reduced-motion paritet izvornog koda kroz sva četiri browsera.
- deterministic ZIP pakiranje i store-readiness metadata validacija.

## Repository i sigurnost

- Product Contract CI, uključujući source contracte koji odbijaju izravno Portable `exception.Message` izlaganje i sprječavaju Windows fatal-startup UI da prikaže lokalnu putanju diagnostics loga.
- CodeQL za C#, JavaScript/TypeScript, Python i GitHub Actions.
- pinned workflow actions.
- dependency monitoring.
- validacija linkova i aktivne dokumentacije kroz product contract.

Povezano: [Performanse i stabilnost](PERFORMANCE.md), [Status proizvoda](PRODUCT-STATUS.md) i [Security Policy](../../SECURITY.md).
