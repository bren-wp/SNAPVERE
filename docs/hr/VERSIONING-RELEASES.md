# SNAPVERE verzioniranje i izdanja

Aktualno javno izdanje je **SNAPVERE 0.1.1**: https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.1

Detaljna povijest izdanja održava se u root datoteci [`RELEASES.md`](../../RELEASES.md). To je kanonski ljudski čitljivi izvor release-notesa, dok [`CHANGELOG.md`](../../CHANGELOG.md) ostaje kraći tehnički sažetak promjena.

## Kanonski verzijski ugovor

Aktivni strojno čitljivi izvor istine je [`product-version.json`](../../product-version.json). Za 0.1.1 usklađuje:

- Windows product version `0.1.1`;
- Windows assembly/file version `0.1.1.0`;
- Android `versionName 0.1.1` / `versionCode 11`;
- browser extension verziju `0.1.1` za Chrome, Edge, Operu i Firefox;
- browser store listing verziju `0.1.1`;
- točan javni GitHub release ugovor od osam datoteka.

`eng/validate-product-contract.py` uspoređuje te vrijednosti sa stvarnim source datotekama i aktivnom dokumentacijom. Također provjerava kanonsku `RELEASES.md` povijest i odbija ponovno stvaranje root `RELEASE_NOTES_<version>.md` fragmentacije.

## Pravilo za release-notes

Za buduća izdanja **ne stvaraj novi `RELEASE_NOTES_<version>.md`**.

Umjesto toga:

1. dodaj novu detaljnu verzijsku sekciju na vrh `RELEASES.md`;
2. starije sekcije ostavi nepromijenjene kao povijesni snapshot;
3. dodaj odgovarajući kraći zapis u `CHANGELOG.md`;
4. uskladi strojno čitljive i platformske verzijske izvore u istom changeu;
5. novu `RELEASES.md` sekciju koristi kao ljudski čitljiv izvor teksta za objavu izdanja.

Sekcija `Unreleased` u `RELEASES.md` može dokumentirati post-release rad na `main` bez tvrdnje da je taj rad retroaktivno uključen u već objavljene binarne datoteke.

## Nepromjenjivost objavljenih izdanja

Objavljeni tagovi i releaseovi povijesni su artefakti. Kasniji development ne smije prepisivati stari tag niti potajno zamijeniti postojeći asset.

Primjeri:

- `v0.1.0` ostaje originalno izdanje s četiri Windows/Android asseta;
- `v0.1.1` je prvo javno izdanje s četiri browser ZIP paketa;
- post-release maintenance na `main` ne pomiče `v0.1.1` tag.

Ako budući build zahtijeva izmijenjene binarne datoteke, dobiva novu verziju i novi tag.

## Povijesni release workflowi

Nakon što je verzija objavljena i verificirana, njezin version-specific workflow treba premjestiti u `.github/release-archive/`, izvan `.github/workflows/`. Time povijesna automatizacija ostaje dostupna za audit, ali se više ne registrira kao aktivni GitHub Actions workflow.

Buduće izdanje dobiva novi pregledani release workflow/trigger primjeren toj verziji. Ljudski čitljivi release tekst treba dolaziti iz odgovarajuće sekcije `RELEASES.md`, a ne iz nove zasebne notes datoteke.

## Checklist za novu verziju

Za buduću verziju u jednom release-preparation changeu uskladi:

1. `product-version.json`;
2. `Directory.Build.props` (`VersionPrefix`, `AssemblyVersion`, `FileVersion`);
3. Android `versionName` i monotonijski rastući `versionCode`;
4. Chrome/Edge/Opera/Firefox manifest verzije;
5. browser `store/listing.json` extension version;
6. aktivni README EN/HR current-release tekst i download linkove;
7. novu detaljnu sekciju na vrhu `RELEASES.md` i kraći zapis u `CHANGELOG.md`;
8. version-specific release/security dokumentaciju gdje je potrebna;
9. CI gateove koji namjerno validiraju novu verziju;
10. pregledani release workflow/trigger za novi tag.

Product Contract CI i primjenjivi Windows, Android, browser i security gateovi trebaju biti zeleni prije mergea i objave.

## Aktualni v0.1.1 asseti

Release sadrži točno:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE.apk
SNAPVERE-Android-Source.zip
SNAPVERE-Chrome.zip
SNAPVERE-Edge.zip
SNAPVERE-Opera.zip
SNAPVERE-Firefox.zip
```

Release pipeline izračunao je hash svake datoteke prije objave, kreirao/provjerio tag nakon validacije, objavio samo odobrena imena te zatim provjerio GitHub digeste.

## Pravila potpisa i store statusa

### Windows

Uspješan build/release sam po sebi ne znači komercijalni Authenticode signing ili reputation. Takva tvrdnja ne smije se dodati bez stvarnog i provjerenog signing identiteta.

### Android

Javni v0.1.1 APK namjerno je dokumentiran kao **CI/debug-signed**. Budući production signing identitet mijenja release kanal i može utjecati na in-place update kompatibilnost.

### Browser storeovi

GitHub ZIP objava odvojena je od Chrome Web Store, Edge Add-ons, Opera Add-ons i Mozilla Add-ons objave. `browsers.storePublication` u `product-version.json` ne smije se promijeniti dok stvarni vanjski store status to ne potvrđuje.

## Povijesna dokumentacija

Starije sekcije u [`RELEASES.md`](../../RELEASES.md) čuvaju činjenice svojih verzija. Povijesni zapis nije “zastario” samo zato što sadrži stari shortcut, package layout, licencu, signing status ili funkcionalnu granicu koja se kasnije promijenila.

Bivše version-specific `RELEASE_NOTES_*` datoteke konsolidirane su kako bi se uklonilo duplicirano održavanje release dokumentacije. Već objavljeni GitHub Release opisi ostaju netaknuti.

## Provjera izdanja

Za aktualno izdanje koristi GitHub release stranicu i SHA-256 digest metapodatke asseta. Povijesni v0.1.1 release workflow prije uspješnog završetka usporedio je objavljene digeste s lokalno validiranim vrijednostima.
