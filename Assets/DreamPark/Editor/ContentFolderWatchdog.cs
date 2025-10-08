using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

[InitializeOnLoad]
public static class ContentFolderWatchdog
{
    // 🚨 public event other scripts can subscribe to
    public static event Action OnContentFolderChanged;

    static ContentFolderWatchdog()
    {
        // fires whenever assets change
        AssetPostprocessorCallback.OnAnyAssetChange += HandleAssetChange;
    }

    private static void HandleAssetChange(string[] paths)
    {
        foreach (var path in paths)
        {
            if (path.StartsWith("Assets/Content/", StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"📂 Content folder changed: {path}");
                // fire the event
                OnContentFolderChanged?.Invoke();
                break;
            }
        }
    }

    // internal hook into asset pipeline
    private class AssetPostprocessorCallback : AssetPostprocessor
    {
        public static event Action<string[]> OnAnyAssetChange;

        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // broadcast any changed paths
            OnAnyAssetChange?.Invoke(importedAssets
                .Concat(deletedAssets)
                .Concat(movedAssets)
                .Concat(movedFromAssetPaths)
                .ToArray());
        }
    }
}