#if ADMOB_INSTALLED
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
        public event Action<string> OnConsentError;

        private bool isConsentInitialized = false;
        private bool developerBypassEnabled = false;
        private bool wasAbleToRequestAds = false;
        private bool lastPrivacyOptionsRequirementState = false;
        private bool hasLoggedPrivacyRequirementState = false;
        private const int TcfPollIntervalMs = 100;
        private const int TcfDataTimeoutMs = 5000;
        private const string TcfPurposeConsentsKey = "IABTCF_PurposeConsents";

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
                OnConsentReady?.Invoke(true);
                isConsentInitialized = true;
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

            wasAbleToRequestAds = ConsentInformation.CanRequestAds();
            return wasAbleToRequestAds;
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
            await Task.Yield();

            Debug.Log("[ConsentManager] === CONSENT STATUS CHECK ===");
            LogDetailedConsentStatus();

            ConsentStatus status = ConsentInformation.ConsentStatus;

            if (ConsentInformation.CanRequestAds())
            {
                Debug.Log("[ConsentManager] CanRequestAds is TRUE");
                
                if (status == ConsentStatus.Obtained)
                {
                    Debug.Log("[ConsentManager] Waiting for UMP SDK to publish TCF strings (initial check)...");
                    await WaitForTcfDataAsync("initial status check");
                }
                
                OnConsentReady?.Invoke(true);
                isConsentInitialized = true;
                return;
            }

            Debug.Log("[ConsentManager] CanRequestAds is FALSE - Analyzing...");
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

        private async void HandleConsentObtained()
        {
            Debug.Log("[ConsentManager] Consent Status: OBTAINED");
            Debug.Log("[ConsentManager] Waiting for UMP SDK to publish TCF strings (obtained handler)...");
            await WaitForTcfDataAsync("consent obtained handler");
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

                    tcs.TrySetResult(true);
                }
            });

            await tcs.Task;

            if (ConsentInformation.CanRequestAds())
            {
                Debug.Log("[ConsentManager] Waiting for UMP SDK to publish TCF strings (post form)...");
                await WaitForTcfDataAsync("post form");

                OnConsentReady?.Invoke(true);
                isConsentInitialized = true;
            }
            else
            {
                if (IsDevelopmentBypassActive())
                {
                    Debug.LogWarning("[ConsentManager] DEVELOPMENT MODE: Initializing anyway. DISABLE IN PRODUCTION!");
                    OnConsentReady?.Invoke(true);
                    isConsentInitialized = true;
                }
            }
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
                OnConsentError?.Invoke(error.Message);
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

        private async Task WaitForTcfDataAsync(string contextLabel)
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (ConsentInformation.ConsentStatus != ConsentStatus.Obtained)
            {
                return;
            }

            int elapsed = 0;
            while (elapsed < TcfDataTimeoutMs)
            {
                if (IsTcfDataAvailable())
                {
                    if (elapsed > 0)
                    {
                        Debug.Log($"[ConsentManager] TCF data ready after {elapsed}ms ({contextLabel})");
                    }
                    return;
                }

                await Task.Delay(TcfPollIntervalMs);
                elapsed += TcfPollIntervalMs;
            }

            Debug.LogWarning($"[ConsentManager] TCF data not available after {TcfDataTimeoutMs}ms ({contextLabel}) - continuing");
#else
            await Task.CompletedTask;
#endif
        }

        private bool IsTcfDataAvailable()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            string purposeConsents = GetAndroidSharedPreference(TcfPurposeConsentsKey, "");
            return !string.IsNullOrEmpty(purposeConsents) && purposeConsents.Length > 2;
#elif UNITY_IOS && !UNITY_EDITOR
            string purposeConsents = GetIOSUserDefault(TcfPurposeConsentsKey, "");
            return !string.IsNullOrEmpty(purposeConsents) && purposeConsents.Length > 2;
#else
            return true;
