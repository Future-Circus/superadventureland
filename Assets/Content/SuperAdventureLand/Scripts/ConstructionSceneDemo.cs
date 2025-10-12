namespace SuperAdventureLand.Scripts
{
    using System.Collections;
    using UnityEngine;

    public class ConstructionSceneDemo : MonoBehaviour
    {
        public ParticleSystem hammerParticle;
        public MeshRenderer blockRenderer;

        public void StartHammering()
        {
            hammerParticle.Play();
            StartCoroutine(JiggleCoroutine());
        }

         private IEnumerator JiggleCoroutine()
        {
            // All jiggle parameters defined locally:
            float duration  = 0.6f;   // total jiggle time
            float frequency = 3f;     // how many "jiggles" per second
            float amplitude = 0.25f;  // size of the jiggle
            float decay     = 5f;     // how fast jiggle fades back to normal

            // Record the original scale
            Vector3 originalScale = blockRenderer.transform.localScale;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Exponential decay to reduce amplitude over time
                float damper = Mathf.Exp(-decay * t);

                // Apply a sine wave for the jiggle, multiplied by the decaying factor
                float scaleFactor = 1f + amplitude * damper * Mathf.Sin(2f * Mathf.PI * frequency * t);

                // Update the object's scale
                blockRenderer.transform.localScale = originalScale * scaleFactor;

                yield return null;
            }

            // Ensure final scale is the original
            blockRenderer.transform.localScale = originalScale;
        }
    }
}
