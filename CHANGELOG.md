# Changelog

All notable changes to the Autech AdMob Mediation Unity Ads package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.1.0] - 2025-10-31

### 🚀 Added
- **Thread-Safe Orchestration**
  - Captures Unity's synchronization context at startup for safe async callbacks
  - Auto-instantiates `AdsManager` when no scene instance exists
  - Concurrency guards (`TrySetAdShowing`) prevent overlapping fullscreen ads
- **Secure Storage Stack**
  - New `SecureStorage` module using AES-256-CBC + HMAC integrity verification
  - `AdPersistenceManager` now generates device-specific keys with graceful fallbacks
  - Legacy XOR migration helpers expose explicit upgrade paths
- **Consent → Mediation Bridge**
  - `MediationConsentManager` mirrors Google UMP decisions directly into Unity Ads metadata
  - One-call refresh helper keeps mediation networks aligned after consent changes
- **Expanded Tooling & Samples**
  - Documentation refresh across README, INSTALL, and consent guides
  - Context-menu shortcuts and sample UIs updated for new APIs and debugging hooks
  - Editor bootstrapper prompts for one-click Google dependency installation

### 🔧 Changed
- Distribution now targets Package Manager Git URL installs instead of `.unitypackage` drops
- README highlights the new architecture, secure persistence, and consent pipeline
- Installation instructions streamlined around GMA/EDM prerequisites and Git imports
- Version history reorganized to surface active development (HEAD) and historical releases

### 🛠️ Fixed
- Race conditions when multiple ads attempted to show concurrently
- Controllers now cancel pending retries before destroying ad instances to avoid leaks
- Consent flow no longer skips mediation metadata when debugging flags are enabled
- Remove Ads persistence logs warnings when developers rotate encryption keys post-release

---

## [2.0.2] - 2024-12-27

### 🚀 Added
- **Enhanced UI Integration**
  - Button state management based on ad availability
  - Real-time button enable/disable based on ad readiness
  - Visual feedback with color-coded Remove Ads button (Red/Green)
  - Event-driven UI updates for better user experience

- **Improved Banner Visibility Control**
  - First-time loading detection and management
  - Smart banner visibility based on `showBannerOnStart` setting
  - Proper timing control after consent and initialization
  - Automatic banner state management after loading completion

- **Debug Logging System**
  - Event-based logging system
  - UI text component integration for real-time log display
  - Configurable logging levels with detailed feedback
  - Clean, professional logging without emojis

### 🔧 Enhanced
- **Code Quality & Maintainability**
  - Removed unnecessary comments and debug logs
  - Cleaner, more professional codebase
  - Improved code organization and readability
  - Better separation of concerns

- **Banner Management**
  - Enhanced banner loading and visibility logic
  - Improved first-time loading detection
  - Better coordination between AdsManager and VerifyAdmob
  - Automatic banner state restoration

- **UI/UX Improvements**
  - Dynamic button states based on ad availability
  - Visual Remove Ads status indication
  - Real-time feedback for user interactions
  - Professional logging output

### 🛠️ Technical Improvements
- Streamlined initialization process with proper timing
- Enhanced event system for UI updates
- Improved error handling and state management
- Better memory management and resource cleanup
- Production-ready logging system

### 📦 Package Information
- **File Size**: ~21 KiB (optimized)
- **Unity Compatibility**: 2020.3+
- **Platform Support**: Android, iOS
- **Dependencies**: Google Mobile Ads Unity SDK 10.2.0+

---

## [2.0.1] - 2024-12-27

### 🚀 Added
- **Complete Remove Ads System**
  - Smart ad filtering that disables Banner, Interstitial, and App Open ads
  - Preserves Rewarded and Rewarded Interstitial ads for continued monetization
  - Performance optimization by preventing loading of disabled ad types
  - Ready for IAP integration with Remove Ads purchases

- **Persistence & Storage System**
  - Local storage with PlayerPrefs support
  - Optional XOR encryption for secure data storage
  - Cloud sync integration points for Unity Cloud Save and Firebase
  - Automatic save/load on app restart
  - Event system for Remove Ads status changes
  - Cross-device synchronization capability

- **Full Configuration Exposure**
  - All AdsManager settings accessible via VerifyAdmob Inspector
  - Runtime modification of all ad settings without code changes
  - Dynamic Ad Unit ID management for Android and iOS platforms
  - Automatic platform detection and ID switching
  - Configuration validation tools

- **Enhanced Ad Unit ID Management**
  - Inspector-configurable Ad Unit IDs (no code modification required)
  - Runtime Ad Unit ID updates with automatic ad refresh
  - Validation tools to detect test IDs and empty values
  - Support for both test and production Ad Unit IDs
  - Platform-specific ID configuration

- **Comprehensive Testing Suite**
  - TestCalls script expanded to test all ad types (now integrated into AdsExampleUI)
  - Banner management testing (show/hide/position/size)
  - Remove Ads workflow testing with automated sequences
  - Context menu integration for easy testing
  - Detailed logging with emoji-based feedback
  - Automated sequential testing of all ad types

- **Developer Tools & Debugging**
  - Status checking for all ad types and system state
  - Ad Unit ID validation and test ID detection
  - Persistence testing tools
  - Context menu shortcuts for common operations
  - Real-time ad system monitoring

