using UnityEngine;

public class EasyRouter : EasyEvent
{
    public EasyRouter[] routes;
    public bool ignoreIncomingEvent = false;

    public override void Awake()
    {
        if (!ignoreIncomingEvent) {
            base.Awake();
        }
    }
    
    public override void Start () {
        eventOnStart = false;
        base.Start();
    }

    public override void OnEvent(object arg0 = null)
    {
        if (routes.Length > 0) {
            foreach (var route in routes) {
                route.OnEvent(arg0);
            }
        }
        onEvent?.Invoke(arg0);
    }
}
