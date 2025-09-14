using UnityEngine;
using System.Collections;

public class VerifyAdmob : MonoBehaviour
{
    [Header("Ad Display Settings")]
    [SerializeField] private BannerPosition bannerPosition = BannerPosition.Bottom;
    [SerializeField] private bool showBannerOnStart = true;

    [Header("Remove Ads")]
    [SerializeField] private bool removeAds = false;
    [SerializeField] private bool showPersistenceDebugLogs = true;

    [Header("Ad Configuration")]
    [SerializeField] private bool autoShowAppOpenAds = true;
    [SerializeField] private bool enableTestAds = true;
    [SerializeField] private float appOpenCooldownTime = 4f;

    [Header("Banner Configuration")]
    [SerializeField] private bool useAdaptiveBanners = true;
    [SerializeField] private bool enableCollapsibleBanners = false;
    [SerializeField] private BannerSize preferredBannerSize = BannerSize.Banner;

    [Header("Consent Configuration")]
    [Space(5)]
    [Tooltip("FOR TESTING ONLY - Forces EEA geography to test consent forms. DISABLE FOR PRODUCTION.")]
    [SerializeField] private bool forceEEAGeographyForTesting = false;

    [Space(5)]
    [Tooltip("FOR TESTING ONLY - Enables consent debugging and bypasses certain errors. DISABLE FOR PRODUCTION.")]
    [SerializeField] private bool enableConsentDebugging = false;

    [Space(5)]
    [Tooltip("PRODUCTION SETTING - Always call consent Update() regardless of geography (Google recommended).")]
    [SerializeField] private bool alwaysRequestConsentUpdate = true;

    [Header("Ad Unit IDs - Android")]
    [SerializeField] private string androidBannerId = "ca-app-pub-3940256099942544/6300978111";
    [SerializeField] private string androidInterstitialId = "ca-app-pub-3940256099942544/1033173712";
    [SerializeField] private string androidRewardedId = "ca-app-pub-3940256099942544/5224354917";
    [SerializeField] private string androidRewardedInterstitialId = "ca-app-pub-3940256099942544/5354046379";
    [SerializeField] private string androidAppOpenId = "ca-app-pub-3940256099942544/9257395921";

    [Header("Ad Unit IDs - iOS")]
    [SerializeField] private string iosBannerId = "ca-app-pub-3940256099942544/2934735716";
    [SerializeField] private string iosInterstitialId = "ca-app-pub-3940256099942544/4411468910";
    [SerializeField] private string iosRewardedId = "ca-app-pub-3940256099942544/1712485313";
    [SerializeField] private string iosRewardedInterstitialId = "ca-app-pub-3940256099942544/6978759866";
    [SerializeField] private string iosAppOpenId = "ca-app-pub-3940256099942544/5575463023";

    void Start()
    {
        // Move all AdsManager interaction to Start()
        AdsManager.OnRemoveAdsChanged += OnRemoveAdsStatusChanged;
        AdsManager.OnRemoveAdsLoadedFromStorage += OnRemoveAdsLoadedFromStorage;

        AdsManager.Instance.VerifyHit();

        // Apply settings after AdsManager is properly created
        StartCoroutine(ApplySettingsWhenReady());

        AdsManager.Instance.SetBannerPosition(bannerPosition);
        StartCoroutine(HandleInitialBannerVisibility());
    }

    private IEnumerator ApplySettingsWhenReady()
    {
        // Wait one frame to ensure proper initialization order
        yield return null;
        
        Debug.Log("[VerifyAdmob] Applying settings after proper initialization");
        ApplyAllSettings();
    }

    private IEnumerator HandleInitialBannerVisibility()
    {
        while (!AdsManager.Instance.IsInitialized)
        {
            yield return null;
        }

        while (AdsManager.Instance.IsFirstTimeLoading)
        {
            yield return null;
        }

        if (showBannerOnStart && !removeAds)
        {
            Debug.Log("[VerifyAdmob] First-time loading complete - showing banner as configured");
            AdsManager.Instance.SetInitialBannerVisibility(true);
        }
        else
        {
            Debug.Log("[VerifyAdmob] First-time loading complete - hiding banner as configured");
            AdsManager.Instance.SetInitialBannerVisibility(false);
        }
    }

