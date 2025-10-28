using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using GoogleMobileAds.Ump.Api;

namespace Autech.Admob
{
    /// <summary>
    /// Manages user consent for personalized ads using Google UMP SDK
    /// </summary>
    public class ConsentManager
    {
        public bool ForceEEAGeographyForTesting { get; set; } = false;
        public bool EnableConsentDebugging { get; set; } = false;
        public bool AlwaysRequestConsentUpdate { get; set; } = true;

        /// <summary>
        /// Tag for users under the age of consent (COPPA compliance).
        /// Set to true if your app targets children or if users are under 13 (US) / 16 (EU).
        /// When enabled, restricts ad personalization and data collection.
        /// Default: false (app targets general audience)
        /// </summary>
        public bool TagForUnderAgeOfConsent { get; set; } = false;

        public event Action<bool> OnConsentReady;

        private bool isConsentInitialized = false;
        private bool developerBypassEnabled = false;

        /// <summary>
        /// Checks if debug bypass is allowed based on build configuration.
        /// GDPR COMPLIANCE: Debug bypasses are ONLY allowed in development builds.
        /// Production builds (Release) will NEVER bypass consent requirements.
        /// </summary>
        private bool IsDebugBypassAllowed()
        {
            // Only allow bypass if:
            // 1. EnableConsentDebugging is explicitly enabled AND
            // 2. Running in editor OR debug build (NOT release build)
            bool isAllowed = EnableConsentDebugging && (Application.isEditor || Debug.isDebugBuild);

            if (isAllowed && !Application.isEditor)
            {
                Debug.LogError("=================== GDPR COMPLIANCE WARNING ===================");
                Debug.LogError("[ConsentManager] CONSENT DEBUGGING IS ENABLED IN A BUILD!");
                Debug.LogError("[ConsentManager] This should ONLY be used during development.");
                Debug.LogError("[ConsentManager] DISABLE EnableConsentDebugging before release!");
                Debug.LogError("===============================================================");
            }

            return isAllowed;
        }

        private bool IsDevelopmentBypassActive()
        {
            return IsDebugBypassAllowed() || developerBypassEnabled;
        }

        /// <summary>
        /// Initializes the consent flow
        /// </summary>
        public async Task<bool> InitializeConsentAsync()
        {
            Debug.Log("[ConsentManager] InitializeConsentAsync() started");
            developerBypassEnabled = IsDebugBypassAllowed() || (Application.isEditor && Debug.isDebugBuild);

            if (!AlwaysRequestConsentUpdate)
            {
                Debug.LogWarning("[ConsentManager] Consent update disabled - NOT recommended by Google");
                return true; // Skip consent
            }

            var request = CreateConsentRequestParameters();

            var tcs = new TaskCompletionSource<bool>();

            ConsentInformation.Update(request, (FormError error) =>
            {
                if (error != null)
                {
                    Debug.LogError($"[ConsentManager] Consent update error: {error.Message}");
                    HandleConsentUpdateError(error, tcs);
                    if (developerBypassEnabled)
                    {
                        Debug.LogWarning("[ConsentManager] DEVELOPMENT MODE: Forcing consent ready state after update error");
                        OnConsentReady?.Invoke(true);
                        isConsentInitialized = true;
                        tcs.TrySetResult(true);
                    }
                }
                else
                {
                    Debug.Log("[ConsentManager] Consent update completed successfully");
                    tcs.TrySetResult(true);
                }
            });

            await tcs.Task;
            await CheckConsentStatusAsync();

            if (!ConsentInformation.CanRequestAds() && developerBypassEnabled)
            {
                Debug.LogWarning("[ConsentManager] DEVELOPMENT MODE: Consent not ready, forcing allow for testing");
                OnConsentReady?.Invoke(true);
                isConsentInitialized = true;
            }

            return ConsentInformation.CanRequestAds();
        }

        private ConsentRequestParameters CreateConsentRequestParameters()
        {
            var request = new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = this.TagForUnderAgeOfConsent
            };

