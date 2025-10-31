using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(EasyLayer), true)]
public class EasyLayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EasyLayer easyLayer = (EasyLayer)target;
        //make newTag a dropdown of all tags in the project
        string[] layers = UnityEditorInternal.InternalEditorUtility.layers;
        int selectedIndex = Mathf.Max(0, System.Array.IndexOf(layers, LayerMask.LayerToName(easyLayer.newLayer)));
        int newSelectedIndex = EditorGUILayout.Popup("New Layer", selectedIndex, layers);
        if (newSelectedIndex != selectedIndex) {
            easyLayer.newLayer = LayerMask.NameToLayer(layers[newSelectedIndex]);
            EditorUtility.SetDirty(easyLayer);
        }
    }
}
#endif

public class EasyLayer : EasyEvent
{
    [HideInInspector] public int newLayer;

    public override void OnEvent(object arg0 = null)
    {
        gameObject.layer = newLayer;
        onEvent?.Invoke(null);
    }
}
