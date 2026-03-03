using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class ReplaceWithPrefabV2 : EditorWindow
{
    private string folderPath = "Assets/";
    private Vector2 scrollPos;
    private List<GameObject> prefabs = new List<GameObject>();
    private GameObject selectedPrefab;

    [MenuItem("Tools/Replace With Prefab V2")]
    static void CreateReplaceWithPrefab()
    {
        EditorWindow.GetWindow<ReplaceWithPrefabV2>();
    }

    private void OnGUI()
    {
        GUIStyle customLabelStyle = new GUIStyle(EditorStyles.boldLabel);
        customLabelStyle.normal.textColor = new Color(1f, .6f, 0f); // Change color
        customLabelStyle.fontSize = 18; // Change font size
        GUILayout.Space(10);
        GUILayout.Label("[Replace With Prefab V2]", customLabelStyle);
        GUILayout.Space(10);

        GUILayout.Label("Select Prefab Folder", EditorStyles.boldLabel);
        if (GUILayout.Button("Select Folder"))
        {
            string path = EditorUtility.OpenFolderPanel("Select Prefab Folder", "Assets/", "");
            if (!string.IsNullOrEmpty(path))
            {
                folderPath = "Assets" + path.Substring(Application.dataPath.Length);
                LoadPrefabsFromFolder();
            }
        }

        EditorGUILayout.LabelField("Folder Path:", folderPath);
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Total Prefabs Loaded: " + prefabs.Count);
        GUILayout.Space(10);

        GUILayout.Label("Select Prefab to Replace With", EditorStyles.boldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));
        foreach (var prefab in prefabs)
        {
            EditorGUILayout.BeginHorizontal();
            bool isSelected = selectedPrefab == prefab;
            if (EditorGUILayout.Toggle(isSelected, GUILayout.Width(20)))
            {
                selectedPrefab = prefab;
            }
            EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        GUI.enabled = selectedPrefab != null;
        if (GUILayout.Button("Replace Selected Objects"))
        {
            ReplaceSelectedObjects();
        }
        GUI.enabled = true;

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Selection count: " + Selection.objects.Length);

       // Show preview of selected prefab
        if (selectedPrefab != null)
        {
            GUILayout.Space(10);
            GUILayout.Label("Prefab Preview", EditorStyles.boldLabel);

            Texture2D previewTexture = AssetPreview.GetAssetPreview(selectedPrefab);
            if (previewTexture != null)
            {
                float previewSize = 200;
                GUILayout.Label(previewTexture, GUILayout.Width(previewSize), GUILayout.Height(previewSize));
            }
            else
            {
                GUILayout.Label("Generating preview...");
                // Force preview generation if not ready
                AssetPreview.GetAssetPreview(selectedPrefab);
            }
        }



    }

    private void LoadPrefabsFromFolder()
    {
        prefabs.Clear();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
            {
                prefabs.Add(prefab);
            }
        }
    }

    private void ReplaceSelectedObjects()
    {
        if (selectedPrefab == null) return;
        var selection = Selection.gameObjects;

        for (var i = selection.Length - 1; i >= 0; --i)
        {
            var selected = selection[i];
            GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);

            if (newObject == null)
            {
                Debug.LogError("Error instantiating prefab");
                break;
            }

            Undo.RegisterCreatedObjectUndo(newObject, "Replace With Prefabs");
            newObject.transform.parent = selected.transform.parent;
            newObject.transform.localPosition = selected.transform.localPosition;
            newObject.transform.localRotation = selected.transform.localRotation;
            newObject.transform.localScale = selected.transform.localScale;
            newObject.transform.SetSiblingIndex(selected.transform.GetSiblingIndex());
            Undo.DestroyObjectImmediate(selected);
        }
    }
}
