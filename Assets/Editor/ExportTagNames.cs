using UnityEngine;
using UnityEditor;
using System.IO;
using TMPro; // Ensure you have TextMeshPro imported in your project

public class ExportTagNames : EditorWindow
{
    private string customFileName = "File Name";

    [MenuItem("Tools/Export Tag Names")]
    public static void ShowWindow()
    {
        GetWindow<ExportTagNames>("Export Tag Names");
    }

    private void OnGUI()
    {
        GUILayout.Label("Export Selected Tag's Text Values", EditorStyles.boldLabel);

        customFileName = EditorGUILayout.TextField("File Name:", customFileName);

        if (GUILayout.Button("Export"))
        {
            ExportTexts();
        }
    }

    private void ExportTexts()
    {
        if (Selection.activeGameObject == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a GameObject in the hierarchy.", "OK");
            return;
        }

        string folderPath = EditorUtility.SaveFolderPanel("Select Folder to Save Text File", "", "");

        if (string.IsNullOrEmpty(folderPath))
        {
            return;
        }

        string filePath = Path.Combine(folderPath, customFileName + ".txt");

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            Transform selectedTransform = Selection.activeGameObject.transform;
            foreach (Transform child in selectedTransform)
            {
                TMP_Text textComponent = child.transform.GetChild(0).gameObject.GetComponent<TMP_Text>();
                if (textComponent != null)
                {
                    writer.WriteLine(textComponent.text);
                }
                else
                {
                    writer.WriteLine($"{child.name} does not have a TextMeshProUGUI component.");
                }
            }
        }

        EditorUtility.DisplayDialog("Success", "Text values have been exported successfully to " + filePath, "OK");
    }
}
