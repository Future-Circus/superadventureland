using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class EasyEvent : MonoBehaviour
{
    [HideInInspector] public UnityEvent<object> onEvent;
    [HideInInspector] public EasyEvent aboveEvent;
    [HideInInspector] public bool eventOnStart = false;

    public virtual void Awake()
    {
        EasyEvent[] allComponents = GetComponents<EasyEvent>();
        if (allComponents.Length > 1) {
            for (int i = 0; i < allComponents.Length; i++)
            {
                if (allComponents[i] == this)
                {
                    if (i == 0) {
                        eventOnStart = true;
                        break;
                    }
                    aboveEvent = allComponents[i - 1];
                    aboveEvent.onEvent.AddListener(OnEvent);
                    break;
                }
            }
        } else {
            eventOnStart = true;
        }
    }

    public virtual void Start()
    {
        if (eventOnStart)
        {
            OnEvent();
        }
    }

    public virtual void OnEvent(object arg0 = null) {
        Debug.Log("OnEvent");
        onEvent?.Invoke(arg0);
    }
}
