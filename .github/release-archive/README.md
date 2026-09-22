# Archived SNAPVERE release workflows

The version-specific release workflows in this directory are retained only as immutable historical build/release references.

They are intentionally stored outside `.github/workflows`, so GitHub Actions does not register or execute them on ordinary pushes. Published tags and GitHub Release assets for these versions are not changed by this archive move.

The archive retains the version-specific publication workflows through **v0.1.13**, including the historical v0.1.0 CI-signed transition workflow. Active development automation belongs under `.github/workflows`; a version-specific release workflow should remain active only while that release is actually being prepared and published.

Detailed release history is consolidated in [`../../RELEASES.md`](../../RELEASES.md). Future releases should add their detailed notes there rather than creating a new root `RELEASE_NOTES_<version>.md` file. A new release may use a newly reviewed version-specific publication workflow while it is being prepared; after the release is published and verified, that workflow should be archived here.

Archived workflow source may reference historical files, package contracts, licenses or tooling that existed at that release. It is audit evidence, not current executable release automation.
