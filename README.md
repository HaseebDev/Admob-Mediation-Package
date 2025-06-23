# 🎮 Autech AdMob Mediation Unity Ads

![Unity Version](https://img.shields.io/badge/Unity-2020.3%2B-blue.svg)
![Version](https://img.shields.io/badge/version-2.0.0-brightgreen.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)
![Release](https://img.shields.io/github/v/release/HaseebDev/Admob-Mediation-Package?include_prereleases)

A powerful and production-ready AdMob integration package with Unity Ads mediation for Unity projects. Featuring advanced banner controls, comprehensive ad management, and enterprise-grade error handling.

## 🎉 Latest Release - v2.0.0

**[📥 Download v2.0.0](https://github.com/HaseebDev/Admob-Mediation-Package/releases/tag/v2.0.0)**

### What's New in v2.0.0:
- 🎯 **Adaptive Banner Support** - Runtime switching between adaptive and standard banners
- 📱 **Collapsible Banner Support** - Complete with custom targeting
- 🔄 **Unity Ads Mediation** - Fully integrated with consent management
- 💰 **Revenue Tracking Framework** - Comprehensive analytics ready
- 🛡️ **Enhanced Error Handling** - Production-ready with retry logic
- 🔧 **Memory Leak Prevention** - Proper event cleanup and resource management

## ✨ Features

- 🔄 **Complete AdMob Integration** - All ad formats (Banner, Interstitial, Rewarded, App Open)
- 🎯 **Unity Ads Mediation** - Seamless mediation with consent management
- 📱 **Adaptive Banners** - Smart sizing for better user experience
- 🎨 **Collapsible Banners** - Advanced banner controls
- ⚡ **Plug & Play** - Simple drag-and-drop implementation
- 🛠️ **Easy Configuration** - Inspector-based settings
- 📊 **Revenue Tracking** - Built-in analytics framework
- 🔒 **GDPR Compliance** - UMP SDK integration
- 🚀 **Production Ready** - Enterprise-grade error handling
- 📱 **Cross-platform** - Android & iOS support

## 📋 Prerequisites

- Unity 2020.3 or higher
- Node.js (for OpenUPM package manager)
- Google Mobile Ads Unity SDK 10.2.0+

## 🚀 Installation Guide

### Option 1: Quick Install (Recommended)

1. **Download Latest Release**
   - [📥 Download v2.0.0.unitypackage](https://github.com/HaseebDev/Admob-Mediation-Package/releases/download/v2.0.0/2.0.0.unitypackage)
   - File size: ~14 KiB

2. **Import Package**
   - In Unity: `Assets > Import Package > Custom Package...`
   - Select the downloaded `.unitypackage` file
   - Import all assets

### Option 2: Manual Setup with Dependencies

#### Install Node.js
1. Visit [https://nodejs.org/](https://nodejs.org/)
2. Download and install the recommended version
3. Verify: `node --version`

#### Install OpenUPM CLI
```bash
npm install -g openupm-cli
```

#### Add Google Mobile Ads
```bash
cd <your-unity-project-path>
openupm add com.google.ads.mobile
```

### 3. Quick Setup

1. **Add to Scene**: 
   - Drag `VerifyAndInitializeAdmob` prefab into your scene
   - Handles all initialization automatically

2. **Configure Settings**:
   - Select the prefab in hierarchy
   - Configure ad settings in the Inspector
   - Replace test Ad IDs with your production IDs

## 🎮 Usage Examples

### Basic Banner Implementation
```csharp
// Show adaptive banner at bottom
AdsManager.Instance.LoadBanner();
AdsManager.Instance.ShowBanner(true);

// Change banner position
AdsManager.Instance.SetBannerPosition(BannerPosition.Top);

// Enable collapsible banners
AdsManager.Instance.EnableCollapsibleBanners(true);
```

### Rewarded Ad with Callback
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

### Check Ad Availability
```csharp
if (AdsManager.Instance.IsRewardedReady())
{
    // Show rewarded ad button
    rewardButton.interactable = true;
}
```

## 📊 Version History

| Version | Release Date | Key Features |
|---------|-------------|--------------|
| **[v2.0.0](https://github.com/HaseebDev/Admob-Mediation-Package/releases/tag/v2.0.0)** | Latest | Adaptive banners, collapsible support, revenue tracking |
| [v1.0.0](https://github.com/HaseebDev/Admob-Mediation-Package/releases/tag/v1.0.0) | Previous | Initial release with basic ad integration |

## 🔧 Troubleshooting

### Unity Ads Mediation Issues
1. **Remove existing Unity Ads**: Delete `Assets/GoogleMobileAds/Mediation/UnityAds`
2. **Download fresh plugin**: [Unity Ads Mediation](https://developers.google.com/admob/unity/mediation/unity)
3. **Reimport and verify** in AdMob dashboard

### Common Issues
- **Ads not loading**: Check internet connection and ad unit IDs
- **Test ads not showing**: Verify test device configuration
- **Revenue not tracking**: Implement `TrackAdRevenue()` method

## 🚀 Roadmap

- 📦 **Package Manager Support** - Unity Package Manager integration
- 🎨 **UI Components** - Pre-built ad UI elements  
- 📊 **Advanced Analytics** - Firebase/Unity Analytics integration
- 🔔 **Push Notifications** - Integrated notification system

## 🤝 Contributing

Found a bug or have a feature request? 
- [Open an Issue](https://github.com/HaseebDev/Admob-Mediation-Package/issues)
- [Submit a Pull Request](https://github.com/HaseebDev/Admob-Mediation-Package/pulls)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**⭐ Star this repo if it helped you!**

Made with ❤️ by [Autech](https://github.com/HaseebDev)
