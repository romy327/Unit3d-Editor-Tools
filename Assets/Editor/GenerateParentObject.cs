using UnityEngine;
using UnityEditor;

public class GenerateParentObject : EditorWindow
{
    private string parentName = "NewParent";
    private string selectedObjectNewName = "";

    [MenuItem("Tools/Generate Custom Parent")]
    static void ShowWindow()
    {
        GetWindow<GenerateParentObject>("Generate Parent");
    }

    void OnGUI()
    {
        GUILayout.Label("Generate Parent for Selected Object", EditorStyles.boldLabel);

        parentName = EditorGUILayout.TextField("New Parent Name", parentName);
        selectedObjectNewName = EditorGUILayout.TextField("Rename Selected Object", selectedObjectNewName);

        if (GUILayout.Button("Create Parent and Rename"))
        {
            CreateParentAndRenameSelected();
        }
    }

    void CreateParentAndRenameSelected()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("No GameObject selected!");
            return;
        }

        Transform currentParent = selected.transform.parent;

        // Create new parent GameObject
        GameObject newParent = new GameObject(parentName);
        Undo.RegisterCreatedObjectUndo(newParent, "Create Parent Object");

        // Place parent at same hierarchy level
        if (currentParent != null)
        {
            newParent.transform.SetParent(currentParent, false);
        }

        // Match transform position to selected
        newParent.transform.position = selected.transform.position;
        newParent.transform.rotation = selected.transform.rotation;
        newParent.transform.localScale = Vector3.one;

        // Reparent the selected object
        Undo.SetTransformParent(selected.transform, newParent.transform, "Set Parent");

        // Rename selected object if a name was provided
        if (!string.IsNullOrEmpty(selectedObjectNewName))
        {
            Undo.RecordObject(selected, "Rename Selected Object");
            selected.name = selectedObjectNewName;
        }
    }
}
