using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class DuplicateObjectFinder : EditorWindow
{
    private List<string> searchStrings = new List<string>();
    private List<GameObject> duplicateObjects = new List<GameObject>();
    private Vector2 scrollPosition;

    [MenuItem("Tools/Duplicate Object Finder")]
    public static void ShowWindow()
    {
        GetWindow<DuplicateObjectFinder>("Duplicate Object Finder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Duplicate Object Finder", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Search String"))
        {
            searchStrings.Add("");
        }

        for (int i = 0; i < searchStrings.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            searchStrings[i] = EditorGUILayout.TextField($"Search String {i + 1}", searchStrings[i]);

            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                searchStrings.RemoveAt(i);
                i--;
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Find Duplicates"))
        {
            FindDuplicates();
        }

        GUILayout.Space(10);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (duplicateObjects.Count > 0)
        {
            GUILayout.Label($"Duplicate Objects Found: {duplicateObjects.Count}", EditorStyles.boldLabel);

            if (GUILayout.Button("Select Duplicates"))
            {
                SelectDuplicates();
            }

            foreach (var obj in duplicateObjects)
            {
                GUILayout.Label(obj.name);
            }
        }
        else
        {
            GUILayout.Label("No duplicates found.");
        }

        EditorGUILayout.EndScrollView();
    }

    private void FindDuplicates()
    {
        duplicateObjects.Clear();
        Dictionary<string, List<GameObject>> objectDict = new Dictionary<string, List<GameObject>>();

        // Use new API
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject obj in allObjects)
        {
            string objectName = CleanName(obj.name);

            if (!ContainsSearchStrings(objectName)) continue;

            if (!objectDict.ContainsKey(objectName))
            {
                objectDict[objectName] = new List<GameObject>();
            }

            objectDict[objectName].Add(obj);
        }

        foreach (var entry in objectDict)
        {
            if (entry.Value.Count > 1)
            {
                duplicateObjects.AddRange(entry.Value);
            }
        }
    }

    private bool ContainsSearchStrings(string name)
    {
        foreach (var searchString in searchStrings)
        {
            if (!string.IsNullOrEmpty(searchString) && name.Contains(searchString))
            {
                return true;
            }
        }
        return false;
    }

    private string CleanName(string name)
    {
        return Regex.Replace(name.Trim(), @"\s*\(\d+\)$", "");
    }

    private void SelectDuplicates()
    {
        Selection.objects = duplicateObjects.ToArray();
        if (duplicateObjects.Count > 0)
        {
            EditorGUIUtility.PingObject(duplicateObjects[0]);
        }
    }
}
