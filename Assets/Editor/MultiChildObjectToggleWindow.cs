using UnityEngine;
using UnityEditor;

public class MultiChildObjectToggleWindow : EditorWindow
{
    private string targetNames = ""; // Comma-separated list of child object names
    private bool enableObjects = true; // Toggle to enable or disable objects

    [MenuItem("Tools/Enable-Disable Multiple Child Objects")]
    public static void ShowWindow()
    {
        GetWindow<MultiChildObjectToggleWindow>("Child Object Toggle");
    }

    private void OnGUI()
    {
        GUILayout.Label("Enable/Disable Multiple Child Objects", EditorStyles.boldLabel);

        // Input field for comma-separated child object names
        targetNames = EditorGUILayout.TextField("Child Object Names (comma-separated):", targetNames);

        // Toggle between enabling or disabling objects
        enableObjects = EditorGUILayout.Toggle("Enable Objects:", enableObjects);

        // Button to apply the changes
        if (GUILayout.Button("Apply"))
        {
            ApplyToggleToSelectedObjects();
        }
    }

    private void ApplyToggleToSelectedObjects()
    {
        if (Selection.transforms.Length == 0)
        {
            Debug.LogWarning("No objects selected.");
            return;
        }

        string[] nameList = targetNames.Split(',');

        foreach (Transform selectedObject in Selection.transforms)
        {
            ToggleChildObjects(selectedObject, nameList);
        }
    }

    private void ToggleChildObjects(Transform parent, string[] nameList)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            foreach (string name in nameList)
            {
                string trimmedName = name.Trim(); // Remove any extra spaces
                if (child.name.Contains(trimmedName))
                {
                    child.gameObject.SetActive(enableObjects);
                    Debug.Log($"Set '{child.name}' to {(enableObjects ? "enabled" : "disabled")}");
                    break; // Avoid redundant operations if a match is already found
                }
            }
        }
    }
}
