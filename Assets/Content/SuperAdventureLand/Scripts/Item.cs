namespace SuperAdventureLand.Scripts
{
    using UnityEngine;
    using UnityEngine.Events;
    using Random = UnityEngine.Random;
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.Events;
#endif
    public enum ItemState
    {
        START,
        STARTING,
        DROP,
        DROPPING,
        GROUND,
        GROUNDING,
        SPIN,
        SPINNING,
        RISE,
        RISING,
        COLLECT,
        COLLECTING,
        DESTROY,
        DESTROYING,
        DESTROYED,
        POP,
        POPPING,
        SPLASH,
        SPLASHING
    }

    public class Item : Entity<ItemState>
    {
        [HideInInspector] public float dp_targetHeight = 0.85f; // Target height for the item
        [HideInInspector] public float dp_trueHeight;
        [HideInInspector] public float dp_constantRotationSpeed = 210.0f; // Degrees per second for constant rotation
        [HideInInspector] public Vector3 dp_targetPosition;
        [HideInInspector] private float currentYRotation = 0.0f; // Track current Y-axis rotation for constant rotation
        [HideInInspector] public string dp_collectFx = "FX_CoinParticle";
        [HideInInspector] public string dp_collectSfx = "coin";
        [HideInInspector] public float dp_spawnUpwardForce = 8f;
        [HideInInspector] public float dp_spawnOutwardForce = 2f;
        public bool dp_isStatic = true;
        public bool dp_canSplash = false;

        private Vector3 popUpForce;
        private Vector3 popUpTorque;

        public override void Start()
        {
            base.Start();
            dp_trueHeight = transform.localPosition.y + dp_targetHeight;
        }
        public override void ExecuteState()
        {
            switch (state)
            {
                case ItemState.START:
                    if (dp_canSplash)
                    {
                        SetState(ItemState.SPLASH);
                    }
                    else if (dp_isStatic)
                    {
                        SetState(ItemState.SPIN);
                    }
                    else
                    {
                        SetState(ItemState.DROP);
                    }
                    break;
                case ItemState.DROP:
                    rb.useGravity = true;
                    rb.isKinematic = false;
                    break;
                case ItemState.SPIN:
                    rb.useGravity = false;
                    rb.constraints = RigidbodyConstraints.FreezeAll;
                    break;
                case ItemState.SPINNING:
                    currentYRotation += dp_constantRotationSpeed * Time.deltaTime;
                    currentYRotation %= 360;
                    transform.rotation = Quaternion.Euler(0, currentYRotation, 0);
                    break;
                case ItemState.GROUNDING:
                    if (rb.linearVelocity.magnitude < 0.05f)
                    {
                        SetState(ItemState.RISE);
                    }
                    break;
                case ItemState.RISE:
                    rb.linearVelocity = Vector3.zero;
                    rb.useGravity = false;
                    rb.constraints = RigidbodyConstraints.FreezeAll;
                    dp_targetPosition = new Vector3(transform.localPosition.x, dp_trueHeight, transform.localPosition.z);
                    currentYRotation = transform.eulerAngles.y;
                    break;
                case ItemState.RISING:
                    currentYRotation += dp_constantRotationSpeed * Time.deltaTime;
                    currentYRotation %= 360;
                    transform.rotation = Quaternion.Euler(0, currentYRotation, 0);
                    transform.localPosition = Vector3.Slerp(transform.localPosition, dp_targetPosition, 0.9f * Time.deltaTime);
                    if (Vector3.Distance(transform.localPosition, dp_targetPosition) < 0.01f)
                    {
                        SetState(ItemState.SPIN);
                    }
                    break;
                case ItemState.COLLECT:
                    dp_collectFx.SpawnAsset(transform.position, Quaternion.identity);
                    dp_collectSfx.PlaySFX(transform.position);
                    Destroy(gameObject);
                    break;
                case ItemState.DESTROY:
                    "FX_SteamPuff".SpawnAsset(transform.position, Quaternion.identity);
                    Destroy(gameObject);
                    break;
                case ItemState.POP:
                    rb.useGravity = true;
                    rb.isKinematic = false;
                    rb.AddForce(popUpForce, ForceMode.Impulse);
                    rb.AddTorque(popUpTorque, ForceMode.Impulse);
                    break;
                case ItemState.POPPING:
                    if (!stateWillChange)
                    {
                        SetState(ItemState.DROP);
                    }
                    break;
                case ItemState.SPLASH:
                    rb.useGravity = true;
                    rb.isKinematic = false;
                    Vector2 randomCircle = Random.insideUnitCircle.normalized;
                    Vector3 horizontalDir = new Vector3(randomCircle.x, 0f, randomCircle.y);
                    float upwardForce = Random.Range(dp_spawnUpwardForce * 0.6f, dp_spawnUpwardForce * 0.8f);
                    float outwardForce = Random.Range(dp_spawnOutwardForce * 0.6f, dp_spawnOutwardForce * 0.8f);
                    Vector3 finalForce = horizontalDir * outwardForce + Vector3.up * upwardForce;
                    rb.AddForce(finalForce, ForceMode.Impulse);
                    rb.AddTorque(Random.insideUnitSphere * outwardForce * 0.3f, ForceMode.Impulse);
                    break;
                case ItemState.SPLASHING:
                    if (transform.position.y < 0f)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.isKinematic = true;
                        SetState(ItemState.RISE);
                    }
                    break;
                default:
                    break;
            }
        }
        public virtual void PlayerHit(CollisionWrapper collision)
        {
            lastCollision = collision;
            SetState(ItemState.COLLECT);
        }

        public virtual void EnemyHit(CollisionWrapper collision)
        {
            lastCollision = collision;
            SetState(ItemState.COLLECT);
        }

        public virtual void GroundHit(CollisionWrapper collision)
        {
            if (isSpinning)
            {
                return;
            }

            if (lastCollision != null)
            {
                if (transform.parent != null)
                {
                    var worldToLocal = transform.parent.InverseTransformPoint(lastCollision.collisionPoint);
                    dp_trueHeight = worldToLocal.y + dp_targetHeight;
                }
                else
                {
                    dp_trueHeight = lastCollision.collisionPoint.y + dp_targetHeight;
                }
            }

            if (state == ItemState.DROP || state == ItemState.DROPPING)
            {
                SetState(ItemState.GROUNDING);
            }
        }
        public virtual void LevelHit(CollisionWrapper collision)
        {
            if (isSpinning)
            {
                return;
            }
            lastCollision = collision;
            if (lastCollision != null)
            {
                if (transform.parent != null)
                {
                    var worldToLocal = transform.parent.InverseTransformPoint(lastCollision.collisionPoint);
                    dp_trueHeight = worldToLocal.y + dp_targetHeight;
                }
                else
                {
                    dp_trueHeight = lastCollision.collisionPoint.y + dp_targetHeight;
                }
            }
            PopUpItem(Vector3.up * 5f + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)));
        }
        public virtual void LavaHit(CollisionWrapper collision)
        {
            lastCollision = collision;
            SetState(ItemState.DESTROY);
        }
        public void PopUpItem(Vector3 popUpForce, Vector3 popUpTorque = default)
        {
            this.popUpForce = popUpForce;
            this.popUpTorque = popUpTorque != Vector3.zero ? popUpTorque : Vector3.up * 0.1f;
            SetState(ItemState.POP);
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
                    layers = new string[] { "Level" },
                    tags = new string[] { "Ground" },
                    onInteractionEnter = new UnityEvent<CollisionWrapper>(),
                    onInteractionExit = new UnityEvent<CollisionWrapper>()
                },
                new InteractionFilter {
                    tags = new string[] { "Lava" },
                    onInteractionEnter = new UnityEvent<CollisionWrapper>(),
                    onInteractionExit = new UnityEvent<CollisionWrapper>()
                },
                new InteractionFilter {
                    layers = new string[] { "Level" },
                    onInteractionEnter = new UnityEvent<CollisionWrapper>(),
                    onInteractionExit = new UnityEvent<CollisionWrapper>()
                },
            };
            UnityEventTools.AddPersistentListener(interactionFilters[0].onInteractionEnter, PlayerHit);
            UnityEventTools.AddPersistentListener(interactionFilters[1].onInteractionEnter, GroundHit);
            UnityEventTools.AddPersistentListener(interactionFilters[2].onInteractionEnter, LavaHit);
            UnityEventTools.AddPersistentListener(interactionFilters[3].onInteractionEnter, LevelHit);
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public void OnValidate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && (interactionFilters == null || interactionFilters.Length == 0))
            {
                SetupInteractionFilters();
            }
#endif
        }

        public bool isSpinning
        {
            get
            {
                return state == ItemState.SPIN || state == ItemState.SPINNING;
            }
        }
    }
}
