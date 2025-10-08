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

namespace DreamPark {
    [InitializeOnLoad]
    public static class ContentProcessor
    {
        static ContentProcessor()
        {
            // Automatically run after compilation or domain reload
            EditorApplication.delayCall += AssignAllGameIds;

            // 🔔 subscribe to folder changes
            ContentFolderWatchdog.OnContentFolderChanged += AssignAllGameIds;
        }

        // ---------------- HELPERS ----------------

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

        // Replace anything before the first dash with our prefix (idempotent)
        private static string AddPrefix(string name)
        {
            string prefix = GetGamePrefix();
            int dash = name.IndexOf('-');
            if (dash >= 0 && dash + 1 < name.Length)
                name = name.Substring(dash + 1);
            return prefix + "-" + Sanitize(name);
        }

        // Try to read gameId from the asset path (Content/<GameName>/...), fallback to GetGamePrefix
        private static string ExtractGameIdFromPath(string assetPath)
        {
            var parts = assetPath.Replace("\\", "/").Split('/');
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (parts[i] == "Content")
                {
                    return Sanitize(parts[i + 1]);
                }
            }
            return GetGamePrefix();
        }

        // Ensure Addressables has a global label entry for this gameId
    private static void EnsureGlobalLabel(AddressableAssetSettings settings, string gameId)
    {
        // AddLabel returns false if it already exists; safe & idempotent
        settings.AddLabel(gameId);
    }

