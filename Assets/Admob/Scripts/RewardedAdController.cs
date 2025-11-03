#if ADMOB_INSTALLED
using System;
using System.Threading.Tasks;
using UnityEngine;
using GoogleMobileAds.Api;

namespace Autech.Admob
{
    /// <summary>
    /// Handles rewarded ad lifecycle and management with concurrency-safe retry logic.
    /// </summary>
    public class RewardedAdController
    {
        private readonly AdConfiguration config;
        private RewardedAd rewardedAd;
        private int retryAttempt = 0;
        private const int maxRetryCount = 3;
        private System.Threading.CancellationTokenSource retryCts;
        private readonly object retryLock = new object();
        private readonly object showLock = new object();
        private bool isShowing = false;
        private bool destroyPending = false;

        public bool IsReady => rewardedAd != null && rewardedAd.CanShowAd();

        public event Action<AdValue> OnAdPaid;

        public RewardedAdController(AdConfiguration configuration)
        {
            config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void LoadAd()
        {
            Debug.Log("[RewardedAdController] LoadAd() started");

            CancelRetryCts();

            if (rewardedAd != null)
            {
                UnregisterEventHandlers(rewardedAd);
                rewardedAd.Destroy();
                rewardedAd = null;
            }

            var adRequest = new AdRequest();
            RewardedAd.Load(config.RewardedId, adRequest, OnAdLoadCallback);
        }

        private void OnAdLoadCallback(RewardedAd ad, LoadAdError error)
        {
            if (error != null)
            {
                Debug.LogError($"[RewardedAdController] Failed to load: {error}");
                _ = RetryLoadAsync();
                return;
            }

            if (rewardedAd != null)
            {
                UnregisterEventHandlers(rewardedAd);
                rewardedAd.Destroy();
            }

            rewardedAd = ad;
            retryAttempt = 0;
            destroyPending = false;
            RegisterEventHandlers(ad);

            Debug.Log("[RewardedAdController] Ad loaded successfully");
        }

        public void Show(Action<Reward> onRewarded = null, Action onSuccess = null, Action onFailure = null)
        {
            Debug.Log("[RewardedAdController] Show() called");

            RewardedAd adToShow;
            bool canShow;

            lock (showLock)
            {
                if (isShowing)
                {
                    Debug.LogWarning("[RewardedAdController] Ad already showing - preventing concurrent Show() call");
                    onFailure?.Invoke();
                    return;
                }

                adToShow = rewardedAd;
                canShow = adToShow != null && adToShow.CanShowAd();

                if (canShow)
                {
                    isShowing = true;
                }
            }

            if (!canShow)
            {
                Debug.LogWarning("[RewardedAdController] Ad not ready");
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

                Debug.Log("[RewardedAdController] Ad closed");

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

                Debug.LogError($"[RewardedAdController] Failed to show: {error}");

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
                    Debug.Log($"[RewardedAdController] Reward granted: {reward.Amount} {reward.Type}");
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

                Debug.LogError($"[RewardedAdController] Exception during Show(): {ex.Message}");
                Debug.LogException(ex);

                onFailure?.Invoke();
                LoadAd();
            }
        }

        public void Destroy()
        {
            CancelRetryCts();

            RewardedAd adToDispose = null;

            lock (showLock)
            {
                if (isShowing && rewardedAd != null)
                {
                    Debug.LogWarning("[RewardedAdController] Cannot destroy immediately - ad is showing. Deferring destroy until close.");
                    destroyPending = true;
                    return;
                }

                adToDispose = rewardedAd;
                rewardedAd = null;
                isShowing = false;
                destroyPending = false;
            }

            if (adToDispose != null)
            {
                UnregisterEventHandlers(adToDispose);
                adToDispose.Destroy();
                Debug.Log("[RewardedAdController] Ad destroyed and events cleared");
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
                    Debug.Log($"[RewardedAdController] Retrying load in {delay}ms (attempt {retryAttempt}/{maxRetryCount})");
                    await Task.Delay(delay, localCts.Token);

                    if (!localCts.IsCancellationRequested)
                    {
                        LoadAd();
                    }
                }
                else
                {
                    Debug.LogWarning($"[RewardedAdController] Max retry count reached ({maxRetryCount})");
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[RewardedAdController] Retry operation was cancelled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RewardedAdController] RetryLoadAsync failed: {ex.Message}");
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

        private void RegisterEventHandlers(RewardedAd ad)
        {
            ad.OnAdPaid += OnAdPaidInternal;
            ad.OnAdImpressionRecorded += OnAdImpressionRecordedInternal;
            ad.OnAdClicked += OnAdClickedInternal;
            ad.OnAdFullScreenContentOpened += OnAdOpenedInternal;
        }

        private void UnregisterEventHandlers(RewardedAd ad)
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
            Debug.Log($"[RewardedAdController] Ad paid: {adValue.Value} {adValue.CurrencyCode}");
            OnAdPaid?.Invoke(adValue);
        }

        private void OnAdImpressionRecordedInternal()
        {
            Debug.Log("[RewardedAdController] Impression recorded");
        }

        private void OnAdClickedInternal()
        {
            Debug.Log("[RewardedAdController] Ad clicked");
        }

        private void OnAdOpenedInternal()
        {
            Debug.Log("[RewardedAdController] Full screen opened");
        }
    }
}
#endif // ADMOB_INSTALLED
