namespace SuperAdventureLand.Scripts
{
    using UnityEngine;

    public class SelfDestruct : MonoBehaviour
    {
        public float time = 1.0f;
        public bool destroyOnDisable = false;

        public ParticleSystem optionalEffect;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // Only set up timed destruction if we're NOT using destroyOnDisable
            if (!destroyOnDisable) {
                Destroy(gameObject, time);
            }
        }

        void OnDisable() {
            if (destroyOnDisable) {
                Destroy(gameObject);
            }
        }

        void OnDestroy() {
            if (optionalEffect != null) {
                optionalEffect.transform.SetParent(null,true);
                optionalEffect.Play();
            }
        }
    }
}
