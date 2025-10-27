namespace SuperAdventureLand
{
    using UnityEngine;
    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(ItemBlock), true)]
    public class ItemBlockEditor : BlockEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            ItemBlock itemBlock = (ItemBlock)target;
            if (GUILayout.Button("Break Block"))
            {
                itemBlock.SetState(BlockState.ACTIVATE);
            }
        }
    }
    #endif

    public class ItemBlock : Block
    {
        public GameObject dp_itemPrefab;
        public Transform dp_itemSpawnPoint;
        [HideInInspector] public Vector3 dp_itemSpawnPos {
            get {
                return dp_itemSpawnPoint ? dp_itemSpawnPoint.transform.position : transform.position;
            }
        }
        [Range(1, 100),SerializeField] public int dp_itemCount = 1;
        [Range(0.01f, 100.00f),SerializeField] public float dp_itemPopupForce = 1.5f;
        [HideInInspector] public bool dp_canSpawnItem = true;
        [HideInInspector] public string dp_itemName;
        public override void Awake () {
            base.Awake();
            if (dp_itemPrefab != null) {
                dp_itemName = dp_itemPrefab.name;
            }
        }
        public override void ExecuteState() {
            switch (state) {
                case BlockState.ACTIVATE:
                Debug.Log($"SpawnItems: {dp_itemCount}");
                    var itemCount = dp_itemCount;
                    while (itemCount > 0)
                    {
                        SpawnItem(hitVelocity, dp_itemCount-itemCount);
                        itemCount--;
                    }
                    base.ExecuteState();
                    break;
                default:
                    base.ExecuteState();
                    break;
            }
        }
        public virtual void SpawnItem(Vector3 velocityDir, int currentItem = 0)
        {
            Vector3 spawnPos = dp_itemSpawnPos;
            dp_itemName.SpawnAsset(spawnPos, Quaternion.identity, null, item => {
                item.transform.SetParent(transform.FindRoot(), true);
                if (item.TryGetComponent(out Item itemScript)) {
                    itemScript.dp_isStatic = false;

                    // Vector3 force = (velocityDir * 2f) + Vector3.up*dp_itemPopupForce;
                    // Vector3 direction = Camera.main ? (Camera.main.transform.position - spawnPos).normalized : Vector3.up;
                    // Vector3 calculatedForce =  (new Vector3(direction.x, 0, direction.z).normalized * 2f) + Vector3.up * Mathf.Max(Vector3.up.y * force.y, 3f);

                    if (dp_itemCount == 1) {
                        itemScript.PopUpItem(Vector3.up*dp_itemPopupForce);
                    } else {
                        Debug.Log($"SpawnItem: {currentItem} of {dp_itemCount}");
                        Vector3 direction;
                        float angle = (360f / dp_itemCount) * currentItem;
                        direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                        direction += new Vector3(
                            Random.Range(-0.1f, 0.1f),
                            0,
                            Random.Range(-0.1f, 0.1f)
                        );

                        direction.Normalize();
                        direction *= 2f;
                        itemScript.PopUpItem(direction + Vector3.up * dp_itemPopupForce);
                    }
                }
            }, error => {
                Debug.LogError($"Failed to load item: {error}");
            });
        }
    }
}
