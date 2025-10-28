using System;
using System.Threading.Tasks;
using UnityEngine;
using GoogleMobileAds.Api;

namespace Autech.Admob
{
    /// <summary>
    /// Handles banner ad lifecycle and management
    /// </summary>
    public class BannerAdController
    {
        private readonly AdConfiguration config;
        private BannerView bannerView;
        private AdPosition currentPosition = AdPosition.Bottom;
        private bool isBannerLoaded = false;
        private bool isBannerVisible = false;
        private int retryAttempt = 0;
        private const int maxRetryCount = 3;
        private System.Threading.CancellationTokenSource retryCts;
        private readonly object retryLock = new object();

        public bool IsBannerLoaded => isBannerLoaded && bannerView != null;
        public bool IsBannerVisible => isBannerVisible && IsBannerLoaded;
        public BannerPosition CurrentPosition => ConvertFromAdPosition(currentPosition);

        public event Action<AdValue> OnAdPaid;

        public BannerAdController(AdConfiguration configuration)
        {
            config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void LoadBanner()
        {
            Debug.Log("[BannerAdController] LoadBanner() started");

            if (config.RemoveAds)
            {
                Debug.Log("[BannerAdController] Skipped - RemoveAds enabled");
                return;
            }

            DestroyBanner();

            AdSize adSize = GetAdSize();
            Debug.Log($"[BannerAdController] Creating banner with size: {adSize.Width}x{adSize.Height}");

            bannerView = new BannerView(config.BannerId, adSize, currentPosition);
            RegisterBannerEvents();

            var adRequest = CreateAdRequest();

            if (config.EnableCollapsibleBanners)
            {
                string collapsibleEdge = GetCollapsibleEdgeForCurrentPosition();
                adRequest.Extras.Add("collapsible", collapsibleEdge);
                Debug.Log($"[BannerAdController] Collapsible banner enabled with position: {collapsibleEdge}");
            }

            bannerView.LoadAd(adRequest);
            Debug.Log("[BannerAdController] Loading banner ad...");
        }

        public void ShowBanner(bool show)
        {
            if (config.RemoveAds && show)
            {
                Debug.Log("[BannerAdController] Cannot show - RemoveAds enabled");
                return;
            }

            if (bannerView != null && isBannerLoaded)
            {
                if (show)
                {
                    bannerView.Show();
                    isBannerVisible = true;
                    Debug.Log("[BannerAdController] Banner displayed");
                }
                else
                {
                    bannerView.Hide();
                    isBannerVisible = false;
                    Debug.Log("[BannerAdController] Banner hidden");
                }
            }
            else if (show)
            {
                isBannerVisible = true;
                LoadBanner();
            }
            else
            {
                isBannerVisible = false;
            }
        }

        public void SetBannerPosition(BannerPosition position)
        {
            AdPosition newPosition = ConvertToAdPosition(position);
            if (currentPosition != newPosition)
            {
                currentPosition = newPosition;
                Debug.Log($"[BannerAdController] Position changed to: {position}");

                if (bannerView != null)
                {
                    bool wasVisible = isBannerVisible;
                    DestroyBanner();
                    LoadBanner();

                    if (wasVisible)
                    {
                        ShowBanner(true);
                    }
                }
            }
        }

        public void DestroyBanner()
        {
            // Cancel any ongoing retry operations
            CancelRetryCts();

            if (bannerView != null)
            {
                // Unregister event handlers before destroying
                UnregisterBannerEvents();

                bannerView.Destroy();
                bannerView = null;
                isBannerLoaded = false;
                isBannerVisible = false;
                Debug.Log("[BannerAdController] Banner destroyed and events unregistered");
            }
        }

        private void UnregisterBannerEvents()
        {
            if (bannerView == null) return;

            // Note: GoogleMobileAds SDK doesn't support unregistering individual event handlers
            // The events are cleared when the banner is destroyed
            Debug.Log("[BannerAdController] Banner events will be cleared on destroy");
        }

        public Vector2 GetBannerSize()
        {
            if (bannerView != null)
            {
                AdSize currentSize = GetAdSize();
                return new Vector2(currentSize.Width, currentSize.Height);
            }
            return Vector2.zero;
        }

        private AdSize GetAdSize()
        {
            if (config.UseAdaptiveBanners)
            {
                Debug.Log("[BannerAdController] Using adaptive banner");
                // Get the actual adaptive banner size
                return AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
            }

            return GetStandardAdSize();
        }

        private AdSize GetStandardAdSize()
        {
            switch (config.PreferredBannerSize)
            {
                case BannerSize.Banner:
                    return AdSize.Banner;
                case BannerSize.LargeBanner:
                    return new AdSize(320, 100);
                case BannerSize.MediumRectangle:
                    return AdSize.MediumRectangle;
                case BannerSize.FullBanner:
                    return new AdSize(468, 60);
                case BannerSize.Leaderboard:
                    return AdSize.Leaderboard;
                case BannerSize.SmartBanner:
                    Debug.LogWarning("[BannerAdController] Smart banners deprecated, using standard");
                    return AdSize.Banner;
                default:
                    return AdSize.Banner;
            }
        }

        private void RegisterBannerEvents()
        {
            if (bannerView == null) return;

            bannerView.OnBannerAdLoaded += () =>
            {
                isBannerLoaded = true;
                retryAttempt = 0;
                Debug.Log("[BannerAdController] Banner loaded successfully");

                if (isBannerVisible)
                {
                    bannerView.Show();
                }
            };

            bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
            {
                isBannerLoaded = false;
                Debug.LogError($"[BannerAdController] Failed to load: {error.GetMessage()}");
                // Fire-and-forget is safe here: RetryLoadBannerAsync has internal try-catch and error logging
                _ = RetryLoadBannerAsync();
            };

            bannerView.OnAdPaid += (AdValue adValue) =>
            {
                Debug.Log($"[BannerAdController] Ad paid: {adValue.Value} {adValue.CurrencyCode}");
                OnAdPaid?.Invoke(adValue);
            };

            bannerView.OnAdImpressionRecorded += () =>
            {
                Debug.Log("[BannerAdController] Impression recorded");
            };

            bannerView.OnAdClicked += () =>
            {
                Debug.Log("[BannerAdController] Ad clicked");
            };

            bannerView.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("[BannerAdController] Full screen opened");
                PauseGame();
            };

            bannerView.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[BannerAdController] Full screen closed");
                ResumeGame();
            };
        }