            if (EnableConsentDebugging)
            {
                Debug.Log("[ConsentManager] CONSENT DEBUGGING ENABLED");
                Debug.LogWarning("[ConsentManager] Should be DISABLED in production");

                var debugSettings = new ConsentDebugSettings
                {
                    DebugGeography = ForceEEAGeographyForTesting ? DebugGeography.EEA : DebugGeography.Other,
                    TestDeviceHashedIds = new List<string>()
                };

                request.ConsentDebugSettings = debugSettings;
            }

            return request;
        }

        private async Task CheckConsentStatusAsync()
        {
            // Wait one frame for Update() to complete
            await Task.Yield();

            Debug.Log("[ConsentManager] === CONSENT STATUS CHECK ===");
            LogDetailedConsentStatus();

            if (ConsentInformation.CanRequestAds())
            {
                Debug.Log("[ConsentManager] CanRequestAds is TRUE");
                OnConsentReady?.Invoke(true);
                isConsentInitialized = true;
                return;
            }

            Debug.Log("[ConsentManager] CanRequestAds is FALSE - Analyzing...");

            ConsentStatus status = ConsentInformation.ConsentStatus;
            bool formAvailable = ConsentInformation.IsConsentFormAvailable();

            switch (status)
            {
                case ConsentStatus.Required:
                    await HandleConsentRequiredAsync(formAvailable);
                    break;

                case ConsentStatus.NotRequired:
                    HandleConsentNotRequired();
                    break;

                case ConsentStatus.Obtained:
                    HandleConsentObtained();
                    break;

                case ConsentStatus.Unknown:
                default:
                    await HandleConsentUnknownAsync();
                    break;
            }
        }

        private async Task HandleConsentRequiredAsync(bool formAvailable)
        {
            Debug.Log("[ConsentManager] Consent Status: REQUIRED");

            if (formAvailable)
            {
                Debug.Log("[ConsentManager] Showing consent form");
                await LoadAndShowConsentFormAsync();
            }
            else
            {
                Debug.LogWarning("[ConsentManager] Consent required but NO FORM AVAILABLE");
                Debug.LogWarning("[ConsentManager] Configure privacy message in AdMob Console");

                // GDPR Compliance: Strengthened debug bypass check
                if (IsDevelopmentBypassActive())
                {
                    Debug.LogWarning("[ConsentManager] DEVELOPMENT MODE: Bypassing for testing. DISABLE IN PRODUCTION!");
                    OnConsentReady?.Invoke(true);
                    isConsentInitialized = true;
                }
            }
        }

        private void HandleConsentNotRequired()
        {
            Debug.Log("[ConsentManager] Consent Status: NOT REQUIRED");
            OnConsentReady?.Invoke(true);
            isConsentInitialized = true;
        }

        private void HandleConsentObtained()
        {
            Debug.Log("[ConsentManager] Consent Status: OBTAINED");
            OnConsentReady?.Invoke(true);
            isConsentInitialized = true;
        }

        private async Task HandleConsentUnknownAsync()
        {
            Debug.Log("[ConsentManager] Consent Status: UNKNOWN");

            // GDPR Compliance: Strengthened debug bypass check
            if (IsDevelopmentBypassActive())
            {
                Debug.LogWarning("[ConsentManager] DEVELOPMENT MODE: Bypassing. DISABLE IN PRODUCTION!");
                OnConsentReady?.Invoke(true);
                isConsentInitialized = true;
            }
            else
            {
                Debug.Log("[ConsentManager] PRODUCTION: Waiting for resolution with timeout");
                await ProductionConsentTimeoutAsync();
            }
        }

