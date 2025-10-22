# AdMob Mediation - Quick Implementation Checklist

## 🚀 Fast Track Implementation Guide

### Phase 1: Basic Setup (15 minutes)

#### 1. AdMob Console Configuration
- [ ] Create/access AdMob account
- [ ] Add your app to AdMob
- [ ] Create ad units (Banner, Interstitial, Rewarded)
- [ ] Copy ad unit IDs

#### 2. Unity Inspector Setup
- [ ] Find `VerifyAdmob` GameObject in scene
- [ ] Paste ad unit IDs into Inspector fields
- [ ] Set `Enable Test Ads` = ✅ (for development)
- [ ] Set `Enable Consent Debugging` = ✅ (for development)

#### 3. Quick Test
```csharp
// Test in your game script
public VerifyAdmob verifyAdmob;

void Start()
{
    // Test ad availability (consent/config checks via VerifyAdmob)
    if (verifyAdmob.CanUserRequestAds())
    {
        Debug.Log("✅ Ads are ready!");
    }
}

void OnButtonClick()
{
    // Show ads via AdsManager.Instance (actual ad operations)
    if (verifyAdmob.CanUserRequestAds() && !verifyAdmob.IsRemoveAdsEnabled())
    {
        AdsManager.Instance.ShowInterstitial();
    }
}
```

---

### Phase 2: GDPR Compliance (10 minutes)

#### 1. AdMob Console Privacy Setup
- [ ] Go to Privacy & messaging → Manage messages
- [ ] Create GDPR consent message for EEA
- [ ] Publish the message

#### 2. Test Consent Flow
- [ ] Use VPN to connect from EU country
- [ ] Launch app - consent form should appear
- [ ] Test both "Accept" and "Deny" flows

#### 3. Privacy Options Button
```csharp
// In your settings menu
public Button privacyButton;
public VerifyAdmob verifyAdmob;

void Start()
{
    // Show privacy button for EEA users
    privacyButton.gameObject.SetActive(verifyAdmob.ShouldShowPrivacyOptionsButton());
    privacyButton.onClick.AddListener(() => verifyAdmob.ShowPrivacyOptionsForm());
}
```

---

### Phase 3: Mediation Setup (5 minutes)

#### 1. Unity Ads Mediation (Already Done! ✅)
The package automatically configures Unity Ads mediation with proper consent handling.

#### 2. Future Networks (Optional)
To add more mediation networks, uncomment the relevant sections in:
```
/Assets/Admob/Scripts/AdsManager.cs → SetMediationConsent() method
```

---

### Phase 4: Production Readiness (10 minutes)

#### 1. Disable Development Settings
- [ ] Set `Enable Test Ads` = ❌
- [ ] Set `Enable Consent Debugging` = ❌
- [ ] Test with real ads

#### 2. Remove Ads Feature
```csharp
// When user purchases Remove Ads
verifyAdmob.SetRemoveAds(true);

// Check before showing ads
if (!verifyAdmob.IsRemoveAdsEnabled() && verifyAdmob.CanUserRequestAds())
{
    AdsManager.Instance.ShowInterstitial();
}
```

#### 3. Error Handling
```csharp
// Always check before showing ads
if (verifyAdmob.CanUserRequestAds() && !verifyAdmob.IsRemoveAdsEnabled())
{
    AdsManager.Instance.ShowInterstitial();
}
else
{
    Debug.Log("Ads not available - respecting user privacy/purchase");
}
```

---

## 🔍 Quick Debugging

### Console Commands (Right-click VerifyAdmob in Inspector)
- **"Log Consent Status"** - Shows current consent state
- **"Show Privacy Options"** - Tests privacy form
- **"Refresh Mediation Consent"** - Updates all networks

### Common Console Messages
```
✅ "[AdsManager] ✅ Consent Status: OBTAINED"
✅ "[AdsManager] Unity Ads consent metadata configured successfully"
✅ "[AdsManager] AdMob initialized successfully"

⚠️  "[AdsManager] ⚠️ User has not consented to ads"
⚠️  "[AdsManager] Remove ads is enabled"

❌ "[AdsManager] ❌ PRODUCTION ERROR: Consent form failed"
```

