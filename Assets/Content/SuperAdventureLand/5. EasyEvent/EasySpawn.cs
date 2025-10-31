namespace SuperAdventureLand
{
    #if UNITY_EDITOR
    using UnityEditor;
    #endif
    using UnityEngine;

    public class EasySpawn : DreamPark.Easy.EasySpawn
    {

    #if UNITY_EDITOR
        public void OnValidate() {
            if (spawnPrefab == null) {
                spawnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Content/SuperAdventureLand/Prefabs/E_COIN.prefab");
            }
        }
    #endif
        public override GameObject Spawn(GameObject prefab, Transform point, int index = 0) {
            GameObject spawnedObject = base.Spawn(prefab, point);
            if (spawnedObject.layer == LayerMask.NameToLayer("Item") && spawnedObject.TryGetComponent(out Item itemScript)) {
                if (amount == 1) {
                    itemScript.PopUpItem(new Vector3(Random.Range(-0.1f, 1f), 2, Random.Range(-0.1f, 1f)));
                } else {
                    Vector3 direction;
                    float angle = (360f / amount) * index;
                    direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                    direction += new Vector3(
                        Random.Range(-0.1f, 0.1f),
                        0,
                        Random.Range(-0.1f, 0.1f)
                    );
                    direction.Normalize();
                    direction *= 2f;
                    itemScript.PopUpItem(direction + Vector3.up * 2f);
                }
            }
            return spawnedObject;
        }
    }
}
