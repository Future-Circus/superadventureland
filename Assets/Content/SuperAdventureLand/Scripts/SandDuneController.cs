namespace SuperAdventureLand
{
    using System;
    using UnityEngine;

    public class SandDuneController : MonoBehaviour
    {
        public AudioSource audioSource;
        public Transform upperDune;
        public Transform lowerDune;
        public Transform spread;
        public EasyEvent onSandDuneHalf;
        public EasyEvent onSandDuneFull;

        private Vector3 upperDuneOgPosition;
        private Vector3 lowerDuneOgScale;
        private float sandDuneHeight = 1f;
        private Collider triggerCollider;

        public void Start()
        {
            triggerCollider = GetComponent<Collider>();
            upperDuneOgPosition = upperDune.position;
            lowerDuneOgScale = lowerDune.localScale;
        }

        public void OnTriggerStay(Collider other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Player") || other.gameObject.tag == "ActiveHit")
            {
                // Get the trigger bounds (this collider’s bounds)
                Bounds triggerBounds = triggerCollider.bounds;

                // Get player’s vertical position
                float playerY = other.bounds.center.y;

                // Compute top and bottom Y of the trigger
                float topY = triggerBounds.max.y;
                float bottomY = triggerBounds.center.y;

                // Normalize the player's Y within the trigger bounds
                float normalizedHeight = Mathf.InverseLerp(bottomY, topY, playerY);
                if (normalizedHeight < sandDuneHeight) {
                    sandDuneHeight = normalizedHeight;
                }

                Debug.Log($"Player vertical position in trigger: {sandDuneHeight:F2}");
            }
        }

        public void Update()
        {
            float totalChange = 0;
            float newSpread = 0;
            float oldPosition = upperDune.position.y;
            var newPosition = new Vector3(upperDuneOgPosition.x, upperDuneOgPosition.y - (1f-sandDuneHeight)*triggerCollider.bounds.size.y/2f, upperDuneOgPosition.z);
            upperDune.position = Vector3.Lerp(upperDune.position, newPosition, Time.deltaTime * 10f);
            totalChange += Math.Abs(newPosition.y - oldPosition);
            if (sandDuneHeight < 0.5f) {
                var beforeScale = lowerDune.localScale;
                var newScale = Vector3.Lerp(lowerDune.localScale, new Vector3(lowerDuneOgScale.x, (sandDuneHeight + 0.5f) * lowerDuneOgScale.y, lowerDuneOgScale.z), Time.deltaTime * 10f);
                lowerDune.localScale = newScale;
                newSpread = Math.Abs(newScale.y - beforeScale.y);
                spread.localScale += new Vector3(1f,1f,0f)*newSpread/2f;
                spread.localScale = Vector3.Min(spread.localScale, Vector3.one*1.5f);
                totalChange += Math.Abs(newScale.y - beforeScale.y);
                if (sandDuneHeight < 0.5f) {
                    onSandDuneHalf?.OnEvent();
                    onSandDuneHalf = null;
                }
            }
            if (sandDuneHeight < 0.1f) {
                onSandDuneFull?.OnEvent();
                onSandDuneFull = null;
                gameObject.layer = LayerMask.NameToLayer("Default");
            }

             if (!audioSource.isPlaying) {
                audioSource.Play();
            }
            audioSource.volume = Mathf.Clamp01(totalChange*4f);
        }
    }

}
