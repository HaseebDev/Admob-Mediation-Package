# Known Issues

## Unity Console Warnings About Missing Meta Files

When installing this package via Unity Package Manager, you may see warnings like:

```
Asset Packages/com.autech.admob-mediation/releases has no meta file, but it's in an immutable folder. The asset will be ignored.
Asset Packages/com.autech.admob-mediation/CHANGELOG.md has no meta file, but it's in an immutable folder. The asset will be ignored.
```

### Why This Happens

These warnings occur because the repository contains additional files (like `releases/` folder and documentation) that are meant for GitHub users but not needed in the Unity package. Unity shows warnings for files without `.meta` files in package folders.

### Is This a Problem?

**No, these warnings are harmless** and can be safely ignored. They indicate that Unity is correctly ignoring files that aren't part of the package runtime.

### What's Being Ignored

- `releases/` - Legacy UnityPackage downloads (for GitHub releases)
- `Assets/`, `Packages/`, `ProjectSettings/` - Unity project files
- Documentation files at root level (already available in `Documentation~/`)
- IDE configuration folders (`.vscode`, `.vs`, etc.)

### What's Working

The package **IS** correctly installing:
- ✅ `Runtime/Scripts/` - All core scripts (AdsManager, VerifyAdmob, etc.)
- ✅ `Runtime/Autech.Admob.Runtime.asmdef` - Assembly definition
- ✅ `Samples~/` - Example scenes and prefabs (importable)
- ✅ `Documentation~/` - Implementation guides

### How to Verify Everything Works

1. **Check Package Manager**:
   - Window > Package Manager
   - Select "Autech AdMob Mediation"
   - You should see the package version and samples

2. **Check Scripts Are Available**:
   - Create a new C# script
   - Try using: `using Autech.Admob;`
   - Type: `AdsManager.Instance`
   - If autocomplete works, scripts are loaded ✅

3. **Import Samples**:
   - In Package Manager, expand "Samples"
   - Click "Import" on Prefabs
   - Check `Assets/Samples/Autech AdMob Mediation/`
   - You should see the prefab ✅

4. **Check Assembly Definition**:
   - In Project window, search: `t:asmdef Autech`
   - You should find `Autech.Admob.Runtime`
   - Click it to verify references are set ✅

### If Scripts Are Actually Missing

If you truly cannot find the scripts (not just warnings), try:

1. **Reimport Package**:
   ```
   Package Manager → Remove package
   Then re-add via Git URL
   ```

2. **Clear Package Cache**:
   ```
   Close Unity
   Delete: Library/PackageCache/com.autech.admob-mediation@*
   Reopen Unity
   ```

3. **Check Dependencies**:
   ```
   Ensure Google Mobile Ads SDK is installed
   Window > Package Manager > My Registries
   Install: Google Mobile Ads (10.4.2+)
   ```

### Future Improvements

We plan to address these warnings by:
- Creating a separate `upm` branch with only package files
- Users would install from: `https://github.com/HaseebDev/Admob-Mediation-Package.git#upm`
- This branch would not have `releases/` or project files

### Need Help?

If you encounter actual issues (not just warnings):
1. Check [INSTALL.md](INSTALL.md) for installation help
2. Verify dependencies are installed
3. Try reimporting the package
4. [Open an issue](https://github.com/HaseebDev/Admob-Mediation-Package/issues) with:
   - Unity version
   - Installation method used
   - Steps to reproduce
   - Screenshots of actual errors (not warnings)

## Other Known Issues

### Dependency Installation

**Issue**: Dependencies don't auto-install
**Solution**: Install manually via OpenUPM (see [INSTALL.md](INSTALL.md))

### Compilation Errors After Import

**Issue**: "Assembly not found" errors
**Solution**:
1. Ensure Google Mobile Ads SDK is installed
2. Restart Unity
3. Reimport package if needed

### Samples Not Showing

**Issue**: Can't see samples in Package Manager
**Solution**: Package must be installed (not embedded). If using local package, samples are in `Samples~/` folder.
