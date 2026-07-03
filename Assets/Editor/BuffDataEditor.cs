using UnityEditor;

[CustomEditor(typeof(Buff))]
public class BuffDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty buffName = serializedObject.FindProperty("buffName");
        SerializedProperty buffNum = serializedObject.FindProperty("buffNum");
        SerializedProperty rank = serializedObject.FindProperty("rank");
        SerializedProperty icon = serializedObject.FindProperty("icon");
        SerializedProperty discription = serializedObject.FindProperty("discription");
        SerializedProperty isInfinite = serializedObject.FindProperty("isInfinite");
        SerializedProperty duration = serializedObject.FindProperty("duration");
        SerializedProperty isConditional = serializedObject.FindProperty("isConditional");
        SerializedProperty conditions = serializedObject.FindProperty("conditions");
        SerializedProperty effects = serializedObject.FindProperty("effects");

        EditorGUILayout.PropertyField(buffName);
        EditorGUILayout.PropertyField(buffNum); ;
        EditorGUILayout.PropertyField(rank);
        EditorGUILayout.PropertyField(icon);
        EditorGUILayout.PropertyField(discription);
        EditorGUILayout.PropertyField(isInfinite);

        if (!isInfinite.boolValue)
        {
            EditorGUILayout.PropertyField(duration);
        }

        EditorGUILayout.PropertyField(isConditional);

        EditorGUILayout.Space();

        if (isConditional.boolValue)
        {
            EditorGUILayout.PropertyField(conditions);
        }

        EditorGUILayout.PropertyField(effects);

        serializedObject.ApplyModifiedProperties();
    }
}