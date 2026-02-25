# AGENTS.md

## Project Overview

This is **Autech AdMob Mediation** (`com.autech.admob-mediation`) — a Unity Package Manager (UPM) package providing AdMob + Unity Ads mediation for Unity projects. It is a C# library, not a standalone application or web service.

- **Language**: C#
- **Engine**: Unity 2020.3+ (developed with Unity 6000.2.6f2)
- **Package format**: UPM Git-based package
- **Namespace**: `Autech.Admob`

### Key directories

| Path | Purpose |
|------|---------|
| `Runtime/Scripts/` | Core package scripts (AdsManager, ad controllers, consent, persistence) |
| `Editor/` | Editor-only tooling (DependencyInstaller) |
| `Samples~/` | Example scenes and prefabs importable via UPM |
| `Assets/Admob/` | Legacy `.unitypackage` sources (tracked for diffing) |
| `Packages/manifest.json` | Unity project manifest with all dependencies |

## Cursor Cloud specific instructions

### Environment limitations

This codebase is a Unity UPM package — all C# code depends on Unity assemblies (`UnityEngine`, `UnityEditor`) and Google Mobile Ads SDK. Full compilation, testing, and running require Unity Editor which is not available in the headless Cloud Agent VM. The codebase has no standalone CLI build system, no npm scripts, no backend server, and no Docker setup.

### What you CAN do in the Cloud Agent VM

1. **JSON validation**: All JSON files (`package.json`, `Packages/manifest.json`, `*.asmdef`) can be validated with `jq`.
2. **C# syntax/formatting checks**: .NET SDK 8.0 is installed at `~/.dotnet`. Use CSharpier for C# formatting validation:
   ```
   export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
   csharpier check Runtime/Scripts/ Editor/ Samples~/
   ```
   CSharpier will parse all C# files and report syntax errors alongside formatting differences. Formatting differences (line wrapping, trailing commas) are style-only and do not indicate broken code.
3. **UPM package structure checks**: Verify required files exist (`package.json`, `README.md`, `CHANGELOG.md`, `LICENSE.md`, `*.asmdef`, `*.meta` files).

### What you CANNOT do

- Compile C# against Unity assemblies (requires Unity Editor)
- Run Unity Test Framework tests (requires Unity Editor)
- Build Android/iOS targets (requires Unity Editor + platform SDKs)
- Test actual ad loading (requires physical device or emulator)

### Code structure notes

- All runtime scripts use `#if ADMOB_INSTALLED` / `#if UNITY_ADS_MEDIATION` preprocessor guards defined via version defines in `Runtime/Autech.Admob.Runtime.asmdef`.
- The `DependencyInstaller.cs` in `Editor/` auto-checks and installs Google SDK dependencies when the package is imported into a Unity project.
- `package.json` is a UPM package manifest (not an npm package). It has no `scripts` section.
- See `README.md` for architecture overview and `INSTALL.md` for installation steps.
