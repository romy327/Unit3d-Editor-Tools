using UnityEngine;
using UnityEditor;
using System.IO;

public class ExportChildrenNames : EditorWindow
{
    private string outputFolderPath = "Assets/";
    private string outputFileName = "ExportedNames.txt";
    private GameObject[] selectedObjects;
    private string prefixToRemove = "";
    private string suffixToRemove = "";
    private string[] keywordsToInclude = new string[] { };
    private string[] wordsToReplace = new string[] { };
    private string[] replacementWords = new string[] { };

    [MenuItem("Tools/Export Children Names")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(ExportChildrenNames));
    }

    void OnGUI()
    {
        GUILayout.Label("Export Children Names", EditorStyles.boldLabel);

        selectedObjects = Selection.gameObjects;

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Output Folder Path:", GUILayout.Width(120));
        outputFolderPath = EditorGUILayout.TextField(outputFolderPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string selectedFolderPath = EditorUtility.OpenFolderPanel("Select Output Folder", outputFolderPath, "");
            if (!string.IsNullOrEmpty(selectedFolderPath))
            {
                outputFolderPath = selectedFolderPath + "/";
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("File Name:", GUILayout.Width(120));
        outputFileName = EditorGUILayout.TextField(outputFileName);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Remove Prefix:");
        prefixToRemove = EditorGUILayout.TextField(prefixToRemove);

        EditorGUILayout.LabelField("Remove Suffix:");
        suffixToRemove = EditorGUILayout.TextField(suffixToRemove);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Keywords to Include (comma-separated):");
        string keywordsInput = EditorGUILayout.TextField(string.Join(",", keywordsToInclude));
        keywordsToInclude = keywordsInput.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Find Words (comma-separated):");
        string findWordsInput = EditorGUILayout.TextField(string.Join(",", wordsToReplace));
        wordsToReplace = findWordsInput.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);

        EditorGUILayout.LabelField("Replace With (comma-separated):");
        string replaceWordsInput = EditorGUILayout.TextField(string.Join(",", replacementWords));
        replacementWords = replaceWordsInput.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);

        EditorGUILayout.Space();

        if (GUILayout.Button("Export Names"))
        {
            ExportNames();
        }
    }

    private void ExportNames()
    {
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("Please select at least one GameObject.");
            return;
        }

        if (wordsToReplace.Length != replacementWords.Length)
        {
            Debug.LogWarning("The number of words to find and replace should match.");
            return;
        }

        try
        {
            using (StreamWriter writer = new StreamWriter(outputFolderPath + outputFileName))
            {
                foreach (GameObject obj in selectedObjects)
                {
                    Transform[] children = obj.GetComponentsInChildren<Transform>(true);

                    foreach (Transform child in children)
                    {
                        string childName = child.name;

                        // Remove prefix if specified
                        if (!string.IsNullOrEmpty(prefixToRemove) && childName.StartsWith(prefixToRemove))
                        {
                            childName = childName.Substring(prefixToRemove.Length);
                        }

                        // Remove suffix if specified
                        if (!string.IsNullOrEmpty(suffixToRemove) && childName.EndsWith(suffixToRemove))
                        {
                            childName = childName.Substring(0, childName.Length - suffixToRemove.Length);
                        }

                        // Include only names containing specified keywords
                        bool containsKeyword = false;
                        foreach (string keyword in keywordsToInclude)
                        {
                            if (childName.Contains(keyword.Trim()))
                            {
                                containsKeyword = true;
                                break;
                            }
                        }

                        if (!containsKeyword)
                        {
                            continue;
                        }

                        // Replace words if specified
                        for (int i = 0; i < wordsToReplace.Length; i++)
                        {
                            string findWord = wordsToReplace[i].Trim();
                            string replaceWith = i < replacementWords.Length ? replacementWords[i].Trim() : "";
                            if (childName.Contains(findWord))
                            {
                                childName = childName.Replace(findWord, replaceWith);
                            }
                        }

                        writer.WriteLine(childName);
                    }
                }
            }

            Debug.Log("Names exported successfully to: " + outputFolderPath + outputFileName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error exporting names: " + e.Message);
        }
    }
}
