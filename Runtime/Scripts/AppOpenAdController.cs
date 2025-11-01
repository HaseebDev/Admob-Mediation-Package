#if ADMOB_INSTALLED
using System;
using System.Threading.Tasks;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;

namespace Autech.Admob
{
    /// <summary>
    /// Handles app open ad lifecycle and management with app state tracking
    /// </summary>
    public class AppOpenAdController
    {
        private readonly AdConfiguration config;
        private AppOpenAd appOpenAd;
        private DateTime appOpenExpireTime;
        private DateTime lastAppOpenShownTime;
        private bool isAppOpenAdShowing = false;
        private int retryAttempt = 0;
        private const int maxRetryCount = 3;

        private bool isColdStart = true;
        private System.Threading.CancellationTokenSource retryCts;
        private readonly object retryLock = new object();

        public bool IsAvailable => appOpenAd != null && DateTime.Now < appOpenExpireTime && !isAppOpenAdShowing;

        public event Action<AdValue> OnAdPaid;

        public AppOpenAdController(AdConfiguration configuration)
        {
            config = configuration ?? throw new ArgumentNullException(nameof(configuration));
            RegisterAppStateListener();
        }

        public void LoadAd()
        {
            Debug.Log("[AppOpenAdController] LoadAd() started");

            if (config.RemoveAds)
            {
                Debug.Log("[AppOpenAdController] Skipped - RemoveAds enabled");
                return;
            }

            CancelRetryCts();

            // Destroy existing ad before loading new one
            if (appOpenAd != null)
            {
                UnregisterEventHandlers(appOpenAd);
                appOpenAd.Destroy();
                appOpenAd = null;
                Debug.Log("[AppOpenAdController] Destroyed existing ad before loading new one");
            }

            var adRequest = new AdRequest();
            AppOpenAd.Load(config.AppOpenId, adRequest, OnAdLoadCallback);
        }

        private void OnAdLoadCallback(AppOpenAd ad, LoadAdError error)
        {
            if (error != null)
            {
                Debug.LogError($"[AppOpenAdController] Failed to load: {error}");
                _ = RetryLoadAsync();
                return;
            }

            appOpenAd = ad;
            appOpenExpireTime = DateTime.Now + TimeSpan.FromHours(4);
            retryAttempt = 0;
            RegisterEventHandlers(appOpenAd);
            Debug.Log("[AppOpenAdController] Ad loaded successfully");
        }

        public void Show(Action onSuccess = null, Action onFailure = null)
        {
            Debug.Log("[AppOpenAdController] Show() called");

            if (config.RemoveAds)
            {
                Debug.Log("[AppOpenAdController] Cannot show - RemoveAds enabled");
                onFailure?.Invoke();
                return;
            }

            if (isAppOpenAdShowing)
            {
                Debug.Log("[AppOpenAdController] Ad already showing");
                onFailure?.Invoke();
                return;
            }

            if (IsAvailable)
            {
                isAppOpenAdShowing = true;

                Action onClosed = null;
                Action<AdError> onFailed = null;

                onClosed = () =>
                {
                    appOpenAd.OnAdFullScreenContentClosed -= onClosed;
                    appOpenAd.OnAdFullScreenContentFailed -= onFailed;

                    isAppOpenAdShowing = false;
                    Debug.Log("[AppOpenAdController] Ad closed");
                    LoadAd();
                    onSuccess?.Invoke();
                };

                onFailed = (error) =>
                {
                    appOpenAd.OnAdFullScreenContentClosed -= onClosed;
                    appOpenAd.OnAdFullScreenContentFailed -= onFailed;

                    isAppOpenAdShowing = false;
                    Debug.LogError($"[AppOpenAdController] Failed to show: {error}");
                    LoadAd();
                    onFailure?.Invoke();
                };

                appOpenAd.OnAdFullScreenContentClosed += onClosed;
                appOpenAd.OnAdFullScreenContentFailed += onFailed;

                try
                {
                    appOpenAd.Show();
                    lastAppOpenShownTime = DateTime.Now;
                }
                catch (Exception ex)
                {
                    // Exception during Show() - clean up and notify failure
                    appOpenAd.OnAdFullScreenContentClosed -= onClosed;
                    appOpenAd.OnAdFullScreenContentFailed -= onFailed;
                    isAppOpenAdShowing = false;

                    Debug.LogError($"[AppOpenAdController] Exception during Show(): {ex.Message}");
                    Debug.LogException(ex);

                    onFailure?.Invoke();
                    LoadAd(); // Try to recover by loading a new ad
                }
            }
            else
            {
                Debug.LogWarning("[AppOpenAdController] Ad not available");
                onFailure?.Invoke();
                LoadAd();
            }
        }

