# Contributing to SNAPVERE

SNAPVERE 0.1.4 is the current public release. The actively maintained product surface is **Windows plus Chrome, Edge, Opera and Firefox**.

Changes should improve correctness, reliability, accessibility, performance, security or maintainability without weakening the local-first model, capture ownership rules, package integrity or evidence gates.

## Before changing code

Keep the scope explicit. A production change should identify the user-visible or technical problem it solves, preserve unrelated behavior and add regression coverage when the failure can be automated.

Do not weaken timeouts, security checks, visual thresholds, permission restrictions, lifecycle assertions or package gates merely to obtain a green build. Fix the regression instead.

## Branch and pull-request workflow

- Branch from the current `main`.
- Keep one coherent concern per pull request where practical.
- Avoid unrelated formatting or generated-file churn.
- Explain the finding, the fix, the validation boundary and any behavior intentionally left unchanged.
- Merge only after the relevant CI, Product Contract and security checks are green.
- Never force-move published tags or replace assets under an existing release tag.

## Windows validation

Run the core validation locally when the required Windows toolchain is available:

```powershell
dotnet restore SNAPVERE.sln
dotnet build SNAPVERE.sln -c Release -p:Platform=x64
dotnet test tests/Snapvere.UnitTests/Snapvere.UnitTests.csproj -c Release -p:Platform=x64
```

CI additionally validates:

- x86 build;
- ARM64 cross-build;
- native payload structure;
- rendered Region/Window/Tray/Options/Language/About UI snapshots;
- visual comparison against the successful `main` baseline;
- universal Setup and Portable generation;
- package-size regression budgets;
- real x64/x86 Setup and Portable lifecycle completion.

Changes affecting capture geometry, DPI, paths, payload integrity, file publication, single-instance behavior or Setup/Portable lifecycle should include focused unit or lifecycle regression coverage when feasible.

## Browser validation

```bash
node ekstenzije/tools/validate-extensions.mjs
node ekstenzije/tools/verify-extension-parity.mjs
node ekstenzije/tools/smoke-test-background.mjs
node ekstenzije/tools/validate-store-readiness.mjs
bash ekstenzije/tools/package-extensions.sh
```

Browser changes must preserve:

- the exact permission allow-list: `activeTab`, `scripting`, `downloads`, `storage`;
- no broad host permissions or remote runtime code;
- locked SNAPVERE product identity and filename prefix;
- EN/HR resource parity;
- active-tab/window ownership checks around frame acquisition;
- bounded full-page tile/canvas/pixel behavior;
- capture-lock cleanup on success, cancellation and failure;
- source parity across variants except documented Firefox manifest/runtime differences;
- deterministic ZIP packaging.

A GitHub release ZIP is not evidence that an extension has been accepted by an external browser store. Do not claim store publication without a real listing.

## Product contract and documentation

Run:

```bash
python3 eng/validate-product-contract.py
```

`product-version.json` is the canonical active Windows/browser product contract. It governs the current version, supported platforms and six maintained package names.

`RELEASES.md` is the canonical detailed release-history file. Historical sections describe what was true at the time of publication and must not be rewritten to match today's product. `CHANGELOG.md` carries the concise engineering history, including an `Unreleased` section for merged post-release work.

Do not create a new root `RELEASE_NOTES_<version>.md` file for future releases. A future binary publication must use a new SemVer version/tag and update version sources, product contract, current EN/HR documentation, CHANGELOG and RELEASES together.

## Security and privacy

Never commit or post:

- private screenshots or capture samples containing user data;
- credentials, cookies, API tokens or signing keys;
- production secrets or publisher credentials;
- crash dumps/logs containing unrelated sensitive material;
- local caches, temporary build output or development-only artifacts.

Security-sensitive findings should follow [SECURITY.md](SECURITY.md), not a public issue when disclosure would expose an unfixed vulnerability.

## Definition of done

A change is ready when its behavior is understandable from the diff, relevant tests/gates pass, user-facing copy is production quality, privacy/security boundaries remain accurate and documentation reflects the product that actually exists rather than a planned or assumed state.
