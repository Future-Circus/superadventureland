using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.Events;
#endif
public class EasyRouter : EasyEvent
{
    public EasyRouter[] routes;
    public bool ignoreAboveEvent = false;

    public override void OnValidate()
    {
        #if UNITY_EDITOR
        if (!ignoreAboveEvent) {
            base.OnValidate();
        } else {
            RemoveSelfLink();
        }
        #endif
    }
    
    public override void Start () {
        eventOnStart = false;
        base.Start();
    }

    public override void OnEvent(object arg0 = null)
    {
        EasyEvent[] easyEvents = GetComponents<EasyEvent>();
        foreach (var easyEvent in easyEvents) {
            easyEvent.isEnabled = false;
        }
        if (routes.Length > 0) {
            foreach (var route in routes) {
                route.OnEvent(arg0);
            }
        }
        onEvent?.Invoke(arg0);
    }
}
