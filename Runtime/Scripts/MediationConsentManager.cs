#if ADMOB_INSTALLED
using System;
using UnityEngine;
using GoogleMobileAds.Ump.Api;
#if UNITY_ADS_MEDIATION
using GoogleMobileAds.Mediation.UnityAds.Api;
#endif

namespace Autech.Admob
{
    /// <summary>
    /// Manages consent for mediation networks
    /// </summary>
    public class MediationConsentManager
    {
        private readonly ConsentManager consentManager;

        public MediationConsentManager(ConsentManager consentManager)
        {
            this.consentManager = consentManager;
        }

        public void SetMediationConsent(bool isAgeRestrictedUser = false)
        {
            Debug.Log("[MediationConsentManager] SetMediationConsent() started");

#if UNITY_ADS_MEDIATION
            try
            {
                if (!IsConsentInformationInitialized())
                {
                    Debug.LogWarning("[MediationConsentManager] ConsentInformation not initialized yet. Skipping mediation consent update.");
                    return;
                }

                bool canRequestAds = ConsentInformation.CanRequestAds();
                ConsentStatus status = ConsentInformation.ConsentStatus;
                bool hasConsent = status == ConsentStatus.Obtained;

                // EEA/GDPR determination logic:
                // - NotRequired: User is NOT in EEA/GDPR region (consent not needed)
                // - Required: User in EEA but consent not yet obtained
                // - Obtained: User in EEA and consent was obtained
                // - Unknown: Status not determined yet
                bool isEEA = status == ConsentStatus.Required || status == ConsentStatus.Obtained;

                Debug.Log($"[MediationConsentManager] Consent Status: {status}");
                Debug.Log($"[MediationConsentManager] Can request ads: {canRequestAds}");
                Debug.Log($"[MediationConsentManager] Has explicit consent: {hasConsent}");
                Debug.Log($"[MediationConsentManager] Is EEA/GDPR user: {isEEA}");

                string consentType = GetConsentTypeSafe();
                bool isPersonalized = consentType == "Personalized";
                bool isNonPersonalized = consentType == "NonPersonalized";
                if (!string.IsNullOrEmpty(consentType))
                {
                    Debug.Log($"[MediationConsentManager] Consent type: {consentType}");
                }

                bool gdprConsentFlag = hasConsent || (!isEEA && canRequestAds);
                bool privacyConsentFlag = canRequestAds;
                bool trackingAllowed = hasConsent && isPersonalized;

                string privacyMode = DeterminePrivacyMode(isEEA, hasConsent, canRequestAds, isPersonalized, isNonPersonalized);

                UnityAds.SetConsentMetaData("gdpr.consent", gdprConsentFlag);
                UnityAds.SetConsentMetaData("privacy.consent", privacyConsentFlag);
                UnityAds.SetConsentMetaData("privacy.tracking", trackingAllowed);
                ApplyPrivacyModeMetadata(privacyMode);
                UnityAds.SetConsentMetaData("privacy.useroveragelimit", !isAgeRestrictedUser);

                Debug.Log($"[MediationConsentManager] Unity Ads gdpr.consent set to: {gdprConsentFlag}");
                Debug.Log($"[MediationConsentManager] Unity Ads privacy.consent set to: {privacyConsentFlag}");
                Debug.Log($"[MediationConsentManager] Unity Ads privacy.tracking set to: {trackingAllowed}");
                Debug.Log($"[MediationConsentManager] Unity Ads privacy.mode set to: {privacyMode}");
                Debug.Log($"[MediationConsentManager] Unity Ads privacy.useroveragelimit set to: {!isAgeRestrictedUser}");

                Debug.Log("[MediationConsentManager] Unity Ads consent configured successfully");
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogWarning($"[MediationConsentManager] ConsentInformation API not ready: {ex.Message}. Skipping mediation consent update.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MediationConsentManager] Failed to set mediation consent: {ex.Message}");
                Debug.LogException(ex);
                // Don't re-throw - consent failures shouldn't crash the app
            }

            // Add other mediation networks as needed:
            /*
            // Facebook Audience Network
            if (canRequestAds)
            {
                // Facebook.Unity.FB.Mobile.SetAdvertiserIDCollectionEnabled(true);
                // Facebook.Unity.FB.Mobile.SetDataProcessingOptions(new string[] {});
            }

            // AppLovin MAX
            if (canRequestAds)
            {
                // MaxSdk.SetHasUserConsent(hasConsent);
                // MaxSdk.SetIsAgeRestrictedUser(false);
            }

            // ironSource
            if (canRequestAds)
            {
                // IronSource.Agent.setConsent(hasConsent);
            }
            */

            Debug.Log("[MediationConsentManager] Mediation consent configuration completed");
#else
            Debug.Log("[MediationConsentManager] Unity Ads mediation package not detected. Skipping consent metadata update.");
#endif
        }

#if UNITY_ADS_MEDIATION
        private bool IsConsentInformationInitialized()
        {
            try
            {
                // Access properties to ensure the UMP SDK has been initialized
                _ = ConsentInformation.ConsentStatus;
                _ = ConsentInformation.CanRequestAds();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private string GetConsentTypeSafe()
        {
            try
            {
                if (consentManager == null)
                {
                    Debug.LogWarning("[MediationConsentManager] ConsentManager is null.");
                    return "Unknown";
                }
                return consentManager.GetConsentType();
            }
            catch (InvalidOperationException)
            {
                Debug.LogWarning("[MediationConsentManager] Consent type not available yet.");
                return "Unknown";
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MediationConsentManager] Failed to read consent type: {ex.Message}");
                return "Unknown";
            }
        }

        private string DeterminePrivacyMode(bool isEEA, bool hasConsent, bool canRequestAds, bool isPersonalized, bool isNonPersonalized)
        {
            if (hasConsent)
            {
                if (isPersonalized)
                {
                    return "personalized";
                }

                if (isNonPersonalized)
                {
                    return "non_personalized";
                }

                return "consented";
            }

            if (isEEA)
            {
                return canRequestAds ? "limited" : "denied";
            }

            return canRequestAds ? "non_eea" : "unknown";
        }

        private void ApplyPrivacyModeMetadata(string privacyMode)
        {
            try
            {
                // Resolve Unity Ads MetaData types via reflection so we don't hard-depend on the package.
                Type metaDataType = Type.GetType("UnityEngine.Advertisements.MetaData, UnityEngine.Advertisements", false);
                Type advertisementType = Type.GetType("UnityEngine.Advertisements.Advertisement, UnityEngine.Advertisements", false);
                if (metaDataType == null || advertisementType == null)
                {
                    Debug.LogWarning("[MediationConsentManager] Unity Ads MetaData API not available. Skipping privacy.mode update.");
                    return;
                }

                object privacyMetaData = Activator.CreateInstance(metaDataType, "privacy");
                metaDataType.GetMethod("Set", new[] { typeof(string), typeof(object) })?.Invoke(privacyMetaData, new object[] { "mode", privacyMode });
                advertisementType.GetMethod("SetMetaData", new[] { metaDataType })?.Invoke(null, new[] { privacyMetaData });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MediationConsentManager] Failed to set Unity Ads privacy.mode metadata: {ex.Message}");
            }
        }
#endif
    }
}
#endif // ADMOB_INSTALLED
