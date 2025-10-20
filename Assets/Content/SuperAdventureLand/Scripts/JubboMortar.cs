namespace SuperAdventureLand
{
    using UnityEngine;

    public class JubboMortar : MonoBehaviour
    {
        public ParticleSystem explosionParticles;
        public void OnTriggerEnter(Collider other)
        {
            if(other.gameObject.tag == "GroundJubbo")
            {
                explosionParticles.Play();
            }
        }
    }
}
