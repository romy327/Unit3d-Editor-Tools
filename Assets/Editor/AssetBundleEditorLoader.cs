using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class AssetBundleEditorLoader : EditorWindow
{
    private string bundleDirectory = "";
    private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
    private Dictionary<string, string[]> bundleAssetNames = new Dictionary<string, string[]>();
    private List<GameObject> instantiatedObjects = new List<GameObject>();
    private Vector2 scrollPos;

    [MenuItem("Tools/AssetBundle Loader")]
    public static void ShowWindow()
    {
        GetWindow<AssetBundleEditorLoader>("AssetBundle Loader");
    }

    private void OnGUI()
    {
        GUILayout.Label("Unity AssetBundle Loader", EditorStyles.boldLabel);

        if (GUILayout.Button("Select AssetBundle Folder"))
        {
            string path = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                bundleDirectory = path;
                LoadAllAssetBundles(bundleDirectory);
            }
        }

        if (string.IsNullOrEmpty(bundleDirectory))
        {
            EditorGUILayout.HelpBox("No folder selected.", MessageType.Info);
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var bundleEntry in loadedBundles)
        {
            EditorGUILayout.LabelField($"Bundle: {Path.GetFileName(bundleEntry.Key)}", EditorStyles.boldLabel);

            var assets = bundleAssetNames[bundleEntry.Key];
            foreach (var assetName in assets)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"- {assetName}", GUILayout.MaxWidth(400));
                if (GUILayout.Button("Instantiate", GUILayout.Width(100)))
                {
                    var asset = bundleEntry.Value.LoadAsset<Object>(assetName);
                    if (asset is GameObject go)
                    {
                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(go);
                        if (instance != null)
                            instantiatedObjects.Add(instance);
                    }
                    else
                    {
                        Debug.LogWarning($"Asset '{assetName}' is not a GameObject and cannot be instantiated.");
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
        }
        EditorGUILayout.EndScrollView();

        if (loadedBundles.Count > 0)
        {
            if (GUILayout.Button("Instantiate All Assets in Scene"))
            {
                InstantiateAllAssetsInScene();
            }

            if (GUILayout.Button("Clear Scene Instances"))
            {
                ClearSceneInstances();
            }

            if (GUILayout.Button("Unload All Bundles"))
            {
                UnloadAllAssetBundles();
            }
        }
    }

    private void LoadAllAssetBundles(string directory)
    {
        UnloadAllAssetBundles();

        var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            if (IsLikelyAssetBundle(file))
            {
                var bundle = AssetBundle.LoadFromFile(file);
                if (bundle != null)
                {
                    loadedBundles[file] = bundle;
                    bundleAssetNames[file] = bundle.GetAllAssetNames();
                    Debug.Log($"Loaded AssetBundle: {file}");
                }
                else
                {
                    Debug.LogWarning($"Failed to load bundle: {file}");
                }
            }
        }

        if (loadedBundles.Count == 0)
        {
            Debug.LogWarning("No AssetBundles found in the selected folder.");
        }
    }

    private void InstantiateAllAssetsInScene()
    {
        int count = 0;

        foreach (var bundle in loadedBundles.Values)
        {
            foreach (var assetName in bundle.GetAllAssetNames())
            {
                var asset = bundle.LoadAsset<Object>(assetName);
                if (asset is GameObject go)
                {
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(go);
                    if (instance != null)
                    {
                        instantiatedObjects.Add(instance);
                        count++;
                    }
                }
                else
                {
                    Debug.LogWarning($"Asset '{assetName}' is not a GameObject and was skipped.");
                }
            }
        }

        Debug.Log($"Instantiated {count} GameObjects.");
    }

    private void ClearSceneInstances()
    {
        foreach (var obj in instantiatedObjects)
        {
            if (obj != null)
                DestroyImmediate(obj);
        }
        instantiatedObjects.Clear();
        Debug.Log("Cleared all instantiated objects from the scene.");
    }

    private void UnloadAllAssetBundles()
    {
        foreach (var bundle in loadedBundles.Values)
        {
            bundle.Unload(true);
        }

        loadedBundles.Clear();
        bundleAssetNames.Clear();
        Debug.Log("Unloaded all AssetBundles.");
    }

    private bool IsLikelyAssetBundle(string file)
    {
        string ext = Path.GetExtension(file).ToLower();
        return ext == "" || ext == ".bundle" || ext == ".unity3d"; // Customize as needed
    }
}
