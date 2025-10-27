using UnityEngine;

public class EasyVelocityTrigger : EasyEvent
{
    public float velocityThreshold = 1f;
    public bool debugger = false;
    private Rigidbody rb;
    private bool triggered = false;

    public override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();
    } 
    public override void Start()
    {
        eventOnStart = false;
        base.Start();
        if (rb == null) {
            Debug.LogError("Rigidbody not found on " + gameObject.name);
            onEvent?.Invoke(null);
        }
    }

    public void Update()
    {
        if (triggered) {
            return;
        }
        if (rb.linearVelocity.magnitude > velocityThreshold || rb.angularVelocity.magnitude > velocityThreshold) {
            Debug.Log("VelocityTrigger: Triggering event");
            triggered = true;
            onEvent?.Invoke(null);
        }
    }

    #if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        if (debugger) {
            Gizmos.color = Color.red;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, rb == null ? "0" : "linVel: " + rb.linearVelocity.ToString("F2") + "\nangVel: " + rb.angularVelocity.ToString("F2"));
        }
    }
    #endif
}
