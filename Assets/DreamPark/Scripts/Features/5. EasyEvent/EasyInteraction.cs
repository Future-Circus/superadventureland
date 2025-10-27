using System.Collections;
using OVR.OpenVR;
using UnityEngine;
using UnityEngine.Events;

public class EasyInteraction : EasyEvent
{
     public enum CollisionEvent {
        ENTER,
        EXIT
    }
    [System.Serializable] 
    public class InteractionFilter {
        [SerializeField] public string[] layers;
        [SerializeField] public string[] tags;
        [SerializeField] public CollisionEvent collisionEvent;
        [SerializeField] public EasyEvent onEvent;
    }
    [SerializeField] public InteractionFilter[] interactionFilters;
    [HideInInspector] private CollisionWrapper _lastCollision;
    [HideInInspector] public CollisionWrapper lastCollision {
        get {
            if (_lastCollision == null) {
                return new CollisionWrapper();
            }
            return _lastCollision;
        }
        set {
            _lastCollision = value;
        }
    }
    private bool detecting = false;
    public override void OnEvent(object arg0 = null)
    {
        detecting = true;
    }
    private void OnCollisionEnter(Collision collision) {
        if (detecting) {
            GameObject other = collision.gameObject;
            CheckInteraction(other, true, new CollisionWrapper(collision));
        }
    }
    private void OnCollisionExit(Collision collision) {
        if (detecting) {
            GameObject other = collision.gameObject;
            CheckInteraction(other, false, new CollisionWrapper(collision)); 
        }
    }
    private void OnTriggerEnter(Collider other) {
        if (detecting) {
            CheckInteraction(other.gameObject, true, new CollisionWrapper(other));
        }
    }
    private void OnTriggerExit(Collider other) {
        if (detecting) {
            CheckInteraction(other.gameObject, false, new CollisionWrapper(other));
        }
    }
    private void CheckInteraction(GameObject other, bool isEnter, CollisionWrapper collision = null) {
        foreach (InteractionFilter filter in interactionFilters) {
            bool layerMatch = false;
            bool tagMatch = false;

            // Check layers
            if (filter.layers != null && filter.layers.Length > 0) {
                foreach (string layerName in filter.layers) {
                    if (other.layer == LayerMask.NameToLayer(layerName)) {
                        layerMatch = true;
                        break;
                    }
                }
            } else {
                layerMatch = true; // No layer restrictions
            }

            // Check tags
            if (filter.tags != null && filter.tags.Length > 0) {
                foreach (string tag in filter.tags) {
                    if (other.CompareTag(tag)) {
                        tagMatch = true;
                        break;
                    }
                }
            } else {
                tagMatch = true; // No tag restrictions
            }

            // If both conditions are met, invoke appropriate event
            if (layerMatch && tagMatch && (filter.collisionEvent == CollisionEvent.ENTER && isEnter || filter.collisionEvent == CollisionEvent.EXIT && !isEnter)) {
                lastCollision = collision;
                if (filter.onEvent == null) {
                    filter.onEvent = aboveEvent;
                }
                filter.onEvent?.OnEvent(lastCollision);
                break;
            }
        }
    }
}
