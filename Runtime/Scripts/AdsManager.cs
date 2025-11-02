#if ADMOB_INSTALLED
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using GoogleMobileAds.Common;
 
namespace Autech.Admob
{
    /// <summary>
    /// Main orchestrator for the ads system - manages all ad components.
    ///
    /// IMPORTANT - NAMESPACE REQUIREMENT (v2.0.3+):
    /// This class is now in the 'Autech.Admob' namespace. If upgrading from earlier versions,
    /// add this using directive to your scripts:
    /// <code>using Autech.Admob;</code>
    ///
    /// MIGRATION GUIDE:
    /// - Add 'using Autech.Admob;' to all scripts that reference AdsManager
    /// - Replace 'AdsManager.Instance' calls (no code change needed, just add using directive)
    /// - All ad-related classes now require the namespace (BannerPosition, BannerSize, etc.)
    /// </summary>
    public class AdsManager : MonoBehaviour
    {
        public struct AdsManagerSettings
        {
            public bool RemoveAds;
            public bool ForceEEAGeographyForTesting;
            public bool EnableConsentDebugging;
            public bool AlwaysRequestConsentUpdate;
            public bool TagForUnderAgeOfConsent;
            public bool AutoShowAppOpenAds;
            public bool EnableTestAds;
            public float AppOpenCooldownTime;
            public bool UseAdaptiveBanners;
            public bool EnableCollapsibleBanners;
            public BannerSize PreferredBannerSize;
            public BannerPosition BannerPosition;
            public string AndroidBannerId;
            public string AndroidInterstitialId;
            public string AndroidRewardedId;
            public string AndroidRewardedInterstitialId;
            public string AndroidAppOpenId;
            public string IosBannerId;
            public string IosInterstitialId;
            public string IosRewardedId;
            public string IosRewardedInterstitialId;
            public string IosAppOpenId;
        }

        private static AdsManager instance;
        private static readonly object lockObject = new object();
        private static SynchronizationContext unityContext;
        private static int mainThreadId = -1;
        private static bool isQuitting = false;
        private static readonly object instanceInitLock = new object();
        private readonly object adShowLock = new object();
        private readonly object initializationTaskLock = new object();
        private Task initializationTask;
        private bool hasAppliedConfiguration;
        private AdsManagerSettings lastAppliedSettings;