- **Enhanced Banner Management**
  - Banner position cycling through all available positions
  - Banner size testing with different AdSize options
  - Improved visibility controls and status tracking
  - Better banner lifecycle management

### 🔧 Enhanced
- **Event System**
  - Added `OnRemoveAdsChanged` and `OnRemoveAdsLoadedFromStorage` events
  - Real-time notifications for Remove Ads status changes
  - Event-driven architecture for better state management

- **Error Handling & Validation**
  - Comprehensive validation for all configuration options
  - Graceful fallbacks for encryption failures
  - Enhanced error messages with actionable guidance
  - Production-ready security measures

- **Code Organization**
  - Clean separation of concerns between components
  - Improved code documentation and comments
  - Better method organization with region grouping
  - Enhanced maintainability and extensibility

### 🛠️ Technical Improvements
- Proper error handling and fallbacks throughout the system
- Event-driven architecture for Remove Ads state management
- Configurable logging levels for debugging and production
- Production-ready encryption system for sensitive data
- Memory management optimizations
- Thread-safe operations for persistence layer

### 📦 Package Information
- **File Size**: ~21 KiB (increased from ~14 KiB due to new features)
- **Unity Compatibility**: 2020.3+
- **Platform Support**: Android, iOS
- **Dependencies**: Google Mobile Ads Unity SDK 10.2.0+

---

## [2.0.0] - 2024-12-XX

### 🚀 Added
- **Adaptive Banner Support**
  - Runtime switching between adaptive and standard banners
  - Smart sizing for better user experience across devices
  - Automatic size optimization based on device characteristics

- **Collapsible Banner Support** 
  - Complete implementation with custom targeting
  - Enhanced user experience with space-saving banners
  - Configurable collapse behavior

- **Unity Ads Mediation Integration**
  - Fully integrated Unity Ads mediation
  - Comprehensive consent management
  - GDPR compliance with UMP SDK
  - Seamless fallback ad serving

- **Revenue Tracking Framework**
  - Built-in analytics framework ready for implementation
  - Support for Firebase Analytics integration
  - Revenue data collection and reporting
  - Ad performance metrics tracking

- **Enhanced Error Handling**
  - Production-ready error handling with retry logic
  - Exponential backoff for failed ad loads
  - Comprehensive error logging and reporting
  - Graceful degradation on failures

- **Memory Management**
  - Proper event cleanup and resource management
  - Memory leak prevention measures
  - Optimized ad loading and disposal
  - Resource pooling for better performance

### 🔧 Enhanced
- Improved ad loading reliability
- Better error reporting and debugging
- Enhanced performance optimizations
- Streamlined initialization process

### 📦 Package Information
- **File Size**: ~14 KiB
- **Unity Compatibility**: 2020.3+
- **Platform Support**: Android, iOS

---

## [1.0.0] - 2024-XX-XX

### 🚀 Added
- **Initial AdMob Integration**
  - Basic Banner ad implementation
  - Interstitial ad support
  - Rewarded ad functionality
  - App Open ad integration

- **Core Features**
  - Simple drag-and-drop implementation
  - Inspector-based configuration
  - Cross-platform support (Android & iOS)
  - Basic error handling

- **Unity Integration**
  - Plug & Play setup with prefab system
  - VerifyAdmob component for easy initialization
  - Basic ad management through AdsManager singleton

### 📦 Package Information
- **File Size**: ~8 KiB
- **Unity Compatibility**: 2020.3+
- **Platform Support**: Android, iOS

---

## Migration Guides

### From v2.0.0 to v2.0.1

#### Configuration Changes
1. **VerifyAdmob Component**: New fields added for Remove Ads and Ad Unit IDs
   - The component will automatically upgrade existing configurations
   - Review new settings in Inspector and configure as needed

2. **Ad Unit IDs**: Now configurable via Inspector
   - Previous hardcoded IDs will be preserved as default values
   - Update Ad Unit IDs in VerifyAdmob component Inspector
   - Use validation tools to ensure correct configuration

3. **Remove Ads Integration**: 
   - Add Remove Ads logic to your IAP purchase handlers
   - Implement `VerifyAdmob.PurchaseRemoveAds()` in purchase success callbacks
   - Test Remove Ads workflow using new testing tools

#### New API Usage
```csharp
// Remove Ads integration
AdsManager.Instance.RemoveAds = true;

// Dynamic Ad Unit ID configuration
verifyAdmob.SetAndroidAdIds(bannerId, interstitialId, rewardedId, rewardedInterId, appOpenId);

// Testing new features
testCalls.TestRemoveAdsFunctionality();
testCalls.CheckAllAdStatus();
```

### From v1.0.0 to v2.0.0

#### Breaking Changes
- Updated minimum Unity version requirement
- New dependency requirements for mediation
- Enhanced initialization process

#### Configuration Updates
1. Install Unity Ads mediation plugin
2. Update Google Mobile Ads SDK to 10.2.0+
3. Configure UMP SDK for GDPR compliance
4. Update ad initialization code if customized

---

## Support & Documentation

- **Issues**: [GitHub Issues](https://github.com/HaseebDev/Admob-Mediation-Package/issues)
- **Documentation**: [README.md](README.md)
- **Releases**: [GitHub Releases](https://github.com/HaseebDev/Admob-Mediation-Package/releases)

---

*For technical support or feature requests, please open an issue on GitHub.* 
