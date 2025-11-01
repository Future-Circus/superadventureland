namespace SuperAdventureLand
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Random = UnityEngine.Random;

    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(CannonBehaviour))]
    public class CannonBehaviourEditor : AwareCreatureEditor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if (GUILayout.Button("Simulate Hit")) {
                ((CannonBehaviour)target).Hit();
            }
        }
    }
    #endif
    public class CannonBehaviour : AwareCreature
    {
        public Transform cannonBase; // The part that rotates around the up axis
        public Transform cannonBarrel; // The part that rotates around the right axis
        public Transform target; // The target to aim at
        public Transform firePoint; // The point where projectiles are fired
        public GameObject projectilePrefab; // The projectile to shoot
        public ParticleSystem muzzleFlash; // The particle system for the muzzle flash
        public Animator moleAnimator;
        public ParticleSystem hitEffect;
        public Transform cannonLidHinge;
        public Transform lidPivot1;
        public Transform lidPivot2;
        public GameObject hitbox;
        public ParticleSystem fireworks;
        public GameObject ground;
        public ParticleSystem groundParticles;
        public float rotationSpeed = 5f; // Speed of cannon rotation
        public float projectileSpeed = 1f; // Speed of the projectile
        public float gravity = 9.81f; // Gravity in your physics world
        public Vector3 baseRotationOffset = Vector3.zero; // Offset for the base rotation
        public Vector3 barrelRotationOffset = Vector3.zero; // Offset for the barrel rotation
        public float minBarrelAngle = 45f; // Minimum pitch angle for the barrel
        public float maxBarrelAngle = 70f; // Maximum pitch angle for the barrel
        public AudioClip jubboSound;
        public AudioClip openSound;
        public AudioClip closeSound;
        private Vector3 lastCameraPos = Vector3.zero;
        private int hp = 3;
        private Coroutine StateCoroutine;
        private List<Creature> projectiles = new List<Creature>();
        private Vector3 groundScale = new Vector3(0,0,0);
        private Coroutine lidCoroutine;
        public override void OnValidate()
        {
            #if UNITY_EDITOR
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (jubboSound == null) {
                jubboSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Content/SuperAdventureLand/Audio/jubbo_wee.mp3");
            }
            if (openSound == null) {
                openSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Content/SuperAdventureLand/Audio/hatch_open.mp3");
            }
            if (closeSound == null) {
                closeSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Content/SuperAdventureLand/Audio/hatch_close.mp3");
            }
            base.OnValidate();
            #endif
        }
        public override void ExecuteState()
        {
            switch (state)
            {
                case CreatureState.START:
                    if (StateCoroutine != null)
                        StopCoroutine(StateCoroutine);

                    ToggleRagdoll(false);

                    moleAnimator.SetBool("isHiding", true);
                    target.localPosition = Vector3.zero;
                    AimCannon();
                    base.ExecuteState();
                    break;
                case CreatureState.TARGET:
                    target.position = Camera.main.transform.position+Camera.main.transform.forward;
                    break;
                case CreatureState.TARGETING:
                    if (StateCondition()) {
                        if (timeSinceStateChange > 1f) {
                            //we want to make sure the player is stopped before we start aiming
                            if (Camera.main.transform.position.Distance(lastCameraPos) > 1f) {
                                target.position = Camera.main.transform.position+Camera.main.transform.forward;
                                lastCameraPos = Camera.main.transform.position;
                            } else {
                                if (AimCannon()) {
                                    SetState(CreatureState.SHOOT);
                                }
                            }
                        }
                    } else {
                        SetState(CreatureState.IDLE);
                    }
                    break;
                case CreatureState.SHOOT:
                    if (lidCoroutine != null)
                        StopCoroutine(lidCoroutine);
                    moleAnimator.SetBool("isHiding", true);
                    lidCoroutine = StartCoroutine(ToggleLid(false));
                    if (StateCoroutine != null)
                        StopCoroutine(StateCoroutine);
                    StateCoroutine = Extensions.Wait(this,1, () => {
                        FireProjectile();
                        StateCoroutine = Extensions.Wait(this,1, () => {
                            SetState(CreatureState.IDLE);
                        });
                    });
                    break;
                case CreatureState.IDLE:
                    ShakeEngine(transform,new Vector3(0,0,0));
                    if (lidCoroutine != null)
                        StopCoroutine(lidCoroutine);
                    groundScale = Vector3.zero;
                    groundParticles.Stop();
                    StartCoroutine(ToggleLid(true));
                    moleAnimator.SetBool("isHiding", false);
                    if (StateCoroutine != null)
                        StopCoroutine(StateCoroutine);
                    break;
                case CreatureState.HIT:
                    if (lidCoroutine != null)
                        StopCoroutine(lidCoroutine);
                    lidCoroutine = StartCoroutine(ToggleLid(true));
                    moleAnimator.SetBool("isHiding", false);
                    moleAnimator.SetBool("isHit", true);
                    hitEffect.Play();
                    hp--;
                    if (StateCoroutine != null)
                        StopCoroutine(StateCoroutine);

                    if (hp > 0) {
                        //wait for animation to finish
                    } else {
                        SetState(CreatureState.DIE);
                    }
                    break;
                case CreatureState.HITTING:
                    ShakeEngine(transform,new Vector3(0,0,0.1f),Mathf.Max(2f-timeSinceStateChange*2f,0f),Mathf.Max(100f-timeSinceStateChange*100f,0f));
                    break;
                case CreatureState.DIE:
                    gameObject.PrioritizeAsset();
                    foreach (var projectile in projectiles) {
                        if (projectile != null && !projectile.IsDestroyed()) {
                            projectile.GetComponent<Creature>().Kill();
                        }
                    }
                    moleAnimator.enabled = false;
                    Destroy(hitbox);
                    ToggleRagdoll(true);
                    fireworks.Play();
                    SunManager.Instance?.OnEvent("mole-cannon-defeated");
                    break;
                case CreatureState.HIDE:
                    if (StateCoroutine != null)
                        StopCoroutine(StateCoroutine);
                    if (lidCoroutine != null)
                        StopCoroutine(lidCoroutine);
                    moleAnimator.SetBool("isHiding", true);
                    lidCoroutine = StartCoroutine(ToggleLid(false));
                    groundScale = Vector3.one;
                    groundParticles.Play();
                    break;
                case CreatureState.HIDING:
                    Vector3 targetPosition = new Vector3(transform.localPosition.x,-1.8f,transform.localPosition.z);
                    transform.localPosition = Vector3.Lerp(transform.localPosition,targetPosition,Time.deltaTime*2f);

                    if (transform.localPosition.y <= -1.7f) {
                        transform.localPosition = targetPosition;
                        groundParticles.Stop();
                    }

                    if (playerIsNotClose) {
                        SetState(CreatureState.UNHIDE);
                    }

                    break;
                case CreatureState.UNHIDE:
                    if (lidCoroutine != null)
                        StopCoroutine(lidCoroutine);
                    moleAnimator.SetBool("isHiding", false);
                    lidCoroutine = StartCoroutine(ToggleLid(true));
                    groundParticles.Play();
                    break;
                case CreatureState.UNHIDING:
                    Vector3 targetPosition0 = new Vector3(transform.localPosition.x,0,transform.localPosition.z);
                    transform.localPosition = Vector3.Lerp(transform.localPosition,targetPosition0,Time.deltaTime*2f);

                    if (transform.localPosition.y >= -0.1f) {
                        transform.localPosition = new Vector3(transform.localPosition.x,0f,transform.localPosition.z);
                        SetState(CreatureState.IDLE);
                    }

                    break;
                default:
                    base.ExecuteState();
                    break;
            }
            if (playerIsTooClose && !isHiding) {
                if (isAlive && !isHitted) {
                    SetState(CreatureState.HIDE);
                }
            }
            ground.transform.localScale = Vector3.Lerp(ground.transform.localScale, groundScale, Time.deltaTime * 2f);
        }
        private bool AimCannon () {
            Vector3 targetDirection = target.position - cannonBase.position;

            Vector3 baseDirection = Vector3.ProjectOnPlane(targetDirection, Vector3.up);
            Quaternion baseRotation = Quaternion.LookRotation(baseDirection, Vector3.up);
            baseRotation *= Quaternion.Euler(baseRotationOffset);
            cannonBase.rotation = Quaternion.Slerp(cannonBase.rotation, baseRotation, Time.deltaTime * rotationSpeed);
            cannonBase.localRotation = Quaternion.Euler(0, 0, cannonBase.localRotation.eulerAngles.z);

            float angle = 260f-40f*Mathf.Clamp(cannonBase.position.Distance(target.position)/25,0,1);

            float d = Quaternion.Angle(cannonBase.rotation, baseRotation);

            if (d < 2) {
                Quaternion barrelRotation = Quaternion.AngleAxis(angle, Vector3.right);
                barrelRotation *= Quaternion.Euler(barrelRotationOffset);
                cannonBarrel.localRotation = Quaternion.Slerp(cannonBarrel.localRotation, barrelRotation, Time.deltaTime * rotationSpeed);
                float dd = Quaternion.Angle(cannonBarrel.localRotation, barrelRotation);
                if (dd >= 2) {
                    ShakeEngine(cannonBarrel,new Vector3(0,0,0.06f));
                }
                return dd < 2;
            } else {
                ShakeEngine(cannonBase,Vector3.zero);
                return false;
            }
        }
        private IEnumerator ToggleLid(bool open) {
            if (!open) {
                closeSound.PlaySFX(cannonLidHinge.position, 1f, 1f);
            } else {
                openSound.PlaySFX(cannonLidHinge.position, 1f, 1f);
            }
            float t = 0;
            float duration = 1f;
            Quaternion start = cannonLidHinge.localRotation;
            Quaternion end = open ? lidPivot1.localRotation : lidPivot2.localRotation;
            while (t < duration) {
                t += Time.deltaTime;
                float easedT = EaseOutBounce(t / duration);
                cannonLidHinge.localRotation = Quaternion.Slerp(start,end,easedT);
                yield return null;
            }
        }
        private float EaseOutBounce(float t) {
            if (t < (1 / 2.75f)) {
                return 7.5625f * t * t;
            } else if (t < (2 / 2.75f)) {
                t -= (1.5f / 2.75f);
                return 7.5625f * t * t + 0.75f;
            } else if (t < (2.5f / 2.75f)) {
                t -= (2.25f / 2.75f);
                return 7.5625f * t * t + 0.9375f;
            } else {
                t -= (2.625f / 2.75f);
                return 7.5625f * t * t + 0.984375f;
            }
        }
       private void FireProjectile()
        {
            if (projectilePrefab == null || firePoint == null) return;

            // Play the muzzle flash particle system
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }
            if (TryGetComponent<AudioSource>(out var audioSource))
            {
                audioSource.Play();
            }
            jubboSound.PlaySFX(firePoint.position, 1f, UnityEngine.Random.Range(0.8f, 1.2f));

            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation, transform.FindRoot());
            projectile.SetActive(true);
            projectiles.Add(projectile.GetComponent<Creature>());
            // Only keep 3 projectiles at a time
            if (projectiles.Count > 3)
            {
                if (projectiles[0] != null && projectiles[0].gameObject != null) projectiles[0].Kill();
                projectiles.RemoveAt(0);
            }
            // Calculate velocity to hit the target
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // use most recent player position to calculate velocity
                target.position = Camera.main.transform.position+Camera.main.transform.forward;
                float distance = Vector3.Distance(firePoint.position, target.position);
                Vector3 fwd = firePoint.forward;
                float initialSpeedSquared = distance * projectileSpeed;
                if (projectile.TryGetComponent(out Creature creature)) {
                    creature.SetState(CreatureState.LAUNCH);
                }
                rb.useGravity = true;
                rb.isKinematic = false;
                rb.freezeRotation = false;
                rb.constraints = RigidbodyConstraints.None;
                rb.linearDamping = 0.01f;
                rb.angularDamping = 0f;
                rb.AddForce(fwd*initialSpeedSquared, ForceMode.Impulse);
                rb.AddTorque(new Vector3(Random.Range(0, 2) * 2 - 1,Random.Range(0, 2) * 2 - 1,Random.Range(0, 2) * 2 - 1) * initialSpeedSquared*4, ForceMode.Impulse);
            }
        }
        private void ShakeEngine(Transform shakeThis, Vector3 shakeStartPos, float shakeIntensity = 0.05f, float shakeSpeed = 100f)
        {
            var noiseSeed = 32f;
            var shakeDirection = new Vector3(0.2f, -0.2f, 1);
            // Generate perlin noise-based offsets
            float noiseX = Mathf.PerlinNoise(Time.time * shakeSpeed, noiseSeed) - 0.5f;
            float noiseY = Mathf.PerlinNoise(Time.time * shakeSpeed, noiseSeed + 1) - 0.5f;
            float noiseZ = Mathf.PerlinNoise(Time.time * shakeSpeed, noiseSeed + 2) - 0.5f;

            // Combine shake direction with noise
            Vector3 shakeOffset = new Vector3(
                noiseX * shakeDirection.x,
                noiseY * shakeDirection.y,
                noiseZ * shakeDirection.z
            ) * shakeIntensity;

            // Apply the shake
            shakeThis.localPosition = shakeStartPos+shakeOffset;
        }
        public void Animation_EndHit () {
            moleAnimator.SetBool("isHit", false);
            SetState(CreatureState.TARGET);
        }
        public void Hit () {
            if (state != CreatureState.HIT)
                SetState(CreatureState.HIT);
        }
        public bool playerIsTooClose {
            get {
                return Vector3.Distance(transform.position,Camera.main.transform.position) < 4f;
            }
        }
        public bool playerIsNotClose {
            get {
                return Vector3.Distance(transform.position,Camera.main.transform.position) > 4.5f;
            }
        }
    }
}