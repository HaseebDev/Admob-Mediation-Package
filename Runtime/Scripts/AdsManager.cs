using UnityEngine;
using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using GoogleMobileAds.Mediation.UnityAds.Api;
using GoogleMobileAds.Common;
using System.Collections.Generic;
using System.Collections;

public enum BannerPosition
{
    Top,
    Bottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Center
}

public enum BannerSize
{
    Banner,              // 320x50
    LargeBanner,         // 320x100
    MediumRectangle,     // 300x250
    FullBanner,          // 468x60
    Leaderboard,         // 728x90
    SmartBanner,         // Deprecated but kept for compatibility
    Adaptive,            // Adaptive banner (recommended)
    AdaptiveInline       // Adaptive inline banner
}

public class AdsManager : MonoBehaviour
{
    private static AdsManager instance;
    public static AdsManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("AdsManager");
                instance = go.AddComponent<AdsManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    // Ad Unit IDs - Default test IDs (override these with real IDs in production)
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

    // Current platform Ad Unit IDs (computed properties)
    private string BANNER_ID => GetCurrentPlatformAdId(androidBannerId, iosBannerId);
    private string INTERSTITIAL_ID => GetCurrentPlatformAdId(androidInterstitialId, iosInterstitialId);
    private string REWARDED_ID => GetCurrentPlatformAdId(androidRewardedId, iosRewardedId);
    private string REWARDED_INTERSTITIAL_ID => GetCurrentPlatformAdId(androidRewardedInterstitialId, iosRewardedInterstitialId);
    private string APP_OPEN_ID => GetCurrentPlatformAdId(androidAppOpenId, iosAppOpenId);

    private string GetCurrentPlatformAdId(string androidId, string iosId)
    {
#if UNITY_ANDROID
        return androidId;
#elif UNITY_IOS
        return iosId;
#else
        return ""; // Editor or unsupported platform
#endif
    }

    [Header("Ad Settings")]
    [SerializeField] private bool autoShowAppOpenAds = true;
    [SerializeField] private bool enableTestAds = true;
    [SerializeField] private float appOpenCooldownTime = 4f; // seconds between app open ads
    [SerializeField] private bool removeAds = false; // Remove non-rewarded ads

    [Header("Persistence Settings")]
    [SerializeField] private bool useEncryptedStorage = true;
    [SerializeField] private bool enableCloudSync = false;
    [SerializeField] private string removeAdsKey = "RemoveAds_Status";
    [SerializeField] private string encryptionKey = "YourCustomEncryptionKey123"; // Used as salt for AES-256 encryption - customize this per app

    [Header("Banner Settings")]
    [SerializeField] private bool useAdaptiveBanners = true;
    [SerializeField] private bool enableCollapsibleBanners = false;
    [SerializeField] private BannerSize preferredBannerSize = BannerSize.Banner;

    [Header("Consent Configuration")]
    [Space(5)]
    [Tooltip("TESTING ONLY: Force EEA geography for consent testing. Must be FALSE for production.")]
    [SerializeField] private bool forceEEAGeographyForTesting = false;

    [Space(5)]
    [Tooltip("TESTING ONLY: Enable debugging and error bypasses. Must be FALSE for production.")]
    [SerializeField] private bool enableConsentDebugging = false;

    [Space(5)]
    [Tooltip("PRODUCTION: Always request consent update (Google's recommendation). Should be TRUE.")]
    [SerializeField] private bool alwaysRequestConsentUpdate = true;

    private BannerView bannerView;
    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;
    private RewardedInterstitialAd rewardedInterstitialAd;
    private AppOpenAd appOpenAd;
    private DateTime appOpenExpireTime;
    private DateTime lastAppOpenShownTime;
    private bool isInitialized = false;
    private bool isShowingAd = false;
    private bool isAppOpenAdShowing = false;
    private AdPosition currentBannerPosition = AdPosition.Bottom;
    private bool isBannerLoaded = false;
    private bool isBannerVisible = false;

    // App state management for app open ads
    // AppStateEventNotifier is static, no instance needed

    // Retry counters for failed loads
    private int interstitialRetryAttempt;
    private int rewardedRetryAttempt;
    private int rewardedInterstitialRetryAttempt;
    private int appOpenRetryAttempt;
    private const int maxRetryCount = 3;

    // Cold start management
    private bool isColdStart = true;
    private bool hasShownFirstAppOpenAd = false;
    private bool isFirstTimeLoading = true;

    // Public properties to expose all configurable values
    public bool AutoShowAppOpenAds
    {
        get => autoShowAppOpenAds;
        set => autoShowAppOpenAds = value;
    }

    public bool EnableTestAds
    {
        get => enableTestAds;
        set => enableTestAds = value;
    }

    public float AppOpenCooldownTime
    {
        get => appOpenCooldownTime;
        set => appOpenCooldownTime = value;
    }

    public bool RemoveAds
    {
        get => removeAds;
        set
        {
            if (removeAds != value)
            {
                removeAds = value;
                SaveRemoveAdsStatus(value);
                OnRemoveAdsChangedInternal(value);
                OnRemoveAdsChanged?.Invoke(value);
            }
        }
    }

    public bool UseAdaptiveBanners
    {
        get => useAdaptiveBanners;
        set => useAdaptiveBanners = value;
    }

    public bool EnableCollapsibleBanners
    {
        get => enableCollapsibleBanners;
        set => enableCollapsibleBanners = value;
    }

    public BannerSize PreferredBannerSize
    {
        get => preferredBannerSize;
        set => preferredBannerSize = value;
    }

    // Consent Configuration Properties
    public bool ForceEEAGeographyForTesting
    {
        get => forceEEAGeographyForTesting;
        set => forceEEAGeographyForTesting = value;
    }

    public bool EnableConsentDebugging
    {
        get => enableConsentDebugging;
        set => enableConsentDebugging = value;
    }

    public bool AlwaysRequestConsentUpdate
    {
        get => alwaysRequestConsentUpdate;
        set => alwaysRequestConsentUpdate = value;
    }

    // Current ad availability properties
    public bool IsInitialized => isInitialized;
    public bool IsShowingAd => isShowingAd;
    public bool IsFirstTimeLoading => isFirstTimeLoading;
    public BannerPosition CurrentBannerPosition => ConvertFromAdPosition(currentBannerPosition);

    // Ad Unit ID Properties - Android
    public string AndroidBannerId
    {
        get => androidBannerId;
        set => androidBannerId = value;
    }
    public string AndroidInterstitialId
    {
        get => androidInterstitialId;
        set => androidInterstitialId = value;
    }
    public string AndroidRewardedId
    {
        get => androidRewardedId;
        set => androidRewardedId = value;
    }
    public string AndroidRewardedInterstitialId
    {
        get => androidRewardedInterstitialId;
        set => androidRewardedInterstitialId = value;
    }
    public string AndroidAppOpenId
    {
        get => androidAppOpenId;
        set => androidAppOpenId = value;
    }

    // Ad Unit ID Properties - iOS
    public string IosBannerId
    {
        get => iosBannerId;
        set => iosBannerId = value;
    }
    public string IosInterstitialId
    {
        get => iosInterstitialId;
        set => iosInterstitialId = value;
    }
    public string IosRewardedId
    {
        get => iosRewardedId;
        set => iosRewardedId = value;
    }
    public string IosRewardedInterstitialId
    {
        get => iosRewardedInterstitialId;
        set => iosRewardedInterstitialId = value;
    }
    public string IosAppOpenId
    {
        get => iosAppOpenId;
        set => iosAppOpenId = value;
    }

    // Current Platform Ad IDs (Read-only)
    public string CurrentBannerId => BANNER_ID;
    public string CurrentInterstitialId => INTERSTITIAL_ID;
    public string CurrentRewardedId => REWARDED_ID;
    public string CurrentRewardedInterstitialId => REWARDED_INTERSTITIAL_ID;
    public string CurrentAppOpenId => APP_OPEN_ID;

    // Events for Remove Ads changes
    public static Action<bool> OnRemoveAdsChanged;
    public static Action<bool> OnRemoveAdsLoadedFromStorage;

    private BannerPosition ConvertFromAdPosition(AdPosition position)
    {
        switch (position)
        {
            case AdPosition.Top: return BannerPosition.Top;
            case AdPosition.Bottom: return BannerPosition.Bottom;
            case AdPosition.TopLeft: return BannerPosition.TopLeft;
            case AdPosition.TopRight: return BannerPosition.TopRight;
            case AdPosition.BottomLeft: return BannerPosition.BottomLeft;
            case AdPosition.BottomRight: return BannerPosition.BottomRight;
            case AdPosition.Center: return BannerPosition.Center;
            default: return BannerPosition.Bottom;
        }
    }

    private void OnRemoveAdsChangedInternal(bool removeAdsEnabled)
    {
        Debug.Log($"Remove Ads {(removeAdsEnabled ? "enabled" : "disabled")}");

        if (removeAdsEnabled)
        {
            // Destroy and stop loading non-rewarded ads
            DestroyBanner();

            if (interstitialAd != null)
            {
                interstitialAd.Destroy();
                interstitialAd = null;
            }

            if (appOpenAd != null)
            {
                appOpenAd.Destroy();
                appOpenAd = null;
            }
        }
        else
        {
            // Re-enable and load non-rewarded ads if initialized
            if (isInitialized)
            {
                LoadBanner();
                LoadInterstitialAd();
                LoadAppOpenAd();
            }
        }
    }

