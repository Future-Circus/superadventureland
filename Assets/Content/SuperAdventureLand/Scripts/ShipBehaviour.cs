namespace SuperAdventureLand
{
    using UnityEngine;
    using System;

    #if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(ShipBehaviour))]
    public class ShipBehaviourEditor : PathCreatureEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            ShipBehaviour shipBehaviour = (ShipBehaviour)target;
            if (GUILayout.Button("Start Battle"))
            {
                shipBehaviour.StartBattle();
            }
            if (GUILayout.Button("Hit"))
            {
                shipBehaviour.ProjectileHit();
            }
            if (GUILayout.Button("Crash"))
            {
                shipBehaviour.Crash();
            }
        }
         private Vector3 PlayModeConversion (Vector3 v, ShipBehaviour j) {
            if (Application.isPlaying) {
                return j.ogPosition.TransformPoint(v);
            } else {
                return j.transform.TransformPoint(v);
            }
        }
        public override void OnSceneGUI()
        {
            base.OnSceneGUI();
            ShipBehaviour script = (ShipBehaviour)target;

            if (script.waypoints == null || script.waypoints.Length == 0) return;

              Handles.color = Color.green;
                for (int i = 0; i < script.waypoints.Length - 1; i++)
                {
                    Handles.DrawLine(
                        PlayModeConversion(script.waypoints[i], script),
                        PlayModeConversion(script.waypoints[i + 1], script)
                    );
                }

            if (script.waypoints.Length > 0)
            {
                Handles.DrawLine(
                    PlayModeConversion(script.waypoints[script.waypoints.Length - 1], script),
                    PlayModeConversion(script.waypoints[0], script)
                );
            }
        }
    }

    #endif

    public class ShipBehaviour : PathCreature
    {
        public Transform cannonsnoutAnchor;
        public CannonSnoutBehaviour cannonsnout;
        public AudioSource wingFlapAudio;
        [HideInInspector] public int hp = 3;
        public ParticleSystem[] healthMarkers;
        public ParticleSystem hitParticle;
        public BobAndWobble bobAndWobble;
        public Transform CrashPoint;
        public GameObject groundPiece;
        public ParticleSystem groundParticle;
        public Rigidbody mastRb;
        public ParticleSystem propellarEffect;
        public AudioSource fallingAudio;
        [HideInInspector] public Vector3 playerPos;
        [HideInInspector] public float crashSpeed = 0.1f;
        public override void ExecuteState()
        {
            switch(state) {
                case CreatureState.START:
                    SetState(CreatureState.IDLE);
                    break;
                case CreatureState.TARGET:
                    SetState(CreatureState.FLY);
                    break;
                case CreatureState.IDLE:
                    break;
                case CreatureState.IDLING:
                    break;
                case CreatureState.FLY:
                    NextWaypoint();
                    break;
                case CreatureState.FLYING:
                    transform.position = Vector3.MoveTowards(transform.position, currentTargetPosition, Time.deltaTime * 5f);
                    Vector3 direction = (currentTargetPosition - transform.position).normalized;
                    direction.y = 0;
                    if (direction != Vector3.zero) {
                        Quaternion targetRotation0 = Quaternion.LookRotation(direction);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation0, Time.deltaTime * 1f);
                    }
                    if (Vector3.Distance(transform.position, currentTargetPosition) < 0.1f)
                    {
                        SetState(CreatureState.ATTACK);
                    }
                    break;
                case CreatureState.ATTACK:
                    cannonsnout.Attack();
                    playerPos = Camera.main.transform.position;
                    break;
                case CreatureState.ATTACKING:
                    Vector3 directionToPlayer = (playerPos - transform.position).normalized;
                    directionToPlayer.y = 0;
                    Vector3 rotatedDirection = Quaternion.Euler(0, -90, 0) * directionToPlayer;
                    Quaternion targetRotation1 = Quaternion.LookRotation(rotatedDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation1, Time.deltaTime * 1.5f);
                    if (cannonsnout.state == CreatureState.IDLE || cannonsnout.state == CreatureState.IDLING) {
                        SetState(CreatureState.FLY);
                    }
                    break;
                case CreatureState.HIT:
                    hp--;
                    healthMarkers[hp]?.Play();
                    hitParticle.Play();
                    cannonsnout.SetState(CreatureState.IDLE);
                    GetComponent<AudioSource>().Play();
                    transform.StartJiggle(this, 2f, 3f, 0.5f, 5f);
                    bobAndWobble.wobbleAngle += 3f;
                    bobAndWobble.wobbleSpeed += 1f;
                    if (hp <= 0) {
                        SetState(CreatureState.DIE);
                    } else {
                        this.Wait(3f, () => {
                            SetState(CreatureState.FLY);
                        });
                    }
                    break;
                case CreatureState.DIE:

                    //we make sure to prioritize ship so it isn't culled by OptimizeAF
                    gameObject.PrioritizeAsset();

                    animator.enabled = false;
                    bobAndWobble.enabled = false;
                    cannonsnout.Crash();
                    fallingAudio.Play();
                    break;
                case CreatureState.DYING:
                    crashSpeed += 0.1f;
                    crashSpeed = Mathf.Min(crashSpeed, 10f);
                    transform.position = Vector3.MoveTowards(transform.position, CrashPoint.position, Time.deltaTime * crashSpeed);
                    Vector3 directionToCrashPoint = (CrashPoint.position - transform.position).normalized;
                    Quaternion targetRotation2 = Quaternion.LookRotation(directionToCrashPoint);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation2, Time.deltaTime * 0.8f);
                    if (Vector3.Distance(transform.position, CrashPoint.position) < 0.1f) {
                        SetState(CreatureState.CRASH);
                    }
                    break;
                case CreatureState.CRASH:
                    transform.StartJiggle(this, 2f, 3f, 0.5f, 5f);
                    groundPiece.SetActive(true);
                    groundParticle.Play();
                    mastRb.isKinematic = false;
                    mastRb.useGravity = true;
                    propellarEffect.Stop();
                    SunManager.Instance?.OnEvent("cannonsnout-defeated");
                    break;
                case CreatureState.CRASHING:
                    break;
                default:
                    base.ExecuteState();
                    break;
            }
            float currentSpeed = Vector3.Distance(transform.position, currentTargetPosition) / Time.deltaTime;
            animator.speed = Mathf.Clamp(currentSpeed / 5f, 0.5f, 2f);
        }
        public void Animator_WingFlap() {
            wingFlapAudio.Play();
        }
        public void StartBattle() {
            SetState(CreatureState.TARGET);
        }
        public void ProjectileHit() {
            if (state == CreatureState.IDLE || state == CreatureState.IDLING) return;
            if (state != CreatureState.ATTACK && state != CreatureState.ATTACKING) return;
            SetState(CreatureState.HIT);
        }
        public void Crash() {
            SetState(CreatureState.DIE);
        }
    }
}
