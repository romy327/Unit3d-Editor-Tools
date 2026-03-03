using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class FileSort : EditorWindow
{
    private string folderPath = "Assets/";
    private Vector2 scrollPos;

    private List<string> assetPaths = new List<string>();
    private Dictionary<string, bool> selectedAssets = new Dictionary<string, bool>();

    private enum SortMode { Name, Type, ModifiedDate }
    private SortMode currentSort = SortMode.Name;

    [MenuItem("Tools/File Sort")]
    public static void ShowWindow()
    {
        GetWindow<FileSort>("File Sort");
    }

    private void OnGUI()
    {
        GUILayout.Label("Sort Project Assets", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        folderPath = EditorGUILayout.TextField("Folder:", folderPath);
        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            string selected = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
            if (!string.IsNullOrEmpty(selected))
            {
                selected = selected.Replace(Application.dataPath, "Assets");
                folderPath = selected;
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Load Assets"))
        {
            LoadAssets();
        }

        GUILayout.Space(10);

        GUILayout.Label("Sorting Options:");
        currentSort = (SortMode)EditorGUILayout.EnumPopup("Sort By:", currentSort);

        if (GUILayout.Button("Apply Sort"))
        {
            SortAssets();
        }

        GUILayout.Space(10);

        // MULTI SELECT BUTTON
        if (GUILayout.Button("Select Checked Assets"))
        {
            SelectCheckedAssets();
        }

        GUILayout.Space(10);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (var path in assetPaths)
        {
            EditorGUILayout.BeginHorizontal();

            // Checkbox
            if (!selectedAssets.ContainsKey(path))
                selectedAssets[path] = false;

            selectedAssets[path] = EditorGUILayout.Toggle(selectedAssets[path], GUILayout.Width(20));

            // Object preview
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            EditorGUILayout.ObjectField(obj, typeof(Object), false);

            // Single select button
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void LoadAssets()
    {
        assetPaths = Directory
            .GetFiles(folderPath, "*", SearchOption.AllDirectories)
            .Where(p => !p.EndsWith(".meta"))
            .ToList();

        selectedAssets.Clear();
    }

    private void SortAssets()
    {
        switch (currentSort)
        {
            case SortMode.Name:
                assetPaths.Sort((a, b) => Path.GetFileName(a).CompareTo(Path.GetFileName(b)));
                break;

            case SortMode.Type:
                assetPaths.Sort((a, b) =>
                    Path.GetExtension(a).CompareTo(Path.GetExtension(b)));
                break;

            case SortMode.ModifiedDate:
                assetPaths.Sort((a, b) =>
                    File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
                break;
        }
    }

    private void SelectCheckedAssets()
    {
        List<Object> objectsToSelect = new List<Object>();

        foreach (var kvp in selectedAssets)
        {
            if (kvp.Value)
            {
                Object obj = AssetDatabase.LoadAssetAtPath<Object>(kvp.Key);
                if (obj != null)
                    objectsToSelect.Add(obj);
            }
        }

        if (objectsToSelect.Count > 0)
        {
            Selection.objects = objectsToSelect.ToArray();
        }
        else
        {
            EditorUtility.DisplayDialog("Nothing Selected", "Please check some assets first.", "OK");
        }
    }
}
