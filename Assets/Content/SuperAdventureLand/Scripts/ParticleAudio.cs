namespace SuperAdventureLand.Scripts
{
    using UnityEngine;

    public class ParticleAudio : MonoBehaviour
    {
        //play audiosource when particle system begins
        public AudioSource audioSource;
        public ParticleSystem ps;

        private bool isPlaying = false;

        void Start()
        {
            ps = GetComponent<ParticleSystem>();
            audioSource = GetComponent<AudioSource>();

            // Stop audio on start if particles aren't playing
            if (audioSource != null && !ps.isPlaying)
            {
                audioSource.Stop();
                isPlaying = false;
            }
        }

        void Update()
        {
            if (ps == null || audioSource == null) return;

            // Check if particles are actually emitting or have particles
            bool particlesActive = ps.isPlaying || ps.particleCount > 0;

            if (!isPlaying && particlesActive && !audioSource.isPlaying)
            {
                audioSource.PlayWithFadeIn(0.5f, this);
                isPlaying = true;
            }
            else if (isPlaying && !particlesActive)
            {
                audioSource.PauseWithFadeOut(0.5f, this);
                isPlaying = false;
            }
        }
    }
}
