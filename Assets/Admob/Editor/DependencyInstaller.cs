using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PackageManagerPackageInfo = UnityEditor.PackageManager.PackageInfo;
using PackageClient = UnityEditor.PackageManager.Client;
using PackageManagerStatusCode = UnityEditor.PackageManager.StatusCode;
using PackageManagerAddRequest = UnityEditor.PackageManager.Requests.AddRequest;

namespace Autech.Admob.EditorTools
{
    [InitializeOnLoad]
    internal static class DependencyInstaller
    {
        private const string DependencyStatusKey = "Autech.Admob.DependencyStatus.v2.1.0";
        private const string SessionStateKey = "Autech.Admob.StartupCheckCompleted";

        private const string GmaPackageId = "com.google.ads.mobile";
        private const string GmaRecommendedVersion = "10.6.0";
        private const string GmaGitUrl = "https://github.com/googleads/googleads-mobile-unity.git?path=packages/com.google.ads.mobile#v10.6.0";
        private const string GmaLegacyFolder = "Assets/GoogleMobileAds";

        private const string UnityMediationPackageId = "com.google.ads.mobile.mediation.unity";
        private const string UnityMediationRecommendedVersion = "3.16.1";
        private const string UnityMediationGitUrl = "https://github.com/googleads/googleads-mobile-unity-mediation.git?path=/UnityAds#UnityAds/v3.16.1";
        private const string UnityMediationLegacyFolder = "Assets/GoogleMobileAds/Mediation/UnityAds";

        private static readonly List<PackageManagerAddRequest> PendingRequests = new();
        private static bool s_hasQueuedInstall;
        private static bool s_postInstallCheckQueued;
        private static bool s_isInstalling;
        private static bool s_projectCheckQueued;

        private enum CheckReason
        {
            Auto,
            Manual,
            PostInstall
        }

        private struct PackageDescriptor
        {
            public string DisplayName;
            public string PackageId;
            public string RecommendedVersion;
            public string GitUrl;
            public string LegacyFolder;

            public PackageDescriptor(string displayName, string packageId, string recommendedVersion, string gitUrl, string legacyFolder)
            {
                DisplayName = displayName;
                PackageId = packageId;
                RecommendedVersion = recommendedVersion;
                GitUrl = gitUrl;
                LegacyFolder = legacyFolder;
            }
        }

        private struct PackageEvaluation
        {
            public PackageDescriptor Descriptor;
            public bool Installed;
            public bool VersionMatch;
            public string InstalledVersion;
            public bool LegacyInstall;

            public bool NeedsInstall => !VersionMatch;
        }

        private static readonly PackageDescriptor[] Descriptors =
        {
            new PackageDescriptor("Google Mobile Ads", GmaPackageId, GmaRecommendedVersion, GmaGitUrl, GmaLegacyFolder),
            new PackageDescriptor("Unity Ads Mediation Adapter", UnityMediationPackageId, UnityMediationRecommendedVersion, UnityMediationGitUrl, UnityMediationLegacyFolder)
        };

        static DependencyInstaller()
        {
            // Only run startup check once per Unity session
            // This prevents it from running on every script compilation or play mode change
            if (!SessionState.GetBool(SessionStateKey, false))
            {
                EditorApplication.update += OnFirstEditorUpdate;
            }

            // Always subscribe to project changes
            EditorApplication.projectChanged += OnProjectChanged;
        }

        [MenuItem("Tools/Autech/Install Google Dependencies", false, 0)]
        private static void ManualInstallMenu()
        {
            if (Application.isBatchMode)
            {
                Debug.LogWarning("[Autech.Admob] Unable to install dependencies while Unity is running in batch mode.");
                return;
            }

            CheckDependencies(CheckReason.Manual);
        }

        [MenuItem("Tools/Autech/Check Dependencies Status", false, 1)]
        private static void CheckDependenciesStatus()
        {
            var evaluations = EvaluatePackages();
            bool allRecommended = AllMatchRecommended(evaluations);
            
            string message = BuildStatusMessage(evaluations, includeLegend: false);
            
            if (allRecommended)
            {
                message += "\n\n✔ All dependencies are correctly installed!";
            }
            else
            {
                message += "\n\n⚠ Some dependencies need attention.";
            }
            
            EditorUtility.DisplayDialog("Dependency Status", message, "OK");
        }

