<div align="center">

<img src="site/icon.png" width="108" alt="Snapvere logo" />

# Snapvere

### Capture what matters. Record with confidence. Create without distraction.

**An all-in-one screenshot & screen recording app by [Brendigo](https://brendigo.com)**

[🌐 Official website](https://snapvere.com) · [📦 Download Snapvere](https://github.com/bren-wp/Snapvere/releases/tag/v0.2.4) · [🔏 Privacy](https://snapvere.com/privacy/) · [🐛 Report an issue](https://github.com/bren-wp/Snapvere/issues)

<img src="site/og.png" width="850" alt="Snapvere — Capture. Record. Create. Brendigo brand artwork"/>

</div>

---

## ✨ See Snapvere in action

Screenshots below are **actual renders of the project's React interface**, recorded by the automated UI preview workflow. The preview uses a local substitute for Tauri's IPC bridge (so capture/recording is **not** exercised by these images). No fictional recordings or customer data are inserted.

<table>
<tr><td width="50%"><img src="docs/screenshots/home.png" alt="Snapvere desktop library with capture toolbar" /></td><td width="50%"><img src="docs/screenshots/settings.png" alt="Snapvere settings interface" /></td></tr>
<tr><td align="center"><b>📸 Capture & Gallery</b><br/>Find your screenshots and start recording.</td><td align="center"><b>⚙️ Customize Your Workflow</b><br/>Privacy, shortcuts, export and recording controls.</td></tr>
</table>

<details>
<summary><b>🌍 Preview the official website</b></summary>

![Snapvere website (live HTML render)](docs/screenshots/website.png)

</details>

## 🚀 Features

| | Capability | Description |
|---|---|---|
| 📷 | **Fast screenshots** | Capture a region, full display or active window. |
| 🎬 | **Screen recording** | MP4 and GIF workflows with adjustable quality, cursor and optional audio. |
| ✏️ | **Edit & protect** | Annotate, crop, blur and redact before sharing. |
| 🗂️ | **Private gallery** | Search and manage captures from a local library. |
| ⌨️ | **Custom hotkeys** | Capture and record without breaking your workflow. |
| 🔐 | **Local-first** | Local files and SQLite; no account needed for basic capture. |

## ⬇️ Download Snapvere v0.2.4

Three Windows editions are built **from the same source revision**, and the release process validates their presence and publishes SHA-256 checksums.

| Edition | Best for | Download |
|---|---|---|
| 🟢 **Portable** | Run without an installer | [**Snapvere-Portable.exe**](https://github.com/bren-wp/Snapvere/releases/download/v0.2.4/Snapvere-Portable.exe) |
| 🖥️ **EXE installer** | Quick setup with NSIS | [**Snapvere-Setup.exe**](https://github.com/bren-wp/Snapvere/releases/download/v0.2.4/Snapvere-Setup.exe) |
| 🧰 **MSI installer** | Managed Windows installations | [**Snapvere-Setup.msi**](https://github.com/bren-wp/Snapvere/releases/download/v0.2.4/Snapvere-Setup.msi) |

**Requirements:** Windows 10/11 (64-bit) with the Microsoft WebView2 Runtime. Portable means **no Snapvere installation** is required; a missing system WebView2 runtime may still need provisioning. These preview builds are unsigned, so Windows SmartScreen may warn.

### 🛡️ Verify your download

Download [SHA256SUMS.txt](https://github.com/bren-wp/Snapvere/releases/download/v0.2.4/SHA256SUMS.txt) from the **same release**, then verify in PowerShell:

```powershell
Get-FileHash .\Snapvere-Portable.exe -Algorithm SHA256
```

Compare the result with the entry in `SHA256SUMS.txt`. Do not use mismatched downloads.

## 💎 Designed for a simpler capture workflow

1. **Capture** an area of the screen or begin recording from the toolbar.
2. **Refine** a screenshot with cropping, annotation or privacy tools.
3. **Organize** captures in the local gallery.
4. **Export or share** the result when you're ready.

## 🧑‍💻 Build from source

**Tech:** Tauri 2, Rust, React, TypeScript, Vite and SQLite.

```bash
cd apps/desktop
npm ci
npm run build
npm run tauri -- build --bundles nsis,msi
```

Windows builds also require Rust with the MSVC toolchain and Tauri's WiX/NSIS dependencies. On macOS, the relevant Tauri platform prerequisites apply. See [the Windows release workflow](.github/workflows/release.yml) for the reproducible build procedure.

```
apps/desktop/              React interface and native Rust backend
site/                      Official website, privacy and download pages
docs/screenshots/          Automated UI render screenshots
tools/capture_ui.py        Preview capture from actual frontend source
.github/workflows/         Source import, screenshot and release pipelines
```

## ❤️ About and privacy

**Snapvere** is a product brand maintained by **[Brendigo](https://brendigo.com)**. The official site is **[snapvere.com](https://snapvere.com)**. The application is local-first: normal screenshot storage does not require a cloud account.

---

<div align="center">

**SNAPVERE** · Designed and maintained by **BRENDIGO**

[🌐 snapvere.com](https://snapvere.com) · [🔗 brendigo.com](https://brendigo.com) · [⭐ Star the project](https://github.com/bren-wp/Snapvere)

</div>
