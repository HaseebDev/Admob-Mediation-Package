using UnityEngine;
using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using GoogleMobileAds.Mediation.UnityAds.Api;
using GoogleMobileAds.Common;

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

    // Ad Unit IDs
#if UNITY_ANDROID
    private const string BANNER_ID = "ca-app-pub-3940256099942544/6300978111";
    private const string INTERSTITIAL_ID = "ca-app-pub-3940256099942544/1033173712";
    private const string REWARDED_ID = "ca-app-pub-3940256099942544/5224354917";
    private const string REWARDED_INTERSTITIAL_ID = "ca-app-pub-3940256099942544/5354046379";
    private const string APP_OPEN_ID = "ca-app-pub-3940256099942544/9257395921";
#elif UNITY_IOS
    private const string BANNER_ID = "ca-app-pub-3940256099942544/2934735716";
    private const string INTERSTITIAL_ID = "ca-app-pub-3940256099942544/4411468910";
    private const string REWARDED_ID = "ca-app-pub-3940256099942544/1712485313";
    private const string REWARDED_INTERSTITIAL_ID = "ca-app-pub-3940256099942544/6978759866";
    private const string APP_OPEN_ID = "ca-app-pub-3940256099942544/5575463023";
#else
    private const string BANNER_ID = "";
    private const string INTERSTITIAL_ID = "";
    private const string REWARDED_ID = "";
    private const string REWARDED_INTERSTITIAL_ID = "";
    private const string APP_OPEN_ID = "";
