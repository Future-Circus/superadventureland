namespace SuperAdventureLand
{
    using UnityEngine;
    using UnityEngine.AI;
    using System.Collections;

    #if UNITY_EDITOR
    using UnityEditor;
    using UnityEditorInternal;

    [CustomEditor(typeof(PathCreature), true)]
    public class PathCreatureEditor : AnimatedCreatureEditor
    {
        public override void OnEnable() {
            showWaypoints = true;
            base.OnEnable();
        }
    }
    #endif
    public class PathCreature : AnimatedCreature
    {
        public float moveSpeed = 0.4f;
        [HideInInspector] public float stopDistance = 0.01f;
        public bool turnInPlace = false;
        [ShowIf("turnInPlace")]
        public float turnSpeed = 1f;
        [ShowIf("turnInPlace")]
        public float turnThreshold = 10f;
        public override void Awake () {
            base.Awake();
            agent = GetComponent<NavMeshAgent>();
            if(agent != null)
            {
                agent.speed = moveSpeed;
            }
        }
        public override void Start() {
            base.Start();
            if (agent != null) {
                StartCoroutine(WakeUp());
            } else {
                NextWaypoint();
            }
        }
        //Dream Park specific - this is to avoid the agent position being set before level is oriented
        IEnumerator WakeUp () {
            agent.enabled = false;
            yield return new WaitForSeconds(0.1f);
            if (waypoints != null && waypoints.Length > 0)
            {
                agent.enabled = true;
            }

    
            yield return new WaitUntil(()=> agent.isOnNavMesh);

            if(waypoints.Length > 0)
            {
                NextWaypoint();
            }
        }

        public override void ToggleRagdoll(bool enable) {
            if (agent != null) {
                agent.enabled = !enable;
            }
            base.ToggleRagdoll(enable);
        }
        public override void ExecuteState()
        {
             switch (state) {
                case CreatureState.IDLE:
                    if (agent != null && agent.isOnNavMesh) {
                        agent.isStopped = true;
                    }
                    break;
                case CreatureState.IDLING:
                    if (agent != null && agent.isOnNavMesh) {
                        agent.isStopped = true;
                    }
                    if (StateCondition()) {
                        SetState(CreatureState.MOVE);
                    }
                    break;
                case CreatureState.MOVE:
                    if (agent != null) {
                        agent.isStopped = false;
                    }
                    break;
                case CreatureState.MOVING:

                    if (agent == null) {
                        transform.localPosition = Vector3.MoveTowards(transform.localPosition, currentTargetPositionLocal, moveSpeed * Time.deltaTime);
                        Vector3 flatDirection = Vector3.ProjectOnPlane(waypoints[0] - transform.localPosition, Vector3.up);
                        if (flatDirection.sqrMagnitude > 0.001f)
                        {
                            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.LookRotation(flatDirection, Vector3.up), turnSpeed * Time.deltaTime);
                        }
                    }

                    if (StateCondition()) {
                        if(waypoints.Length <= 1)
                        {
                            SetState(CreatureState.IDLE);
                        }
                        else
                        {
                            NextWaypoint();
                            if (turnInPlace) {
                                SetState(CreatureState.TURN);
                            } else {
                                SetState(CreatureState.MOVE);
                            }
                        }
                    }
                    break;
                case CreatureState.TURN:
                    if (agentEnabled) {
                        agent.isStopped = true; // stop moving while turning
                    }
                    break;
                case CreatureState.TURNING:
                    Vector3 direction = currentTargetPosition - transform.position;
                    Quaternion targetRotation = Quaternion.LookRotation(direction);

                    if (Quaternion.Angle(transform.rotation, targetRotation) > turnThreshold)
                    {
                        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
                    } else {
                        if (StateCondition()) {
                            SetState(CreatureState.MOVE);
                        } else {
                            SetState(CreatureState.IDLE);
                        }
                    }
                    break;
                case CreatureState.DIE:
                    ToggleRagdoll(true);
                    if (agent != null) {
                        agent.enabled = false;
                    }
                    break;
                case CreatureState.DYING:
                    break;
                default:
                    base.ExecuteState();
                    break;
            }
        }

        [HideInInspector] public bool navigationReady {
            get {
                return agent == null || (agent.enabled && agent.isOnNavMesh);
            }
        }

        [HideInInspector] public bool agentEnabled {
            get {

                return agent != null && agent.enabled && agent.IsOnNavMeshWhileDisabled();
            }
        }

        public override bool StateCondition () {
            switch (state) {
                case CreatureState.IDLING:
                    return hasWaypoints && navigationReady;
                case CreatureState.MOVING:
                    if (agent != null) {
                        return agent.remainingDistance < 0.05f;
                    } else {
                        return transform.position.Distance(currentTargetPosition) < stopDistance || transform.localPosition.Distance(currentTargetPositionLocal) < stopDistance;
                    }
                case CreatureState.TURNING:
                    return navigationReady;
                default:
                    return true;
            }
        }
    }
}
