using UnityEngine;
namespace DreamPark.Easy {
    public class EasySpawn : EasyEvent
    {
        public GameObject spawnPrefab;
        public Transform spawnPoint;

        public override void Awake() {
            base.Awake();
            if (spawnPoint == null) {
                spawnPoint = transform;
            }
        }

        public override void OnEvent(object arg0 = null) {
            if (spawnPrefab != null) {
                Spawn(spawnPrefab, spawnPoint);
                onEvent?.Invoke(null);
            }
        }

        public GameObject Spawn(GameObject prefab, Transform point) {
            return Instantiate(prefab, point.position, point.rotation);
        }
    }
}