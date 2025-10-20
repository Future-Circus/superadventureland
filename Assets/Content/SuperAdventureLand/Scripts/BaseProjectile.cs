namespace SuperAdventureLand
{
    using UnityEngine;

    public class BaseProjectile : MonoBehaviour
    {
        public float selfDestructTime = 10f;
        public LayerMask collisionMask = -1;

        public void OnCollisionEnter (Collision collision)
        {
            if (((1 << collision.gameObject.layer) & collisionMask) != 0)
            {
                Destroy(gameObject);
            }
        }

        void Start ()
        {
            Destroy(gameObject, selfDestructTime);
        }
    }
}
