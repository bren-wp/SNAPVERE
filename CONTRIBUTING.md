# Contributing to SNAPVERE

SNAPVERE is developed as a production Windows application. Contributions should improve a real user workflow, correctness, reliability, accessibility, performance, security or maintainability.

## Requirements

- Windows 11 development environment
- .NET SDK pinned by `global.json`
- Windows application development prerequisites for WinUI 3
- x64 build capability; ARM64 changes must preserve ARM64 compatibility

## Before opening a change

1. Keep capture, imaging and UI responsibilities separated.
2. Do not add fake or nonfunctional controls to stable UI.
3. Do not hardcode 96-DPI or primary-monitor assumptions.
4. Avoid blocking the UI thread for capture, encoding, OCR or storage work.
5. Add tests for geometry/data transformations and regressions when practical.
6. Keep documentation aligned with actual implementation status.

## Validation

```powershell
dotnet restore SNAPVERE.sln
dotnet build SNAPVERE.sln -c Release -p:Platform=x64
dotnet test tests/Snapvere.UnitTests/Snapvere.UnitTests.csproj -c Release -p:Platform=x64
```

## Commit style

Use specific imperative messages such as:

- `Implement mixed-DPI display mapping`
- `Add Windows graphics capture backend`
- `Harden capture frame validation`

Avoid generic messages such as `update`, `fix`, `changes`, `final` or `stuff`.

## Sensitive data

Never commit screenshots containing private data, signing keys, production credentials, license secrets, debug dumps or user runtime data.
