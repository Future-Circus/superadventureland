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
        public override void OnEvent(object arg0 = null) {
            if (spawnPrefab.layer == LayerMask.NameToLayer("Item")) {
                GameObject item = Spawn(spawnPrefab, spawnPoint);
                if (item.TryGetComponent(out Item itemScript)) {
                    itemScript.PopUpItem(new Vector3(Random.Range(-0.1f, 1f), 2, Random.Range(-0.1f, 1f)));
                }
                onEvent?.Invoke(null);
            } else {
                base.OnEvent(arg0);
            }
        }
    }
}
