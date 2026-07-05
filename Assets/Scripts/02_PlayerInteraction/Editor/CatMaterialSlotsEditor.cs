#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CatMaterialSlots))]
public class CatMaterialSlotsEditor : Editor
{
    private SerializedProperty slotsProp;
    private SerializedProperty applyOnAwakeProp;

    private void OnEnable()
    {
        slotsProp = serializedObject.FindProperty("slots");
        applyOnAwakeProp = serializedObject.FindProperty("applyOnAwake");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        CatMaterialSlots slotsComponent = (CatMaterialSlots)target;

        EditorGUILayout.HelpBox(
            "每一行对应模型上的一个部位（一个 Renderer 的一个子网格）。\n" +
            "把材质直接拖进右侧插槽即可实时预览，不需要改代码或 Shader。\n" +
            "如果模型的材质槽数量变了（比如美术在 Blender 里重新拆分了部位），点击下方 \"Refresh Slots\" 重新生成插槽。",
            MessageType.Info);

        EditorGUILayout.PropertyField(applyOnAwakeProp, new GUIContent("Apply On Awake（运行时自动应用一次）"));

        EditorGUILayout.Space();

        if (slotsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("还没有插槽，点击下方 \"Refresh Slots\" 根据当前模型生成。", MessageType.Warning);
        }

        for (int i = 0; i < slotsProp.arraySize; i++)
        {
            SerializedProperty slot = slotsProp.GetArrayElementAtIndex(i);
            SerializedProperty label = slot.FindPropertyRelative("label");
            SerializedProperty material = slot.FindPropertyRelative("material");
            SerializedProperty rendererProp = slot.FindPropertyRelative("renderer");

            using (new EditorGUILayout.HorizontalScope("box"))
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(170)))
                {
                    EditorGUILayout.LabelField(string.IsNullOrEmpty(label.stringValue) ? "(未命名)" : label.stringValue, EditorStyles.boldLabel);
                    Renderer rendererValue = rendererProp.objectReferenceValue as Renderer;
                    string typeName = rendererValue is SkinnedMeshRenderer ? "Skinned Mesh" : "Mesh";
                    EditorGUILayout.LabelField(typeName, EditorStyles.miniLabel);
                }

                EditorGUILayout.PropertyField(material, GUIContent.none);
            }
        }

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh Slots"))
            {
                Undo.RecordObject(slotsComponent, "Refresh Cat Material Slots");
                slotsComponent.RefreshSlots();
                EditorUtility.SetDirty(slotsComponent);
            }

            if (GUILayout.Button("Apply Now"))
            {
                slotsComponent.Apply();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
