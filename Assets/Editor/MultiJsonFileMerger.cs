using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using Unity.Plastic.Newtonsoft.Json.Linq;

public class MultiJsonFileMerger : EditorWindow
{
    private List<string> jsonFilePaths = new List<string>();
    private string outputJsonFilePath;

    [MenuItem("Tools/Merge Multiple JSON Files")]
    public static void ShowWindow()
    {
        GetWindow<MultiJsonFileMerger>("Merge Multiple JSON Files");
    }

    private void OnGUI()
    {
        GUILayout.Label("Merge Multiple JSON Files", EditorStyles.boldLabel);

        if (GUILayout.Button("Select JSON Files"))
        {
            string[] paths = EditorUtility.OpenFilePanel("Select JSON Files", "", "json").Split('|');
            if (paths != null && paths.Length > 0)
            {
                foreach (string path in paths)
                {
                    jsonFilePaths.Add(path);
                }
            }
        }

        foreach (string filePath in jsonFilePaths)
        {
            EditorGUILayout.TextField("JSON File Path:", filePath);
        }

        if (GUILayout.Button("Select Output JSON File"))
        {
            outputJsonFilePath = EditorUtility.SaveFilePanel("Select Output JSON File", "", "output", "json");
        }
        EditorGUILayout.TextField("Output JSON File Path:", outputJsonFilePath);

        EditorGUILayout.Space();

        if (GUILayout.Button("Merge"))
        {
            MergeJsonFiles(jsonFilePaths.ToArray(), outputJsonFilePath);
        }

        if (GUILayout.Button("Clear All"))
        {
            jsonFilePaths.Clear();
            outputJsonFilePath = "";/*Json Merged file path*/
        }
    }

    private void MergeJsonFiles(string[] filePaths, string outputFilePath)
    {
        if (filePaths == null || filePaths.Length < 2)
        {
            Debug.LogError("Please select at least two JSON files to merge.");
            return;
        }

        JObject mergedJsonObject = new JObject();

        foreach (string filePath in filePaths)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"File does not exist: {filePath}");
                return;
            }

            string json = File.ReadAllText(filePath);
            JObject jsonObject = JObject.Parse(json);

            mergedJsonObject.Merge(jsonObject, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Union
            });
        }

        // Write the merged JSON to the output file with formatting to compress
        File.WriteAllText(outputFilePath, mergedJsonObject.ToString(Unity.Plastic.Newtonsoft.Json.Formatting.None));

        Debug.Log("JSON files merged successfully.");
    }
}