        private static void OnFirstEditorUpdate()
        {
            EditorApplication.update -= OnFirstEditorUpdate;

            // Mark that we've completed the startup check for this Unity session
            SessionState.SetBool(SessionStateKey, true);

            if (Application.isBatchMode)
            {
                return;
            }

            CheckDependencies(CheckReason.Auto);
        }

        private static void CheckDependencies(CheckReason reason)
        {
            if (s_isInstalling && reason == CheckReason.Auto)
            {
                return;
            }

            var evaluations = EvaluatePackages();

            bool allRecommended = AllMatchRecommended(evaluations);

            if (allRecommended)
            {
                if (reason == CheckReason.Manual)
                {
                    ShowVerificationDialog(evaluations);
                }
                else
                {
                    Debug.Log("[Autech.Admob] All dependencies are correctly installed.");
                }
                return;
            }

            Debug.Log("[Autech.Admob] Dependency check triggered. Some dependencies need attention.");
            PromptInstallation(evaluations, reason);
        }

        private static void PromptInstallation(IReadOnlyList<PackageEvaluation> evaluations, CheckReason reason)
        {
            string title = "Google Mobile Ads dependencies";
            string message = BuildStatusMessage(evaluations, includeLegend: true);

            int choice = EditorUtility.DisplayDialogComplex(
                title,
                message,
                "Install Recommended",
                "Keep Current",
                "Cancel");

            switch (choice)
            {
                case 0: // Install
                    QueueInstallRequests(evaluations);
                    break;
                case 1: // Keep current
                    Debug.LogWarning("[Autech.Admob] Proceeding with current dependency versions. Some features may be untested with this configuration.");
                    break;
                default: // Cancel
                    if (reason == CheckReason.Manual)
                    {
                        Debug.Log("[Autech.Admob] Dependency installation cancelled.");
                    }
                    break;
            }
        }

        private static void ShowVerificationDialog(IReadOnlyList<PackageEvaluation> evaluations)
        {
            string message = BuildStatusMessage(evaluations, includeLegend: false) +
                             "\n\nAll dependencies match the recommended versions for this package.";

            EditorUtility.DisplayDialog("Google Mobile Ads dependencies", message, "OK");
            Debug.Log("[Autech.Admob] Google Mobile Ads dependencies verified.");
        }

        private static PackageEvaluation[] EvaluatePackages()
        {
            var result = new PackageEvaluation[Descriptors.Length];

            for (int i = 0; i < Descriptors.Length; i++)
            {
                var descriptor = Descriptors[i];
                var evaluation = new PackageEvaluation
                {
                    Descriptor = descriptor,
                    Installed = false,
                    VersionMatch = false,
                    InstalledVersion = "Missing",
                    LegacyInstall = false
                };

                try
                {
                    PackageManagerPackageInfo packageInfo =
                        PackageManagerPackageInfo.FindForAssetPath($"Packages/{descriptor.PackageId}");
                    if (packageInfo != null)
                    {
                        evaluation.Installed = true;
                        evaluation.InstalledVersion = string.IsNullOrEmpty(packageInfo.version)
                            ? "Unknown"
                            : packageInfo.version;
                        evaluation.VersionMatch = packageInfo.version == descriptor.RecommendedVersion;
                    }
                    else if (AssetDatabase.IsValidFolder(descriptor.LegacyFolder))
                    {
                        evaluation.Installed = true;
                        evaluation.LegacyInstall = true;
                        evaluation.InstalledVersion = "Legacy (Assets/)";
                        evaluation.VersionMatch = false;
                    }
                }
                catch
                {
                    // Ignore exceptions; treated as missing.
                }

                result[i] = evaluation;
            }

            return result;
        }

        private static bool AllMatchRecommended(IEnumerable<PackageEvaluation> evaluations)
        {
            foreach (var eval in evaluations)
            {
                if (!eval.VersionMatch)
                {
                    return false;
                }
            }

            return true;
        }

