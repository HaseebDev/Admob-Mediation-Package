#if ADMOB_INSTALLED
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GoogleMobileAds.Api;
using Autech.Admob;

public class AdsExampleUI : MonoBehaviour
{
    public Button showRewardedBtn;
    public Button showInterstitialBtn;
    public Button toggleBannerBtn;
    public Button toggleRemoveAdsBtn;
    public Button privacyOptionsBtn;

    [Header("Debug Logging")]
    public TMP_Text debugLogText;

    Action Success, Fail;
    Action<Reward> OnRewardGranted;

    [Header("Test Configuration")]
    [SerializeField] private bool showDetailedLogs = true;
    [SerializeField] private BannerPosition testBannerPosition = BannerPosition.Bottom;
    
    [Header("Privacy Options Display")]
    [Tooltip("How to display the Privacy Options button when not required")]
    [SerializeField] private PrivacyButtonDisplayMode privacyButtonMode = PrivacyButtonDisplayMode.DisabledWhenNotRequired;
    
    public enum PrivacyButtonDisplayMode
    {
        AlwaysVisible,              // Always show button, enable/disable based on requirement (Google recommendation)
        DisabledWhenNotRequired,    // Show button but grey out when not required (current default)
        HideWhenNotRequired         // Completely hide button when not required (minimum compliance)
    }

    private string textLog = "ADMOB DEBUG LOG: \n";

    // Event-based logging system
    public delegate void DebugEvent(string msg);
    public static event DebugEvent OnDebugLog;

    private void Start()
    {
        showRewardedBtn.onClick.AddListener(() => CallRewarded(3));
        showInterstitialBtn.onClick.AddListener(() => CallInterstitial(2));
        toggleBannerBtn.onClick.AddListener(ToggleBannerTestCall);
        toggleRemoveAdsBtn.onClick.AddListener(TestToggleRemoveAds);
        
        if (privacyOptionsBtn != null)
            privacyOptionsBtn.onClick.AddListener(ShowPrivacyOptions);
            
        InvokeRepeating(nameof(UpdateButtonStates), 0f, 1f);
        
        SubscribeToConsentEvents();
    }

    private void OnEnable()
    {
        AdsExampleUI.OnDebugLog += HandleDebugLog;
    }

    private void OnDisable()
    {
        AdsExampleUI.OnDebugLog -= HandleDebugLog;
        UnsubscribeFromConsentEvents();
    }

    private void HandleDebugLog(string msg)
    {
        textLog += "\n" + msg + "\n";
        if (debugLogText != null)
        {
            debugLogText.text = textLog;
        }
    }

    private void UpdateButtonStates()
    {
        if (showRewardedBtn != null)
            showRewardedBtn.interactable = AdsManager.Instance.IsRewardedReady();

        if (showInterstitialBtn != null)
            showInterstitialBtn.interactable = AdsManager.Instance.IsInterstitialReady();

        if (toggleBannerBtn != null)
            toggleBannerBtn.interactable = AdsManager.Instance.IsBannerLoaded();

        if (toggleRemoveAdsBtn != null)
        {
            bool removeAdsEnabled = AdsManager.Instance.RemoveAds;
            Image image = toggleRemoveAdsBtn.GetComponent<Image>();

            if (removeAdsEnabled)
            {
                image.color = Color.red;
            }
            else
            {
                image.color = Color.green;
            }
        }
        
        if (privacyOptionsBtn != null)
        {
            bool isRequired = AdsManager.Instance.ShouldShowPrivacyOptionsButton();
            
            switch (privacyButtonMode)
            {
                case PrivacyButtonDisplayMode.AlwaysVisible:
                    // Always show and enable button (best practice)
                    privacyOptionsBtn.gameObject.SetActive(true);
                    privacyOptionsBtn.interactable = true;
                    break;
                    
                case PrivacyButtonDisplayMode.DisabledWhenNotRequired:
                    // Show button but disable when not required (current behavior)
                    privacyOptionsBtn.gameObject.SetActive(true);
                    privacyOptionsBtn.interactable = isRequired;
                    break;
                    
                case PrivacyButtonDisplayMode.HideWhenNotRequired:
                    // Completely hide button when not required (minimum compliance)
                    privacyOptionsBtn.gameObject.SetActive(isRequired);
                    privacyOptionsBtn.interactable = isRequired;
                    break;
            }
        }
    }

