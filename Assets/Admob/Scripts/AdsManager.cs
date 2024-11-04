using UnityEngine;
using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using GoogleMobileAds.Mediation.UnityAds.Api;

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
    private const string BANNER_ID = "ca-app-pub-1768687954002835/9451348212";
    private const string INTERSTITIAL_ID = "ca-app-pub-xxx/yyy";
    private const string REWARDED_ID = "ca-app-pub-xxx/yyy";
    private const string REWARDED_INTERSTITIAL_ID = "ca-app-pub-xxx/yyy";
    private const string APP_OPEN_ID = "ca-app-pub-xxx/yyy";
#else
    private const string BANNER_ID = "";
    private const string INTERSTITIAL_ID = "";
    private const string REWARDED_ID = "";
    private const string REWARDED_INTERSTITIAL_ID = "";
    private const string APP_OPEN_ID = "";
#endif

    private BannerView bannerView;
    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;
    private RewardedInterstitialAd rewardedInterstitialAd;
    private AppOpenAd appOpenAd;
    private DateTime appOpenExpireTime;
    private bool isInitialized = false;
    private bool isShowingAd = false;
    private AdPosition currentBannerPosition = AdPosition.Bottom;

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

    private void InitializeAds()
    {
        InitializeAdMob();
        //RequestConsentInfo();
    }

    private void RequestConsentInfo()
    {
        var debugSettings = new ConsentDebugSettings
        {
            DebugGeography = DebugGeography.EEA
        };

        ConsentRequestParameters request = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = false,
            ConsentDebugSettings = Debug.isDebugBuild ? debugSettings : null
        };

        ConsentInformation.Update(request, OnConsentInfoUpdated);
    }

    private void OnConsentInfoUpdated(FormError error)
    {
        if (error != null)
        {
            Debug.LogError($"Consent Error: {error}");
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
                return;
            }

            if (ConsentInformation.ConsentStatus == ConsentStatus.Required)
            {
                consentForm.Show((showError) =>
                {
                    if (showError != null)
                    {
                        Debug.LogError($"Error showing consent form: {showError}");
                        return;
                    }

                    // Set Unity Ads consent after user's choice
                    if (ConsentInformation.ConsentStatus == ConsentStatus.Obtained)
                    {
                        UnityAds.SetConsentMetaData("gdpr.consent", true);
                        UnityAds.SetConsentMetaData("privacy.consent", true);
                    }
                    else
                    {
                        UnityAds.SetConsentMetaData("gdpr.consent", false);
                        UnityAds.SetConsentMetaData("privacy.consent", false);
                    }

                    InitializeAdMob();
                });
            }
            else
            {
                InitializeAdMob();
            }
        });
    }

    private void InitializeAdMob()
    {
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            isInitialized = true;
            LoadInterstitialAd();
            LoadRewardedAd();
            LoadRewardedInterstitialAd();
            LoadAppOpenAd();
            LoadBanner();
        });
    }

    #region Banner Ads
    public void LoadBanner()
    {
        if (!isInitialized) return;

        if (bannerView != null)
        {
            bannerView.Destroy();
        }

        bannerView = new BannerView(BANNER_ID, AdSize.Banner, currentBannerPosition);
    }

    public void ShowBanner(bool show)
    {
        if (bannerView != null)
        {
            if (show)
                bannerView.Show();
            else
                bannerView.Hide();
        }
    }


    public void SetBannerPosition(BannerPosition position)
    {
        AdPosition newPosition = ConvertToAdPosition(position);
        if (currentBannerPosition != newPosition)
        {
            currentBannerPosition = newPosition;
            // Reload banner with new position if it exists
            if (bannerView != null)
            {
                bannerView.Destroy();
                LoadBanner();
            }
        }
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

        var adRequest = new AdRequest();
        InterstitialAd.Load(INTERSTITIAL_ID, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogError($"Interstitial ad failed to load: {error}");
                return;
            }

            interstitialAd = ad;
            //RegisterInterstitialEvents();
        });
    }

    private void RegisterInterstitialEvents()
    {

        LoadInterstitialAd();

    }

    public void ShowInterstitial(Action onSuccess, Action onFailure)
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                onSuccess?.Invoke();
            };
            interstitialAd.OnAdFullScreenContentFailed += (error) =>
            {
                onFailure?.Invoke();
            };
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
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                onSuccess?.Invoke();
            };
            interstitialAd.Show();
        }
        else
        {
            LoadInterstitialAd();
        }
    }

    public void ShowInterstitial()
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
        }
        else
        {
            LoadInterstitialAd();
        }
    }
    #endregion

    #region Rewarded Ads
    private void LoadRewardedAd()
    {
        if (!isInitialized) return;

        var adRequest = new AdRequest();
        RewardedAd.Load(REWARDED_ID, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogError($"Rewarded ad failed to load: {error}");
                return;
            }

            rewardedAd = ad;

        });
    }

    private void RegisterRewardedEvents()
    {

        LoadRewardedAd();

    }

    public void ShowRewarded(Action onSuccess, Action onFailure)
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.OnAdFullScreenContentClosed += () =>
            {
                onSuccess?.Invoke();
            };
            rewardedAd.OnAdFullScreenContentFailed += (error) =>
            {
                onFailure?.Invoke();
            };
            rewardedAd.Show((reward) => { });
        }
        else
        {
            onFailure?.Invoke();
            LoadRewardedAd();
        }
    }

    public void ShowRewarded(Action onSuccess)
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.OnAdFullScreenContentClosed += () =>
            {
                onSuccess?.Invoke();
            };
            rewardedAd.Show((reward) => { });
        }
        else
        {
            LoadRewardedAd();
        }
    }

    public void ShowRewarded()
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show((reward) => { });
        }
        else
        {
            LoadRewardedAd();
        }
    }
    #endregion

    #region Rewarded Interstitial Ads
    private void LoadRewardedInterstitialAd()
    {
        if (!isInitialized) return;

        var adRequest = new AdRequest();
        RewardedInterstitialAd.Load(REWARDED_INTERSTITIAL_ID, adRequest,
            (RewardedInterstitialAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogError($"Rewarded interstitial ad failed to load: {error}");
                return;
            }

            rewardedInterstitialAd = ad;
        });
    }

    private void RegisterRewardedInterstitialEvents()
    {

        LoadRewardedInterstitialAd();

    }

    public void ShowRewardedInterstitial(Action onSuccess, Action onFailure)
    {
        if (rewardedInterstitialAd != null)
        {
            rewardedInterstitialAd.OnAdFullScreenContentClosed += () =>
            {
                onSuccess?.Invoke();
            };
            rewardedInterstitialAd.OnAdFullScreenContentFailed += (error) =>
            {
                onFailure?.Invoke();
            };
            rewardedInterstitialAd.Show((reward) => { });
        }
        else
        {
            onFailure?.Invoke();
            LoadRewardedInterstitialAd();
        }
    }

    public void ShowRewardedInterstitial(Action onSuccess)
    {
        if (rewardedInterstitialAd != null)
        {
            rewardedInterstitialAd.OnAdFullScreenContentClosed += () =>
            {
                onSuccess?.Invoke();
            };
            rewardedInterstitialAd.Show((reward) => { });
        }
        else
        {
            LoadRewardedInterstitialAd();
        }
    }

    public void ShowRewardedInterstitial()
    {
        if (rewardedInterstitialAd != null)
        {
            rewardedInterstitialAd.Show((reward) => { });
        }
        else
        {
            LoadRewardedInterstitialAd();
        }
    }
    #endregion

    #region App Open Ads
    private void LoadAppOpenAd()
    {
        if (!isInitialized) return;

        var adRequest = new AdRequest();
        AppOpenAd.Load(APP_OPEN_ID, adRequest,
            (AppOpenAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogError($"App open ad failed to load: {error}");
                return;
            }

            appOpenAd = ad;
            appOpenExpireTime = DateTime.Now + TimeSpan.FromHours(4);
            
        });
    }

    private void RegisterAppOpenEvents()
    {

        isShowingAd = false;
        LoadAppOpenAd();

    }

    public void ShowAppOpenAd(Action onSuccess, Action onFailure)
    {
        if (isShowingAd)
        {
            onFailure?.Invoke();
            return;
        }

        if (appOpenAd != null && DateTime.Now < appOpenExpireTime)
        {
            isShowingAd = true;
            appOpenAd.OnAdFullScreenContentClosed += () =>
            {
                onSuccess?.Invoke();
            };
            appOpenAd.OnAdFullScreenContentFailed += (error) =>
            {
                isShowingAd = false;
                onFailure?.Invoke();
            };
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
        if (isShowingAd) return;

        if (appOpenAd != null && DateTime.Now < appOpenExpireTime)
        {
            isShowingAd = true;
            appOpenAd.OnAdFullScreenContentClosed += () =>
            {
                onSuccess?.Invoke();
            };
            appOpenAd.Show();
        }
        else
        {
            LoadAppOpenAd();
        }
    }

    public void ShowAppOpenAd()
    {
        if (isShowingAd) return;

        if (appOpenAd != null && DateTime.Now < appOpenExpireTime)
        {
            isShowingAd = true;
            appOpenAd.Show();
        }
        else
        {
            LoadAppOpenAd();
        }
    }
    #endregion


    #region CallBacks

    private void RegisterEventHandlers(AppOpenAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {

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
        };
    }



    #endregion

    private void OnDestroy()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
        }

        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
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