    private void OnDestroy()
    {
        AdsManager.OnRemoveAdsChanged -= OnRemoveAdsStatusChanged;
        AdsManager.OnRemoveAdsLoadedFromStorage -= OnRemoveAdsLoadedFromStorage;
    }

    private void OnRemoveAdsStatusChanged(bool newStatus)
    {
        removeAds = newStatus;

        if (showPersistenceDebugLogs)
        {
            Debug.Log($"[VerifyAdmob] Remove Ads status changed: {newStatus}");
        }

        if (newStatus)
        {
            AdsManager.Instance.ShowBanner(false);
        }
        else if (showBannerOnStart)
        {
            AdsManager.Instance.ShowBanner(true);
        }
    }

    private void OnRemoveAdsLoadedFromStorage(bool loadedStatus)
    {
        removeAds = loadedStatus;

        if (showPersistenceDebugLogs)
        {
            Debug.Log($"[VerifyAdmob] Remove Ads status loaded from storage: {loadedStatus}");
        }
    }

    private void ApplyAllSettings()
    {
        ApplyAdUnitIds();

        // Core settings
        AdsManager.Instance.RemoveAds = removeAds;
        
        // Consent configuration (production + testing overrides)
        AdsManager.Instance.ForceEEAGeographyForTesting = forceEEAGeographyForTesting;
        AdsManager.Instance.EnableConsentDebugging = enableConsentDebugging;
        AdsManager.Instance.AlwaysRequestConsentUpdate = alwaysRequestConsentUpdate;

        // Ad configuration
        AdsManager.Instance.AutoShowAppOpenAds = autoShowAppOpenAds;
        AdsManager.Instance.EnableTestAds = enableTestAds;
        AdsManager.Instance.AppOpenCooldownTime = appOpenCooldownTime;

        // Banner configuration  
        AdsManager.Instance.UseAdaptiveBanners = useAdaptiveBanners;
        AdsManager.Instance.EnableCollapsibleBanners = enableCollapsibleBanners;
        AdsManager.Instance.PreferredBannerSize = preferredBannerSize;

        Debug.Log($"All ad settings applied. Remove Ads: {removeAds}");
        LogCurrentAdStatus();
    }

    private void ApplyAdUnitIds()
    {
        AdsManager.Instance.SetAllAdIds(
            androidBannerId, androidInterstitialId, androidRewardedId, androidRewardedInterstitialId, androidAppOpenId,
            iosBannerId, iosInterstitialId, iosRewardedId, iosRewardedInterstitialId, iosAppOpenId
        );

        if (showPersistenceDebugLogs)
        {
            Debug.Log("[VerifyAdmob] Ad Unit IDs applied to AdsManager");
        }
    }

    // Public methods to modify settings at runtime
    public void SetRemoveAds(bool remove)
    {
        removeAds = remove;
        AdsManager.Instance.RemoveAds = remove;

        if (remove)
        {
            AdsManager.Instance.ShowBanner(false);
        }
        else if (showBannerOnStart)
        {
            AdsManager.Instance.ShowBanner(true);
        }
    }

    public void SetBannerPosition(BannerPosition position)
    {
        bannerPosition = position;
        AdsManager.Instance.SetBannerPosition(position);
    }

    public void SetBannerSize(BannerSize size)
    {
        preferredBannerSize = size;
        AdsManager.Instance.PreferredBannerSize = size;
        AdsManager.Instance.SetBannerSize(size);
    }

    public void SetAutoShowAppOpen(bool enable)
    {
        autoShowAppOpenAds = enable;
        AdsManager.Instance.AutoShowAppOpenAds = enable;
    }

    public void SetTestAds(bool enable)
    {
        enableTestAds = enable;
        AdsManager.Instance.EnableTestAds = enable;
    }

    public void SetAppOpenCooldown(float cooldown)
    {
        appOpenCooldownTime = cooldown;
        AdsManager.Instance.AppOpenCooldownTime = cooldown;
    }

    public void SetAdaptiveBanners(bool enable)
    {
        useAdaptiveBanners = enable;
        AdsManager.Instance.UseAdaptiveBanners = enable;
        AdsManager.Instance.EnableAdaptiveBanners(enable);
    }

    public void SetCollapsibleBanners(bool enable)
    {
        enableCollapsibleBanners = enable;
        AdsManager.Instance.EnableCollapsibleBanners = enable;
    }

