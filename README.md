# Snapvere

**Capture. Record. Create.** A screenshot and screen recording app by **Brendigo**.

**Website:** https://snapvere.com · **Author:** https://brendigo.com · **GitHub:** https://github.com/bren-wp/Snapvere

## Features

- Screenshot regions, entire displays and active windows
- Annotation and blur/redaction before saving
- MP4 and GIF recordings with audio and cursor controls
- Searchable offline local gallery and preview player
- Configurable shortcuts, export preferences and autostart

Built with **Tauri v2, Rust, React, TypeScript and SQLite**.

## Build

Node.js 20+, Rust stable and Tauri v2 native prerequisites are required.

```sh
cd apps/desktop
npm ci
npm run build
npm run tauri build
```

The release pipeline creates Windows and macOS artifacts. Artifacts are not yet code-signed; verify builds and distribution rights before any public release.

## Project structure

- `apps/desktop/`: desktop frontend and Rust backend
- `site/`: website, download page, privacy policy
- `docs/`: specification and task breakdown
- `.github/workflows/release.yml`: build and release configuration

## Credits

**Snapvere brand and product maintainer:** [Brendigo](https://brendigo.com). **Official website:** [snapvere.com](https://snapvere.com).

Based on the user-supplied **RvgeShot** snapshot, which matches original source [notbrec/RvgeShot](https://github.com/notbrec/RvgeShot) at commit `99976bc3500f236c49459a917e38b91e33ddbaeb`. Original authors retain their rights. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
