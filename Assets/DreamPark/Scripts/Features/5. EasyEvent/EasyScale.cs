using System.Collections;
using UnityEditor.XR;
using UnityEngine;
using UnityEngine.Events;

public class EasyScale : EasyEvent
{
    public Vector3 targetScale = Vector3.one;
    public float duration = 1f;
    public bool delayNextEvent = false;
    public override void OnEvent(object arg0 = null)
    {
        StartCoroutine(Scale());
        if (!delayNextEvent) {
            onEvent?.Invoke(null);
        }
    }
    private IEnumerator Scale()
    {
        if (targetScale == Vector3.zero) {
            if (TryGetComponent<Rigidbody>(out var rb)) {
                rb.isKinematic = true;
            }
        }

        float startTime = Time.time;
        while (Time.time - startTime < duration)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, (Time.time - startTime) / duration);
            yield return null;
        }
        if (delayNextEvent) {
            onEvent?.Invoke(null);
        }
    }
}