    #region Interstitial Ads
    public void CallInterstitial(int number)
    {
        LogTest($"Testing Interstitial Ad (Method {number})");

        switch (number)
        {
            case 0:
                Success = OnSuccessInter;
                AdsManager.Instance.ShowInterstitial(Success);
                break;
            case 1:
                AdsManager.Instance.ShowInterstitial();
                break;
            case 2:
                Success = OnSuccessInter;
                Fail = OnFailInter;
                AdsManager.Instance.ShowInterstitial(Success, Fail);
                break;
        }
    }

    private void OnSuccessInter()
    {
        LogTest("Interstitial Ad closed successfully");
    }

    private void OnFailInter()
    {
        LogTest("Interstitial Ad failed to show");
    }
    #endregion

    #region Rewarded Ads
    public void CallRewarded(int number)
    {
        LogTest($"Testing Rewarded Ad (Method {number})");

        switch (number)
        {
            case 0:
                Success = OnSuccessRewarded;
                AdsManager.Instance.ShowRewarded(Success);
                break;
            case 1:
                AdsManager.Instance.ShowRewarded();
                break;
            case 2:
                Success = OnSuccessRewarded;
                Fail = OnFailRewarded;
                AdsManager.Instance.ShowRewarded(Success, Fail);
                break;
            case 3:
                OnRewardGranted = OnRewardedAdReward;
                Success = OnSuccessRewarded;
                Fail = OnFailRewarded;
                AdsManager.Instance.ShowRewarded(OnRewardGranted, Success, Fail);
                break;
        }
    }

    private void OnSuccessRewarded()
    {
        LogTest("Rewarded Ad closed successfully");
    }

    private void OnFailRewarded()
    {
        LogTest("Rewarded Ad failed to show");
    }

    private void OnRewardedAdReward(Reward reward)
    {
        LogTest($"Reward Granted! Amount: {reward.Amount}, Type: {reward.Type}");
    }
    #endregion

    #region Rewarded Interstitial Ads
    public void CallRewardedInterstitial(int number)
    {
        LogTest($"Testing Rewarded Interstitial Ad (Method {number})");

        switch (number)
        {
            case 0:
                Success = OnSuccessRewardedInter;
                AdsManager.Instance.ShowRewardedInterstitial(Success);
                break;
            case 1:
                AdsManager.Instance.ShowRewardedInterstitial();
                break;
            case 2:
                Success = OnSuccessRewardedInter;
                Fail = OnFailRewardedInter;
                AdsManager.Instance.ShowRewardedInterstitial(Success, Fail);
                break;
            case 3:
                OnRewardGranted = OnRewardedInterAdReward;
                Success = OnSuccessRewardedInter;
                Fail = OnFailRewardedInter;
                AdsManager.Instance.ShowRewardedInterstitial(OnRewardGranted, Success, Fail);
                break;
        }
    }

    private void OnSuccessRewardedInter()
    {
        LogTest("Rewarded Interstitial Ad closed successfully");
    }

    private void OnFailRewardedInter()
    {
        LogTest("Rewarded Interstitial Ad failed to show");
    }

    private void OnRewardedInterAdReward(Reward reward)
    {
        LogTest($"Rewarded Interstitial Reward! Amount: {reward.Amount}, Type: {reward.Type}");
    }
    #endregion

