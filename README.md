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

## 🏗️ Architecture Overview

The package uses a clean, modular architecture:

- **`AdsManager`** - Main singleton orchestrating all ad operations
- **`VerifyAdmob`** - MonoBehaviour for scene integration and initial configuration
- **`AdConfiguration`** - Centralized configuration management
- **Ad Controllers** - Specialized controllers for each ad type:
  - `BannerAdController` - Banner ad lifecycle
  - `InterstitialAdController` - Interstitial ads
  - `RewardedAdController` - Rewarded ads
  - `RewardedInterstitialAdController` - Rewarded interstitial ads
  - `AppOpenAdController` - App open ads
- **`ConsentManager`** - GDPR/UMP consent handling
- **`MediationConsentManager`** - Unity Ads mediation consent
- **`AdPersistenceManager`** - Storage with encryption
- **`AdsExampleUI`** - Example UI implementation with testing utilities

All components are in the `Autech.Admob` namespace.

### Professional Debug Logging
```csharp
// Event-based logging system with UI integration
// Assign a TMP_Text component to debugLogText in AdsExampleUI

// Subscribe to debug log events
AdsExampleUI.OnDebugLog += (message) => {
    Debug.Log($"Ads Debug: {message}");
};

// Trigger debug log events
AdsExampleUI.OnDebugLog?.Invoke("Custom log message");

// Check current ad system status
AdsManager.Instance.LogDebugStatus();
```

### Dynamic Ad Unit ID Configuration
```csharp
// Set platform-specific Ad Unit IDs at runtime
AdsManager.Instance.SetAndroidAdIds(
    "ca-app-pub-YOUR_ID/banner",
    "ca-app-pub-YOUR_ID/interstitial",
    "ca-app-pub-YOUR_ID/rewarded",
    "ca-app-pub-YOUR_ID/rewarded_interstitial",
    "ca-app-pub-YOUR_ID/app_open"
);

// Set iOS Ad Unit IDs
AdsManager.Instance.SetIosAdIds(
    "ca-app-pub-YOUR_ID/banner",
    "ca-app-pub-YOUR_ID/interstitial",
    "ca-app-pub-YOUR_ID/rewarded",
    "ca-app-pub-YOUR_ID/rewarded_interstitial",
    "ca-app-pub-YOUR_ID/app_open"
);

// Validate configuration
bool isValid = AdsManager.Instance.AreAdIdsValid();
bool isTestMode = AdsManager.Instance.AreTestAdIds();
AdsManager.Instance.LogCurrentAdIds();
```

### Comprehensive Testing
```csharp
// Get reference to test UI component
AdsExampleUI testUI = FindObjectOfType<AdsExampleUI>();

// Test all ad types sequentially
testUI.TestAllAdsSequentially();

// Test Remove Ads workflow
testUI.TestRemoveAdsFunctionality();

// Check system status
testUI.CheckAllAdStatus();
testUI.CheckAdIds();

// Individual ad tests
testUI.CallInterstitial(2);      // Show interstitial with callbacks
testUI.CallRewarded(3);           // Show rewarded with full callbacks
testUI.CallRewardedInterstitial(3); // Show rewarded interstitial
testUI.CallAppOpen(2);            // Show app open ad
testUI.ToggleBannerTestCall();    // Toggle banner visibility
```

### Rewarded Ad with Full Callbacks
```csharp
// Show rewarded ad with full callbacks
AdsManager.Instance.ShowRewarded(
    onRewarded: (reward) => {
        Debug.Log($"Reward granted: {reward.Amount} {reward.Type}");
        // Give player reward here
    },
    onSuccess: () => {
        Debug.Log("Ad completed successfully");
    },
    onFailure: () => {
        Debug.Log("Ad failed to show");
    }
);

// Simplified versions
AdsManager.Instance.ShowRewarded(); // No callbacks
AdsManager.Instance.ShowRewarded(OnAdClosed); // Success callback only
AdsManager.Instance.ShowRewarded(OnAdClosed, OnAdFailed); // Success + failure
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

> **Important**: All code examples require the `Autech.Admob` namespace:
> ```csharp
> using Autech.Admob;
> ```

### Basic Setup
```csharp
using Autech.Admob;

// Access the singleton instance
AdsManager adsManager = AdsManager.Instance;

// Check if ads system is initialized
if (adsManager.IsInitialized)
{
    Debug.Log("Ads system is ready!");
}
```

### Interstitial Ads
```csharp
// Show interstitial with callbacks
AdsManager.Instance.ShowInterstitial(
    onSuccess: () => {
        Debug.Log("Interstitial closed");
        // Continue game flow
    },
    onFailure: () => {
        Debug.Log("Interstitial failed to show");
        // Continue without ad
    }
);

// Simplified versions
AdsManager.Instance.ShowInterstitial(); // No callbacks
AdsManager.Instance.ShowInterstitial(OnAdClosed); // Success callback only

