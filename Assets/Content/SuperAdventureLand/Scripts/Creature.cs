namespace SuperAdventureLand.Scripts
{
    using System;
    using UnityEngine;
    using System.Linq;
    using UnityEngine.Events;
    using System.Collections.Generic;

    #if UNITY_EDITOR
    using UnityEditor;
    using UnityEditorInternal;

    [CustomEditor(typeof(Creature), true)]
    public class CreatureEditor : StandardEntityEditor<CreatureState>
    {

    }
    #endif

    public enum CreatureState {
        START,
        STARTING,
        IDLE,
        IDLING,
        MOVE,
        MOVING,
        RUN,
        RUNNING,
        TARGET,
        TARGETING,
        TURN,
        TURNING,
        FLY,
        FLYING,
        HIT,
        HITTING,
        ATTACK,
        ATTACKING,
        DIE,
        DYING,
        STONE,
        STONING,
        KICK,
        KICKING,
        LAUNCH,
        LAUNCHING,
        SHOOT,
        SHOOTING,
        TAUNT,
        TAUNTING,
        HIDE,
        HIDING,
        UNHIDE,
        UNHIDING,
        CRASH,
        CRASHING
    }
    public class Creature : StandardEntity<CreatureState>
    {
        public class RigidbodyJoint {
            public Rigidbody rb;
            public Collider collider;

            public RigidbodyJoint(Rigidbody rb) {
                this.rb = rb;
                this.collider = rb.GetComponent<Collider>();
            }
        }
        [HideInInspector] protected RigidbodyJoint[] ragdollJoints;
        [HideInInspector] public Collision collisionInfo;
        [HideInInspector] public Transform rbTransform {
            get {
                if (!mainCollider.enabled) {
                    return ragdollJoints[1].rb.transform;
                }
                return transform;
            }
        }

        public override void Awake () {
            base.Awake();
            if (transform.childCount > 0) {
                Transform ragdollRoot = gameObject.transform?.GetChild(0);
                Rigidbody[] childRigidbodies = ragdollRoot.GetComponentsInChildren<Rigidbody>();
                if (childRigidbodies != null && childRigidbodies.Length > 0) {
                    ragdollJoints = new RigidbodyJoint[childRigidbodies.Length];
                    for (int i = 0; i < childRigidbodies.Length; i++) {
                        ragdollJoints[i] = new RigidbodyJoint(childRigidbodies[i]);
                    }
                }
            }
            SaveOriginalPosition();
            ToggleRagdoll(false);
        }

        public virtual void ToggleRagdoll(bool enable)
        {
            if (enable)
            {
                if (rb != null) {
                    // Freeze the Rigidbody to prevent unwanted motion
                    rb.constraints = RigidbodyConstraints.FreezeAll;
                    rb.linearVelocity = Vector3.zero; // Clear velocity
                    rb.angularVelocity = Vector3.zero;
                }
                if (mainCollider != null)
                {
                    mainCollider.enabled = false;
                }
            }
            else
            {
                if (rb != null) {
                    // Restore Rigidbody constraints if needed
                    rb.constraints = RigidbodyConstraints.FreezeRotation;
                }
                if (mainCollider != null)
                {
                    mainCollider.enabled = true;
                }
            }
            // Enable/Disable Ragdoll Rigidbodies and Colliders
            if (ragdollJoints != null) {
                foreach (RigidbodyJoint joint in ragdollJoints)
                {
                    if (joint.collider != null) {
                        joint.collider.enabled = enable;
                    }
                    joint.rb.isKinematic = !enable;
                }
            }
        }
        public virtual void Kill(Collision collision = null) {
            collisionInfo = collision;
            SetState(CreatureState.DIE);
        }

        public override void ExecuteState () {
            switch (state) {
                case CreatureState.START:
                    //by default we start our creature in the idle state
                    SetState(CreatureState.IDLE);
                    break;
                case CreatureState.DIE:
                    ToggleRagdoll(true);
                    break;
                case CreatureState.DYING:
                    break;
                default:
                    break;
            }
        }
        [HideInInspector] public bool isDead {
            get {
                return state == CreatureState.DIE || state == CreatureState.DYING;
            }
        }
        [HideInInspector] public bool isAlive {
            get {
                return !isDead;
            }
        }
        [HideInInspector] public bool isHitted {
            get {
                return state == CreatureState.HIT || state == CreatureState.HITTING;
            }
        }
        [HideInInspector] public bool isHiding {
            get {
                return state == CreatureState.HIDE || state == CreatureState.HIDING;
            }
        }
    }
}
