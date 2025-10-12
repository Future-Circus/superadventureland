namespace SuperAdventureLand.Scripts
{
    using UnityEngine;

    public class Projectile : MonoBehaviour
    {
        public LayerMask collisionMask = -1; // Default to all layers
        public float speed = 5.0f;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (speed > 0)
                transform.position += transform.forward * Time.deltaTime * speed;
        }

        //On collision
        void OnCollisionEnter(Collision collision)
        {
            // Check if collision is on the specified layer mask
            if ((collisionMask.value & (1 << collision.gameObject.layer)) != 0)
            {
                if (collision.gameObject.TryGetComponent<PengoBehaviour>(out var pengo))
                {
                    pengo.Kill();
                }
                else if (collision.gameObject.TryGetComponent<Block>(out var block))
                {
                    block.rb.AddForce(transform.forward * 1000);
                }

                Destroy(gameObject);
            }
        }
    }
}
