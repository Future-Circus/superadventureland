namespace SuperAdventureLand
{
    using System.Collections;
    using UnityEngine;

    public class RollingBallBehaviour : MonoBehaviour
    {
        private Vector3 ogScale;
        public ParticleSystem hitEffect;
        public ParticleSystem buildEffect;
        public UnityEngine.Events.UnityEvent onDestroy;

        private Rigidbody rb;

        void OnCollisionEnter(Collision collision)
        {
            // Check if colliding with Player or Entity layer
            if (collision.gameObject.layer == LayerMask.NameToLayer("Player") ||
                collision.gameObject.layer == LayerMask.NameToLayer("Entity"))
            {
                Break();
            }
        }

        void Break()
        {
                if (hitEffect != null)
                {
                    // Detach particle system before destroying the object
                    hitEffect.transform.SetParent(null,true);
                    hitEffect.Play();
                }

                // Destroy this game object
                onDestroy?.Invoke();
                Destroy(gameObject);
        }

        void Start()
        {
            rb = GetComponent<Rigidbody>();

            ogScale = transform.localScale;
            // Set initial scale to zero
            transform.localScale = Vector3.zero;

            // Initially disable physics until scale up completes
            if (TryGetComponent<Rigidbody>(out var rigidBody)) {
                rigidBody.isKinematic = true;
            }
            if (TryGetComponent<Collider>(out var collider)) {
                collider.enabled = false;
            }

            // Start scale up coroutine
            StartCoroutine(ScaleUp());

             Invoke("Break", 10f);
        }

        private IEnumerator ScaleUp() {
            float elapsedTime = 0;
            float duration = 1f;

            if (buildEffect != null) {
                buildEffect.Play();
            }

            while (elapsedTime < duration) {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;

                // Smooth step for easing
                float smoothProgress = progress * progress * (3f - 2f * progress);
                transform.localScale = Vector3.Lerp(Vector3.zero, ogScale, smoothProgress);

                yield return null;
            }

            transform.localScale = ogScale;

            buildEffect.Stop();

            // Enable physics components after scale up
            if (TryGetComponent<Rigidbody>(out var rb)) {
                rb.isKinematic = false;
            }
            if (TryGetComponent<Collider>(out var col)) {
                col.enabled = true;
            }
        }

        void Update()
        {
            if (rb && !rb.isKinematic) {
                rb.AddTorque(transform.right * 0.5f, ForceMode.Impulse);
            }
        }

    }
}
