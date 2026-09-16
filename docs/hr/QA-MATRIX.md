# SNAPVERE 0.1.2 QA matrica

Ova matrica opisuje automatiziranu regresijsku evidenciju. Zeleni gate znači da je provjereni ugovor prošao na tom commitu; ne predstavlja apsolutno jamstvo platformskog ponašanja.

## Windows gateovi

- .NET restore s vulnerability auditom.
- x64 build i unit-test suite.
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
- runtime background smoke testovi.
- active-tab ownership provjere prije i nakon capturea.
- bounded full-page memory ponašanje.
- deterministic ZIP pakiranje i store-readiness metadata validacija.

## Repository i sigurnost

- Product Contract CI.
- CodeQL za C#, JavaScript/TypeScript, Python i GitHub Actions.
- pinned workflow actions.
- dependency monitoring.
- validacija linkova i aktivne dokumentacije kroz product contract.

Povezano: [Performanse i stabilnost](PERFORMANCE.md), [Status proizvoda](PRODUCT-STATUS.md) i [Security Policy](../../SECURITY.md).
