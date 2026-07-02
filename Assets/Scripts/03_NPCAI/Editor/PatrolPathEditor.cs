using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PatrolPath))]
public class PatrolPathEditor : Editor
{
    private void OnSceneGUI()
    {
        PatrolPath path = (PatrolPath)target;

        SerializedProperty points =
            serializedObject.FindProperty("waypoints");

        for (int i = 0; i < points.arraySize; i++)
        {
            SerializedProperty point = points.GetArrayElementAtIndex(i);

            Vector3 world =
                path.transform.TransformPoint(point.vector3Value);

            EditorGUI.BeginChangeCheck();

            world = Handles.PositionHandle(world, Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(path, "Move Waypoint");

                point.vector3Value =
                    path.transform.InverseTransformPoint(world);

                serializedObject.ApplyModifiedProperties();
            }

            Handles.Label(world + Vector3.up * .3f, i.ToString());
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PatrolPath path = (PatrolPath)target;

        if (GUILayout.Button("Add Point"))
        {
            Undo.RecordObject(path, "Add Point");

            Vector3 worldPos = path.transform.position + path.transform.forward * 1.5f;
            Vector3 localPos = path.transform.InverseTransformPoint(worldPos);

            path.Add(localPos);

            EditorUtility.SetDirty(path);
        }

        if (GUILayout.Button("Clear"))
        {
            Undo.RecordObject(path, "Clear");
            path.Clear();
            EditorUtility.SetDirty(path);
        }
    }
}