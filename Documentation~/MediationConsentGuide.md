# AdMob Mediation Consent Management Guide

## 🎯 Overview

Your AdMob system now includes comprehensive mediation consent management that ensures GDPR compliance across all mediation networks. This guide explains the new features and how to use them.

## ✅ What Was Added

### 1. Enhanced SetMediationConsent() Method
- **Comprehensive consent handling** for Unity Ads mediation
- **EEA-specific consent logic** for GDPR compliance
- **Ready-to-use templates** for future mediation networks (Facebook, AppLovin, ironSource, Chartboost)
- **Detailed logging** for debugging consent issues

### 2. Automatic Consent Updates
The system now automatically calls `SetMediationConsent()` in all consent scenarios:
- ✅ Consent not required (non-EEA users)
- ✅ Consent already obtained (returning users)
- ✅ After consent form completion
- ✅ During debugging modes
- ✅ Before AdMob initialization

### 3. New Public API Methods

#### AdsManager.cs Methods
```csharp
// Manually refresh mediation consent
public void RefreshMediationConsent()

// Show privacy options form for users to change consent
public void ShowPrivacyOptionsForm()

// Check if privacy options button should be shown
public bool ShouldShowPrivacyOptionsButton()

// Get current consent status
public ConsentStatus GetCurrentConsentStatus()

// Check if user can request ads
public bool CanUserRequestAds()
```

#### VerifyAdmob.cs Methods
```csharp
// All the same methods plus debugging utilities
public void RefreshMediationConsent()
public void ShowPrivacyOptionsForm()
public bool ShouldShowPrivacyOptionsButton()
public ConsentStatus GetCurrentConsentStatus()
public bool CanUserRequestAds()

// Additional debugging method
public void LogConsentStatus()
```

## 🚀 How to Use

### Basic Implementation

```csharp
// In your game's settings menu
public class GameSettingsUI : MonoBehaviour
{
    [SerializeField] private Button privacyOptionsButton;
    [SerializeField] private VerifyAdmob adsManager;

    void Start()
    {
        // Show/hide privacy options button based on user status
        privacyOptionsButton.gameObject.SetActive(adsManager.ShouldShowPrivacyOptionsButton());
        
        // Connect button to show privacy options
        privacyOptionsButton.onClick.AddListener(() => {
            adsManager.ShowPrivacyOptionsForm();
        });
    }
}
```

### Advanced Usage

```csharp
// Check consent status for analytics or UI customization
public class AdUIController : MonoBehaviour
{
    [SerializeField] private VerifyAdmob adsManager;
    [SerializeField] private GameObject adRelatedUI;

    void Update()
    {
        // Show/hide ad-related UI based on consent
        bool canShowAds = adsManager.CanUserRequestAds() && !adsManager.IsRemoveAdsEnabled();
        adRelatedUI.SetActive(canShowAds);
    }

    public void OnConsentChanged()
    {
        // Call this when you know consent might have changed
        adsManager.RefreshMediationConsent();
        
        // Log current status for debugging
        Debug.Log($"Consent Status: {adsManager.GetCurrentConsentStatus()}");
        Debug.Log($"Can Request Ads: {adsManager.CanUserRequestAds()}");
    }
}
```

## 🔧 Testing Your Implementation

### 1. Use Inspector Context Menus
- Select your VerifyAdmob GameObject in the scene
- Right-click in Inspector → "Refresh Mediation Consent"
- Right-click in Inspector → "Show Privacy Options"
- Right-click in Inspector → "Log Consent Status"

### 2. Enable Consent Debugging
```csharp
// In VerifyAdmob Inspector
enableConsentDebugging = true;
```

### 3. Check Console Logs
Look for these log patterns:
```
[AdsManager] ✅ Consent Status: OBTAINED
[AdsManager] Unity Ads consent metadata configured successfully
[AdsManager] Mediation consent configuration completed for all networks
```

## 🌍 Adding New Mediation Networks

When you add new mediation networks, update the `SetMediationConsent()` method:

```csharp
// In AdsManager.cs -> SetMediationConsent()
// Uncomment and modify the relevant section:

// Example: AppLovin MAX
if (canRequestAds)
{
    MaxSdk.SetHasUserConsent(hasConsent);
    MaxSdk.SetIsAgeRestrictedUser(false);
    Debug.Log("[AdsManager] AppLovin mediation consent configured");
}
```

## 🛡️ GDPR Compliance Features

### Automatic EEA Detection
- Non-EEA users: Consent not required, ads work normally
- EEA users: Must provide explicit consent before ads can be shown

### Privacy Options
- EEA users get a "Manage Consent" option
- Users can change their consent choices anytime
- Consent is automatically synchronized across all mediation networks

### Debugging vs Production
- **Debugging mode**: Bypasses consent issues for testing
- **Production mode**: Strictly enforces GDPR compliance

## 📋 Best Practices

### 1. Always Check Consent Before Showing Ads
```csharp
if (adsManager.CanUserRequestAds() && !adsManager.IsRemoveAdsEnabled())
{
    // Safe to show ads
    adsManager.ShowInterstitial();
}
```

### 2. Provide Privacy Controls
```csharp
// Show privacy options button for EEA users
if (adsManager.ShouldShowPrivacyOptionsButton())
{
    privacySettingsButton.SetActive(true);
}
```

### 3. Handle Consent Changes
```csharp
// After user changes consent via privacy options
adsManager.RefreshMediationConsent();
```

### 4. Monitor Consent Status
```csharp
// For analytics or debugging
ConsentStatus status = adsManager.GetCurrentConsentStatus();
bool canRequestAds = adsManager.CanUserRequestAds();
```

## 🐛 Troubleshooting

### Common Issues

1. **"Mediation consent not working"**
   - Check if `SetMediationConsent()` is being called
   - Look for consent status logs in console
   - Verify `CanUserRequestAds()` returns true

2. **"Privacy options button not showing"**
   - User might not be in EEA
   - User might not have given consent yet
   - Check `ShouldShowPrivacyOptionsButton()` return value

3. **"Ads not showing after consent"**
   - Call `RefreshMediationConsent()` manually
   - Check if Remove Ads is enabled
   - Verify AdMob initialization completed

### Debug Commands
```csharp
// Log comprehensive status
adsManager.LogConsentStatus();

// Force refresh consent
adsManager.RefreshMediationConsent();

// Check all conditions
Debug.Log($"Can request ads: {adsManager.CanUserRequestAds()}");
Debug.Log($"Remove ads enabled: {adsManager.IsRemoveAdsEnabled()}");
Debug.Log($"AdMob initialized: {adsManager.IsAdsManagerInitialized()}");
```

## 🎉 Summary

Your AdMob mediation system now provides:
- ✅ **Automatic GDPR compliance** for all mediation networks
- ✅ **Easy-to-use API** for consent management
- ✅ **Privacy options** for user control
- ✅ **Production-ready** consent handling
- ✅ **Extensible design** for future mediation networks
- ✅ **Comprehensive debugging** tools

The system handles all consent scenarios automatically while providing you with the tools to create compliant and user-friendly ad experiences.