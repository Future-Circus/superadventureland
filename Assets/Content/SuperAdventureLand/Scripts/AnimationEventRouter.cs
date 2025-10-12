namespace SuperAdventureLand.Scripts
{
    using UnityEngine;
    using System.Reflection;
    using System.Linq;

    public class AnimationEventRouter : MonoBehaviour
    {
        public GameObject target;

        public void RouteEvent(string methodName)
        {
            if (!target) return;

            var methods = target.GetComponents<MonoBehaviour>()
                .SelectMany(m => m.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .Where(m => m.Name == methodName && m.GetParameters().Length == 0);

            foreach (var method in methods)
            {
                method.Invoke(target.GetComponent(method.DeclaringType), null);
                return;
            }

            Debug.LogWarning($"AnimationEventRouter: Could not find method '{methodName}' on {target.name}");
        }
    }
}
