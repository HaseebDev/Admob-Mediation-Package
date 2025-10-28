using System;
using System.Threading.Tasks;
using UnityEngine;
using GoogleMobileAds.Api;

namespace Autech.Admob
{
    /// <summary>
    /// Handles rewarded interstitial ad lifecycle and management with safe retry handling.
    /// </summary>
    public class RewardedInterstitialAdController
    {
        private readonly AdConfiguration config;
        private RewardedInterstitialAd rewardedInterstitialAd;
        private int retryAttempt = 0;
        private const int maxRetryCount = 3;
        private System.Threading.CancellationTokenSource retryCts;
        private readonly object retryLock = new object();
        private readonly object showLock = new object();
        private bool isShowing = false;
        private bool destroyPending = false;

        public bool IsReady => rewardedInterstitialAd != null && rewardedInterstitialAd.CanShowAd();

        public event Action<AdValue> OnAdPaid;

        public RewardedInterstitialAdController(AdConfiguration configuration)
        {
            config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void LoadAd()
        {
            Debug.Log("[RewardedInterstitialAdController] LoadAd() started");

            if (config.RemoveAds)
            {
                Debug.Log("[RewardedInterstitialAdController] Skipped - RemoveAds enabled");
                return;
            }

            CancelRetryCts();

            if (rewardedInterstitialAd != null)
            {
                UnregisterEventHandlers(rewardedInterstitialAd);
                rewardedInterstitialAd.Destroy();
                rewardedInterstitialAd = null;
            }

            var adRequest = new AdRequest();
            RewardedInterstitialAd.Load(config.RewardedInterstitialId, adRequest, OnAdLoadCallback);
        }

        private void OnAdLoadCallback(RewardedInterstitialAd ad, LoadAdError error)
        {
            if (error != null)
            {
                Debug.LogError($"[RewardedInterstitialAdController] Failed to load: {error}");
                _ = RetryLoadAsync();
                return;
            }

            if (rewardedInterstitialAd != null)
            {
                UnregisterEventHandlers(rewardedInterstitialAd);
                rewardedInterstitialAd.Destroy();
            }

            rewardedInterstitialAd = ad;
            retryAttempt = 0;
            destroyPending = false;
            RegisterEventHandlers(ad);
            Debug.Log("[RewardedInterstitialAdController] Ad loaded successfully");
        }

        public void Show(Action<Reward> onRewarded = null, Action onSuccess = null, Action onFailure = null)
        {
            Debug.Log("[RewardedInterstitialAdController] Show() called");

            if (config.RemoveAds)
            {
                Debug.Log("[RewardedInterstitialAdController] Cannot show - RemoveAds enabled");
                onFailure?.Invoke();
                return;
            }

            RewardedInterstitialAd adToShow;
            bool canShow;

            lock (showLock)
            {
                if (isShowing)
                {
                    Debug.LogWarning("[RewardedInterstitialAdController] Ad already showing - preventing concurrent Show() call");
                    onFailure?.Invoke();
                    return;
                }

                adToShow = rewardedInterstitialAd;
                canShow = adToShow != null && adToShow.CanShowAd();

                if (canShow)
                {
                    isShowing = true;
                }
            }

            if (!canShow)
            {
                Debug.LogWarning("[RewardedInterstitialAdController] Ad not ready");
                onFailure?.Invoke();
                LoadAd();
                return;
            }

            destroyPending = false;

            Action onClosed = null;
            Action<AdError> onFailed = null;

            onClosed = () =>
            {
                adToShow.OnAdFullScreenContentClosed -= onClosed;
                adToShow.OnAdFullScreenContentFailed -= onFailed;

                lock (showLock)
                {
                    isShowing = false;
                }

                Debug.Log("[RewardedInterstitialAdController] Ad closed");

                if (destroyPending)
                {
                    Destroy();
                }
                else
                {
                    LoadAd();
                }

                onSuccess?.Invoke();
            };

            onFailed = (error) =>
            {
                adToShow.OnAdFullScreenContentClosed -= onClosed;
                adToShow.OnAdFullScreenContentFailed -= onFailed;

                lock (showLock)
                {
                    isShowing = false;
                }

                Debug.LogError($"[RewardedInterstitialAdController] Failed to show: {error}");

                if (destroyPending)
                {
                    Destroy();
                }
                else
                {
                    LoadAd();
                }

                onFailure?.Invoke();
            };

            adToShow.OnAdFullScreenContentClosed += onClosed;
            adToShow.OnAdFullScreenContentFailed += onFailed;

            try
            {
                adToShow.Show(reward =>
                {
                    Debug.Log($"[RewardedInterstitialAdController] Reward granted: {reward.Amount} {reward.Type}");
                    onRewarded?.Invoke(reward);
                });
            }
            catch (Exception ex)
            {
                adToShow.OnAdFullScreenContentClosed -= onClosed;
                adToShow.OnAdFullScreenContentFailed -= onFailed;

                lock (showLock)
                {
                    isShowing = false;
                }

                Debug.LogError($"[RewardedInterstitialAdController] Exception during Show(): {ex.Message}");
                Debug.LogException(ex);

                onFailure?.Invoke();
                LoadAd();
            }
        }

        public void Destroy()
        {
            CancelRetryCts();

            RewardedInterstitialAd adToDispose = null;

            lock (showLock)
            {
                if (isShowing && rewardedInterstitialAd != null)
                {
                    Debug.LogWarning("[RewardedInterstitialAdController] Cannot destroy immediately - ad is showing. Deferring destroy until close.");
                    destroyPending = true;
                    return;
                }

                adToDispose = rewardedInterstitialAd;
                rewardedInterstitialAd = null;
                isShowing = false;
                destroyPending = false;
            }

            if (adToDispose != null)
            {
                UnregisterEventHandlers(adToDispose);
                adToDispose.Destroy();
                Debug.Log("[RewardedInterstitialAdController] Ad destroyed and events cleared");
            }
        }

        private async Task RetryLoadAsync()
        {
            var localCts = CreateRetryCts();

            try
            {
                retryAttempt++;
                // Exponential backoff up to maxRetryCount attempts.
                if (retryAttempt <= maxRetryCount)
                {
                    int delay = (int)(Math.Pow(2, retryAttempt) * 1000);
                    Debug.Log($"[RewardedInterstitialAdController] Retrying load in {delay}ms (attempt {retryAttempt}/{maxRetryCount})");
                    await Task.Delay(delay, localCts.Token);

                    if (!localCts.IsCancellationRequested)
                    {
                        LoadAd();
                    }
                }
                else
                {
                    Debug.LogWarning($"[RewardedInterstitialAdController] Max retry count reached ({maxRetryCount})");
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[RewardedInterstitialAdController] Retry operation was cancelled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RewardedInterstitialAdController] RetryLoadAsync failed: {ex.Message}");
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

        private void RegisterEventHandlers(RewardedInterstitialAd ad)
        {
            ad.OnAdPaid += OnAdPaidInternal;
            ad.OnAdImpressionRecorded += OnAdImpressionRecordedInternal;
            ad.OnAdClicked += OnAdClickedInternal;
            ad.OnAdFullScreenContentOpened += OnAdOpenedInternal;
        }

        private void UnregisterEventHandlers(RewardedInterstitialAd ad)
        {
            if (ad == null)
            {
                return;
            }

            ad.OnAdPaid -= OnAdPaidInternal;
            ad.OnAdImpressionRecorded -= OnAdImpressionRecordedInternal;
            ad.OnAdClicked -= OnAdClickedInternal;
            ad.OnAdFullScreenContentOpened -= OnAdOpenedInternal;
        }

        private void OnAdPaidInternal(AdValue adValue)
        {
            Debug.Log($"[RewardedInterstitialAdController] Ad paid: {adValue.Value} {adValue.CurrencyCode}");
            OnAdPaid?.Invoke(adValue);
        }

        private void OnAdImpressionRecordedInternal()
        {
            Debug.Log("[RewardedInterstitialAdController] Impression recorded");
        }

        private void OnAdClickedInternal()
        {
            Debug.Log("[RewardedInterstitialAdController] Ad clicked");
        }

        private void OnAdOpenedInternal()
        {
            Debug.Log("[RewardedInterstitialAdController] Full screen opened");
        }
    }
}
