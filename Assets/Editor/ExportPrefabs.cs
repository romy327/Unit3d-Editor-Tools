using UnityEngine;
using UnityEditor;
using System.IO;

public class PrefabExporterTXT : EditorWindow
{
    private string prefabsFolder = "Assets/Prefabs";
    private string exportPath = "Assets/PrefabNames.txt";

    [MenuItem("Tools/Export Prefabs to Notepad")]
    public static void ShowWindow()
    {
        GetWindow<PrefabExporterTXT>("Export Prefabs");
    }

    private void OnGUI()
    {
        GUILayout.Label("Export Prefabs to Notepad", EditorStyles.boldLabel);
        prefabsFolder = EditorGUILayout.TextField("Prefabs Folder", prefabsFolder);
        exportPath = EditorGUILayout.TextField("Export Path", exportPath);

        if (GUILayout.Button("Export"))
        {
            ExportPrefabs();
        }
    }

    private void ExportPrefabs()
    {
        string[] prefabFiles = Directory.GetFiles(prefabsFolder, "*.prefab", SearchOption.AllDirectories);
        using (StreamWriter writer = new StreamWriter(exportPath))
        {
            writer.WriteLine("Prefab Names:");
            writer.WriteLine("----------------------");

            foreach (string prefabPath in prefabFiles)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    writer.WriteLine(prefab.name);
                }
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"Prefabs exported to {exportPath} successfully!");
    }
}