#endif
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
                        OnConsentError?.Invoke(formError.Message);
                    }
                    else
                    {
                        Debug.Log("[ConsentManager] Privacy options form completed");
                        LogDetailedConsentStatus();
                        
                        bool canRequestAds = ConsentInformation.CanRequestAds();
                        wasAbleToRequestAds = canRequestAds;
                        isConsentInitialized = true;
                        OnConsentReady?.Invoke(canRequestAds);
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
            if (!hasLoggedPrivacyRequirementState || shouldShow != lastPrivacyOptionsRequirementState)
            {
                Debug.Log($"[ConsentManager] Should show privacy options button: {shouldShow}");
                lastPrivacyOptionsRequirementState = shouldShow;
                hasLoggedPrivacyRequirementState = true;
            }
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

        public void CheckConsentStatusOnResume()
        {
            if (!isConsentInitialized)
            {
                return;
            }

            bool currentCanRequestAds = ConsentInformation.CanRequestAds();
            
            if (currentCanRequestAds != wasAbleToRequestAds)
            {
                Debug.Log($"[ConsentManager] Consent status changed: {wasAbleToRequestAds} -> {currentCanRequestAds}");
                wasAbleToRequestAds = currentCanRequestAds;
                OnConsentReady?.Invoke(currentCanRequestAds);
                
                if (!currentCanRequestAds)
                {
                    Debug.LogWarning("[ConsentManager] User revoked consent - ads should be stopped");
                }
            }
        }

        public void ResetConsentInformationForTesting()
        {
            if (!EnableConsentDebugging && !Application.isEditor)
            {
                Debug.LogError("[ConsentManager] ResetConsentInformationForTesting() can only be called when EnableConsentDebugging is true!");
                Debug.LogError("[ConsentManager] This prevents accidental consent resets in production builds.");
                return;
            }

            Debug.LogWarning("[ConsentManager] ========== RESETTING CONSENT INFORMATION ==========");
            Debug.LogWarning("[ConsentManager] This will clear ALL consent data!");
            
            ConsentInformation.Reset();
            
            isConsentInitialized = false;
            wasAbleToRequestAds = false;

#if UNITY_ANDROID && !UNITY_EDITOR
            ClearAndroidTCFData();
#elif UNITY_IOS && !UNITY_EDITOR
            ClearIOSTCFData();
#endif

            Debug.LogWarning("[ConsentManager] Consent information reset complete");
            Debug.LogWarning("[ConsentManager] ===================================================");
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void ClearAndroidTCFData()
        {
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var context = activity.Call<AndroidJavaObject>("getApplicationContext"))
                using (var prefs = context.Call<AndroidJavaObject>("getSharedPreferences", 
                    context.Call<string>("getPackageName") + "_preferences", 0))
                using (var editor = prefs.Call<AndroidJavaObject>("edit"))
                {
                    string[] tcfKeys = new string[]
                    {
                        "IABTCF_TCString",
                        "IABTCF_gdprApplies",
                        "IABTCF_PurposeConsents",
                        "IABTCF_PurposeOneTreatment",
                        "IABTCF_UseNonStandardStacks",
                        "IABTCF_PublisherCC",
                        "IABTCF_PurposeLegitimateInterests",
                        "IABTCF_SpecialFeaturesOptIns",
                        "IABTCF_VendorConsents",
                        "IABTCF_VendorLegitimateInterests",
                        "IABTCF_PublisherRestrictions",
                        "IABTCF_PublisherConsent",
                        "IABTCF_PublisherLegitimateInterests",
                        "IABTCF_PublisherCustomPurposesConsents",
                        "IABTCF_PublisherCustomPurposesLegitimateInterests"
                    };

                    foreach (string key in tcfKeys)
                    {
                        editor.Call<AndroidJavaObject>("remove", key);
                        Debug.Log($"[ConsentManager] Cleared Android key: {key}");
                    }

                    editor.Call("apply");
                    Debug.Log("[ConsentManager] Android TCF data cleared from SharedPreferences");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConsentManager] Failed to clear Android TCF data: {ex.Message}");
            }
        }
#endif

#if UNITY_IOS && !UNITY_EDITOR
        private void ClearIOSTCFData()
        {
            try
            {
                string[] tcfKeys = new string[]
                {
                    "IABTCF_TCString",
                    "IABTCF_gdprApplies",
                    "IABTCF_PurposeConsents",
                    "IABTCF_PurposeOneTreatment",
                    "IABTCF_UseNonStandardStacks",
                    "IABTCF_PublisherCC",
                    "IABTCF_PurposeLegitimateInterests",
                    "IABTCF_SpecialFeaturesOptIns",
                    "IABTCF_VendorConsents",
                    "IABTCF_VendorLegitimateInterests",
                    "IABTCF_PublisherRestrictions",
                    "IABTCF_PublisherConsent",
                    "IABTCF_PublisherLegitimateInterests",
                    "IABTCF_PublisherCustomPurposesConsents",
                    "IABTCF_PublisherCustomPurposesLegitimateInterests"
                };

                foreach (string key in tcfKeys)
                {
                    _RemoveUserDefault(key);
                    Debug.Log($"[ConsentManager] Cleared iOS key: {key}");
                }

                Debug.Log("[ConsentManager] iOS TCF data cleared from UserDefaults");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConsentManager] Failed to clear iOS TCF data: {ex.Message}");
            }
        }

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void _RemoveUserDefault(string key);
#endif

        public string GetTCFConsentString()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return GetAndroidSharedPreference("IABTCF_TCString", "");
