namespace SuperAdventureLand.Scripts
{
    using System;
    using System.Collections;
    using UnityEngine;

    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(SpawnBlock))]
    public class SpawnBlockEditor : UnityEditor.Editor
    {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();  // Draws the default inspector

            SpawnBlock script = (SpawnBlock)target;

            if (GUILayout.Button("Spawn"))
            {
                script.Spawn();
            }
        }

         private Vector3 PlayModeConversion (Vector3 v, SpawnBlock j) {
            if (Application.isPlaying) {
                return j.ogPosition.TransformPoint(v);
            } else {
                return j.transform.TransformPoint(v);
            }
        }

        private void OnSceneGUI()
        {

            // Get the target object
            SpawnBlock script = (SpawnBlock)target;

            /* ---- Waypoint Handles ---- */

            // For each waypoint, draw a handle
            for (int i = 0; i < script.waypoints.Length; i++)
            {
                Handles.color = Color.red;

                // Convert local waypoint to world space
                Vector3 worldPosition = PlayModeConversion(script.waypoints[i], script);

                // Get user-adjusted position in world space
                Vector3 newWorldPosition = Handles.FreeMoveHandle(
                    worldPosition,
                    0.2f, // Handle size
                    Vector3.zero, // No snapping
                    Handles.SphereHandleCap // Handle shape
                );

                // If positions have changed, update them and support Undo
                if (!Application.isPlaying && newWorldPosition != worldPosition)
                {
                    // Convert back to local space before saving
                    Vector3 newLocalPosition = script.transform.InverseTransformPoint(newWorldPosition);

                    script.waypoints[i] = newLocalPosition;
                    EditorUtility.SetDirty(script);
                    Undo.RegisterCompleteObjectUndo(script, "Move Handle");
                }
            }

            // Draw lines between waypoints
            Handles.color = Color.green;
            for (int i = 0; i < script.waypoints.Length - 1; i++)
            {
                Handles.DrawLine(
                    PlayModeConversion(script.waypoints[i], script),
                    PlayModeConversion(script.waypoints[i + 1], script)
                );
            }

            if (script.waypoints.Length > 0)
            {
                // Draw line between last and first waypoint
                Handles.DrawLine(
                    PlayModeConversion(script.waypoints[script.waypoints.Length - 1], script),
                    PlayModeConversion(script.waypoints[0], script)
                );
            }
        }
    }
    #endif

    public class SpawnBlock : MonoBehaviour
    {
        public GameObject spawnPrefab;
        public Transform SpawnPoint;
        public ParticleSystem revealParticle;
        public bool autoSpawn = true;
        public float spawnDelay = 5f;
        public Vector3[] waypoints;
        GameObject spawned;
        bool spawning = false;

        public Transform ogPosition;

        void Awake () {
            ogPosition = new GameObject("ogPosition").transform;
            ogPosition.position = transform.position;
            ogPosition.rotation = transform.rotation;
            ogPosition.SetParent(transform);
        }

        void Start () {

            if (autoSpawn) {
                Spawn();
            }
        }

        public void Spawn () {
            revealParticle.Play();
            if (spawnPrefab == null) {
                Debug.Log("SpawnPrefab is null");
                return;
            }
            spawnPrefab.SpawnAsset(SpawnPoint.position, SpawnPoint.rotation, transform.parent, spawned => {
                if (waypoints.Length > 0) {
                    if (spawned.TryGetComponent(out Jubbo jubbo)) {
                        var _waypoints = new Vector3[waypoints.Length];
                        for (int i = 0; i < waypoints.Length; i++) {
                            _waypoints[i] = transform.parent.InverseTransformPoint(transform.TransformPoint(waypoints[i]));
                        }
                        jubbo.waypoints = _waypoints;
                    };
                }
            }, error => {
                Debug.LogError($"Failed to spawn object: {error}");
            });
            spawning = false;
        }

        void Update () {
            if (spawned == null || spawned.IsDestroyed()) {
                if (!spawning) {
                    SpawnDelayed(spawnDelay);
                    spawning = true;
                }
            }
        }

        public IEnumerator SpawnSignal (float delay) {
            yield return new WaitForSeconds(delay);
            float startTime = Time.time;
            float duration = 1f;
            while (Time.time-startTime < duration) {
                var t = (Time.time - startTime) / duration;
                transform.localScale = Vector3.one + Vector3.one * (0.4f * t);
                transform.localRotation = Quaternion.Euler(Mathf.Sin(t*10)*(t*10), Mathf.Cos(t*10)*(t*10), Mathf.Sin(-t*10)*(t*10));
                yield return null;
            }
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
        }

        public void SpawnDelayed (float delay) {
            StartCoroutine(SpawnSignal(delay-1f));
            Invoke("Spawn", delay);
        }
    }
}
