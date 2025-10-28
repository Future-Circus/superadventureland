namespace SuperAdventureLand
{
    using System;
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

        private float lastValue = 0f;

        public void Start() {
            previousPosition = transform.position;
        }
        public void Update()
        {
            float velocity = (transform.position - previousPosition).magnitude;
            if (audioControl == AudioControl.VELOCITY) {
                audioSource.pitch = 1f + (velocity / 10f);
                audioSource.volume = Mathf.Clamp(Mathf.Lerp(lastValue, velocity * sensitivity, Time.deltaTime * 10f), 0f, 1f);
                lastValue = audioSource.volume;
            }
            previousPosition = transform.position;
        }
    }
}
