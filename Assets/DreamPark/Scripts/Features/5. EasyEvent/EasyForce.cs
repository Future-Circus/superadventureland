using UnityEngine;

public class EasyForce : EasyEvent
{
    public Vector3 force;
    public Vector3 forceVariation;
    public Transform forceOrigin;
    public ForceMode forceMode = ForceMode.Impulse;
    private Rigidbody rb;
    public override void Awake() {
        base.Awake();
        rb = GetComponent<Rigidbody>();
    }
    public override void OnEvent(object arg0 = null)
    {
        if (rb) {            
            if (forceOrigin) {
                Vector3 forceDirection = (forceOrigin.position - transform.position).normalized;
                Vector3 randomForce = new Vector3(Random.Range(-forceVariation.x, forceVariation.x), Random.Range(-forceVariation.y, forceVariation.y), Random.Range(-forceVariation.z, forceVariation.z));
                Vector3 finalForce = force + new Vector3(forceDirection.x * randomForce.x, forceDirection.y * randomForce.y, forceDirection.z * randomForce.z);
                rb.AddForce(finalForce*rb.mass, forceMode);
            } else {
                Vector3 randomForce = force + new Vector3(Random.Range(-forceVariation.x, forceVariation.x), Random.Range(-forceVariation.y, forceVariation.y), Random.Range(-forceVariation.z, forceVariation.z));
                rb.AddForce(randomForce*rb.mass, forceMode);
            }
        }
        onEvent?.Invoke(null);
    }
}
