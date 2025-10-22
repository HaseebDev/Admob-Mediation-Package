# Example Scene Sample

This sample contains a complete example scene demonstrating the AdMob Mediation package integration.

## Contents

- **ExampleAdsScene.unity**: Fully configured scene with UI controls
- UI components for testing all ad types
- Consent management examples
- Remove Ads functionality demonstration

## How to Use

1. Import this sample from the Package Manager
2. Open `ExampleAdsScene.unity`
3. Replace the test Ad Unit IDs with your own in the VerifyAndInitializeAdmob GameObject
4. Press Play to test the implementation

## Scene Components

### VerifyAndInitializeAdmob
Main configuration GameObject containing:
- Ad Unit IDs for Android and iOS
- Consent settings
- Remove Ads configuration
- Banner settings

### UI Controls
- Show Interstitial button
- Show Rewarded button
- Toggle Banner button
- Toggle Remove Ads button
- Debug log display

## Next Steps

1. Configure your AdMob App ID in `Assets/GoogleMobileAdsSettings.asset`
2. Replace test Ad Unit IDs with production IDs
3. Test consent flow in EEA region (use VPN for testing)
4. Integrate into your own scenes

For detailed implementation guide, see the Documentation in the package.
