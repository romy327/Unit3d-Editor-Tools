using UnityEditor;
using UnityEngine;
using System.IO;

public class MultiPackageImporter : EditorWindow
{
    private string[] packagePaths;
    private Vector2 scrollPosition; // For scroll position

    [MenuItem("Tools/Multi Package Importer")]
    public static void ShowWindow()
    {
        GetWindow<MultiPackageImporter>("Multi Package Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Select Unity Packages", EditorStyles.boldLabel);

        if (GUILayout.Button("Browse..."))
        {
            string path = EditorUtility.OpenFolderPanel("Select Folder with Unity Packages", "", "");
            if (!string.IsNullOrEmpty(path))
            {
                packagePaths = Directory.GetFiles(path, "*.unitypackage");
            }
        }

        // Add scroll view
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        if (packagePaths != null && packagePaths.Length > 0)
        {
            GUILayout.Label("Selected Packages:");
            foreach (var packagePath in packagePaths)
            {
                GUILayout.Label(Path.GetFileName(packagePath));
            }

            if (GUILayout.Button("Import All"))
            {
                ImportAllPackages();
            }
        }

        GUILayout.EndScrollView(); // End scroll view
    }

    private void ImportAllPackages()
    {
        if (packagePaths == null || packagePaths.Length == 0)
        {
            Debug.LogWarning("No packages selected for import.");
            return;
        }

        foreach (var packagePath in packagePaths)
        {
            AssetDatabase.ImportPackage(packagePath, false);
        }

        Debug.Log("All selected packages have been imported.");
    }
}
