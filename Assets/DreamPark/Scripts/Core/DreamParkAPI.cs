using UnityEngine;
using UnityEngine.Networking;
using Defective.JSON;
using System;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif

namespace DreamPark.API
{
    public class DreamParkAPI
    {
        public class APIResponse
        {
            public bool success;
            public JSONObject json;
            public byte[] data;
            public long statusCode;
            public string rawText;
            public string error;

            public APIResponse(bool success, long code, string error, string rawText = "", byte[] data = null)
            {
                this.success = success;
                this.statusCode = code;
                this.rawText = rawText ?? "";
                this.error = error;
                this.json = rawText != "" ? new JSONObject(rawText) : null;
                this.data = data ?? null;
            }
        }
        
        [Tooltip("Optional: override base URLs at runtime.")]
        public static string devBaseUrl  = "https://dreampark-dev-da8108e9492c.herokuapp.com";
        public static void POST(string endpoint, string authToken, object body, Action<bool, APIResponse> callback)
        {
            var url = devBaseUrl + endpoint;
            byte[] bodyRaw = null;
            if (body is JSONObject) {
                bodyRaw = System.Text.Encoding.UTF8.GetBytes((body ?? new JSONObject()).ToString());
            } else if (body is byte[]) {
                bodyRaw = body as byte[];
            }else if (body is List<KeyValuePair<string, UploadContentData>>) {
                Debug.Log("Posting multipart: " + body);
                #if UNITY_EDITOR
                EditorCoroutineUtility.StartCoroutineOwnerless(PostMultipart(url, authToken, body as List<KeyValuePair<string, UploadContentData>>, callback));
                #endif
                return;
            }
            Debug.Log("Posting request: " + url);
            #if UNITY_EDITOR
            EditorCoroutineUtility.StartCoroutineOwnerless(PostRequest(url, authToken, bodyRaw, callback));
            #endif
        }

        public static void GET(string endpoint, string authToken, Action<bool, APIResponse> callback)
        {
            var url = devBaseUrl + endpoint;
            #if UNITY_EDITOR
            EditorCoroutineUtility.StartCoroutineOwnerless(GetRequest(url, authToken, callback));
            #endif
        }
        private static IEnumerator PostRequest(string url, string authToken, byte[] bodyRaw, Action<bool, APIResponse> callback)
        {
            using (UnityWebRequest req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();

                req.SetRequestHeader("Content-Type", "application/json");
                if (!string.IsNullOrEmpty(authToken))
                    req.SetRequestHeader("Authorization", authToken);

                yield return req.SendWebRequest();

                var response = BuildResponse(req);
                callback?.Invoke(response.success, response);
            }
        }

        private static IEnumerator GetRequest(string url, string authToken, Action<bool, APIResponse> callback)
        {
            using (UnityWebRequest req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
            {
                req.downloadHandler = new DownloadHandlerBuffer();
                if (!string.IsNullOrEmpty(authToken))
                    req.SetRequestHeader("Authorization", authToken);

                yield return req.SendWebRequest();

                var response = BuildResponse(req);
                callback?.Invoke(response.success, response);
            }
        }

        private static IEnumerator PostMultipart(string url, string authToken, List<KeyValuePair<string, UploadContentData>> files, Action<bool, APIResponse> callback)
        {
            WWWForm form = new WWWForm();
            foreach (var kvp in files)
            {
                form.AddBinaryData(kvp.Key, kvp.Value.data, kvp.Value.fileName, kvp.Value.mimeType);
            }

            using (UnityWebRequest req = UnityWebRequest.Post(url, form))
            {
                if (!string.IsNullOrEmpty(authToken))
                    req.SetRequestHeader("Authorization", authToken);

                var operation = req.SendWebRequest();
                while (!operation.isDone)
                {
                    Debug.Log($"[Multipart Upload] Upload Progress: {req.uploadProgress:P1}, Download Progress: {req.downloadProgress:P1}");
                    yield return null;
                }

                Debug.Log("[Multipart Upload] Completed request, status: " + req.result);

                var response = BuildResponse(req);
                callback?.Invoke(response.success, response);
            }
        }
        private static APIResponse BuildResponse(UnityWebRequest req)
        {
            bool success = !(req.result == UnityWebRequest.Result.ConnectionError ||
                            req.result == UnityWebRequest.Result.ProtocolError);

            string rawText = req.downloadHandler != null ? req.downloadHandler.text : "";
            byte[] data = req.downloadHandler != null ? req.downloadHandler.data : null;

            return new APIResponse(
                success,
                req.responseCode,
                success ? null : req.error,
                rawText,
                data
            );
        }
    }
}