# SNAPVERE 0.1.15 Versioning and Releases

Current public release: **v0.1.15**.

`product-version.json` is the canonical active product contract. Current active distribution consists of exactly six packages: Windows Setup/Portable plus Chrome, Edge, Opera and Firefox ZIP packages.

Published tags and assets are historical output and are not silently rewritten by later maintenance. New binary behavior belongs in a future version/tag. Detailed historical release notes are kept in the root `RELEASES.md`; no root `RELEASE_NOTES_<version>.md` file is created for the active release line. Active product documentation describes the current maintained product surface.

Release preparation must pass Product Contract CI plus Windows, browser and security gates before publication.

For browser release preparation, `ekstenzije/store/listing.json` must use the same semantic version as every active browser manifest. Its `developmentChannel` follows the exact form `v<extensionVersion>-release`; for SNAPVERE 0.1.15 this resolves to `v0.1.15-release`. Store-readiness validation derives that value from `extensionVersion` rather than hardcoding a historical release. Package names, permission justifications, privacy disclosures and prepared store assets must remain aligned with the current browser sources.

External store acceptance is a publisher/reviewer state, not a repository CI result. A version-aligned ZIP and store kit must not be described as published until a real store listing is available.

The active 0.1.15 release contract is:

```text
SNAPVERE-Setup.exe
SNAPVERE-Portable.exe
SNAPVERE-Chrome.zip
SNAPVERE-Edge.zip
SNAPVERE-Opera.zip
SNAPVERE-Firefox.zip
```