    // Status checking methods
    public bool IsRemoveAdsEnabled()
    {
        return removeAds;
    }

    public bool IsAdsManagerInitialized()
    {
        return AdsManager.Instance.IsInitialized;
    }

    public bool IsAnyAdShowing()
    {
        return AdsManager.Instance.IsShowingAd;
    }

    public bool IsBannerVisible()
    {
        return AdsManager.Instance.IsBannerVisible();
    }

    public bool IsInterstitialReady()
    {
        return !removeAds && AdsManager.Instance.IsInterstitialReady();
    }

    public bool IsRewardedReady()
    {
        return AdsManager.Instance.IsRewardedReady();
    }

    public bool IsRewardedInterstitialReady()
    {
        return AdsManager.Instance.IsRewardedInterstitialReady();
    }

    public bool IsAppOpenAdAvailable()
    {
        return !removeAds && AdsManager.Instance.IsAppOpenAdAvailable();
    }

    // Test methods for showing ads (respects Remove Ads setting)
    [ContextMenu("Show Interstitial")]
    public void TestShowInterstitial()
    {
        if (removeAds)
        {
            Debug.Log("Cannot show interstitial - Remove Ads is enabled");
            return;
        }
        AdsManager.Instance.ShowInterstitial();
    }

    [ContextMenu("Show Rewarded")]
    public void TestShowRewarded()
    {
        AdsManager.Instance.ShowRewarded();
    }

    [ContextMenu("Show Rewarded Interstitial")]
    public void TestShowRewardedInterstitial()
    {
        AdsManager.Instance.ShowRewardedInterstitial();
    }

    [ContextMenu("Show App Open")]
    public void TestShowAppOpen()
    {
        if (removeAds)
        {
            Debug.Log("Cannot show app open ad - Remove Ads is enabled");
            return;
        }
        AdsManager.Instance.ShowAppOpenAd();
    }

    [ContextMenu("Toggle Banner")]
    public void TestToggleBanner()
    {
        if (removeAds)
        {
            Debug.Log("Cannot show banner - Remove Ads is enabled");
            return;
        }

        bool isVisible = AdsManager.Instance.IsBannerVisible();
        AdsManager.Instance.ShowBanner(!isVisible);
    }

    [ContextMenu("Toggle Remove Ads")]
    public void TestToggleRemoveAds()
    {
        SetRemoveAds(!removeAds);
        Debug.Log($"Remove Ads is now: {(removeAds ? "ENABLED" : "DISABLED")}");
    }

    // Persistence-related methods
    [ContextMenu("Force Load From Storage")]
    public void TestForceLoadFromStorage()
    {
        AdsManager.Instance.ForceLoadFromStorage();
    }

    [ContextMenu("Force Save To Storage")]
    public void TestForceSaveToStorage()
    {
        AdsManager.Instance.ForceSaveToStorage();
    }

    [ContextMenu("Clear Remove Ads Data")]
    public void TestClearRemoveAdsData()
    {
        AdsManager.Instance.ClearRemoveAdsData();
        Debug.Log("Remove Ads data cleared!");
    }

    public bool HasRemoveAdsDataInStorage()
    {
        return AdsManager.Instance.HasRemoveAdsDataInStorage();
    }

    public void PurchaseRemoveAds()
    {
        // This method would typically be called after a successful IAP purchase
        Debug.Log("Remove Ads purchased! Enabling...");
        SetRemoveAds(true);

        if (showPersistenceDebugLogs)
        {
            Debug.Log("Remove Ads status has been saved to persistent storage");
        }
    }

    public void RestorePurchases()
    {
        // This method would typically be called to restore previous purchases
        Debug.Log("Restoring purchases...");
        AdsManager.Instance.ForceLoadFromStorage();
    }

    // Ad Unit ID Management Methods
    public void SetAndroidAdIds(string bannerId, string interstitialId, string rewardedId, string rewardedInterstitialId, string appOpenId)
    {
        androidBannerId = bannerId;
        androidInterstitialId = interstitialId;
        androidRewardedId = rewardedId;
        androidRewardedInterstitialId = rewardedInterstitialId;
        androidAppOpenId = appOpenId;

        AdsManager.Instance.SetAndroidAdIds(bannerId, interstitialId, rewardedId, rewardedInterstitialId, appOpenId);
        Debug.Log("[VerifyAdmob] Android Ad Unit IDs updated");
    }

