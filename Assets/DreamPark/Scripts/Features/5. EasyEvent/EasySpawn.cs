using UnityEngine;
namespace DreamPark.Easy {
    public class EasySpawn : EasyEvent
    {
        public GameObject spawnPrefab;
        public Transform spawnPoint;
        [Range(1, 10)] public int amount = 1;
        public bool copyRotation = true;

        public override void Awake() {
            base.Awake();
            if (spawnPoint == null) {
                spawnPoint = transform;
            }
        }

        public override void OnEvent(object arg0 = null) {
            if (spawnPrefab != null) {
                if (amount == 1) {
                    Spawn(spawnPrefab, spawnPoint);
                } else {
                    for (int i = 0; i < amount; i++) {
                        Spawn(spawnPrefab, spawnPoint, i);
                    }
                }
                onEvent?.Invoke(null);
            }
        }

        public virtual GameObject Spawn(GameObject prefab, Transform point, int index = 0) {
            GameObject spawnedObject = Instantiate(prefab);
            spawnedObject.transform.position = point.position;
            if (copyRotation) {
                spawnedObject.transform.rotation = point.rotation;
            }
            return spawnedObject;
        }
    }
}