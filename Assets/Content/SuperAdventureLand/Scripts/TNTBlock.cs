namespace SuperAdventureLand.Scripts
{
    using UnityEngine;

    public class TNTBlock : Block
    {
        public GameObject dp_explosionPrefab;
        [HideInInspector] public string dp_explosionName = "FX_Explosion";
        public override void Awake () {
            base.Awake();
            if (dp_explosionPrefab != null) {
                dp_explosionName = dp_explosionPrefab.name;
            }
        }
        public override void ExecuteState() {
            switch (state) {
                case BlockState.ACTIVATE:
                    if (lastCollision != null && lastCollision.gameObject != null && lastCollision.gameObject.TryGetComponentInParent(out Creature creature)) {
                        creature.SetState(CreatureState.FLY);
                    }

                    dp_explosionName.SpawnAsset(transform.position, Quaternion.identity);
                    base.ExecuteState();
                    break;
                default:
                    base.ExecuteState();
                    break;
            }
        }

        public override void MoleHit(CollisionWrapper collision) {
            lastCollision = collision;
            SetState(BlockState.ACTIVATE);
        }
    }
}
