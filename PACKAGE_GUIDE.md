# Unity Package Manager Installation Guide

This guide explains how to install and use the Autech AdMob Mediation package as a Unity Package Manager (UPM) package.

## What is UPM?

Unity Package Manager (UPM) is Unity's official package management system. Installing via UPM provides several benefits:

- **Automatic Updates**: Easy version management
- **Clean Project Structure**: Package files stay separate from project assets
- **Dependency Management**: Automatic dependency resolution
- **Samples System**: Import examples only when needed
- **Git Integration**: Install directly from GitHub

## Installation Methods

### Method 1: Git URL (Recommended)

**Fastest way to get started:**

1. Open Unity Package Manager: `Window > Package Manager`
2. Click the `+` button → `Add package from git URL...`
3. Enter: `https://github.com/HaseebDev/Admob-Mediation-Package.git`
4. Click `Add`

Unity will automatically clone the package and add it to your project.

### Method 2: Install Specific Version

To install a specific version or branch:

```
# Latest version
https://github.com/HaseebDev/Admob-Mediation-Package.git

# Specific version tag
https://github.com/HaseebDev/Admob-Mediation-Package.git#v2.0.3

# Specific branch
https://github.com/HaseebDev/Admob-Mediation-Package.git#develop
```

### Method 3: Local Package

For development or offline use:

1. Clone the repository:
   ```bash
   git clone https://github.com/HaseebDev/Admob-Mediation-Package.git
   ```

2. In Unity Package Manager:
   - Click `+` → `Add package from disk...`
   - Navigate to cloned folder
   - Select `package.json`

### Method 4: manifest.json

Directly edit your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.autech.admob-mediation": "https://github.com/HaseebDev/Admob-Mediation-Package.git",
    "com.google.ads.mobile": "10.4.2",
    "com.google.ads.mobile.mediation.unity": "3.15.0"
  },
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.google.ads.mobile",
        "com.google.external-dependency-manager"
      ]
    }
  ]
}
```

## Package Structure

When installed as a UPM package, the structure is:

```
Packages/
└── Autech AdMob Mediation/
    ├── package.json          # Package manifest
    ├── README.md             # Main documentation
    ├── CHANGELOG.md          # Version history
    ├── LICENSE.md            # License information
    ├── Runtime/              # Core scripts
    │   ├── Scripts/
    │   │   ├── AdsManager.cs
    │   │   ├── VerifyAdmob.cs
    │   │   ├── AdsExampleUI.cs
    │   │   └── ConsentUIExample.cs
    │   └── Autech.Admob.Runtime.asmdef
    ├── Documentation~/       # Documentation files
    │   ├── ComprehensiveImplementationGuide.md
    │   ├── QuickImplementationChecklist.md
    │   ├── MediationConsentGuide.md
    │   └── GoogleCertifiedCMPImplementation.md
    └── Samples~/            # Example content (import as needed)
        ├── ExampleScene/
        │   ├── ExampleAdsScene.unity
        │   └── README.md
        └── Prefabs/
            ├── VerifyandInitializeAdmob.prefab
            └── README.md
```

## Working with Samples

### Importing Samples

Samples are optional and won't clutter your project until imported:

1. Open Package Manager
2. Select "Autech AdMob Mediation" package
3. Expand `Samples` section
4. Click `Import` next to the sample you want:
   - **Prefabs**: Essential prefab for quick setup
   - **Example Scene and UI**: Complete example implementation

### Where Samples Are Imported

Imported samples appear in your project's `Assets/Samples/` folder:

```
Assets/
└── Samples/
    └── Autech AdMob Mediation/
        └── 2.0.3/
            ├── ExampleScene/
            └── Prefabs/
```

### Updating Samples

When you update the package, reimport samples to get the latest versions.

## Dependencies

This package requires:

### Required Dependencies

```json
"com.google.ads.mobile": "10.4.2"
"com.google.ads.mobile.mediation.unity": "3.15.0"
```

### Installing Dependencies

**Option A: Automatic** (may not always work)
- Dependencies should install automatically when you add the package

**Option B: Manual via OpenUPM**
```bash
# Install OpenUPM CLI
npm install -g openupm-cli

# Navigate to your project
cd your-unity-project/

