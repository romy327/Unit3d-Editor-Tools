using UnityEngine;
using UnityEditor;
using System.IO;

public class PrefabListExporter : EditorWindow
{
    [MenuItem("Tools/Export Prefab List")]
    public static void ExportPrefabList()
    {
        // Select a folder containing prefabs
        string selectedFolder = EditorUtility.OpenFolderPanel("Select Folder Containing Prefabs", "", "");
        if (string.IsNullOrEmpty(selectedFolder))
            return;

        // Get all prefabs in the selected folder
        string[] prefabFiles = Directory.GetFiles(selectedFolder, "*.prefab");
        string[] prefabNames = new string[prefabFiles.Length];

        for (int i = 0; i < prefabFiles.Length; i++)
        {
            string prefabPath = prefabFiles[i];
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            prefabNames[i] = prefabName;
        }

        // Specify the output text file path
        string outputPath = EditorUtility.SaveFilePanel("Save Prefab List", "", "PrefabList", "txt");
        if (string.IsNullOrEmpty(outputPath))
            return;

        // Write prefab names to the text file
        using (StreamWriter writer = new StreamWriter(outputPath))
        {
            foreach (string name in prefabNames)
            {
                writer.WriteLine(name);
            }
        }

        Debug.Log("Prefab list exported successfully!");
    }
}
