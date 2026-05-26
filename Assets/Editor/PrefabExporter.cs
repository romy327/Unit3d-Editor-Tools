using UnityEditor;
using UnityEngine;
using System.IO;

public class PrefabExporter : EditorWindow
{
    private string exportFolderPath = "";

    [MenuItem("Tools/Export Selected Prefabs to Packages")]
    static void Init()
    {
        PrefabExporter window = (PrefabExporter)EditorWindow.GetWindow(typeof(PrefabExporter));
        window.titleContent = new GUIContent("Prefab Exporter");
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Export Prefabs to .unitypackage", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        EditorGUILayout.TextField("Export Folder", exportFolderPath);
        if (GUILayout.Button("Browse", GUILayout.MaxWidth(70)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Export Folder", "", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                exportFolderPath = selectedPath;
            }
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Export Selected Prefabs"))
        {
            if (string.IsNullOrEmpty(exportFolderPath))
            {
                EditorUtility.DisplayDialog("Export Folder Not Set", "Please select an export folder first.", "OK");
            }
            else
            {
                ExportSelectedPrefabs();
            }
        }
    }

    void ExportSelectedPrefabs()
    {
        Object[] selectedObjects = Selection.GetFiltered(typeof(GameObject), SelectionMode.Assets);

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No prefabs selected in the Project window.");
            return;
        }

        foreach (Object obj in selectedObjects)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);

            if (!assetPath.EndsWith(".prefab"))
            {
                Debug.LogWarning($"Skipping {assetPath} (not a prefab)");
                continue;
            }

            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            string packagePath = Path.Combine(exportFolderPath, fileName + ".unitypackage");

            Debug.Log($"Exporting {fileName} to {packagePath}");

            AssetDatabase.ExportPackage(
                assetPath,
                packagePath,
                ExportPackageOptions.IncludeDependencies | ExportPackageOptions.Recurse
            );
        }

        EditorUtility.DisplayDialog("Export Complete", "Selected prefabs have been exported.", "OK");
        Debug.Log("Export complete!");
    }
}