    #region Persistence Methods
    private void LoadRemoveAdsStatus()
    {
        bool savedValue = false;

        if (useEncryptedStorage)
        {
            savedValue = LoadEncryptedBool(removeAdsKey, false);
        }
        else
        {
            savedValue = PlayerPrefs.GetInt(removeAdsKey, 0) == 1;
        }

        if (removeAds != savedValue)
        {
            removeAds = savedValue;
            OnRemoveAdsChangedInternal(savedValue);
            OnRemoveAdsLoadedFromStorage?.Invoke(savedValue);
            Debug.Log($"Loaded Remove Ads status from storage: {savedValue}");
        }

        // If cloud sync is enabled, check for cloud data
        if (enableCloudSync)
        {
            RequestCloudData();
        }
    }

    private void SaveRemoveAdsStatus(bool value)
    {
        if (useEncryptedStorage)
        {
            SaveEncryptedBool(removeAdsKey, value);
        }
        else
        {
            PlayerPrefs.SetInt(removeAdsKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        Debug.Log($"Saved Remove Ads status to storage: {value}");

        // If cloud sync is enabled, save to cloud
        if (enableCloudSync)
        {
            SaveToCloud(value);
        }
    }

    private void SaveEncryptedBool(string key, bool value)
    {
        // Use new AES-256 encryption via SecureStorage
        bool success = SecureStorage.SaveEncryptedBool(key, value, encryptionKey);
        if (!success)
        {
            Debug.LogWarning($"[AdsManager] Failed to save encrypted value for key: {key}");
        }
    }

    private bool LoadEncryptedBool(string key, bool defaultValue)
    {
        // Load with AES-256 encryption with HMAC integrity verification
        // SECURITY NOTE: Automatic migration has been removed for security
        // If migrating from old XOR encryption, call MigrateLegacyData() once
        return SecureStorage.LoadEncryptedBool(key, defaultValue, encryptionKey);
    }

    // Cloud Save Integration Points
    private void RequestCloudData()
    {
        // Implement your cloud save integration here
        // Examples: Unity Cloud Save, Firebase, PlayFab, etc.
        Debug.Log("Requesting Remove Ads status from cloud...");

        // Example Unity Cloud Save integration:
        /*
        Unity.Services.CloudSave.CloudSaveService.Instance.Data.LoadAsync(new List<string> { removeAdsKey })
            .ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    var results = task.Result;
                    if (results.TryGetValue(removeAdsKey, out var cloudValue))
                    {
                        bool cloudRemoveAds = bool.Parse(cloudValue.Value.GetAs<string>());
                        UnityEngine.MainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            SyncWithCloudData(cloudRemoveAds);
                        });
                    }
                }
            });
        */
    }

    private void SaveToCloud(bool value)
    {
        // Implement your cloud save integration here
        Debug.Log($"Saving Remove Ads status to cloud: {value}");

        // Example Unity Cloud Save integration:
        /*
        var data = new Dictionary<string, object> { { removeAdsKey, value } };
        Unity.Services.CloudSave.CloudSaveService.Instance.Data.ForceSaveAsync(data);
        */

        // Example Firebase integration:
        /*
        Firebase.Database.FirebaseDatabase.DefaultInstance
            .GetReference("users").Child(Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId)
            .Child("removeAds").SetValueAsync(value);
        */
    }

    private void SyncWithCloudData(bool cloudValue)
    {
        // Resolve conflicts between local and cloud data
        if (removeAds != cloudValue)
        {
            Debug.Log($"Cloud sync: Local={removeAds}, Cloud={cloudValue}");

            // You can implement your conflict resolution strategy here
            // For now, cloud data takes precedence
            RemoveAds = cloudValue;
            Debug.Log($"Synced Remove Ads status with cloud: {cloudValue}");
        }
    }

    // Public methods for external integration
    public void ForceLoadFromStorage()
    {
        LoadRemoveAdsStatus();
    }

    public void ForceSaveToStorage()
    {
        SaveRemoveAdsStatus(removeAds);
    }

    public void ClearRemoveAdsData()
    {
        if (useEncryptedStorage)
        {
            SecureStorage.DeleteEncryptedData(removeAdsKey);
        }
        else
        {
            PlayerPrefs.DeleteKey(removeAdsKey);
        }
        PlayerPrefs.Save();

        RemoveAds = false;
        Debug.Log("Remove Ads data cleared from storage");
    }

    public bool HasRemoveAdsDataInStorage()
    {
        if (useEncryptedStorage)
        {
            return SecureStorage.HasEncryptedData(removeAdsKey);
        }
        else
        {
            return PlayerPrefs.HasKey(removeAdsKey);
        }
    }

    /// <summary>
    /// Manually migrates legacy XOR-encrypted data to new secure AES format.
    /// SECURITY: Call this ONCE when upgrading from version with old XOR encryption.
    /// Then REMOVE this call from your code to prevent exploitation.
    /// </summary>
    /// <returns>True if migration completed or not needed, false if failed</returns>
    public bool MigrateLegacyEncryption()
    {
        if (!useEncryptedStorage)
        {
            Debug.Log("[AdsManager] Encryption not enabled - migration not needed");
            return true;
        }

        // Check if migration is needed
        if (!SecureStorage.HasLegacyData(removeAdsKey))
        {
            Debug.Log("[AdsManager] No legacy data found - migration not needed");
            return true;
        }

        Debug.LogWarning("[AdsManager] ========================================");
        Debug.LogWarning("[AdsManager] MIGRATING LEGACY XOR DATA TO AES-256");
        Debug.LogWarning("[AdsManager] This should only run ONCE");
        Debug.LogWarning("[AdsManager] ========================================");

        // Perform migration using the encryption key (which was the XOR key)
        bool success = SecureStorage.MigrateLegacyData(removeAdsKey, encryptionKey, encryptionKey);

        if (success)
        {
            Debug.LogWarning("[AdsManager] ========================================");
            Debug.LogWarning("[AdsManager] MIGRATION COMPLETED SUCCESSFULLY");
            Debug.LogWarning("[AdsManager] IMPORTANT: Remove MigrateLegacyEncryption() call from your code NOW");
            Debug.LogWarning("[AdsManager] ========================================");

            // Reload the migrated data
            LoadRemoveAdsStatus();
        }
        else
        {
            Debug.LogError("[AdsManager] Migration failed - please check logs");
        }

        return success;
    }

    /// <summary>
    /// Checks if legacy data exists that needs migration.
    /// Use this to determine if you should call MigrateLegacyEncryption().
    /// </summary>
    public bool NeedsLegacyMigration()
    {
        return useEncryptedStorage && SecureStorage.HasLegacyData(removeAdsKey);
    }
    #endregion

    #region Ad Unit ID Management
    public void SetAndroidAdIds(string bannerId, string interstitialId, string rewardedId, string rewardedInterstitialId, string appOpenId)
    {
        androidBannerId = bannerId;
        androidInterstitialId = interstitialId;
        androidRewardedId = rewardedId;
        androidRewardedInterstitialId = rewardedInterstitialId;
        androidAppOpenId = appOpenId;

        Debug.Log("Android Ad Unit IDs updated");
        RefreshAdsWithNewIds();
    }

    public void SetIosAdIds(string bannerId, string interstitialId, string rewardedId, string rewardedInterstitialId, string appOpenId)
    {
        iosBannerId = bannerId;
        iosInterstitialId = interstitialId;
        iosRewardedId = rewardedId;
        iosRewardedInterstitialId = rewardedInterstitialId;
        iosAppOpenId = appOpenId;

        Debug.Log("iOS Ad Unit IDs updated");
        RefreshAdsWithNewIds();
    }

    public void SetAllAdIds(
        string androidBanner, string androidInterstitial, string androidRewarded, string androidRewardedInterstitial, string androidAppOpen,
        string iosBanner, string iosInterstitial, string iosRewarded, string iosRewardedInterstitial, string iosAppOpen)
    {
        // Set Android IDs
        androidBannerId = androidBanner;
        androidInterstitialId = androidInterstitial;
        androidRewardedId = androidRewarded;
        androidRewardedInterstitialId = androidRewardedInterstitial;
        androidAppOpenId = androidAppOpen;

        // Set iOS IDs
        iosBannerId = iosBanner;
        iosInterstitialId = iosInterstitial;
        iosRewardedId = iosRewarded;
        iosRewardedInterstitialId = iosRewardedInterstitial;
        iosAppOpenId = iosAppOpen;

        Debug.Log("All Ad Unit IDs updated for both platforms");
        RefreshAdsWithNewIds();
    }

    private void RefreshAdsWithNewIds()
    {
        if (!isInitialized)
        {
            Debug.Log("AdsManager not initialized yet. New Ad IDs will be used when initialized.");
            return;
        }

        Debug.Log("Refreshing ads with new Ad Unit IDs...");

        // Destroy existing ads
        DestroyBanner();

        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        if (rewardedInterstitialAd != null)
        {
            rewardedInterstitialAd.Destroy();
            rewardedInterstitialAd = null;
        }

        if (appOpenAd != null)
        {
            appOpenAd.Destroy();
            appOpenAd = null;
        }

        // Reload all ads with new IDs
        LoadAllAds();
    }

    // Utility methods for getting current platform specific IDs
    public void LogCurrentAdIds()
    {
        Debug.Log($"Current Platform Ad IDs:");
        Debug.Log($"Banner: {CurrentBannerId}");
        Debug.Log($"Interstitial: {CurrentInterstitialId}");
        Debug.Log($"Rewarded: {CurrentRewardedId}");
        Debug.Log($"Rewarded Interstitial: {CurrentRewardedInterstitialId}");
        Debug.Log($"App Open: {CurrentAppOpenId}");
    }

    public bool AreAdIdsValid()
    {
        return !string.IsNullOrEmpty(CurrentBannerId) &&
               !string.IsNullOrEmpty(CurrentInterstitialId) &&
               !string.IsNullOrEmpty(CurrentRewardedId) &&
               !string.IsNullOrEmpty(CurrentRewardedInterstitialId) &&
               !string.IsNullOrEmpty(CurrentAppOpenId);
    }

    public bool AreTestAdIds()
    {
        // Check if current IDs are Google's test IDs
        return CurrentBannerId.Contains("ca-app-pub-3940256099942544") ||
               CurrentInterstitialId.Contains("ca-app-pub-3940256099942544") ||
               CurrentRewardedId.Contains("ca-app-pub-3940256099942544") ||
               CurrentRewardedInterstitialId.Contains("ca-app-pub-3940256099942544") ||
               CurrentAppOpenId.Contains("ca-app-pub-3940256099942544");
    }
    #endregion

    private void Awake()
    {
        Debug.Log("[AdsManager] Awake() called");

        if (instance == null)
        {
            Debug.Log("[AdsManager] Creating singleton instance");
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadRemoveAdsStatus(); // Load saved Remove Ads status first
            Debug.Log($"[AdsManager] Remove Ads Status: {removeAds}");
            InitializeAds();
        }
        else
        {
            Debug.Log("[AdsManager] Duplicate instance detected - destroying");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Debug.Log("[AdsManager] Start() called");
        // Initialize app state notifier for app open ads
        AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
        Debug.Log("[AdsManager] App state notifier registered");
    }

    private void InitializeAds()
    {
        Debug.Log("[AdsManager] InitializeAds() started");

        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        Debug.Log("[AdsManager] Set RaiseAdEventsOnUnityMainThread = true");

        // Add this for testing - reset consent to simulate fresh install
        if (enableTestAds)
        {
            Debug.Log("[AdsManager] Resetting consent for testing");
            ConsentInformation.Reset();
        }

        // Log current ad IDs for debugging
        Debug.Log($"[AdsManager] Using Banner ID: {CurrentBannerId}");
        Debug.Log($"[AdsManager] Using Interstitial ID: {CurrentInterstitialId}");
        Debug.Log($"[AdsManager] Using Rewarded ID: {CurrentRewardedId}");
        Debug.Log($"[AdsManager] Using Rewarded Interstitial ID: {CurrentRewardedInterstitialId}");
        Debug.Log($"[AdsManager] Using App Open ID: {CurrentAppOpenId}");
        Debug.Log($"[AdsManager] Test Ads Enabled: {enableTestAds}");
        Debug.Log($"[AdsManager] Are Test Ad IDs: {AreTestAdIds()}");

        // Then continue with consent
        RequestConsentInfo();
    }

    private void RequestConsentInfo()
    {
        Debug.Log("[AdsManager] RequestConsentInfo() started");
        
        // Production-first approach: Google recommends ALWAYS calling Update()
        if (!alwaysRequestConsentUpdate)
        {
            Debug.LogWarning("[AdsManager] Consent update disabled - this is NOT recommended by Google");
            Debug.LogWarning("[AdsManager] Skipping consent and initializing directly");
            InitializeAdMob();
            return;
        }
        
        ConsentRequestParameters request = CreateConsentRequestParameters();
        
        Debug.Log("[AdsManager] Calling ConsentInformation.Update() - Google's required step");
        ConsentInformation.Update(request, OnConsentInfoUpdated);
        
        // Google's recommendation: Check CanRequestAds immediately after Update()
        StartCoroutine(CheckConsentAfterUpdate());
    }

    /// <summary>
    /// Creates consent request parameters with production defaults and optional testing overrides
    /// </summary>
    private ConsentRequestParameters CreateConsentRequestParameters()
    {
        // Production default: No debug settings (uses real device geography)
        ConsentRequestParameters request = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = false
        };
        
        // Apply testing overrides only if debugging is enabled
        if (enableConsentDebugging)
        {
            Debug.Log("[AdsManager] CONSENT DEBUGGING ENABLED - Using test configuration");
            Debug.LogWarning("[AdsManager] This should be DISABLED in production builds");
            
            var debugSettings = new ConsentDebugSettings();
            
            // Geography override for testing consent forms
            if (forceEEAGeographyForTesting)
            {
                debugSettings.DebugGeography = DebugGeography.EEA;
                Debug.Log("[AdsManager] Forcing EEA geography for consent testing");
            }
            else
            {
                debugSettings.DebugGeography = DebugGeography.Other;
                Debug.Log("[AdsManager] Using Other geography for testing");
            }
            
            // Let AdMob auto-detect test devices (no manual device ID needed)
            debugSettings.TestDeviceHashedIds = new List<string>();
            Debug.Log("[AdsManager] Test device will be auto-detected by AdMob SDK");
            
            request.ConsentDebugSettings = debugSettings;
        }
        else
        {
            Debug.Log("[AdsManager] Using production consent configuration");
            Debug.Log("[AdsManager] Real device geography and behavior will be used");
        }
        
        return request;
    }

    /// <summary>
    /// Google's recommended approach: Check consent status immediately after Update()
    /// This handles cases where consent was obtained in previous sessions
    /// </summary>
    private IEnumerator CheckConsentAfterUpdate()
    {
        // Wait one frame for Update() to complete its internal processing
        yield return null;
        
        Debug.Log("[AdsManager] === CONSENT STATUS CHECK ===");
        LogDetailedConsentStatus();
        
        // STEP 1: Google's primary recommendation - check CanRequestAds first
        if (ConsentInformation.CanRequestAds())
        {
            if (!isInitialized)
            {
                Debug.Log("[AdsManager] CanRequestAds is TRUE - Initializing AdMob immediately");
                Debug.Log("[AdsManager] This means user has given consent or consent is not required");
                InitializeAdMob();
            }
            else
            {
                Debug.Log("[AdsManager] CanRequestAds is TRUE but AdMob already initialized");
            }
            yield break; // Exit early - we can proceed
        }
        
        // STEP 2: CanRequestAds is false - analyze why and handle appropriately
        Debug.Log("[AdsManager] CanRequestAds is FALSE - Analyzing consent status...");
        
        ConsentStatus status = ConsentInformation.ConsentStatus;
        bool formAvailable = ConsentInformation.IsConsentFormAvailable();
        
        switch (status)
        {
            case ConsentStatus.Required:
                HandleConsentRequired(formAvailable);
                break;
                
            case ConsentStatus.NotRequired:
                HandleConsentNotRequired();
                break;
                
            case ConsentStatus.Obtained:
                HandleConsentObtained();
                break;
                
            case ConsentStatus.Unknown:
            default:
                HandleConsentUnknown();
                break;
        }
    }

    /// <summary>
    /// Logs detailed consent status for debugging and production monitoring
    /// </summary>
    private void LogDetailedConsentStatus()
    {
        Debug.Log($"[AdsManager] CanRequestAds: {ConsentInformation.CanRequestAds()}");
        Debug.Log($"[AdsManager] ConsentStatus: {ConsentInformation.ConsentStatus}");
        Debug.Log($"[AdsManager] IsConsentFormAvailable: {ConsentInformation.IsConsentFormAvailable()}");
        Debug.Log($"[AdsManager] PrivacyOptionsRequirementStatus: {ConsentInformation.PrivacyOptionsRequirementStatus}");
        Debug.Log($"[AdsManager] AdMob Initialized: {isInitialized}");
        Debug.Log($"[AdsManager] Debugging Enabled: {enableConsentDebugging}");
        Debug.Log($"[AdsManager] Force EEA Testing: {forceEEAGeographyForTesting}");
    }

    /// <summary>
    /// Handles ConsentStatus.Required scenario (typically EEA users)
    /// </summary>
    private void HandleConsentRequired(bool formAvailable)
    {
        Debug.Log("[AdsManager] Consent Status: REQUIRED");
        
        if (formAvailable)
        {
            Debug.Log("[AdsManager] Consent form is available");
            Debug.Log("[AdsManager] Waiting for OnConsentInfoUpdated callback to show consent form");
            Debug.Log("[AdsManager] User will be prompted with consent options");
            // The callback OnConsentInfoUpdated() will handle showing the form
        }
        else
        {
            Debug.LogWarning("[AdsManager] Consent required but NO FORM AVAILABLE");
            Debug.LogWarning("[AdsManager] This indicates missing privacy message configuration in AdMob Console");
            Debug.LogWarning("[AdsManager] Go to AdMob Console > Privacy & messaging > Create message");
            
            HandleMissingConsentForm();
        }
    }

    /// <summary>
    /// Handles ConsentStatus.NotRequired scenario (typically non-EEA users)
    /// </summary>
    private void HandleConsentNotRequired()
    {
        Debug.Log("[AdsManager] Consent Status: NOT REQUIRED");
        Debug.Log("[AdsManager] User is not in a region requiring consent (non-EEA)");
        
        // Set mediation consent for non-EEA users too
        SetMediationConsent();
        
        if (!isInitialized)
        {
            Debug.Log("[AdsManager] Proceeding with AdMob initialization");
            InitializeAdMob();
        }
    }

    /// <summary>
    /// Handles ConsentStatus.Obtained scenario (user previously gave consent)
    /// </summary>
    private void HandleConsentObtained()
    {
        Debug.Log("[AdsManager] Consent Status: OBTAINED");
        Debug.Log("[AdsManager] User previously provided consent");
        
        // Set mediation consent for users with existing consent
        SetMediationConsent();
        
        if (!isInitialized)
        {
            Debug.Log("[AdsManager] Proceeding with AdMob initialization");
            InitializeAdMob();
        }
    }

    /// <summary>
    /// Handles ConsentStatus.Unknown scenario (edge cases, network issues, etc.)
    /// </summary>
    private void HandleConsentUnknown()
    {
        Debug.Log("[AdsManager] Consent Status: UNKNOWN");
        Debug.Log("[AdsManager] This can happen due to network issues or first-time initialization");
        
        if (enableConsentDebugging)
        {
            Debug.Log("[AdsManager] DEBUGGING MODE: Bypassing unknown status for testing");
            Debug.Log("[AdsManager] In production, this would wait for network recovery or callback");
            
            // Set mediation consent even with unknown status in debugging
            SetMediationConsent();
            
            if (!isInitialized)
            {
                InitializeAdMob();
            }
        }
        else
        {
            Debug.Log("[AdsManager] PRODUCTION MODE: Waiting for consent resolution");
            Debug.Log("[AdsManager] Options: 1) Callback will resolve, 2) Network will recover, 3) Timeout will trigger");
            
            // Start timeout fallback for production reliability
            StartCoroutine(ProductionConsentTimeout());
        }
    }

    /// <summary>
    /// Handles missing consent form configuration (production issue detection)
    /// </summary>
    private void HandleMissingConsentForm()
    {
        if (enableConsentDebugging)
        {
            Debug.Log("[AdsManager] DEBUGGING MODE: Bypassing missing form for testing");
            Debug.Log("[AdsManager] In production, you MUST configure privacy messages in AdMob Console");
            
            // Set mediation consent even when debugging missing forms
            SetMediationConsent();
            
            if (!isInitialized)
            {
                InitializeAdMob();
            }
        }
        else
        {
            Debug.LogError("[AdsManager] PRODUCTION ERROR: Missing consent form configuration");
            Debug.LogError("[AdsManager] REQUIRED ACTION: Configure privacy message in AdMob Console");
            Debug.LogError("[AdsManager] Cannot proceed with ad initialization in compliant manner");
            
            // In production, you might want to:
            // 1. Show user a message about ads being unavailable
            // 2. Proceed without ads
            // 3. Initialize with limited functionality
            // For now, we'll wait for manual intervention or timeout
        }
    }

    /// <summary>
    /// Production timeout fallback to prevent indefinite waiting
    /// </summary>
    private IEnumerator ProductionConsentTimeout()
    {
        float timeoutSeconds = 15f;
        Debug.Log($"[AdsManager] Starting production consent timeout ({timeoutSeconds}s)");
        
        yield return new WaitForSeconds(timeoutSeconds);
        
        if (!isInitialized)
        {
            Debug.LogWarning("[AdsManager] Consent timeout reached in production");
            Debug.LogWarning("[AdsManager] This may indicate network issues or SDK problems");
            
            // Decision point: Initialize anyway or remain in non-ads state?
            // This depends on your app's requirements
            
            if (ConsentInformation.CanRequestAds())
            {
                Debug.Log("[AdsManager] CanRequestAds became true during timeout - initializing");
                InitializeAdMob();
            }
            else
            {
                Debug.LogWarning("[AdsManager] Still cannot request ads - continuing without ads");
                // Optionally notify other systems that ads are unavailable
            }
        }
    }

    /// <summary>
    /// Callback executed when ConsentInformation.Update() completes
    /// This handles consent form presentation and user interaction
    /// </summary>
    private void OnConsentInfoUpdated(FormError error)
    {
        // Prevent double initialization race conditions
        if (isInitialized)
        {
            Debug.Log("[AdsManager] OnConsentInfoUpdated called but AdMob already initialized");
            return;
        }
        
        Debug.Log("[AdsManager] === CONSENT UPDATE CALLBACK ===");
        Debug.Log("[AdsManager] OnConsentInfoUpdated() callback received");
        
        // Handle consent update errors
        if (error != null)
        {
            Debug.LogError($"[AdsManager] Consent update error: {error}");
            Debug.LogError($"[AdsManager] Error message: {error.Message}");
            
            HandleConsentUpdateError(error);
            return;
        }

        Debug.Log("[AdsManager] Consent update completed successfully");
        LogDetailedConsentStatus();
        
        // Google's next step: Load and show consent form if required
        Debug.Log("[AdsManager] Calling LoadAndShowConsentFormIfRequired()");
        
        ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
        {
            HandleConsentFormResult(formError);
        });
    }

    /// <summary>
    /// Handles errors from ConsentInformation.Update()
    /// </summary>
    private void HandleConsentUpdateError(FormError error)
    {
        // Check if we can still request ads despite the error
        if (ConsentInformation.CanRequestAds())
        {
            Debug.Log("[AdsManager] Error occurred but CanRequestAds is still true");
            Debug.Log("[AdsManager] This can happen with network issues but cached consent is valid");
            InitializeAdMob();
            return;
        }
        
        if (enableConsentDebugging)
        {
            Debug.Log("[AdsManager] DEBUGGING MODE: Bypassing consent error for testing");
            InitializeAdMob();
        }
        else
        {
            Debug.LogError("[AdsManager] PRODUCTION ERROR: Cannot recover from consent update error");
            Debug.LogError("[AdsManager] Consider implementing retry logic or fallback behavior");
            
            // In production, you might want to:
            // 1. Retry the consent update after delay
            // 2. Continue without ads
            // 3. Show user an error message
            StartCoroutine(RetryConsentUpdate());
        }
    }

    /// <summary>
    /// Handles the result of LoadAndShowConsentFormIfRequired()
    /// </summary>
    private void HandleConsentFormResult(FormError formError)
    {
        Debug.Log("[AdsManager] === CONSENT FORM RESULT ===");
        
        if (formError != null)
        {
            Debug.LogError($"[AdsManager] Consent form error: {formError}");
            Debug.LogError($"[AdsManager] Error message: {formError.Message}");
            
            if (enableConsentDebugging)
            {
                Debug.Log("[AdsManager] DEBUGGING MODE: Bypassing form error for testing");
                // Set mediation consent even in debugging mode
                SetMediationConsent();
                InitializeAdMob();
            }
            else
            {
                Debug.LogError("[AdsManager] PRODUCTION ERROR: Consent form failed");
                // Handle form error in production
            }
            return;
        }

        Debug.Log("[AdsManager] Consent form process completed successfully");
        LogDetailedConsentStatus();
        
        // IMPORTANT: Update mediation consent after form completion
        SetMediationConsent();
        
        // Final check: Can we now request ads?
        if (ConsentInformation.CanRequestAds())
        {
            if (!isInitialized)
            {
                Debug.Log("[AdsManager] Final consent check PASSED - Initializing AdMob");
                InitializeAdMob();
            }
        }
        else
        {
            Debug.LogWarning("[AdsManager] Consent form completed but still cannot request ads");
            Debug.LogWarning("[AdsManager] User may have denied consent or form had issues");
            
            if (enableConsentDebugging)
            {
                Debug.Log("[AdsManager] DEBUGGING MODE: Initializing anyway for testing");
                InitializeAdMob();
            }
            else
            {
                Debug.Log("[AdsManager] PRODUCTION: Respecting user's consent decision - no ads");
                // Continue app without ads, respect user choice
            }
        }
    }

    /// <summary>
    /// Production retry mechanism for consent update failures
    /// </summary>
    private IEnumerator RetryConsentUpdate()
    {
        int retryDelay = 5; // seconds
        Debug.Log($"[AdsManager] Retrying consent update in {retryDelay} seconds...");
        
        yield return new WaitForSeconds(retryDelay);
        
        if (!isInitialized)
        {
            Debug.Log("[AdsManager] Attempting consent update retry");
            RequestConsentInfo();
        }
    }

    private void SetMediationConsent()
    {
        Debug.Log("[AdsManager] SetMediationConsent() started");

        bool canRequestAds = ConsentInformation.CanRequestAds();
        bool hasConsent = ConsentInformation.ConsentStatus == ConsentStatus.Obtained;
        bool isEEA = ConsentInformation.ConsentStatus == ConsentStatus.Required;
        
        Debug.Log($"[AdsManager] Can request ads: {canRequestAds}");
        Debug.Log($"[AdsManager] Has explicit consent: {hasConsent}");
        Debug.Log($"[AdsManager] Is EEA user: {isEEA}");

        // Unity Ads mediation consent - Enhanced for better GDPR compliance
        UnityAds.SetConsentMetaData("gdpr.consent", canRequestAds);
        UnityAds.SetConsentMetaData("privacy.consent", canRequestAds);
        
        // Additional Unity Ads GDPR compliance for EEA users
        if (isEEA)
        {
            UnityAds.SetConsentMetaData("gdpr.consent", hasConsent);
            Debug.Log($"[AdsManager] Unity Ads EEA-specific consent set: {hasConsent}");
        }
        
        Debug.Log($"[AdsManager] Unity Ads consent metadata configured successfully");

        // Add other mediation networks as needed - Ready for future expansion
        // When you add more mediation networks, configure their consent here:
        
        /*
        // Example: Facebook Audience Network (if you add it later)
        if (canRequestAds)
        {
            // Facebook.Unity.FB.Mobile.SetAdvertiserIDCollectionEnabled(true);
            // Facebook.Unity.FB.Mobile.SetDataProcessingOptions(new string[] {});
            Debug.Log("[AdsManager] Facebook mediation consent configured");
        }
        
        // Example: AppLovin MAX (if you add it later)
        if (canRequestAds)
        {
            // MaxSdk.SetHasUserConsent(hasConsent);
            // MaxSdk.SetIsAgeRestrictedUser(false);
            Debug.Log("[AdsManager] AppLovin mediation consent configured");
        }
        
        // Example: ironSource (if you add it later)
        if (canRequestAds)
        {
            // IronSource.Agent.setConsent(hasConsent);
            Debug.Log("[AdsManager] ironSource mediation consent configured");
        }
        
        // Example: Chartboost (if you add it later)
        if (canRequestAds)
        {
            // Chartboost.setPIDataUseConsent(hasConsent ? CBPIDataUseConsent.YesBehavioral : CBPIDataUseConsent.NoBehavioral);
            Debug.Log("[AdsManager] Chartboost mediation consent configured");
        }
        */

        Debug.Log("[AdsManager] Mediation consent configuration completed for all networks");
    }

    private void InitializeAdMob()
    {
        Debug.Log("[AdsManager] InitializeAdMob() started");

        // Set mediation consent based on current consent status
        SetMediationConsent();

        // Configure request configuration for test devices if needed
        var requestConfiguration = new RequestConfiguration
        {
            TestDeviceIds = enableTestAds ? new System.Collections.Generic.List<string> { "YOUR_TEST_DEVICE_ID" } : null
        };
        Debug.Log($"[AdsManager] Request configuration created with test devices: {enableTestAds}");

        MobileAds.SetRequestConfiguration(requestConfiguration);
        Debug.Log("[AdsManager] Request configuration set");

        Debug.Log("[AdsManager] Calling MobileAds.Initialize()");
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            Debug.Log("[AdsManager] MobileAds.Initialize() callback executed");

            isInitialized = true;
            isColdStart = false; // Initialization complete, no longer cold start
            Debug.Log("[AdsManager] AdMob initialized successfully");

            // Log mediation adapter statuses
            Debug.Log("[AdsManager] Mediation adapter statuses:");
            foreach (var adapterStatus in initStatus.getAdapterStatusMap())
            {
                Debug.Log($"[AdsManager] Adapter: {adapterStatus.Key}, Status: {adapterStatus.Value.InitializationState}, Description: {adapterStatus.Value.Description}");
            }

            Debug.Log("[AdsManager] Starting to load all ads");
            LoadAllAds();
        });
    }

    private void LoadAllAds()
    {
        Debug.Log("[AdsManager] LoadAllAds() started");
        Debug.Log($"[AdsManager] Remove ads status: {removeAds}");

        // Always load rewarded ads
        Debug.Log("[AdsManager] Loading rewarded ads...");
        LoadRewardedAd();
        LoadRewardedInterstitialAd();

        // Only load non-rewarded ads if removeAds is false
        if (!removeAds)
        {
            Debug.Log("[AdsManager] Loading non-rewarded ads...");
            LoadInterstitialAd();
            LoadAppOpenAd();
            LoadBanner();
            // Don't automatically show banner - let VerifyAdmob control initial visibility
        }
        else
        {
            Debug.Log("[AdsManager] Skipping non-rewarded ads due to Remove Ads setting");
        }

        Debug.Log("[AdsManager] LoadAllAds() completed");
    }

    // App State Management for App Open Ads
    private void OnAppStateChanged(AppState appState)
    {
        Debug.Log($"App state changed to: {appState}");

        if (appState == AppState.Foreground && autoShowAppOpenAds)
        {
            // Don't show on cold start or if an ad is already showing
            if (!isColdStart && !isShowingAd && !isAppOpenAdShowing)
            {
                // Check cooldown period
                if (DateTime.Now.Subtract(lastAppOpenShownTime).TotalSeconds >= appOpenCooldownTime)
                {
                    ShowAppOpenAdIfAvailable();
                }
            }
        }
    }

    private void ShowAppOpenAdIfAvailable()
    {
        if (IsAppOpenAdAvailable())
        {
            ShowAppOpenAd(() =>
            {
                hasShownFirstAppOpenAd = true;
                lastAppOpenShownTime = DateTime.Now;
            });
        }
    }

    #region Banner Ads
    public void LoadBanner()
    {
        Debug.Log("[AdsManager] LoadBanner() started");

        if (!isInitialized)
        {
            Debug.LogWarning("[AdsManager] AdMob not initialized yet - cannot load banner");
            return;
        }

        if (removeAds)
        {
            Debug.Log("[AdsManager] Banner ads are disabled due to Remove Ads setting");
            return;
        }

        Debug.Log($"[AdsManager] Loading banner with ID: {BANNER_ID}");
        Debug.Log($"[AdsManager] Banner position: {currentBannerPosition}");
        Debug.Log($"[AdsManager] Using adaptive banners: {useAdaptiveBanners}");
        Debug.Log($"[AdsManager] Collapsible banners enabled: {enableCollapsibleBanners}");

        // Don't automatically set visibility - let external code control it
        DestroyBanner();

        AdSize adSize = GetAdSize();
        Debug.Log($"[AdsManager] Banner ad size: {adSize.Width}x{adSize.Height}");
        bannerView = new BannerView(BANNER_ID, adSize, currentBannerPosition);
        Debug.Log("[AdsManager] BannerView created");

        // Register banner events
        RegisterBannerEvents();
        Debug.Log("[AdsManager] Banner events registered");

        // Load the banner
        var adRequest = CreateAdRequest();

        // Add collapsible banner custom targeting if enabled
        if (enableCollapsibleBanners)
        {
            adRequest.Extras.Add("collapsible", "bottom"); // or "top"
            Debug.Log("[AdsManager] Added collapsible banner targeting");
        }

        Debug.Log("[AdsManager] Calling bannerView.LoadAd()");
        bannerView.LoadAd(adRequest);

        Debug.Log($"[AdsManager] Loading {(useAdaptiveBanners ? "adaptive" : "standard")} banner ad...");
    }

    private AdSize GetAdSize()
    {
        if (useAdaptiveBanners)
        {
            return GetAdaptiveAdSize();
        }

        return GetStandardAdSize();
    }

    private AdSize GetAdaptiveAdSize()
    {
        // For adaptive banners, use the standard Banner size as fallback
        // In newer versions of Google Mobile Ads, adaptive sizing is handled automatically
        Debug.Log("Using adaptive banner (automatic sizing)");
        return AdSize.Banner;
    }

    private AdSize GetStandardAdSize()
    {
        switch (preferredBannerSize)
        {
            case BannerSize.Banner:
                return AdSize.Banner;
            case BannerSize.LargeBanner:
                return new AdSize(320, 100); // Large Banner
            case BannerSize.MediumRectangle:
                return AdSize.MediumRectangle;
            case BannerSize.FullBanner:
                return new AdSize(468, 60); // Full Banner
            case BannerSize.Leaderboard:
                return AdSize.Leaderboard;
            case BannerSize.SmartBanner:
                // Smart banners are deprecated, use Banner instead
                Debug.LogWarning("Smart banners are deprecated. Using standard banner instead.");
                return AdSize.Banner;
            case BannerSize.Adaptive:
            case BannerSize.AdaptiveInline:
                return AdSize.Banner;
            default:
                return AdSize.Banner;
        }
    }

    private void RegisterBannerEvents()
    {
        if (bannerView == null) return;

        // Banner loaded successfully
        bannerView.OnBannerAdLoaded += () =>
        {
            isBannerLoaded = true;
            Debug.Log("Banner ad loaded successfully");

            // Mark first-time loading as complete
            if (isFirstTimeLoading)
            {
                isFirstTimeLoading = false;
                Debug.Log("First-time ad loading completed");
            }

            // Auto-show banner if it was meant to be visible
            if (isBannerVisible)
            {
                bannerView.Show();
            }
        };

        // Banner failed to load
        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            isBannerLoaded = false;
            Debug.LogError($"Banner ad failed to load: {error.GetMessage()}\nError Code: {error.GetCode()}\nCause: {error.GetCause()}");

            // Retry loading after a delay
            RetryLoadBanner();
        };

        // Banner revenue tracking
        bannerView.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log($"Banner ad paid: {adValue.Value} {adValue.CurrencyCode}");
            // Here you can send revenue data to analytics services
            TrackAdRevenue("banner", adValue);
        };

        // Banner impression recorded
        bannerView.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Banner ad impression recorded");
        };

        // Banner clicked
        bannerView.OnAdClicked += () =>
        {
            Debug.Log("Banner ad was clicked");
        };

        // Banner opened full screen content
        bannerView.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Banner ad opened full screen content");
            // Pause game logic if needed
            OnBannerFullScreenOpened();
        };

        // Banner closed full screen content
        bannerView.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Banner ad closed full screen content");
            // Resume game logic if needed
            OnBannerFullScreenClosed();
        };
    }

    private void RetryLoadBanner()
    {
        // Implement exponential backoff for banner retries
        Invoke(nameof(LoadBanner), 2f);
    }

    private void OnBannerFullScreenOpened()
    {
        // Pause your game or app logic here
        if (Time.timeScale == 1f)
        {
            Time.timeScale = 0f;
        }
    }

    private void OnBannerFullScreenClosed()
    {
        // Resume your game or app logic here
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }

    public void ShowBanner(bool show)
    {
        if (removeAds && show)
        {
            Debug.Log("Cannot show banner - ads are disabled due to Remove Ads setting");
            return;
        }

        isBannerVisible = show;

        if (bannerView != null && isBannerLoaded)
        {
            if (show)
            {
                bannerView.Show();
                Debug.Log("Banner ad displayed");
            }
            else
            {
                bannerView.Hide();
                Debug.Log("Banner ad hidden");
            }
        }
        else if (show)
        {
            // Load banner if it's not loaded yet
            LoadBanner();
        }
    }

    public void SetInitialBannerVisibility(bool show)
    {
        // This method is called by VerifyAdmob to set initial banner visibility
        // after first-time loading is complete
        if (isBannerLoaded)
        {
            ShowBanner(show);
        }
        else
        {
            // If banner is not loaded yet, set the intended visibility
            // and it will be applied when the banner loads
            isBannerVisible = show;
        }
    }

    public void SetBannerPosition(BannerPosition position)
    {
        AdPosition newPosition = ConvertToAdPosition(position);
        if (currentBannerPosition != newPosition)
        {
            currentBannerPosition = newPosition;
            Debug.Log($"Banner position changed to: {position}");

            // Reload banner with new position if it exists
            if (bannerView != null)
            {
                bool wasVisible = isBannerVisible;
                DestroyBanner();
                LoadBanner();

                // Restore visibility state
                if (wasVisible)
                {
                    ShowBanner(true);
                }
            }
        }
    }

    public void SetBannerSize(BannerSize size)
    {
        if (preferredBannerSize != size)
        {
            preferredBannerSize = size;
            Debug.Log($"Banner size changed to: {size}");

            // Reload banner with new size if it exists
            if (bannerView != null)
            {
                bool wasVisible = isBannerVisible;
                DestroyBanner();
                LoadBanner();

                // Restore visibility state
                if (wasVisible)
                {
                    ShowBanner(true);
                }
            }
        }
    }

    public void EnableAdaptiveBanners(bool enable)
    {
        if (useAdaptiveBanners != enable)
        {
            useAdaptiveBanners = enable;
            Debug.Log($"Adaptive banners {(enable ? "enabled" : "disabled")}");

            // Reload banner with new setting
            if (bannerView != null)
            {
                bool wasVisible = isBannerVisible;
                DestroyBanner();
                LoadBanner();

                // Restore visibility state
                if (wasVisible)
                {
                    ShowBanner(true);
                }
            }
        }
    }

    private void DestroyBanner()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
            isBannerLoaded = false;
            isBannerVisible = false;
            Debug.Log("Banner ad destroyed");
        }
    }

    // Utility methods for banner management
    public bool IsBannerLoaded()
    {
        return isBannerLoaded && bannerView != null;
    }

    public bool IsBannerVisible()
    {
        return isBannerVisible && IsBannerLoaded();
    }

    public Vector2 GetBannerSize()
    {
        if (bannerView != null)
        {
            // Return the size based on the current banner type
            AdSize currentSize = GetAdSize();
            return new Vector2(currentSize.Width, currentSize.Height);
        }
        return Vector2.zero;
    }

    // Revenue tracking helper
    private void TrackAdRevenue(string adFormat, AdValue adValue)
    {
        // Implement your analytics tracking here
        // Example: Firebase Analytics, Unity Analytics, etc.
        Debug.Log($"Ad Revenue - Format: {adFormat}, Value: {adValue.Value}, Currency: {adValue.CurrencyCode}");

        // Example implementation for different analytics services:
        /*
        // Firebase Analytics
        Firebase.Analytics.FirebaseAnalytics.LogEvent("ad_impression", new Firebase.Analytics.Parameter[] {
            new Firebase.Analytics.Parameter("ad_platform", "GoogleAdMob"),
            new Firebase.Analytics.Parameter("ad_format", adFormat),
            new Firebase.Analytics.Parameter("ad_unit_name", BANNER_ID),
            new Firebase.Analytics.Parameter("currency", adValue.CurrencyCode),
            new Firebase.Analytics.Parameter("value", adValue.Value)
        });
        */
    }

    private AdPosition ConvertToAdPosition(BannerPosition position)
    {
        switch (position)
        {
            case BannerPosition.Top:
                return AdPosition.Top;
            case BannerPosition.Bottom:
                return AdPosition.Bottom;
            case BannerPosition.TopLeft:
                return AdPosition.TopLeft;
            case BannerPosition.TopRight:
                return AdPosition.TopRight;
            case BannerPosition.BottomLeft:
                return AdPosition.BottomLeft;
            case BannerPosition.BottomRight:
                return AdPosition.BottomRight;
            case BannerPosition.Center:
                return AdPosition.Center;
            default:
                return AdPosition.Bottom;
        }
    }
    #endregion

    #region Interstitial Ads
    private void LoadInterstitialAd()
    {
        if (!isInitialized) return;

        if (removeAds)
        {
            Debug.Log("Interstitial ads are disabled due to Remove Ads setting");
            return;
        }

        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
        }

        var adRequest = CreateAdRequest();
        InterstitialAd.Load(INTERSTITIAL_ID, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogError($"Interstitial ad failed to load: {error}");
                RetryLoadInterstitial();
                return;
            }

            interstitialAd = ad;
            interstitialRetryAttempt = 0;
            RegisterEventHandlers(interstitialAd);
            Debug.Log("Interstitial ad loaded successfully");
        });
    }

    private void RetryLoadInterstitial()
    {
        interstitialRetryAttempt++;
        if (interstitialRetryAttempt < maxRetryCount)
        {
            Invoke(nameof(LoadInterstitialAd), Mathf.Pow(2, interstitialRetryAttempt));
        }
    }

    private void RegisterInterstitialEvents()
    {
        LoadInterstitialAd();
    }

    public void ShowInterstitial(Action onSuccess, Action onFailure)
    {
        if (removeAds)
        {
            Debug.Log("Cannot show interstitial - ads are disabled due to Remove Ads setting");
            onFailure?.Invoke();
            return;
        }

        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            isShowingAd = true;

            // Create delegates that will unsubscribe themselves after execution
            System.Action onClosed = null;
            System.Action<AdError> onFailed = null;

            onClosed = () =>
            {
                // CRITICAL: Unsubscribe first to prevent memory leaks
                interstitialAd.OnAdFullScreenContentClosed -= onClosed;
                interstitialAd.OnAdFullScreenContentFailed -= onFailed;

                isShowingAd = false;
                Debug.Log("[AdsManager] Interstitial ad closed - loading next ad");
                LoadInterstitialAd(); // CRITICAL: Load next ad immediately after successful display
                onSuccess?.Invoke();
            };

            onFailed = (error) =>
            {
                // CRITICAL: Unsubscribe first to prevent memory leaks
                interstitialAd.OnAdFullScreenContentClosed -= onClosed;
                interstitialAd.OnAdFullScreenContentFailed -= onFailed;

                isShowingAd = false;
                Debug.Log("[AdsManager] Interstitial ad failed - loading next ad");
                LoadInterstitialAd(); // CRITICAL: Load next ad even after failure
                onFailure?.Invoke();
            };

            interstitialAd.OnAdFullScreenContentClosed += onClosed;
            interstitialAd.OnAdFullScreenContentFailed += onFailed;

            interstitialAd.Show();
        }
        else
        {
            onFailure?.Invoke();
            LoadInterstitialAd();
        }
    }

    public void ShowInterstitial(Action onSuccess)
    {
        ShowInterstitial(onSuccess, null);
    }

    public void ShowInterstitial()
    {
        ShowInterstitial(null, null);
    }

    private void OnInterstitialClosed()
    {
        isShowingAd = false;
        Debug.Log("Interstitial ad closed");
    }

    private void OnInterstitialFailed(AdError error)
    {
        isShowingAd = false;
        Debug.LogError($"Interstitial ad failed to show: {error}");
    }
    #endregion

    #region Rewarded Ads
    private void LoadRewardedAd()
    {
        Debug.Log("[AdsManager] LoadRewardedAd() started");

        if (!isInitialized)
        {
            Debug.LogWarning("[AdsManager] Cannot load rewarded ad - AdMob not initialized yet");
            return;
        }

        Debug.Log($"[AdsManager] Loading rewarded ad with ID: {REWARDED_ID}");
        var adRequest = CreateAdRequest();
        Debug.Log("[AdsManager] Ad request created, calling RewardedAd.Load()");

        RewardedAd.Load(REWARDED_ID, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            Debug.Log("[AdsManager] RewardedAd.Load() callback executed");

            if (error != null)
            {
                Debug.LogError($"[AdsManager] Rewarded ad failed to load: {error}");
                Debug.LogError($"[AdsManager] Error domain: {error.GetDomain()}, code: {error.GetCode()}, message: {error.GetMessage()}");
                RetryLoadRewarded();
                return;
            }

            Debug.Log("[AdsManager] Rewarded ad loaded successfully");
            rewardedAd = ad;
            rewardedRetryAttempt = 0;
            RegisterEventHandlers(rewardedAd);
            Debug.Log("[AdsManager] Rewarded ad event handlers registered");
        });
    }

    private void RetryLoadRewarded()
    {
        rewardedRetryAttempt++;
        if (rewardedRetryAttempt < maxRetryCount)
        {
            Invoke(nameof(LoadRewardedAd), Mathf.Pow(2, rewardedRetryAttempt));
        }
    }

    private void RegisterRewardedEvents()
    {
        LoadRewardedAd();
    }

    public void ShowRewarded(Action<Reward> onRewarded, Action onSuccess, Action onFailure)
    {
        Debug.Log("[AdsManager] ShowRewarded() called");
        Debug.Log($"[AdsManager] Rewarded ad status - exists: {rewardedAd != null}, can show: {rewardedAd?.CanShowAd() ?? false}");
        Debug.Log($"[AdsManager] Is currently showing ad: {isShowingAd}");

        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            Debug.Log("[AdsManager] Showing rewarded ad");
            isShowingAd = true;

            // Create delegates that will unsubscribe themselves after execution
            System.Action onClosed = null;
            System.Action<AdError> onFailed = null;

            onClosed = () =>
            {
                Debug.Log("[AdsManager] Rewarded ad closed callback");
                // CRITICAL: Unsubscribe first to prevent memory leaks
                rewardedAd.OnAdFullScreenContentClosed -= onClosed;
                rewardedAd.OnAdFullScreenContentFailed -= onFailed;

                isShowingAd = false;
                Debug.Log("[AdsManager] Rewarded ad completed successfully - loading next ad");
                LoadRewardedAd(); // CRITICAL: Load next ad immediately after successful display
                onSuccess?.Invoke();
            };

            onFailed = (error) =>
            {
                Debug.LogError($"[AdsManager] Rewarded ad failed callback: {error}");
                // CRITICAL: Unsubscribe first to prevent memory leaks
                rewardedAd.OnAdFullScreenContentClosed -= onClosed;
                rewardedAd.OnAdFullScreenContentFailed -= onFailed;

                isShowingAd = false;
                Debug.Log("[AdsManager] Rewarded ad failed - loading new ad");
                LoadRewardedAd(); // CRITICAL: Load next ad even after failure
                onFailure?.Invoke();
            };

            rewardedAd.OnAdFullScreenContentClosed += onClosed;
            rewardedAd.OnAdFullScreenContentFailed += onFailed;

            Debug.Log("[AdsManager] Calling rewardedAd.Show()");
            rewardedAd.Show((reward) =>
            {
                Debug.Log($"[AdsManager] Rewarded ad granted reward: {reward.Amount} {reward.Type}");
                onRewarded?.Invoke(reward);
            });
        }
        else
        {
            Debug.LogWarning("[AdsManager] Rewarded ad not available - loading new ad");
            onFailure?.Invoke();
            LoadRewardedAd();
        }
    }

    public void ShowRewarded(Action onSuccess, Action onFailure)
    {
        ShowRewarded(null, onSuccess, onFailure);
    }

    public void ShowRewarded(Action onSuccess)
    {
        ShowRewarded(null, onSuccess, null);
    }

    public void ShowRewarded()
    {
        ShowRewarded(null, null, null);
    }

    private void OnRewardedClosed()
    {
        isShowingAd = false;
        Debug.Log("Rewarded ad closed");
    }

    private void OnRewardedFailed(AdError error)
    {
        isShowingAd = false;
        Debug.LogError($"Rewarded ad failed to show: {error}");
    }
    #endregion

    #region Rewarded Interstitial Ads
    private void LoadRewardedInterstitialAd()
    {
        if (!isInitialized) return;

        var adRequest = CreateAdRequest();
        RewardedInterstitialAd.Load(REWARDED_INTERSTITIAL_ID, adRequest,
            (RewardedInterstitialAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogError($"Rewarded interstitial ad failed to load: {error}");
                RetryLoadRewardedInterstitial();
                return;
            }

            rewardedInterstitialAd = ad;
            rewardedInterstitialRetryAttempt = 0;
            RegisterEventHandlers(rewardedInterstitialAd);
            Debug.Log("Rewarded interstitial ad loaded successfully");
        });
    }

    private void RetryLoadRewardedInterstitial()
    {
        rewardedInterstitialRetryAttempt++;
        if (rewardedInterstitialRetryAttempt < maxRetryCount)
        {
            Invoke(nameof(LoadRewardedInterstitialAd), Mathf.Pow(2, rewardedInterstitialRetryAttempt));
        }
    }

    private void RegisterRewardedInterstitialEvents()
    {
        LoadRewardedInterstitialAd();
    }

    public void ShowRewardedInterstitial(Action<Reward> onRewarded, Action onSuccess, Action onFailure)
    {
        if (rewardedInterstitialAd != null && rewardedInterstitialAd.CanShowAd())
        {
            isShowingAd = true;

            // Create delegates that will unsubscribe themselves after execution
            System.Action onClosed = null;
            System.Action<AdError> onFailed = null;

            onClosed = () =>
            {
                // CRITICAL: Unsubscribe first to prevent memory leaks
                rewardedInterstitialAd.OnAdFullScreenContentClosed -= onClosed;
                rewardedInterstitialAd.OnAdFullScreenContentFailed -= onFailed;

                isShowingAd = false;
                Debug.Log("[AdsManager] Rewarded interstitial ad closed - loading next ad");
                LoadRewardedInterstitialAd(); // CRITICAL: Load next ad immediately after successful display
                onSuccess?.Invoke();
            };

            onFailed = (error) =>
            {
                // CRITICAL: Unsubscribe first to prevent memory leaks
                rewardedInterstitialAd.OnAdFullScreenContentClosed -= onClosed;
                rewardedInterstitialAd.OnAdFullScreenContentFailed -= onFailed;

                isShowingAd = false;
                Debug.Log("[AdsManager] Rewarded interstitial ad failed - loading next ad");
                LoadRewardedInterstitialAd(); // CRITICAL: Load next ad even after failure
                onFailure?.Invoke();
            };

            rewardedInterstitialAd.OnAdFullScreenContentClosed += onClosed;
            rewardedInterstitialAd.OnAdFullScreenContentFailed += onFailed;

            rewardedInterstitialAd.Show((reward) =>
            {
                Debug.Log($"Rewarded interstitial ad granted reward: {reward.Amount} {reward.Type}");
                onRewarded?.Invoke(reward);
            });
        }
        else
        {
            onFailure?.Invoke();
            LoadRewardedInterstitialAd();
        }
    }

    public void ShowRewardedInterstitial(Action onSuccess, Action onFailure)
    {
        ShowRewardedInterstitial(null, onSuccess, onFailure);
    }

    public void ShowRewardedInterstitial(Action onSuccess)
    {
        ShowRewardedInterstitial(null, onSuccess, null);
    }

    public void ShowRewardedInterstitial()
    {
        ShowRewardedInterstitial(null, null, null);
    }
    #endregion

    #region App Open Ads
    private void LoadAppOpenAd()
    {
        if (!isInitialized) return;

        if (removeAds)
        {
            Debug.Log("App open ads are disabled due to Remove Ads setting");
            return;
        }

        var adRequest = CreateAdRequest();
        AppOpenAd.Load(APP_OPEN_ID, adRequest,
            (AppOpenAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogError($"App open ad failed to load: {error}");
                RetryLoadAppOpen();
                return;
            }

            appOpenAd = ad;
            appOpenExpireTime = DateTime.Now + TimeSpan.FromHours(4);
            appOpenRetryAttempt = 0;
            RegisterEventHandlers(appOpenAd);
            Debug.Log("App open ad loaded successfully");
        });
    }

    private void RetryLoadAppOpen()
    {
        appOpenRetryAttempt++;
        if (appOpenRetryAttempt < maxRetryCount)
        {
            Invoke(nameof(LoadAppOpenAd), Mathf.Pow(2, appOpenRetryAttempt));
        }
    }

    private void RegisterAppOpenEvents()
    {
        isAppOpenAdShowing = false;
        LoadAppOpenAd();
    }

    public bool IsAppOpenAdAvailable()
    {
        return appOpenAd != null && DateTime.Now < appOpenExpireTime && !isShowingAd && !isAppOpenAdShowing;
    }

    public void ShowAppOpenAd(Action onSuccess, Action onFailure)
    {
        if (removeAds)
        {
            Debug.Log("Cannot show app open ad - ads are disabled due to Remove Ads setting");
            onFailure?.Invoke();
            return;
        }

        if (isShowingAd || isAppOpenAdShowing)
        {
            onFailure?.Invoke();
            return;
        }

        if (IsAppOpenAdAvailable())
        {
            isShowingAd = true;
            isAppOpenAdShowing = true;

            // Create delegates that will unsubscribe themselves after execution
            System.Action onClosed = null;
            System.Action<AdError> onFailed = null;

            onClosed = () =>
            {
                // CRITICAL: Unsubscribe first to prevent memory leaks
                appOpenAd.OnAdFullScreenContentClosed -= onClosed;
                appOpenAd.OnAdFullScreenContentFailed -= onFailed;

                isShowingAd = false;
                isAppOpenAdShowing = false;
                Debug.Log("[AdsManager] App open ad closed - loading next ad");
                LoadAppOpenAd(); // CRITICAL: Load next ad immediately after successful display
                onSuccess?.Invoke();
            };

            onFailed = (error) =>
            {
                // CRITICAL: Unsubscribe first to prevent memory leaks
                appOpenAd.OnAdFullScreenContentClosed -= onClosed;
                appOpenAd.OnAdFullScreenContentFailed -= onFailed;

                isShowingAd = false;
                isAppOpenAdShowing = false;
                Debug.Log("[AdsManager] App open ad failed - loading next ad");
                LoadAppOpenAd(); // CRITICAL: Load next ad even after failure
                onFailure?.Invoke();
            };

            appOpenAd.OnAdFullScreenContentClosed += onClosed;
            appOpenAd.OnAdFullScreenContentFailed += onFailed;

            appOpenAd.Show();
        }
        else
        {
            onFailure?.Invoke();
            LoadAppOpenAd();
        }
    }

    public void ShowAppOpenAd(Action onSuccess)
    {
        ShowAppOpenAd(onSuccess, null);
    }

    public void ShowAppOpenAd()
    {
        ShowAppOpenAd(null, null);
    }
    #endregion

    #region Helper Methods
    private AdRequest CreateAdRequest()
    {
        Debug.Log("[AdsManager] CreateAdRequest() started");

        var adRequest = new AdRequest();
        Debug.Log("[AdsManager] AdRequest created successfully");

        // Add any additional request parameters here
        // For example: keywords, content URL, etc.

        return adRequest;
    }

    /// <summary>
    /// Logs comprehensive debug information about AdsManager state
    /// </summary>
    public void LogDebugStatus()
    {
        Debug.Log("=== [AdsManager] DEBUG STATUS ===");
        Debug.Log($"[AdsManager] Is Initialized: {isInitialized}");
        Debug.Log($"[AdsManager] Is Cold Start: {isColdStart}");
        Debug.Log($"[AdsManager] Remove Ads: {removeAds}");
        Debug.Log($"[AdsManager] Is Showing Ad: {isShowingAd}");
        Debug.Log($"[AdsManager] Test Ads Enabled: {enableTestAds}");

        Debug.Log("[AdsManager] Ad IDs:");
        Debug.Log($"[AdsManager]   Banner: {BANNER_ID}");
        Debug.Log($"[AdsManager]   Interstitial: {INTERSTITIAL_ID}");
        Debug.Log($"[AdsManager]   Rewarded: {REWARDED_ID}");
        Debug.Log($"[AdsManager]   Rewarded Interstitial: {REWARDED_INTERSTITIAL_ID}");
        Debug.Log($"[AdsManager]   App Open: {APP_OPEN_ID}");

        Debug.Log("[AdsManager] Ad States:");
        Debug.Log($"[AdsManager]   Banner Loaded: {isBannerLoaded}");
        Debug.Log($"[AdsManager]   Interstitial Available: {interstitialAd != null && interstitialAd.CanShowAd()}");
        Debug.Log($"[AdsManager]   Rewarded Available: {rewardedAd != null && rewardedAd.CanShowAd()}");
        Debug.Log($"[AdsManager]   Rewarded Interstitial Available: {rewardedInterstitialAd != null && rewardedInterstitialAd.CanShowAd()}");
        Debug.Log($"[AdsManager]   App Open Available: {IsAppOpenAdAvailable()}");

        Debug.Log("=== [AdsManager] DEBUG STATUS END ===");
    }

    /// <summary>
    /// Logs information about the encryption system being used for Remove Ads data.
    /// Useful for debugging storage and security issues.
    /// </summary>
    public void LogEncryptionInfo()
    {
        Debug.Log("=== [AdsManager] ENCRYPTION INFO ===");
        Debug.Log($"[AdsManager] Encrypted Storage Enabled: {useEncryptedStorage}");
        Debug.Log($"[AdsManager] Has Stored Data: {HasRemoveAdsDataInStorage()}");

        if (useEncryptedStorage)
        {
            Debug.Log($"[AdsManager] Encryption Method: AES-256-CBC");
            Debug.Log($"[AdsManager] Key Derivation: PBKDF2 (10000 iterations)");
            Debug.Log($"[AdsManager] {SecureStorage.GetEncryptionInfo()}");
            Debug.Log($"[AdsManager] Custom Salt Configured: {!string.IsNullOrEmpty(encryptionKey)}");

            if (encryptionKey == "YourCustomEncryptionKey123")
            {
                Debug.LogWarning("[AdsManager] WARNING: Using default encryption key. Change this in production!");
            }
        }
        else
        {
            Debug.Log($"[AdsManager] Encryption Method: None (Plain PlayerPrefs)");
            Debug.LogWarning("[AdsManager] WARNING: Remove Ads data is not encrypted!");
        }

        Debug.Log($"[AdsManager] Cloud Sync Enabled: {enableCloudSync}");
        Debug.Log("=== [AdsManager] ENCRYPTION INFO END ===");
    }
    #endregion

    #region CallBacks
    private void RegisterEventHandlers(AppOpenAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log($"App open ad paid {adValue.Value} {adValue.CurrencyCode}");
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("App open ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("App open ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("App open ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            RegisterAppOpenEvents();
            Debug.Log("App open ad full screen content closed.");
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("App open ad failed to open full screen content " +
                           "with error : " + error);
            RegisterAppOpenEvents();
        };
    }

    private void RegisterEventHandlers(InterstitialAd interstitialAd)
    {
        // Raised when the ad is estimated to have earned money.
        interstitialAd.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Interstitial ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        interstitialAd.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Interstitial ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        interstitialAd.OnAdClicked += () =>
        {
            Debug.Log("Interstitial ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        interstitialAd.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Interstitial ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        interstitialAd.OnAdFullScreenContentClosed += () =>
        {
            RegisterInterstitialEvents();
            Debug.Log("Interstitial ad full screen content closed.");
        };
        // Raised when the ad failed to open full screen content.
        interstitialAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Interstitial ad failed to open full screen content " +
                           "with error : " + error);
            RegisterInterstitialEvents();
        };
    }

    private void RegisterEventHandlers(RewardedInterstitialAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Rewarded interstitial ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Rewarded interstitial ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("Rewarded interstitial ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Rewarded interstitial ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            RegisterRewardedInterstitialEvents();
            Debug.Log("Rewarded interstitial ad full screen content closed.");
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Rewarded interstitial ad failed to open " +
                           "full screen content with error : " + error);
            RegisterRewardedInterstitialEvents();
        };
    }

    private void RegisterEventHandlers(RewardedAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Rewarded ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Rewarded ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("Rewarded ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Rewarded ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            RegisterRewardedEvents();
            Debug.Log("Rewarded ad full screen content closed.");
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Rewarded ad failed to open full screen content " +
                           "with error : " + error);
            RegisterRewardedEvents();
        };
    }
    #endregion

    // Public utility methods
    public bool IsInterstitialReady()
    {
        return interstitialAd != null && interstitialAd.CanShowAd();
    }

    public bool IsRewardedReady()
    {
        return rewardedAd != null && rewardedAd.CanShowAd();
    }

    public bool IsRewardedInterstitialReady()
    {
        return rewardedInterstitialAd != null && rewardedInterstitialAd.CanShowAd();
    }

    public void EnableAutoAppOpenAds(bool enable)
    {
        autoShowAppOpenAds = enable;
    }

    public string GetAdapterStatus(string adapterName)
    {
        // This would require storing the initialization status
        return "Not implemented";
    }

    private void OnDestroy()
    {
        // Clean up app state notifier
        AppStateEventNotifier.AppStateChanged -= OnAppStateChanged;

        DestroyBanner();

        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
        }

        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
        }

        if (rewardedInterstitialAd != null)
        {
            rewardedInterstitialAd.Destroy();
        }

        if (appOpenAd != null)
        {
            appOpenAd.Destroy();
        }
    }

    public void VerifyHit()
    {
        Debug.Log("Admob Verified and Instanciated");
    }

    /// <summary>
    /// Manually refresh mediation consent for all mediation networks.
    /// Useful when consent status changes or when adding new mediation networks.
    /// </summary>
    public void RefreshMediationConsent()
    {
        Debug.Log("[AdsManager] RefreshMediationConsent() called manually");
        SetMediationConsent();
    }

    /// <summary>
    /// Shows the privacy options form to allow users to change their consent choices.
    /// This should be called from a "Privacy Settings" or "Manage Consent" button in your UI.
    /// </summary>
    public void ShowPrivacyOptionsForm()
    {
        Debug.Log("[AdsManager] ShowPrivacyOptionsForm() called");
        
        if (ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required)
        {
            Debug.Log("[AdsManager] Privacy options form is required - showing form");
            ConsentForm.ShowPrivacyOptionsForm((FormError formError) =>
            {
                if (formError != null)
                {
                    Debug.LogError($"[AdsManager] Privacy options form error: {formError}");
                    Debug.LogError($"[AdsManager] Error message: {formError.Message}");
                }
                else
                {
                    Debug.Log("[AdsManager] Privacy options form completed successfully");
                    // Refresh mediation consent after privacy options change
                    SetMediationConsent();
                }
            });
        }
        else
        {
            Debug.LogWarning("[AdsManager] Privacy options form is not required for this user");
            Debug.LogWarning("[AdsManager] This typically means the user is not in EEA or has not given consent yet");
        }
    }

    /// <summary>
    /// Checks if privacy options entry point should be shown in your UI.
    /// Call this to determine whether to show a "Privacy Settings" button.
    /// </summary>
    public bool ShouldShowPrivacyOptionsButton()
    {
        bool shouldShow = ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
        Debug.Log($"[AdsManager] Should show privacy options button: {shouldShow}");
        return shouldShow;
    }

    /// <summary>
    /// Gets the current consent status for debugging and analytics purposes.
    /// </summary>
    public ConsentStatus GetCurrentConsentStatus()
    {
        return ConsentInformation.ConsentStatus;
    }

    /// <summary>
    /// Checks if the user can request ads based on current consent status.
    /// Useful for showing/hiding ad-related UI elements.
    /// </summary>
    public bool CanUserRequestAds()
    {
        return ConsentInformation.CanRequestAds();
    }
}