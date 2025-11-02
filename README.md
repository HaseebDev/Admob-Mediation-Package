# 🎮 Autech AdMob Mediation Unity Ads

## 📖 Documentation

**Everything you need ships with the package:**

### 📚 Included Documentation
- **[📋 Quick Implementation Checklist](Documentation~/QuickImplementationChecklist.md)** – 15‑minute fast start
- **[📖 Comprehensive Implementation Guide](Documentation~/ComprehensiveImplementationGuide.md)** – Architecture deep dive
- **[🛡️ GDPR Consent Management Guide](Documentation~/MediationConsentGuide.md)** – Full Google UMP flow
- **[✅ Google Certified CMP Integration](Documentation~/GoogleCertifiedCMPImplementation.md)** – Enterprise consent solutions

> **💡 Quick Start**: Add the package via Unity Package Manager (Git URL), import the Prefabs sample, press Play.

![Unity Version](https://img.shields.io/badge/Unity-2020.3%2B-blue.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)
![Release](https://img.shields.io/github/v/release/HaseebDev/Admob-Mediation-Package?include_prereleases)

A production-ready AdMob + Unity Ads mediation stack for Unity projects. It provides thread-safe ad orchestration, direct Google UMP ➜ Unity Ads consent mirroring, AES-secured “Remove Ads” persistence, and developer-friendly diagnostics.

## ✨ System Highlights

- 🧠 **Thread-Safe Ad Orchestration** – `AdsManager` captures Unity’s sync context, enforces main-thread access, auto-instantiates when missing, and guards every show call against overlaps.
- 🛡️ **Consent-Aware Mediation** – `ConsentManager` runs the full Google UMP async flow, while `MediationConsentManager` maps user selections into Unity Ads metadata (gdpr.consent, privacy.mode, tracking, age flags).
- 💾 **Secure Remove Ads Persistence** – `AdPersistenceManager` stores purchases through AES-256 + HMAC (`SecureStorage`), includes legacy migration helpers, and exposes load/save events for cloud sync.
- 🎯 **Complete Ad Coverage** – Dedicated controllers handle Banner, Interstitial, Rewarded, Rewarded Interstitial, and App Open inventory with adaptive + collapsible banner support.
- 🧪 **Developer Tooling** – Rich logging, consent diagnostics, context-menu shortcuts, and sample UIs (`VerifyAdmob`, `AdsExampleUI`, `ConsentUIExample`) accelerate integration and testing.

## ✨ Core Features

### 🎯 Ad Management
- 🔄 **All Ad Formats** – Banner, Interstitial, Rewarded, Rewarded Interstitial, App Open controllers with unified retry logic
- 🎯 **Unity Ads Mediation** – Consent-aware metadata updates with a single call
- 📱 **Adaptive Banners** – Anchored adaptive requests sized per device
- 🎨 **Collapsible Banners** – Edge-aware collapsible extras for banner experiments

### 🚫 Remove Ads System
- **Smart Filtering** – Banner, Interstitial, and App Open automatically disabled when Remove Ads is active
- **Rewarded Preservation** – Rewarded formats stay available to keep monetization paths open
- **Performance Savings** – Controllers skip load attempts for disabled formats
- **IAP Ready** – Direct setters, events, and persistence hooks for purchase flows

### 💾 Persistence & Storage
- **AES-256 Secure Storage** – Encrypted + HMAC-verified persistence via `SecureStorage`
- **Cloud Sync Friendly** – Events fire when values change or load, enabling cross-device propagation
- **Legacy Migration** – Manual XOR migration helpers protect older installs
- **Automatic Management** – Load/save on startup with manual overrides when needed

### ⚙️ Configuration Management
- **Inspector Friendly** – `VerifyAdmob` exposes all knobs without code edits
- **Runtime Mutations** – Update IDs, banner options, or toggles at runtime
- **Platform Awareness** – `AdConfiguration` handles Android/iOS switching automatically
- **Validation Tools** – Built-ins check for empty IDs or lingering Google test units

### 🧪 Testing & Debugging
- **Sample UI Suite** – `AdsExampleUI` includes buttons for every show path and status indicator
- **Context Menu Shortcuts** – Right-click scripts to toggle Remove Ads, refresh consent, clear storage
- **Verbose Logging** – Consent, initialization, and controller state changes are fully logged
- **Status Helpers** – `AdsManager.LogDebugStatus()` surfaces key toggles and readiness flags

### 🎨 UI Integration
- **Dynamic Button States** – Sample UI toggles interactability based on readiness checks
- **Visual Remove Ads Indicator** – Color-coded buttons reflect Remove Ads state
- **Professional Log Panel** – Hook `TMP_Text` to mirror debug events in-game
- **Smart Banner Control** – First-load tracking ensures banners appear only after consent

## 🏗️ Architecture Overview

- **`AdsManager`** – Singleton orchestrator, synchronization context guard, lifecycle manager
- **`VerifyAdmob`** – Scene entry point for configuration + initialization
- **`AdConfiguration`** – Centralized ad unit IDs and global toggles
- **Ad Controllers** – `BannerAdController`, `InterstitialAdController`, `RewardedAdController`, `RewardedInterstitialAdController`, `AppOpenAdController`
- **`ConsentManager`** – Google UMP integration with async flows
- **`MediationConsentManager`** – Unity Ads metadata bridge
- **`AdPersistenceManager`** – Remove Ads persistence coordinator
- **`SecureStorage`** – AES-256 + HMAC secure storage utility
- **Samples** – `AdsExampleUI`, `ConsentUIExample`, and supporting prefabs for rapid testing

All runtime scripts live in the `Autech.Admob` namespace.

### Professional Debug Logging
```csharp
// Event-based logging system with UI integration
// Assign a TMP_Text component to debugLogText in AdsExampleUI

AdsExampleUI.OnDebugLog += message => Debug.Log($"Ads Debug: {message}");
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

AdsManager.Instance.SetIosAdIds(
    "ca-app-pub-YOUR_ID/banner",
    "ca-app-pub-YOUR_ID/interstitial",
    "ca-app-pub-YOUR_ID/rewarded",
    "ca-app-pub-YOUR_ID/rewarded_interstitial",
    "ca-app-pub-YOUR_ID/app_open"
);

bool isValid = AdsManager.Instance.AreAdIdsValid();
bool isTestMode = AdsManager.Instance.AreTestAdIds();
AdsManager.Instance.LogCurrentAdIds();
```

### Comprehensive Testing
```csharp
AdsExampleUI testUI = FindObjectOfType<AdsExampleUI>();

testUI.TestAllAdsSequentially();      // Run full suite
testUI.TestRemoveAdsFunctionality();  // Walk Remove Ads workflow
testUI.CheckAllAdStatus();            // Log readiness for every format
testUI.CheckAdIds();                  // Validate configuration

testUI.CallInterstitial(2);
testUI.CallRewarded(3);
testUI.CallRewardedInterstitial(3);
testUI.CallAppOpen(2);
testUI.ToggleBannerTestCall();
```

### Rewarded Ad with Full Callbacks
```csharp
AdsManager.Instance.ShowRewarded(
    onRewarded: reward => {
        Debug.Log($"Reward granted: {reward.Amount} {reward.Type}");
        // Grant your in-game reward here
    },
    onSuccess: () => Debug.Log("Rewarded ad finished"),
    onFailure: () => Debug.Log("Rewarded ad unavailable")
);

// Simplified helpers
AdsManager.Instance.ShowRewarded();
AdsManager.Instance.ShowRewarded(OnRewardedClosed);
AdsManager.Instance.ShowRewarded(OnRewardedClosed, OnRewardedFailed);
```

## 📋 Prerequisites

- Unity 2020.3 or higher
- Google Mobile Ads Unity SDK 10.6.0+
- Git installed (for Package Manager Git URL import)

## 🚀 Installation Guide

> ⚠️ **IMPORTANT**: Install Google Mobile Ads (GMA) and External Dependency Manager before adding this package.  
> **Need visuals?** See [INSTALL.md](INSTALL.md) for step-by-step screenshots.

### Quick Install (Git URL)

1. Open `Window > Package Manager`
2. Click `+` → **Add package from git URL…**
3. Paste `https://github.com/HaseebDev/Admob-Mediation-Package.git`
4. Click **Add**

### Dependencies

Ensure the following packages exist (via Package Manager or `Packages/manifest.json`):

- `com.google.ads.mobile` (Google Mobile Ads, 10.6.0+ recommended)
- `com.google.ads.mobile.mediation.unity` (Unity Ads mediation adapter)
- `com.google.external-dependency-manager` (EDM4U)

> 💡 The package now detects these dependencies on import and can auto-install them from the official Google repository after a quick confirmation dialog. You can rerun the installer anytime via `Tools ▸ Autech ▸ Install Google Dependencies`.

### Optional: OpenUPM CLI Install

```bash
npm install -g openupm-cli
cd your-unity-project
openupm add com.google.ads.mobile
openupm add com.google.ads.mobile.mediation.unity
```

Finish by adding this package via Git URL as shown above.

---

## 📦 After Installation

1. **Configure AdMob** → `Assets > Google Mobile Ads > Settings`
2. **Import Prefabs Sample** → Package Manager → Samples → **Import**
3. **Add to Scene** → Drag `Samples/Prefabs/VerifyandInitializeAdmob` prefab
4. **Configure** → Set ad unit IDs, consent toggles, Remove Ads defaults
5. **Press Play** → Use the sample UI to validate every ad path

---

## 🆘 Troubleshooting

**“Package cannot be found”** – Install the GMA and EDM packages first, then retry the Git URL import.  
**“No meta file” console warnings** – Harmless; scripts live under `Packages/com.autech.admob-mediation/Runtime/`.  
**Dependencies missing** – Open `Packages/manifest.json` to confirm the dependency entries.  
**Unity Ads mediation not respecting consent** – After UMP completes, call `AdsManager.Instance.RefreshMediationConsent()`.  
**Remove Ads not persisting** – Verify encryption key consistency and confirm `AdPersistenceManager.UseEncryptedStorage` matches your expectations.  
**Ads not loading** – Double-check ad unit IDs, network connectivity, and test vs production settings.  
**Need more help?** – See [INSTALL.md](INSTALL.md) and [KNOWN_ISSUES.md](KNOWN_ISSUES.md).

---

## 📊 Version History

| Version | Release Date | Key Features |
|---------|--------------|--------------|
| **[v2.1.0](https://github.com/HaseebDev/Admob-Mediation-Package/releases/tag/v2.1.0)** | Latest | Thread-safe `AdsManager`, AES-secured persistence, Google UMP ➜ Unity Ads consent bridge, unified controller retries, refreshed tooling & docs |
| [v2.0.2](https://github.com/HaseebDev/Admob-Mediation-Package/releases/tag/v2.0.2) | Previous | Enhanced UI integration, visual Remove Ads indicator, smart banner visibility control, professional debug logging |
| [v2.0.1](https://github.com/HaseebDev/Admob-Mediation-Package/releases/tag/v2.0.1) | Previous | Remove Ads system, persistence, comprehensive testing |
| [v2.0.0](https://github.com/HaseebDev/Admob-Mediation-Package/releases/tag/v2.0.0) | Legacy | Adaptive banners, collapsible support, revenue tracking |
| [v1.0.0](https://github.com/HaseebDev/Admob-Mediation-Package/releases/tag/v1.0.0) | Initial release | Basic ad integration |

---

## 🔧 Configuration & Troubleshooting Tips

### Ad Unit ID Configuration
- Use `VerifyAdmob` in the Inspector for quick edits.
- Call `AdsManager.Instance.SetAllAdIds(...)` at runtime to refresh everything in one hit.
- Run `AdsManager.Instance.AreAdIdsValid()` or `AdsManager.Instance.AreTestAdIds()` before production builds.

### Remove Ads Checklist
- Confirm `AdsManager.Instance.RemoveAds` updates the UI via `AdsManager.OnRemoveAdsChanged`.
- Persist purchases by calling `AdsManager.Instance.ForceSaveToStorage()` after IAP success.
- Restore purchases with `AdsManager.Instance.ForceLoadFromStorage()` on startup.

### Consent Flow
- Configure debug geography and consent debugging in `VerifyAdmob`.
- Trigger `AdsManager.Instance.ShowPrivacyOptionsForm()` from your settings menu.
- Refresh mediation metadata whenever the consent state changes.

---

## 🧪 Testing & Development

### Context Menu Testing

Right-click the component in the Inspector:
- **VerifyAdmob** – Toggle Remove Ads, validate IDs, refresh consent metadata
- **AdsExampleUI** – Run sequential ad tests, check status, clear logs
- **AdsManager** – Force load/save, clear Remove Ads data, print debug status

### Automated Testing Helpers
```csharp
AdsExampleUI testUI = FindObjectOfType<AdsExampleUI>();

testUI.TestAllAdsSequentially();
testUI.TestRemoveAdsFunctionality();
testUI.CheckAllAdStatus();
testUI.CheckAdIds();
testUI.ClearDebugLog();

AdsManager.Instance.LogDebugStatus();
```

---

## 🚀 Roadmap

- 📦 Unity Package Manager registry publishing
- 🎨 Built-in UI widgets with Remove Ads awareness
- 📊 Analytics hooks (Firebase / Unity Analytics) with ad revenue tracking
- 🔔 Notification-ready consent prompts
- 🏪 Expanded IAP helper utilities
- 🌐 Advanced multi-device sync tooling

---

## 🤝 Contributing

- [Open an Issue](https://github.com/HaseebDev/Admob-Mediation-Package/issues)
- [Submit a Pull Request](https://github.com/HaseebDev/Admob-Mediation-Package/pulls)
- Check the [CHANGELOG](CHANGELOG.md) for detailed history

---

## 📄 License

This project is licensed under the MIT License – see the [LICENSE](LICENSE) file.

---

**⭐ Star this repo if it helps your next project!**

Made with ❤️ by [Autech](https://github.com/HaseebDev)
