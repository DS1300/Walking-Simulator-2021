using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WalkingSimulator.ProjectAssets
{
    [InitializeOnLoad]
    internal static class ProjectAssetStartup
    {
        static ProjectAssetStartup()
        {
            try
            {
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string path = Path.Combine(root, ".project-storage", "manifest.json");
                if (!File.Exists(path)) return;
                AssetManifest manifest = JsonUtility.FromJson<AssetManifest>(
                    "{\"assets\":" + File.ReadAllText(path) + "}");
                int restored = ProjectAssetRestore.Restore(root, manifest.assets, null);
                if (restored > 0)
                {
                    EditorApplication.delayCall += () => AssetDatabase.Refresh();
                    Debug.Log("Project assets are ready (" + restored + " files restored).");
                }
            }
            catch (Exception error)
            {
                Debug.LogError("Project assets could not be prepared: " + error.Message
                    + " Check that the complete repository, including .project-storage, was downloaded.");
            }
        }
    }
}
