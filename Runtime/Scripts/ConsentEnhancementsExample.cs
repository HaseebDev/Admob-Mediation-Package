#if ADMOB_INSTALLED
using UnityEngine;
using GoogleMobileAds.Ump.Api;

namespace Autech.Admob
{
    public class ConsentEnhancementsExample : MonoBehaviour
    {
        [Header("UI References (Optional)")]
        [SerializeField] private UnityEngine.UI.Text statusText;

        private void Start()
        {
            SubscribeToConsentEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromConsentEvents();
        }

        private void SubscribeToConsentEvents()
        {
            if (AdsManager.Instance != null && AdsManager.Instance.consentManager != null)
            {
                AdsManager.Instance.consentManager.OnConsentReady += OnConsentReady;
                AdsManager.Instance.consentManager.OnConsentError += OnConsentError;
            }
        }

        private void UnsubscribeFromConsentEvents()
        {
            if (AdsManager.Instance != null && AdsManager.Instance.consentManager != null)
            {
                AdsManager.Instance.consentManager.OnConsentReady -= OnConsentReady;
                AdsManager.Instance.consentManager.OnConsentError -= OnConsentError;
            }
        }

        private void OnConsentReady(bool canRequestAds)
        {
            if (canRequestAds)
            {
                Debug.Log("[ConsentEnhancementsExample] Consent obtained - ads enabled");
                UpdateStatusText("Ads Enabled");
                LogConsentDetails();
            }
            else
            {
                Debug.Log("[ConsentEnhancementsExample] Consent denied - ads disabled");
                UpdateStatusText("Ads Disabled - Consent Required");
            }
        }

        private void OnConsentError(string errorMessage)
        {
            Debug.LogError($"[ConsentEnhancementsExample] Consent error: {errorMessage}");
            UpdateStatusText($"Error: {errorMessage}");
        }

        private void LogConsentDetails()
        {
            string consentType = AdsManager.Instance.GetConsentType();
            Debug.Log($"[ConsentEnhancementsExample] Consent Type: {consentType}");
            
            switch (consentType)
            {
                case "Personalized":
                    Debug.Log("[ConsentEnhancementsExample] Showing personalized ads");
                    break;
                case "NonPersonalized":
                    Debug.Log("[ConsentEnhancementsExample] Showing non-personalized ads");
                    break;
                case "NotRequired":
                    Debug.Log("[ConsentEnhancementsExample] Consent not required (non-EEA region)");
                    break;
                case "Unknown":
                    Debug.Log("[ConsentEnhancementsExample] Consent type unknown");
                    break;
            }

            LogTCFConsentStatus();
        }

        private void LogTCFConsentStatus()
        {
            string tcfString = AdsManager.Instance.GetTCFConsentString();
            if (!string.IsNullOrEmpty(tcfString))
            {
                Debug.Log($"[ConsentEnhancementsExample] TCF String: {tcfString}");
                
                bool hasStorageConsent = AdsManager.Instance.HasConsentForPurpose(1);
                bool hasPersonalizedAdsConsent = AdsManager.Instance.HasConsentForPurpose(3);
                bool hasAdSelectionConsent = AdsManager.Instance.HasConsentForPurpose(4);
                
                Debug.Log($"[ConsentEnhancementsExample] TCF Purposes - Storage: {hasStorageConsent}, Personalized Profile: {hasPersonalizedAdsConsent}, Ad Selection: {hasAdSelectionConsent}");
            }
        }

        private void UpdateStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        [ContextMenu("Show Privacy Options")]
        public void ShowPrivacyOptions()
        {
            Debug.Log("[ConsentEnhancementsExample] Showing privacy options form");
            AdsManager.Instance.ShowPrivacyOptionsForm();
        }

        [ContextMenu("Check Current Consent Status")]
        public void CheckConsentStatus()
        {
            Debug.Log("[ConsentEnhancementsExample] Manually checking consent status");
            AdsManager.Instance.CheckConsentStatus();
        }

        [ContextMenu("Log Consent Details")]
        public void LogCurrentConsentDetails()
        {
            Debug.Log("=== CONSENT STATUS ===");
            Debug.Log($"Can Request Ads: {AdsManager.Instance.consentManager?.CanUserRequestAds()}");
            Debug.Log($"Consent Status: {AdsManager.Instance.consentManager?.GetCurrentConsentStatus()}");
            LogConsentDetails();
        }
    }
}
#endif // ADMOB_INSTALLED