    public void SetIosAdIds(string bannerId, string interstitialId, string rewardedId, string rewardedInterstitialId, string appOpenId)
    {
        iosBannerId = bannerId;
        iosInterstitialId = interstitialId;
        iosRewardedId = rewardedId;
        iosRewardedInterstitialId = rewardedInterstitialId;
        iosAppOpenId = appOpenId;

        AdsManager.Instance.SetIosAdIds(bannerId, interstitialId, rewardedId, rewardedInterstitialId, appOpenId);
        Debug.Log("[VerifyAdmob] iOS Ad Unit IDs updated");
    }

    public void RefreshAdsWithCurrentIds()
    {
        ApplyAdUnitIds();
        Debug.Log("[VerifyAdmob] Ads refreshed with current Ad Unit IDs");
    }

    [ContextMenu("Log Current Ad IDs")]
    public void LogCurrentAdIds()
    {
        AdsManager.Instance.LogCurrentAdIds();
    }

    [ContextMenu("Check If Test Ad IDs")]
    public void CheckIfTestAdIds()
    {
        bool isTest = AdsManager.Instance.AreTestAdIds();
        Debug.Log($"Using Test Ad IDs: {isTest}");

        if (isTest)
        {
            Debug.LogWarning("WARNING: You are using Google's test Ad Unit IDs. Make sure to replace these with your real Ad Unit IDs before publishing!");
        }
        else
        {
            Debug.Log("Using custom Ad Unit IDs (not test IDs)");
        }
    }

    [ContextMenu("Validate Ad IDs")]
    public void ValidateAdIds()
    {
        bool valid = AdsManager.Instance.AreAdIdsValid();
        Debug.Log($"Ad Unit IDs Valid: {valid}");

        if (!valid)
        {
            Debug.LogError("Some Ad Unit IDs are empty or invalid!");
        }
        else
        {
            Debug.Log("All Ad Unit IDs are valid");
        }
    }

    private void LogCurrentAdStatus()
    {
        if (showPersistenceDebugLogs)
        {
            Debug.Log($"[VerifyAdmob] Current Ad Status:");
            Debug.Log($"- Remove Ads: {removeAds}");
            Debug.Log($"- Test Ad IDs: {AdsManager.Instance.AreTestAdIds()}");
            Debug.Log($"- Ad IDs Valid: {AdsManager.Instance.AreAdIdsValid()}");
            Debug.Log($"- Ads Manager Initialized: {AdsManager.Instance.IsInitialized}");
        }
    }

    // Getters for current Ad Unit IDs
    public string GetCurrentBannerId() => AdsManager.Instance.CurrentBannerId;
    public string GetCurrentInterstitialId() => AdsManager.Instance.CurrentInterstitialId;
    public string GetCurrentRewardedId() => AdsManager.Instance.CurrentRewardedId;
    public string GetCurrentRewardedInterstitialId() => AdsManager.Instance.CurrentRewardedInterstitialId;
    public string GetCurrentAppOpenId() => AdsManager.Instance.CurrentAppOpenId;

    // Getters for platform-specific IDs
    public string GetAndroidBannerId() => androidBannerId;
    public string GetAndroidInterstitialId() => androidInterstitialId;
    public string GetAndroidRewardedId() => androidRewardedId;
    public string GetAndroidRewardedInterstitialId() => androidRewardedInterstitialId;
    public string GetAndroidAppOpenId() => androidAppOpenId;

    public string GetIosBannerId() => iosBannerId;
    public string GetIosInterstitialId() => iosInterstitialId;
    public string GetIosRewardedId() => iosRewardedId;
    public string GetIosRewardedInterstitialId() => iosRewardedInterstitialId;
    public string GetIosAppOpenId() => iosAppOpenId;

    // Validation method
    private void OnValidate()
    {
        // CRITICAL FIX: Don't access AdsManager.Instance during OnValidate
        if (!Application.isPlaying)
            return;
        
        // Keep this validation - it's harmless and useful
        if (appOpenCooldownTime < 0)
            appOpenCooldownTime = 0;

        // Don't access AdsManager.Instance here anymore
        // The settings will be applied in Start() instead
    }
}