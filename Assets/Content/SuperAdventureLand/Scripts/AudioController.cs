namespace SuperAdventureLand.Scripts
{
    using UnityEngine;

    public enum AudioControl {
        NONE,
        VELOCITY
    }

    public class AudioController : MonoBehaviour
    {
        public AudioSource audioSource;
        public float sensitivity = 10f;
        private Vector3 previousPosition;
        public AudioControl audioControl = AudioControl.NONE;

        public void Update()
        {
            float velocity = (transform.position - previousPosition).magnitude;
            if (audioControl == AudioControl.VELOCITY) {
                audioSource.pitch = 1f + (velocity / 10f);
                audioSource.volume = Mathf.Clamp(velocity * sensitivity, 0f, 1f);
            }
            previousPosition = transform.position;
        }
    }
}
