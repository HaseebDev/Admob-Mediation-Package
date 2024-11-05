# 🎮 Autech AdMob Mediation Unity Ads

![Unity Version](https://img.shields.io/badge/Unity-2020.3%2B-blue.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)

A powerful and easy-to-integrate package that seamlessly combines AdMob mediation with Unity Ads for your Unity projects. Streamline your ad implementation and maximize revenue with this comprehensive solution.

## ✨ Features

- 🔄 Seamless AdMob mediation integration
- 🎯 Unity Ads support
- ⚡ Simple drag-and-drop implementation
- 🛠️ Easy configuration
- 📱 Cross-platform support

## 📋 Prerequisites

- Unity 2020.3 or higher
- Node.js (for OpenUPM package manager)
- Google Mobile Ads package

## 🚀 Installation Guide

### 1. Set Up OpenUPM

First, we need to set up OpenUPM to handle our dependencies:

```bash
# Install OpenUPM CLI
npm install -g openupm-cli

# Add Google Mobile Ads to your project
cd <your-unity-project-path>
openupm add com.google.ads.mobile
```

### 2. Import the Package

1. Download the latest release from our [Releases](releases) section
2. In Unity, go to `Assets > Import Package > Custom Package...`
3. Select the downloaded `.unitypackage` file
4. Import all assets

### 3. Quick Setup

1. **Add to Scene**: 
   - Drag the `VerifyAndInitializeAdmob` prefab into your scene
   - This prefab handles all necessary initialization

2. **Configure Ad IDs**:
   - Locate the `AdsManager` script
   - Replace placeholder Ad IDs with your own

```csharp
// Example Ad ID configuration
public string BannerAdUnitId = "your-banner-ad-id";
public string InterstitialAdUnitId = "your-interstitial-ad-id";
```

## 🔧 Troubleshooting Unity Ads Integration

If you encounter issues with Unity Ads mediation:

1. **Remove Existing Unity Ads**
   ```
   Delete Assets/UnityAds folder
   ```

2. **Get Fresh Unity Ads Plugin**
   - Visit [Unity Ads Mediation Plugin Changelog](https://developers.google.com/admob/unity/mediation/unity#step_1_import_the_unity_ads_unity_plugin)
   - Download the latest version

3. **Reinstall Plugin**
   - Import the new Unity Ads plugin
   - Verify integration in AdMob dashboard

## 🔜 Coming Soon

- 📦 Package Manager support
- ⚙️ Simplified Ad ID configuration
- 🎨 Enhanced UI components
- 📊 Advanced analytics integration

---

Made with ❤️ by Autech
