namespace SuperAdventureLand
{
    using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine.AI;
    using System.Collections;

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
        public float runSpeed = 10f;
        public float forwardDistance = 1f;
        public float jumpDelay = 3f;
        public float jumpDuration = 1f;
        public float jumpDistance = 3f;
        public MusicArea battleArena;
        private Vector3 runTarget;
        private int pounceCount = 0;
        private Coroutine jumpCoroutine;
        public override void Start()
        {
            base.Start();
            // battleArena.onEnter.AddListener(() =>
            // {
            //     Debug.Log("Battle Arena Entered");
            //     if (isIdling)
            //     {
            //         SetState(CreatureState.TARGET);
            //     }
            // });
            // battleArena.onExit.AddListener(() =>
            // {
            //     SetState(CreatureState.IDLE);
            // });
        }
        public override void ExecuteState()
        {
            switch (state)
            {
                case CreatureState.START:
                    SetState(CreatureState.IDLE);
                    break;
                case CreatureState.IDLE:
                    animator.SetBool("isRunning", false);
                    break;
                case CreatureState.IDLING:
                    if(Physics.OverlapSphere(transform.position, detectionRadius, LayerMask.GetMask("Player")).Length > 0)
                    {
                        SetState(CreatureState.TARGET);
                    }
                    break;
                case CreatureState.TARGET:
                    LookAtPlayer();
                    animator.SetTrigger("pounce");
                    pounceCount++;
                    Extensions.Wait(this, 2.5f, () =>
                    {
                        // Shuffle between run and launch
                        // if (pounceCount % 2 == 0)
                        // {
                        //     SetState(CreatureState.RUN);
                        // }
                        // else
                        // {
                            SetState(CreatureState.LAUNCH);
                        // }
                    });
                    break;
                case CreatureState.RUN:
                    animator.SetBool("isRunning", true);
                    runTarget = Camera.main.transform.position + Camera.main.transform.forward * forwardDistance;
                    runTarget.y = 0;
                    LookAtPlayer();
                    break;
                case CreatureState.RUNNING:
                    transform.position = Vector3.MoveTowards(transform.position, runTarget, runSpeed * Time.deltaTime);
                    if(Vector3.Distance(transform.position, runTarget) < 0.1f)
                    {
                        SetState(CreatureState.IDLE);
                    }
                    break;
                case CreatureState.ATTACK:
                    SetState(CreatureState.IDLE); // Change to attack logic
                    break;
                case CreatureState.HIT:
                    SetState(CreatureState.IDLE); // Change to hit logic
                    break;
                case CreatureState.LAUNCH:
                    animator.SetTrigger("jump");
                    jumpCoroutine = StartCoroutine(JumpCoroutine());
                    break;
                case CreatureState.CRASH:
                    animator.SetTrigger("crash");
                    if(jumpCoroutine != null)
                    {
                        StopCoroutine(jumpCoroutine);
                        jumpCoroutine = null;
                    }
                    Extensions.Wait(this, 2.5f, () =>
                    {
                        if (isCrashing)
                        {
                            SetState(CreatureState.IDLE);
                        }
                    });
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
            Vector3 endPos = transform.position + transform.forward * jumpDistance;
            endPos.y = 0;

            yield return new WaitForSeconds(jumpDelay);
            float t = 0f;
            float duration = jumpDuration;
            while (t < duration)
            {
                if(isCrashing) yield break;

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
            SetState(CreatureState.CRASH);
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
        public bool isCrashing
        {
            get
            {
                return state == CreatureState.CRASH || state == CreatureState.CRASHING;
            }
        }
    }

}
