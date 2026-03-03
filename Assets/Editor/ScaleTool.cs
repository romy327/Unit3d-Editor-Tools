using UnityEditor;
using UnityEngine;

public class ScaleTool : EditorWindow
{
    private float scaleValue = 1.0f;

    [MenuItem("Tools/Set Scale")]
    private static void ShowWindow()
    {
        GetWindow(typeof(ScaleTool), false, "Set Scale");
    }

    private void OnGUI()
    {
        GUILayout.Label("Set Scale", EditorStyles.boldLabel);

        // Input field for scale value
        scaleValue = EditorGUILayout.FloatField("Scale Value", scaleValue);

        // Button to apply scale
        if (GUILayout.Button("Apply Scale"))
        {
            SetScale();
        }
        else if (GUILayout.Button("Reset Rotation"))
        { 
            ResetRotation();
        }
    }

    private void SetScale()
    {
        // Iterate through all selected objects
        foreach (Transform selectedTransform in Selection.transforms)
        {
            Undo.RecordObject(selectedTransform, "Set Scale");

            // Set the scale of the object
            selectedTransform.localScale = new Vector3(scaleValue, scaleValue, scaleValue);
        }
    }
    private void ResetRotation() 
    {
        // Iterate through all selected objects
        foreach (Transform transform in Selection.transforms)
        {
            transform.rotation = Quaternion.identity;
        }
    }
}
