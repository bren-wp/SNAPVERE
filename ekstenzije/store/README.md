# Browser Store Submission Kit

This directory contains the version-controlled material needed to submit the post-v0.1.0 SNAPVERE browser extensions to Chrome Web Store, Microsoft Edge Add-ons, Opera Add-ons and Mozilla Add-ons (AMO).

## Contents

- `listing.json` — canonical EN/HR listing copy, single-purpose statement, permission justifications, privacy declarations and exact ZIP names.
- `reviewer-notes.md` — functional test path and reviewer-specific notes.
- `assets/` — store screenshots and promotional graphics in the dimensions used by the target stores.
- `../PRIVACY.md` — public privacy policy suitable for linking from store dashboards.

The extension packages themselves are produced by `ekstenzije/tools/package-extensions.sh` and GitHub Actions as:

- `SNAPVERE-Chrome.zip`
- `SNAPVERE-Edge.zip`
- `SNAPVERE-Opera.zip`
- `SNAPVERE-Firefox.zip`

## Submission boundary

Repository automation can validate and package the extension, but it intentionally does not claim store publication. Final publication requires authenticated publisher access in each external store and completion of the store's own review/signing process.

The missing external actions are:

1. Chrome Web Store — sign in to the publisher dashboard, create/choose the item, upload `SNAPVERE-Chrome.zip`, apply the listing/privacy data and graphics from this directory, choose distribution, and submit for review.
2. Microsoft Edge Add-ons — use an enrolled Edge developer/Partner Center account, upload `SNAPVERE-Edge.zip`, enter properties/privacy/listing data, and submit for certification.
3. Opera Add-ons — sign in to the Opera extension repository, upload `SNAPVERE-Opera.zip`, provide the listing metadata/screenshots, and submit for moderator review.
4. Mozilla Add-ons — sign in to AMO, upload `SNAPVERE-Firefox.zip`, provide listing/reviewer information, and let Mozilla validate, review and sign the add-on. Release/Beta Firefox requires Mozilla signing.

Do not invent store item IDs, approval status, signatures, reviewer decisions or publication URLs before the corresponding store actually supplies them.

## Current official references

- Chrome Web Store preparation: https://developer.chrome.com/docs/webstore/prepare
- Chrome privacy fields: https://developer.chrome.com/docs/webstore/cws-dashboard-privacy
- Chrome listing images: https://developer.chrome.com/docs/webstore/images
- Microsoft Edge publishing: https://learn.microsoft.com/en-us/microsoft-edge/extensions/publish/publish-extension
- Opera publishing guidelines: https://help.opera.com/en/extensions/publishing-guidelines/
- Opera acceptance criteria: https://help.opera.com/en/extensions/acceptance-criteria/
- Mozilla submission: https://extensionworkshop.com/documentation/publish/submitting-an-add-on/
- Mozilla policies: https://extensionworkshop.com/documentation/publish/add-on-policies/
- Mozilla signing: https://extensionworkshop.com/documentation/publish/signing-and-distribution-overview/
