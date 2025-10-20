namespace SuperAdventureLand
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class BrickBlock : Block
    {
        public override void Start()
        {
            base.Start();
            dp_hitSfx = "bricks";
        }
        public override void ExecuteState() {
            base.ExecuteState();
            switch (state) {
                case BlockState.ACTIVATE:
                    mainCollider.enabled = false;
                    base.ExecuteState();
                    // unparent the brick block from the parent
                    dp_activatedBlock.transform.SetParent(null);

                    Rigidbody[] childRigidbodies = dp_activatedBlock.GetComponentsInChildren<Rigidbody>();
                    MeshCollider[] meshColliders = dp_activatedBlock.GetComponentsInChildren<MeshCollider>();

                    foreach (MeshCollider meshCollider in meshColliders)
                    {
                        meshCollider.enabled = true;
                    }

                    mainCollider.enabled = false;
                    hitVelocity = Vector3.Max(hitVelocity,hitVelocity.normalized*25f);

                    foreach (Rigidbody piece in childRigidbodies)
                    {
                        piece.isKinematic = false;
                        piece.AddForce(hitVelocity);
                    }
                    Destroy(dp_activatedBlock.gameObject, 4f);
                    SetState(BlockState.DESTROY);
                    break;
            }
        }
    }
}