    #region App Open Ads
    public void CallAppOpen(int number)
    {
        LogTest($"Testing App Open Ad (Method {number})");

        switch (number)
        {
            case 0:
                Success = OnSuccessAppOpen;
                AdsManager.Instance.ShowAppOpenAd(Success);
                break;
            case 1:
                AdsManager.Instance.ShowAppOpenAd();
                break;
            case 2:
                Success = OnSuccessAppOpen;
                Fail = OnFailAppOpen;
                AdsManager.Instance.ShowAppOpenAd(Success, Fail);
                break;
        }
    }

    private void OnSuccessAppOpen()
    {
        LogTest("App Open Ad closed successfully");
    }

    private void OnFailAppOpen()
    {
        LogTest("App Open Ad failed to show");
    }
    #endregion

    #region Banner Ads
    public void ShowBannerTestCall()
    {
        LogTest("Showing Banner Ad");
        AdsManager.Instance.ShowBanner(true);
    }

    public void HideBannerTestCall()
    {
        LogTest("Hiding Banner Ad");
        AdsManager.Instance.ShowBanner(false);
    }

    public void ToggleBannerTestCall()
    {
        bool isVisible = AdsManager.Instance.IsBannerVisible();
        AdsManager.Instance.ShowBanner(!isVisible);
        LogTest($"Toggling Banner Ad (Currently: {(!isVisible ? "Visible" : "Hidden")})");
    }

    public void ChangeBannerPosition()
    {
        LogTest($"Changing Banner Position to: {testBannerPosition}");
        AdsManager.Instance.SetBannerPosition(testBannerPosition);
    }

    public void CycleBannerPosition()
    {
        BannerPosition[] positions = {
            BannerPosition.Top, BannerPosition.Bottom, BannerPosition.TopLeft,
            BannerPosition.TopRight, BannerPosition.BottomLeft, BannerPosition.BottomRight,
            BannerPosition.Center
        };

        BannerPosition current = AdsManager.Instance.CurrentBannerPosition;
        int currentIndex = Array.IndexOf(positions, current);
        int nextIndex = (currentIndex + 1) % positions.Length;
        BannerPosition nextPosition = positions[nextIndex];

        LogTest($"Cycling Banner Position: {current} → {nextPosition}");
        AdsManager.Instance.SetBannerPosition(nextPosition);
    }

    public void TestBannerSizes()
    {
        BannerSize[] sizes = {
            BannerSize.Banner, BannerSize.LargeBanner, BannerSize.MediumRectangle,
            BannerSize.FullBanner, BannerSize.Leaderboard, BannerSize.Adaptive
        };

        BannerSize randomSize = sizes[UnityEngine.Random.Range(0, sizes.Length)];
        LogTest($"Testing Banner Size: {randomSize}");
        AdsManager.Instance.SetBannerSize(randomSize);
    }
    #endregion

    #region Remove Ads Testing
    public void TestEnableRemoveAds()
    {
        LogTest("Testing: Enable Remove Ads");
        AdsManager.Instance.RemoveAds = true;
    }

    public void TestDisableRemoveAds()
    {
        LogTest("Testing: Disable Remove Ads (Re-enable Ads)");
        AdsManager.Instance.RemoveAds = false;
    }

    public void TestToggleRemoveAds()
    {
        bool current = AdsManager.Instance.RemoveAds;
        LogTest($"Toggling Remove Ads (Currently: {(current ? "Enabled" : "Disabled")})");
        AdsManager.Instance.RemoveAds = !current;
    }

    public void TestPurchaseRemoveAds()
    {
        LogTest("Simulating Remove Ads Purchase");
        // Simulate a successful purchase
        AdsManager.Instance.RemoveAds = true;
        LogTest("Remove Ads purchased and saved!");
    }
    #endregion

