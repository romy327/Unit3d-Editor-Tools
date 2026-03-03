using UnityEditor;
using UnityEngine;

public class ObjectAlignWindow : EditorWindow
{
    private GameObject selectedObject;
    private Vector3 alignmentAxis = Vector3.up;

    [MenuItem("Tools/Object Align Window")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(ObjectAlignWindow), false, "Object Align");
    }

    private void OnGUI()
    {
        GUILayout.Label("Select an object and choose alignment axis", EditorStyles.boldLabel);

        selectedObject = EditorGUILayout.ObjectField("Selected Object", selectedObject, typeof(GameObject), true) as GameObject;

        alignmentAxis = EditorGUILayout.Vector3Field("Alignment Axis", alignmentAxis);

        if (GUILayout.Button("Align Objects"))
        {
            AlignObjects();
        }
    }

    private void AlignObjects()
    {
        if (selectedObject == null)
        {
            Debug.LogWarning("Please select an object to use as reference.");
            return;
        }

        // Get all selected objects in the scene
        GameObject[] selectedObjects = Selection.gameObjects;

        // Align each selected object
        foreach (GameObject obj in selectedObjects)
        {
            if (obj != selectedObject)
            {
                Undo.RecordObject(obj.transform, "Align Object");

                // Calculate the position shift needed to align the current object along the specified axis
                Vector3 targetPosition = selectedObject.transform.position +
                                         Vector3.Scale(alignmentAxis.normalized, Vector3.Scale(selectedObject.transform.localScale, selectedObject.transform.forward));

                // Apply the new aligned position to the current object
                obj.transform.position = targetPosition;
            }
        }

        Debug.Log("Alignment complete.");
    }
}
