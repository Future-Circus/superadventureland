#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using DreamPark.API;
using System;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using System.Linq;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

namespace DreamPark {
    public class ContentUploaderPanel : EditorWindow
    {
        [HideInInspector] public string email = "test@test.com";
        [HideInInspector] public string password = "password";
        private string contentId = "";
        private string contentName = "";
        private string contentDescription = "";
        private bool isUploading = false;

        private List<string> contentIdOptions = new List<string>();
        private int contentIdIndex = 0;

        [MenuItem("DreamPark/Content Uploader")]
        public static void ShowWindow()
        {
            GetWindow<ContentUploaderPanel>("Content Uploader");
        }

        // Listen for folder changes in Assets/Content and update content options

        private static FileSystemWatcher contentFolderWatcher;

        [InitializeOnLoadMethod]
        private static void InitContentFolderWatcher()
        {
            string path = Path.Combine(Application.dataPath, "Content");
            if (!Directory.Exists(path)) return;

            if (contentFolderWatcher != null)
            {
                contentFolderWatcher.EnableRaisingEvents = false;
                contentFolderWatcher.Dispose();
            }

            contentFolderWatcher = new FileSystemWatcher(path)
            {
                NotifyFilter = NotifyFilters.DirectoryName,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true,
            };
            contentFolderWatcher.Created += (s, e) =>
            {
                if (Directory.Exists(e.FullPath))
                {
                    // Folder created
                    EditorApplication.delayCall += () =>
                    {
                        // Find all open ContentUploaderPanels and refresh their content options
                        foreach (ContentUploaderPanel win in Resources.FindObjectsOfTypeAll<ContentUploaderPanel>())
                        {
                            win.RefreshContentIdOptions();
                            win.Repaint();
                        }
                    };
                }
            };

            contentFolderWatcher.Deleted += (s, e) =>
            {
                EditorApplication.delayCall += () =>
                {
                    // Find all open ContentUploaderPanels and refresh their content options
                    foreach (ContentUploaderPanel win in Resources.FindObjectsOfTypeAll<ContentUploaderPanel>())
                    {
                        win.RefreshContentIdOptions();
                        win.Repaint();
                    }
                };
            };
        }

        private void OnEnable()
        {
            RefreshContentIdOptions();

            // Try to pick the "most likely" game prefix
            var defaultPrefix = ContentProcessor.GetGamePrefix();
            if (!string.IsNullOrEmpty(defaultPrefix) && contentIdOptions.Count > 0)
            {
                int idx = contentIdOptions.IndexOf(defaultPrefix);
                if (idx >= 0) {
                    contentIdIndex = idx;
                    contentId = contentIdOptions[contentIdIndex];
                } else {
                    contentIdIndex = 0;
                    contentId = contentIdOptions.Count > 0 ? contentIdOptions[0] : "";
                }
            }
            else if (contentIdOptions.Count > 0)
            {
                contentIdIndex = 0;
                contentId = contentIdOptions[0];
            }
        }

