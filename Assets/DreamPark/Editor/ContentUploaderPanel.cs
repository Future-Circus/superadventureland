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
namespace DreamPark {
    public class ContentUploaderPanel : EditorWindow
    {
        [HideInInspector] public string email = "test@test.com";
        [HideInInspector] public string password = "password";
        private string gameId = "";
        private string contentName = "";
        private string contentDescription = "";
        private bool isUploading = false;

        [MenuItem("DreamPark/Content Uploader")]
        public static void ShowWindow()
        {
            GetWindow<ContentUploaderPanel>("Content Uploader");
        }

        private void OnEnable()
        {
            gameId = ContentProcessor.GetGamePrefix();
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

            if (!AuthAPI.isLoggedIn) {
                if (GUILayout.Button("Login")) {
                    Login(email, password);
                }
                if (GUILayout.Button("Sign Up")) {
                    Application.OpenURL("https://dreampark.app/signup");
                }
            } else {
                if (GUILayout.Button("Logout")) {
                    Logout();
                }
                EditorGUILayout.LabelField("User ID: " + AuthAPI.userId);
            }

            GUILayout.Space(10);
            GUILayout.Label("Upload New Content", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Game ID", gameId);
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(5);
            contentName = EditorGUILayout.TextField("Name", contentName);
            contentDescription = EditorGUILayout.TextField("Description", contentDescription, GUILayout.Height(40));

            GUILayout.Space(10);

            GUI.enabled = !isUploading && !string.IsNullOrEmpty(gameId) && !string.IsNullOrEmpty(contentName);
            if (GUILayout.Button("Upload", GUILayout.Height(32)))
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

        public void Login(string email, string password) { 
            AuthAPI.Login(email, password, (success, response) => {
                if (success) {
                    Debug.Log("Login successful: " + response.json.Print());
                } else {
                    Debug.LogError("Failed to login: " + response.error);
                }
            });
        }

        public void Logout() {
            AuthAPI.Logout((success, response) => {
                if (success) {
                    Debug.Log("Logout successful: " + response.json.Print());
                } else {
                    Debug.LogError("Failed to logout: " + response.error);
                }
            });
        }

        private void UploadContent(bool build = false)
        {
            try {
            if (string.IsNullOrEmpty(gameId))
            {
                Debug.LogError("❌ Game ID not detected. Make sure your content folder exists under Assets/Content.");
                return;
            }

            isUploading = true;

            Debug.Log($"🚀 Uploading content for {gameId}...");

            ContentAPI.GetContent(gameId, (exists, response) =>
            {
                if (exists)
                {
                    Debug.Log("Content found: " + response.json.Print());
                    var contentDirectory = response.json;
                    var versionNumber = contentDirectory.HasField("content") && contentDirectory.GetField("content").HasField("versions") ? contentDirectory.GetField("content").GetField("versions").list.Count+1 : 1;     
                    var targetUrl = $"https://dreampark-dev-da8108e9492c.herokuapp.com/app/content/addressables/{gameId}/{versionNumber}";

                    string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                    string serverDataPath = Path.Combine(projectRoot, "ServerData");

                    bool buildSuccess = true;
                    try
                    {      
                        if (build) {
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
                            ContentProcessor.AssignAllGameIds();
                            ContentProcessor.EnforceContentNamespaces();

                            buildSuccess &= BuildUnityPackage(gameId);
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
                        ContentAPI.UploadContent(gameId, (success, response) => {
                            if (success) {
                                Debug.Log("✅ Content uploaded successfully");
                                EditorUtility.DisplayDialog("Success", $"'{contentName}' uploaded successfully!", "OK");
                            } else {
                                Debug.LogError($"❌ Content uploaded failed: {response.error}");
                                EditorUtility.DisplayDialog("Error", $"Upload failed: {response.error}", "OK");
                            }
                            isUploading = false;
                        });
                    } catch (Exception e) {
                        Debug.LogError("❌ Addressable build failed: " + e);
                        EditorUtility.DisplayDialog("Error", $"Error: {e.Message}", "OK");
                        isUploading = false;
                    }
                    return;
                }
                else
                {
                    ContentAPI.AddContent(gameId, contentName, contentDescription, (success, response) =>
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
            } catch (Exception e) {
                Debug.LogError("❌ Upload failed: " + e);
                EditorUtility.DisplayDialog("Error", $"Error: {e.Message}", "OK");
                isUploading = false;
            }
        }
        public static bool BuildForTarget(BuildTarget target, BuildTargetGroup group, string targetUrl)
        {
            try {
                
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
                if (string.IsNullOrEmpty(existingValue)) {
                    settings.profileSettings.CreateValue("RemoteLoadPath", targetUrl);
                    Debug.Log("Created RemoteLoadPath profile variable");
                } else {
                    settings.profileSettings.SetValue(profileId, "RemoteLoadPath", targetUrl);
                    Debug.Log($"🌐 RemoteLoadPath set to {targetUrl}");
                }

                // 🔹 Kick off the Addressables build
                AddressablesPlayerBuildResult result;
                AddressableAssetSettings.BuildPlayerContent(out result);

                if (!string.IsNullOrEmpty(result.Error)) {
                    throw new System.Exception($"❌ Addressables build failed for {target}: {result.Error}");
                }
                Debug.Log($"✅ Addressables build complete for {target}");
                return true;
            } catch (Exception e) {
                Debug.LogError($"❌ Addressables build failed for {target}: {e}");
                return false;
            }
        }

        public static bool BuildUnityPackage(string contentId) {
            Debug.Log($"Building unity package for {contentId}");
            try {
            string sourceFolder = "Assets/Content/" + ContentProcessor.GetGamePrefix(); // change this to your folder
                string[] guids = AssetDatabase.FindAssets("t:Script", new[] { sourceFolder });

                string[] assetPaths = guids
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .ToArray();

                if (assetPaths.Length == 0)
                {
                    Debug.LogError("No scripts found in folder: " + sourceFolder);
                    return true;
                }

                // Export to a temporary location
                string tempPath = Path.Combine(Application.dataPath, $"../{contentId}.unitypackage");
                AssetDatabase.ExportPackage(assetPaths, tempPath, ExportPackageOptions.Default);

                // Ensure the "ServerData" and "Unity" directories exist
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string serverDataPath = Path.Combine(projectRoot, "ServerData");
                string unityPath = Path.Combine(serverDataPath, "Unity");

                if (!Directory.Exists(serverDataPath))
                {
                    Directory.CreateDirectory(serverDataPath);
                    Debug.Log("Created directory: " + serverDataPath);
                }

                if (!Directory.Exists(unityPath))
                {
                    Directory.CreateDirectory(unityPath);
                    Debug.Log("Created directory: " + unityPath);
                }

                // Move/copy to persistent storage
                string destPath = Path.Combine(unityPath, $"{contentId}.unitypackage");
                File.Copy(tempPath, destPath, overwrite: true);

                Debug.Log("Scripts exported to: " + destPath);
                return true;
            } catch (Exception e) {
                Debug.LogError("❌ Content upload failed: " + e);
                return false;
            }
        }
    }
}
#endif