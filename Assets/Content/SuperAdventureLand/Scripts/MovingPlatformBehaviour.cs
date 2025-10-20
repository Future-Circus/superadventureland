namespace SuperAdventureLand
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public class MovingPlatformBehaviour : AwareCreature
    {
        public MovingPlatformBehaviour[] connectedPlatforms;
        [Range(0.00f,10.00f)]
        public float waitTime = 1f;
        public override void ExecuteState() {
            switch (state) {
                case CreatureState.TURN:
                    break;
                case CreatureState.TURNING:
                    if (timeSinceStateChange >= waitTime) {
                        if (StateCondition()) {
                            SetState(CreatureState.MOVE);
                        } else {
                            SetState(CreatureState.IDLE);
                        }
                    }
                    break;
                case CreatureState.TARGET:
                    transform.StartJiggle(this, 1f, 3f, 0.1f);
                    break;
                case CreatureState.TARGETING:
                    if (!StateCondition()) {
                        SetState(CreatureState.TURN);
                    }
                    break;
                default:
                    base.ExecuteState();
                    break;
            }
        }
        public override void SetState(CreatureState newState) {
            if (queue.Count == 0 || queue.Last() != newState) {
                queue.Add(newState);
                foreach (AwareCreature entity in connectedPlatforms) {
                    entity.queue.Add(newState);
                }
            }
        }
    }
}
