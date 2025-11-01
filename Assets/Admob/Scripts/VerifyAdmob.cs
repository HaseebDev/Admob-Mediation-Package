#if ADMOB_INSTALLED
using System;
using System.Threading.Tasks;
using UnityEngine;
using GoogleMobileAds.Ump.Api;
using Autech.Admob;

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
    [SerializeField] private bool useAdaptiveBanners = false;
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
        // Subscribe to events
        AdsManager.OnRemoveAdsChanged += OnRemoveAdsStatusChanged;
        AdsManager.OnRemoveAdsLoadedFromStorage += OnRemoveAdsLoadedFromStorage;

        // Fire-and-forget is safe here: InitializeAsync has internal try-catch with comprehensive error logging
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var manager = AdsManager.Instance;
            var settings = ApplyAllSettings(manager);

            manager.VerifyHit();

            await manager.InitializeAsync();

            if (showBannerOnStart && !settings.RemoveAds)
            {
                Debug.Log("[VerifyAdmob] Initialization complete - showing banner");
                manager.SetInitialBannerVisibility(true);
            }
            else
            {
                Debug.Log("[VerifyAdmob] Initialization complete - hiding banner");
                manager.SetInitialBannerVisibility(false);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[VerifyAdmob] Initialization failed: {ex.Message}");
            Debug.LogException(ex);
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
            Debug.Log($"[VerifyAdmob] Remove Ads loaded from storage: {loadedStatus}");
        }
    }

    private AdsManager.AdsManagerSettings BuildManagerSettings(AdsManager manager)
    {
        return new AdsManager.AdsManagerSettings
        {
            RemoveAds = manager.RemoveAds,  // Always use manager's persisted value, never inspector default
            ForceEEAGeographyForTesting = forceEEAGeographyForTesting,
            EnableConsentDebugging = enableConsentDebugging,
            AlwaysRequestConsentUpdate = alwaysRequestConsentUpdate,
            TagForUnderAgeOfConsent = false,
            AutoShowAppOpenAds = autoShowAppOpenAds,
            EnableTestAds = enableTestAds,
            AppOpenCooldownTime = appOpenCooldownTime,
            UseAdaptiveBanners = useAdaptiveBanners,
            EnableCollapsibleBanners = enableCollapsibleBanners,
            PreferredBannerSize = preferredBannerSize,
            BannerPosition = bannerPosition,
            AndroidBannerId = androidBannerId,
            AndroidInterstitialId = androidInterstitialId,
            AndroidRewardedId = androidRewardedId,
            AndroidRewardedInterstitialId = androidRewardedInterstitialId,
            AndroidAppOpenId = androidAppOpenId,
            IosBannerId = iosBannerId,
            IosInterstitialId = iosInterstitialId,
            IosRewardedId = iosRewardedId,
            IosRewardedInterstitialId = iosRewardedInterstitialId,
            IosAppOpenId = iosAppOpenId
        };
    }

    private AdsManager.AdsManagerSettings ApplyAllSettings(AdsManager manager)
    {
        var settings = BuildManagerSettings(manager);
        manager.ApplyConfiguration(settings);

        if (showPersistenceDebugLogs)
        {
            Debug.Log("[VerifyAdmob] Ad Unit IDs applied");
        }

        Debug.Log($"[VerifyAdmob] All settings applied. RemoveAds: {settings.RemoveAds}");
        LogCurrentAdStatus(manager);

        return settings;
    }

    // Public methods
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

    // Status methods
    public bool IsRemoveAdsEnabled() => removeAds;
    public bool IsAdsManagerInitialized() => AdsManager.Instance.IsInitialized;
    public bool IsAnyAdShowing() => AdsManager.Instance.IsShowingAd;
    public bool IsBannerVisible() => AdsManager.Instance.IsBannerVisible();
    public bool IsInterstitialReady() => !removeAds && AdsManager.Instance.IsInterstitialReady();
    public bool IsRewardedReady() => AdsManager.Instance.IsRewardedReady();
    public bool IsRewardedInterstitialReady() => AdsManager.Instance.IsRewardedInterstitialReady();
    public bool IsAppOpenAdAvailable() => !removeAds && AdsManager.Instance.IsAppOpenAdAvailable();

    // Test methods
    [ContextMenu("Show Interstitial")]
    public void TestShowInterstitial()
    {
        if (removeAds)
        {
            Debug.Log("[VerifyAdmob] Cannot show - RemoveAds enabled");
            return;
        }
        AdsManager.Instance.ShowInterstitial();
    }

    [ContextMenu("Show Rewarded")]
    public void TestShowRewarded() => AdsManager.Instance.ShowRewarded();

    [ContextMenu("Show Rewarded Interstitial")]
    public void TestShowRewardedInterstitial() => AdsManager.Instance.ShowRewardedInterstitial();

    [ContextMenu("Show App Open")]
    public void TestShowAppOpen()
    {
        if (removeAds)
        {
            Debug.Log("[VerifyAdmob] Cannot show - RemoveAds enabled");
            return;
        }
        AdsManager.Instance.ShowAppOpenAd();
    }

    [ContextMenu("Toggle Banner")]
    public void TestToggleBanner()
    {
        if (removeAds)
        {
            Debug.Log("[VerifyAdmob] Cannot show - RemoveAds enabled");
            return;
        }

        bool isVisible = AdsManager.Instance.IsBannerVisible();
        AdsManager.Instance.ShowBanner(!isVisible);
    }

    [ContextMenu("Toggle Remove Ads")]
    public void TestToggleRemoveAds()
    {
        SetRemoveAds(!removeAds);
        Debug.Log($"[VerifyAdmob] Remove Ads: {(removeAds ? "ENABLED" : "DISABLED")}");
    }

    // Persistence methods
    [ContextMenu("Force Load From Storage")]
    public void TestForceLoadFromStorage() => AdsManager.Instance.ForceLoadFromStorage();

    [ContextMenu("Force Save To Storage")]
    public void TestForceSaveToStorage() => AdsManager.Instance.ForceSaveToStorage();

    [ContextMenu("Clear Remove Ads Data")]
    public void TestClearRemoveAdsData()
    {
        AdsManager.Instance.ClearRemoveAdsData();
        Debug.Log("[VerifyAdmob] Remove Ads data cleared");
    }

    public bool HasRemoveAdsDataInStorage() => AdsManager.Instance.HasRemoveAdsDataInStorage();

    public void PurchaseRemoveAds()
    {
        Debug.Log("[VerifyAdmob] Remove Ads purchased!");
        SetRemoveAds(true);
    }

    public void RestorePurchases()
    {
        Debug.Log("[VerifyAdmob] Restoring purchases...");
        AdsManager.Instance.ForceLoadFromStorage();
    }

    // Ad Unit ID Management
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
        AdsManager.Instance.ApplyConfiguration(BuildManagerSettings(AdsManager.Instance));
        Debug.Log("[VerifyAdmob] Ads refreshed");
    }

    [ContextMenu("Log Current Ad IDs")]
    public void LogCurrentAdIds() => AdsManager.Instance.LogCurrentAdIds();

    [ContextMenu("Check If Test Ad IDs")]
    public void CheckIfTestAdIds()
    {
        bool isTest = AdsManager.Instance.AreTestAdIds();
        Debug.Log($"[VerifyAdmob] Using Test Ad IDs: {isTest}");

        if (isTest)
        {
            Debug.LogWarning("[VerifyAdmob] WARNING: Replace with real Ad Unit IDs before publishing!");
        }
    }

    [ContextMenu("Validate Ad IDs")]
    public void ValidateAdIds()
    {
        bool valid = AdsManager.Instance.AreAdIdsValid();
        Debug.Log($"[VerifyAdmob] Ad Unit IDs Valid: {valid}");
    }

    private void LogCurrentAdStatus(AdsManager manager)
    {
        if (!showPersistenceDebugLogs)
        {
            return;
        }

        Debug.Log($"[VerifyAdmob] Remove Ads: {manager.RemoveAds}");
        Debug.Log($"[VerifyAdmob] Test IDs: {manager.AreTestAdIds()}");
        Debug.Log($"[VerifyAdmob] IDs Valid: {manager.AreAdIdsValid()}");
        Debug.Log($"[VerifyAdmob] Initialized: {manager.IsInitialized}");
    }

    // Consent methods
    [ContextMenu("Refresh Mediation Consent")]
    public void RefreshMediationConsent()
    {
        AdsManager.Instance.RefreshMediationConsent();
        Debug.Log("[VerifyAdmob] Mediation consent refreshed");
    }

    [ContextMenu("Show Privacy Options")]
    public void ShowPrivacyOptionsForm() => AdsManager.Instance.ShowPrivacyOptionsForm();

    public bool ShouldShowPrivacyOptionsButton() => AdsManager.Instance.ShouldShowPrivacyOptionsButton();

    public ConsentStatus GetCurrentConsentStatus() => AdsManager.Instance.GetCurrentConsentStatus();

    public bool CanUserRequestAds() => AdsManager.Instance.CanUserRequestAds();

    [ContextMenu("Log Consent Status")]
    public void LogConsentStatus()
    {
        Debug.Log("=== [VerifyAdmob] CONSENT STATUS ===");
        Debug.Log($"Consent Status: {GetCurrentConsentStatus()}");
        Debug.Log($"Can Request Ads: {CanUserRequestAds()}");
        Debug.Log($"Show Privacy Options: {ShouldShowPrivacyOptionsButton()}");
        Debug.Log($"RemoveAds: {IsRemoveAdsEnabled()}");
        Debug.Log($"Initialized: {IsAdsManagerInitialized()}");
        Debug.Log("===================================");
    }

    [ContextMenu("Log Encryption Info")]
    public void LogEncryptionInfo() => AdsManager.Instance.LogEncryptionInfo();

    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        if (appOpenCooldownTime < 0)
            appOpenCooldownTime = 0;
    }
}
#endif // ADMOB_INSTALLED
