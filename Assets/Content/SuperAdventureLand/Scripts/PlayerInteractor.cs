namespace SuperAdventureLand
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public class PlayerInteractor : MonoBehaviour
    {
        public GameObject hitParticlePrefab;
        public bool resetPositionOnEnable = true;
        public bool unparentOnStart = false;
        private Vector3 lastPosition;
        private Rigidbody rb;
        void OnCollisionEnter(Collision collision)
        {
            if (hitParticlePrefab)
                Instantiate(hitParticlePrefab, transform.position, Quaternion.identity);
        }

        //track the position of this object over time using an array of samples and use that to determine the direction of movement and velocity
        private List<Vector3> positionSamples = new List<Vector3>();

        void Awake () {
            rb = GetComponent<Rigidbody>();
        }

        void Start () {
            lastPosition = transform.position;
            rb = GetComponent<Rigidbody>();
            if (unparentOnStart && TryGetComponent(out FixedJoint fj) && fj.connectedBody != null && fj.connectedBody.transform.parent != null) {
                transform.parent = fj.connectedBody.transform.parent;
            }
        }

        void FixedUpdate () {
            positionSamples.Add(transform.position-lastPosition);
            lastPosition = transform.position;
            if (positionSamples.Count > 30) {
                positionSamples.RemoveAt(0);
            }
        }

        public Vector3 GetDirection () {
            if (positionSamples.Count > 0) {
                return positionSamples.Aggregate((a, b) => a + b).normalized;
            }
            return Vector3.zero;
        }

        public float GetVelocity (float multiplier = 1f) {
            float totalMagnitude = positionSamples.Sum(sample => sample.magnitude);
            // Calculate total time covered by the samples
            float totalTime = positionSamples.Count * Time.fixedDeltaTime;
            // Return velocity as displacement divided by time
            return multiplier * (totalMagnitude / totalTime);
        }

        public void OnDrawGizmos () {
            if (positionSamples != null && positionSamples.Count > 0) {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position,GetDirection());
            }
        }

        public IEnumerator ResetPosition () {
            if (rb == null) {
                yield break;
            }
            rb.isKinematic = true;
            yield return new WaitForEndOfFrame();
            if (TryGetComponent(out FixedJoint fj) && transform.parent != null) {
                var connectedBody = fj.connectedBody;
                Destroy(fj);
                transform.position = connectedBody.transform.position;
                transform.rotation = connectedBody.transform.rotation;
                gameObject.AddComponent<FixedJoint>().connectedBody = connectedBody;
            }
            positionSamples.Clear();
            yield return new WaitForEndOfFrame();
            rb.isKinematic = false;
        }

        void OnEnable () {
            if (resetPositionOnEnable) {
                Debug.Log("Resetting position");
                StartCoroutine(ResetPosition());
            }
        }
    }
}
