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
            "每一块对应模型上的一个部位（一个 Renderer 的一个子网格）。\n" +
            "把材质直接拖进插槽即可实时预览。勾选下面的 \"Override\" 后，可以单独给这个部位调 " +
            "Specular / Smoothness / Emission，不会改到 .mat 材质文件本身。\n" +
            "如果模型的材质槽数量变了，点击下方 \"Refresh Slots\" 重新生成插槽。",
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
            SerializedProperty overrideProp = slot.FindPropertyRelative("overrideShaderValues");
            SerializedProperty specularProp = slot.FindPropertyRelative("specular");
            SerializedProperty smoothnessProp = slot.FindPropertyRelative("smoothness");
            SerializedProperty emissionProp = slot.FindPropertyRelative("emission");

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
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

                EditorGUILayout.PropertyField(overrideProp, new GUIContent("Override Specular / Smoothness / Emission"));

                if (overrideProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.Slider(specularProp, 0f, 1f, new GUIContent("Specular"));
                    EditorGUILayout.Slider(smoothnessProp, 0f, 1f, new GUIContent("Smoothness"));
                    EditorGUILayout.Slider(emissionProp, 0f, 1f, new GUIContent("Emission"));
                    EditorGUI.indentLevel--;
                }
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
