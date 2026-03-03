using UnityEditor;
using UnityEngine;
using System.IO;

public class ChildNamesExporter : EditorWindow
{
    private Transform selectedObject;
    private string prefixToRemove = "";

    [MenuItem("Window/Honeywell/Child Names Exporter")]
    private static void ShowWindow()
    {
        GetWindow<ChildNamesExporter>("Child Names Exporter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Selected Object:", EditorStyles.boldLabel);
        selectedObject = EditorGUILayout.ObjectField(selectedObject, typeof(Transform), true) as Transform;

        GUILayout.Space(10);

        GUILayout.Label("Prefix to Remove:", EditorStyles.boldLabel);
        prefixToRemove = EditorGUILayout.TextField(prefixToRemove);

        GUILayout.Space(10);

        if (GUILayout.Button("Export Child Names"))
        {
            ExportChildNames();
        }
    }

    private void ExportChildNames()
    {
        if (selectedObject == null)
        {
            Debug.LogError("No object selected!");
            return;
        }

        string fileName = EditorUtility.SaveFilePanel("Export Child Names", "", "child_names.txt", "txt");

        if (string.IsNullOrEmpty(fileName))
        {
            return;
        }

        using (StreamWriter writer = new StreamWriter(fileName))
        {
            foreach (Transform child in selectedObject)
            {
                string childName = child.name;
                if (!string.IsNullOrEmpty(prefixToRemove) && childName.StartsWith(prefixToRemove))
                {
                    childName = childName.Substring(prefixToRemove.Length);
                }
                writer.WriteLine(childName);
            }
        }

        Debug.Log("Child names exported to: " + fileName);
    }
}