    // Apply the gameId label to ALL addressable entries whose asset lives under Assets/Content/<gameId>/**
    private static void ApplyGameIdLabelToContentEntries(AddressableAssetSettings settings, string gameId)
    {
        string contentRoot = $"Assets/Content/{gameId}/";
        EnsureGlobalLabel(settings, gameId);

        int labeled = 0;
        foreach (var group in settings.groups.Where(g => g != null))
        {
            // AddressableAssetGroup.entries is safe to enumerate
            foreach (var entry in group.entries.ToList())
            {
                var path = AssetDatabase.GUIDToAssetPath(entry.guid).Replace("\\", "/");
                if (string.IsNullOrEmpty(path) || !path.StartsWith(contentRoot)) continue;

                // add gameId label, remove placeholder if you used one
                entry.SetLabel(gameId, true, true);
                entry.SetLabel("YOUR_GAME_HERE", false, true);
                labeled++;
            }
        }

        if (labeled > 0)
        {
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, null, true);
            Debug.Log($"🏷  Applied label '{gameId}' to {labeled} entry(ies) under {contentRoot}");
        }
    }

    [MenuItem("DreamPark/Tools/Build Addressable Groups")]
        public static void BuildGroups()
        {
            // ----- 1. Locate Content root -----
            string contentRoot = "Assets/Content";
            if (!AssetDatabase.IsValidFolder(contentRoot))
            {
                Debug.LogError("❌ No folder found at Assets/Content/");
                return;
            }

            string[] gameDirs = Directory.GetDirectories(contentRoot);
            if (gameDirs.Length == 0)
            {
                Debug.LogError("❌ No sub-folders under Assets/Content/");
                return;
            }

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("❌ AddressableAssetSettings not found.");
                return;
            }

            string remoteBuildPath = settings.profileSettings.GetValueByName(settings.activeProfileId, "RemoteBuildPath");
            string remoteLoadPath = settings.profileSettings.GetValueByName(settings.activeProfileId, "RemoteLoadPath");

            Debug.Log($"🌐 Active profile remote paths:\nBuild: {remoteBuildPath}\nLoad:  {remoteLoadPath}");

            // ----- 3. Process each game folder -----
            foreach (string gameDir in gameDirs)
            {
                string gameName = Path.GetFileName(gameDir);
                string gamePrefix = Sanitize(gameName);

                string[] subfolders = Directory.GetDirectories(gameDir);
                foreach (string folder in subfolders)
                {
                    string folderName = Path.GetFileName(folder);
                    string groupName = $"{gamePrefix}-{folderName}";

                    // Find or create the group
                    var group = settings.groups.FirstOrDefault(g => g != null && g.Name == groupName);
                    if (group == null)
                    {
                        group = settings.CreateGroup(groupName, false, false, true,
                            new List<AddressableAssetGroupSchema>(new System.Type[] { typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema) }.Select(t => (AddressableAssetGroupSchema)Activator.CreateInstance(t))));
                        Debug.Log($"📦 Created group: {groupName}");
                    }

                    // ----- 4. Configure as Remote -----
                    var bag = group.GetSchema<BundledAssetGroupSchema>() ?? group.AddSchema<BundledAssetGroupSchema>();
                    bag.BuildPath.SetVariableByName(settings, "RemoteBuildPath");
                    bag.LoadPath.SetVariableByName(settings, "RemoteLoadPath");
                    bag.UseAssetBundleCache = true;
                    bag.UseAssetBundleCrc = true;
                    bag.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
                    bag.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
                    EditorUtility.SetDirty(bag);
                    Debug.Log($"🌐 Set {groupName} to Remote paths");

                    // ----- 5. Add all assets in folder -----
                    string[] assetGuids = AssetDatabase.FindAssets("", new[] { folder });
                    foreach (string guid in assetGuids)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        if (assetPath.EndsWith(".meta") || assetPath.EndsWith(".cs") || assetPath.EndsWith(".unity") || AssetDatabase.IsValidFolder(assetPath))
                            continue;

                        AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);

                        entry.address = Path.GetFileNameWithoutExtension(assetPath);
                        foreach (var label in entry.labels.ToList())
                            entry.SetLabel(label, false, false);

                        entry.SetLabel(gamePrefix, true, true);
                    }

                    EditorUtility.SetDirty(group);
                }
            }

            // ----- 6. Save and refresh -----
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorApplication.RepaintProjectWindow();

            Debug.Log("✅ Finished building and configuring remote Addressable groups.");
        }

        // Ensure variable exists in Addressables profile
        private static void EnsureProfileVariable(AddressableAssetSettings settings, string varName, string defaultValue)
        {
            var profileSettings = settings.profileSettings;
            if (profileSettings.GetVariableNames().Contains(varName))
                return;

            profileSettings.CreateValue(varName, defaultValue);
            profileSettings.SetValue(settings.activeProfileId, varName, defaultValue);
            Debug.Log($"➕ Created Addressables profile variable: {varName} = {defaultValue}");
        }

        // Addressables RemoveGroup compatibility across versions
        private static void RemoveGroupCompat(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            var t = typeof(AddressableAssetSettings);
            var withBool = t.GetMethod("RemoveGroup", new[] { typeof(AddressableAssetGroup), typeof(bool) });
            if (withBool != null) { withBool.Invoke(settings, new object[] { group, true }); return; }

            var noBool = t.GetMethod("RemoveGroup", new[] { typeof(AddressableAssetGroup) });
            if (noBool != null) { noBool.Invoke(settings, new object[] { group }); return; }

            // Fallback cleanup if asset still exists
            string path = AssetDatabase.GetAssetPath(group);
            if (!string.IsNullOrEmpty(path) && AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                AssetDatabase.DeleteAsset(path);
        }

        private static AddressableAssetGroup CloneGroup(AddressableAssetSettings settings, AddressableAssetGroup oldGroup, string newName)
        {
            var schemaTypes = oldGroup.Schemas.Select(s => s.GetType()).ToArray();
            var newGroup = settings.CreateGroup(newName, false, false, true, new List<AddressableAssetGroupSchema>(schemaTypes.Select(t => (AddressableAssetGroupSchema)Activator.CreateInstance(t))));

            for (int i = 0; i < oldGroup.Schemas.Count; i++)
            {
                var src = oldGroup.Schemas[i];
                var dst = newGroup.Schemas[i];
                if (src && dst)
                    EditorUtility.CopySerialized(src, dst);
            }

            EditorUtility.SetDirty(newGroup);
            return newGroup;
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        [MenuItem("DreamPark/Tools/Enforce Content Namespaces")]
        public static void EnforceContentNamespaces()
        {
            string root = Path.Combine(Application.dataPath, "Content");
            if (!Directory.Exists(root))
            {
                Debug.LogError("❌ No Assets/Content folder found.");
                return;
            }

            string[] csFiles = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);
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

                // read file as one string with normalized newlines
                string text = File.ReadAllText(path, Encoding.UTF8)
                                .Replace("\r\n", "\n")
                                .Replace("\r", "\n");

                // --- strip existing namespace wrapper if present ---
                var nsPattern = new System.Text.RegularExpressions.Regex(
                    @"^\s*namespace\s+[A-Za-z0-9_.]+\s*\{([\s\S]*)\}\s*$",
                    System.Text.RegularExpressions.RegexOptions.Multiline);
                if (nsPattern.IsMatch(text))
                {
                    string inner = nsPattern.Match(text).Groups[1].Value;
                    // remove one indentation level and trim extra blank lines
                    var innerLines = inner.Split('\n')
                        .Select(l => l.StartsWith("    ") ? l.Substring(4) : l)
                        .ToList();
                    // trim leading/trailing empties
                    while (innerLines.Count > 0 && string.IsNullOrWhiteSpace(innerLines[0])) innerLines.RemoveAt(0);
                    while (innerLines.Count > 0 && string.IsNullOrWhiteSpace(innerLines[^1])) innerLines.RemoveAt(innerLines.Count - 1);
                    text = string.Join("\n", innerLines);
                }

                // --- build final wrapped script with clean spacing ---
                var sb = new StringBuilder();
                sb.AppendLine($"namespace {expectedNamespace}");
                sb.AppendLine("{");

                string[] rawLines = text.Split('\n');
                foreach (var line in rawLines)
                {
                    // keep blank lines exactly once
                    if (string.IsNullOrWhiteSpace(line))
                        sb.AppendLine();
                    else
                        sb.AppendLine("    " + line.TrimEnd());
                }

                sb.AppendLine("}");

                // collapse any >2 consecutive blank lines
                string final = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"\n{3,}", "\n\n");

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

    private static void ProcessContentPrefabsOnly()
        {
            string gamePrefix = GetGameFolderName();
            string contentRoot = $"Assets/Content/{gamePrefix}";
            if (!AssetDatabase.IsValidFolder(contentRoot))
            {
                Debug.LogWarning($"⚠️ No folder found at {contentRoot}");
                return;
            }

            // Get all prefabs only under this game's Content folder
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { contentRoot });
            int changed = 0, renamedFiles = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                GameObject prefabRoot = null;
                try { prefabRoot = PrefabUtility.LoadPrefabContents(path); }
                catch { continue; }

                if (prefabRoot == null) continue;

                // New root name
                string desiredRootName = AddPrefix(prefabRoot.name);
                string gameIdForPrefab = gamePrefix;

                // Rename root GO if needed
                if (prefabRoot.name != desiredRootName)
                {
                    prefabRoot.name = desiredRootName;
                    changed++;
                }

                // Assign gameId fields inside prefab
                var components = prefabRoot.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var mb in components)
                {
                    if (mb == null) continue;
                    var field = mb.GetType().GetField("gameId");
                    if (field != null && field.FieldType == typeof(string))
                    {
                        field.SetValue(mb, gameIdForPrefab);
                        EditorUtility.SetDirty(mb);
                    }
                }

                // Save and unload prefab
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                PrefabUtility.UnloadPrefabContents(prefabRoot);

                // Rename prefab asset file to match root name (optional, safe)
                string currentFileName = Path.GetFileNameWithoutExtension(path);
                if (currentFileName != desiredRootName)
                {
                    string newPath = Path.Combine(Path.GetDirectoryName(path)!, desiredRootName + ".prefab").Replace("\\", "/");
                    if (!AssetDatabase.AssetPathExists(newPath))
                    {
                        string err = AssetDatabase.MoveAsset(path, newPath);
                        if (string.IsNullOrEmpty(err))
                        {
                            renamedFiles++;
                            path = newPath;
                        }
                        else Debug.LogWarning($"⚠️ Could not rename prefab asset '{currentFileName}' → '{desiredRootName}': {err}");
                    }
                }
            }

            if (changed > 0 || renamedFiles > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"🧩 Updated prefabs inside {contentRoot}. Root renames: {changed}, file renames: {renamedFiles}");
        }

        // ---------------- MAIN ----------------

        [MenuItem("DreamPark/Tools/Force Update Game ID")]
        static void AssignAllGameIds()
        {
            // --- Step 1: Assign gameId fields + rename scene/instances (your original behavior) ---
            var allScripts = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
                .Where(mb => mb != null && mb.GetType().GetField("gameId") != null);

            foreach (var script in allScripts)
            {
                var path = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(script));
                if (string.IsNullOrEmpty(path))
                {
                    // Only warn for scene objects that won’t have a script asset path
                    continue;
                }

                string newName = AddPrefix(script.gameObject.name);
                script.gameObject.name = newName;

                // Prefab instance
                if (PrefabUtility.IsPartOfPrefabInstance(script.gameObject))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(script.gameObject);
                }

                // Prefab asset root loaded in memory (rare; still save)
                if (PrefabUtility.IsPartOfPrefabAsset(script.gameObject))
                {
                    string prefabPath = AssetDatabase.GetAssetPath(script.gameObject);
                    AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(prefabPath));
                }

                // Scene object
                if (script.gameObject.scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(script.gameObject.scene);

                var field = script.GetType().GetField("gameId");
                if (field != null)
                {
                    field.SetValue(script, GetGamePrefix());
                    EditorUtility.SetDirty(script);
                }

                EditorUtility.SetDirty(script.gameObject);
            }

            // --- Step 1b: Process prefab ASSETS (root name + gameId inside prefab asset) ---
            ProcessContentPrefabsOnly();

            // --- Step 2: Rename Addressable Groups via migration (1.21+ safe) ---
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("❌ AddressableAssetSettings not found.");
                return;
            }

            var groupsSnapshot = settings.groups.Where(g => g != null).ToList();
            string prefix = GetGamePrefix();
            int migrated = 0;

            foreach (var oldGroup in groupsSnapshot)
            {
                if (oldGroup == null || oldGroup.ReadOnly) continue;

                string desiredName = AddPrefix(oldGroup.Name);
                if (oldGroup.Name == desiredName) continue;

                bool wasDefault = settings.DefaultGroup == oldGroup;

                // Create or reuse destination group
                var destGroup = settings.groups.FirstOrDefault(g => g != null && g.Name == desiredName);
                if (destGroup == null)
                {
                    destGroup = CloneGroup(settings, oldGroup, desiredName);
                }

                // Move entries using the supported API
                var entries = oldGroup.entries.ToList();
                foreach (var entry in entries)
                {
                    settings.CreateOrMoveEntry(entry.guid, destGroup, readOnly: false, postEvent: false);
                }

                if (wasDefault) settings.DefaultGroup = destGroup;

                RemoveGroupCompat(settings, oldGroup);
                migrated++;
                Debug.Log($"✅ Migrated group '{oldGroup.Name}' → '{destGroup.Name}'");
            }

            BuildGroups();
            EnforceContentNamespaces();

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupAdded, null, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"🎯 Finished. Migrated {migrated} Addressable group(s) with prefix '{prefix}-'.");
        }
    }
}