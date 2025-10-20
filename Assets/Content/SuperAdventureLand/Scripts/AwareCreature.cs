namespace SuperAdventureLand
{
    using UnityEngine;

    #if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(AwareCreature), true)]
    public class AwareCreatureEditor : PathCreatureEditor
    {
        public override void OnEnable() {
            showAwareFeature = true;
            base.OnEnable();
        }
    }
    #endif
    public class AwareCreature : PathCreature
    {
        public override void ExecuteState()
        {
            switch (state)
            {
                case CreatureState.TARGET:
                    if (agent != null && agent.isOnNavMesh) {
                        agent.isStopped = true;
                    }
                    break;
                case CreatureState.TARGETING:
                    if (StateCondition()) {
                        Vector3 currentPosition = transform.position;
                        currentTargetPosition = new Vector3(Camera.main.transform.position.x, currentPosition.y, Camera.main.transform.position.z);
                        Vector3 direction = (currentTargetPosition - currentPosition).normalized;
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
                    } else {
                        SetState(CreatureState.IDLE);
                    }
                    break;
                case CreatureState.IDLING:
                    base.ExecuteState();
                    if (TargetCondition(CreatureState.IDLING)) {
                        SetState(CreatureState.TARGET);
                    }
                    break;
                case CreatureState.MOVING:
                    base.ExecuteState();
                    if (TargetCondition(CreatureState.MOVING)) {
                        SetState(CreatureState.TARGET);
                    }
                    break;
                default:
                    base.ExecuteState();
                    break;
            }
        }
        [HideInInspector] public virtual bool TargetCondition(CreatureState _state) {
            if (Camera.main == null) {
                return false;
            }
            var p1 = new Vector3(Camera.main.transform.position.x,0,Camera.main.transform.position.z);
            var p2 = new Vector3(transform.position.x,0,transform.position.z);
            return targetPlayer && state == _state && p1.Distance(p2) < targetRange;
        }
        public override bool StateCondition()
        {
            switch (state)
            {
                case CreatureState.IDLING:
                    return base.StateCondition() && playerInRange;
                case CreatureState.TURNING:
                    return base.StateCondition() && playerInRange;
                case CreatureState.TARGETING:
                    return TargetCondition(state);
                default:
                    return base.StateCondition();
            }
        }
    }
}
