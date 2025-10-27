using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class EasyDelay : EasyEvent
{
    public bool randomDelay = false;
    [Range(0f, 60f)] public float delay = 0.5f;
    [ShowIf("randomDelay")][Range(0f, 60f)] public float maxDelay = 0.5f;
    public override void OnEvent(object arg0 = null)
    {
        if (randomDelay) {
            delay = Random.Range(delay, maxDelay);
        }
        StartCoroutine(Delay());
    }
    public IEnumerator Delay()
    {
        if (delay > 0f) {
            yield return new WaitForSeconds(delay);
        }
        onEvent?.Invoke(null);
    }
}
