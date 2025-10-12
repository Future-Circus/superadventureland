namespace SuperAdventureLand.Scripts
{
    using UnityEngine;
    public class SA_Hammer : SA_PowerUp
    {
        public Transform launchPoint;
        public GameObject projectilePrefab;
        public float spinForce = 1000f;
        private Vector3 previousPosition;
        private bool hasTriggered = false;
        private float velocityThreshold = 1f;
        private float directionResetAngle = 80f;
        private float minTrackingVelocity = 0.5f;
        private Vector3 lastTriggerDirection = Vector3.zero;
        public override void ExecuteState()
        {
            switch (state)
            {
                case SA_PowerUpState.ACTIVATE:
                    previousPosition = Vector3.zero;
                    hasTriggered = false;
                    base.ExecuteState();
                    break;
                case SA_PowerUpState.USE:
                    Vector3 spawnPosition = launchPoint.position;
                    Vector3 forwardDirection = launchPoint.forward;
                    Vector3 upDirection = launchPoint.up;

                    Quaternion spawnRotation = Quaternion.LookRotation(forwardDirection);
                    GameObject projectile = Instantiate(projectilePrefab,
                        spawnPosition,
                        spawnRotation);

                    "hammer_throw".PlaySFX(spawnPosition, 0.6f, Random.Range(0.8f, 1.2f));

                    Rigidbody rb = projectile.GetComponent<Rigidbody>();
                    if (!rb.LaunchAtLayer(LayerMask.NameToLayer("Entity"), 5f, LayerMask.NameToLayer("Level"), 20f))
                    {
                        Vector3 forwardVelocity = forwardDirection * 0.2f;
                        Vector3 upwardVelocity = Vector3.up * Mathf.Sqrt(2f * Physics.gravity.magnitude * 2f);
                        rb.linearVelocity = forwardVelocity + upwardVelocity;
                    }
                    rb.angularVelocity = upDirection * spinForce;

                    Destroy(projectile, 5.0f);

                    break;
                default:
                    base.ExecuteState();
                    break;
            }
        }

        private HandTracker hand;
        //we detect the throw by tracking the hand velocity
        public override bool PLAYER_INPUT {
            get {
                if (hand == null) {
                    hand = FindAnyObjectByType<HandTracker>();
                }
                if (hand != null) {
                    Vector3 currentPosition = hand.transform.position;
                    Vector3 velocity = (currentPosition - previousPosition) / Time.deltaTime;
                    previousPosition = currentPosition;
                    float speed = velocity.magnitude;
                    if (hasTriggered && speed > minTrackingVelocity)
                    {
                        float angle = Vector3.Angle(lastTriggerDirection, velocity);
                        if (angle > directionResetAngle)
                        {
                            hasTriggered = false;
                        }
                    }
                    if (speed > velocityThreshold && !hasTriggered)
                    {
                        lastTriggerDirection = velocity.normalized;
                        hasTriggered = true;
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
