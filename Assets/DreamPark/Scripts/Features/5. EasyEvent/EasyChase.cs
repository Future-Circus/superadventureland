namespace DreamPark.Easy
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.AI;

    public class EasyChase : EasyEvent
    {
        public NavMeshAgent agent;
        public Transform target;
        public float speed = 10f;
        public float stoppingDistance = 0.05f;
        public float turnSpeed = 1f;
        public bool waitUntilDestinationReached = true;
        private Rigidbody rb;
        #if UNITY_EDITOR
        [Space(10)]
        [ReadOnly] public bool agentEnabled = false;
        [ReadOnly] public bool agentOnNavMesh = false;
        [ReadOnly] public bool agentIsOnNavMesh = false;
        #endif
        public override void Awake () {
            base.Awake();
            agent = GetComponent<NavMeshAgent>();
            if(agent != null)
            {
                agent.speed = speed;
            }
            rb = GetComponent<Rigidbody>();
        }
        public override void Start() {
            base.Start();
            if (!waitUntilDestinationReached) {
                onEvent?.Invoke(null);
            }
        }

        IEnumerator WakeUp () {
            agent.enabled = false;
            yield return new WaitForSeconds(0.1f);
            agent.enabled = true;
        }

        IEnumerator GetUpRoutine()
        {
            agent.enabled = false;

            Quaternion startRot = transform.rotation;

            Vector3 direction = (new Vector3(target.position.x, 0, target.position.z) - new Vector3(transform.position.x, 0, transform.position.z)).normalized;
            Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
            Vector3 startPos = transform.position;
            Vector3 apex = startPos + Vector3.up * 1f; // height of the hop

            float duration = 0.6f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0, 1, t);

                // Arc motion
                transform.position = Vector3.Lerp(
                    Vector3.Lerp(startPos, apex, t < 0.5f ? t * 2f : (1f - t) * 2f),
                    startPos,
                    t
                );

                // Smooth upright rotation
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

                yield return null;
            }
            agent.enabled = true;
        }

        IEnumerator FallToNavMesh()
        {
            agent.enabled = false;
            Quaternion startRot = transform.rotation;
            Vector3 direction = (new Vector3(target.position.x, 0, target.position.z) - new Vector3(transform.position.x, 0, transform.position.z)).normalized;
            Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                Vector3 target = hit.position;
                float fallTime = 0.4f;
                Vector3 start = transform.position;
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime / fallTime;
                    float y = Mathf.Sin(t * Mathf.PI) * 1f; // nice arc
                    transform.position = Vector3.Lerp(start, target, t) + Vector3.up * y;
                    transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                    yield return null;
                }
            }
            agent.enabled = true;
        }

        public override void OnEvent(object arg0 = null) {
            Debug.Log("[EasyChase] OnEvent called");
            isEnabled = true;
            if (rb != null) {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
                rb.WakeUp();
            }
            if (arg0 is GameObject gameObject) {
                target = gameObject.transform;
            }
            if (target == null) {
                target = Camera.main.transform;
            }
            if (agent != null) {
                 if (!agent.IsOnNavMesh(out NavMeshHit _)) {
                    StartCoroutine(FallToNavMesh());
                } else {
                    StartCoroutine(GetUpRoutine());
                }
            }
        }

        void Update() {
            #if UNITY_EDITOR
                agentEnabled = agent != null && agent.enabled;
                agentOnNavMesh = agent != null && agent.isOnNavMesh;
                agentIsOnNavMesh = agent != null && agent.IsOnNavMesh(out NavMeshHit _);
            #endif

            if (!isEnabled) {
                return;
            }
            Vector3 targetPosition = target.position;

            if (agent == null) {
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPosition, speed * Time.deltaTime);
                Vector3 flatDirection = Vector3.ProjectOnPlane(targetPosition - transform.localPosition, Vector3.up);
                if (flatDirection.sqrMagnitude > 0.001f)
                {
                    transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.LookRotation(flatDirection, Vector3.up), turnSpeed * Time.deltaTime);
                }
            } else if (agent != null &&agent.enabled && agent.isOnNavMesh && agent.IsOnNavMesh(out NavMeshHit _)) {
                //Debug.Log("[EasyChase] Setting destination to " + targetPosition);
                agent.SetDestination(targetPosition);
                return;
            }

            // catch!
            if ((agent != null && agent.isOnNavMesh && agent.IsOnNavMesh(out NavMeshHit _) && agent.remainingDistance < stoppingDistance) || (agent == null && transform.position.Distance(targetPosition) < stoppingDistance)) {
                //Debug.Log("[EasyChase] Stopping distance reached");
                isEnabled = false;
                if (waitUntilDestinationReached) {
                    onEvent?.Invoke(null);
                }
            }
        }
    }

}
