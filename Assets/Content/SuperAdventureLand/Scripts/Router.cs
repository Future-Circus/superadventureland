namespace SuperAdventureLand.Scripts
{
    using UnityEngine;
    using UnityEngine.Events;

    public class Router : MonoBehaviour
    {
        public UnityEvent routerEvent;
        public void Invoke () {
            routerEvent?.Invoke();
        }
    }
}