#elif UNITY_IOS && !UNITY_EDITOR
            return GetIOSUserDefault("IABTCF_TCString", "");
#else
            Debug.LogWarning("[ConsentManager] TCF strings only available on Android/iOS builds");
            return "";
#endif
        }

        public bool HasConsentForPurpose(int purposeId)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            string purposeConsents = GetAndroidSharedPreference("IABTCF_PurposeConsents", "");
#elif UNITY_IOS && !UNITY_EDITOR
            string purposeConsents = GetIOSUserDefault("IABTCF_PurposeConsents", "");
#else
            string purposeConsents = "";
#endif

            if (purposeId <= 0 || purposeId > purposeConsents.Length)
                return false;
            return purposeConsents[purposeId - 1] == '1';
        }

        public string GetConsentType()
        {
            try
            {
                ConsentStatus status = ConsentInformation.ConsentStatus;
                
                if (status == ConsentStatus.Obtained)
                {
#if UNITY_ANDROID && !UNITY_EDITOR
                    string purposeConsents = GetAndroidSharedPreference("IABTCF_PurposeConsents", "");
#elif UNITY_IOS && !UNITY_EDITOR
                    string purposeConsents = GetIOSUserDefault("IABTCF_PurposeConsents", "");
#else
                    string purposeConsents = "";
#endif
                    
                    if (!string.IsNullOrEmpty(purposeConsents) && purposeConsents.Length > 2)
                    {
                        bool hasPersonalizationConsent = purposeConsents[2] == '1';
                        Debug.Log($"[ConsentManager] TCF Purpose 3 (Ad Personalization): {hasPersonalizationConsent}");
                        return hasPersonalizationConsent ? "Personalized" : "NonPersonalized";
                    }
                    else
                    {
                        Debug.LogWarning("[ConsentManager] TCF PurposeConsents string not found or invalid - returning Unknown");
                        return "Unknown";
                    }
                }
                else if (status == ConsentStatus.NotRequired)
                {
                    return "NotRequired";
                }
                
                return "Unknown";
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConsentManager] GetConsentType() error: {ex.Message}");
                return "Unknown";
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private string GetAndroidSharedPreference(string key, string defaultValue)
        {
            Debug.Log($"[ConsentManager] >>> GetAndroidSharedPreference called for key: '{key}'");
            
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var context = activity.Call<AndroidJavaObject>("getApplicationContext"))
                {
                    string packageName = context.Call<string>("getPackageName");
                    string prefsName = packageName + "_preferences";
                    Debug.Log($"[ConsentManager] Reading from SharedPreferences: '{prefsName}'");
                    
                    using (var prefs = context.Call<AndroidJavaObject>("getSharedPreferences", prefsName, 0))
                    {
                        string value = prefs.Call<string>("getString", key, defaultValue);
                        
                        if (string.IsNullOrEmpty(value) || value == defaultValue)
                        {
                            Debug.LogWarning($"[ConsentManager] Key '{key}' NOT FOUND or EMPTY (returned default: '{defaultValue}')");
                        }
                        else
                        {
                            int displayLength = Math.Min(50, value.Length);
                            Debug.Log($"[ConsentManager] Key '{key}' FOUND - Length: {value.Length}, Value: {value.Substring(0, displayLength)}...");
                        }
                        
                        return value;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConsentManager] EXCEPTION reading Android SharedPreference '{key}': {ex.Message}");
                Debug.LogError($"[ConsentManager] Stack trace: {ex.StackTrace}");
                return defaultValue;
            }
        }
#endif

#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern string _GetUserDefault(string key, string defaultValue);

        private string GetIOSUserDefault(string key, string defaultValue)
        {
            Debug.Log($"[ConsentManager] >>> GetIOSUserDefault called for key: '{key}'");
            
            try
            {
                string value = _GetUserDefault(key, defaultValue);
                
                if (string.IsNullOrEmpty(value) || value == defaultValue)
                {
                    Debug.LogWarning($"[ConsentManager] Key '{key}' NOT FOUND or EMPTY (returned default: '{defaultValue}')");
                }
                else
                {
                    int displayLength = Math.Min(50, value.Length);
                    Debug.Log($"[ConsentManager] Key '{key}' FOUND - Length: {value.Length}, Value: {value.Substring(0, displayLength)}...");
                }
                
                return value;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConsentManager] EXCEPTION reading iOS UserDefault '{key}': {ex.Message}");
                Debug.LogError($"[ConsentManager] Stack trace: {ex.StackTrace}");
                return defaultValue;
            }
        }
#endif
    }
}
#endif // ADMOB_INSTALLED
