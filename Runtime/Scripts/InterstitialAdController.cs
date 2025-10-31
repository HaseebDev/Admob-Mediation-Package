using System;
using System.Threading.Tasks;
using UnityEngine;
using GoogleMobileAds.Api;

namespace Autech.Admob
{
    /// <summary>
    /// Handles interstitial ad lifecycle and management with concurrency-safe retry logic.
    /// </summary>
    public class InterstitialAdController
    {
        private readonly AdConfiguration config;
        private InterstitialAd interstitialAd;
        private int retryAttempt = 0;
        private const int maxRetryCount = 3;
        private System.Threading.CancellationTokenSource retryCts;
        private readonly object retryLock = new object();
        private readonly object showingLock = new object();
        private bool isShowing = false;
        private bool destroyPending = false;

        public bool IsReady => interstitialAd != null && interstitialAd.CanShowAd();

        public event Action<AdValue> OnAdPaid;

        public InterstitialAdController(AdConfiguration configuration)
        {
            config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void LoadAd()
        {
            Debug.Log("[InterstitialAdController] LoadAd() started");

            CancelRetryCts();

            if (config.RemoveAds)
            {
                Debug.Log("[InterstitialAdController] Skipped - RemoveAds enabled");
                return;
            }

            InterstitialAd adToDispose = null;

            lock (showingLock)
            {
                if (interstitialAd != null)
                {
                    adToDispose = interstitialAd;
                    interstitialAd = null;
                }

                destroyPending = false;
            }

            if (adToDispose != null)
            {
                UnregisterEventHandlers(adToDispose);
                adToDispose.Destroy();
                Debug.Log("[InterstitialAdController] Previous ad destroyed before loading new one");
            }

            var adRequest = new AdRequest();
            InterstitialAd.Load(config.InterstitialId, adRequest, OnAdLoadCallback);
        }

        private void OnAdLoadCallback(InterstitialAd ad, LoadAdError error)
        {
            if (error != null)
            {
                Debug.LogError($"[InterstitialAdController] Failed to load: {error}");
                _ = RetryLoadAsync();
                return;
            }

            lock (showingLock)
            {
                interstitialAd = ad;
                destroyPending = false;
            }

            retryAttempt = 0;
            RegisterEventHandlers(ad);
            Debug.Log("[InterstitialAdController] Ad loaded successfully");
        }

        public void Show(Action onSuccess = null, Action onFailure = null)
        {
            Debug.Log("[InterstitialAdController] Show() called");

            if (config.RemoveAds)
            {
                Debug.Log("[InterstitialAdController] Cannot show - RemoveAds enabled");
                onFailure?.Invoke();
                return;
            }

            InterstitialAd adToShow;

            lock (showingLock)
            {
                if (isShowing)
                {
                    Debug.LogWarning("[InterstitialAdController] Ad already showing - preventing concurrent Show() call");
                    onFailure?.Invoke();
                    return;
                }

                adToShow = interstitialAd;
                if (adToShow != null && adToShow.CanShowAd())
                {
                    isShowing = true;
                }
                else
                {
                    Debug.LogWarning("[InterstitialAdController] Ad not ready");
                    onFailure?.Invoke();
                    LoadAd();
                    return;
                }
            }

            Action onClosed = null;
            Action<AdError> onFailed = null;

            onClosed = () =>
            {
                adToShow.OnAdFullScreenContentClosed -= onClosed;
                adToShow.OnAdFullScreenContentFailed -= onFailed;

                bool shouldDestroy;
                lock (showingLock)
                {
                    isShowing = false;
                    shouldDestroy = destroyPending;
                    destroyPending = false;
                }

                Debug.Log("[InterstitialAdController] Ad closed");

                if (shouldDestroy)
                {
                    ClearManagedAdReference(adToShow);
                    FinalizeDestroy(adToShow);
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

                bool shouldDestroy;
                lock (showingLock)
                {
                    isShowing = false;
                    shouldDestroy = destroyPending;
                    destroyPending = false;
                }

                Debug.LogError($"[InterstitialAdController] Failed to show: {error}");

                if (shouldDestroy)
                {
                    ClearManagedAdReference(adToShow);
                    FinalizeDestroy(adToShow);
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
                adToShow.Show();
            }
            catch (Exception ex)
            {
                adToShow.OnAdFullScreenContentClosed -= onClosed;
                adToShow.OnAdFullScreenContentFailed -= onFailed;

                bool shouldDestroy;
                lock (showingLock)
                {
                    isShowing = false;
                    shouldDestroy = destroyPending;
                    destroyPending = false;
                }

                Debug.LogError($"[InterstitialAdController] Exception during Show(): {ex.Message}");
                Debug.LogException(ex);

                if (shouldDestroy)
                {
                    ClearManagedAdReference(adToShow);
                    FinalizeDestroy(adToShow);
                }
                else
                {
                    LoadAd();
                }

                onFailure?.Invoke();
            }
        }

        public void Destroy()
        {
            CancelRetryCts();

            InterstitialAd adToDispose = null;

            lock (showingLock)
            {
                if (isShowing && interstitialAd != null)
                {
                    Debug.LogWarning("[InterstitialAdController] Cannot destroy immediately - ad is showing. Deferring destroy until close.");
                    destroyPending = true;
                    return;
                }

                adToDispose = interstitialAd;
                interstitialAd = null;
                isShowing = false;
                destroyPending = false;
            }

            if (adToDispose != null)
            {
                FinalizeDestroy(adToDispose);
            }
        }

        private void FinalizeDestroy(InterstitialAd ad)
        {
            if (ad == null)
            {
                return;
            }

            UnregisterEventHandlers(ad);
            ad.Destroy();
            Debug.Log("[InterstitialAdController] Ad destroyed and events cleared");
        }

        private void ClearManagedAdReference(InterstitialAd ad)
        {
            if (ad == null)
            {
                return;
            }

            lock (showingLock)
            {
                if (ReferenceEquals(interstitialAd, ad))
                {
                    interstitialAd = null;
                }
            }
        }

        private async Task RetryLoadAsync()
        {
            var localCts = CreateRetryCts();

            try
            {
                retryAttempt++;
                if (retryAttempt <= maxRetryCount)
                {
                    int delay = (int)(Math.Pow(2, retryAttempt) * 1000);
                    Debug.Log($"[InterstitialAdController] Retrying load in {delay}ms (attempt {retryAttempt}/{maxRetryCount})");
                    await Task.Delay(delay, localCts.Token);

                    if (!localCts.IsCancellationRequested)
                    {
                        LoadAd();
                    }
                }
                else
                {
                    Debug.LogWarning($"[InterstitialAdController] Max retry count reached ({maxRetryCount})");
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[InterstitialAdController] Retry operation was cancelled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InterstitialAdController] RetryLoadAsync failed: {ex.Message}");
                Debug.LogException(ex);
            }
            finally
            {
                ReleaseRetryCts(localCts);
            }
        }

        private void RegisterEventHandlers(InterstitialAd ad)
        {
            ad.OnAdPaid += OnAdPaidHandler;
            ad.OnAdImpressionRecorded += OnAdImpressionRecordedHandler;
            ad.OnAdClicked += OnAdClickedHandler;
            ad.OnAdFullScreenContentOpened += OnAdOpenedHandler;
        }

        private void UnregisterEventHandlers(InterstitialAd ad)
        {
            if (ad == null)
            {
                return;
            }

            ad.OnAdPaid -= OnAdPaidHandler;
            ad.OnAdImpressionRecorded -= OnAdImpressionRecordedHandler;
            ad.OnAdClicked -= OnAdClickedHandler;
            ad.OnAdFullScreenContentOpened -= OnAdOpenedHandler;
        }

        private void OnAdPaidHandler(AdValue adValue)
        {
            Debug.Log($"[InterstitialAdController] Ad paid: {adValue.Value} {adValue.CurrencyCode}");
            OnAdPaid?.Invoke(adValue);
        }

        private void OnAdImpressionRecordedHandler()
        {
            Debug.Log("[InterstitialAdController] Impression recorded");
        }

        private void OnAdClickedHandler()
        {
            Debug.Log("[InterstitialAdController] Ad clicked");
        }

        private void OnAdOpenedHandler()
        {
            Debug.Log("[InterstitialAdController] Full screen opened");
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
    }
}
