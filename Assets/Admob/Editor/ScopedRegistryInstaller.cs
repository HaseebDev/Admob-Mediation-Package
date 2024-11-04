using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json.Linq;

[InitializeOnLoad]
public static class ScopedRegistryInstaller
{
    static ScopedRegistryInstaller()
    {
        AddScopedRegistryIfNeeded();
    }

    private static void AddScopedRegistryIfNeeded()
    {
        string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");

        if (!File.Exists(manifestPath))
        {
            Debug.LogError("manifest.json not found at: " + manifestPath);
            return;
        }

        var manifestJson = File.ReadAllText(manifestPath);
        var manifest = JObject.Parse(manifestJson);

        // Define the required scoped registry
        JObject openUpmRegistry = new JObject
        {
            { "name", "package.openupm.com" },
            { "url", "https://package.openupm.com" },
            { "scopes", new JArray("com.google.ads.mobile", "com.google.external-dependency-manager") }
        };

        // Retrieve existing scoped registries or create a new array
        var scopedRegistries = manifest["scopedRegistries"] as JArray;
        if (scopedRegistries == null)
        {
            scopedRegistries = new JArray();
            manifest["scopedRegistries"] = scopedRegistries;
        }

        // Check if the OpenUPM registry already exists
        bool registryExists = scopedRegistries
            .Children<JObject>()
            .Any(registry => registry["url"]?.ToString() == openUpmRegistry["url"]?.ToString());

        if (!registryExists)
        {
            scopedRegistries.Add(openUpmRegistry);
            File.WriteAllText(manifestPath, manifest.ToString());
            Debug.Log("OpenUPM scoped registry added successfully.");
        }
        else
        {
            Debug.Log("OpenUPM scoped registry already exists.");
        }
    }
}
