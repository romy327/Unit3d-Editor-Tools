using UnityEngine;
using UnityEditor;

public class RotateObjectsWindow : EditorWindow
{
    [MenuItem("Tools/Rotate Objects")]
    public static void ShowWindow()
    {
        GetWindow<RotateObjectsWindow>("Rotate Objects");
    }

    private void OnGUI()
    {
        GUILayout.Label("Rotate Selected Objects", EditorStyles.boldLabel);

        GUILayout.Space(10);

        // X-Axis Rotation
        GUILayout.Label("Rotate on X Axis");
        if (GUILayout.Button("90°"))
        {
            RotateSelectedObjects(Vector3.right * 90);
        }
        if (GUILayout.Button("180°"))
        {
            RotateSelectedObjects(Vector3.right * 180);
        }
        if (GUILayout.Button("270°"))
        {
            RotateSelectedObjects(Vector3.right * 270);
        }
        if (GUILayout.Button("-90°"))
        {
            RotateSelectedObjects(Vector3.right * -90);
        }
        if (GUILayout.Button("-180°"))
        {
            RotateSelectedObjects(Vector3.right * -180);
        }

        GUILayout.Space(10);

        // Y-Axis Rotation
        GUILayout.Label("Rotate on Y Axis");
        if (GUILayout.Button("90°"))
        {
            RotateSelectedObjects(Vector3.up * 90);
        }
        if (GUILayout.Button("180°"))
        {
            RotateSelectedObjects(Vector3.up * 180);
        }
        if (GUILayout.Button("270°"))
        {
            RotateSelectedObjects(Vector3.up * 270);
        }
        if (GUILayout.Button("-90°"))
        {
            RotateSelectedObjects(Vector3.up * -90);
        }
        if (GUILayout.Button("-180°"))
        {
            RotateSelectedObjects(Vector3.up * -180);
        }

        GUILayout.Space(10);

        // Z-Axis Rotation
        GUILayout.Label("Rotate on Z Axis");
        if (GUILayout.Button("90°"))
        {
            RotateSelectedObjects(Vector3.forward * 90);
        }
        if (GUILayout.Button("180°"))
        {
            RotateSelectedObjects(Vector3.forward * 180);
        }
        if (GUILayout.Button("270°"))
        {
            RotateSelectedObjects(Vector3.forward * 270);
        }
        if (GUILayout.Button("-90°"))
        {
            RotateSelectedObjects(Vector3.forward * -90);
        }
        if (GUILayout.Button("-180°"))
        {
            RotateSelectedObjects(Vector3.forward * -180);
        }
    }

    private void RotateSelectedObjects(Vector3 rotation)
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            Undo.RecordObject(obj.transform, "Rotate Object");
            obj.transform.Rotate(rotation, Space.Self);
            EditorUtility.SetDirty(obj);
        }
    }
}
