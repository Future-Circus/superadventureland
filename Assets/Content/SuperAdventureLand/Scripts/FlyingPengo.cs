namespace SuperAdventureLand
{
    using UnityEngine;
    using UnityEngine.AI;

    public class FlyingPengo : PengoBehaviour
    {
        public Animator propellarAnimator;
        [Header("Flying Settings")]
        public float dp_bobHeight = 0.3f;
        public float dp_bobSpeed = 1.5f;
        public float dp_hoverOffset = 0f;
        public float propellarMoveSpeed = 2f;
        private float offset = 0f;
        public override void ExecuteState()
        {
            switch (state) {
                case CreatureState.START:
                    if (agent != null) {
                        agent.updateUpAxis = false;
                        agent.updatePosition = false;
                        agent.updateRotation = false;
                    }
                    base.ExecuteState();
                    break;
                case CreatureState.IDLE:
                    base.ExecuteState();
                    rb.useGravity = false;
                    offset = Random.Range(0f, 1f);
                    break;
                case CreatureState.IDLING:
                    Bob();
                    base.ExecuteState();
                    break;
                case CreatureState.TARGETING:
                    Bob();
                    base.ExecuteState();
                    break;
                case CreatureState.MOVING:
                    base.ExecuteState();
                    Bob();
                    break;
                default:
                    base.ExecuteState();
                    break;
            }
        }

        public void Bob() {
            float targetY = dp_hoverOffset + Mathf.Sin(offset + Time.time * dp_bobSpeed) * dp_bobHeight;
            if (transform.parent != null) {
                targetY += transform.parent.InverseTransformPoint(ogPosition.position).y;
            }
            float smoothedY = Mathf.Lerp(transform.localPosition.y, targetY, Time.deltaTime * dp_bobSpeed);
            float velocity = Mathf.Abs(transform.localPosition.y - targetY);

            Vector3 newPosition = transform.localPosition;
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) {
                newPosition = agent.nextPosition;
                if (transform.parent != null) {
                    newPosition = transform.parent.InverseTransformPoint(newPosition);
                }
            }
            newPosition.y = smoothedY;
            transform.localPosition = newPosition;
            propellarAnimator.speed = propellarMoveSpeed/2+velocity*propellarMoveSpeed/2;
        }
        void TrySnapToNavMeshWithFloatOffset()
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 100f, NavMesh.AllAreas))
            {
                Vector3 newPos = hit.position + Vector3.up * ogPosition.position.y;
                transform.position = newPos;

                // Optional: if using NavMeshAgent, sync the position
                NavMeshAgent agent = GetComponent<NavMeshAgent>();
                if (agent)
                {
                    agent.Warp(newPos); // avoids out-of-sync issues
                    agent.baseOffset = ogPosition.position.y;
                }
            }
            else
            {
                if (debugger) {
                    Debug.LogWarning("No NavMesh found below or nearby this object!");
                }
            }
        }
    }
}