        private void RefreshContentIdOptions()
        {
            contentIdOptions.Clear();
            string contentPath = Path.Combine(Application.dataPath, "Content");
            if (Directory.Exists(contentPath))
            {
                var dirs = Directory.GetDirectories(contentPath)
                    .Select(d => Path.GetFileName(d))
                    .Where(d => !string.IsNullOrEmpty(d))
                    .OrderBy(d => d)
                    .ToList();
                contentIdOptions.AddRange(dirs);
            }
            else
            {
                Debug.LogWarning("No Assets/Content folder exists in this project.");
            }

            // Sanity - no invalid selection
            if (contentIdOptions.Count == 0)
            {
                contentId = "";
                contentIdIndex = 0;
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Login", EditorStyles.boldLabel);
            GUI.enabled = !AuthAPI.isLoggedIn;
            EditorGUILayout.LabelField("Email");
            email = EditorGUILayout.TextField(email);

            EditorGUILayout.LabelField("Password");
            password = EditorGUILayout.PasswordField(password);
            GUI.enabled = true;

            if (!AuthAPI.isLoggedIn)
            {
                if (GUILayout.Button("Login"))
                {
                    Login(email, password);
                }
                if (GUILayout.Button("Sign Up"))
                {
                    Application.OpenURL("https://dreampark.app/signup");
                }
            }
            else
            {
                if (GUILayout.Button("Logout"))
                {
                    Logout();
                }
                EditorGUILayout.LabelField("User ID: " + AuthAPI.userId);
            }

            GUILayout.Space(10);
            GUILayout.Label("Upload New Content", EditorStyles.boldLabel);

            // ContentId dropdown
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Content ID", GUILayout.Width(EditorGUIUtility.labelWidth));
            int prevIndex = contentIdIndex;

            EditorGUI.BeginDisabledGroup(contentIdOptions.Count == 0);

            contentIdIndex = EditorGUILayout.Popup(contentIdIndex, contentIdOptions.ToArray());
            if (contentIdOptions.Count > 0)
            {
                // Only set contentId if a valid selection is available
                contentId = contentIdOptions[contentIdIndex];
            }
            else
            {
                contentId = "";
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            if (contentIdOptions.Count == 0)
            {
                EditorGUILayout.HelpBox("No content folders found under Assets/Content. Please create at least one game/content folder.", MessageType.Warning);
            }

            GUILayout.Space(5);
            contentName = EditorGUILayout.TextField("Name", contentName);
            contentDescription = EditorGUILayout.TextField("Description", contentDescription, GUILayout.Height(40));

            GUILayout.Space(10);

            GUI.enabled = !isUploading && !string.IsNullOrEmpty(contentId) && !string.IsNullOrEmpty(contentName);
            if (GUILayout.Button("Compile & Upload", GUILayout.Height(32)))
            {
                UploadContent(true);
            }
            if (GUILayout.Button("Try Reupload", GUILayout.Height(32)))
            {
                UploadContent(false);
            }
            GUI.enabled = true;

            if (isUploading)
            {
                GUILayout.Space(10);
                GUILayout.Label("Uploading...", EditorStyles.miniLabel);
            }
        }

        public void Login(string email, string password)
        {
            AuthAPI.Login(email, password, (success, response) =>
            {
                if (success)
                {
                    Debug.Log("Login successful: " + response.json.Print());
                }
                else
                {
                    Debug.LogError("Failed to login: " + response.error);
                }
            });
        }

        public void Logout()
        {
            AuthAPI.Logout((success, response) =>
            {
                if (success)
                {
                    Debug.Log("Logout successful: " + response.json.Print());
                }
                else
                {
                    Debug.LogError("Failed to logout: " + response.error);
                }
            });
        }

        private void UploadContent(bool build = false)
        {
            try
            {
                if (string.IsNullOrEmpty(contentId))
                {
                    Debug.LogError("❌ Game ID not detected. Make sure your content folder exists under Assets/Content.");
                    return;
                }

                isUploading = true;

                Debug.Log($"🚀 Uploading content for {contentId}...");

                ContentAPI.GetContent(contentId, (exists, response) =>
                {
                    if (exists)
                    {
                        Debug.Log("Content found: " + response.json.Print());
                        var contentDirectory = response.json;
                        var versionNumber = contentDirectory.HasField("content") && contentDirectory.GetField("content").HasField("versions") ? contentDirectory.GetField("content").GetField("versions").list.Count + 1 : 1;
                        var targetUrl = $"https://dreampark-dev-da8108e9492c.herokuapp.com/app/content/addressables/{contentId}/{versionNumber}";

                        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                        string serverDataPath = Path.Combine(projectRoot, "ServerData");

                        bool buildSuccess = true;
                        try
                        {
                            if (build)
                            {
                                if (Directory.Exists(serverDataPath))
                                {
                                    Directory.Delete(serverDataPath, true);
                                    Debug.Log("ServerData folder deleted successfully.");
                                }
                                else
                                {
                                    Debug.LogWarning("ServerData folder does not exist.");
                                }
                                Caching.ClearCache();
                                Addressables.ClearResourceLocators();
                                AssetDatabase.Refresh();
                                Debug.Log("✅ Cache purge complete.");

                                //refresh database with new addressables
                                ContentProcessor.ForceUpdateContent(contentId);
                                // ContentProcessor.SelectBuildGroups(contentId);
                                ContentProcessor.EnforceContentNamespaces(contentId);
                                // ContentProcessor.RemoveUnsavedAssets(contentId);
                                buildSuccess &= ContentProcessor.BuildUnityPackage(contentId);

                                if (!buildSuccess) throw new Exception("Unity package build failed");
                                buildSuccess &= BuildForTarget(BuildTarget.Android, BuildTargetGroup.Android, $"{targetUrl}/Android");
                                if (!buildSuccess) throw new Exception("Android build failed");
                                buildSuccess &= BuildForTarget(BuildTarget.iOS, BuildTargetGroup.iOS, $"{targetUrl}/iOS");
                                if (!buildSuccess) throw new Exception("iOS build failed");
                                buildSuccess &= BuildForTarget(BuildTarget.StandaloneOSX, BuildTargetGroup.Standalone, $"{targetUrl}/StandaloneOSX");
                                if (!buildSuccess) throw new Exception("OSX build failed");
                                buildSuccess &= BuildForTarget(BuildTarget.StandaloneWindows, BuildTargetGroup.Standalone, $"{targetUrl}/StandaloneWindows");
                                if (!buildSuccess) throw new Exception("Windows build failed");
                            }
                            ContentAPI.UploadContent(contentId, (success, response) =>
                            {
                                if (success)
                                {
                                    Debug.Log("✅ Content uploaded successfully");
                                    EditorUtility.DisplayDialog("Success", $"'{contentName}' uploaded successfully!", "OK");
                                }
                                else
                                {
                                    Debug.LogError($"❌ Content uploaded failed: {response.error}");
                                    EditorUtility.DisplayDialog("Error", $"Upload failed: {response.error}", "OK");
                                }
                                isUploading = false;
                            });
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("❌ Addressable build failed: " + e);
                            EditorUtility.DisplayDialog("Error", $"Error: {e.Message}", "OK");
                            isUploading = false;
                        }
                        return;
                    }
                    else
                    {
                        ContentAPI.AddContent(contentId, contentName, contentDescription, (success, response) =>
                        {
                            if (success)
                            {
                                Debug.Log($"✅ Content '{contentName}' uploaded successfully!");
                                UploadContent(build);
                            }
                            else
                            {
                                Debug.LogError($"❌ Failed to create new content: {response.error}");
                                EditorUtility.DisplayDialog("Error", $"Failed to create new content: {response.error}", "OK");
                                isUploading = false;
                            }
                        });
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogError("❌ Upload failed: " + e);
                EditorUtility.DisplayDialog("Error", $"Error: {e.Message}", "OK");
                isUploading = false;
            }
        }
        public static bool BuildForTarget(BuildTarget target, BuildTargetGroup group, string targetUrl)
        {
            try
            {

                if (!BuildPipeline.IsBuildTargetSupported(group, target))
                {
                    throw new System.Exception($"❌ Build target {target} is not supported. Please ensure the necessary build support is installed.");
                }

                var settings = AddressableAssetSettingsDefaultObject.Settings;

                Debug.Log($"Switching build target to {target} in {group}");

                // 🔹 Switch build target (Addressables only respects the *active* target)
                if (EditorUserBuildSettings.activeBuildTarget != target)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);

                Debug.Log($"Setting RemoteLoadPath to {targetUrl}");
                string profileId = settings.activeProfileId;
                string existingValue = settings.profileSettings.GetValueByName(profileId, "RemoteLoadPath");
                if (string.IsNullOrEmpty(existingValue))
                {
                    settings.profileSettings.CreateValue("RemoteLoadPath", targetUrl);
                    Debug.Log("Created RemoteLoadPath profile variable");
                }
                else
                {
                    settings.profileSettings.SetValue(profileId, "RemoteLoadPath", targetUrl);
                    Debug.Log($"🌐 RemoteLoadPath set to {targetUrl}");
                }

                // 🔹 Kick off the Addressables build
                AddressablesPlayerBuildResult result;
                AddressableAssetSettings.BuildPlayerContent(out result);

                if (!string.IsNullOrEmpty(result.Error))
                {
                    throw new System.Exception($"❌ Addressables build failed for {target}: {result.Error}");
                }
                Debug.Log($"✅ Addressables build complete for {target}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Addressables build failed for {target}: {e}");
                return false;
            }
        }
    }
}
#endif