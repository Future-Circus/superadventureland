using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Defective.JSON;
using UnityEngine;
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
            this.platform = platform.ToLower();
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
            UploadContentRequest data = new UploadContentRequest();
            var list = data.ToList();
            Debug.Log("Uploading content: " + list);
            DreamParkAPI.POST($"/api/content/{contentId}/upload", AuthAPI.GetUserAuth(), list, (success, response) => {
                callback?.Invoke(success, response);
            });
        }
    }
}