        private async Task LoadAndShowConsentFormAsync()
        {
            var tcs = new TaskCompletionSource<bool>();

            ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
            {
                if (formError != null)
                {
                    Debug.LogError($"[ConsentManager] Consent form error: {formError.Message}");

            // GDPR Compliance: Strengthened debug bypass check
                if (IsDevelopmentBypassActive())
                {
                    Debug.LogWarning("[ConsentManager] DEVELOPMENT MODE: Bypassing form error. DISABLE IN PRODUCTION!");
                    OnConsentReady?.Invoke(true);
                    isConsentInitialized = true;
                }
                    tcs.TrySetResult(false);
                }
                else
                {
                    Debug.Log("[ConsentManager] Consent form completed successfully");
                    LogDetailedConsentStatus();

                    if (ConsentInformation.CanRequestAds())
                    {
                        OnConsentReady?.Invoke(true);
                        isConsentInitialized = true;
                    }
                    else
                    {
                        // GDPR Compliance: Strengthened debug bypass check
                        if (IsDevelopmentBypassActive())
                        {
                            Debug.LogWarning("[ConsentManager] DEVELOPMENT MODE: Initializing anyway. DISABLE IN PRODUCTION!");
                            OnConsentReady?.Invoke(true);
                            isConsentInitialized = true;
                        }
                    }

                    tcs.TrySetResult(true);
                }
            });

            await tcs.Task;
        }

        private void HandleConsentUpdateError(FormError error, TaskCompletionSource<bool> tcs)
        {
            if (ConsentInformation.CanRequestAds())
            {
                Debug.Log("[ConsentManager] Error occurred but CanRequestAds is true");
                OnConsentReady?.Invoke(true);
                tcs.TrySetResult(true);
                return;
            }

            // GDPR Compliance: Strengthened debug bypass check
            if (IsDevelopmentBypassActive())
            {
                Debug.LogWarning("[ConsentManager] DEVELOPMENT MODE: Bypassing error. DISABLE IN PRODUCTION!");
                OnConsentReady?.Invoke(true);
                isConsentInitialized = true;
                tcs.TrySetResult(true);
            }
            else
            {
                Debug.LogError("[ConsentManager] PRODUCTION ERROR: Cannot recover");
                Debug.LogError("[ConsentManager] User must provide valid consent to show ads");
                tcs.TrySetResult(false);
            }
        }

        private async Task ProductionConsentTimeoutAsync()
        {
            float timeoutSeconds = 15f;
            Debug.Log($"[ConsentManager] Starting timeout ({timeoutSeconds}s)");

            await Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));

            if (!isConsentInitialized)
            {
                Debug.LogWarning("[ConsentManager] Consent timeout reached");

                if (ConsentInformation.CanRequestAds())
                {
                    Debug.Log("[ConsentManager] CanRequestAds became true - proceeding");
                    OnConsentReady?.Invoke(true);
                    isConsentInitialized = true;
                }
                else
                {
                    Debug.LogWarning("[ConsentManager] Still cannot request ads");
                    OnConsentReady?.Invoke(false);
                }
            }
        }

        private void LogDetailedConsentStatus()
        {
            Debug.Log($"[ConsentManager] CanRequestAds: {ConsentInformation.CanRequestAds()}");
            Debug.Log($"[ConsentManager] ConsentStatus: {ConsentInformation.ConsentStatus}");
            Debug.Log($"[ConsentManager] IsConsentFormAvailable: {ConsentInformation.IsConsentFormAvailable()}");
            Debug.Log($"[ConsentManager] PrivacyOptionsRequirementStatus: {ConsentInformation.PrivacyOptionsRequirementStatus}");
        }

        public void ShowPrivacyOptionsForm()
        {
            Debug.Log("[ConsentManager] ShowPrivacyOptionsForm() called");

            if (ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required)
            {
                ConsentForm.ShowPrivacyOptionsForm((FormError formError) =>
                {
                    if (formError != null)
                    {
                        Debug.LogError($"[ConsentManager] Privacy options form error: {formError.Message}");
                    }
                    else
                    {
                        Debug.Log("[ConsentManager] Privacy options form completed");
                    }
                });
            }
            else
            {
                Debug.LogWarning("[ConsentManager] Privacy options form not required for this user");
            }
        }

        public bool ShouldShowPrivacyOptionsButton()
        {
            bool shouldShow = ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
            Debug.Log($"[ConsentManager] Should show privacy options button: {shouldShow}");
            return shouldShow;
        }

        public ConsentStatus GetCurrentConsentStatus()
        {
            return ConsentInformation.ConsentStatus;
        }

        public bool CanUserRequestAds()
        {
            return ConsentInformation.CanRequestAds();
        }
    }
}
