namespace SuperAdventureLand.Scripts
{
    using UnityEngine;

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
        [Range(-1, 100),SerializeField]public int dp_itemPerHit = -1;
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
                    if (dp_itemPerHit == -1)
                    {
                        dp_itemPerHit = dp_itemCount;
                    }
                    for (int i = 0; i < dp_itemCount; i+=dp_itemPerHit)
                    {
                        SpawnItem(hitVelocity);
                        dp_itemCount--;
                    }
                    if (dp_itemCount <= 0)
                    {
                        base.ExecuteState();
                    }
                    break;
                default:
                    base.ExecuteState();
                    break;
            }
        }
        public virtual void SpawnItem(Vector3 velocityDir)
        {
            Vector3 spawnPos = dp_itemSpawnPos;
            dp_itemName.SpawnAsset(spawnPos, Quaternion.identity, transform.FindRoot(), item => {
                if (item.TryGetComponent(out Item itemScript)) {
                    itemScript.dp_isStatic = false;

                    Vector3 force = (velocityDir * 2f) + Vector3.up*dp_itemPopupForce;
                    Vector3 direction = Camera.main ? (Camera.main.transform.position - spawnPos).normalized : Vector3.up;
                    Vector3 calculatedForce =  (new Vector3(direction.x, 0, direction.z).normalized * 2f) + Vector3.up * Mathf.Max(Vector3.up.y * force.y, 3f);

                    itemScript.PopUpItem(calculatedForce);
                }
            }, error => {
                Debug.LogError($"Failed to load item: {error}");
            });
        }
    }
}
