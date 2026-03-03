using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;

public class ReplaceTMPFontInPrefabs : EditorWindow
{
    private string selectedFolderPath = "";
    private TMP_FontAsset newFontAsset;

    [MenuItem("Tools/TMP/Replace TMP Font (Windows Picker)")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceTMPFontInPrefabs>("Replace TMP Font");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("Replace TMP Font In Prefabs", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Folder selection
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Target Folder", GUILayout.Width(100));
        EditorGUILayout.SelectableLabel(
            string.IsNullOrEmpty(selectedFolderPath) ? "No folder selected" : selectedFolderPath,
            GUILayout.Height(18));
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Select Folder (Windows Browser)", GUILayout.Height(30)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Folder Containing Prefabs", "Assets", "");

            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    selectedFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Folder",
                        "Folder must be inside the project's Assets folder.",
                        "OK");
                }
            }
        }

        GUILayout.Space(10);

        // Font selection
        newFontAsset = (TMP_FontAsset)EditorGUILayout.ObjectField(
            "New TMP Font Asset",
            newFontAsset,
            typeof(TMP_FontAsset),
            false);

        GUILayout.Space(20);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Replace Fonts In Prefabs", GUILayout.Height(40)))
        {
            if (string.IsNullOrEmpty(selectedFolderPath) || newFontAsset == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "Please select a valid folder and TMP Font Asset.",
                    "OK");
                return;
            }

            ReplaceFonts();
        }
        GUI.backgroundColor = Color.white;
    }

    private void ReplaceFonts()
    {
        string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab", new[] { selectedFolderPath });

        int modifiedCount = 0;
        int total = prefabGUIDs.Length;

        for (int i = 0; i < total; i++)
        {
            string guid = prefabGUIDs[i];
            string path = AssetDatabase.GUIDToAssetPath(guid);

            EditorUtility.DisplayProgressBar("Replacing Fonts",
                $"Processing {Path.GetFileName(path)} ({i + 1}/{total})",
                (float)i / total);

            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;

            var tmpUGUI = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in tmpUGUI)
            {
                if (text.font != newFontAsset)
                {
                    text.font = newFontAsset;
                    EditorUtility.SetDirty(text);
                    changed = true;
                }
            }

            var tmp3D = prefab.GetComponentsInChildren<TextMeshPro>(true);
            foreach (var text in tmp3D)
            {
                if (text.font != newFontAsset)
                {
                    text.font = newFontAsset;
                    EditorUtility.SetDirty(text);
                    changed = true;
                }
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                modifiedCount++;
            }

            PrefabUtility.UnloadPrefabContents(prefab);
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Completed",
            $"Font replacement done.\nModified {modifiedCount} prefabs.",
            "OK");

        Debug.Log($"TMP Font replacement finished. Modified {modifiedCount} prefabs.");
    }
}
