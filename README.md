# 🌟 Autech Admob Mediation Unity Ads

Welcome to the **Autech Admob Mediation Unity Ads** package! This package provides seamless integration of AdMob mediation with Unity Ads for your Unity projects.

---

## 📦 Integration Steps

Follow these simple steps to integrate this package into your Unity project using the Package Manager:

### 1. Open Your Unity Project
   - Launch your Unity Editor and open the project where you want to integrate this package.

### 2. Open the Package Manager
   - Navigate to `Window` > `Package Manager` in the Unity Editor.

### 3. Add the Package
   - In the Package Manager window, click the `+` button in the top left corner.
   - Select **Add package from git URL...**.
   - Enter the following URL for the package:
     ```
     https://github.com/HaseebDev/Autech-Admob-Mediation-UnityAds.git
     ```
   - Click **Add** to import the package.

### 4. Confirm Installation
   - Ensure the package appears in your list of installed packages. You can now start using the AdMob mediation functionality in your Unity project.

### 5. Drag and Drop the Prefab
   - To initialize AdMob, drag and drop the `VerifyAndInitializeAdmob` prefab into your scene. This prefab is essential for verifying and initializing the AdMob services.

### 6. Change Ad IDs
   - Currently, to change the Ad IDs, navigate to the **AdsManager** script and update the Ad IDs accordingly. We are planning to improve accessibility for changing Ad IDs in the next version.
---

## 🛠️ Troubleshooting Mediation Issues

If you encounter issues with AdMob mediation, particularly with Unity Ads, follow these steps to resolve them:

### 1. Delete the Existing Unity Ads Plugin
   - Navigate to your Unity project folder and delete the following directory:
     ```
     Assets/UnityAds
     ```

### 2. Download the Latest Version of Unity Ads
   - Go to the official Unity Ads Mediation documentation at:
     [Unity Ads Mediation Plugin Changelog](https://developers.google.com/admob/unity/mediation/unity#unity-ads-unity-mediation-plugin-changelog)
   - Follow the instructions to download the latest version of the Unity Ads plugin.

### 3. Re-import the Plugin
   - Once you have the latest version, re-import the plugin into your Unity project.

### 4. Verify the Integration
   - Ensure that the Unity Ads are correctly integrated by checking the AdMob mediation settings in the Unity dashboard.

If you continue to experience issues after following these steps, please check the official documentation or reach out for further assistance.

---
