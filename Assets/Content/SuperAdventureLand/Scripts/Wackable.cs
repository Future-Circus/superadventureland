namespace SuperAdventureLand.Scripts
{
    using System;
    using UnityEngine;
    using Random = UnityEngine.Random;
    using UnityEngine.Events;

    #if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.Events;
    [CustomEditor(typeof(Wackable), true)]
    public class WackableEditor : StandardEntityEditor<WackableState>
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();  // Draws the default inspector

            Wackable wackable = (Wackable)target;

            if (GUILayout.Button("Wack"))
            {
                wackable.lastCollision = new CollisionWrapper(wackable.gameObject);
                wackable.SetState(WackableState.WACK);
            }
        }
    }
    #endif
    public enum WackableState {
        START,
        IDLE,
        IDLING,
        WACK,
        WACKING,
        AIRBORNE,
        AIRBORNING,
        RESET,
        RESETING
    }
    public class Wackable : StandardEntity<WackableState>
    {
        public AudioSource smackSound;
        [HideInInspector] string dp_hitSfx = "slap";
        public float forceMultiplier = 2.0f; // Strength of the smack
        public float upwardMin = 2.0f;
        public GameObject target;
        public LayerMask targetLayer;
        public string targetTags = "";
        public bool resetAfterHit = false;
        [ShowIf("resetAfterHit")]
        public float resetTime = 3f;
        public override void ExecuteState() {
            switch (state) {
                case WackableState.START:
                    SaveOriginalPosition();
                    TrackJunk("ogTag", gameObject.tag);
                    break;
                case WackableState.IDLE:
                    gameObject.tag = GetJunk<string>("ogTag");
                    break;
                case WackableState.IDLING:
                    if (!isGrounded) {
                        SetState(WackableState.AIRBORNE);
                    }
                    break;
                case WackableState.WACK:
                    if (lastCollision != null && lastCollision.gameObject != null && !lastCollision.gameObject.IsDestroyed() && lastCollision.gameObject.TryGetComponent(out PlayerInteractor interactor)) {
                        Wack(interactor.GetDirection(), interactor.GetVelocity());
                    } else {
                        Wack(transform.position - lastCollision.contactPoint, lastCollision.relativeVelocity.magnitude);
                    }
                    break;
                case WackableState.WACKING:
                    if (timeSinceStateChange > 0.4f) {
                        SetState(WackableState.AIRBORNE);
                    }
                    break;
                case WackableState.AIRBORNING:
                    if (isGrounded) {
                        SetState(WackableState.RESET);
                    }
                    break;
                case WackableState.RESET:
                    if (!resetAfterHit) {
                        SetState(WackableState.IDLE);
                    }
                    break;
                case WackableState.RESETING:
                    if (timeSinceStateChange > resetTime && isStill) {
                        transform.position = ogPosition.position;
                        transform.rotation = ogPosition.rotation;
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        "FX_SteamCloud".SpawnAsset(transform.position, transform.rotation);
                        SetState(WackableState.IDLE);
                    }
                    break;
                default:
                    break;
            }
        }
        public virtual void PlayerHit(CollisionWrapper collision) {
            if (!isWacking) {
                lastCollision = collision;
                SetState(WackableState.WACK);
            }
        }
        public virtual void KickHit(CollisionWrapper collision) {
            lastCollision = collision;
            SetState(WackableState.WACK);
        }
        public virtual void ProjectileHit(CollisionWrapper collision) {
            lastCollision = collision;
            SetState(WackableState.WACK);
        }
        public virtual void GroundHit(CollisionWrapper collision) {
            //SetState(WackableState.RESET);
        }
        public void Wack(Vector3 impactDirection, float impactForce = 1f)
        {
            // Apply the force to the Rigidbody
            rb.freezeRotation = false;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.None;
            rb.useGravity = true;
            rb.WakeUp();

            if (smackSound != null) {
                smackSound.Play();
            } else if (!string.IsNullOrEmpty(dp_hitSfx)) {
                dp_hitSfx.PlaySFX(transform.position, 1f, UnityEngine.Random.Range(0.8f, 1.2f));
            }

            if (target != null)
            {
                if (rb.LaunchAtTarget(target)) {
                    return;
                }
            } else if (!string.IsNullOrEmpty(targetTags))
            {
                if (rb.LaunchAtLayerWithTag(targetLayer, targetTags, 20f)) {
                    return;
                }
            } else if (targetLayer != 0)
            {
                if (rb.LaunchAtLayer(targetLayer, 20f)) {
                    return;
                }
            }

            gameObject.tag = "ActiveHit";

            impactDirection.Normalize();

            Vector3 hittedForce = impactDirection * impactForce * forceMultiplier;

            hittedForce.y = Math.Max(upwardMin,hittedForce.y);

            if (forceMultiplier == 0)
                return;

            rb.AddForce(hittedForce, ForceMode.Impulse);

            Vector3 randomTorque = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            );
            rb.AddTorque(randomTorque, ForceMode.Impulse);
        }

        public override void SetupInteractionFilters() {
            #if UNITY_EDITOR
            interactionFilters = new InteractionFilter[] {
                new InteractionFilter {
                    layers = new string[] { "Player" },
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
                    layers = new string[] { "Projectile" },
                    tags = new string[] { "ActiveHit" },
                    onInteractionEnter = new UnityEvent<CollisionWrapper>(),
                    onInteractionExit = new UnityEvent<CollisionWrapper>()
                },
                new InteractionFilter {
                    layers = new string[] { "Level" },
                    tags = new string[] { "Ground" },
                    onInteractionEnter = new UnityEvent<CollisionWrapper>(),
                    onInteractionExit = new UnityEvent<CollisionWrapper>()
                }
            };

            UnityEventTools.AddPersistentListener(interactionFilters[0].onInteractionEnter, PlayerHit);
            UnityEventTools.AddPersistentListener(interactionFilters[1].onInteractionEnter, KickHit);
            UnityEventTools.AddPersistentListener(interactionFilters[2].onInteractionEnter, ProjectileHit);
            UnityEventTools.AddPersistentListener(interactionFilters[3].onInteractionEnter, GroundHit);
            #endif
        }
        public bool isAirborne {
            get {
                return state == WackableState.WACK || state == WackableState.WACKING || state == WackableState.AIRBORNE || state == WackableState.AIRBORNING;
            }
        }

        public bool isWacking {
            get {
                return state == WackableState.WACK || state == WackableState.WACKING;
            }
        }

        public bool isGrounded {
            get {
                return Physics.Raycast(transform.position, Vector3.down, gameObject.GetBounds().size.y/2f+0.1f, LayerMask.GetMask("Level"));
            }
        }

        public bool isStill {
            get {
                return rb.linearVelocity.magnitude < 0.1f;
            }
        }
    }
}
