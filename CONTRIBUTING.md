# Contributing to SNAPVERE

SNAPVERE 0.1.1 is maintained as a production capture product for Windows, Chrome, Edge, Opera and Firefox.

Changes should improve correctness, reliability, accessibility, performance, security or maintainability without weakening the local-first model.

## Windows validation

```powershell
dotnet restore SNAPVERE.sln
dotnet build SNAPVERE.sln -c Release -p:Platform=x64
dotnet test tests/Snapvere.UnitTests/Snapvere.UnitTests.csproj -c Release -p:Platform=x64
```

CI additionally builds x86/ARM64, validates rendered UI, universal Setup/Portable lifecycle and package-size budgets.

## Browser validation

```bash
node ekstenzije/tools/validate-extensions.mjs
node ekstenzije/tools/verify-extension-parity.mjs
node ekstenzije/tools/smoke-test-background.mjs
node ekstenzije/tools/validate-store-readiness.mjs
bash ekstenzije/tools/package-extensions.sh
```

Browser changes must preserve the exact permission allow-list, locked SNAPVERE branding, EN/HR resource parity, bounded capture memory, active-tab ownership checks and reproducible packaging.

## Product contract

```bash
python3 eng/validate-product-contract.py
```

`product-version.json` is the canonical active Windows/browser product contract. Historical release notes stay in `RELEASES.md`.

Do not weaken timeouts, security checks, visual thresholds or package gates merely to obtain a green build. Fix the regression. Never commit private screenshots, credentials, signing keys, tokens or crash dumps containing user material.
