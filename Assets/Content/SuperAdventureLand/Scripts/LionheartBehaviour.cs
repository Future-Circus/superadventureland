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
        public float runSpeed = 10f;
        public float forwardDistance = 1f;
        public float jumpDelay = 3f;
        public float jumpDuration = 1f;
        public float jumpDistance = 3f;
        public MusicArea battleArena;
        private Vector3 runTarget;
        public override void Start()
        {
            base.Start();
            battleArena.onEnter.AddListener(() =>
            {
                Debug.Log("Battle Arena Entered");
                if (isIdling)
                {
                    SetState(CreatureState.TARGET);
                }
            });
            battleArena.onExit.AddListener(() =>
            {
                SetState(CreatureState.IDLE);
            });
        }
        public override void ExecuteState()
        {
            switch (state)
            {
                case CreatureState.START:
                    SetState(CreatureState.IDLE);
                    break;
                case CreatureState.IDLE:
                    break;
                case CreatureState.TARGET:
                    transform.LookAt(Camera.main.transform.position);
                    transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);
                    animator.SetTrigger("pounce");
                    Extensions.Wait(this, 2.5f, () =>
                    {
                        // Shuffle between run and launch
                        // if (Random.value < 0.5f)
                        // {
                        SetState(CreatureState.RUN);
                        // }
                        // else
                        // {
                        // SetState(CreatureState.LAUNCH);
                        // }
                    });
                    break;
                case CreatureState.RUN:
                    animator.SetTrigger("run");
                    runTarget = Camera.main.transform.position + Camera.main.transform.forward * forwardDistance;
                    runTarget.y = 0;
                    break;
                case CreatureState.RUNNING:
                    transform.position = Vector3.MoveTowards(transform.position, runTarget, runSpeed * Time.deltaTime);
                    break;
                case CreatureState.ATTACK:
                    break;
                case CreatureState.LAUNCH:
                    animator.SetTrigger("jump");
                    StartCoroutine(JumpCoroutine());
                    break;
                case CreatureState.CRASH:
                    animator.SetTrigger("crash");
                    Extensions.Wait(this, 3f, () =>
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
        // public override void ExecuteState()
        // {
        //     switch (state)
        //     {
        //         case CreatureState.START:
        //             SetState(CreatureState.IDLE);
        //             break;
        //         case CreatureState.IDLE:
        //             animator.SetBool("isRunning", false);
        //             break;
        //         case CreatureState.TARGET:
        //             transform.LookAt(Camera.main.transform.position);
        //             transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);
        //             Extensions.Wait(this, 1f, () =>
        //             {
        //                 runTarget = Camera.main.transform.position + Camera.main.transform.forward * forwardDistance;
        //                 runTarget.y = 0;
        //                 SetState(CreatureState.RUN);
        //             });
        //             break;
        //         case CreatureState.RUN:
        //             animator.SetBool("isRunning", true);
        //             break;
        //         case CreatureState.RUNNING:
        //             transform.position = Vector3.MoveTowards(transform.position, runTarget, runSpeed * Time.deltaTime);
        //             break;
        //         case CreatureState.ATTACK:
        //             animator.SetTrigger("pounce");
        //             StartCoroutine(PounceCoroutine());
        //             break;
        //         case CreatureState.HIT:
        //             animator.SetTrigger("hit");
        //             Extensions.Wait(this, 1f, () =>
        //             {
        //                 SetState(CreatureState.IDLE);
        //             });
        //             break;
        //         case CreatureState.HITTING:
        //             Vector3 escapeDir = Camera.main.transform.forward;
        //             escapeDir.y = 0;
        //             escapeDir = escapeDir.normalized;
        //             transform.rotation = Quaternion.LookRotation(escapeDir);
        //             transform.position += escapeDir * runSpeed * Time.deltaTime;
        //             break;
        //         case CreatureState.CRASH:
        //             animator.SetTrigger("crash");
        //             Extensions.Wait(this, 3f, () =>
        //             {
        //                 if (isCrashing)
        //                 {
        //                     SetState(CreatureState.IDLE);
        //                 }
        //             });
        //             break;
        //         default:
        //             base.ExecuteState();
        //             break;
        //     }
        // }

        // private IEnumerator PounceCoroutine()
        // {
        //     Vector3 startPos = transform.position;
        //     Vector3 endPos = transform.position + transform.forward * jumpDistance;
        //     endPos.y = 0;
        //     yield return new WaitForSeconds(jumpDelay);
        //     float t = 0f;
        //     float duration = jumpDuration;
        //     while (t < duration)
        //     {
        //         t += Time.deltaTime;
        //         float normalized = Mathf.Clamp01(t / duration);

        //         // Quadratic ease-out
        //         float quadT = 1f - (1f - normalized) * (1f - normalized);

        //         // Horizontal lerp
        //         Vector3 pos = Vector3.Lerp(startPos, endPos, quadT);

        //         transform.position = pos;
        //         yield return null;
        //     }

        //     transform.position = endPos;
        //     SetState(CreatureState.IDLE);
        // }

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
