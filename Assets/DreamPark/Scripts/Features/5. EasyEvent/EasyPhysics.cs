using UnityEngine;

public class EasyPhysics : EasyEvent
{
    public bool isKinematic = false;
    public bool useGravity = true;
    public bool enableColliders = true;
    public RigidbodyConstraints constraints = RigidbodyConstraints.None;

    public override void OnEvent(object arg0 = null)
    {
        var colliders = GetComponentsInChildren<Collider>();
        foreach (var collider in colliders) {
            collider.enabled = enableColliders;
        }
        if (TryGetComponent<Rigidbody>(out var rb)) {
            rb.isKinematic = isKinematic;
            rb.useGravity = useGravity;
            rb.constraints = constraints;
        }
        onEvent?.Invoke(null);
    }
}
