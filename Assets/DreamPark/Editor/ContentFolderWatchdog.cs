using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DreamPark
{
    [InitializeOnLoad]
    public static class ContentFolderWatchdog
    {
        /// <summary>Fires after file changes settle, passing a list of changed absolute file paths.</summary>
        public static event Action<List<string>> OnContentFilesChanged;

        private static FileSystemWatcher _watcher;
        private static readonly HashSet<string> _pendingChanges = new();
        private static double _nextRunTime;
        private const double DebounceSeconds = 0.5;

        static ContentFolderWatchdog()
        {
            string contentRoot = Path.Combine(Application.dataPath, "Content");
            if (!Directory.Exists(contentRoot))
                return;

            // --- OS-level watcher ---
            _watcher = new FileSystemWatcher(contentRoot)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName |
                               NotifyFilters.DirectoryName |
                               NotifyFilters.LastWrite
            };

            _watcher.Changed += OnChanged;
            _watcher.Created += OnChanged;
            _watcher.Deleted += OnChanged;
            _watcher.Renamed += OnRenamed;

            // --- Unity-level fallback for imported/moved assets ---
            AssetPostprocessorWatcher.OnAssetChanged += path =>
            {
                lock (_pendingChanges)
                    _pendingChanges.Add(path);
                Debounce();
            };
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                return;

            lock (_pendingChanges)
                _pendingChanges.Add(e.FullPath.Replace("\\", "/"));

            Debounce();
        }

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            lock (_pendingChanges)
                _pendingChanges.Add(e.FullPath.Replace("\\", "/"));

            Debounce();
        }

        private static void Debounce()
        {
            EditorApplication.update -= CheckDebounce;
            _nextRunTime = EditorApplication.timeSinceStartup + DebounceSeconds;
            EditorApplication.update += CheckDebounce;
        }

        private static void CheckDebounce()
        {
            if (EditorApplication.timeSinceStartup < _nextRunTime)
                return;

            EditorApplication.update -= CheckDebounce;

            List<string> snapshot;
            lock (_pendingChanges)
            {
                snapshot = _pendingChanges.ToList();
                _pendingChanges.Clear();
            }

            if (snapshot.Count > 0)
                OnContentFilesChanged?.Invoke(snapshot);
        }

        /// <summary>
        /// Returns true if the last detected change was under the given path (e.g. "Assets/Content/GameName").
        /// </summary>
        public static bool LastChangeWasUnder(string assetRelativePath)
        {
            string absTarget = Path.GetFullPath(assetRelativePath).Replace("\\", "/");
            lock (_pendingChanges)
                return _pendingChanges.Any(p => p.StartsWith(absTarget, StringComparison.OrdinalIgnoreCase));
        }
    }

    // ---------------------------------------------------------------------
    // Unity import fallback (catches drag-drops, FBX imports, moves, etc.)
    // ---------------------------------------------------------------------
    internal class AssetPostprocessorWatcher : AssetPostprocessor
    {
        public static event Action<string> OnAssetChanged;

        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            void Handle(IEnumerable<string> assets)
            {
                foreach (string path in assets)
                {
                    if (!path.StartsWith("Assets/Content/", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string abs = Path.GetFullPath(path).Replace("\\", "/");
                    OnAssetChanged?.Invoke(abs);
                }
            }

            Handle(importedAssets);
            Handle(deletedAssets);
            Handle(movedAssets);
            Handle(movedFromAssetPaths);
        }
    }
}