# Installation Guide – Autech AdMob Mediation (v2.1.0)

This package is delivered as a Unity Package Manager (UPM) Git dependency. Follow the steps below to bring the new architecture (thread-safe `AdsManager`, secure persistence, and Google UMP ➜ Unity Ads consent bridge) into your project.

---

## 1. Prerequisites

- **Unity** 2020.3 LTS or newer  
- **Git** installed on your machine (required for UPM Git imports)  
- **Google Mobile Ads Unity SDK** `com.google.ads.mobile` v10.4.2+  
- **Unity Ads Mediation Adapter** `com.google.ads.mobile.mediation.unity` v3.15.0+  
- **External Dependency Manager** `com.google.external-dependency-manager` (EDM4U)

> 📌 Install or update the Google packages first. The mediation package expects their assemblies to be present when compiling.
>
> 💡 After import, you’ll be prompted to auto-install the Google SDK and Unity Ads mediation adapter via `UnityEditor.PackageManager.Client.Add`. You can trigger the prompt later through **Tools ▸ Autech ▸ Install Google Dependencies**.

---

## 2. Install via Package Manager UI (Recommended)

1. Open **Window ▸ Package Manager**.
2. Click the **+** button ▸ **Add package from Git URL…**
3. Paste the repository URL:
   ```
   https://github.com/HaseebDev/Admob-Mediation-Package.git
   ```
4. Click **Add**. Unity will fetch the package, compile scripts, and import samples metadata.

---

## 3. Alternative Installation Methods

### Option A – Edit `Packages/manifest.json`

```jsonc
{
  "dependencies": {
    "com.google.ads.mobile": "10.4.2",
    "com.google.ads.mobile.mediation.unity": "3.15.0",
    "com.google.external-dependency-manager": "1.2.180",
    "com.autech.admob-mediation": "https://github.com/HaseebDev/Admob-Mediation-Package.git"
  }
}
```

Save the file and reopen the project; Unity will download the dependencies.

### Option B – OpenUPM CLI (quick dependency setup)

```bash
npm install -g openupm-cli
cd /path/to/your-unity-project
openupm add com.google.ads.mobile
openupm add com.google.ads.mobile.mediation.unity
```

After the dependencies are installed, add this package through the Package Manager Git URL as shown above.

---

## 4. Upgrading from Legacy `.unitypackage` Releases

1. Delete the old `Assets/Admob` (or equivalent) folder if you previously imported the unitypackage.
2. Remove legacy copies of `Assets/GoogleMobileAds/Mediation/UnityAds` and re-import the official adapter from Google if needed.
3. Clean out any obsolete prefabs; the new sample lives under `Packages/com.autech.admob-mediation/Samples~/Prefabs`.
4. Follow the UPM install steps; scripts will now be sourced from the package instead of `/Assets`.

---

## 5. Post-Install Checklist

1. **Configure AdMob** → `Assets ▸ Google Mobile Ads ▸ Settings`
2. **Import Samples** → Package Manager ▸ select **Autech AdMob Mediation Unity Ads** ▸ Samples ▸ **Import**
3. **Add Prefab** → Drag `Samples/Prefabs/VerifyandInitializeAdmob` into your scene
4. **Set IDs & Consent Flags** in the `VerifyAdmob` inspector
5. **Press Play** and use the sample UI (`AdsExampleUI`) to validate each ad format, Remove Ads persistence, and consent refresh

---

## 6. Troubleshooting

| Problem | Resolution |
|---------|------------|
| Package Manager reports “package cannot be found” | Ensure the prerequisite Google packages are installed; then retry the Git URL import. |
| Compilation errors referencing legacy scripts | Confirm the old `/Assets/Admob` folder has been removed and the project recompiled. |
| Unity Ads still shows personalized ads without consent | Call `AdsManager.Instance.RefreshMediationConsent()` after the Google UMP flow completes. |
| Remove Ads status not sticking between sessions | Verify `AdPersistenceManager.UseEncryptedStorage` and that `SecureStorage` logs a successful save; check PlayerPrefs permissions on the target platform. |
| Duplicate symbol or EDM conflicts | Update EDM4U to the latest version; delete `Assets/ExternalDependencyManager` before reinstalling if necessary. |

If you run into other issues, consult:
- [README.md](README.md) – architecture overview & usage examples  
- [KNOWN_ISSUES.md](KNOWN_ISSUES.md) – known problems and workarounds  
- [CHANGELOG.md](CHANGELOG.md) – what changed in v2.1.0  
- [GitHub Issues](https://github.com/HaseebDev/Admob-Mediation-Package/issues) – community support

---

You’re ready to use the v2.1.0 stack—enjoy the updated consent pipeline, secure persistence, and safer ad orchestration!
