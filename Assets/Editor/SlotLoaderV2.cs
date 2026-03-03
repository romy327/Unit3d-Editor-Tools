using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class SlotLoaderV2 : EditorWindow
{
    string myString;
    int childCount = 0;
    List<string> areaNames = new List<string>();

    [MenuItem("Tools/Slot Loader")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(SlotLoaderV2));
    }

    void OnGUI()
    {
        GUIStyle customLabelStyle = new GUIStyle(EditorStyles.boldLabel);
        customLabelStyle.normal.textColor = Color.cyan; // Change color
        customLabelStyle.fontSize = 18; // Change font size
        GUILayout.Space(10);
        GUILayout.Label("[Slot Loader V2]", customLabelStyle);
        GUILayout.Space(10);

        GUILayout.Label("Area Codes", EditorStyles.boldLabel);

        for (int i = 0; i < areaNames.Count; i++)
        {
            areaNames[i] = EditorGUILayout.TextField($"Area Code {i + 1}", areaNames[i], GUILayout.Width((int)EditorGUIUtility.currentViewWidth - 10));
        }

        if (GUILayout.Button("Add Area Code", GUILayout.Width(150)))
        {
            areaNames.Add("");
        }

        if (areaNames.Count > 0 && GUILayout.Button("Remove Last Area Code", GUILayout.Width(200)))
        {
            areaNames.RemoveAt(areaNames.Count - 1);
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Show Child Count", GUILayout.Width(300), GUILayout.Height(30)))
        {
            foreach (var selection in Selection.gameObjects)
            {
                childCount = selection.transform.childCount;
            }
        }

        GUILayout.Space(10);
        GUILayout.Label(childCount > 0 ? $"Child Count: {childCount}" : "No Children found", EditorStyles.boldLabel);

        GUILayout.Space(10);
        if (GUILayout.Button("Load All Assets", GUILayout.Width(200), GUILayout.Height(30)))
        {
            LoadAllAssets();
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Clear Slots", GUILayout.Width(200), GUILayout.Height(30)))
        {
            ClearSlots();
        }
    }

    void LoadAllAssets()
    {
        GameObject slots = Selection.activeGameObject ?? GameObject.Find("Slots");
        if (slots == null) return;

        Transform[] children = slots.GetComponentsInChildren<Transform>();
        Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();
        string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();

        foreach (string assetPath in allAssetPaths)
        {
            if (assetPath.StartsWith("Assets/Resources/") && assetPath.EndsWith(".prefab"))
            {
                string fileName = Path.GetFileNameWithoutExtension(assetPath);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null)
                {
                    prefabCache[fileName] = prefab;
                }
            }
        }

        foreach (string areaCode in areaNames)
        {
            string trimmedCode = "_" + areaCode.Trim() + "_";
            
            for (int i = 1; i < children.Length; i++)
            {
                string name = children[i].name;
                string r2 = "";
                bool areaCodeFound = false;

                if (children[i].name.Contains(trimmedCode))
                {
                    int index = name.IndexOf(trimmedCode);
                    string result = name.Remove(index + 1);
                    r2 = result.Replace("_sep_", "").Replace("Slot", "").Replace("heightslot", "");
                    areaCodeFound = true;
                }
                if (!areaCodeFound && name.Contains("_sep_"))
                {
                    r2 = name.Replace("_sep_", "").Replace("Slot", "").Replace("heightslot", "");
                }

                Transform actualChild = children[i];
                if (actualChild.childCount > 0)
                {
                    Debug.LogWarning("Slot not empty!");
                    continue;
                }

                if (prefabCache.TryGetValue(r2, out GameObject reAsset))
                {
                    GameObject goInstance = PrefabUtility.InstantiatePrefab(reAsset, actualChild) as GameObject;
                    if (goInstance != null)
                    {
                        goInstance.name = reAsset.name;
                        goInstance.transform.localPosition = Vector3.zero;
                        goInstance.transform.localEulerAngles = Vector3.zero;
                        goInstance.transform.localScale = Vector3.one;
                    }
                }
            }
        }
    }

    void ClearSlots()
    {
        GameObject slotsParent = Selection.activeGameObject ?? GameObject.Find("Slots");
        if (slotsParent == null) return;

        Transform[] children = slotsParent.GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child != null && (child.gameObject.name.Contains("Slot_sep_") || child.gameObject.name.Contains("heightslot_sep_")) && child.childCount > 0)
            {
                GameObject childGo = child.GetChild(0).gameObject;
                if (childGo != null)
                    DestroyImmediate(childGo);
            }
        }
    }
}
