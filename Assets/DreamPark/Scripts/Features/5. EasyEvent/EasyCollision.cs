using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class EasyCollision : EasyEvent
{
    public string[] layers;
    public string[] tags;
    public enum CollisionEvent {
        ENTER,
        EXIT
    }
    public CollisionEvent collisionEvent;
    private bool detecting = false;
    public override void OnEvent(object arg0 = null)
    {
        detecting = true;
    }

    private void OnCollisionEnter(Collision collision) {
        if (detecting && collisionEvent == CollisionEvent.ENTER) {
            GameObject other = collision.gameObject;
            CheckInteraction(other, new CollisionWrapper(collision));
        }
    }
    private void OnCollisionExit(Collision collision) {
        if (detecting && collisionEvent == CollisionEvent.EXIT) {
        GameObject other = collision.gameObject;
            CheckInteraction(other, new CollisionWrapper(collision)); 
        }
    }
    private void OnTriggerEnter(Collider other) {
        if (detecting && collisionEvent == CollisionEvent.ENTER) {
            CheckInteraction(other.gameObject, new CollisionWrapper(other));
        }
    }
    private void OnTriggerExit(Collider other) {
        if (detecting && collisionEvent == CollisionEvent.EXIT) {
            CheckInteraction(other.gameObject, new CollisionWrapper(other));
        }
    }

     private void CheckInteraction(GameObject other, CollisionWrapper collision = null) {
        bool layerMatch = false;
        bool tagMatch = false;

        if (layers != null && layers.Length > 0) {
            foreach (string layerName in layers) {
                if (other.layer == LayerMask.NameToLayer(layerName)) {
                    layerMatch = true;
                    break;
                }
            }
        } else {
            layerMatch = true;
        }
        if (tags != null && tags.Length > 0) {
            foreach (string tag in tags) {
                if (other.CompareTag(tag)) {
                    tagMatch = true;
                    break;
                }
            }
        } else {
            tagMatch = true;
        }
        if (layerMatch && tagMatch) {
            onEvent?.Invoke(collision);
        }
     }
}
