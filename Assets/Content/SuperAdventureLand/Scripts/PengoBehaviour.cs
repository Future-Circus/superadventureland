namespace SuperAdventureLand
{
    using UnityEngine;
    using System;
    public class PengoBehaviour : StandardCreature
    {
        public GameObject decoyGrenade;
        public GameObject grenadePrefab;
        public Transform launchPoint;

        [HideInInspector] public Vector3 targetPosition;
        public float throwInterval = 4f;
        private float lastThrowTime = 0f;
        public override void Awake() {
            base.Awake();
            decoyGrenade?.SetActive(false);
        }
        public override void GroundHit(CollisionWrapper collision)
        {
            //for pengos a kick throws them but doesn't kill them
            if (groundHitAfterKick) {
                SetState(CreatureState.IDLE);
            } else if (groundHitAfterFly) {
                SetState(CreatureState.DIE);
            }
        }
        public override void ExecuteState() {
            switch (state) {
                case CreatureState.KICK:
                    if (lastCollision != null && lastCollision.gameObject != null && lastCollision.gameObject.TryGetComponent(out PlayerInteractor interactor)) {
                        currentTargetPosition = Camera.main.transform.position;
                        Vector3 hittedForce = interactor.GetDirection().normalized * Mathf.Min(2f,interactor.GetVelocity());
                        Debug.Log("Kick force: " + hittedForce.magnitude);
                        hittedForce.y = Math.Max(5f,hittedForce.y);
                        Rigidbody rb = GetComponent<Rigidbody>();
                        rb.useGravity = true;
                        rb.WakeUp();
                        rb.AddForce(hittedForce, ForceMode.Impulse);
                    } else {
                        SetState(CreatureState.IDLE);
                    }
                    break;
                case CreatureState.KICKING:
                    base.ExecuteState();
                    Vector3 currentPosition = transform.position;
                    currentTargetPosition.y = currentPosition.y;
                    Vector3 direction = (currentTargetPosition - currentPosition).normalized;
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
                    break;
                case CreatureState.TARGET:
                    base.ExecuteState();
                    (UnityEngine.Random.Range(0,2) < 1 ? "pengo_laugh" : "pengo_laugh_alt").PlaySFX(transform.position, 1f, UnityEngine.Random.Range(0.8f, 1.2f));
                    break;
                case CreatureState.TARGETING:
                    base.ExecuteState();
                    if (StateCondition()) {
                        Vector3 directionToPlayer = (Camera.main.transform.position - transform.position).normalized;
                        float dotProduct = Vector3.Dot(transform.forward, directionToPlayer);
                        if (dotProduct > 0.9f && Time.time - lastThrowTime >= throwInterval) {
                            SetState(CreatureState.ATTACK);
                        }
                    }
                    break;
                case CreatureState.ATTACK:

                    Transform player = Camera.main?.transform;
                    if (player != null) {
                        targetPosition = new Vector3(player.position.x,0,player.position.z) + player.forward*1f;
                    } else {
                        targetPosition = transform.position + transform.forward*3f;
                    }

                    break;
                case CreatureState.ATTACKING:
                    if (StateCondition()) {

                    } else {
                        SetState(CreatureState.IDLE);
                    }
                    break;
                default:
                    base.ExecuteState();
                    break;
            }
        }
        public override bool StateCondition() {
            switch (state) {
                case CreatureState.ATTACKING:
                    return playerInRange;
                default:
                    return base.StateCondition();
            }
        }
        public virtual void Animator_ShowGrenade () {
            decoyGrenade?.SetActive(true);
            "pengo_throw".PlaySFX(transform.position, 1f, UnityEngine.Random.Range(0.8f, 1.2f));
        }
        public virtual void Animator_SpawnGrenade () {
            decoyGrenade?.SetActive(false);
            if (grenadePrefab != null && launchPoint != null) {
                "egg_throw".PlaySFX(transform.position, 1f, UnityEngine.Random.Range(0.9f, 1.1f));
                GameObject projectile = Instantiate(grenadePrefab, launchPoint.position, Quaternion.identity);
                Rigidbody rb = projectile.GetComponent<Rigidbody>();
                rb.LaunchAtTarget(targetPosition, 1f, true, projectile.tag);
                lastThrowTime = Time.time;
            } else {
                if (debugger) {
                    Debug.LogError("Animator_SpawnGrenade - Missing references in script");
                }
            }
        }
        public virtual void Animator_EndThrow () {
            SetState(CreatureState.IDLE);
        }
    }
}