// Check if ready
if (AdsManager.Instance.IsInterstitialReady())
{
    // Show the ad
}
```

### Rewarded Interstitial Ads
```csharp
// Show rewarded interstitial with full callbacks
AdsManager.Instance.ShowRewardedInterstitial(
    onRewarded: (reward) => {
        Debug.Log($"Reward: {reward.Amount} {reward.Type}");
        // Grant reward
    },
    onSuccess: () => Debug.Log("Ad closed"),
    onFailure: () => Debug.Log("Ad failed")
);

// Check if ready
bool isReady = AdsManager.Instance.IsRewardedInterstitialReady();
```

### App Open Ads
```csharp
// Show app open ad
AdsManager.Instance.ShowAppOpenAd(
    onSuccess: () => Debug.Log("App open ad closed"),
    onFailure: () => Debug.Log("App open ad failed")
);

// Enable/disable auto-show on app resume
AdsManager.Instance.AutoShowAppOpenAds = true;

// Set cooldown time (seconds)
AdsManager.Instance.AppOpenCooldownTime = 4f;

// Check availability
bool isAvailable = AdsManager.Instance.IsAppOpenAdAvailable();
```

### Consent Management
```csharp
// Show privacy options form (GDPR)
AdsManager.Instance.ShowPrivacyOptionsForm();

// Check if privacy options button should be shown
if (AdsManager.Instance.ShouldShowPrivacyOptionsButton())
{
    // Show privacy options button in settings
}

// Get consent status
ConsentStatus status = AdsManager.Instance.GetCurrentConsentStatus();
bool canRequestAds = AdsManager.Instance.CanUserRequestAds();

// Refresh mediation consent
AdsManager.Instance.RefreshMediationConsent();
```

### Remove Ads System
```csharp
using Autech.Admob;

// Enable Remove Ads (disables Banner, Interstitial, App Open)
AdsManager.Instance.RemoveAds = true;

// Disable Remove Ads (re-enable ads)
AdsManager.Instance.RemoveAds = false;

// Check Remove Ads status
if (AdsManager.Instance.RemoveAds)
{
    // Show premium UI, hide ad buttons
}

// Subscribe to Remove Ads changes
AdsManager.OnRemoveAdsChanged += (isEnabled) => {
    Debug.Log($"Remove Ads is now: {(isEnabled ? "Enabled" : "Disabled")}");
    // Update UI accordingly
};

// IAP Integration
public void OnRemoveAdsPurchased()
{
    AdsManager.Instance.RemoveAds = true;
    // Automatically saves to persistent storage
}

// Restore purchases
public void OnRestorePurchases()
{
    AdsManager.Instance.ForceLoadFromStorage();
}

// Check if data exists in storage
bool hasPurchase = AdsManager.Instance.HasRemoveAdsDataInStorage();
```

### Persistence & Storage
```csharp
// Force load Remove Ads status from storage
AdsManager.Instance.ForceLoadFromStorage();

// Force save current Remove Ads status
AdsManager.Instance.ForceSaveToStorage();

// Clear all Remove Ads data
AdsManager.Instance.ClearRemoveAdsData();

// Check if Remove Ads data exists in storage
bool hasData = AdsManager.Instance.HasRemoveAdsDataInStorage();

// Legacy encryption migration (if upgrading from old version)
if (AdsManager.Instance.NeedsLegacyMigration())
{
    bool migrated = AdsManager.Instance.MigrateLegacyEncryption();
    Debug.Log($"Migration success: {migrated}");
}

// Debug encryption information
AdsManager.Instance.LogEncryptionInfo();

// Subscribe to storage events
AdsManager.OnRemoveAdsLoadedFromStorage += (wasEnabled) => {
    Debug.Log($"Loaded from storage: Remove Ads = {wasEnabled}");
};
```

### Advanced Banner Management
```csharp
using Autech.Admob;

// Load and show banner
AdsManager.Instance.LoadBanner();
AdsManager.Instance.ShowBanner(true);

// Hide banner
AdsManager.Instance.ShowBanner(false);

// Change banner position
AdsManager.Instance.SetBannerPosition(BannerPosition.Top);
AdsManager.Instance.SetBannerPosition(BannerPosition.Bottom);

// Change banner size
AdsManager.Instance.SetBannerSize(BannerSize.MediumRectangle);
AdsManager.Instance.SetBannerSize(BannerSize.Leaderboard);

// Enable adaptive banners
AdsManager.Instance.EnableAdaptiveBanners(true);

// Enable collapsible banners
AdsManager.Instance.EnableCollapsibleBanners = true;

// Check banner status
bool isLoaded = AdsManager.Instance.IsBannerLoaded();
bool isVisible = AdsManager.Instance.IsBannerVisible();
Vector2 bannerSize = AdsManager.Instance.GetBannerSize();
BannerPosition currentPos = AdsManager.Instance.CurrentBannerPosition;
```

### UI Integration & Button Management
```csharp
using Autech.Admob;

