using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class GameObjectParentingWindow : EditorWindow
{
    private List<GameObject> parentObjects = new List<GameObject>(); // Store multiple parent objects
    private List<bool> parentSelection = new List<bool>(); // Track selected parent objects
    private static string parentObjectsKey = "GameObjectParentingWindow_ParentObjectIDs";

    [MenuItem("Tools/GameObject Parenting Window")]
    public static void ShowWindow()
    {
        GameObjectParentingWindow window = (GameObjectParentingWindow)GetWindow(typeof(GameObjectParentingWindow), false, "Parenting Window");
        window.LoadEditorPrefs(); // Load saved parent objects
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Add and Select Parent Objects", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Parent Object"))
        {
            parentObjects.Add(null);
            parentSelection.Add(false);
        }

        for (int i = 0; i < parentObjects.Count; i++)
        {
            GUILayout.BeginHorizontal();

            parentSelection[i] = GUILayout.Toggle(parentSelection[i], "Select");
            parentObjects[i] = EditorGUILayout.ObjectField("Parent Object:", parentObjects[i], typeof(GameObject), true) as GameObject;

            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                parentObjects.RemoveAt(i);
                parentSelection.RemoveAt(i);
                i--; // Adjust index due to removal
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.Space(20);

        if (GUILayout.Button("Parent Selected Objects to Checked Parent(s)"))
        {
            ParentSelectedObjects();
        }
    }

    private void ParentSelectedObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No objects selected to parent.");
            return;
        }

        bool anyParentSelected = false;

        for (int i = 0; i < parentObjects.Count; i++)
        {
            if (parentSelection[i] && parentObjects[i] != null)
            {
                anyParentSelected = true;
                Undo.RegisterCompleteObjectUndo(parentObjects[i].transform, "Parenting Objects");

                foreach (GameObject obj in selectedObjects)
                {
                    Undo.SetTransformParent(obj.transform, parentObjects[i].transform, "Parenting Objects");
                }
            }
        }

        if (!anyParentSelected)
        {
            Debug.LogWarning("No parent object selected. Please select at least one parent.");
            return;
        }

        Debug.Log("Parenting complete.");
        Selection.objects = new Object[0]; // Clear selection to reflect changes
    }

    private void OnDestroy()
    {
        SaveEditorPrefs(); // Save parent objects when the window is destroyed
    }

    private void SaveEditorPrefs()
    {
        List<int> parentObjectIDs = new List<int>();

        foreach (var parent in parentObjects)
        {
            if (parent != null)
                parentObjectIDs.Add(parent.GetInstanceID());
        }

        EditorPrefs.SetString(parentObjectsKey, string.Join(",", parentObjectIDs));
    }

    private void LoadEditorPrefs()
    {
        parentObjects.Clear();
        parentSelection.Clear();

        string savedIDs = EditorPrefs.GetString(parentObjectsKey, "");

        if (!string.IsNullOrEmpty(savedIDs))
        {
            string[] ids = savedIDs.Split(',');
            foreach (var idStr in ids)
            {
                if (int.TryParse(idStr, out int id))
                {
                    GameObject obj = EditorUtility.InstanceIDToObject(id) as GameObject;
                    if (obj != null)
                    {
                        parentObjects.Add(obj);
                        parentSelection.Add(false); // Default to unselected
                    }
                }
            }
        }
    }
}
