namespace SuperAdventureLand
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using Newtonsoft.Json;
    using UnityEngine;
    using UnityEngine.Networking;

    public class SunManager : MonoBehaviour
    {
        public static SunManager Instance;
        private Dictionary<string, List<Action>> sunEventCallbacks = new Dictionary<string, List<Action>>();

        private void Awake() {
            if (Instance == null) {
                Instance = this;
            }
        }

        private void Start() {
            gameObject.PrioritizeAsset();
        }

        public void RegisterSunEvent(string eventId, Action callback) {
            if (!sunEventCallbacks.ContainsKey(eventId)) {
                sunEventCallbacks[eventId] = new List<Action>();
            }
            sunEventCallbacks[eventId].Add(callback);
        }

        public void OnEvent(string eventId) {
            if (sunEventCallbacks.ContainsKey(eventId)) {
                foreach (Action callback in sunEventCallbacks[eventId]) {
                    Debug.Log($"SunManager: Calling event {eventId}");
                    callback();
                }
            } else {
                Debug.LogWarning($"SunManager: Event {eventId} not found");
            }
        }
    }
}
