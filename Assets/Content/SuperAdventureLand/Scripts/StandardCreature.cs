namespace SuperAdventureLand
{
    using UnityEngine;
    using UnityEngine.Events;
    using Random = UnityEngine.Random;
    using System;

    #if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.Events;

    [CustomEditor(typeof(StandardCreature), true)]
    public class StandardCreatureEditor : AwareCreatureEditor
    {
        private CollisionWrapper SimulateCollision(string layer, string tag)
        {
            StandardCreature creature = (StandardCreature)target;
            GameObject collisionSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            collisionSphere.transform.localScale = Vector3.one * 0.1f;
            collisionSphere.layer = LayerMask.NameToLayer(layer);
            collisionSphere.tag = tag;
            SphereCollider collider = collisionSphere.AddComponent<SphereCollider>();

            EditorApplication.delayCall += () =>
            {
                if (collisionSphere != null)
                {
                    DestroyImmediate(collisionSphere);
                }
            };
            return new CollisionWrapper(collider);
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying)
            {
                StandardCreature creature = (StandardCreature)target;
                if (GUILayout.Button("Simulate Hit"))
                {
                    creature.PlayerHit(SimulateCollision("Player", "Player"));
                }

                if (GUILayout.Button("Simulate Kick"))
                {
                    creature.KickHit(SimulateCollision("Player", "Kicker"));
                }
            }
        }
    }

    #endif

    //This is a standard creature for Super Adventure Land
    public class StandardCreature : AwareCreature
    {
        [Serializable]
        public class HitSettings
        {
            [SerializeField] public float directionForce = 5.0f;
            [SerializeField] public float angularForce = 12.0f;
            [SerializeField] public float upwardForce = 10f;
        }
        [HideInInspector] public HitSettings hitSettings;

        public void SetupInteractionFilters()
        {
    #if UNITY_EDITOR
            interactionFilters = new InteractionFilter[] {
                new InteractionFilter {
                    layers = new string[] { "Level" },
                    tags = new string[] { "Block" },
                    onInteractionEnter = new UnityEvent<CollisionWrapper>(),
                    onInteractionExit = new UnityEvent<CollisionWrapper>()
                },
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
                    layers = new string[] { "Level" },
                    tags = new string[] { "Ground" },
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
                    tags = new string[] { "Mortar" },
                    onInteractionEnter = new UnityEvent<CollisionWrapper>(),
                    onInteractionExit = new UnityEvent<CollisionWrapper>()
                },
                new InteractionFilter {
                    tags = new string[] { "Lava" },
                    onInteractionEnter = new UnityEvent<CollisionWrapper>(),
                    onInteractionExit = new UnityEvent<CollisionWrapper>()
                }
            };

            UnityEventTools.AddPersistentListener(interactionFilters[0].onInteractionEnter, BlockHit);
            UnityEventTools.AddPersistentListener(interactionFilters[1].onInteractionEnter, PlayerHit);
            UnityEventTools.AddPersistentListener(interactionFilters[2].onInteractionEnter, ProjectileHit);
            UnityEventTools.AddPersistentListener(interactionFilters[3].onInteractionEnter, GroundHit);
            UnityEventTools.AddPersistentListener(interactionFilters[4].onInteractionEnter, KickHit);
            UnityEventTools.AddPersistentListener(interactionFilters[5].onInteractionEnter, MortarHit);
            UnityEventTools.AddPersistentListener(interactionFilters[6].onInteractionEnter, LavaHit);
            UnityEditor.EditorUtility.SetDirty(this);
    #endif
        }

        public override void OnValidate()
        {
    #if UNITY_EDITOR
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (!Application.isPlaying && (interactionFilters == null || interactionFilters.Length == 0))
            {
                SetupInteractionFilters();
            }
            base.OnValidate();
    #endif
        }

        public virtual void BlockHit(CollisionWrapper collision)
        {
            if (state == CreatureState.HIT || state == CreatureState.HITTING)
            {
                SetState(CreatureState.DIE);
            }
        }
        public virtual void PlayerHit(CollisionWrapper collision)
        {
            lastCollision = collision;
            SetState(CreatureState.HIT);
        }
        public virtual void ProjectileHit(CollisionWrapper collision)
        {
            lastCollision = collision;
            if (state == CreatureState.STONE || state == CreatureState.STONING) {
                SetState(CreatureState.HIT);
            } else {
                SetState(CreatureState.FLY);
            }
        }
        public virtual void GroundHit(CollisionWrapper collision)
        {
            lastCollision = collision;
            if (groundHitAfterKick || groundHitAfterFly)
            {
                SetState(CreatureState.DIE);
            }
        }
        public virtual void KickHit(CollisionWrapper collision)
        {
            lastCollision = collision;
            SetState(CreatureState.KICK);
        }
        public virtual void MortarHit(CollisionWrapper collision)
        {
            lastCollision = collision;
            SetState(CreatureState.HIT);
        }
        public virtual void LavaHit(CollisionWrapper collision)
        {
            lastCollision = collision;
            SetState(CreatureState.STONE);
        }
        public virtual void BumperHit(CollisionWrapper collision)
        {
            lastCollision = collision;
            SetState(CreatureState.FLY);
            Vector3 popUpForce = Vector3.up * 100f + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            Rigidbody ragdollRoot = ragdollJoints[1].rb;
            ragdollRoot.AddForce(popUpForce, ForceMode.Impulse);
        }

        private bool requireCoin = false;

        public override void ExecuteState()
        {
            switch (state)
            {
                case CreatureState.START:
                    base.ExecuteState();
                    animator.Update(Random.Range(0.00f, 10.00f));
                    break;
                case CreatureState.IDLE:
                    base.ExecuteState();
                    break;
                case CreatureState.HIT:
                    "slap".PlaySFX(transform.position);
                    SetState(CreatureState.DIE);
                    break;
                case CreatureState.KICK:
                    SetState(CreatureState.IDLE);
                    break;
                case CreatureState.DIE:
                    Vector3 position = rbTransform.position + new Vector3(0, 0.5f, 0); ;
                    "FX_SteamPuff".SpawnAsset(position, Quaternion.LookRotation(position - Camera.main.transform.position));
                    if (lastCollision.layer == LayerMask.NameToLayer("Level"))
                    {
                        "impact".PlaySFX(lastCollision.collisionPoint, 1f, Random.Range(0.8f, 1.2f));
                    }

                    //make sure only the player can get a coin
                    if (requireCoin || (lastCollision != null && (lastCollision.tag == "Player" || lastCollision.tag == "ActiveHit")))
                    {
                        "E_COIN".GetAsset<GameObject>(coinPrefab =>
                        {
                            GameObject coin = Instantiate(coinPrefab, position, Quaternion.identity, transform.FindRoot());
                            coin.GetComponent<Coin>().PopUpItem(new Vector3(Random.Range(-0.1f, 1f), 2, Random.Range(-0.1f, 1f)));
                            Destroy(gameObject);
                        }, error =>
                        {
                            Debug.LogError($"Failed to load coin: {error}");
                            Destroy(gameObject);
                        });
                    } else {
                        Destroy(gameObject);
                    }
                    break;
                case CreatureState.STONE:
                    //standardizes the effects of lava across all creatures
                    "FX_SteamPuff".SpawnAsset(transform.position + new Vector3(0, 0.5f, 0), Quaternion.identity);
                    animator.enabled = false;
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.freezeRotation = true;
                    rb.constraints = RigidbodyConstraints.FreezeAll;
                    var meshRenderers = GetComponentsInChildren<Renderer>();
                    "StoneMat".GetAsset<Material>(mat =>
                    {
                        foreach (var meshRenderer in meshRenderers)
                        {
                            meshRenderer.sharedMaterial = mat;
                        }
                    }, error =>
                    {
                        Debug.LogError($"Failed to load stone material: {error}");
                    });
                    gameObject.tag = "Stone";
                    gameObject.layer = LayerMask.NameToLayer("Level");
                    break;
                case CreatureState.FLY:
                    requireCoin = true;
                    ToggleRagdoll(true);
                    gameObject.layer = LayerMask.NameToLayer("Default");

                    if (TryGetComponent<EyeController>(out var eyeController)) {
                        eyeController.enabled = false;
                    }
                    Vector3 popUpForce = Vector3.up * 100f + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                    Rigidbody ragdollRoot = ragdollJoints[1].rb;
                    ragdollRoot.AddForce(popUpForce, ForceMode.Impulse);
                    Extensions.Wait(this, 5f, () =>
                    {
                        if (state == CreatureState.FLY || state == CreatureState.FLYING)
                        {
                            SetState(CreatureState.DIE);
                        }
                    });
                    break;
                default:
                    base.ExecuteState();
                    break;
            }
        }

        public async void AssignStoneTexture(Renderer[] meshRenderers)
        {
            string materialName = "Assets/Content/SuperAdventureLand/Materials/StoneMat.mat";
            var mat = await CoreExtensions.GetAsset<Material>(materialName);
            if (mat != null)
            {
                foreach (var meshRenderer in meshRenderers)
                {
                    meshRenderer.sharedMaterial = mat;
                }
            }
        }

        [HideInInspector]
        public bool groundHitAfterKick
        {
            get
            {
                return state == CreatureState.HIT || state == CreatureState.HITTING || state == CreatureState.KICK || state == CreatureState.KICKING;
            }
        }

        public bool groundHitAfterFly
        {
            get
            {
                return state == CreatureState.FLY || state == CreatureState.FLYING;
            }
        }
    }
}
