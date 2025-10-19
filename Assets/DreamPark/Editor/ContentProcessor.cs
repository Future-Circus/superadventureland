using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.Collections.Generic;
using System;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using System.Text;
using System.Text.RegularExpressions;

namespace DreamPark {
    [InitializeOnLoad]
    public static class ContentProcessor
    {
        private const string kLastFingerprintKey = "DreamPark.ContentProcessor.lastFingerprint";

        static ContentProcessor()
        {
            // Subscribe to file change events from the Watchdog
            ContentFolderWatchdog.OnContentFilesChanged += OnContentFilesChanged;
        }

      [InitializeOnLoadMethod]
        private static void RunOnStartup()
        {
            // Only run once per Unity session
            if (SessionState.GetBool("DreamPark_RanOnStartup", false))
                return;

            SessionState.SetBool("DreamPark_RanOnStartup", true);

            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling)
                {
                    Debug.Log("🪄 Auto-running AssignAllGameIds on Editor startup (first time this session)...");
                    AssignAllGameIds();
                    EnforceContentNamespaces();
                }
            };
        }

        // ---------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------

        private static string GetGameFolderName()
        {
            string[] possibleContentPaths = Directory.GetDirectories("Assets", "Content", SearchOption.AllDirectories);
            foreach (string contentPath in possibleContentPaths)
            {
                var subdirs = Directory.GetDirectories(contentPath);
                if (subdirs.Length > 0)
                {
                    string folderName = Path.GetFileName(subdirs[0]);
                    if (!string.IsNullOrEmpty(folderName))
                        return folderName;
                }
            }
            return "YOUR_GAME_HERE";
        }

        public static string GetGamePrefix()
        {
            return Sanitize(GetGameFolderName());
        }

        private static string Sanitize(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c.ToString(), "");
            return name.Replace("[", "").Replace("]", "").Trim();
        }

        private static void EnsureGlobalLabel(AddressableAssetSettings settings, string gameId)
        {
            settings?.AddLabel(gameId);
        }

        // ---------------------------------------------------------------------
        // Manual run entry point
        // ---------------------------------------------------------------------
        [MenuItem("DreamPark/Tools/Force Update Game Assets")]
        public static void AssignAllGameIds()
        {
            Debug.Log("🔄 Assigning all game IDs...");
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            string gamePrefix = GetGameFolderName();
            string contentRoot = $"Assets/Content/{gamePrefix}";
            if (!AssetDatabase.IsValidFolder(contentRoot))
            {
                Debug.LogWarning($"⚠️ No folder found at {contentRoot}");
                return;
            }

            Debug.Log($"🔄 Force updating all prefabs and addressables for {gamePrefix}...");

            // Process all prefabs under this content folder
            string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { contentRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();

            UpdateSpecificPrefabs(allPrefabs.ToList(), gamePrefix);
            ApplyGameIdLabelToContentEntries(AddressableAssetSettingsDefaultObject.Settings, gamePrefix);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("✅ Finished manual full content update.");
        }

        // ---------------------------------------------------------------------
        // Incremental change handler
        // ---------------------------------------------------------------------
        private static void OnContentFilesChanged(List<string> changedFiles)
        {
            if (changedFiles == null || changedFiles.Count == 0)
                return;

            string gamePrefix = GetGameFolderName();
            string contentRoot = $"Assets/Content/{gamePrefix}";
            if (!AssetDatabase.IsValidFolder(contentRoot))
                return;

            // ✅ Cache the Addressable settings once
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return;

            // ✅ Local helper to convert absolute → Unity-relative paths safely
            static string ToAssetPath(string absolutePath)
            {
                if (string.IsNullOrEmpty(absolutePath) || !absolutePath.StartsWith(Application.dataPath))
                    return null;

                string rel = Path.GetRelativePath(Application.dataPath, absolutePath)
                    .Replace("\\", "/");
                return "Assets/" + rel;
            }

            // ✅ Precompute all Unity asset paths once
            var assetPaths = changedFiles
                .Select(ToAssetPath)
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct()
                .ToList();

            if (assetPaths.Count == 0)
                return;

            // ✅ Separate by type using extension filters (fast, not LoadAssetAtPath)
            var prefabPaths = assetPaths
                .Where(p => p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var scriptPaths = assetPaths
                .Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var otherAssets = assetPaths
                .Except(prefabPaths)
                .Except(scriptPaths)
                .Where(p => !AssetDatabase.IsValidFolder(p))
                .ToList();

            // ✅ Handle prefabs first
            if (prefabPaths.Count > 0)
            {
                UpdateSpecificPrefabs(prefabPaths, gamePrefix);
                ApplyGameIdLabelToContentEntries(settings, gamePrefix, prefabPaths);
            }

            // ✅ Handle other addressable assets (textures, FBX, etc.)
            if (otherAssets.Count > 0)
            {
                ApplyGameIdLabelToContentEntries(settings, gamePrefix, otherAssets);
            }

            // ✅ Enforce namespaces for scripts (optional but good)
            if (scriptPaths.Count > 0)
            {
                EnforceContentNamespaces(scriptPaths);
            }

            // ✅ Save + refresh once
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"🔄 Processed {prefabPaths.Count} prefabs, {otherAssets.Count} assets, {scriptPaths.Count} scripts for {gamePrefix}");
        }

        // ---------------------------------------------------------------------
        // Prefab Updates (incremental)
        // ---------------------------------------------------------------------
        private static void UpdateSpecificPrefabs(List<string> prefabPaths, string gameId)
        {
            if (prefabPaths == null || prefabPaths.Count == 0) return;

            AssetDatabase.StartAssetEditing();
            int modified = 0;

            try
            {
                foreach (var path in prefabPaths)
                {
                    if (string.IsNullOrEmpty(path) || !File.Exists(path))
                        continue;

                    using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
                    {
                        var root = scope.prefabContentsRoot;
                        bool any = false;
                        foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                        {
                            if (mb == null) continue;
                            var f = mb.GetType().GetField("gameId");
                            if (f != null && f.FieldType == typeof(string))
                            {
                                if (!string.Equals((string)f.GetValue(mb), gameId, StringComparison.Ordinal))
                                {
                                    f.SetValue(mb, gameId);
                                    EditorUtility.SetDirty(mb);
                                    any = true;
                                }
                            }
                        }

                        if (any)
                        {
                            PrefabUtility.SaveAsPrefabAsset(scope.prefabContentsRoot, path);
                            modified++;
                        }
                    }
                }

                if (modified > 0)
                    Debug.Log($"🧩 Updated {modified} prefab(s) for gameId={gameId}.");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        private static bool ShouldSkipAsset(string assetPath)
        {
            if (!assetPath.Contains("/ThirdParty/") && !assetPath.Contains("\\ThirdParty\\"))
                return false;

            if (string.IsNullOrEmpty(assetPath) || assetPath.EndsWith(".meta"))
                return true;

            var main = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (main == null)
                return true;

            // Skip if the main asset itself is flagged
            if ((main.hideFlags & (HideFlags.DontSave | HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset)) != 0)
            {
                Debug.LogWarning($"⏭️ Skipped (HideFlags) asset: {assetPath}");
                return true;
            }

            // For prefabs, check all serialized references
            if (main is GameObject go)
            {
                foreach (var c in go.GetComponentsInChildren<Component>(true))
                {
                    if (c == null) continue;
                    if ((c.hideFlags & (HideFlags.DontSave | HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset)) != 0)
                    {
                        Debug.LogWarning($"⏭️ Skipped prefab with DontSave component: {assetPath}");
                        return true;
                    }

                    // NEW: scan serialized fields for hidden refs
                    using (var so = new SerializedObject(c))
                    {
                        var prop = so.GetIterator();
                        while (prop.NextVisible(true))
                        {
                            if (prop.propertyType == SerializedPropertyType.ObjectReference)
                            {
                                var obj = prop.objectReferenceValue;
                                if (obj != null && (obj.hideFlags & (HideFlags.DontSave | HideFlags.HideAndDontSave)) != 0)
                                {
                                    //Debug.LogWarning($"⏭️ Skipped prefab with hidden DontSave reference ({obj.name}) in {assetPath}");
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static List<string> disallowedExtensionsList = new List<string> {
            ".meta",
            ".cs",
            ".unity",
            ".blend",
            ".blend1",
            ".js",
            ".boo",
            ".asmdef",
            ".asmref",
            ".dll",
            ".pdb",
            ".mdb",
            ".sln",
            ".csproj",
            ".buildreport",
            ".assetstore",
            ".log",
            ".tmp",
            ".max",
            ".ma",
            ".mb",  
            ".c4d",
            ".psd",
            ".ai",
            ".svg",
            ".unitypackage",
            ".zip",
            ".7z",
            ".gz",
            ".rar",
            ".tar",
            ".hdr",
            ".so",
            ".pdf",
            ".exe",
            ".app",
            ".apk",
            ".aab",
            ".ipa",
            ".so",
            ".bundle",
            ".framework",
            ".dylib"
        };

        // ---------------------------------------------------------------------
        // Addressable Updates (targeted)
        // ---------------------------------------------------------------------
        private static void ApplyGameIdLabelToContentEntries(AddressableAssetSettings settings, string gameId, List<string> specificPaths = null)
        {
            if (settings == null) return;

            EnsureGlobalLabel(settings, gameId);

            IEnumerable<string> assetPaths;
            if (specificPaths != null && specificPaths.Count > 0)
            {
                assetPaths = specificPaths;
            }
            else
            {
                string contentRoot = $"Assets/Content/{gameId}/";
                assetPaths = AssetDatabase.FindAssets("", new[] { contentRoot })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(p => !string.IsNullOrEmpty(p) && !AssetDatabase.IsValidFolder(p))
                    .Where(p => !disallowedExtensionsList.Any(p.EndsWith))
                    .Where(p => !ShouldSkipAsset(p));

                try {
                    // Collect allowed assets first
                    var validAssets = AssetDatabase.FindAssets("", new[] { contentRoot })
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Where(p => !string.IsNullOrEmpty(p))
                        .Where(p => !AssetDatabase.IsValidFolder(p))
                        .Where(p => !disallowedExtensionsList.Any(ext => p.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                        .Where(p => !ShouldSkipAsset(p))
                        .ToHashSet(); // prevent duplicates

                    // Then selectively remove only invalid entries
                    foreach (var group in settings.groups.Where(g => g != null))
                    {
                        foreach (var e in group.entries.ToList())
                        {
                            string path = AssetDatabase.GUIDToAssetPath(e.guid);
                            if (!validAssets.Contains(path))
                            {
                                settings.RemoveAssetEntry(e.guid);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error removing invalid assets from content entries: {e.Message}");
                }
            }

            int labeled = 0, moved = 0;
            foreach (var assetPath in assetPaths)
            {
                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid)) continue;

                var rel = assetPath.Replace("\\", "/");
                int idx = rel.IndexOf($"{gameId}/");
                if (idx < 0) continue;

                var subPath = rel.Substring(idx + gameId.Length + 1);
                var firstSlash = subPath.IndexOf('/');
                string folderName = firstSlash > 0 ? subPath.Substring(0, firstSlash) : "Root";
                string groupName = $"{Sanitize(gameId)}-{folderName}";

                var group = settings.groups.FirstOrDefault(g => g != null && g.Name == groupName)
                    ?? settings.CreateGroup(groupName, false, false, true,
                        new List<AddressableAssetGroupSchema> {
                            (AddressableAssetGroupSchema)Activator.CreateInstance(typeof(BundledAssetGroupSchema)),
                            (AddressableAssetGroupSchema)Activator.CreateInstance(typeof(ContentUpdateGroupSchema))
                        });

                var bag = group.GetSchema<BundledAssetGroupSchema>() ?? group.AddSchema<BundledAssetGroupSchema>();
                bag.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
                bag.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
                bag.UseAssetBundleCache = true;
                bag.UseAssetBundleCrc = true;
                bag.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
                bag.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;

                var entry = settings.FindAssetEntry(guid);
                if (entry == null || entry.parentGroup != group)
                {
                    entry = settings.CreateOrMoveEntry(guid, group, false, false);
                    moved++;
                }

                var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                string extension = Path.GetExtension(assetPath).ToLowerInvariant();
                string typeFolder = null;

                // ✅ Handle special cases first
                if (extension == ".fbx" || extension == ".obj" || extension == ".dae") {
                    typeFolder = "Models";
                } else if (extension == ".asset") {
                    typeFolder = "Assets";
                } else if (assetType == typeof(AudioClip))
                    typeFolder = "Audio";
                else if (assetType == typeof(Texture) || assetType == typeof(Texture2D) || assetType == typeof(RenderTexture))
                    typeFolder = "Textures";
                else if (assetType == typeof(Material))
                    typeFolder = "Materials";
                else if (assetType == typeof(Shader) || extension == ".shadergraph")
                    typeFolder = "Shaders";
                else if (assetType == typeof(AnimationClip))
                    typeFolder = "Animations";

                string desiredAddress;
                if (typeFolder == null && assetType == typeof(GameObject))
                    desiredAddress = $"{gameId}/{Path.GetFileNameWithoutExtension(assetPath)}";
                else if (!string.IsNullOrEmpty(typeFolder))
                    desiredAddress = $"{gameId}/{typeFolder}/{Path.GetFileNameWithoutExtension(assetPath)}";
                else
                    desiredAddress = assetPath;

                if (entry.address != desiredAddress)
                        entry.address = desiredAddress;

                if (!entry.labels.Contains(gameId))
                {
                    entry.SetLabel(gameId, true, true);
                    labeled++;
                }
            }

            if (labeled > 0 || moved > 0)
                Debug.Log($"🏷 Addressables: {moved} moved/created, {labeled} labeled for '{gameId}'.");
        }

        [MenuItem("DreamPark/Tools/Enforce Content Namespaces")]
        public static void EnforceContentNamespaces()
        {
            EnforceContentNamespaces(null);
        }
        public static void EnforceContentNamespaces(List<string> specificPaths = null)
        {
            string root = Path.Combine(Application.dataPath, "Content");
            if (!Directory.Exists(root))
            {
                Debug.LogError("❌ No Assets/Content folder found.");
                return;
            }

            string[] csFiles = (specificPaths != null && specificPaths.Count > 0 ? specificPaths.ToArray() : Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
                .Select(f => f.Replace('\\', '/').ToLowerInvariant())
                .Where(f => !f.Contains("/thirdparty/"))
                .ToArray();

            int modified = 0, skipped = 0;

            foreach (string sysPath in csFiles)
            {
                string path = sysPath.Replace("\\", "/");

                if (path.Contains("/Editor/") || path.Contains("/Generated/"))
                {
                    skipped++;
                    continue;
                }

                string[] parts = path.Split('/');
                if (parts.Length < 4) { skipped++; continue; }

                string gameId = parts[parts.ToList().IndexOf("Content") + 1];
                string relative = path.Substring(path.IndexOf($"{gameId}/") + gameId.Length + 1);
                string folderStructure = Path.GetDirectoryName(relative)?.Replace("\\", "/") ?? "";
                string expectedNamespace = string.IsNullOrEmpty(folderStructure)
                    ? gameId
                    : $"{gameId}.{folderStructure.Replace("/", ".")}";
                expectedNamespace = SanitizeNamespace(expectedNamespace);

                // read file text with normalized newlines
                string text = File.ReadAllText(path, Encoding.UTF8)
                    .Replace("\r\n", "\n")
                    .Replace("\r", "\n");
                    
                var nsPatternCheck = new Regex(@"^\s*namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Multiline);

                var match = nsPatternCheck.Match(text);
                if (match.Success)
                {
                    string existingNamespace = match.Groups[1].Value.Trim();
                    if (existingNamespace == expectedNamespace)
                    {
                        // ✅ Namespace already correct — skip rewriting
                        skipped++;
                        continue;
                    }
                }

                // strip existing namespace wrapper if present
                var nsPattern = new System.Text.RegularExpressions.Regex(
                    @"^\s*namespace\s+[A-Za-z0-9_.]+\s*\{([\s\S]*)\}\s*$",
                    System.Text.RegularExpressions.RegexOptions.Multiline);

                if (nsPattern.IsMatch(text))
                {
                    string existingNamespace = nsPattern.Match(text).Groups[1].Value.Trim();
                    if (existingNamespace == expectedNamespace)
                    {
                        // ✅ Already correct, skip rewriting
                        skipped++;
                        continue;
                    }
                    string inner = nsPattern.Match(text).Groups[1].Value;
                    var innerLines = inner.Split('\n')
                        .Select(l => l.StartsWith("    ") ? l.Substring(4) : l)
                        .ToList();

                    while (innerLines.Count > 0 && string.IsNullOrWhiteSpace(innerLines[0])) innerLines.RemoveAt(0);
                    while (innerLines.Count > 0 && string.IsNullOrWhiteSpace(innerLines[^1])) innerLines.RemoveAt(innerLines.Count - 1);
                    text = string.Join("\n", innerLines);
                }

                // wrap with clean namespace
                var sb = new StringBuilder();
                sb.AppendLine($"namespace {expectedNamespace}");
                sb.AppendLine("{");
                string[] rawLines = text.Split('\n');
                foreach (var line in rawLines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        sb.AppendLine();
                    else
                        sb.AppendLine("    " + line.TrimEnd());
                }
                sb.AppendLine("}");

                string final = Regex.Replace(sb.ToString(), @"\n{3,}", "\n\n");

                if (final.Contains("class CoreExtensionsInterface"))
                {
                    string pattern = @"(\[.*?\]\s*)?(public\s*)?(static\s+string\s+gameId\s*=\s*)(?:""[^""]*""|string\.Empty|null|[^;\n]*)";
                    string replacement = $"${{1}}${{2}}${{3}}\"{gameId}\"";
                    string updated = Regex.Replace(final, pattern, replacement);

                    final = updated;
                    Debug.Log($"updatedupdatedupdatedupdatedupdated: {updated}");
                }

                File.WriteAllText(path, final.TrimEnd() + "\n", Encoding.UTF8);

                Debug.Log($"🧩 Ensured single namespace '{expectedNamespace}' in {path}");
                modified++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"✅ Namespace enforcement complete. Modified {modified}, skipped {skipped}.");
        }

        private static string SanitizeNamespace(string ns)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (var c in invalid)
                ns = ns.Replace(c.ToString(), "");
            ns = ns.Replace(" ", "").Replace("__", "_").Replace("..", ".");
            return ns;
        }

        [MenuItem("DreamPark/Tools/Scan Script Usage")]
        public static void Scan()
        {
            string[] scriptGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/Content" });
            var scriptPaths = scriptGuids.Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var used = new HashSet<string>();

            string[] assetGuids = AssetDatabase.FindAssets("t:Object");
            int checkedCount = 0;

            foreach (var guid in assetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".cs") || AssetDatabase.IsValidFolder(path)) continue;

                var deps = AssetDatabase.GetDependencies(path, recursive: true);
                foreach (var dep in deps)
                {
                    if (dep.EndsWith(".cs"))
                        used.Add(dep);
                }

                checkedCount++;
            }

            Debug.Log($"🔍 Scanned {checkedCount} assets for script dependencies.");

            // --- Log all unused scripts individually ---
            int unusedCount = 0;
            foreach (string scriptPath in scriptPaths)
            {
                if (!used.Contains(scriptPath))
                {
                    Debug.LogWarning($"🗑 Unused script detected: {scriptPath}");
                    unusedCount++;
                }
            }

            Debug.Log($"✅ Script usage scan complete. Found {unusedCount} unused script(s).");
        }
    }
}