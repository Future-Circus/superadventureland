namespace SuperAdventureLand.Scripts
{
    using UnityEngine;

    public class BreakableItemBlock : ItemBlock
    {
        public override void ExecuteState() {
            base.ExecuteState();
            switch (state) {
                case BlockState.ACTIVATE:
                    mainCollider.enabled = false;
                    base.ExecuteState();
                    dp_activatedBlock.SetParent(null);
                    Extensions.Wait(this, 0.4f, () => {
                        SetState(BlockState.DESTROY);
                    });
                    Destroy(dp_activatedBlock.gameObject, 4f);
                    break;
            }
        }
    }
}
