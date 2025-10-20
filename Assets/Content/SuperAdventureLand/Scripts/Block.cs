namespace SuperAdventureLand
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;

    #if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.Events;
    [CustomEditor(typeof(Block), true)]
    public class BlockEditor : StandardEntityEditor<BlockState>
    {

    }
    #endif
    public enum BlockState
    {
        START,
        IDLE,
        IDLING,
        HIT,
        HITTING,
        ACTIVATE,
        ACTIVATING,
        DESTROY,
        DESTROYING
    }
    public class Block : StandardEntity<BlockState>
    {
        public Transform dp_unactivatedBlock;
        public Transform dp_activatedBlock;
        public string dp_hitSfx = "thud";
        private float dp_activateThreshold = 0.6f;
        private SpringJoint springJoint;
        [HideInInspector] public Vector3 hitVelocity;

        public override void Awake()
        {
            base.Awake();
            SaveOriginalRigidbody(true);
        }
        public override void Start()
        {
            base.Start();
            springJoint = GetComponent<SpringJoint>();
        }
        public override void ExecuteState()
        {
            switch (state)
            {
                case BlockState.IDLE:
                    break;
                case BlockState.IDLING:
                    if (isInPhysicsRange)
                    {
                        ogRigidbody.Restore(rb);
                    }
                    else
                    {
                        ogRigidbody.Freeze(rb);
                    }
                    break;
                case BlockState.HIT:
                    SetState(BlockState.IDLE);
                    break;
                case BlockState.ACTIVATE:
                    dp_hitSfx.PlaySFX(transform.position, 1f, UnityEngine.Random.Range(0.8f, 1.2f));
                    if (!shouldDestroy)
                    {
                        dp_unactivatedBlock?.gameObject.SetActive(false);
                        dp_activatedBlock?.gameObject.SetActive(true);
                    }
                    else
                    {
                        SetState(BlockState.DESTROY);
                    }
                    break;
                case BlockState.ACTIVATING:
                    break;
                case BlockState.DESTROY:
                    Destroy(gameObject);
                    break;
                default:
                    base.ExecuteState();
                    break;
            }
        }
        public override void Update()
        {
            base.Update();
            if (springJoint == null)
            {
                return;
            }
            Vector3 springDirection = (springJoint.connectedBody.position - transform.position).normalized;
            Vector3 velocity = rb.linearVelocity;
            if (springDirection.x >= springDirection.y && springDirection.x >= springDirection.z)
            {
                velocity.y = 0;
                velocity.z = 0;
            }
            else if (springDirection.y >= springDirection.x && springDirection.y >= springDirection.z)
            {
                velocity.x = 0;
                velocity.z = 0;
            }
            else
            {
                velocity.x = 0;
                velocity.y = 0;
            }

            bool breakCondition = Math.Abs(velocity.magnitude) > dp_activateThreshold || Vector3.Distance(springJoint.connectedBody.position, transform.position) > 0.1f;

            if (breakCondition && !isActivated && !isDestroying)
            {
                hitVelocity = Vector3.Normalize(velocity);
                SetState(BlockState.ACTIVATE);
            }
        }
        void FixedUpdate()
        {
            if (rb == null)
            {
                return;
            }
            Vector3 velocity = rb.linearVelocity;
            if (velocity.magnitude < 0.1f)
            {
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                return;
            }
            Vector3 springDirection = (springJoint.connectedBody.position - transform.position).normalized;
            springDirection = new Vector3(Math.Abs(springDirection.x), Math.Abs(springDirection.y), Math.Abs(springDirection.z));
            if (springDirection.x >= springDirection.y && springDirection.x >= springDirection.z)
            {
                rb.constraints = RigidbodyConstraints.FreezePositionY |
                RigidbodyConstraints.FreezePositionZ |
                RigidbodyConstraints.FreezeRotation;
                velocity.y = 0;
                velocity.z = 0;
            }
            else if (springDirection.y >= springDirection.x && springDirection.y >= springDirection.z)
            {
                rb.constraints = RigidbodyConstraints.FreezePositionX |
                RigidbodyConstraints.FreezePositionZ |
                RigidbodyConstraints.FreezeRotation;
                velocity.x = 0;
                velocity.z = 0;
            }
            else
            {
                rb.constraints = RigidbodyConstraints.FreezePositionX |
                RigidbodyConstraints.FreezePositionY |
                RigidbodyConstraints.FreezeRotation;
                velocity.x = 0;
                velocity.y = 0;
            }
            rb.linearVelocity = velocity;
        }

        public void TryHit(CollisionWrapper collision)
        {
            if (!isActivated && !isDestroying)
            {
                lastCollision = collision;
                SetState(BlockState.HIT);
            }
        }

        public virtual void PlayerHit(CollisionWrapper collision)
        {
            TryHit(collision);
        }
        public virtual void ProjectileHit(CollisionWrapper collision)
        {
            TryHit(collision);
        }
        public virtual void KickHit(CollisionWrapper collision)
        {

        }
        public virtual void MoleHit(CollisionWrapper collision)
        {
            TryHit(collision);
        }
        public virtual void EntityHit(CollisionWrapper collision)
        {
            lastCollision = collision;
            if (lastCollision.gameObject.TryGetComponent(out Creature creature))
            {
                if (creature.state == CreatureState.HIT || creature.state == CreatureState.HITTING)
                {
                    TryHit(collision);
                }
            }
        }
        public virtual void BodyHit(CollisionWrapper collision)
        {
            if (!isActivated && !isDestroying)
            {
                lastCollision = collision;
                SetState(BlockState.ACTIVATE);
            }
        }
        [HideInInspector]
        public virtual bool isActivated
        {
            get
            {
                return state == BlockState.ACTIVATE || state == BlockState.ACTIVATING;
            }
        }
        [HideInInspector]
        public virtual bool isDestroying
        {
            get
            {
                return state == BlockState.DESTROY || state == BlockState.DESTROYING;
            }
        }
        [HideInInspector]
        public bool shouldDestroy
        {
            get
            {
                return dp_activatedBlock == null;
            }
        }
        public void SetupInteractionFilters()
        {
    #if UNITY_EDITOR
            interactionFilters = new InteractionFilter[] {
                new InteractionFilter {
                    layers = new string[] { "Player" },
                    tags = new string[] { "Player" },
                    onInteractionEnter = new UnityEvent<CollisionWrapper>(),
                    onInteractionExit = new UnityEvent<CollisionWrapper>()
                },
                new InteractionFilter {
                    tags = new string[] { "ActiveHit" },
                    onInteractionEnter = new UnityEvent<CollisionWrapper>(),
                    onInteractionExit = new UnityEvent<CollisionWrapper>()
                },
                new InteractionFilter {
                    layers = new string[] { "Player" },
                    tags = new string[] { "Kicker" },
                    onInteractionEnter = new UnityEvent<CollisionWrapper>(),
                    onInteractionExit = new UnityEvent<CollisionWrapper>()
                },
                new InteractionFilter {
                    layers = new string[] { "Player" },
                    tags = new string[] { "Mole" },
                    onInteractionEnter = new UnityEvent<CollisionWrapper>(),
                    onInteractionExit = new UnityEvent<CollisionWrapper>()
                },
                new InteractionFilter {
                    layers = new string[] { "Entity" },
                    onInteractionEnter = new UnityEvent<CollisionWrapper>(),
                    onInteractionExit = new UnityEvent<CollisionWrapper>()
                }
            };

            UnityEventTools.AddPersistentListener(interactionFilters[0].onInteractionEnter, PlayerHit);
            UnityEventTools.AddPersistentListener(interactionFilters[1].onInteractionEnter, ProjectileHit);
            UnityEventTools.AddPersistentListener(interactionFilters[2].onInteractionEnter, KickHit);
            UnityEventTools.AddPersistentListener(interactionFilters[3].onInteractionEnter, MoleHit);
            UnityEventTools.AddPersistentListener(interactionFilters[4].onInteractionEnter, EntityHit);
            EditorUtility.SetDirty(this);
    #endif
        }
    }
}