# Install dependencies
openupm add com.google.ads.mobile
openupm add com.google.ads.mobile.mediation.unity
```

**Option C: Manual via Package Manager**
1. Add OpenUPM registry to your project:
   - Edit `Packages/manifest.json`
   - Add the scoped registry (see Method 4 above)
2. In Package Manager:
   - Select "My Registries"
   - Find and install Google Mobile Ads packages

## Quick Start

### 5-Minute Setup

1. **Install Package** (see installation methods above)

2. **Install Dependencies** (OpenUPM recommended)
   ```bash
   openupm add com.google.ads.mobile
   openupm add com.google.ads.mobile.mediation.unity
   ```

3. **Import Prefabs Sample**
   - Package Manager → Autech AdMob Mediation → Samples → Prefabs → Import

4. **Add to Scene**
   - Drag `Assets/Samples/.../Prefabs/VerifyandInitializeAdmob.prefab` into scene

5. **Configure**
   - Select prefab in Hierarchy
   - Set your Ad Unit IDs in Inspector
   - Configure consent settings

6. **Test**
   - Press Play
   - Check console for initialization messages

## Updating the Package

### Update to Latest Version

1. In Package Manager, select the package
2. Click `Update` if available

Or remove and re-add via Git URL to force update.

### Update to Specific Version

1. Remove the package
2. Re-add with version tag:
   ```
   https://github.com/HaseebDev/Admob-Mediation-Package.git#v2.0.3
   ```

### Check Current Version

- Package Manager → Select package → Version shown in details
- Or check `Packages/manifest.json`

## Troubleshooting

### Package Not Found

**Issue**: "No 'git' executable was found"

**Solution**:
- Install Git: https://git-scm.com/
- Restart Unity after installation

### Dependencies Not Installing

**Issue**: Google Mobile Ads packages not found

**Solution**:
- Add OpenUPM registry to manifest.json (see Method 4)
- Or install manually via OpenUPM CLI

### Assembly Definition Errors

**Issue**: "Assembly 'Autech.Admob.Runtime' has reference to 'GoogleMobileAds.Core' but assembly not found"

**Solution**:
- Ensure Google Mobile Ads SDK is properly installed
- Try reimporting the package
- Check Package Manager for errors

### Samples Not Showing

**Issue**: Can't see samples in Package Manager

**Solution**:
- Package must be installed (not embedded) to show samples
- If embedded, samples are in `Packages/com.autech.admob-mediation/Samples~/`
- Copy manually to `Assets/Samples/`

### Build Errors After Update

**Issue**: Build fails after package update

**Solution**:
1. Close Unity
2. Delete `Library/` folder
3. Reopen Unity (will reimport everything)
4. Rebuild

## Package Development

### Testing Local Changes

1. Clone repository
2. Make changes in cloned folder
3. In Unity: Package Manager → `+` → Add package from disk
4. Select `package.json` from your cloned folder
5. Changes will reflect immediately

### Submitting Changes

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## Migration from UnityPackage

If you previously used the `.unitypackage` version:

1. **Backup your project** (important!)

2. **Remove old assets**:
   - Delete `Assets/Admob/` folder
   - Remove any scripts you copied

3. **Install via UPM** (see installation methods)

4. **Import Prefabs sample**

5. **Update references**:
   - Drag the new prefab into scenes
   - Update any custom scripts referencing the package

6. **Test thoroughly**:
   - Check all scenes
   - Test ad functionality
   - Verify consent flow

## Best Practices

### Version Control

Add to `.gitignore`:
```
# Unity Package Manager
Packages/
Library/

# Keep your manifest
!Packages/manifest.json
!Packages/packages-lock.json
```

### Updates

- Test updates in a separate branch first
- Review CHANGELOG.md before updating
- Check for breaking changes

### Samples

- Import samples to Assets/Samples/
- Modify as needed for your project
- Original samples stay in package, can reimport anytime

### Dependencies

- Keep Google Mobile Ads SDK updated
- Check compatibility before Unity upgrades
- Test on both Android and iOS after updates

## Advanced Configuration

### Assembly Definitions

The package includes assembly definitions for faster compilation:

- `Autech.Admob.Runtime.asmdef` - Runtime scripts

If you need to reference this package in your code:
1. Add reference in your assembly definition
2. Or place scripts in `Assets/Scripts/` (auto-references packages)

### Platform-Specific Code

The package uses preprocessor directives:
- `UNITY_ANDROID` - Android-specific code
- `UNITY_IOS` - iOS-specific code
- `ADMOB_INSTALLED` - AdMob SDK check
- `UNITY_ADS_MEDIATION` - Unity Ads mediation check

### Conditional Compilation

```csharp
#if ADMOB_INSTALLED
    // Code that requires AdMob SDK
#endif

#if UNITY_ADS_MEDIATION
    // Code that requires Unity Ads mediation
#endif
```

## Support

- **Documentation**: See `Documentation~/` in package
- **Issues**: https://github.com/HaseebDev/Admob-Mediation-Package/issues
- **Releases**: https://github.com/HaseebDev/Admob-Mediation-Package/releases
- **Wiki**: https://github.com/HaseebDev/Admob-Mediation-Package/wiki

## Version History

See [CHANGELOG.md](CHANGELOG.md) for detailed version history.

## License

This package is licensed under the MIT License. See [LICENSE.md](LICENSE.md) for details.
