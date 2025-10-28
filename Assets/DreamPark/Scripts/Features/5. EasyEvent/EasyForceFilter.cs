using UnityEngine;

public class EasyForceFilter : EasyEvent
{
    public float minimumForce = 1f;
    public override void OnEvent(object arg0 = null) {
        if (arg0 is CollisionWrapper collision) {
            if (collision.relativeVelocity.magnitude >= minimumForce) {
                onEvent?.Invoke(collision);
            } else {
                Debug.LogWarning("[EasyForceFilter] force is too low: " + collision.relativeVelocity.magnitude + " lower than " + minimumForce);
            }
        } else {
            Debug.LogWarning("[EasyForceFilter] you are not passing a collision! please use directly with InteractionFilter");
        }
    }
}
