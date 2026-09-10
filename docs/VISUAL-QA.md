# Rendered UI visual QA

SNAPVERE treats rendered Windows UI as a CI-tested product surface. The visual-QA path uses the real x64 `Snapvere.exe` produced by the workflow; SVG illustrations and generated mockups are not accepted as runtime screenshots.

## Captured surfaces

`eng/Capture-SnapvereVisualQa.ps1` launches dedicated probe modes and captures exactly six rendered PNG files:

- `region-capture.png`
- `window-capture.png`
- `tray-menu.png`
- `options.png`
- `language.png`
- `about.png`

The capture script also writes `manifest.json` with the rendered dimensions, PNG byte size and SHA-256 digest for every surface. A missing, visually empty or unexpectedly small frame fails the workflow before packaging continues.

## Pull-request regression baseline

Pull-request CI uses the most recent successful `main` push that still has a non-expired `snapvere-visual-qa-<sha>` artifact. The workflow downloads that artifact directly through GitHub Actions and compares it with the current PR snapshots using `eng/Compare-SnapvereVisualQa.ps1`.

The comparator first enforces the six-file artifact contract and manifest coverage. It then compares each matching surface after normalizing both images to a fixed sample grid. The gate records:

- baseline and current dimensions;
- width and height drift ratios;
- PNG byte-size ratio;
- normalized mean RGB difference;
- ratio of materially changed sampled pixels;
- pass/fail reasons per surface.

The generated `comparison.json` is uploaded together with the current rendered PNGs and manifest.

## Default regression policy

The default policy intentionally tolerates small anti-aliasing and hosted-runner rendering variation while rejecting large accidental changes:

| Check | Default limit |
| --- | ---: |
| width drift | 8% |
| height drift | 8% |
| normalized mean RGB difference | 0.18 |
| materially changed sampled pixels | 70% |
| significant sampled-pixel threshold | 0.20 |
| PNG byte-size ratio | 0.35–3.0 |
| normalized sample grid | 64×64 |

These values are a regression safety boundary, not a design-quality score. Spacing, typography, icon sizing, rounded corners and alignment still require review of the actual uploaded PNG artifact. Do not weaken the limits merely to make a failing PR green; first determine whether the change is intentional, runner noise or a real UI regression.

## Baseline retention

Current visual-QA artifacts are retained for 90 days. PR CI checks recent successful `main` runs and selects the first matching artifact that has not expired. If no valid rendered baseline exists, the regression step fails explicitly instead of silently skipping comparison.

## What this proves

A green visual-QA regression step proves that:

- the x64 application rendered all six required surfaces on the GitHub Windows runner;
- the screenshots are non-empty and satisfy the artifact contract;
- the current PR is within the configured visual drift limits relative to a successful `main` runtime snapshot;
- a machine-readable comparison report was produced.

It does **not** prove physical ARM64 runtime behavior, every mixed-DPI topology, every graphics driver, every Windows theme/configuration or pixel-perfect parity with a human design reference. ARM64 remains cross-built and structurally validated on the hosted x64 runner unless a real ARM64 runtime job is explicitly introduced.

## Reviewing an intentional UI change

For an intentional visual adjustment:

1. inspect the PR's uploaded `snapvere-visual-qa-<sha>` artifact;
2. compare the affected real PNG with the current SNAPVERE design reference;
3. verify that the change does not expose placeholder controls or unimplemented features;
4. keep the regression policy unchanged unless there is a documented technical reason to alter it;
5. merge only after the final PR head and all CI jobs are green.

After merge, the next successful `main` visual artifact naturally becomes the baseline for subsequent pull requests.
