namespace SuperAdventureLand.Scripts
{
    using UnityEngine;
    public class SA_Wand : SA_PowerUp
    {
        public Transform launchPoint;
        public GameObject projectilePrefab;
        public float launchForce = 100f;
        public float spinForce = 10f;
        public override void ExecuteState()
        {
            switch (state)
            {
                case SA_PowerUpState.USE:
                    Vector3 spawnPosition = launchPoint.position;
                    Vector3 forwardDirection = launchPoint.forward;
                    Vector3 upDirection = launchPoint.up;

                    Quaternion spawnRotation = Quaternion.LookRotation(forwardDirection);
                    GameObject projectile = Instantiate(projectilePrefab,
                        spawnPosition,
                        spawnRotation);

                    "bubble_wand".PlaySFX(spawnPosition, 0.6f, Random.Range(0.8f, 1.2f));

                    Rigidbody rb = projectile.GetComponent<Rigidbody>();
                    rb.linearVelocity = forwardDirection * launchForce * 0.1f;
                    rb.angularVelocity = upDirection * spinForce;

                    Destroy(projectile, 5.0f);

                    break;
                default:
                    base.ExecuteState();
                    break;
            }
        }
    }
}