    #region Ad Status Testing
    public void CheckAllAdStatus()
    {
        LogTest("Checking All Ad Status:");
        LogTest($"   Banner Ready: {AdsManager.Instance.IsBannerLoaded()}");
        LogTest($"   Interstitial Ready: {AdsManager.Instance.IsInterstitialReady()}");
        LogTest($"   Rewarded Ready: {AdsManager.Instance.IsRewardedReady()}");
        LogTest($"   Rewarded Interstitial Ready: {AdsManager.Instance.IsRewardedInterstitialReady()}");
        LogTest($"   App Open Available: {AdsManager.Instance.IsAppOpenAdAvailable()}");
        LogTest($"   Remove Ads Enabled: {AdsManager.Instance.RemoveAds}");
        LogTest($"   Ads Manager Initialized: {AdsManager.Instance.IsInitialized}");
        LogTest($"   Currently Showing Ad: {AdsManager.Instance.IsShowingAd}");
    }

    public void CheckAdIds()
    {
        LogTest("Checking Current Ad Unit IDs:");
        AdsManager.Instance.LogCurrentAdIds();

        bool isTest = AdsManager.Instance.AreTestAdIds();
        bool isValid = AdsManager.Instance.AreAdIdsValid();

        LogTest($"   Using Test IDs: {isTest}");
        LogTest($"   IDs Valid: {isValid}");

        if (isTest)
        {
            LogTest("   WARNING: Using Google test Ad Unit IDs!");
        }
    }
    #endregion

    #region Comprehensive Testing
    [ContextMenu("Test All Ads Sequentially")]
    public void TestAllAdsSequentially()
    {
        LogTest("Starting Comprehensive Ad Test Sequence");
        StartCoroutine(TestSequence());
    }

    private System.Collections.IEnumerator TestSequence()
    {
        LogTest("1. Testing Banner Ad");
        ShowBannerTestCall();
        yield return new WaitForSeconds(3f);

        LogTest("2. Testing Interstitial Ad");
        CallInterstitial(2);
        yield return new WaitForSeconds(1f);

        LogTest("3. Testing Rewarded Ad");
        CallRewarded(3);
        yield return new WaitForSeconds(1f);

        LogTest("4. Testing Rewarded Interstitial Ad");
        CallRewardedInterstitial(3);
        yield return new WaitForSeconds(1f);

        LogTest("5. Testing App Open Ad");
        CallAppOpen(2);
        yield return new WaitForSeconds(1f);

        LogTest("Comprehensive test sequence completed!");
    }

    [ContextMenu("Test Remove Ads Functionality")]
    public void TestRemoveAdsFunctionality()
    {
        LogTest("Testing Remove Ads Functionality");
        StartCoroutine(RemoveAdsTestSequence());
    }

    private System.Collections.IEnumerator RemoveAdsTestSequence()
    {
        LogTest("Initial Status:");
        CheckAllAdStatus();

        yield return new WaitForSeconds(2f);

        LogTest("Enabling Remove Ads...");
        TestEnableRemoveAds();

        yield return new WaitForSeconds(1f);

        LogTest("Status After Enabling Remove Ads:");
        CheckAllAdStatus();

        yield return new WaitForSeconds(2f);

        LogTest("Re-enabling Ads...");
        TestDisableRemoveAds();

        yield return new WaitForSeconds(1f);

        LogTest("Final Status:");
        CheckAllAdStatus();

        LogTest("Remove Ads functionality test completed!");
    }
    #endregion

    #region Utility Methods
    private void LogTest(string message)
    {
        if (showDetailedLogs)
        {
            OnDebugLog?.Invoke($"{message}");
            Debug.Log($"[TestCalls] {message}");
        }
    }

    public void ClearDebugLog()
    {
        textLog = "ADMOB DEBUG LOG: \n";
        if (debugLogText != null)
        {
            debugLogText.text = textLog;
        }
    }

    // Context Menu items for easy testing
    [ContextMenu("Show Interstitial")]
    private void ContextShowInterstitial() => CallInterstitial(2);

    [ContextMenu("Show Rewarded")]
    private void ContextShowRewarded() => CallRewarded(3);

    [ContextMenu("Show Rewarded Interstitial")]
    private void ContextShowRewardedInterstitial() => CallRewardedInterstitial(3);

