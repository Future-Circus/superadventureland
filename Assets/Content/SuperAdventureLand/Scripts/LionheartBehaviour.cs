namespace SuperAdventureLand
{
    using UnityEngine;
    using System.Collections;

#if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(LionheartBehaviour))]
    public class LionheartBehaviourEditor : CreatureEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            LionheartBehaviour LionheartBehaviour = (LionheartBehaviour)target;
            if (GUILayout.Button("Aim At Player"))
            {
                LionheartBehaviour.SetState(CreatureState.TARGET);
            }
            if (GUILayout.Button("Attack"))
            {
                LionheartBehaviour.SetState(CreatureState.ATTACK);
                Extensions.Wait(LionheartBehaviour, 2.5f, () =>
                {
                    LionheartBehaviour.SetState(CreatureState.IDLE);
                });
            }
        }
    }
#endif

    public class LionheartBehaviour : Creature
    {
        public float detectionRadius = 4f;
        public float detectionAngle = 120f;
        public float detectionTime = 2f;
        public float runSpeed = 10f;
        public float forwardDistance = 1f;
        public float jumpDelay = 3f;
        public float jumpDuration = 1f;
        public float jumpDistance = 3f;
        public float dizzyTime = 2.5f;
        public float crashDelay = 0.5f;
        public MusicArea battleArena;
        private Vector3 runTarget;
        private int pounceCount = 0;
        private Coroutine jumpCoroutine;
        private Coroutine crashCoroutine;
        private float lastInSightTime = 0f;
        private bool playerInSight = false;
        private CreatureState lastState;
        public override void Start()
        {
            base.Start();
            battleArena.onEnter.AddListener(() =>
            {
                animator.SetBool("playerInArena", true);
            });
            battleArena.onExit.AddListener(() =>
            {
                animator.SetBool("playerInArena", false);
            });
        }
        public override void ExecuteState()
        {
            if (lastState != state)
            {
                lastState = state;
                Debug.Log("Current State: " + state);
            }

            switch (state)
            {
                case CreatureState.START:
                    animator.ResetTrigger("pounce");
                    SetState(CreatureState.IDLE);
                    break;
                case CreatureState.IDLE:
                    if (crashCoroutine != null)
                    {
                        StopCoroutine(crashCoroutine);
                        crashCoroutine = null;
                    }
                    animator.ResetTrigger("pounce");
                    animator.SetBool("isRunning", false);
                    animator.SetBool("isDizzy", false);
                    playerInSight = false;
                    break;
                case CreatureState.IDLING:
                    if (InSight(transform, Camera.main.transform))
                    {
                        if (!playerInSight)
                        {
                            lastInSightTime = Time.time;
                            playerInSight = true;
                        }
                        else if (Time.time - lastInSightTime > detectionTime)
                        {
                            playerInSight = false;
                            SetState(CreatureState.TARGET);
                        }
                    }
                    else
                    {
                        playerInSight = false;
                    }
                    break;
                case CreatureState.TARGET:
                    animator.SetTrigger("pounce");
                    LookAtPlayer();
                    runTarget = Camera.main.transform.position + Camera.main.transform.forward * forwardDistance; // Run to right in front of player
                    runTarget.y = 0;
                    pounceCount++;
                    Extensions.Wait(this, 2.5f, () =>
                    {
                        if (pounceCount % 2 == 0)
                        {
                            SetState(CreatureState.RUN);
                        }
                        else
                        {
                            SetState(CreatureState.LAUNCH);
                        }
                    });
                    break;
                case CreatureState.RUN:
                    animator.SetBool("isRunning", true);
                    break;
                case CreatureState.RUNNING:
                    transform.position = Vector3.MoveTowards(transform.position, runTarget, runSpeed * Time.deltaTime);
                    if (Vector3.Distance(transform.position, runTarget) < 0.5f)
                    {
                        SetState(CreatureState.IDLE);
                    }
                    break;
                case CreatureState.ATTACK:
                    SetState(CreatureState.IDLE); // TODO Change to attack logic
                    break;
                case CreatureState.HIT:
                    Extensions.Wait(this, 2f, () =>
                    {
                        SetState(CreatureState.IDLE); // TODO Change to hit logic
                    });
                    break;
                case CreatureState.LAUNCH:
                    animator.SetTrigger("jump");
                    jumpCoroutine = StartCoroutine(JumpCoroutine());
                    break;
                case CreatureState.CRASH:
                    animator.SetTrigger("crash");
                    if (jumpCoroutine != null)
                    {
                        StopCoroutine(jumpCoroutine);
                        jumpCoroutine = null;
                    }
                    crashCoroutine = StartCoroutine(CrashCoroutine());
                    break;
                default:
                    base.ExecuteState();
                    break;
            }
        }

        private void LookAtPlayer()
        {
            transform.LookAt(Camera.main.transform.position);
            transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);
        }

        private IEnumerator JumpCoroutine()
        {
            Vector3 startPos = transform.position;
            float dist = Mathf.Min(jumpDistance, Vector3.Distance(transform.position, Camera.main.transform.position));
            Vector3 endPos = transform.position + transform.forward * dist;
            endPos.y = 0;

            yield return new WaitForSeconds(jumpDelay);
            float t = 0f;
            float duration = jumpDuration;
            while (t < duration)
            {
                if (isCrashing) yield break;

                t += Time.deltaTime;
                float normalized = Mathf.Clamp01(t / duration);

                // Quadratic ease-out
                float quadT = 1f - (1f - normalized) * (1f - normalized);

                // Horizontal lerp
                Vector3 pos = Vector3.Lerp(startPos, endPos, quadT);

                transform.position = pos;
                yield return null;
            }

            transform.position = endPos;
            SetState(CreatureState.IDLE);
        }

        private IEnumerator CrashCoroutine()
        {
            yield return new WaitForSeconds(crashDelay);
            animator.SetBool("isDizzy", true);
            yield return new WaitForSeconds(dizzyTime);
            animator.SetBool("isDizzy", false);
            SetState(CreatureState.IDLE);
        }

        private bool InSight(Transform origin, Transform target)
        {
            Vector3 toTarget = target.position - origin.position;
            if (toTarget.sqrMagnitude > detectionRadius * detectionRadius)
                return false;

            // angle check
            float angle = Vector3.Angle(origin.forward, toTarget);
            return angle < detectionAngle * 0.5f;
        }

        public void PlayerHit(CollisionWrapper collision)
        {
            if (isCrashing)
            {
                SetState(CreatureState.HIT);
            }
            else if (isRunning)
            {
                SetState(CreatureState.ATTACK);
            }
        }

        public void EntityHit(CollisionWrapper collision)
        {
            if (isRunning || isJumping)
            {
                SetState(CreatureState.CRASH);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(runTarget, 1f);
        }

        [HideInInspector]
        public bool isIdling
        {
            get
            {
                return state == CreatureState.IDLE || state == CreatureState.IDLING;
            }
        }

        [HideInInspector]
        public bool isRunning
        {
            get
            {
                return state == CreatureState.RUN || state == CreatureState.RUNNING;
            }
        }

        [HideInInspector]
        public bool isJumping
        {
            get
            {
                return state == CreatureState.LAUNCH || state == CreatureState.LAUNCHING;
            }
        }

        [HideInInspector]
        public bool isCrashing
        {
            get
            {
                return state == CreatureState.CRASH || state == CreatureState.CRASHING;
            }
        }
    }

}
