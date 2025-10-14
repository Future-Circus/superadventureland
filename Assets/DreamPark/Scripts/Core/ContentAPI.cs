using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Defective.JSON;
using UnityEngine;
using UnityEngine.Networking;
using APIResponse = DreamPark.API.DreamParkAPI.APIResponse;

namespace DreamPark.API
{  
    public class UploadContentData {
        public string filePath = null;
        public string fileName = null;
        public string mimeType = null;
        public byte[] data = null;

        public UploadContentData(string filePath, byte[] data) {
            this.filePath = filePath;
            this.fileName = Path.GetFileName(filePath);
            if (Path.GetExtension(fileName) == ".json") {
                mimeType = "application/json";
            } else if (Path.GetExtension(fileName) == ".hash") {
                mimeType = "text/plain";
            } else {
                mimeType = "application/octet-stream";
            }
            this.data = data;
        }
    }
    public class PlatformContentData {
        public string platform = null;
        public List<UploadContentData> data = null;

        public PlatformContentData(string platform) {
            this.platform = platform;
            string directory = Path.Combine("ServerData", this.platform);
            if (Directory.Exists(directory))
            {
                data = new List<UploadContentData>();
                var files = Directory.GetFiles(directory);
                foreach (var filePath in files)
                {
                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    data.Add(new UploadContentData(filePath, fileBytes));
                }
            }
            else
            {
                Debug.LogError($"{this.platform} directory not found in ServerData.");
            }
        }

        public List<KeyValuePair<string, UploadContentData>> ToList() {
            if (data == null) {
                return null;
            }
            List<KeyValuePair<string, UploadContentData>> list = new List<KeyValuePair<string, UploadContentData>>();
            foreach (var d in data) {
                list.Add(new KeyValuePair<string, UploadContentData>(platform, d));
            }
            return list;
        }
    }
    public class UploadContentRequest {
        public PlatformContentData ios;
        public PlatformContentData android;
        public PlatformContentData mac;
        public PlatformContentData windows;
        public PlatformContentData unity;
        public UploadContentRequest() {
            ios = new PlatformContentData("iOS");
            android = new PlatformContentData("Android");
            mac = new PlatformContentData("StandaloneOSX");
            windows = new PlatformContentData("StandaloneWindows");
            unity = new PlatformContentData("Unity");
        }

        public List<KeyValuePair<string, UploadContentData>> ToList() {
            List<KeyValuePair<string, UploadContentData>> list = new List<KeyValuePair<string, UploadContentData>>();
            
            var iosData = ios.ToList();
            var androidData = android.ToList();
            var macData = mac.ToList();
            var windowsData = windows.ToList();
            var unityData = unity.ToList();

            if (androidData != null) {
                list.AddRange(androidData);
            }
            if (iosData != null) {
                list.AddRange(iosData);
            }
            if (macData != null) {
                list.AddRange(macData);
            }
            if (windowsData != null) {
                list.AddRange(windowsData);
            }
            if (unityData != null) {
                list.AddRange(unityData);
            }
            return list;
        }
    }
    public class ContentAPI
    {
        public static void GetContent(Action<bool, APIResponse> callback) {
            Debug.Log("ContentAPI: Getting content catalog");
            DreamParkAPI.GET($"/api/content/", AuthAPI.GetUserAuth(), (success, response) => {
                if (success) {
                    Debug.Log("Content got successfully: " + response.json.Print());
                    callback?.Invoke(true, response);
                } else {
                    Debug.LogError("Failed to get content: " + response.error);
                    callback?.Invoke(false, response);
                }
            });
        }
        public static void GetContent(string contentId, Action<bool, APIResponse> callback) {
            DreamParkAPI.GET($"/api/content/{contentId}", AuthAPI.GetUserAuth(), (success, response) => {
                callback?.Invoke(success, response);
            });
        }
        public static void UpdateContent(string contentId, JSONObject update, Action<bool, APIResponse> callback) {
            DreamParkAPI.POST($"/api/content/{contentId}/update", AuthAPI.GetUserAuth(), update, (success, response) => {
                callback?.Invoke(success, response);
            });
        }
        public static void AddContent(string contentId, string contentName, string contentDescription, Action<bool, APIResponse> callback) {
            JSONObject body = new JSONObject();
            body.AddField("contentId", contentId);
            body.AddField("contentName", contentName);
            body.AddField("contentDescription", contentDescription);
            DreamParkAPI.POST($"/api/content/add", AuthAPI.GetUserAuth(), body, (success, response) => {
                callback?.Invoke(success, response);
            });
        }

