using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

// This part is editor-only
[CustomPropertyDrawer(typeof(ShowIfAttribute))]
public class ShowIfDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ShowIfAttribute showIf = (ShowIfAttribute)attribute;
        SerializedProperty conditionProp = property.serializedObject.FindProperty(showIf.conditionFieldName);

        if (conditionProp != null && conditionProp.propertyType == SerializedPropertyType.Boolean && conditionProp.boolValue)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ShowIfAttribute showIf = (ShowIfAttribute)attribute;
        SerializedProperty conditionProp = property.serializedObject.FindProperty(showIf.conditionFieldName);

        if (conditionProp != null && conditionProp.propertyType == SerializedPropertyType.Boolean && conditionProp.boolValue)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        return 0;
    }
}
#endif

// This part is visible to both runtime and editor
public class ShowIfAttribute : PropertyAttribute
{
    public string conditionFieldName;

    public ShowIfAttribute(string conditionFieldName)
    {
        this.conditionFieldName = conditionFieldName;
    }
}