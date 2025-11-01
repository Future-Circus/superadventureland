using UnityEngine;

public class EasyVelocityTrigger : EasyEvent
{
    public enum ForceFilterMode {
        GREATER_THAN,
        LESS_THAN
    }
    public float velocityThreshold = 1f;
    public ForceFilterMode velocityFilterMode = ForceFilterMode.GREATER_THAN;
    public bool debugger = false;
    private Rigidbody rb;
    private bool triggered = false;
    private Vector3 lastPosition;

    public override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();
    } 
    public override void Start()
    {
        eventOnStart = false;
        base.Start();
    }
    public override void OnEvent(object arg0 = null)
    {
        lastPosition = transform.position;
        base.OnEvent(arg0);
    }

    public void Update()
    {
        if (triggered) {
            return;
        }
        if (rb != null) {
            if (velocityFilterMode == ForceFilterMode.GREATER_THAN && (rb.linearVelocity.magnitude >= velocityThreshold || rb.angularVelocity.magnitude >= velocityThreshold) || velocityFilterMode == ForceFilterMode.LESS_THAN && rb.linearVelocity.magnitude <= velocityThreshold && rb.angularVelocity.magnitude <= velocityThreshold) {
                triggered = true;
                onEvent?.Invoke(rb.linearVelocity.magnitude > rb.angularVelocity.magnitude ? rb.linearVelocity : rb.angularVelocity);
            }
        } else {
            if (velocityFilterMode == ForceFilterMode.GREATER_THAN && (transform.position - lastPosition).magnitude >= velocityThreshold || velocityFilterMode == ForceFilterMode.LESS_THAN && (transform.position - lastPosition).magnitude <= velocityThreshold) {
                triggered = true;
                onEvent?.Invoke((transform.position - lastPosition).normalized);
            }
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
