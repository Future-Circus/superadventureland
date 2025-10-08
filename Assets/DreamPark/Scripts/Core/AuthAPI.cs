using System;
using Defective.JSON;
using UnityEngine;
using APIResponse = DreamPark.API.DreamParkAPI.APIResponse;

namespace DreamPark.API
{
    public class AuthAPI : MonoBehaviour
    {
        public static string GetUserAuth() {
#if UNITY_EDITOR
            var sessionToken = UnityEditor.EditorPrefs.GetString("sessionToken", "");
#else
            var sessionToken = PlayerPrefs.GetString("sessionToken", "");
#endif
            return $"Bearer {sessionToken}";
        }

        public static string GetAPIKey() {
            return $"ApiKey 3JpMxtvHlAYHXzPlEaZteRh7ocStWL0mvogddB0ojvw=";
        }
        
        public static bool isLoggedIn {
            get {
#if UNITY_EDITOR
                var sessionToken = UnityEditor.EditorPrefs.GetString("sessionToken", "");
#else
                var sessionToken = PlayerPrefs.GetString("sessionToken", "");
#endif
                return !string.IsNullOrEmpty(sessionToken);
            }
        }
        public static string userId {
            get {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetString("userId", "");
#else
                return PlayerPrefs.GetString("userId", "");
#endif            
            }
        }
        public static string sessionToken {
            get {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetString("sessionToken", "");
#else
                return PlayerPrefs.GetString("sessionToken", "");
#endif
            }
        }
        public static void Login(string email, string password, Action<bool, APIResponse> callback) {
            var body = new JSONObject(JSONObject.Type.Object);
            body.AddField("email", email);
            body.AddField("password", password);
            DreamParkAPI.POST("/auth/login", "", body, (success, response) => {
                if (success) {
#if UNITY_EDITOR
                    UnityEditor.EditorPrefs.SetString("sessionToken", response.json.GetField("session").stringValue);
                    UnityEditor.EditorPrefs.SetString("userId", response.json.GetField("uid").stringValue);
#else
                    PlayerPrefs.SetString("sessionToken", response.json.GetField("session").stringValue);
                    PlayerPrefs.SetString("userId", response.json.GetField("uid").stringValue);
                    PlayerPrefs.Save();
#endif
                    callback?.Invoke(success, response);
                } else {
                    callback?.Invoke(success, response);
                }
            });
        }

        public static void Logout(Action<bool, APIResponse> callback) {
            JSONObject body = new JSONObject();
            body.AddField("session", sessionToken);
            DreamParkAPI.POST("/auth/logout", AuthAPI.GetUserAuth(), body, (success, response) => {
                if (success) {
                    callback?.Invoke(success, response);
                } else {
                    callback?.Invoke(success, response);
                }
#if UNITY_EDITOR
                    UnityEditor.EditorPrefs.DeleteKey("sessionToken");
                    UnityEditor.EditorPrefs.DeleteKey("userId");
#else
                    PlayerPrefs.DeleteKey("sessionToken");
                    PlayerPrefs.DeleteKey("userId");
#endif
            });
        }
    }
}