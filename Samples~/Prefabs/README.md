# Prefabs Sample

This sample contains ready-to-use prefabs for quick integration.

## Contents

- **VerifyandInitializeAdmob.prefab**: Main AdMob management prefab

## How to Use

### Quick Setup (5 minutes)

1. **Import this sample** from the Package Manager
2. **Drag the prefab** into your scene: `VerifyandInitializeAdmob.prefab`
3. **Configure in Inspector**:
   - Set your Ad Unit IDs (Android and iOS)
   - Configure banner position and settings
   - Set Remove Ads preferences
4. **Press Play** to test

### Prefab Components

#### VerifyAndInitializeAdmob
Contains two main scripts:

**VerifyAdmob** (Configuration Layer):
- Ad Unit IDs for all platforms
- Display settings (banner position, show on start)
- Remove Ads configuration
- Ad behavior settings
- Consent configuration

**AdsManager** (Core Logic):
- Automatic initialization
- GDPR consent handling
- Ad loading and display
- Mediation management
- Persistence system

## Configuration Guide

### 1. Ad Unit IDs (Required)

Replace the default test IDs with your AdMob IDs:

**Android:**
```
Banner: ca-app-pub-XXXXXXXXXXXXXXXX/YYYYYYYYYY
Interstitial: ca-app-pub-XXXXXXXXXXXXXXXX/YYYYYYYYYY
Rewarded: ca-app-pub-XXXXXXXXXXXXXXXX/YYYYYYYYYY
Rewarded Interstitial: ca-app-pub-XXXXXXXXXXXXXXXX/YYYYYYYYYY
App Open: ca-app-pub-XXXXXXXXXXXXXXXX/YYYYYYYYYY
```

**iOS:**
Same format as Android, but with iOS-specific IDs.

### 2. Consent Settings

**For Testing:**
- `Enable Consent Debugging`: ☑ (during development)
- `Force EEA Geography`: ☑ (to test consent forms)
- `Always Request Consent Update`: ☑

**For Production:**
- `Enable Consent Debugging`: ☐ (MUST be disabled)
- `Force EEA Geography`: ☐ (use real location)
- `Always Request Consent Update`: ☑ (recommended by Google)

### 3. Banner Settings

- **Banner Position**: Top, Bottom, etc.
- **Show Banner On Start**: Show banner immediately after initialization
- **Use Adaptive Banners**: Recommended for better user experience
- **Enable Collapsible Banners**: For space-saving banners

### 4. Remove Ads Settings

- **Remove Ads**: Enable to disable non-rewarded ads
- **Remove Ads Key**: PlayerPrefs key for persistence
- **Show Persistence Debug**: Enable logging for debugging

## Using in Code

### Show Ads
```csharp
// Interstitial
AdsManager.Instance.ShowInterstitial(() => {
    Debug.Log("Interstitial closed");
});

// Rewarded
AdsManager.Instance.ShowRewarded((reward) => {
    Debug.Log($"Reward: {reward.Amount} {reward.Type}");
    // Give player reward
});

// Banner
AdsManager.Instance.ShowBanner(true);  // Show
AdsManager.Instance.ShowBanner(false); // Hide
```

### Check Ad Availability
```csharp
if (AdsManager.Instance.IsInterstitialReady())
{
    // Show interstitial button
}

if (AdsManager.Instance.IsRewardedReady())
{
    // Show rewarded button
}
```

### Remove Ads Integration
```csharp
// After IAP purchase
public void OnRemoveAdsPurchased()
{
    AdsManager.Instance.RemoveAds = true;
    // Automatically saves to PlayerPrefs
}

// Check status
if (AdsManager.Instance.RemoveAds)
{
    // Hide ad buttons, show premium UI
}
```

### Consent Management
```csharp
// Show privacy settings (in settings menu)
VerifyAdmob verifyScript = FindObjectOfType<VerifyAdmob>();
verifyScript.ShowPrivacyOptionsForm();

// Check if should show privacy button
if (verifyScript.ShouldShowPrivacyOptionsButton())
{
    // Show privacy settings button in UI
}
```

## Testing Checklist

- [ ] Test ads load and show correctly
- [ ] Test consent form (use VPN to simulate EEA)
- [ ] Test Remove Ads functionality
- [ ] Test all ad types (Banner, Interstitial, Rewarded)
- [ ] Verify Ad Unit IDs are correct for your platform
- [ ] Check console for any errors

## Production Checklist

- [ ] Replace all test Ad Unit IDs with production IDs
- [ ] Disable consent debugging
- [ ] Disable test ads
- [ ] Test on real devices (Android and iOS)
- [ ] Verify GDPR consent flow in EEA region
- [ ] Test Remove Ads persistence across app restarts

## Troubleshooting

**Ads not showing?**
1. Check console for initialization errors
2. Verify Ad Unit IDs are correct
3. Ensure internet connection is available
4. Check if Remove Ads is enabled
5. Verify consent status (must be obtained/not required)

**Consent form not showing?**
1. Enable "Force EEA Geography" for testing
2. Check AdMob console privacy messages are configured
3. Verify internet connection
4. Enable consent debugging temporarily

**Need more help?**
See the comprehensive documentation in the package or visit the GitHub repository.
