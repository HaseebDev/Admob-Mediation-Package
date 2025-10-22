# Quick Installation Guide

Follow these steps **in order** to install the package successfully.

## ⚠️ Important: Install Dependencies FIRST

Unity Package Manager cannot automatically install dependencies from OpenUPM. You **must** install the required Google Mobile Ads packages **before** installing this package.

## Step-by-Step Installation

### Step 1: Add OpenUPM Scoped Registry

1. Open your Unity project
2. Go to `Edit > Project Settings > Package Manager`
3. Add a new Scoped Registry with these details:
   - **Name**: `package.openupm.com`
   - **URL**: `https://package.openupm.com`
   - **Scope(s)**:
     - `com.google.ads.mobile`
     - `com.google.external-dependency-manager`

4. Click **Save**

### Step 2: Install Google Mobile Ads SDK

1. Open Package Manager: `Window > Package Manager`
2. Change dropdown to **My Registries**
3. Find **Google Mobile Ads**
4. Click **Install**
5. Wait for installation to complete

**Version Required**: 10.4.2 or higher

### Step 3: Install Unity Ads Mediation

1. In Package Manager, still on **My Registries**
2. Find **Google Mobile Ads Unity Ads Mediation**
3. Click **Install**
4. Wait for installation to complete

**Version Required**: 3.15.0 or higher

### Step 4: Install This Package

Now you can install the Autech AdMob Mediation package:

1. In Package Manager, click the **+** button
2. Select **Add package from git URL...**
3. Enter: `https://github.com/HaseebDev/Admob-Mediation-Package.git`
4. Click **Add**

✅ **Success!** The package should now install without errors.

## Alternative: OpenUPM CLI Method

If you have Node.js installed, this is the fastest method:

```bash
# Install OpenUPM CLI globally
npm install -g openupm-cli

# Navigate to your Unity project
cd path/to/your/unity/project

# Install dependencies
openupm add com.google.ads.mobile
openupm add com.google.ads.mobile.mediation.unity

# Then add this package via Package Manager Git URL
```

## Troubleshooting

### "Package cannot be found" Error

This means dependencies aren't installed yet. Go back to Step 1 and ensure:
- OpenUPM scoped registry is added correctly
- Google Mobile Ads SDK is installed
- Unity Ads Mediation is installed

### Windows Permission Error (EPERM)

**Error**: `Failed to rename...error code [EPERM]`

**If you see this error**, it means you're using an older version of the repository. This has been fixed!

**Solution**:
1. Remove the package if partially installed
2. Close Unity
3. Delete: `Library/PackageCache/com.autech.admob-mediation@*`
4. Reopen Unity
5. Try installing again - the issue is now resolved in the latest version

**What was the problem?**
- Earlier versions had problematic files that Windows couldn't handle
- These have been removed from the repository
- Latest version installs cleanly on Windows

### OpenUPM Registry Not Showing Packages

1. Close and reopen Package Manager
2. Ensure scopes are added correctly (with `com.` prefix)
3. Try refreshing: Right-click in Package Manager → Refresh

### Still Having Issues?

1. **Check Unity version**: Requires Unity 2020.3 or higher
2. **Check internet connection**: Package Manager needs internet access
3. **Clear Package Cache**:
   - Close Unity
   - Delete `Library/PackageCache/`
   - Reopen Unity

## After Installation

1. **Import Prefabs Sample**:
   - Package Manager → Autech AdMob Mediation
   - Samples section → Prefabs → Import

2. **Configure AdMob**:
   - `Assets > Google Mobile Ads > Settings`
   - Add your AdMob App ID

3. **Add to Scene**:
   - Drag prefab from Samples into your scene
   - Configure Ad Unit IDs
   - Test!

## Manual manifest.json Method

If you prefer editing manifest.json directly:

1. Close Unity
2. Open `Packages/manifest.json`
3. Add the scoped registry and dependencies:

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.google.ads.mobile",
        "com.google.external-dependency-manager"
      ]
    }
  ],
  "dependencies": {
    "com.google.ads.mobile": "10.4.2",
    "com.google.ads.mobile.mediation.unity": "3.15.0",
    "com.autech.admob-mediation": "https://github.com/HaseebDev/Admob-Mediation-Package.git",
    ...other packages...
  }
}
```

4. Save and reopen Unity
5. All packages will install automatically

## Next Steps

See [PACKAGE_GUIDE.md](PACKAGE_GUIDE.md) for complete documentation on:
- Using the package
- Configuration options
- Samples and examples
- Advanced features
- Troubleshooting

## Support

- [Issues](https://github.com/HaseebDev/Admob-Mediation-Package/issues)
- [Documentation](Documentation~/)
- [README](README.md)