---

## 🎯 Essential Code Patterns

### Basic Ad Display
```csharp
public class GameController : MonoBehaviour
{
    public VerifyAdmob verifyAdmob;

    public void ShowAdForBonus()
    {
        if (verifyAdmob.CanUserRequestAds() && !verifyAdmob.IsRemoveAdsEnabled())
        {
            AdsManager.Instance.ShowRewarded();
        }
        else
        {
            // Give bonus without ad, or show purchase option
            GiveBonusDirectly();
        }
    }
}
```

### Settings Menu Integration
```csharp
public class SettingsMenu : MonoBehaviour
{
    public Button privacySettingsButton;
    public Button removeAdsButton;
    public VerifyAdmob verifyAdmob;

    void Start()
    {
        // Show privacy options for EEA users
        privacySettingsButton.gameObject.SetActive(verifyAdmob.ShouldShowPrivacyOptionsButton());
        
        // Show/hide remove ads button
        removeAdsButton.gameObject.SetActive(!verifyAdmob.IsRemoveAdsEnabled());
        
        // Wire up buttons
        privacySettingsButton.onClick.AddListener(verifyAdmob.ShowPrivacyOptionsForm);
        removeAdsButton.onClick.AddListener(OpenRemoveAdsStore);
    }
}
```

### Consent Status Check
```csharp
public class AdUIController : MonoBehaviour
{
    public GameObject adRelatedButtons;
    public VerifyAdmob verifyAdmob;

    void Update()
    {
        // Show ad-related UI only when appropriate
        bool showAdUI = verifyAdmob.CanUserRequestAds() && 
                       !verifyAdmob.IsRemoveAdsEnabled();
        adRelatedButtons.SetActive(showAdUI);
    }
}
```

---

## 📋 Final Checklist for Launch

### Development Complete
- [ ] All ad types working (Banner, Interstitial, Rewarded)
- [ ] Consent form appears for EEA users
- [ ] Privacy options button works
- [ ] Remove Ads feature implemented
- [ ] Error handling in place

### Production Ready
- [ ] Test ads disabled
- [ ] Consent debugging disabled
- [ ] Real ad unit IDs configured
- [ ] Privacy policy mentions GDPR compliance
- [ ] App store listing mentions ads

### Testing Complete
- [ ] Tested with EEA VPN (consent required)
- [ ] Tested with US VPN (consent not required)
- [ ] Tested Remove Ads purchase flow
- [ ] Tested privacy options modification
- [ ] Tested all ad types on both platforms

---

## 🆘 Emergency Troubleshooting

### "No ads showing at all"
1. Check: `AdsManager.Instance.IsInitialized` → should be `true`
2. Check: `verifyAdmob.CanUserRequestAds()` → should be `true`
3. Check: `verifyAdmob.IsRemoveAdsEnabled()` → should be `false`
4. Check: Internet connection
5. Check: Ad unit IDs are correct (not test IDs in production)

### "Consent issues"
1. Enable `enableConsentDebugging` temporarily
2. Check AdMob Console → Privacy & messaging
3. Test with EU VPN
4. Check console for UMP error messages

### "Mediation not working"
1. Check: Unity Ads package is installed
2. Check: Console for "[AdsManager] Unity Ads consent metadata configured"
3. Verify: AdMob Console mediation groups are active

---

## 📞 Support Resources

- **Comprehensive Guide**: `ComprehensiveImplementationGuide.md`
- **Usage Examples**: `ConsentUIExample.cs`
- **Official Docs**: [Google AdMob Unity Guide](https://developers.google.com/admob/unity)
- **Console Commands**: Right-click VerifyAdmob in Inspector

---

**Time to Implementation**: ~40 minutes for complete setup
**Time to Production**: ~1 hour including testing

🎉 **You're ready to monetize with full GDPR compliance!**