#endif

    [Header("Ad Settings")]
    [SerializeField] private bool autoShowAppOpenAds = true;
    [SerializeField] private bool enableTestAds = true;
    [SerializeField] private float appOpenCooldownTime = 4f; // seconds between app open ads
    
    [Header("Banner Settings")]
    [SerializeField] private bool useAdaptiveBanners = true;
    [SerializeField] private bool enableCollapsibleBanners = false;
    [SerializeField] private BannerSize preferredBannerSize = BannerSize.Banner;
    
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

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAds();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Initialize app state notifier for app open ads
        AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
    }

    private void InitializeAds()
    {
        RequestConsentInfo();
    }

    private void RequestConsentInfo()
    {
        var debugSettings = new ConsentDebugSettings
        {
            DebugGeography = enableTestAds ? DebugGeography.EEA : DebugGeography.Disabled
        };

        ConsentRequestParameters request = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = false,
            ConsentDebugSettings = debugSettings
        };

        ConsentInformation.Update(request, OnConsentInfoUpdated);
    }

    private void OnConsentInfoUpdated(FormError error)
    {
        if (error != null)
        {
            Debug.LogError($"Consent Error: {error}");
            // Initialize anyway if consent fails
            InitializeAdMob();
            return;
        }

        if (ConsentInformation.IsConsentFormAvailable())
        {
            LoadAndShowConsentForm();
        }
        else
        {
            InitializeAdMob();
        }
    }

   private void LoadAndShowConsentForm()
    {
        ConsentForm.Load((consentForm, loadError) =>
        {
            if (loadError != null)
            {
                Debug.LogError($"Consent form error: {loadError}");
                // Initialize anyway if consent form fails to load
                InitializeAdMob();
                return;
            }

            if (ConsentInformation.ConsentStatus == ConsentStatus.Required)
            {
                consentForm.Show((showError) =>
                {
                    if (showError != null)
                    {
                        Debug.LogError($"Error showing consent form: {showError}");
                    }

                    // Set Unity Ads consent after user's choice
                    SetMediationConsent();
                    InitializeAdMob();
                });
            }
            else
            {
                // Set consent metadata for existing consent
                SetMediationConsent();
                InitializeAdMob();
            }
        });
    }

    private void SetMediationConsent()
    {
        bool hasConsent = ConsentInformation.ConsentStatus == ConsentStatus.Obtained;
        
        // Unity Ads mediation consent
        UnityAds.SetConsentMetaData("gdpr.consent", hasConsent);
        UnityAds.SetConsentMetaData("privacy.consent", hasConsent);
        
        // Additional mediation networks can be added here
        Debug.Log($"Mediation consent set: {hasConsent}");
    }

    private void InitializeAdMob()
    {
        // Configure request configuration for test devices if needed
        var requestConfiguration = new RequestConfiguration
        {
            TestDeviceIds = enableTestAds ? new System.Collections.Generic.List<string> { "YOUR_TEST_DEVICE_ID" } : null
        };
        
        MobileAds.SetRequestConfiguration(requestConfiguration);

        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            isInitialized = true;
            isColdStart = false; // Initialization complete, no longer cold start
            Debug.Log("AdMob initialized successfully");
            
            // Log mediation adapter statuses
            foreach (var adapterStatus in initStatus.getAdapterStatusMap())
            {
                Debug.Log($"Adapter: {adapterStatus.Key}, Status: {adapterStatus.Value.InitializationState}, Description: {adapterStatus.Value.Description}");
            }
            
            LoadAllAds();
        });
    }

    private void LoadAllAds()
    {
        LoadInterstitialAd();
        LoadRewardedAd();
        LoadRewardedInterstitialAd();
        LoadAppOpenAd();
        LoadBanner();
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
            ShowAppOpenAd(() => {
                hasShownFirstAppOpenAd = true;
                lastAppOpenShownTime = DateTime.Now;
            });
        }
    }

    #region Banner Ads
    public void LoadBanner()
    {
        if (!isInitialized) 
        {
            Debug.LogWarning("AdMob not initialized yet");
            return;
        }

        DestroyBanner();

        AdSize adSize = GetAdSize();
        bannerView = new BannerView(BANNER_ID, adSize, currentBannerPosition);
        
        // Register banner events
        RegisterBannerEvents();
        
        // Load the banner
        var adRequest = CreateAdRequest();
        
        // Add collapsible banner custom targeting if enabled
        if (enableCollapsibleBanners)
        {
            adRequest.Extras.Add("collapsible", "bottom"); // or "top"
        }
        
        bannerView.LoadAd(adRequest);
        
        Debug.Log($"Loading {(useAdaptiveBanners ? "adaptive" : "standard")} banner ad...");
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
        bannerView.OnBannerAdLoaded += () => {
            isBannerLoaded = true;
            Debug.Log("Banner ad loaded successfully");
            
            // Auto-show banner if it was meant to be visible
            if (isBannerVisible)
            {
                bannerView.Show();
            }
        };

        // Banner failed to load
        bannerView.OnBannerAdLoadFailed += (LoadAdError error) => {
            isBannerLoaded = false;
            Debug.LogError($"Banner ad failed to load: {error.GetMessage()}\nError Code: {error.GetCode()}\nCause: {error.GetCause()}");
            
            // Retry loading after a delay
            RetryLoadBanner();
        };

        // Banner revenue tracking
        bannerView.OnAdPaid += (AdValue adValue) => {
            Debug.Log($"Banner ad paid: {adValue.Value} {adValue.CurrencyCode}");
            // Here you can send revenue data to analytics services
            TrackAdRevenue("banner", adValue);
        };

        // Banner impression recorded
        bannerView.OnAdImpressionRecorded += () => {
            Debug.Log("Banner ad impression recorded");
        };

        // Banner clicked
        bannerView.OnAdClicked += () => {
            Debug.Log("Banner ad was clicked");
        };

        // Banner opened full screen content
        bannerView.OnAdFullScreenContentOpened += () => {
            Debug.Log("Banner ad opened full screen content");
            // Pause game logic if needed
            OnBannerFullScreenOpened();
        };

        // Banner closed full screen content
        bannerView.OnAdFullScreenContentClosed += () => {
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

    public void EnableCollapsibleBanners(bool enable)
    {
        if (enableCollapsibleBanners != enable)
        {
            enableCollapsibleBanners = enable;
            Debug.Log($"Collapsible banners {(enable ? "enabled" : "disabled")}");
            
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
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            isShowingAd = true;
            
            // Subscribe to events with cleanup
            System.Action onClosed = () => {
                isShowingAd = false;
                onSuccess?.Invoke();
            };
            System.Action<AdError> onFailed = (error) => {
                isShowingAd = false;
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
        if (!isInitialized) return;

        var adRequest = CreateAdRequest();
        RewardedAd.Load(REWARDED_ID, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogError($"Rewarded ad failed to load: {error}");
                RetryLoadRewarded();
                return;
            }

            rewardedAd = ad;
            rewardedRetryAttempt = 0;
            RegisterEventHandlers(rewardedAd);
            Debug.Log("Rewarded ad loaded successfully");
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
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            isShowingAd = true;
            
            System.Action onClosed = () => {
                isShowingAd = false;
                onSuccess?.Invoke();
            };
            System.Action<AdError> onFailed = (error) => {
                isShowingAd = false;
                onFailure?.Invoke();
            };
            
            rewardedAd.OnAdFullScreenContentClosed += onClosed;
            rewardedAd.OnAdFullScreenContentFailed += onFailed;
            
            rewardedAd.Show((reward) => {
                Debug.Log($"Rewarded ad granted reward: {reward.Amount} {reward.Type}");
                onRewarded?.Invoke(reward);
            });
        }
        else
        {
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
            
            System.Action onClosed = () => {
                isShowingAd = false;
                onSuccess?.Invoke();
            };
            System.Action<AdError> onFailed = (error) => {
                isShowingAd = false;
                onFailure?.Invoke();
            };
            
            rewardedInterstitialAd.OnAdFullScreenContentClosed += onClosed;
            rewardedInterstitialAd.OnAdFullScreenContentFailed += onFailed;
            
            rewardedInterstitialAd.Show((reward) => {
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
        if (isShowingAd || isAppOpenAdShowing)
        {
            onFailure?.Invoke();
            return;
        }

        if (IsAppOpenAdAvailable())
        {
            isShowingAd = true;
            isAppOpenAdShowing = true;
            
            System.Action onClosed = () => {
                isShowingAd = false;
                isAppOpenAdShowing = false;
                onSuccess?.Invoke();
            };
            System.Action<AdError> onFailed = (error) => {
                isShowingAd = false;
                isAppOpenAdShowing = false;
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
        var adRequest = new AdRequest();
        
        // Add any additional request parameters here
        // For example: keywords, content URL, etc.
        
        return adRequest;
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
}