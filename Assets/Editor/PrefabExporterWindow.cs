using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class PrefabExporterWindow : EditorWindow
{
    private List<GameObject> prefabsToExport = new List<GameObject>();
    private bool includeMRTK = false;
    private bool includeMixedRealityToolkit = false;
    private bool includeDependencies = false;

    private Vector2 scrollPosition;

    // New variable to store the export folder path
    private string exportFolderPath = "";

    [MenuItem("Tools/Prefab Exporter")]
    public static void ShowWindow()
    {
        GetWindow<PrefabExporterWindow>("Prefab Exporter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Export Selected Prefabs", EditorStyles.boldLabel);

        // Begin scroll view
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Display selected prefabs
        if (Selection.gameObjects.Length > 0)
        {
            if (GUILayout.Button("Add Selected Prefabs"))
            {
                AddSelectedPrefabs();
            }

            GUILayout.Label("Prefabs to Export:");
            foreach (var prefab in prefabsToExport)
            {
                GUILayout.Label(prefab.name);
            }
        }
        else
        {
            GUILayout.Label("No prefabs selected. Select prefabs in the hierarchy and click 'Add Selected Prefabs'.");
        }

        GUILayout.Space(10);

        includeMRTK = EditorGUILayout.Toggle("Include MRTK SDK", includeMRTK);
        includeMixedRealityToolkit = EditorGUILayout.Toggle("Include MixedRealityToolkit", includeMixedRealityToolkit);
        includeDependencies = EditorGUILayout.Toggle("Include Dependencies", includeDependencies);

        GUILayout.Space(10);

        // Display the currently selected export folder or indicate if none is selected
        GUILayout.Label("Export Folder: " + (string.IsNullOrEmpty(exportFolderPath) ? "Not selected" : exportFolderPath));

        // Button to open the folder selection dialog
        if (GUILayout.Button("Select Export Folder"))
        {
            SelectExportFolder();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Export Prefabs as Packages"))
        {
            ExportPrefabs();
        }

        // End scroll view
        EditorGUILayout.EndScrollView();
    }

    // Method to open the folder selection dialog and store the selected path
    private void SelectExportFolder()
    {
        string path = EditorUtility.OpenFolderPanel("Select Export Folder", "", "");
        if (!string.IsNullOrEmpty(path))
        {
            exportFolderPath = path;
        }
    }

    private void AddSelectedPrefabs()
    {
        prefabsToExport.Clear();
        foreach (var obj in Selection.gameObjects)
        {
            if (PrefabUtility.GetPrefabAssetType(obj) == PrefabAssetType.Regular ||
                PrefabUtility.GetPrefabAssetType(obj) == PrefabAssetType.Variant)
            {
                prefabsToExport.Add(obj);
            }
        }
    }

    private void ExportPrefabs()
    {
        // Check if the export folder has been selected
        if (string.IsNullOrEmpty(exportFolderPath))
        {
            EditorUtility.DisplayDialog("Export Folder Not Set", "Please select an export folder before exporting.", "OK");
            return;
        }

        foreach (var prefab in prefabsToExport)
        {
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            // Construct the export path using the selected folder and prefab name
            string packagePath = Path.Combine(exportFolderPath, prefab.name + ".unitypackage");

            List<string> assetPaths = new List<string> { prefabPath };

            if (includeDependencies)
            {
                AddDependencyAssets(assetPaths, prefabPath);
            }

            if (includeMRTK)
            {
                AddMRTKAssets(assetPaths);
            }

            if (includeMixedRealityToolkit)
            {
                AddMixedRealityToolkitAssets(assetPaths);
            }

            AssetDatabase.ExportPackage(assetPaths.ToArray(), packagePath, ExportPackageOptions.Recurse);
        }
        Debug.Log("Export completed!");
    }

    private void AddDependencyAssets(List<string> assetPaths, string rootPrefabPath)
    {
        var dependencies = AssetDatabase.GetDependencies(rootPrefabPath, true);
        foreach (var dependency in dependencies)
        {
            if (!assetPaths.Contains(dependency))
            {
                assetPaths.Add(dependency);
            }
        }
    }

    private void AddMRTKAssets(List<string> assetPaths)
    {
        string mrtkPath = "Packages/com.microsoft.mixedreality.toolkit/";
        if (Directory.Exists(mrtkPath))
        {
            string[] mrtkAssets = Directory.GetFiles(mrtkPath, "*.*", SearchOption.AllDirectories);
            foreach (var asset in mrtkAssets)
            {
                assetPaths.Add(asset.Replace("\\", "/"));
            }
        }
    }

    private void AddMixedRealityToolkitAssets(List<string> assetPaths)
    {
        string toolkitPath = "Packages/com.microsoft.mixedreality.toolkit/Definitions/";
        if (Directory.Exists(toolkitPath))
        {
            string[] toolkitAssets = Directory.GetFiles(toolkitPath, "*.*", SearchOption.AllDirectories);
            foreach (var asset in toolkitAssets)
            {
                assetPaths.Add(asset.Replace("\\", "/"));
            }
        }
    }
}
