# SNAPVERE 0.1.1 Versioning and Releases

`product-version.json` is the canonical active product contract. Current active distribution consists of Windows Setup/Portable plus Chrome, Edge, Opera and Firefox ZIP packages.

Published tags and assets are historical output and are not silently rewritten by later maintenance. New binary behavior belongs in a future version/tag. Detailed historical release notes are kept in the root `RELEASES.md`; active product documentation describes the current maintained product surface.

Release preparation must pass Product Contract CI plus Windows, browser and security gates before publication.

For browser release preparation, `ekstenzije/store/listing.json` must use the same semantic version as every active browser manifest. Its `developmentChannel` is release metadata and follows the exact form `v<extensionVersion>-release`; store-readiness validation derives that value from `extensionVersion` rather than hardcoding a historical release. Package names, permission justifications, privacy disclosures and prepared store assets must remain aligned with the current browser sources.

External store acceptance is a publisher/reviewer state, not a repository CI result. A version-aligned ZIP and store kit must not be described as published until a real store listing is available.