        public void Destroy()
        {
            // Cancel any ongoing retry operations
            CancelRetryCts();

            // Unsubscribe from app state listener to prevent memory leak
            AppStateEventNotifier.AppStateChanged -= OnAppStateChanged;

            if (appOpenAd != null)
            {
                // Unregister event handlers before destroying to prevent potential memory leaks
                UnregisterEventHandlers(appOpenAd);

                appOpenAd.Destroy();
                appOpenAd = null;
                Debug.Log("[AppOpenAdController] Ad destroyed and cleaned up");
            }

            // Reset showing flag
            isAppOpenAdShowing = false;
        }

        private void RegisterAppStateListener()
        {
            AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
        }

        private void OnAppStateChanged(AppState appState)
        {
            Debug.Log($"[AppOpenAdController] App state changed to: {appState}");

            if (appState == AppState.Foreground && config.AutoShowAppOpenAds)
            {
                if (!isColdStart && !isAppOpenAdShowing)
                {
                    if (DateTime.Now.Subtract(lastAppOpenShownTime).TotalSeconds >= config.AppOpenCooldownTime)
                    {
                        Show();
                    }
                }
            }
        }

        public void MarkColdStartComplete()
        {
            isColdStart = false;
            Debug.Log("[AppOpenAdController] Cold start marked complete");
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

        private async Task RetryLoadAsync()
        {
            var localCts = CreateRetryCts();

            try
            {
                retryAttempt++;
                // Fixed retry logic: allows exactly maxRetryCount retries (3 attempts)
                if (retryAttempt <= maxRetryCount)
                {
                    int delay = (int)(Math.Pow(2, retryAttempt) * 1000);
                    Debug.Log($"[AppOpenAdController] Retrying load in {delay}ms (attempt {retryAttempt}/{maxRetryCount})");
                    await Task.Delay(delay, localCts.Token);

                    if (!localCts.IsCancellationRequested)
                    {
                        LoadAd();
                    }
                }
                else
                {
                    Debug.LogWarning($"[AppOpenAdController] Max retry count reached ({maxRetryCount})");
                }
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log("[AppOpenAdController] Retry operation was cancelled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppOpenAdController] RetryLoadAsync failed: {ex.Message}");
                Debug.LogException(ex);
            }
            finally
            {
                ReleaseRetryCts(localCts);
            }
        }

        private void RegisterEventHandlers(AppOpenAd ad)
        {
            ad.OnAdPaid += OnAdPaidHandler;
            ad.OnAdImpressionRecorded += OnAdImpressionRecordedHandler;
            ad.OnAdClicked += OnAdClickedHandler;
            ad.OnAdFullScreenContentOpened += OnAdFullScreenContentOpenedHandler;
        }

        private void UnregisterEventHandlers(AppOpenAd ad)
        {
            if (ad == null) return;

            // Note: We need to use the same handler references to unsubscribe
            // GoogleMobileAds SDK should handle cleanup on Destroy(), but explicit unsubscribe is safer
            ad.OnAdPaid -= OnAdPaidHandler;
            ad.OnAdImpressionRecorded -= OnAdImpressionRecordedHandler;
            ad.OnAdClicked -= OnAdClickedHandler;
            ad.OnAdFullScreenContentOpened -= OnAdFullScreenContentOpenedHandler;

            Debug.Log("[AppOpenAdController] Event handlers unregistered");
        }

        // Event handler methods for proper unsubscription
        private void OnAdPaidHandler(AdValue adValue)
        {
            Debug.Log($"[AppOpenAdController] Ad paid: {adValue.Value} {adValue.CurrencyCode}");
            OnAdPaid?.Invoke(adValue);
        }

        private void OnAdImpressionRecordedHandler()
        {
            Debug.Log("[AppOpenAdController] Impression recorded");
        }

        private void OnAdClickedHandler()
        {
            Debug.Log("[AppOpenAdController] Ad clicked");
        }

        private void OnAdFullScreenContentOpenedHandler()
        {
            Debug.Log("[AppOpenAdController] Full screen opened");
        }
    }
}
#endif // ADMOB_INSTALLED
