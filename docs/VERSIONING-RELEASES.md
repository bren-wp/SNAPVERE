# SNAPVERE Versioning & Releases

The current public release is **SNAPVERE 0.1.1**: https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.1

Detailed release history is maintained in the repository-root [`RELEASES.md`](../RELEASES.md). It is the canonical human-readable release-notes source. [`CHANGELOG.md`](../CHANGELOG.md) remains the shorter engineering change summary.

## Canonical version contract

The active machine-readable source of truth is [`product-version.json`](../product-version.json). For 0.1.1 it aligns:

- Windows product version `0.1.1`;
- Windows assembly/file version `0.1.1.0`;
- Android `versionName 0.1.1` / `versionCode 11`;
- browser extension version `0.1.1` for Chrome, Edge, Opera and Firefox;
- browser store listing version `0.1.1`;
- the exact eight-file public GitHub release contract.

`eng/validate-product-contract.py` checks these values against the actual source files and active documentation. It also validates the canonical `RELEASES.md` history and rejects a return to root `RELEASE_NOTES_<version>.md` fragmentation.

## Release-note policy

For future releases, **do not create another `RELEASE_NOTES_<version>.md` file**.

Instead:

1. prepend a new detailed version section to `RELEASES.md`;
2. leave all older version sections intact as historical snapshots;
3. add a concise corresponding entry to `CHANGELOG.md`;
4. update the machine-readable/platform version sources together;
5. use the new section as the human-readable source for the release description.

The `Unreleased` section in `RELEASES.md` can describe post-release `main` work without implying that those changes were retroactively included in the most recent published binaries.

## Release immutability

Published tags/releases are historical artifacts. A later development pass must not rewrite an older tag or silently replace an older release asset to make history appear cleaner.

Examples:

- `v0.1.0` remains the original four-asset Windows/Android release.
- `v0.1.1` is the first public release that includes the four browser ZIP packages.
- post-release maintenance on `main` does not move the `v0.1.1` tag.

If a future build needs changed binaries, it receives a new version/tag rather than mutating v0.1.1.

## Historical release workflows

Once a version has been published and verified, its version-specific workflow belongs under `.github/release-archive/`, outside `.github/workflows/`. This keeps the historical automation available for audit while preventing old publication workflows from remaining registered as active Actions indefinitely.

A future release should get a new reviewed release workflow/trigger appropriate to that version. Its human-readable release text should come from the matching `RELEASES.md` section rather than a new version-specific notes file.

## Version update checklist

For a future version, update all of the following in one release-preparation change:

1. `product-version.json`;
2. `Directory.Build.props` (`VersionPrefix`, `AssemblyVersion`, `FileVersion`);
3. Android `versionName` and monotonically increasing `versionCode`;
4. Chrome/Edge/Opera/Firefox manifest versions;
5. browser `store/listing.json` extension version;
6. active README EN/HR current-release copy and download links;
7. prepend the detailed release section to `RELEASES.md` and update `CHANGELOG.md`;
8. version-specific release/security documentation where needed;
9. CI gates that intentionally validate the release version;
10. a reviewed release workflow/trigger for the new tag.

Product Contract CI plus applicable Windows, Android, browser and security gates should be green before merge/publication.

## Current v0.1.1 release assets

The release contains exactly:

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

The release pipeline hashed every file before publication, created/verified the release tag after validation, published only the approved asset names and verified GitHub's published digests afterward.

## Platform signing/status rules

### Windows

A successful build/release does not by itself imply commercial Authenticode signing or reputation. Do not add such a claim unless a real signing identity is used and verified.

### Android

The public v0.1.1 APK is intentionally documented as **CI/debug-signed**. A future production-signing identity is a release-channel change and can affect in-place update compatibility.

### Browser stores

GitHub ZIP publication is separate from Chrome Web Store, Edge Add-ons, Opera Add-ons and Mozilla Add-ons publication. Do not change `browsers.storePublication` in `product-version.json` until the real external store status supports the claim.

## Historical documentation

Older sections in [`RELEASES.md`](../RELEASES.md) preserve the facts of their own versions. A historical note is not “stale” merely because it contains an old shortcut, package shape, license, signing statement or feature boundary that later changed.

The former version-specific `RELEASE_NOTES_*` files were consolidated to remove duplicated release-document maintenance. Published GitHub Release descriptions remain untouched.

## Release verification

For the current release, use GitHub's release page and asset SHA-256 digest metadata. The historical v0.1.1 release workflow verified published asset digests against locally validated values before completing successfully.
