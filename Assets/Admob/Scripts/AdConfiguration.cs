#if ADMOB_INSTALLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Autech.Admob
{
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

    /// <summary>
    /// Manages ad unit IDs and configuration settings for the ads system
    /// </summary>
    public class AdConfiguration
    {
        private const string GoogleTestPublisherId = "ca-app-pub-3940256099942544";

        private const string DefaultAndroidBannerId = "ca-app-pub-3940256099942544/6300978111";
        private const string DefaultAndroidInterstitialId = "ca-app-pub-3940256099942544/1033173712";
        private const string DefaultAndroidRewardedId = "ca-app-pub-3940256099942544/5224354917";
        private const string DefaultAndroidRewardedInterstitialId = "ca-app-pub-3940256099942544/5354046379";
        private const string DefaultAndroidAppOpenId = "ca-app-pub-3940256099942544/9257395921";

        private const string DefaultIosBannerId = "ca-app-pub-3940256099942544/2934735716";
        private const string DefaultIosInterstitialId = "ca-app-pub-3940256099942544/4411468910";
        private const string DefaultIosRewardedId = "ca-app-pub-3940256099942544/1712485313";
        private const string DefaultIosRewardedInterstitialId = "ca-app-pub-3940256099942544/6978759866";
        private const string DefaultIosAppOpenId = "ca-app-pub-3940256099942544/5575463023";

        private static readonly HashSet<string> KnownGoogleTestIds = new HashSet<string>(StringComparer.Ordinal)
        {
            DefaultAndroidBannerId,
            DefaultAndroidInterstitialId,
            DefaultAndroidRewardedId,
            DefaultAndroidRewardedInterstitialId,
            DefaultAndroidAppOpenId,
            DefaultIosBannerId,
            DefaultIosInterstitialId,
            DefaultIosRewardedId,
            DefaultIosRewardedInterstitialId,
            DefaultIosAppOpenId
        };

        // Ad Unit IDs - Android
        public string AndroidBannerId { get; set; } = DefaultAndroidBannerId;
        public string AndroidInterstitialId { get; set; } = DefaultAndroidInterstitialId;
        public string AndroidRewardedId { get; set; } = DefaultAndroidRewardedId;
        public string AndroidRewardedInterstitialId { get; set; } = DefaultAndroidRewardedInterstitialId;
        public string AndroidAppOpenId { get; set; } = DefaultAndroidAppOpenId;

        // Ad Unit IDs - iOS
        public string IosBannerId { get; set; } = DefaultIosBannerId;
        public string IosInterstitialId { get; set; } = DefaultIosInterstitialId;
        public string IosRewardedId { get; set; } = DefaultIosRewardedId;
        public string IosRewardedInterstitialId { get; set; } = DefaultIosRewardedInterstitialId;
        public string IosAppOpenId { get; set; } = DefaultIosAppOpenId;

        // Current platform Ad Unit IDs
        public string BannerId => GetCurrentPlatformAdId(AndroidBannerId, IosBannerId);
        public string InterstitialId => GetCurrentPlatformAdId(AndroidInterstitialId, IosInterstitialId);
        public string RewardedId => GetCurrentPlatformAdId(AndroidRewardedId, IosRewardedId);
        public string RewardedInterstitialId => GetCurrentPlatformAdId(AndroidRewardedInterstitialId, IosRewardedInterstitialId);
        public string AppOpenId => GetCurrentPlatformAdId(AndroidAppOpenId, IosAppOpenId);

        // Settings
        public bool AutoShowAppOpenAds { get; set; } = true;

        /// <summary>
        /// PRODUCTION WARNING: Test ads are ENABLED by default for development convenience.
        /// Set to FALSE before publishing to production or configure via VerifyAdmob prefab.
        /// Test ads use Google's demo ad unit IDs and won't generate real revenue.
        /// </summary>
        public bool EnableTestAds { get; set; } = true;

        public float AppOpenCooldownTime { get; set; } = 4f;
        public bool RemoveAds { get; set; } = false;

        // Banner Settings
        public bool UseAdaptiveBanners { get; set; } = false;
        public bool EnableCollapsibleBanners { get; set; } = false;
        public BannerSize PreferredBannerSize { get; set; } = BannerSize.Banner;

        private string GetCurrentPlatformAdId(string androidId, string iosId)
        {
#if UNITY_ANDROID
            return androidId;
#elif UNITY_IOS
            return iosId;
#else
            // Unity Editor: Use Android test IDs for testing
            return androidId;
#endif
        }

        public void SetAndroidAdIds(string bannerId, string interstitialId, string rewardedId, string rewardedInterstitialId, string appOpenId)
        {
            AndroidBannerId = bannerId;
            AndroidInterstitialId = interstitialId;
            AndroidRewardedId = rewardedId;
            AndroidRewardedInterstitialId = rewardedInterstitialId;
            AndroidAppOpenId = appOpenId;
            Debug.Log("[AdConfiguration] Android Ad Unit IDs updated");
        }

        public void SetIosAdIds(string bannerId, string interstitialId, string rewardedId, string rewardedInterstitialId, string appOpenId)
        {
            IosBannerId = bannerId;
            IosInterstitialId = interstitialId;
            IosRewardedId = rewardedId;
            IosRewardedInterstitialId = rewardedInterstitialId;
            IosAppOpenId = appOpenId;
            Debug.Log("[AdConfiguration] iOS Ad Unit IDs updated");
        }

        public void SetAllAdIds(
            string androidBanner, string androidInterstitial, string androidRewarded, string androidRewardedInterstitial, string androidAppOpen,
            string iosBanner, string iosInterstitial, string iosRewarded, string iosRewardedInterstitial, string iosAppOpen)
        {
            SetAndroidAdIds(androidBanner, androidInterstitial, androidRewarded, androidRewardedInterstitial, androidAppOpen);
            SetIosAdIds(iosBanner, iosInterstitial, iosRewarded, iosRewardedInterstitial, iosAppOpen);
            Debug.Log("[AdConfiguration] All Ad Unit IDs updated for both platforms");
        }

        public void LogCurrentAdIds()
        {
            Debug.Log($"[AdConfiguration] Current Platform Ad IDs:");
            Debug.Log($"Banner: {BannerId}");
            Debug.Log($"Interstitial: {InterstitialId}");
            Debug.Log($"Rewarded: {RewardedId}");
            Debug.Log($"Rewarded Interstitial: {RewardedInterstitialId}");
            Debug.Log($"App Open: {AppOpenId}");
        }

        public bool AreAdIdsValid()
        {
            return !string.IsNullOrEmpty(BannerId) &&
                   !string.IsNullOrEmpty(InterstitialId) &&
                   !string.IsNullOrEmpty(RewardedId) &&
                   !string.IsNullOrEmpty(RewardedInterstitialId) &&
                   !string.IsNullOrEmpty(AppOpenId);
        }

        public bool AreTestAdIds()
        {
            return IsGoogleTestId(BannerId) ||
                   IsGoogleTestId(InterstitialId) ||
                   IsGoogleTestId(RewardedId) ||
                   IsGoogleTestId(RewardedInterstitialId) ||
                   IsGoogleTestId(AppOpenId);
        }

        public bool AreDefaultTestAdIdsConfiguredForAllPlatforms()
        {
            return AndroidBannerId == DefaultAndroidBannerId &&
                   AndroidInterstitialId == DefaultAndroidInterstitialId &&
                   AndroidRewardedId == DefaultAndroidRewardedId &&
                   AndroidRewardedInterstitialId == DefaultAndroidRewardedInterstitialId &&
                   AndroidAppOpenId == DefaultAndroidAppOpenId &&
                   IosBannerId == DefaultIosBannerId &&
                   IosInterstitialId == DefaultIosInterstitialId &&
                   IosRewardedId == DefaultIosRewardedId &&
                   IosRewardedInterstitialId == DefaultIosRewardedInterstitialId &&
                   IosAppOpenId == DefaultIosAppOpenId;
        }

        public void WarnIfUsingTestIds()
        {
            if (!AreTestAdIds())
            {
                return;
            }

            bool isProductionRuntime = !Application.isEditor && !Debug.isDebugBuild;

            if (!EnableTestAds && isProductionRuntime)
            {
                Debug.LogError("[AdConfiguration] PRODUCTION build is configured with Google test Ad Unit IDs. Replace them with live IDs before release.");
            }
            else
            {
                Debug.LogWarning("[AdConfiguration] Google test Ad Unit IDs detected. Remember to swap these for production before publishing.");
            }
        }

        private static bool IsGoogleTestId(string adUnitId)
        {
            if (string.IsNullOrEmpty(adUnitId))
            {
                return false;
            }

            if (adUnitId.StartsWith(GoogleTestPublisherId, StringComparison.Ordinal))
            {
                return true;
            }

            return KnownGoogleTestIds.Contains(adUnitId);
        }
    }
}
#endif // ADMOB_INSTALLED
