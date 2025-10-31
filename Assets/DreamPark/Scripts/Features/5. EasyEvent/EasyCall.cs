using UnityEngine;
using System.Linq;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
[CustomEditor(typeof(EasyCall), true)]
public class EasyCallEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var easyCall = target as EasyCall;
        base.OnInspectorGUI();
        // list out all methods of the target
        if (easyCall.target != null)
        {
            var methods = easyCall.target.GetType()
                .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                .Where(m => !m.IsSpecialName)
                .Select(m => m.Name)
                .Distinct()
                .ToArray();

            int selectedIndex = Mathf.Max(0, System.Array.IndexOf(methods, easyCall.methodName));
            int newSelectedIndex = EditorGUILayout.Popup("Method", selectedIndex, methods);
            if (newSelectedIndex != selectedIndex)
            {
                easyCall.methodName = methods[newSelectedIndex];
                EditorUtility.SetDirty(easyCall);
            }
        }
    }
}
#endif

public class EasyCall : EasyEvent
{
    public MonoBehaviour target;
    [HideInInspector] public string methodName;
    public override void OnEvent(object arg0 = null)
    {
        if (target == null || String.IsNullOrEmpty(methodName)) {
            Debug.LogWarning("[EasyCall] " + gameObject.name + " - target is null or methodName is empty");
            return;
        }
        target.SendMessage(methodName, arg0);
    }
}
