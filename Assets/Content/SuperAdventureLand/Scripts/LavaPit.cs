namespace SuperAdventureLand
{
    using UnityEngine;

    public class LavaPit : MonoBehaviour
    {
        public Transform lavaPitSurface;
        public GameObject lavaSplashPrefab;
        public void OnCollisionEnter(UnityEngine.Collision collision)
        {
            GameObject other = collision.gameObject;
            Collider otherCollider = collision.collider;
            Vector3 surfacePosition = new Vector3(other.transform.position.x, lavaPitSurface.position.y + 0.02f, other.transform.position.z);
            GameObject lavaSplash = Instantiate(lavaSplashPrefab, surfacePosition, Quaternion.identity, transform);
            var scale = otherCollider.bounds.size.x*1.5f;
            Debug.Log("Scale: " + scale);
            lavaSplash.transform.localScale = new Vector3(scale, scale, scale);

        }
    }

}
