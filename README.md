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

#### Install Node.js
1. Visit [https://nodejs.org/](https://nodejs.org/)
2. Download and install the recommended version for your operating system
3. Verify installation by opening a terminal/command prompt and running:
   ```bash
   node --version
   ```

#### Install OpenUPM CLI
1. Open a terminal or command prompt
2. Run the following command:
   ```bash
   npm install -g openupm-cli
   ```
3. Verify OpenUPM installation:
   ```bash
   openupm --version
   ```

#### Add Google Mobile Ads
1. Navigate to your Unity project directory in the terminal:
   ```bash
   cd <your-unity-project-path>
   ```
2. Add Google Mobile Ads package:
   ```bash
   openupm add com.google.ads.mobile
   ```
   
> **Note**: If you encounter any issues, ensure Node.js and OpenUPM are installed correctly and your terminal has administrative privileges.

### 2. Import the Package

1. Download the latest release from our [Releases](releases) section
   - Look for: `Autech-Admob-Mediation-UnityAds-v1.0.0.unitypackage` (or latest version)
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
   Delete Assets/GoogleMobileAds/Mediation/UnityAds folder
   ```

2. **Get Fresh Unity Ads Plugin**
   - Visit [Unity Ads Mediation Plugin Changelog](https://developers.google.com/admob/unity/mediation/unity#unity-ads-unity-mediation-plugin-changelog)
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
