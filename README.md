# 🎮 Autech AdMob Mediation Unity Ads

## 📖 Documentation

**Comprehensive guides and documentation are available in the package:**

### 📚 Documentation Files (Available in v2.0.3+)
- **[📋 Quick Implementation Checklist](Documentation~/QuickImplementationChecklist.md)** - Fast-track setup guide (15 minutes)
- **[📖 Comprehensive Implementation Guide](Documentation~/ComprehensiveImplementationGuide.md)** - Deep-dive architecture and best practices
- **[🛡️ GDPR Consent Management Guide](Documentation~/MediationConsentGuide.md)** - Complete consent flow implementation
- **[✅ Google Certified CMP Integration](Documentation~/GoogleCertifiedCMPImplementation.md)** - Enterprise consent solutions

> **💡 Quick Start**: Install via Package Manager (Git URL) and import the Prefabs sample for instant setup!

![Unity Version](https://img.shields.io/badge/Unity-2020.3%2B-blue.svg)
![Version](https://img.shields.io/badge/version-2.0.3-brightgreen.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)
![Release](https://img.shields.io/github/v/release/HaseebDev/Admob-Mediation-Package?include_prereleases)

A powerful and production-ready AdMob integration package with Unity Ads mediation for Unity projects. Featuring advanced banner controls, comprehensive ad management, Remove Ads system with persistence, and enterprise-grade error handling.

## 🎉 Latest Release - v2.0.3

**[📥 Download v2.0.3](https://github.com/HaseebDev/Admob-Mediation-Package/releases/tag/v2.0.3)**

### 🚀 What's New in v2.0.3:
- 🎯 **Enhanced UI Integration** - Dynamic button states based on ad availability with real-time feedback
- 🎨 **Visual Remove Ads Indicator** - Color-coded button (Red/Green) showing current Remove Ads status
- 📱 **Smart Banner Visibility Control** - First-time loading detection with proper timing after consent
- 📝 **Professional Debug Logging** - Event-based logging system with UI text integration
- 🧹 **Code Quality Improvements** - Cleaner, more maintainable codebase with optimized performance
- ⚡ **Real-time UI Updates** - Event-driven UI state management for better user experience
- 🔧 **Enhanced Banner Management** - Improved coordination between AdsManager and VerifyAdmob
- 🎮 **Better User Experience** - Professional logging output and streamlined initialization

## ✨ Core Features

### 🎯 Ad Management
- 🔄 **Complete AdMob Integration** - All ad formats (Banner, Interstitial, Rewarded, Rewarded Interstitial, App Open)
- 🎯 **Unity Ads Mediation** - Seamless mediation with consent management
- 📱 **Adaptive Banners** - Smart sizing for better user experience
- 🎨 **Collapsible Banners** - Advanced banner controls

### 🚫 Remove Ads System
- **Smart Ad Filtering** - Disables Banner, Interstitial, and App Open ads
- **Monetization Preservation** - Keeps Rewarded ads active for continued revenue
- **Performance Optimization** - Prevents loading of disabled ad types
- **IAP Integration Ready** - Perfect for Remove Ads purchases

### 💾 Persistence & Storage
- **Local Storage** - PlayerPrefs with optional XOR encryption
- **Cloud Sync Ready** - Integration points for Unity Cloud Save, Firebase
- **Cross-Device Support** - Sync Remove Ads status across devices
- **Automatic Management** - Save/load on app restart with event notifications

### ⚙️ Configuration Management
- **Inspector Configuration** - All settings configurable without code changes
- **Runtime Modification** - Change settings during gameplay
- **Platform Detection** - Automatic Android/iOS Ad Unit ID switching
- **Validation Tools** - Check for test IDs and configuration errors

### 🧪 Testing & Debugging
- **Comprehensive Test Suite** - Test all ad types with various callback options (integrated in AdsExampleUI)
- **Remove Ads Testing** - Complete workflow validation
- **Automated Testing** - Sequential test execution with timing
- **Context Menu Integration** - Right-click testing from Inspector
- **Detailed Logging** - Professional feedback with configurable verbosity

### 🎨 UI Integration
- **Dynamic Button States** - Buttons automatically enable/disable based on ad availability
- **Visual Remove Ads Indicator** - Color-coded button showing current status (Red=Enabled, Green=Disabled)
- **Real-time UI Updates** - Event-driven updates for immediate user feedback
- **Professional Logging Display** - UI text component integration for real-time log viewing
- **Smart Banner Control** - Automatic banner visibility management with first-time loading detection

### Professional Debug Logging
```csharp
// Event-based logging system with UI integration
// Assign a TMP_Text component to debugLogText in AdsExampleUI

// Log messages automatically appear in UI
AdsExampleUI.OnDebugLog?.Invoke("Custom log message");

// Clear debug log
AdsExampleUI.Instance.ClearDebugLog();

// Check if detailed logging is enabled
if (showDetailedLogs)
{
    // Log messages will be displayed
}
```

### Dynamic Ad Unit ID Configuration
```csharp
// Set platform-specific Ad Unit IDs at runtime
VerifyAdmob verifyScript = FindObjectOfType<VerifyAdmob>();
verifyScript.SetAndroidAdIds(
    "ca-app-pub-YOUR_ID/banner",
    "ca-app-pub-YOUR_ID/interstitial",
    "ca-app-pub-YOUR_ID/rewarded",
    "ca-app-pub-YOUR_ID/rewarded_interstitial", 
    "ca-app-pub-YOUR_ID/app_open"
);

// Validate configuration
verifyScript.ValidateAdIds();
verifyScript.CheckIfTestAdIds();
```

### Comprehensive Testing
```csharp
AdsExampleUI testScript = FindObjectOfType<AdsExampleUI>();

// Test all ad types sequentially
testScript.TestAllAdsSequentially();

// Test Remove Ads workflow
testScript.TestRemoveAdsFunctionality();

// Check system status
testScript.CheckAllAdStatus();
```

### Rewarded Ad with Full Callbacks
```csharp
AdsManager.Instance.ShowRewarded(
    (reward) => {
        Debug.Log($"Reward granted: {reward.Amount} {reward.Type}");
        // Give player reward
    },
    () => Debug.Log("Ad completed successfully"),
    () => Debug.Log("Ad failed to show")
);
```

## 📋 Prerequisites

- Unity 2020.3 or higher
- Google Mobile Ads Unity SDK 10.2.0+
- Git installed (for Git URL installation method)

## 🚀 Installation Guide

> ⚠️ **IMPORTANT**: Dependencies must be installed FIRST!
>
> **[📖 Complete Installation Guide →](INSTALL.md)**

### Quick Install (3 Steps)

**Step 1: Add OpenUPM Registry**
- `Edit > Project Settings > Package Manager`
- Add Scoped Registry:
  - Name: `package.openupm.com`
  - URL: `https://package.openupm.com`
  - Scopes: `com.google.ads.mobile`, `com.google.external-dependency-manager`
- Click **Save**

**Step 2: Install Dependencies**
- `Window > Package Manager`
- Change dropdown to **My Registries**
- Install **Google Mobile Ads** (10.4.2+)
- Install **Google Mobile Ads Unity Ads Mediation** (3.15.0+)

**Step 3: Install This Package**
- Package Manager → `+` → **Add package from git URL**
- Enter: `https://github.com/HaseebDev/Admob-Mediation-Package.git`
- Click **Add**

✅ **Done!** The package will install successfully.

---

### Alternative: OpenUPM CLI (Fastest)

```bash
# Install OpenUPM CLI
npm install -g openupm-cli

# Navigate to project
cd your-unity-project

# Install dependencies
openupm add com.google.ads.mobile
openupm add com.google.ads.mobile.mediation.unity
```

Then add this package via Git URL in Package Manager.

---

### Alternative: manifest.json (One-Step)

Edit `Packages/manifest.json`:

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": ["com.google.ads.mobile", "com.google.external-dependency-manager"]
    }
  ],
  "dependencies": {
    "com.google.ads.mobile": "10.4.2",
    "com.google.ads.mobile.mediation.unity": "3.15.0",
    "com.autech.admob-mediation": "https://github.com/HaseebDev/Admob-Mediation-Package.git"
  }
}
```

Save, reopen Unity - everything installs automatically!

---

### Legacy: UnityPackage Download

[📥 Download v2.0.3.unitypackage](https://github.com/HaseebDev/Admob-Mediation-Package/releases/download/v2.0.3/2.0.3.unitypackage) (51 KiB)

Import via `Assets > Import Package > Custom Package...`

---

## 📦 After Installation

1. **Configure AdMob** → `Assets > Google Mobile Ads > Settings`
2. **Import Prefabs Sample** → Package Manager → Samples → Import
3. **Add to Scene** → Drag `Samples/Prefabs/VerifyandInitializeAdmob` prefab
4. **Configure** → Set Ad Unit IDs, consent settings
5. **Test** → Press Play!

---

## 🆘 Troubleshooting

**Error: "Package cannot be found"?**
→ Install dependencies first! See [INSTALL.md](INSTALL.md)

**Console warnings about "no meta file"?**
→ These are harmless! See [KNOWN_ISSUES.md](KNOWN_ISSUES.md) - Package works correctly

**Dependencies not showing?**
→ Add OpenUPM scoped registry (Step 1 above)

**Plugin folder is empty / Can't find scripts?**
→ Scripts are in `Packages/com.autech.admob-mediation/Runtime/Scripts/` (not Plugins/)
→ Check Package Manager shows the package is installed

**Still having issues?**
→ Check [INSTALL.md](INSTALL.md) for detailed troubleshooting

## 🎮 Usage Examples

### Remove Ads System
```csharp
// Enable Remove Ads (disables Banner, Interstitial, App Open)
AdsManager.Instance.RemoveAds = true;

// Check Remove Ads status
if (AdsManager.Instance.RemoveAds)
{
    // Show premium UI, hide ad buttons
}

// IAP Integration
public void OnRemoveAdsPurchased()
{
    VerifyAdmob.Instance.PurchaseRemoveAds();
    // Automatically saves to persistent storage
}
```

### Advanced Banner Management
```csharp
// Adaptive banner with position cycling
AdsManager.Instance.LoadBanner();
AdsManager.Instance.ShowBanner(true);

// Runtime configuration
AdsManager.Instance.SetBannerPosition(BannerPosition.Top);
AdsManager.Instance.EnableCollapsibleBanners(true);
AdsManager.Instance.EnableAdaptiveBanners(true);
```

### UI Integration & Button Management
```csharp
// Buttons automatically update based on ad availability
// No code needed - handled automatically by AdsExampleUI

// Check if buttons are interactable
bool canShowRewarded = showRewardedBtn.interactable;
bool canShowInterstitial = showInterstitialBtn.interactable;
bool canToggleBanner = toggleBannerBtn.interactable;

// Remove Ads button color indicates status
// Red = Remove Ads enabled, Green = Remove Ads disabled
Image removeAdsButtonImage = toggleRemoveAdsBtn.GetComponent<Image>();
Color currentStatus = removeAdsButtonImage.color;
```

### Smart Banner Visibility Control
```csharp
// Banner automatically shows/hides based on first-time loading and settings
// Configure in VerifyAdmob Inspector:
// - showBannerOnStart = true/false
// - removeAds = true/false

// Manual control after initialization
AdsManager.Instance.SetInitialBannerVisibility(true);  // Show banner
AdsManager.Instance.SetInitialBannerVisibility(false); // Hide banner

// Check first-time loading status
if (AdsManager.Instance.IsFirstTimeLoading)
{
    Debug.Log("Still in first-time loading phase");
}
```

## 📊 Version History

| Version | Release Date | Key Features |
|---------|-------------|--------------|
| **[v2.0.3](https://github.com/HaseebDev/Admob-Mediation-Package/releases/tag/v2.0.3)** | **Latest** | **Added comprehensive documentation and enhanced package, enhanced logging system, improved consent flow, fixed rewarded crashes, code readability improvements, minor bug fixes** |
| [v2.0.2](https://github.com/HaseebDev/Admob-Mediation-Package/releases/tag/v2.0.2) | Previous | Enhanced UI integration, visual remove ads indicator, smart banner visibility control, professional debug logging, code quality improvements, real-time UI updates, enhanced banner management, better user experience |
| [v2.0.1](https://github.com/HaseebDev/Admob-Mediation-Package/releases/tag/v2.0.1) | Previous | Remove Ads system, persistence, full configuration exposure, comprehensive testing |
| [v2.0.0](https://github.com/HaseebDev/Admob-Mediation-Package/releases/tag/v2.0.0) | Legacy | Adaptive banners, collapsible support, revenue tracking |
| [v1.0.0](https://github.com/HaseebDev/Admob-Mediation-Package/releases/tag/v1.0.0) | Initial release | Basic ad integration |

## 🔧 Configuration & Troubleshooting

### Ad Unit ID Configuration
- **Inspector Setup**: Configure all Ad Unit IDs directly in VerifyAdmob component
- **Runtime Changes**: Update IDs programmatically with automatic ad refresh
- **Validation**: Built-in tools to check for test IDs and empty values
- **Platform Detection**: Automatic Android/iOS ID selection

### Remove Ads Troubleshooting
- **Persistence Issues**: Check storage permissions and encryption settings
- **IAP Integration**: Ensure Remove Ads is set after successful purchase
- **Cross-Device Sync**: Verify cloud storage configuration
- **Testing**: Use context menu testing tools for validation

### Unity Ads Mediation Issues
1. **Remove existing Unity Ads**: Delete `Assets/GoogleMobileAds/Mediation/UnityAds`
2. **Download fresh plugin**: [Unity Ads Mediation](https://developers.google.com/admob/unity/mediation/unity)
3. **Reimport and verify** in AdMob dashboard

### Common Issues
- **Ads not loading**: Check internet connection and ad unit IDs
- **Test ads not showing**: Verify test device configuration using validation tools
- **Remove Ads not persisting**: Check storage settings and encryption configuration
- **Revenue not tracking**: Implement `TrackAdRevenue()` method

### UI Integration Issues
- **Buttons not updating**: Ensure AdsExampleUI component is properly configured
- **Remove Ads button color not changing**: Check if Image component is assigned
- **Debug logs not appearing**: Verify TMP_Text component is assigned to debugLogText
- **Banner not showing on first load**: Check showBannerOnStart and removeAds settings

## 🧪 Testing & Development

### Context Menu Testing
Right-click on any script component for instant testing:
- **VerifyAdmob**: Toggle Remove Ads, check status, validate IDs
- **AdsExampleUI**: Show ads, test workflows, check availability, comprehensive testing
- **AdsManager**: Force load/save, clear data, log current state

### Automated Testing
```csharp
// Sequential ad testing
AdsExampleUI.Instance.TestAllAdsSequentially();

// Remove Ads workflow testing  
AdsExampleUI.Instance.TestRemoveAdsFunctionality();

// Comprehensive status checking
AdsExampleUI.Instance.CheckAllAdStatus();
```

## 🚀 Roadmap

- 📦 **Package Manager Support** - Unity Package Manager integration
- 🎨 **UI Components** - Pre-built ad UI elements with Remove Ads integration
- 📊 **Advanced Analytics** - Firebase/Unity Analytics integration with revenue tracking
- 🔔 **Push Notifications** - Integrated notification system
- 🏪 **Enhanced IAP Integration** - Complete in-app purchase workflow
- 🌐 **Multi-Platform Sync** - Advanced cloud synchronization options

## 🤝 Contributing

Found a bug or have a feature request? 
- [Open an Issue](https://github.com/HaseebDev/Admob-Mediation-Package/issues)
- [Submit a Pull Request](https://github.com/HaseebDev/Admob-Mediation-Package/pulls)
- [View Changelog](CHANGELOG.md) for detailed version history

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**⭐ Star this repo if it helped you!**

Made with ❤️ by [Autech](https://github.com/HaseebDev)
