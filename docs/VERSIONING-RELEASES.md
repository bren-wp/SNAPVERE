# SNAPVERE Versioning & Releases

The current public release is **SNAPVERE 0.1.1**: https://github.com/bren-wp/SNAPVERE/releases/tag/v0.1.1

## Canonical version contract

The active machine-readable source of truth is [`product-version.json`](../product-version.json). For 0.1.1 it aligns:

- Windows product version `0.1.1`;
- Windows assembly/file version `0.1.1.0`;
- Android `versionName 0.1.1` / `versionCode 11`;
- browser extension version `0.1.1` for Chrome, Edge, Opera and Firefox;
- browser store listing version `0.1.1`;
- the exact eight-file public GitHub release contract.

`eng/validate-product-contract.py` checks these values against the actual source files and active documentation.

## Release immutability

Published tags/releases are treated as historical artifacts. A later development pass must not rewrite an older tag or silently replace an older release asset to make history appear cleaner.

Examples:

- `v0.1.0` remains the original four-asset Windows/Android release.
- `v0.1.1` is the first public release that includes the four browser ZIP packages.
- post-release maintenance on `main` does not move the `v0.1.1` tag.

If a future build needs changed binaries, it should receive a new version/tag rather than mutating v0.1.1.

## Version update checklist

For a future version, update all of the following in one release-preparation change:

1. `product-version.json`;
2. `Directory.Build.props` (`VersionPrefix`, `AssemblyVersion`, `FileVersion`);
3. Android `versionName` and monotonically increasing `versionCode`;
4. Chrome/Edge/Opera/Firefox manifest versions;
5. browser `store/listing.json` extension version;
6. active README EN/HR current-release copy and download links;
7. changelog and new release notes;
8. version-specific release/security documentation;
9. CI gates that intentionally validate the release version;
10. release workflow/trigger for the new tag.

Product Contract CI should remain green before merge.

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

The release pipeline hashes every file before publication, creates/verifies the release tag after validation, publishes only the approved asset names and verifies GitHub's published digests afterward.

## Platform signing/status rules

### Windows

A successful build/release does not by itself imply commercial Authenticode signing or reputation. Do not add such a claim unless a real signing identity is used and verified.

### Android

The public v0.1.1 APK is intentionally documented as **CI/debug-signed**. A future production-signing identity is a release-channel change and can affect in-place update compatibility.

### Browser stores

GitHub ZIP publication is separate from Chrome Web Store, Edge Add-ons, Opera Add-ons and Mozilla Add-ons publication. Do not change `browsers.storePublication` in `product-version.json` until the real external store status supports the claim.

## Historical documentation

Historical `RELEASE_NOTES_*` files should preserve the facts of their own releases. Active product guides may reference only the current release, but historical notes are not “stale” simply because they contain an older version number.

## Release verification

For the current release, use GitHub's release page and asset SHA-256 digest metadata. The v0.1.1 release workflow also verifies published asset digests against locally validated values before it completes successfully.