        private static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == mainThreadId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CaptureUnitySynchronizationContext()
        {
            unityContext = SynchronizationContext.Current;
            mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public static AdsManager Instance
        {
            get
            {
                if (!ReferenceEquals(instance, null))
                {
                    return instance;
                }

                EnsureMainThreadContext();

                if (IsMainThread)
                {
                    EnsureInstanceInitialized();
                }
                else if (unityContext != null)
                {
                    using (var waitHandle = new ManualResetEventSlim(false))
                    {
                        unityContext.Post(_ =>
                        {
                            try
                            {
                                EnsureInstanceInitialized();
                            }
                            finally
                            {
                                waitHandle.Set();
                            }
                        }, null);

                        waitHandle.Wait();
                    }
                }
                else
                {
                    Debug.LogWarning("[AdsManager] Unity synchronization context unavailable. Returning existing instance reference.");
                }

                return instance;
            }
        }

        private static void EnsureMainThreadContext()
        {
            if (unityContext == null && SynchronizationContext.Current != null)
            {
                unityContext = SynchronizationContext.Current;
            }

            if (mainThreadId == -1)
            {
                mainThreadId = Thread.CurrentThread.ManagedThreadId;
            }
        }

        private static void EnsureInstanceInitialized()
        {
            if (!ReferenceEquals(instance, null) || isQuitting)
            {
                return;
            }

            lock (instanceInitLock)
            {
                if (!ReferenceEquals(instance, null) || isQuitting)
                {
                    return;
                }

#if UNITY_2023_1_OR_NEWER
                instance = FindFirstObjectByType<AdsManager>();
#else
                instance = FindObjectOfType<AdsManager>();
#endif

                if (!ReferenceEquals(instance, null))
                {
                    return;
                }

                Debug.LogWarning("[AdsManager] No instance found. Creating new AdsManager automatically.");
                Debug.LogWarning("[AdsManager] For better control, add VerifyAdmob prefab to your scene.");

                var adsManagerObject = new GameObject("AdsManager");
                instance = adsManagerObject.AddComponent<AdsManager>();
            }
        }

        // Components
        private AdConfiguration config;
        private BannerAdController bannerController;
        private InterstitialAdController interstitialController;
        private RewardedAdController rewardedController;
        private RewardedInterstitialAdController rewardedInterstitialController;
        private AppOpenAdController appOpenController;
        private AdPersistenceManager persistenceManager;
        private MediationConsentManager mediationConsentManager;

        private bool isInitialized = false;
        private bool isShowingAd = false;
        private bool isFirstTimeLoading = true;

        // Events
        public static Action<bool> OnRemoveAdsChanged;
        public static Action<bool> OnRemoveAdsLoadedFromStorage;

        // Public Properties - Components
        public ConsentManager consentManager { get; private set; }

        // Public Properties - Configuration
        public bool IsInitialized => isInitialized;
        public bool IsShowingAd => isShowingAd;
        public bool IsFirstTimeLoading => isFirstTimeLoading;

        // Configuration accessors
        public bool AutoShowAppOpenAds
        {
            get => config?.AutoShowAppOpenAds ?? true;
            set { if (config != null) config.AutoShowAppOpenAds = value; }
        }

        public bool EnableTestAds
        {
            get => config?.EnableTestAds ?? true;
            set { if (config != null) config.EnableTestAds = value; }
        }

        public float AppOpenCooldownTime
        {
            get => config?.AppOpenCooldownTime ?? 4f;
            set { if (config != null) config.AppOpenCooldownTime = value; }
        }

        public bool RemoveAds
        {
            get => config?.RemoveAds ?? false;
            set
            {
                if (config == null || persistenceManager == null) return;

                if (config.RemoveAds != value)
                {
                    config.RemoveAds = value;
                    persistenceManager.SaveRemoveAdsStatus(value);
                    OnRemoveAdsChangedInternal(value);
                    OnRemoveAdsChanged?.Invoke(value);
                }
            }
        }

        public bool UseAdaptiveBanners
        {
            get => config?.UseAdaptiveBanners ?? true;
            set { if (config != null) config.UseAdaptiveBanners = value; }
        }

        public bool EnableCollapsibleBanners
        {
            get => config?.EnableCollapsibleBanners ?? false;
            set { if (config != null) config.EnableCollapsibleBanners = value; }
        }

        public BannerSize PreferredBannerSize
        {
            get => config?.PreferredBannerSize ?? BannerSize.Banner;
            set { if (config != null) config.PreferredBannerSize = value; }
        }

        // Consent Configuration Properties
        public bool ForceEEAGeographyForTesting
        {
            get => consentManager?.ForceEEAGeographyForTesting ?? false;
            set { if (consentManager != null) consentManager.ForceEEAGeographyForTesting = value; }
        }

        public bool EnableConsentDebugging
        {
            get => consentManager?.EnableConsentDebugging ?? false;
            set { if (consentManager != null) consentManager.EnableConsentDebugging = value; }
        }

        public bool AlwaysRequestConsentUpdate
        {
            get => consentManager?.AlwaysRequestConsentUpdate ?? true;
            set { if (consentManager != null) consentManager.AlwaysRequestConsentUpdate = value; }
        }

        public bool TagForUnderAgeOfConsent
        {
            get => consentManager?.TagForUnderAgeOfConsent ?? false;
            set { if (consentManager != null) consentManager.TagForUnderAgeOfConsent = value; }
        }

        // Ad Unit ID Properties
        public string AndroidBannerId { get => config?.AndroidBannerId ?? ""; set { if (config != null) config.AndroidBannerId = value; } }
        public string AndroidInterstitialId { get => config?.AndroidInterstitialId ?? ""; set { if (config != null) config.AndroidInterstitialId = value; } }
        public string AndroidRewardedId { get => config?.AndroidRewardedId ?? ""; set { if (config != null) config.AndroidRewardedId = value; } }
        public string AndroidRewardedInterstitialId { get => config?.AndroidRewardedInterstitialId ?? ""; set { if (config != null) config.AndroidRewardedInterstitialId = value; } }
        public string AndroidAppOpenId { get => config?.AndroidAppOpenId ?? ""; set { if (config != null) config.AndroidAppOpenId = value; } }

        public string IosBannerId { get => config?.IosBannerId ?? ""; set { if (config != null) config.IosBannerId = value; } }
        public string IosInterstitialId { get => config?.IosInterstitialId ?? ""; set { if (config != null) config.IosInterstitialId = value; } }
        public string IosRewardedId { get => config?.IosRewardedId ?? ""; set { if (config != null) config.IosRewardedId = value; } }
        public string IosRewardedInterstitialId { get => config?.IosRewardedInterstitialId ?? ""; set { if (config != null) config.IosRewardedInterstitialId = value; } }
        public string IosAppOpenId { get => config?.IosAppOpenId ?? ""; set { if (config != null) config.IosAppOpenId = value; } }

        public string CurrentBannerId => config?.BannerId ?? "";
        public string CurrentInterstitialId => config?.InterstitialId ?? "";
        public string CurrentRewardedId => config?.RewardedId ?? "";
        public string CurrentRewardedInterstitialId => config?.RewardedInterstitialId ?? "";
        public string CurrentAppOpenId => config?.AppOpenId ?? "";

        public BannerPosition CurrentBannerPosition => bannerController?.CurrentPosition ?? BannerPosition.Bottom;

        private void EnsureInitializationRequested()
        {
            if (isInitialized || initializationTask != null)
            {
                return;
            }

            Debug.LogWarning("[AdsManager] API invoked before InitializeAsync(). Starting initialization with current settings.");
            _ = InitializeAsync();
        }

        public void ApplyConfiguration(AdsManagerSettings settings)
        {
            if (config == null || consentManager == null)
            {
                Debug.LogWarning("[AdsManager] ApplyConfiguration called before components were initialized.");
                return;
            }

            AutoShowAppOpenAds = settings.AutoShowAppOpenAds;
            EnableTestAds = settings.EnableTestAds;
            AppOpenCooldownTime = settings.AppOpenCooldownTime;
            UseAdaptiveBanners = settings.UseAdaptiveBanners;
            EnableCollapsibleBanners = settings.EnableCollapsibleBanners;
            PreferredBannerSize = settings.PreferredBannerSize;

            ForceEEAGeographyForTesting = settings.ForceEEAGeographyForTesting;
            EnableConsentDebugging = settings.EnableConsentDebugging;
            AlwaysRequestConsentUpdate = settings.AlwaysRequestConsentUpdate;
            TagForUnderAgeOfConsent = settings.TagForUnderAgeOfConsent;

            SetAllAdIds(
                settings.AndroidBannerId, settings.AndroidInterstitialId, settings.AndroidRewardedId, settings.AndroidRewardedInterstitialId, settings.AndroidAppOpenId,
                settings.IosBannerId, settings.IosInterstitialId, settings.IosRewardedId, settings.IosRewardedInterstitialId, settings.IosAppOpenId);

            SetBannerPosition(settings.BannerPosition);

            RemoveAds = settings.RemoveAds;

            hasAppliedConfiguration = true;
            lastAppliedSettings = settings;
        }

        public Task InitializeAsync()
        {
            if (isInitialized)
            {
                return Task.CompletedTask;
            }

            lock (initializationTaskLock)
            {
                if (initializationTask == null)
                {
                    if (!hasAppliedConfiguration)
                    {
                        Debug.LogWarning("[AdsManager] InitializeAsync() called before ApplyConfiguration(). Using current configuration values.");
                    }

                    initializationTask = InitializeAdsAsyncInternal();
                }
            }

            return initializationTask;
        }

        /// <summary>
        /// Thread-safe method to check and set the ad showing flag atomically.
        /// Returns true if ad can be shown, false if another ad is already showing.
        /// </summary>
        private bool TrySetAdShowing()
        {
            lock (adShowLock)
            {
                if (isShowingAd)
                {
                    return false;
                }
                isShowingAd = true;
                return true;
            }
        }

        /// <summary>
        /// Thread-safe method to clear the ad showing flag.
        /// </summary>
        private void ClearAdShowing()
        {
            lock (adShowLock)
            {
                isShowingAd = false;
            }
        }

        private void Awake()
        {
            Debug.Log("[AdsManager] Awake() called");

            // Thread-safe singleton initialization in Awake
            EnsureMainThreadContext();

            lock (lockObject)
            {
                if (instance == null)
                {
                    Debug.Log("[AdsManager] Creating singleton instance");
                    instance = this;
                    DontDestroyOnLoad(gameObject);
                    MobileAdsEventExecutor.Initialize();
                    MobileAds.RaiseAdEventsOnUnityMainThread = true;
                    InitializeComponents();
                }
                else
                {
                    Debug.Log("[AdsManager] Duplicate instance detected - destroying");
                    Destroy(gameObject);
                }
            }
        }

        private void InitializeComponents()
        {
            Debug.Log("[AdsManager] Initializing components");

            config = new AdConfiguration();
            consentManager = new ConsentManager();
            persistenceManager = new AdPersistenceManager();
            mediationConsentManager = new MediationConsentManager(consentManager);

            bannerController = new BannerAdController(config);
            interstitialController = new InterstitialAdController(config);
            rewardedController = new RewardedAdController(config);
            rewardedInterstitialController = new RewardedInterstitialAdController(config);
            appOpenController = new AppOpenAdController(config);

            // Load saved RemoveAds status
            bool savedRemoveAds = persistenceManager.LoadRemoveAdsStatus();
            config.RemoveAds = savedRemoveAds;

            // Subscribe to persistence events
            persistenceManager.OnRemoveAdsLoadedFromStorage += OnRemoveAdsLoadedFromStorageHandler;

            // Subscribe to consent events
            consentManager.OnConsentReady += OnConsentReady;
            consentManager.OnConsentError += OnConsentError;

            Debug.Log($"[AdsManager] Components initialized. RemoveAds: {config.RemoveAds}");
        }

        private async Task InitializeAdsAsyncInternal()
        {
            try
            {
                Debug.Log("[AdsManager] InitializeAdsAsync() started");

            if (config.EnableTestAds)
            {
                Debug.Log("[AdsManager] Resetting consent for testing");
                ConsentInformation.Reset();
            }

            config.LogCurrentAdIds();
            config.WarnIfUsingTestIds();

            bool skipConsent = consentManager.EnableConsentDebugging;

            if (!skipConsent)
            {
                // Initialize consent first
                await consentManager.InitializeConsentAsync();
            }
            else
            {
                Debug.LogWarning("[AdsManager] Consent debugging enabled – skipping UMP flow and initializing AdMob directly.");
                mediationConsentManager.SetMediationConsent(consentManager?.TagForUnderAgeOfConsent ?? false);
                InitializeAdMob();
                CompleteInitialization(null);
            }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AdsManager] Initialization failed: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        private void OnConsentReady(bool canRequestAds)
        {
            Debug.Log($"[AdsManager] OnConsentReady() - Can request ads: {canRequestAds}");

            if (canRequestAds)
            {
                mediationConsentManager.SetMediationConsent(consentManager?.TagForUnderAgeOfConsent ?? false);
                InitializeAdMob();
            }
            else
            {
                Debug.LogWarning("[AdsManager] Cannot request ads - initialization aborted");
            }
        }

        private void OnConsentError(string errorMessage)
        {
            Debug.LogError($"[AdsManager] Consent error occurred: {errorMessage}");
        }

        private void InitializeAdMob()
        {
            Debug.Log("[AdsManager] InitializeAdMob() started");

            var requestConfiguration = new RequestConfiguration
            {
                // Test mode is enabled via EnableTestAds flag - no need for specific device IDs
                //TestDeviceIds = config.EnableTestAds ? new System.Collections.Generic.List<string>() : null
                TestDeviceIds = new System.Collections.Generic.List<string>()
            };

            MobileAds.SetRequestConfiguration(requestConfiguration);

            MobileAds.Initialize((InitializationStatus initStatus) =>
            {
                Debug.Log("[AdsManager] MobileAds.Initialize() callback executed");
                CompleteInitialization(initStatus);
            });
        }

        private void CompleteInitialization(InitializationStatus initStatus)
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;

            if (initStatus != null)
            {
                foreach (var adapterStatus in initStatus.getAdapterStatusMap())
                {
                    Debug.Log($"[AdsManager] Adapter: {adapterStatus.Key}, Status: {adapterStatus.Value.InitializationState}");
                }
            }

            LoadAllAds();
            appOpenController.MarkColdStartComplete();
        }

        private void LoadAllAds()
        {
            Debug.Log($"[AdsManager] LoadAllAds() - RemoveAds: {config.RemoveAds}");

            // Always load rewarded ads
            rewardedController.LoadAd();
            rewardedInterstitialController.LoadAd();

            // Only load non-rewarded ads if RemoveAds is false
            if (!config.RemoveAds)
            {
                // Note: Banner loads when ShowBanner(true) is called, not pre-loaded
                interstitialController.LoadAd();
                appOpenController.LoadAd();
            }

            isFirstTimeLoading = false;
        }

        private void OnRemoveAdsChangedInternal(bool removeAdsEnabled)
        {
            Debug.Log($"[AdsManager] Remove Ads {(removeAdsEnabled ? "enabled" : "disabled")}");

            if (removeAdsEnabled)
            {
                bannerController.DestroyBanner();
                interstitialController.Destroy();
                appOpenController.Destroy();
            }
            else if (isInitialized)
            {
                bannerController.LoadBanner();
                interstitialController.LoadAd();
                appOpenController.LoadAd();
            }
        }

        #region Banner Ads
        public void LoadBanner()
        {
            EnsureInitializationRequested();
            bannerController.LoadBanner();
        }

        public void ShowBanner(bool show)
        {
            EnsureInitializationRequested();
            bannerController.ShowBanner(show);
        }
        public void SetInitialBannerVisibility(bool show) => ShowBanner(show);
        public void SetBannerPosition(BannerPosition position) => bannerController.SetBannerPosition(position);
        public void SetBannerSize(BannerSize size)
        {
            config.PreferredBannerSize = size;
            if (bannerController != null)
            {
                bool wasVisible = bannerController.IsBannerVisible;
                bannerController.DestroyBanner();
                bannerController.LoadBanner();
                if (wasVisible) bannerController.ShowBanner(true);
            }
        }
        public void EnableAdaptiveBanners(bool enable)
        {
            config.UseAdaptiveBanners = enable;
            if (bannerController != null)
            {
                bool wasVisible = bannerController.IsBannerVisible;
                bannerController.DestroyBanner();
                bannerController.LoadBanner();
                if (wasVisible) bannerController.ShowBanner(true);
            }
        }
        public bool IsBannerLoaded() => bannerController?.IsBannerLoaded ?? false;
        public bool IsBannerVisible() => bannerController?.IsBannerVisible ?? false;
        public Vector2 GetBannerSize() => bannerController?.GetBannerSize() ?? Vector2.zero;
        #endregion

        #region Interstitial Ads
        public void ShowInterstitial(Action onSuccess, Action onFailure)
        {
            EnsureInitializationRequested();
            if (interstitialController == null)
            {
                Debug.LogWarning("[AdsManager] InterstitialController not initialized");
                onFailure?.Invoke();
                return;
            }

            // Prevent concurrent ad displays (thread-safe)
            if (!TrySetAdShowing())
            {
                Debug.LogWarning("[AdsManager] Cannot show interstitial - another ad is currently being displayed");
                onFailure?.Invoke();
                return;
            }

            try
            {
                interstitialController.Show(() =>
                {
                    ClearAdShowing();
                    onSuccess?.Invoke();
                }, () =>
                {
                    ClearAdShowing();
                    onFailure?.Invoke();
                });
            }
            catch (Exception ex)
            {
                ClearAdShowing();
                Debug.LogError($"[AdsManager] ShowInterstitial failed: {ex.Message}");
                onFailure?.Invoke();
            }
        }
        public void ShowInterstitial(Action onSuccess) => ShowInterstitial(onSuccess, null);
        public void ShowInterstitial() => ShowInterstitial(null, null);
        public bool IsInterstitialReady() => interstitialController?.IsReady ?? false;
        #endregion

        #region Rewarded Ads
        public void ShowRewarded(Action<Reward> onRewarded, Action onSuccess, Action onFailure)
        {
            EnsureInitializationRequested();
            if (rewardedController == null)
            {
                Debug.LogWarning("[AdsManager] RewardedController not initialized");
                onFailure?.Invoke();
                return;
            }

            // Prevent concurrent ad displays (thread-safe)
            if (!TrySetAdShowing())
            {
                Debug.LogWarning("[AdsManager] Cannot show rewarded ad - another ad is currently being displayed");
                onFailure?.Invoke();
                return;
            }

            
            try
            {
                rewardedController.Show(onRewarded, () =>
                {
                    ClearAdShowing();
                    onSuccess?.Invoke();
                }, () =>
                {
                    ClearAdShowing();
                    onFailure?.Invoke();
                });
            }
            catch (Exception ex)
            {
                ClearAdShowing();
                Debug.LogError($"[AdsManager] ShowRewarded failed: {ex.Message}");
                onFailure?.Invoke();
            }
        }
        public void ShowRewarded(Action onSuccess, Action onFailure) => ShowRewarded(null, onSuccess, onFailure);
        public void ShowRewarded(Action onSuccess) => ShowRewarded(null, onSuccess, null);
        public void ShowRewarded() => ShowRewarded(null, null, null);
        public bool IsRewardedReady() => rewardedController?.IsReady ?? false;
        #endregion

        #region Rewarded Interstitial Ads
        public void ShowRewardedInterstitial(Action<Reward> onRewarded, Action onSuccess, Action onFailure)
        {
            EnsureInitializationRequested();
            if (rewardedInterstitialController == null)
            {
                Debug.LogWarning("[AdsManager] RewardedInterstitialController not initialized");
                onFailure?.Invoke();
                return;
            }

            // Prevent concurrent ad displays (thread-safe)
            if (!TrySetAdShowing())
            {
                Debug.LogWarning("[AdsManager] Cannot show rewarded interstitial - another ad is currently being displayed");
                onFailure?.Invoke();
                return;
            }

            
            try
            {
                rewardedInterstitialController.Show(onRewarded, () =>
                {
                    ClearAdShowing();
                    onSuccess?.Invoke();
                }, () =>
                {
                    ClearAdShowing();
                    onFailure?.Invoke();
                });
            }
            catch (Exception ex)
            {
                ClearAdShowing();
                Debug.LogError($"[AdsManager] ShowRewardedInterstitial failed: {ex.Message}");
                onFailure?.Invoke();
            }
        }
        public void ShowRewardedInterstitial(Action onSuccess, Action onFailure) => ShowRewardedInterstitial(null, onSuccess, onFailure);
        public void ShowRewardedInterstitial(Action onSuccess) => ShowRewardedInterstitial(null, onSuccess, null);
        public void ShowRewardedInterstitial() => ShowRewardedInterstitial(null, null, null);
        public bool IsRewardedInterstitialReady() => rewardedInterstitialController?.IsReady ?? false;
        #endregion

        #region App Open Ads
        public void ShowAppOpenAd(Action onSuccess, Action onFailure)
        {
            EnsureInitializationRequested();
            if (appOpenController == null)
            {
                Debug.LogWarning("[AdsManager] AppOpenController not initialized");
                onFailure?.Invoke();
                return;
            }

            // Prevent concurrent ad displays (thread-safe)
            if (!TrySetAdShowing())
            {
                Debug.LogWarning("[AdsManager] Cannot show app open ad - another ad is currently being displayed");
                onFailure?.Invoke();
                return;
            }

            
            try
            {
                appOpenController.Show(() =>
                {
                    ClearAdShowing();
                    onSuccess?.Invoke();
                }, () =>
                {
                    ClearAdShowing();
                    onFailure?.Invoke();
                });
            }
            catch (Exception ex)
            {
                ClearAdShowing();
                Debug.LogError($"[AdsManager] ShowAppOpenAd failed: {ex.Message}");
                onFailure?.Invoke();
            }
        }
        public void ShowAppOpenAd(Action onSuccess) => ShowAppOpenAd(onSuccess, null);
        public void ShowAppOpenAd() => ShowAppOpenAd(null, null);
        public bool IsAppOpenAdAvailable() => appOpenController?.IsAvailable ?? false;
        public void EnableAutoAppOpenAds(bool enable) { if (config != null) config.AutoShowAppOpenAds = enable; }
        #endregion

        #region Ad Unit ID Management
        public void SetAndroidAdIds(string bannerId, string interstitialId, string rewardedId, string rewardedInterstitialId, string appOpenId)
        {
            config.SetAndroidAdIds(bannerId, interstitialId, rewardedId, rewardedInterstitialId, appOpenId);
            RefreshAdsWithNewIds();
        }

        public void SetIosAdIds(string bannerId, string interstitialId, string rewardedId, string rewardedInterstitialId, string appOpenId)
        {
            config.SetIosAdIds(bannerId, interstitialId, rewardedId, rewardedInterstitialId, appOpenId);
            RefreshAdsWithNewIds();
        }

        public void SetAllAdIds(
            string androidBanner, string androidInterstitial, string androidRewarded, string androidRewardedInterstitial, string androidAppOpen,
            string iosBanner, string iosInterstitial, string iosRewarded, string iosRewardedInterstitial, string iosAppOpen)
        {
            config.SetAllAdIds(androidBanner, androidInterstitial, androidRewarded, androidRewardedInterstitial, androidAppOpen,
                              iosBanner, iosInterstitial, iosRewarded, iosRewardedInterstitial, iosAppOpen);
            RefreshAdsWithNewIds();
        }

        private void RefreshAdsWithNewIds()
        {
            if (!isInitialized)
            {
                Debug.Log("[AdsManager] Not initialized yet - new IDs will be used when initialized");
                return;
            }

            Debug.Log("[AdsManager] Refreshing ads with new Ad Unit IDs");

            // Verify ad controllers are initialized before destroying
            if (bannerController == null || interstitialController == null || 
                rewardedController == null || rewardedInterstitialController == null || 
                appOpenController == null)
            {
                Debug.LogWarning("[AdsManager] Ad controllers not fully initialized - skipping refresh");
                return;
            }

            bannerController.DestroyBanner();
            interstitialController.Destroy();
            rewardedController.Destroy();
            rewardedInterstitialController.Destroy();
            appOpenController.Destroy();

            LoadAllAds();
        }

        public void LogCurrentAdIds() => config.LogCurrentAdIds();
        public bool AreAdIdsValid() => config.AreAdIdsValid();
        public bool AreTestAdIds() => config.AreTestAdIds();
        #endregion

        #region Persistence
        public void ForceLoadFromStorage()
        {
            bool savedValue = persistenceManager.LoadRemoveAdsStatus();
            RemoveAds = savedValue;
        }

        public void ForceSaveToStorage() => persistenceManager.SaveRemoveAdsStatus(config.RemoveAds);
        public void ClearRemoveAdsData()
        {
            persistenceManager.ClearRemoveAdsData();
            RemoveAds = false;
        }
        public bool HasRemoveAdsDataInStorage() => persistenceManager.HasRemoveAdsDataInStorage();
        public bool MigrateLegacyEncryption() => persistenceManager.MigrateLegacyEncryption();
        public bool NeedsLegacyMigration() => persistenceManager.NeedsLegacyMigration();
        #endregion

        #region Consent
        public void ShowPrivacyOptionsForm() => consentManager.ShowPrivacyOptionsForm();

        public string GetTCFConsentString() => consentManager?.GetTCFConsentString() ?? "";

        public bool HasConsentForPurpose(int purposeId) => consentManager?.HasConsentForPurpose(purposeId) ?? false;

        public string GetConsentType() => consentManager?.GetConsentType() ?? "Unknown";

        public void CheckConsentStatus() => consentManager?.CheckConsentStatusOnResume();
        public bool ShouldShowPrivacyOptionsButton() => consentManager.ShouldShowPrivacyOptionsButton();
        public ConsentStatus GetCurrentConsentStatus() => consentManager.GetCurrentConsentStatus();
        public bool CanUserRequestAds() => consentManager.CanUserRequestAds();
        public void RefreshMediationConsent() => mediationConsentManager.SetMediationConsent(consentManager?.TagForUnderAgeOfConsent ?? false);
        #endregion

        #region Debug
        public void VerifyHit() => Debug.Log("[AdsManager] Admob Verified and Instantiated");

        public void LogDebugStatus()
        {
            Debug.Log("=== [AdsManager] DEBUG STATUS ===");
            Debug.Log($"Is Initialized: {isInitialized}");
            Debug.Log($"Remove Ads: {config.RemoveAds}");
            Debug.Log($"Test Ads: {config.EnableTestAds}");
            Debug.Log($"Banner Ready: {IsBannerLoaded()}");
            Debug.Log($"Interstitial Ready: {IsInterstitialReady()}");
            Debug.Log($"Rewarded Ready: {IsRewardedReady()}");
            Debug.Log($"App Open Ready: {IsAppOpenAdAvailable()}");
            Debug.Log("================================");
        }

        public void LogEncryptionInfo() => persistenceManager.LogEncryptionInfo();
        #endregion

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            if (consentManager != null)
            {
                consentManager.OnConsentReady -= OnConsentReady;
                consentManager.OnConsentError -= OnConsentError;
            }

            if (persistenceManager != null)
            {
                persistenceManager.OnRemoveAdsLoadedFromStorage -= OnRemoveAdsLoadedFromStorageHandler;
            }

            // Destroy ad controllers
            bannerController?.DestroyBanner();
            interstitialController?.Destroy();
            rewardedController?.Destroy();
            rewardedInterstitialController?.Destroy();
            appOpenController?.Destroy();

            // Clear instance reference if this is the singleton
            if (instance == this)
            {
                instance = null;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus && isInitialized && consentManager != null)
            {
                Debug.Log("[AdsManager] App resumed - checking consent status");
                consentManager.CheckConsentStatusOnResume();
            }
        }

        private void OnRemoveAdsLoadedFromStorageHandler(bool value)
        {
            OnRemoveAdsLoadedFromStorage?.Invoke(value);
        }
    }
}
#endif // ADMOB_INSTALLED