        public static void UploadContent(string contentId, Action<bool, APIResponse> callback) {
            // 1️⃣ Collect local files for all platforms
            UploadContentRequest data = new UploadContentRequest();
            var files = data.ToList();

            if (files == null || files.Count == 0)
            {
                Debug.LogWarning("No files found to upload.");
                callback?.Invoke(false, new DreamParkAPI.APIResponse(false, 0, "No files found"));
                return;
            }

            Debug.Log($"[ContentAPI] Uploading {files.Count} files for content {contentId}");

        #if UNITY_EDITOR
            Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(UploadFlow(contentId, files, callback));
        #else
            CoroutineRunner.Run(UploadFlow(contentId, files, callback));
        #endif
        }

        private static IEnumerator UploadFlow(string contentId, List<KeyValuePair<string, UploadContentData>> files, Action<bool, APIResponse> callback)
        {
            // Convert coroutine to async UniTask for concurrency
            UploadFlowAsync(contentId, files, callback).Forget();
            yield break;
        }

        private static async UniTaskVoid UploadFlowAsync(string contentId, List<KeyValuePair<string, UploadContentData>> files, Action<bool, DreamParkAPI.APIResponse> callback)
        {
            int uploaded = 0;
            int failed = 0;
            int versionNumber = 1;
            var uploadTasks = new List<UniTask>();
            var uploadedFilesDict = new Dictionary<string, List<string>>();

            // 🔹 Create a parallel upload task for each file
            foreach (var kvp in files)
            {
                string platform = kvp.Key;
                UploadContentData file = kvp.Value;

                uploadTasks.Add(HandleFileUpload(contentId, platform, file)
                    .ContinueWith(result =>
                    {
                        if (result.success)
                        {
                            uploaded++;
                            lock (uploadedFilesDict)
                            {
                                if (!uploadedFilesDict.ContainsKey(platform))
                                    uploadedFilesDict[platform] = new List<string>();
                                uploadedFilesDict[platform].Add(result.uploadPath);
                            }
                        }
                        else
                        {
                            failed++;
                        }
                    }));
            }

            // 🔹 Wait for all uploads to complete concurrently
            await UniTask.WhenAll(uploadTasks);

            bool overallSuccess = failed == 0;
            string summary = $"Uploaded {uploaded}/{files.Count} files ({failed} failed)";
            Debug.Log($"[ContentUploader] {summary}");

            // 🔹 Build commit body
            JSONObject commitBody = new JSONObject();
            JSONObject uploadedFilesJson = new JSONObject();

            foreach (var kvp in uploadedFilesDict)
            {
                JSONObject arr = new JSONObject(JSONObject.Type.Array);
                foreach (var path in kvp.Value)
                    arr.Add(path);
                uploadedFilesJson.AddField(kvp.Key, arr);
            }

            commitBody.AddField("uploadedFiles", uploadedFilesJson);
            commitBody.AddField("versionNumber", versionNumber);

            // 🔹 Commit upload version metadata to server
            DreamParkAPI.POST($"/api/content/{contentId}/commitUpload", AuthAPI.GetUserAuth(), commitBody,
                (success, response) =>
                {
                    Debug.Log(success ? "✅ Version committed!" : $"❌ Commit failed: {response.error}");
                    callback?.Invoke(success, response);
                });
        }

        private static async UniTask<(bool success, string uploadPath)> HandleFileUpload(string contentId, string platform, UploadContentData file)
        {
            try
            {
                // Step 1: request presigned URL
                var body = new JSONObject();
                body.AddField("platform", platform);
                body.AddField("filename", file.fileName);
                body.AddField("contentType", file.mimeType);

                var tcs = new UniTaskCompletionSource<(bool success, string url, string uploadPath)>();
                DreamParkAPI.POST($"/api/content/{contentId}/uploadUrl", AuthAPI.GetUserAuth(), body, (success, response) =>
                {
                    if (success && response.json != null)
                    {
                        var uploadUrl = response.json.GetField("uploadUrl")?.stringValue;
                        var uploadPath = response.json.GetField("uploadPath")?.stringValue;
                        tcs.TrySetResult((true, uploadUrl, uploadPath));
                    }
                    else
                    {
                        Debug.LogError($"❌ Failed to get presigned URL for {file.fileName}");
                        tcs.TrySetResult((false, null, null));
                    }
                });

                var (ok, uploadUrl, uploadPath) = await tcs.Task;
                if (!ok || string.IsNullOrEmpty(uploadUrl))
                    return (false, null);

                // Step 2: upload to Firebase
                var uploadTcs = new UniTaskCompletionSource<bool>();
                DreamParkAPI.PUT(uploadUrl, "", file.data, file.mimeType, (success, _) =>
                {
                    uploadTcs.TrySetResult(success);
                });

                bool successUpload = await uploadTcs.Task;
                if (successUpload)
                    Debug.Log($"✅ Uploaded {file.fileName} ({platform})");
                else
                    Debug.LogError($"❌ Upload failed for {file.fileName}");

                return (successUpload, successUpload ? uploadPath : null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Exception uploading {file.fileName}: {ex.Message}");
                return (false, null);
            }
        }
    }
}