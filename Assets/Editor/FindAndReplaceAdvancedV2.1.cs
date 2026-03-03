using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class ReplaceObjectNamesWindow : EditorWindow
{
    private List<string> findStrings = new List<string>();
    private List<string> replaceStrings = new List<string>();
    private bool includeChildren = true;
    private bool onlyChildrenWithStrings = false;
    private List<string> FilterStrings = new List<string>();

    private Vector2 scrollPosition = Vector2.zero;

    private const string FindPlaceholder = "Enter text to find...";
    private const string ReplacePlaceholder = "Enter replacement text...";
    private const string ChildrenFilterPlaceholder = "Enter string to filter children...";

    [MenuItem("Tools/Find and Replace V2.1")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceObjectNamesWindow>("Find and Replace V2.1");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("Advanced Find and Replace", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Input fields for multiple find strings
        GUILayout.Label("Find:");
        DrawStringList(findStrings, FindPlaceholder);

        // Input fields for multiple replace strings
        GUILayout.Label("Replace:");
        DrawStringList(replaceStrings, ReplacePlaceholder);

        // Option to include children in the find and replace operation
        includeChildren = EditorGUILayout.Toggle("Include Children", includeChildren);

        // Option to filter children by specific strings
        onlyChildrenWithStrings = EditorGUILayout.Toggle("Only Children with Specific Strings", onlyChildrenWithStrings);

        if (onlyChildrenWithStrings)
        {
            GUILayout.Label("Children Filter Strings:");
            DrawStringList(FilterStrings, ChildrenFilterPlaceholder);
        }

        if (GUILayout.Button("Replace in Selected Objects"))
        {
            FindAndReplaceInSelectedObjects(findStrings, replaceStrings);
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("This will replace the text in the names of selected objects and optionally their children.", MessageType.Info);
    }

    private void DrawStringList(List<string> stringList, string placeholder)
    {
        int removeIndex = -1;

        for (int i = 0; i < stringList.Count; i++)
        {
            GUILayout.BeginHorizontal();
            stringList[i] = TextFieldWithPlaceholder(stringList[i], placeholder);

            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                removeIndex = i;
            }
            GUILayout.EndHorizontal();
        }

        if (removeIndex >= 0)
        {
            stringList.RemoveAt(removeIndex);
        }

        if (GUILayout.Button("Add"))
        {
            stringList.Add(string.Empty);
        }
    }

    private void FindAndReplaceInSelectedObjects(List<string> finds, List<string> replaces)
    {
        if (finds.Count == 0 || replaces.Count == 0)
        {
            Debug.LogWarning("Find or Replace strings are empty. Please enter valid strings.");
            return;
        }

        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No objects selected. Please select objects to rename.");
            return;
        }

        int replacedCount = 0;

        foreach (GameObject obj in selectedObjects)
        {
            replacedCount += ReplaceNameRecursively(obj.transform, finds, replaces);
        }

        Debug.Log($"Replacement complete !. Total objects renamed: {replacedCount}");
    }

    private int ReplaceNameRecursively(Transform objTransform, List<string> finds, List<string> replaces)
    {
        int replacedCount = 0;
        string newName = objTransform.name;

        for (int i = 0; i < finds.Count; i++)
        {
            if (i < replaces.Count)
            {
                newName = newName.Replace(finds[i], replaces[i]);
            }
        }

        if (newName != objTransform.name)
        {
            objTransform.name = newName;
            replacedCount++;
        }

        if (includeChildren)
        {
            foreach (Transform child in objTransform)
            {
                if (ShouldReplaceChild(child.name))
                {
                    replacedCount += ReplaceNameRecursively(child, finds, replaces);
                }
            }
        }

        return replacedCount;
    }

    private bool ShouldReplaceChild(string childName)
    {
        if (!onlyChildrenWithStrings) return true;

        foreach (var filterString in FilterStrings)
        {
            if (childName.Contains(filterString))
            {
                return true;
            }
        }
        return false;
    }

    private string TextFieldWithPlaceholder(string text, string placeholder)
    {
        GUI.SetNextControlName("TextField");
        text = EditorGUILayout.TextField(text);

        if (string.IsNullOrEmpty(text) && Event.current.type == EventType.Repaint && !IsTextFieldFocused("TextField"))
        {
            var rect = GUILayoutUtility.GetLastRect();
            EditorGUI.LabelField(rect, placeholder, new GUIStyle() { normal = new GUIStyleState() { textColor = Color.gray } });
        }

        return text;
    }

    private bool IsTextFieldFocused(string controlName)
    {
        return GUI.GetNameOfFocusedControl() == controlName;
    }
}
