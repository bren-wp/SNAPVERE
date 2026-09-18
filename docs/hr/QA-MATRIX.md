# SNAPVERE QA matrica

Ova matrica opisuje automatiziranu regresijsku evidenciju za aktivno održavani Windows/browser proizvod. Aktualno javno izdanje je v0.1.4, dok `main` može sadržavati neobjavljeni hardening. Zeleni gate znači da je provjereni ugovor prošao na tom commitu; ne predstavlja apsolutno jamstvo platformskog ponašanja.

## Windows gateovi

- .NET restore s vulnerability auditom.
- x64 build i unit-test suite, uključujući paralelno PNG spremanje dviju snimki s istim timestampom.
- x86 build i ARM64 cross-build.
- provjera native payload strukture.
- stvarni renderirani WinUI snapshotovi za Region, Window, Tray, Options, Language i About.
- PR visual usporedba s posljednjim uspješnim `main` baselineom.
- universal Setup/Portable build.
- točan two-file Windows package contract.
- package-size regression budget.
- x64 i x86 Setup/Portable lifecycle completion.
- tray-first launch provjera unutar lifecycle probea.

## Browser gateovi

- Manifest V3 i točna permission validacija.
- zaključan SNAPVERE naziv, wordmark i filename prefix.
- Chrome/Edge/Opera paritet i dokumentirane Firefox razlike.
- EN/HR locale paritet.
- runtime background smoke testovi, uključujući owner-safe stale-lock cleanup interleaving.
- active-tab ownership provjere prije i nakon capturea.
- bounded full-page memory ponašanje.
- Settings/Recent behavioral testovi za odbacivanje zastarjelih async rezultata i zaštitu od dvostrukih Open/folder akcija.
- točan `downloads.open` permission za izričitu Recent > Otvori radnju uz trajnu zabranu širokog host pristupa.
- responsive, disabled-state i reduced-motion paritet izvornog koda kroz sva četiri browsera.
- deterministic ZIP pakiranje i store-readiness metadata validacija.

## Repository i sigurnost

- Product Contract CI.
- CodeQL za C#, JavaScript/TypeScript, Python i GitHub Actions.
- pinned workflow actions.
- dependency monitoring.
- validacija linkova i aktivne dokumentacije kroz product contract.

Povezano: [Performanse i stabilnost](PERFORMANCE.md), [Status proizvoda](PRODUCT-STATUS.md) i [Security Policy](../../SECURITY.md).
