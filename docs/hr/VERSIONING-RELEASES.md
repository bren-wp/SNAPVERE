# SNAPVERE verzioniranje i izdanja

Aktualno javno izdanje je **SNAPVERE 0.1.1**: https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.1

## Canonical version contract

Aktivni strojno čitljivi izvor istine je [`product-version.json`](../../product-version.json). Za 0.1.1 usklađuje:

- Windows product version `0.1.1`;
- Windows assembly/file version `0.1.1.0`;
- Android `versionName 0.1.1` / `versionCode 11`;
- browser extension verziju `0.1.1` za Chrome, Edge, Operu i Firefox;
- browser store listing verziju `0.1.1`;
- exact javni GitHub release ugovor od osam datoteka.

`eng/validate-product-contract.py` uspoređuje te vrijednosti sa stvarnim source datotekama i aktivnom dokumentacijom.

## Nepromjenjivost objavljenih izdanja

Objavljeni tagovi/releaseovi tretiraju se kao povijesni artefakti. Kasniji development ne smije prepisivati stari tag niti potajno zamijeniti stari release asset kako bi povijest izgledala urednije.

Primjeri:

- `v0.1.0` ostaje originalno izdanje s četiri Windows/Android asseta.
- `v0.1.1` je prvo javno izdanje s četiri browser ZIP paketa.
- post-release maintenance na `main` ne pomiče `v0.1.1` tag.

Ako budući build zahtijeva izmijenjene binarne datoteke, treba dobiti novu verziju/tag umjesto izmjene v0.1.1.

## Checklist za novu verziju

Za buduću verziju u jednom release-preparation changeu uskladi:

1. `product-version.json`;
2. `Directory.Build.props` (`VersionPrefix`, `AssemblyVersion`, `FileVersion`);
3. Android `versionName` i monotonijski rastući `versionCode`;
4. Chrome/Edge/Opera/Firefox manifest verzije;
5. browser `store/listing.json` extension version;
6. aktivni README EN/HR current-release tekst i download linkove;
7. changelog i nove release notes;
8. version-specific release/security dokumentaciju;
9. CI gateove koji namjerno validiraju novu verziju;
10. release workflow/trigger za novi tag.

Product Contract CI mora ostati zelen prije mergea.

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

Release pipeline računa hash svake datoteke prije objave, kreira/provjerava tag nakon validacije, objavljuje samo odobrena imena te nakon toga provjerava GitHub digeste.

## Pravila potpisa i store statusa

### Windows

Uspješan build/release sam po sebi ne znači komercijalni Authenticode signing/reputation. Takva tvrdnja ne smije se dodati bez stvarnog i provjerenog signing identiteta.

### Android

Javni v0.1.1 APK namjerno je dokumentiran kao **CI/debug-signed**. Budući production signing identitet mijenja release kanal i može utjecati na mogućnost in-place updatea.

### Browser storeovi

GitHub ZIP objava odvojena je od Chrome Web Store, Edge Add-ons, Opera Add-ons i Mozilla Add-ons objave. `browsers.storePublication` u `product-version.json` ne smije se promijeniti dok stvarni vanjski store status ne podržava tu tvrdnju.

## Povijesna dokumentacija

Povijesni `RELEASE_NOTES_*` dokumenti trebaju čuvati činjenice svog izdanja. Aktivni vodiči mogu biti usmjereni na aktualni release, ali povijesni note nije “zastario” samo zato što navodi staru verziju.

## Provjera izdanja

Za aktualno izdanje koristi GitHub release stranicu i SHA-256 digest metapodatke asseta. v0.1.1 release workflow dodatno uspoređuje objavljene digeste s lokalno validiranim vrijednostima prije uspješnog završetka.