    [ContextMenu("Show App Open")]
    private void ContextShowAppOpen() => CallAppOpen(2);

    [ContextMenu("Toggle Banner")]
    private void ContextToggleBanner() => ToggleBannerTestCall();

    [ContextMenu("Toggle Remove Ads")]
    private void ContextToggleRemoveAds() => TestToggleRemoveAds();

    [ContextMenu("Check All Status")]
    private void ContextCheckStatus() => CheckAllAdStatus();
    #endregion

    #region Consent & Privacy Options (NEW)
    private void SubscribeToConsentEvents()
    {
        if (AdsManager.Instance?.consentManager != null)
        {
            AdsManager.Instance.consentManager.OnConsentReady += OnConsentStatusChanged;
            AdsManager.Instance.consentManager.OnConsentError += OnConsentError;
        }
    }

    private void UnsubscribeFromConsentEvents()
    {
        if (AdsManager.Instance?.consentManager != null)
        {
            AdsManager.Instance.consentManager.OnConsentReady -= OnConsentStatusChanged;
            AdsManager.Instance.consentManager.OnConsentError -= OnConsentError;
        }
    }

    private void OnConsentStatusChanged(bool canRequestAds)
    {
        if (canRequestAds)
        {
            LogTest("✅ Consent Status: CAN REQUEST ADS");
        }
        else
        {
            LogTest("❌ Consent Status: CANNOT REQUEST ADS (User revoked or denied)");
        }
        
        LogConsentDetails();
    }

    private void OnConsentError(string errorMessage)
    {
        LogTest($"⚠️ CONSENT ERROR: {errorMessage}");
    }

    public void ShowPrivacyOptions()
    {
        LogTest("Opening Privacy Options Form...");
        
        if (AdsManager.Instance.ShouldShowPrivacyOptionsButton())
        {
            AdsManager.Instance.ShowPrivacyOptionsForm();
        }
        else
        {
            LogTest("Privacy Options not required (user not in EEA or already declined)");
        }
    }

    public void LogConsentDetails()
    {
        LogTest("=== CONSENT STATUS ===");
        LogTest($"Can Request Ads: {AdsManager.Instance.CanUserRequestAds()}");
        LogTest($"Consent Status: {AdsManager.Instance.GetCurrentConsentStatus()}");
        
        string consentType = AdsManager.Instance.GetConsentType();
        LogTest($"Consent Type: {consentType}");
        
        string tcfString = AdsManager.Instance.GetTCFConsentString();
        if (!string.IsNullOrEmpty(tcfString))
        {
            LogTest($"TCF String: {tcfString.Substring(0, Math.Min(30, tcfString.Length))}...");
            
            bool hasStorage = AdsManager.Instance.HasConsentForPurpose(1);
            bool hasPersonalization = AdsManager.Instance.HasConsentForPurpose(3);
            bool hasAdSelection = AdsManager.Instance.HasConsentForPurpose(4);
            bool hasAnalytics = AdsManager.Instance.HasConsentForPurpose(7);
            
            LogTest($"TCF Purposes:");
            LogTest($"  - Storage (1): {hasStorage}");
            LogTest($"  - Personalization (3): {hasPersonalization}");
            LogTest($"  - Ad Selection (4): {hasAdSelection}");
            LogTest($"  - Analytics (7): {hasAnalytics}");
        }
        else
        {
            LogTest("TCF String: Not available");
        }
        
        LogTest("======================");
    }

    [ContextMenu("Show Privacy Options")]
    private void ContextShowPrivacyOptions() => ShowPrivacyOptions();

    [ContextMenu("Log Consent Details")]
    private void ContextLogConsentDetails() => LogConsentDetails();

    [ContextMenu("Check Consent Status")]
    private void ContextCheckConsentStatus()
    {
        AdsManager.Instance.CheckConsentStatus();
        LogTest("Manually triggered consent status check");
    }
    #endregion

}
#endif // ADMOB_INSTALLED
