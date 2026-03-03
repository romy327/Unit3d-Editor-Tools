using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;

public class RenameSelectedObject : EditorWindow
{
    private static string customPrefixKey = "RenameSelectedObject_CustomPrefix";
    private static string customSuffixKey = "RenameSelectedObject_CustomSuffix";

    public string customPrefix = "";
    public string customSuffix = "";
    public string SlotPosition = "";

    [MenuItem("Tools/Rename Selected Object with Child Name")]
    private static void Init()
    {
        RenameSelectedObject window = (RenameSelectedObject)EditorWindow.GetWindow(typeof(RenameSelectedObject));
        window.titleContent = new GUIContent("Rename Selected Object");
        window.LoadEditorPrefs(); // Load saved preferences
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Custom Prefix:", EditorStyles.boldLabel);
        customPrefix = GUILayout.TextField(customPrefix);

        GUILayout.Label("Custom Suffix:", EditorStyles.boldLabel);
        customSuffix = GUILayout.TextField(customSuffix);

        if (GUILayout.Button("Rename Selected Object"))
        {
            RenameObject();
        }
    }

    private void RenameObject()
    {
        foreach (GameObject selectedObject in Selection.gameObjects)
        {
            if (selectedObject != null && selectedObject.transform.childCount > 0)
            {
                Transform firstChild = selectedObject.transform.GetChild(0);
                TMP_Text textMeshPro = firstChild.GetComponent<TMP_Text>();
                string HotspotObj = firstChild.gameObject.name;

                // Check if selectedObject has a parent and a grandparent
                string SlotPosition = (selectedObject.transform.parent != null && selectedObject.transform.parent.parent != null) 
                    ? selectedObject.transform.parent.parent.name 
                    : "No Parent";

                if (textMeshPro != null)
                {
                    string newName = "TagPlate_" + textMeshPro.text + customSuffix;
                    string[] newNameArray = newName.Split(' ', '\n');

                    Undo.RecordObject(selectedObject, "Rename Object");
                    selectedObject.name = newNameArray[0];

                    Debug.Log("TagPlate Named: " + newNameArray[0]);
                }
                else if (HotspotObj == "Hotspot")
                {
                    string newName = HotspotObj + customSuffix;
                    string[] newNameArray = newName.Split(new string[] { "_dot" }, System.StringSplitOptions.None);

                    Undo.RecordObject(selectedObject, "Rename Object");
                    selectedObject.name = newNameArray[0];

                    Debug.Log("Hotspot Named: " + newNameArray[0]);
                }
                else
                {
                    string newName = customPrefix + firstChild.gameObject.name + customSuffix;
                    Undo.RecordObject(selectedObject, "Rename Object");
                    selectedObject.name = newName;

                    Debug.Log("Slot Named: " + newName + "\nSlots Renamed in: " + SlotPosition);
                }
            }
            else
            {
                Debug.Log("Selected object has no children or is null.");
            }
        }
    }

    private void OnDestroy()
    {
        SaveEditorPrefs(); // Save data when the window is destroyed
    }

    private void SaveEditorPrefs()
    {
        EditorPrefs.SetString(customPrefixKey, customPrefix);
        EditorPrefs.SetString(customSuffixKey, customSuffix);
    }

    private void LoadEditorPrefs()
    {
        customPrefix = EditorPrefs.GetString(customPrefixKey, "");
        customSuffix = EditorPrefs.GetString(customSuffixKey, "");
    }
}
