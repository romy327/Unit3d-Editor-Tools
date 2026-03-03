// File: Assets/Editor/QuickSlotLoaderWindow.cs
using UnityEditor;
using UnityEngine;

public class QuickSlotLoaderWindow : EditorWindow
{
    private Transform parentObject;
    private string resourcePath = "AUpdates/Light";
    private int loadedCount = 0;
    private bool loadAttempted = false;

    [MenuItem("Tools/Quick Slot Loader")]
    public static void ShowWindow()
    {
        GetWindow<QuickSlotLoaderWindow>("Quick Slot Loader");
    }

    void OnGUI()
    {
        GUILayout.Label("Load Prefabs into Children", EditorStyles.boldLabel);

        parentObject = (Transform)EditorGUILayout.ObjectField("Parent Object", parentObject, typeof(Transform), true);
        resourcePath = EditorGUILayout.TextField("Resource Path", resourcePath);

        if (GUILayout.Button("Load Prefabs"))
        {
            if (parentObject != null)
            {
                loadedCount = LoadPrefabs();
                loadAttempted = true;
            }
            else
            {
                Debug.LogWarning("Parent object is not assigned.");
            }
        }

        if (GUILayout.Button("Clear Loaded Prefabs"))
        {
            if (parentObject != null)
            {
                int cleared = ClearLoadedPrefabs();
                Debug.Log($"Cleared {cleared} prefab instances.");
            }
        }

        if (loadAttempted)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Slots Loaded:", loadedCount.ToString());
        }
    }

    private int LoadPrefabs()
    {
        int count = 0;

        foreach (Transform child in parentObject)
        {
            if (child.transform.childCount > 0)
            {
                continue;
            }
            string childName = child.name;
            if (childName.Contains("_sep_"))
            {
                string[] parts = childName.Split(new string[] { "_sep_" }, System.StringSplitOptions.None);
                if (parts.Length > 1)
                {
                    string prefabName = parts[1];
                    string fullPath = resourcePath + "/" + prefabName;

                    GameObject prefab = Resources.Load<GameObject>(fullPath);

                    if (prefab != null)
                    {
                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        instance.transform.position = child.position;
                        instance.transform.SetParent(child, worldPositionStays: true);
                        instance.name = prefab.name;
                        instance.transform.localScale = Vector3.one;
                        instance.transform.localRotation =Quaternion.Euler(Vector3.zero);
                        Undo.RegisterCreatedObjectUndo(instance, "Instantiate Prefab");
                        count++;
                    }
                    else
                    {
                        Debug.LogWarning("Prefab not found at: " + fullPath);
                    }
                }
            }
        }

        return count;
    }

    private int ClearLoadedPrefabs()
    {
        int removedCount = 0;

        foreach (Transform child in parentObject)
        {
            // Skip children that are slots, only delete their children
            for (int i = child.childCount - 1; i >= 0; i--)
            {
                Transform toRemove = child.GetChild(i);
                Undo.DestroyObjectImmediate(toRemove.gameObject);
                removedCount++;
            }
        }

        return removedCount;
    }
}
