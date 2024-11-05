🌟 Autech Admob Mediation Unity Ads

Welcome to the Autech Admob Mediation Unity Ads package! This package provides seamless integration of AdMob mediation with Unity Ads for your Unity projects.
📦 Integration Steps

Follow these steps to manually integrate this package into your Unity project:
1. Set Up OpenUPM (Required for Dependencies)

This package requires Google Mobile Ads, available through OpenUPM. Here’s how to get OpenUPM set up:

    Install Node.js (if not already installed):
        Download and install Node.js from https://nodejs.org/.
Then

    Install OpenUPM CLI:
        Open a command prompt or terminal and run:

Then

    npm install -g openupm-cli

Add Google Mobile Ads via OpenUPM:

    In your Unity project directory, run:
        openupm add com.google.ads.mobile

    This command will add Google Mobile Ads to your project. If you encounter issues, ensure Node.js and OpenUPM are installed correctly.

2. Import the Autech Admob Mediation Unity Ads Package

After setting up OpenUPM and adding Google Mobile Ads, proceed with importing the .unitypackage:

    Download the Latest Unity Package:
        Download the latest release of this package from the Releases section of this repository (e.g., release/v1.0.0/Autech-Admob-Mediation-UnityAds-v1.0.0.unitypackage).

    Import the Unity Package:
        Open your Unity project.
        Go to Assets > Import Package > Custom Package....
        Select the downloaded .unitypackage file and import it.

3. Drag and Drop the Prefab

To initialize AdMob in your project:

    Drag and drop the VerifyAndInitializeAdmob prefab into your scene.
    This prefab handles the verification and initialization of AdMob services.

4. Update Ad IDs

Currently, Ad IDs can be modified in the AdsManager script:

    Open AdsManager and locate the Ad ID fields.
    Replace the default Ad IDs with your own.

Note: In future versions, we aim to simplify access to Ad ID settings.
🛠️ Troubleshooting Mediation Issues

If you encounter issues with AdMob mediation or Unity Ads, follow these steps:
1. Delete the Existing Unity Ads Plugin

    Go to the following directory in your Unity project:

    Assets/UnityAds

    Delete the folder to remove the existing Unity Ads plugin.

2. Download the Latest Unity Ads Plugin

    Go to the official Unity Ads Mediation documentation: Unity Ads Mediation Plugin Changelog
    Download the latest version of the Unity Ads plugin.

3. Re-import the Plugin

    Import the newly downloaded plugin back into your Unity project.

4. Verify the Integration

    Check your AdMob mediation settings on the Unity dashboard to confirm that Unity Ads is integrated correctly.


   Package Manager Import will be coming soon.
