namespace SuperAdventureLand
{
    using System.Collections.Generic;
    using UnityEngine;
    using System.Threading.Tasks;

    #if UNITY_EDITOR
    using UnityEditor;
    using UnityEditorInternal;

    [CustomEditor(typeof(Spawner), true)]
    public class SpawnerEditor : StandardEntityEditor<SpawnerState>
    {
        public override void OnEnable()
        {
            showAwareFeature = true;
            showWaypoints = true;
            base.OnEnable();
        }
    }
    #endif

    public enum SpawnerState {
        IDLE,
        IDLING,
        SPAWN,
        SPAWNING,
        DESTROY,
        DESTROYING
    }

    public class Spawner : StandardEntity<SpawnerState>
    {
        public GameObject dp_prefabToSpawn;
        public ParticleSystem spawnEffect;
        public int dp_maxSpawnedObjects = 1;
        public float dp_retryDelay = 0.5f;
        public bool destroySpawnedObjectsOnDestroy = false;
        public float lastSpawnTime = 0f;
        [HideInInspector] public List<GameObject> spawnedObjects = new List<GameObject>();
        public override void ExecuteState() {
            switch (state) {
                case SpawnerState.IDLE:
                    break;
                case SpawnerState.IDLING:
                    if (!stateWillChange) {
                        spawnedObjects.RemoveAll(obj => obj == null);
                        if (spawnedObjects.Count < dp_maxSpawnedObjects) {
                            if (Time.time-lastSpawnTime >= dp_retryDelay && playerInRange &&
                            Physics.OverlapSphere(transform.position, 1.5f, LayerMask.GetMask("Player")).Length == 0) {
                                SetState(SpawnerState.SPAWN);
                            }
                        } else {
                            lastSpawnTime = Time.time;
                            SetState(SpawnerState.IDLE);
                        }
                    }
                    break;
                case SpawnerState.SPAWN:
                    lastSpawnTime = Time.time;
                    SpawnObj();
                    break;
                case SpawnerState.SPAWNING:
                    SetState(SpawnerState.IDLE);
                    break;
                case SpawnerState.DESTROY:
                    if (destroySpawnedObjectsOnDestroy) {
                        foreach (var obj in spawnedObjects) {
                            Destroy(obj);
                        }
                        spawnedObjects.Clear();
                    }
                    enabled = false;
                    Destroy(gameObject);
                    break;
                default:
                    break;
            }
        }

        public void SpawnObj () {
            if (spawnEffect != null) {
                spawnEffect.Play();
            }
            dp_prefabToSpawn.SpawnAsset(transform.position, transform.rotation, transform.parent, obj => {
                if (obj.TryGetComponent<Collider>(out var collider)) {
                    var bounds = collider.bounds;
                    var heightOffset = Vector3.up * (bounds.size.y / 2);
                    obj.transform.position = transform.position + heightOffset;
                }
                obj.SetActive(true);
                if (waypoints.Length > 0 && obj.TryGetComponent<PathCreature>(out var pathCreature)) {
                    pathCreature.waypoints = waypoints;
                }
                spawnedObjects.Add( obj );
            }, error => {
                Debug.LogError($"Failed to spawn object: {error}");
            });
        }
    }
}
