#if ADMOB_INSTALLED
using UnityEngine;
using UnityEngine.UI;
using GoogleMobileAds.Ump.Api;
using Autech.Admob;

namespace AdmobMediationPackage
{
    /// <summary>
    /// Example UI script demonstrating how to use the new consent management features.
    /// This shows best practices for implementing GDPR-compliant ad controls in your game.
    /// IMPORTANT: This script uses AdsManager.Instance for ad operations and VerifyAdmob for configuration/consent.
    /// Updated for refactored architecture with modular components and async/await.
    /// </summary>
    public class ConsentUIExample : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button privacySettingsButton;
        [SerializeField] private Button refreshConsentButton;
        [SerializeField] private Button logStatusButton;
        [SerializeField] private Button showInterstitialButton;
        [SerializeField] private Button showRewardedButton;
        
        [Header("Status Display")]
        [SerializeField] private Text consentStatusText;
        [SerializeField] private Text canRequestAdsText;
        [SerializeField] private GameObject adControlsPanel;
        
        [Header("Dependencies - Assign in Inspector")]
        [SerializeField] private VerifyAdmob verifyAdmob;

        private void Start()
        {
            // Validate that we have the required components
            if (verifyAdmob == null)
            {
                Debug.LogError("[ConsentUIExample] VerifyAdmob reference not assigned! Please assign it in the Inspector.");
                return;
            }

            if (AdsManager.Instance == null)
            {
                Debug.LogError("[ConsentUIExample] AdsManager.Instance is null! Make sure AdsManager is properly initialized.");
                return;
            }

            SetupButtons();
            UpdateUI();
            
            // Update UI every few seconds to reflect any changes
            InvokeRepeating(nameof(UpdateUI), 1f, 3f);
        }

        private void SetupButtons()
        {
            // Privacy Settings Button
            if (privacySettingsButton != null)
            {
                privacySettingsButton.onClick.AddListener(ShowPrivacyOptions);
            }

            // Refresh Consent Button
            if (refreshConsentButton != null)
            {
                refreshConsentButton.onClick.AddListener(RefreshConsent);
            }

            // Log Status Button (for debugging)
            if (logStatusButton != null)
            {
                logStatusButton.onClick.AddListener(LogConsentStatus);
            }

            // Ad Buttons
            if (showInterstitialButton != null)
            {
                showInterstitialButton.onClick.AddListener(ShowInterstitialAd);
            }

            if (showRewardedButton != null)
            {
                showRewardedButton.onClick.AddListener(ShowRewardedAd);
            }
        }

        private void UpdateUI()
        {
            if (verifyAdmob == null || AdsManager.Instance == null) return;

            // Update consent status display
            if (consentStatusText != null)
            {
                ConsentStatus status = verifyAdmob.GetCurrentConsentStatus();
                consentStatusText.text = $"Consent Status: {status}";
                
                // Color code the status
                switch (status)
                {
                    case ConsentStatus.Obtained:
                        consentStatusText.color = Color.green;
                        break;
                    case ConsentStatus.Required:
                        consentStatusText.color = Color.yellow;
                        break;
                    case ConsentStatus.NotRequired:
                        consentStatusText.color = Color.blue;
                        break;
                    default:
                        consentStatusText.color = Color.gray;
                        break;
                }
            }

            // Update can request ads status
            if (canRequestAdsText != null)
            {
                bool canRequestAds = verifyAdmob.CanUserRequestAds();
                canRequestAdsText.text = $"Can Request Ads: {canRequestAds}";
                canRequestAdsText.color = canRequestAds ? Color.green : Color.red;
            }

            // Show/hide privacy settings button based on requirement
            if (privacySettingsButton != null)
            {
                bool shouldShow = verifyAdmob.ShouldShowPrivacyOptionsButton();
                privacySettingsButton.gameObject.SetActive(shouldShow);
            }

            // Show/hide ad controls based on consent and remove ads status
            if (adControlsPanel != null)
            {
                bool canShowAds = verifyAdmob.CanUserRequestAds() && 
                                 !verifyAdmob.IsRemoveAdsEnabled() &&
                                 AdsManager.Instance.IsInitialized;
                adControlsPanel.SetActive(canShowAds);
            }

            // Update interactability of ad buttons
            bool adsAvailable = verifyAdmob.CanUserRequestAds() && 
                               !verifyAdmob.IsRemoveAdsEnabled() &&
                               AdsManager.Instance.IsInitialized;

            if (showInterstitialButton != null)
            {
                showInterstitialButton.interactable = adsAvailable;
            }

            if (showRewardedButton != null)
            {
                showRewardedButton.interactable = adsAvailable;
            }
        }

        #region Button Event Handlers