        private async Task RetryLoadBannerAsync()
        {
            var localCts = CreateRetryCts();

            try
            {
                retryAttempt++;
                // Fixed retry logic: allows exactly maxRetryCount retries (3 attempts)
                if (retryAttempt <= maxRetryCount)
                {
                    int delay = (int)(Math.Pow(2, retryAttempt) * 1000);
                    Debug.Log($"[BannerAdController] Retrying load in {delay}ms (attempt {retryAttempt}/{maxRetryCount})");
                    await Task.Delay(delay, localCts.Token);

                    if (!localCts.IsCancellationRequested)
                    {
                        LoadBanner();
                    }
                }
                else
                {
                    Debug.LogWarning($"[BannerAdController] Max retry count reached ({maxRetryCount})");
                }
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log("[BannerAdController] Retry operation was cancelled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BannerAdController] RetryLoadBannerAsync failed: {ex.Message}");
                Debug.LogException(ex);
            }
            finally
            {
                ReleaseRetryCts(localCts);
            }
        }

        private System.Threading.CancellationTokenSource CreateRetryCts()
        {
            lock (retryLock)
            {
                if (retryCts != null)
                {
                    retryCts.Cancel();
                    retryCts.Dispose();
                }

                retryCts = new System.Threading.CancellationTokenSource();
                return retryCts;
            }
        }

        private void CancelRetryCts()
        {
            System.Threading.CancellationTokenSource localCts = null;

            lock (retryLock)
            {
                if (retryCts != null)
                {
                    localCts = retryCts;
                    retryCts = null;
                }
            }

            if (localCts != null)
            {
                localCts.Cancel();
                localCts.Dispose();
            }
        }

        private void ReleaseRetryCts(System.Threading.CancellationTokenSource cts)
        {
            if (cts == null)
            {
                return;
            }

            lock (retryLock)
            {
                if (ReferenceEquals(retryCts, cts))
                {
                    retryCts = null;
                }
            }

            cts.Dispose();
        }

        private void PauseGame()
        {
            // Note: Modifying Time.timeScale globally can interfere with game logic
            // Consider using AudioListener.pause or a custom pause event instead
            // This is disabled by default - enable only if needed for your game
            // if (Time.timeScale == 1f)
            // {
            //     Time.timeScale = 0f;
            // }
            Debug.Log("[BannerAdController] Full screen content opened (pause disabled by default)");
        }

        private void ResumeGame()
        {
            // Note: Modifying Time.timeScale globally can interfere with game logic
            // Consider using AudioListener.pause or a custom pause event instead
            // This is disabled by default - enable only if needed for your game
            // if (Time.timeScale == 0f)
            // {
            //     Time.timeScale = 1f;
            // }
            Debug.Log("[BannerAdController] Full screen content closed (resume disabled by default)");
        }

        private AdRequest CreateAdRequest()
        {
            return new AdRequest();
        }

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

        private string GetCollapsibleEdgeForCurrentPosition()
        {
            switch (currentPosition)
            {
                case AdPosition.Top:
                case AdPosition.TopLeft:
                case AdPosition.TopRight:
                    return "top";
                default:
                    return "bottom";
            }
        }

        private AdPosition ConvertToAdPosition(BannerPosition position)
        {
            switch (position)
            {
                case BannerPosition.Top: return AdPosition.Top;
                case BannerPosition.Bottom: return AdPosition.Bottom;
                case BannerPosition.TopLeft: return AdPosition.TopLeft;
                case BannerPosition.TopRight: return AdPosition.TopRight;
                case BannerPosition.BottomLeft: return AdPosition.BottomLeft;
                case BannerPosition.BottomRight: return AdPosition.BottomRight;
                case BannerPosition.Center: return AdPosition.Center;
                default: return AdPosition.Bottom;
            }
        }
    }
}
