namespace SuperAdventureLand
{
    using System.Collections;
    using System.Linq;
    using UnityEngine;

    public class PengoProjectile : MonoBehaviour
    {
        private enum ProjectileState
        {
            THROWN,
            PRIMED,
            RETURNED
        }

        private ProjectileState state = ProjectileState.THROWN;

        public GameObject spawnPrefab; // Prefab to spawn when the projectile hits the ground

        private Coroutine explodeCoroutine;

        private Rigidbody rb;

        void Awake () {
            rb = GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Check if the collided object has the "Ground" tag
            if (state == ProjectileState.THROWN && collision.gameObject.CompareTag("Ground"))
            {
                state = ProjectileState.PRIMED;
                explodeCoroutine = StartCoroutine(ExplodeCountdown());
                rb.linearDamping = 1f;
                rb.angularDamping = 1f;
            } else if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Kicker") || collision.gameObject.CompareTag("MainCamera")) {

                if (state == ProjectileState.PRIMED) {
                    StopCoroutine(explodeCoroutine);
                }
                rb.linearDamping = 0f;
                rb.angularDamping = 0.05f;
                state = ProjectileState.RETURNED;

                gameObject.tag = "ActiveHit"; // Lets Egg kill pengos
                GameObject bestTarget = FindBestTarget();
                if (bestTarget != null)
                {
                    // Adjust the target position with the offset
                    Vector3 targetPosition = bestTarget.GetComponent<Collider>().bounds.center;
                    Vector3 velocity = CalculateLobVelocity(transform.position, targetPosition, 1f);

                    Rigidbody rb = GetComponent<Rigidbody>();

                    rb.linearVelocity = velocity;

                    // Apply random angular velocity for spinning
                    Vector3 randomTorque = new Vector3(
                        Random.Range(-1f, 1f),
                        Random.Range(-1f, 1f),
                        Random.Range(-1f, 1f)
                    );
                    rb.AddTorque(randomTorque, ForceMode.Impulse);
                } else
                {
                    Vector3 impactDirection = transform.position - collision.contacts[0].point;
                    impactDirection.Normalize();

                    Vector3 hittedForce = impactDirection * 5f;

                    hittedForce.y = Mathf.Max(2f, hittedForce.y);

                    rb.AddForce(hittedForce, ForceMode.Impulse);

                    Vector3 randomTorque = new Vector3(
                        Random.Range(-1f, 1f),
                        Random.Range(-1f, 1f),
                        Random.Range(-1f, 1f)
                    ) * 12f;
                    rb.AddTorque(randomTorque, ForceMode.Impulse);
                }
            } else if (state == ProjectileState.THROWN && collision.gameObject.name.StartsWith("E_JUBBO")) {
                //special case for midair jubbo to egg collision
                Explode();
            } else if (state == ProjectileState.RETURNED) {
                //on a return volley, we want to explode on level geometry and entities only
                if (collision.gameObject.layer == LayerMask.NameToLayer("Entity") || collision.gameObject.layer == LayerMask.NameToLayer("Level") || collision.gameObject.layer == LayerMask.NameToLayer("Enemy")) {
                    Explode();
                }
            }
        }

        private GameObject FindBestTarget()
        {

            // Get layer masks for "Level" and "Entity"
            int levelLayerMask = LayerMask.GetMask("Entity") | LayerMask.GetMask("Level");
            int entityLayerMask = LayerMask.GetMask("Enemy");

            GameObject bestTarget = null;
            float highestScore = Mathf.NegativeInfinity;

            // Check "Level" layer objects
            Collider[] levelColliders = Physics.OverlapSphere(transform.position, 10f, levelLayerMask);
            bestTarget = EvaluateColliders(levelColliders, ref highestScore, false);

            // Check "Entity" layer objects
            Collider[] entityColliders = Physics.OverlapSphere(transform.position, 20f, entityLayerMask);
            bestTarget = EvaluateColliders(entityColliders, ref highestScore, true, bestTarget);

            return bestTarget;
        }
        private GameObject EvaluateColliders(Collider[] colliders, ref float highestScore, bool isEntityLayer, GameObject currentBestTarget = null)
        {
            GameObject bestTarget = currentBestTarget;
            Transform player = Camera.main.transform;

            foreach (Collider collider in colliders)
            {
                string[] filteredTags = { null, "", gameObject.tag, "ActiveHit", "Ground", "Item", "Lava", "Stone" };

                // Dont target objects if it has one of these tags
                if (filteredTags.Contains(collider.gameObject.tag))
                {
                    continue;
                }

                // Check if the object is in front of the player
                Vector3 toTarget = collider.transform.position - player.position;

                // Project both vectors onto the ground plane (ignore the height)
                Vector3 forwardOnGround = new Vector3(player.forward.x, 0, player.forward.z).normalized;
                Vector3 toTargetOnGround = new Vector3(toTarget.x, 0, toTarget.z).normalized;
                float dotProduct = Vector3.Dot(forwardOnGround, toTargetOnGround);

                if (dotProduct < 0.7f)
                {
                    continue; // Ignore targets outside the field of view on the ground plane
                }

                // Calculate distance score (closer objects are better)
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                float distanceScore = 1 / distance;

                // Add layer bonus if it's on the "Entity" layer
                float layerBonus = (isEntityLayer && collider.gameObject.tag != "Fence") ? 2f : 0f;

                // Combine distance score and layer bonus
                float totalScore = distanceScore + layerBonus;

                // Prioritize objects with the highest total score
                if (totalScore > highestScore)
                {
                    highestScore = totalScore;
                    bestTarget = collider.gameObject;
                }
            }

            return bestTarget;
        }

        private Vector3 CalculateLobVelocity(Vector3 start, Vector3 target, float height)
        {
            var gravity = 9.81f;
            // Displacement in XZ plane
            Vector3 displacementXZ = new Vector3(target.x - start.x, 0, target.z - start.z);

            // Height difference between the start and target
            float verticalDisplacement = target.y - start.y;

            // Ensure height is positive and higher than the target position
            float peakHeight = Mathf.Max(height, verticalDisplacement + 0.1f); // Add a small buffer to avoid negative sqrt

            // Time to reach the apex (vertical motion)
            float timeToApex = Mathf.Sqrt(2 * peakHeight / gravity);

            // Total time for the projectile to reach the target
            float totalFlightTime = timeToApex + Mathf.Sqrt(2 * (peakHeight - verticalDisplacement) / gravity);

            if (float.IsNaN(totalFlightTime) || totalFlightTime <= 0)
            {
                return Vector3.zero;
            }

            // Velocity in XZ plane
            Vector3 velocityXZ = displacementXZ / totalFlightTime;

            // Velocity in Y (vertical motion)
            float velocityY = Mathf.Sqrt(2 * gravity * peakHeight);

            // Combine horizontal and vertical components
            return velocityXZ + Vector3.up * velocityY;
        }

        private IEnumerator ExplodeCountdown () {
            yield return new WaitForSeconds(2f);
            Explode();
        }

        public void Explode () {
             // Spawn the prefab at the collision position
            if (spawnPrefab != null)
            {
                Instantiate(spawnPrefab, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
    }
}