// Buttons automatically update based on ad availability in AdsExampleUI
// The UpdateButtonStates() method handles this automatically

// Check ad availability for UI updates
bool canShowRewarded = AdsManager.Instance.IsRewardedReady();
bool canShowInterstitial = AdsManager.Instance.IsInterstitialReady();
bool canToggleBanner = AdsManager.Instance.IsBannerLoaded();
bool canShowAppOpen = AdsManager.Instance.IsAppOpenAdAvailable();
bool canShowRewardedInterstitial = AdsManager.Instance.IsRewardedInterstitialReady();

// Subscribe to Remove Ads events for UI updates
AdsManager.OnRemoveAdsChanged += (isEnabled) => {
    // Update button colors, visibility, etc.
    removeAdsButton.GetComponent<Image>().color = isEnabled ? Color.red : Color.green;
};

// Check if any ad is currently showing (to disable UI)
bool isShowingAd = AdsManager.Instance.IsShowingAd;
```

### Smart Banner Visibility Control
```csharp
using Autech.Admob;

// Banner automatically shows/hides based on first-time loading and settings
// Configure in VerifyAdmob Inspector:
// - showBannerOnStart = true/false
// - removeAds = true/false

// Manual banner visibility control
AdsManager.Instance.ShowBanner(true);  // Show banner
AdsManager.Instance.ShowBanner(false); // Hide banner

// Set initial banner visibility (same as ShowBanner)
AdsManager.Instance.SetInitialBannerVisibility(true);

// Check first-time loading status
if (AdsManager.Instance.IsFirstTimeLoading)
{
    Debug.Log("Still in first-time loading phase - waiting for ads to initialize");
}

// Check if ads system is ready
if (AdsManager.Instance.IsInitialized)
{
    Debug.Log("Ads system fully initialized");
}
```

### Configuration with VerifyAdmob
```csharp
// VerifyAdmob is a MonoBehaviour that configures AdsManager
// Add it to a GameObject in your scene

// Access VerifyAdmob methods
VerifyAdmob verifyAdmob = FindObjectOfType<VerifyAdmob>();

// Set Remove Ads
verifyAdmob.SetRemoveAds(true);

// Configure banner
verifyAdmob.SetBannerPosition(BannerPosition.Bottom);
verifyAdmob.SetBannerSize(BannerSize.Adaptive);
verifyAdmob.SetAdaptiveBanners(true);
verifyAdmob.SetCollapsibleBanners(false);

// Configure app open ads
verifyAdmob.SetAutoShowAppOpen(true);
verifyAdmob.SetAppOpenCooldown(4f);

// Configure test mode
verifyAdmob.SetTestAds(true);

// Set Ad Unit IDs at runtime
verifyAdmob.SetAndroidAdIds(
    "ca-app-pub-YOUR_ID/banner",
    "ca-app-pub-YOUR_ID/interstitial",
    "ca-app-pub-YOUR_ID/rewarded",
    "ca-app-pub-YOUR_ID/rewarded_interstitial",
    "ca-app-pub-YOUR_ID/app_open"
);

verifyAdmob.SetIosAdIds(
    "ca-app-pub-YOUR_ID/banner",
    "ca-app-pub-YOUR_ID/interstitial",
    "ca-app-pub-YOUR_ID/rewarded",
    "ca-app-pub-YOUR_ID/rewarded_interstitial",
    "ca-app-pub-YOUR_ID/app_open"
);

// Validation methods
verifyAdmob.ValidateAdIds();
verifyAdmob.CheckIfTestAdIds();
verifyAdmob.LogCurrentAdIds();

// Status checks
bool isInitialized = verifyAdmob.IsAdsManagerInitialized();
bool isRemoveAdsEnabled = verifyAdmob.IsRemoveAdsEnabled();
bool isShowingAd = verifyAdmob.IsAnyAdShowing();

// Consent methods
verifyAdmob.ShowPrivacyOptionsForm();
verifyAdmob.RefreshMediationConsent();
verifyAdmob.LogConsentStatus();

// Persistence methods
verifyAdmob.PurchaseRemoveAds();
verifyAdmob.RestorePurchases();
verifyAdmob.TestForceLoadFromStorage();
verifyAdmob.TestForceSaveToStorage();
verifyAdmob.TestClearRemoveAdsData();
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
// Get reference to test UI
AdsExampleUI testUI = FindObjectOfType<AdsExampleUI>();

// Sequential ad testing (runs all ad types in order)
testUI.TestAllAdsSequentially();

// Remove Ads workflow testing
testUI.TestRemoveAdsFunctionality();

// Comprehensive status checking
testUI.CheckAllAdStatus();
testUI.CheckAdIds();

// Clear debug log
testUI.ClearDebugLog();

// Individual ad status checks via AdsManager
AdsManager.Instance.LogDebugStatus();
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
