namespace SuperAdventureLand.Scripts
{
    using UnityEngine;
    using UnityEngine.Events;
    #if UNITY_EDITOR
    using UnityEditor;
    #endif

    public class Collide : MonoBehaviour
    {
        public string tagFilter;
        public UnityEvent onCollisionEnter;
        public UnityEvent onCollisionExit;

        public void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.tag.StartsWith(tagFilter))
            {
                onCollisionEnter.Invoke();
            }
        }

        public void OnCollisionExit(Collision other)
        {
            if (other.gameObject.tag.StartsWith(tagFilter))
            {
                onCollisionExit.Invoke();
            }
        }
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(Collide), true)]
    public class CollideEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();  // Draws the default inspector

            Collide collide = (Collide)target;

            if (GUILayout.Button("Test"))
            {
                collide.onCollisionEnter.Invoke();
            }
        }
    }
    #endif
}