        /// <summary>
        /// Shows the privacy options form to let users manage their consent.
        /// This should be accessible from your game's settings menu.
        /// </summary>
        public void ShowPrivacyOptions()
        {
            Debug.Log("[ConsentUIExample] Privacy options requested by user");
            verifyAdmob.ShowPrivacyOptionsForm();
            
            // Update UI after a short delay to reflect any changes
            Invoke(nameof(UpdateUI), 1f);
        }

        /// <summary>
        /// Manually refreshes mediation consent for all networks.
        /// Useful for debugging or when you've added new mediation networks.
        /// </summary>
        public void RefreshConsent()
        {
            Debug.Log("[ConsentUIExample] Consent refresh requested by user");
            verifyAdmob.RefreshMediationConsent();
            UpdateUI();
        }

        /// <summary>
        /// Logs comprehensive consent status for debugging purposes.
        /// </summary>
        public void LogConsentStatus()
        {
            Debug.Log("[ConsentUIExample] Logging consent status for debugging");
            verifyAdmob.LogConsentStatus();
        }

        /// <summary>
        /// Shows an interstitial ad with proper consent checking.
        /// Uses AdsManager.Instance for actual ad display.
        /// </summary>
        public void ShowInterstitialAd()
        {
            if (!CanShowAds())
            {
                Debug.LogWarning("[ConsentUIExample] Cannot show interstitial - consent or remove ads issue");
                ShowAdUnavailableMessage();
                return;
            }

            Debug.Log("[ConsentUIExample] Showing interstitial ad");
            AdsManager.Instance.ShowInterstitial();
        }

        /// <summary>
        /// Shows a rewarded ad with proper consent checking.
        /// Uses AdsManager.Instance for actual ad display.
        /// </summary>
        public void ShowRewardedAd()
        {
            if (!CanShowAds())
            {
                Debug.LogWarning("[ConsentUIExample] Cannot show rewarded ad - consent or remove ads issue");
                ShowAdUnavailableMessage();
                return;
            }

            Debug.Log("[ConsentUIExample] Showing rewarded ad");
            AdsManager.Instance.ShowRewarded();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Comprehensive check if ads can be shown right now.
        /// Uses both VerifyAdmob for configuration/consent and AdsManager for initialization status.
        /// </summary>
        private bool CanShowAds()
        {
            if (verifyAdmob == null)
            {
                Debug.LogError("[ConsentUIExample] VerifyAdmob reference is null");
                return false;
            }

            if (AdsManager.Instance == null)
            {
                Debug.LogError("[ConsentUIExample] AdsManager.Instance is null");
                return false;
            }

            // Check if user has consented to ads
            if (!verifyAdmob.CanUserRequestAds())
            {
                Debug.Log("[ConsentUIExample] User has not consented to ads");
                return false;
            }

            // Check if remove ads is purchased
            if (verifyAdmob.IsRemoveAdsEnabled())
            {
                Debug.Log("[ConsentUIExample] Remove ads is enabled");
                return false;
            }

            // Check if AdMob is initialized
            if (!AdsManager.Instance.IsInitialized)
            {
                Debug.Log("[ConsentUIExample] AdMob is not yet initialized");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Shows a user-friendly message about why ads can't be shown.
        /// Call this when ad requests fail due to consent issues.
        /// </summary>
        public void ShowAdUnavailableMessage()
        {
            string message = "";

            if (!verifyAdmob.CanUserRequestAds())
            {
                ConsentStatus status = verifyAdmob.GetCurrentConsentStatus();
                switch (status)
                {
                    case ConsentStatus.Required:
                        message = "Ads require your consent. Please check privacy settings.";
                        break;
                    case ConsentStatus.Unknown:
                        message = "Checking consent status. Please try again in a moment.";
                        break;
                    default:
                        message = "Ads are not available at this time.";
                        break;
                }
            }
            else if (verifyAdmob.IsRemoveAdsEnabled())
            {
                message = "Ads have been removed. Thank you for your purchase!";
            }
            else if (!AdsManager.Instance.IsInitialized)
            {
                message = "Ads are loading. Please try again in a moment.";
            }

            Debug.Log($"[ConsentUIExample] Ad unavailable: {message}");
            
            // Here you would show this message to the user in your UI
            // For example: ShowToast(message) or UpdateStatusText(message)
        }

        #endregion

        #region Context Menu Methods (for debugging)

        [ContextMenu("Test Privacy Options")]
        private void TestPrivacyOptions()
        {
            ShowPrivacyOptions();
        }

        [ContextMenu("Test Consent Refresh")]
        private void TestConsentRefresh()
        {
            RefreshConsent();
        }

        [ContextMenu("Force UI Update")]
        private void ForceUIUpdate()
        {
            UpdateUI();
        }

        #endregion
    }
}#endif // ADMOB_INSTALLED