        private static string BuildStatusMessage(IReadOnlyList<PackageEvaluation> evaluations, bool includeLegend)
        {
            var lines = new List<string>();

            foreach (var eval in evaluations)
            {
                string status;
                if (!eval.Installed)
                {
                    status = "Missing";
                }
                else if (eval.LegacyInstall)
                {
                    status = "Legacy Assets install detected";
                }
                else
                {
                    status = $"Installed {eval.InstalledVersion}";
                }

                string recommended = eval.Descriptor.RecommendedVersion;
                string matchNote = eval.VersionMatch ? "✔ Matches recommended version" : "⚠ Needs update";

                if (eval.LegacyInstall)
                {
                    matchNote = "⚠ Replace with UPM package";
                }

                lines.Add($"{eval.Descriptor.DisplayName} ({eval.Descriptor.PackageId})\n  Status: {status}\n  Recommended: {recommended}\n  {matchNote}");
            }

            if (includeLegend)
            {
                lines.Add("\nThis package was tested with the versions listed under \"Recommended\". Installing those versions is strongly advised.");
            }

            return string.Join("\n\n", lines);
        }

        private static string BuildStatusSignature(IEnumerable<PackageEvaluation> evaluations) => string.Empty;

        private static void QueueInstallRequests(IReadOnlyList<PackageEvaluation> evaluations)
        {
            var toInstall = new List<(PackageDescriptor descriptor, string gitUrl)>();

            foreach (var eval in evaluations)
            {
                if (eval.NeedsInstall)
                {
                    toInstall.Add((eval.Descriptor, eval.Descriptor.GitUrl));
                }
            }

            if (toInstall.Count == 0)
            {
                Debug.Log("[Autech.Admob] All dependencies already match the recommended versions.");
                return;
            }

            PendingRequests.Clear();
            s_isInstalling = true;
            foreach (var item in toInstall)
            {
                var request = PackageClient.Add(item.gitUrl);
                PendingRequests.Add(request);
                Debug.Log($"[Autech.Admob] Installing {item.descriptor.DisplayName} ({item.descriptor.PackageId}) from {item.gitUrl}...");
            }

            s_hasQueuedInstall = true;
            s_postInstallCheckQueued = false;
            EditorApplication.update += MonitorInstallRequests;
        }

        private static void MonitorInstallRequests()
        {
            if (!s_hasQueuedInstall)
            {
                EditorApplication.update -= MonitorInstallRequests;
                return;
            }

            for (int i = PendingRequests.Count - 1; i >= 0; i--)
            {
                var request = PendingRequests[i];
                if (!request.IsCompleted)
                {
                    continue;
                }

                if (request.Status == PackageManagerStatusCode.Success)
                {
                    var packageId = request.Result?.name ?? "unknown package";
                    var version = request.Result?.version ?? "unknown version";
                    Debug.Log($"[Autech.Admob] Installed dependency: {packageId} ({version})");
                }
                else if (request.Status >= PackageManagerStatusCode.Failure)
                {
                    Debug.LogError($"[Autech.Admob] Failed to install dependency: {request.Error?.message ?? "Unknown error"}");
                }

                PendingRequests.RemoveAt(i);
            }

            if (PendingRequests.Count == 0)
            {
                s_hasQueuedInstall = false;
                EditorApplication.update -= MonitorInstallRequests;

                s_isInstalling = false;

                // Force re-evaluation after installs complete.
                if (!s_postInstallCheckQueued)
                {
                    s_postInstallCheckQueued = true;
                    EditorApplication.delayCall += () =>
                    {
                        s_postInstallCheckQueued = false;
                        CheckDependencies(CheckReason.PostInstall);
                    };
                }
            }
        }

        private static void OnProjectChanged()
        {
            if (s_isInstalling)
            {
                return;
            }

            if (s_projectCheckQueued)
            {
                return;
            }

            s_projectCheckQueued = true;
            EditorApplication.delayCall += () =>
            {
                s_projectCheckQueued = false;
                CheckDependencies(CheckReason.Auto);
            };
        }
    }
}
