namespace SuperAdventureLand
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;

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
        public MusicArea battleArena;

        [Header("Player Detection")]
        public float detectionRadius = 4f;
        public float detectionAngle = 120f;
        public float detectionTime = 2f;

        [Header("Head Follow Player")]
        public Transform headBoneTransform;
        public float headRotationMaxAngle = 30f;
        public float headRotationSpeed = 30f;

        [Header("Lionheart Movement")]
        public float forwardDistance = 1f;
        public float turnSpeed = 30f;
        public float runSpeed = 10f;
        public float jumpDelay = 3f;
        public float jumpDuration = 1f;
        public float jumpDistance = 3f;
        public float backflipDuration = 0.5f;
        public float backflipJumpHeight = 3f;
        public float backflipBackDistance = 2f;
        public float dizzyTime = 2.5f;
        public float crashDelay = 0.5f;
        public List<ParticleSystem> dustParticles;
        private Vector3 runTarget;
        private int pounceCount = 0;
        private Coroutine jumpCoroutine;
        private Coroutine crashCoroutine;
        private float lastInSightTime = Mathf.Infinity;
        private bool playerInSight = false;
        private CreatureState lastState;
        private Quaternion targetRotation;
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

                    float dotThreshold = Mathf.Cos(detectionAngle * Mathf.Deg2Rad);
                    if (Vector3.Dot(transform.forward, (Camera.main.transform.position - transform.position).normalized) < 0.5f)
                    {
                        Debug.Log("Force turn: " + Vector3.Dot(transform.forward, (Camera.main.transform.position - transform.position).normalized));
                        SetState(CreatureState.TURN);
                    }
                    break;
                case CreatureState.IDLING:
                    if (InSight(transform, Camera.main.transform))
                    {
                        if (Time.time - lastInSightTime >= detectionTime)
                        {
                            SetState(CreatureState.TARGET);
                        }

                        if (!playerInSight)
                        {
                            lastInSightTime = Time.time;
                            playerInSight = true;
                        }
                    }
                    else
                    {
                        playerInSight = false;
                    }
                    break;
                case CreatureState.TARGET:
                    animator.SetTrigger("pounce");
                    runTarget = Camera.main.transform.position + Camera.main.transform.forward * forwardDistance; // Run to right in front of player
                    runTarget.y = 0;
                    LookAtTarget(runTarget);
                    pounceCount++;
                    SetDustParticles(true);

                    Extensions.Wait(this, 2.5f, () =>
                    {
                        SetDustParticles(false);
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
                case CreatureState.TARGETING:
                    break;
                case CreatureState.RUN:
                    animator.SetBool("isRunning", true);
                    // runTarget = Camera.main.transform.position + Camera.main.transform.forward * forwardDistance; // Run to right in front of player
                    // runTarget.y = 0;
                    // LookAtTarget(runTarget);
                    break;
                case CreatureState.RUNNING:
                    transform.position = Vector3.MoveTowards(transform.position, runTarget, runSpeed * Time.deltaTime);
                    if (Vector3.Distance(transform.position, runTarget) < 0.5f)
                    {
                        SetState(CreatureState.IDLE);
                    }
                    break;
                case CreatureState.LAUNCH:
                    animator.SetTrigger("jump");
                    jumpCoroutine = StartCoroutine(JumpCoroutine());
                    break;
                case CreatureState.TURN:
                    break;
                case CreatureState.TURNING:
                    targetRotation = TurnToTarget(Camera.main.transform.position);
                    if (Quaternion.Angle(transform.rotation, targetRotation) < 1f)
                    {
                        SetState(CreatureState.IDLE);
                    }
                    break;
                case CreatureState.MOVE:
                    break;
                case CreatureState.ATTACK:
                    // TODO Add attack logic
                    SetState(CreatureState.IDLE);
                    break;
                case CreatureState.HIT:
                    StartCoroutine(Backflip(transform, Camera.main.transform, backflipDuration, backflipJumpHeight, backflipBackDistance));
                    Extensions.Wait(this, backflipDuration, () =>
                    {
                        SetState(CreatureState.IDLE);
                    });
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

        new private void LateUpdate()
        {
            Vector3 dir = (Camera.main.transform.position - headBoneTransform.position).normalized;

            // Desired rotation
            Quaternion targetRot = Quaternion.LookRotation(dir);

            // Measure angle between current forward and desired direction
            float angle = Vector3.Angle(headBoneTransform.forward, dir);

            if (angle < headRotationMaxAngle)
            {
                // Within allowed range, rotate normally
                headBoneTransform.rotation = targetRot;
            }
            else
            {
                // Outside limit: clamp direction
                Vector3 clampedDir = Vector3.Slerp(
                    headBoneTransform.forward,
                    dir,
                    headRotationMaxAngle / angle
                );

                Quaternion clampedRot = Quaternion.LookRotation(clampedDir);
                headBoneTransform.rotation = clampedRot;
            }
        }

        private Quaternion TurnToTarget(Vector3 target)
        {
            Quaternion targetRotation = Quaternion.LookRotation(target - transform.position);
            targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            return targetRotation;
        }

        private void LookAtTarget(Vector3 target)
        {
            transform.LookAt(target);
            transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);
        }

        private void SetDustParticles(bool active)
        {
            foreach (ParticleSystem dustParticle in dustParticles)
            {
                dustParticle.gameObject.SetActive(active);
                if (active)
                {
                    dustParticle.Play();
                }
                else
                {
                    dustParticle.Stop();
                }
            }
        }

        IEnumerator Backflip(Transform target, Transform player, float duration, float jumpHeight, float backDistance)
        {
            LookAtTarget(player.position);

            float elapsed = 0f;
            Vector3 startPos = target.position;
            Vector3 startEuler = target.localEulerAngles; // store full rotation
            float startRotX = startEuler.x;
            float endRotX = startRotX + 360f;

            // Direction away from player
            Vector3 awayDir = (target.position - player.position).normalized;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Rotation ease-in-out (smoothstep) around X only
                float rotT = t * t * (3f - 2f * t);
                float angleX = Mathf.Lerp(startRotX, endRotX, rotT);

                // Vertical parabola (0 at start, peak at t=0.5, back to 0 at end)
                float heightOffset = 4f * jumpHeight * t * (1 - t);

                // Backward offset
                float backwardOffset = backDistance * t;

                // Apply rotation (X changes, Y and Z stay same)
                Vector3 euler = new Vector3(angleX, startEuler.y, startEuler.z);
                target.localEulerAngles = euler;

                // Apply position
                Vector3 newPos = startPos + awayDir * backwardOffset + Vector3.up * heightOffset;
                newPos.y = Mathf.Max(newPos.y, 0f); // ensure it doesn’t go below floor

                target.position = newPos;

                yield return null;
            }

            // Snap final rotation and position
            target.localEulerAngles = new Vector3(endRotX % 360f, startEuler.y, startEuler.z);
            target.position = new Vector3(startPos.x + awayDir.x * backDistance,
                                          0f,
                                          startPos.z + awayDir.z * backDistance);
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

            // Vision cone edges
            Vector3 forward = transform.forward;
            Vector3 pos = transform.position;

            // Half the cone angle
            float halfAngle = detectionAngle * 0.5f;

            // Compute directions for boundary rays
            Quaternion leftRot = Quaternion.Euler(0, -halfAngle, 0);
            Quaternion rightRot = Quaternion.Euler(0, halfAngle, 0);

            Vector3 leftDir = leftRot * forward;
            Vector3 rightDir = rightRot * forward;

            Gizmos.color = Color.white;
            Gizmos.DrawLine(pos, pos + leftDir * detectionRadius);
            Gizmos.DrawLine(pos, pos + rightDir * detectionRadius);

            // Optional: Fill the arc (debug cone arc)
            const int arcSegments = 20;
            Gizmos.color = Color.cyan;
            Vector3 prevPoint = pos + leftDir * detectionRadius;

            for (int i = 1; i <= arcSegments; i++)
            {
                float t = (float)i / arcSegments;
                float angleStep = Mathf.Lerp(-halfAngle, halfAngle, t);
                Quaternion rot = Quaternion.Euler(0, angleStep, 0);
                Vector3 point = pos + (rot * forward) * detectionRadius;

                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }

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
