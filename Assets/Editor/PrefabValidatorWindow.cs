using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

public class PrefabValidatorWindow : EditorWindow
{
    private string folderPath = "Assets/";
    private Vector2 scrollPos;
    private Dictionary<GameObject, List<string>> validationResults = new Dictionary<GameObject, List<string>>();

    [MenuItem("Tools/Prefab Validator")]
    public static void ShowWindow()
    {
        GetWindow<PrefabValidatorWindow>("Prefab Validator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Prefab Validation Tool", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        folderPath = EditorGUILayout.TextField("Folder", folderPath);
        if (GUILayout.Button("Select", GUILayout.Width(60)))
        {
            string selected = EditorUtility.OpenFolderPanel("Select Prefab Folder", Application.dataPath, "");
            if (!string.IsNullOrEmpty(selected))
            {
                if (selected.StartsWith(Application.dataPath))
                    folderPath = "Assets" + selected.Substring(Application.dataPath.Length);
                else
                    Debug.LogError("Folder must be inside the Assets directory");
            }
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Validate Prefabs"))
        {
            ValidatePrefabs();
        }

        EditorGUILayout.Space();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (var kvp in validationResults)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            kvp.Key.name = EditorGUILayout.TextField("Prefab", kvp.Key.name);
            if (GUILayout.Button("Ping", GUILayout.Width(50)))
                EditorGUIUtility.PingObject(kvp.Key);
            EditorGUILayout.EndHorizontal();

            foreach (string issue in kvp.Value)
                EditorGUILayout.HelpBox(issue, MessageType.Warning);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }

    private void ValidatePrefabs()
    {
        validationResults.Clear();
        string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

        foreach (string guid in prefabGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

            List<string> issues = new List<string>();
            Component[] components = prefab.GetComponentsInChildren<Component>(true);

            bool hasAnimator = false;

            foreach (Component comp in components)
            {
                if (comp == null)
                {
                    issues.Add("Missing Component in one of the GameObjects.");
                    continue;
                }

                // Animator validation
                if (comp is Animator animator)
                {
                    hasAnimator = true;

                    if (!animator.enabled)
                        issues.Add($"Animator on {comp.gameObject.name} → Animator is disabled.");

                    if (animator.runtimeAnimatorController == null)
                        issues.Add($"Animator on {comp.gameObject.name} → No controller assigned.");
                }

                if (IsTextMeshProComponent(comp))
                {
                    issues.Add($"{comp.GetType().Name} on {comp.gameObject.name} → Please manually verify TextMeshPro fields.");
                    continue;
                }

                SerializedObject so = new SerializedObject(comp);
                SerializedProperty prop = so.GetIterator();
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (prop.objectReferenceValue == null &&
                            prop.name != "m_Script" &&
                            IsFieldRequired(comp, prop.name))
                        {
                            issues.Add($"{comp.GetType().Name} on {comp.gameObject.name} → Unassigned field: {prop.displayName}");
                        }
                    }
                }
            }

            // If no Animator component found at all
            if (!hasAnimator)
                issues.Add("Missing Animator component in the prefab or its children.");

            if (issues.Count > 0)
                validationResults[prefab] = issues;
        }

        Debug.Log($"Validation completed. Found {validationResults.Count} prefab(s) with issues.");
    }

    private bool IsFieldRequired(Component component, string fieldName)
    {
        var type = component.GetType();
        var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (field == null) return false;

        return field.GetCustomAttribute<SerializeField>() != null || field.IsPublic;
    }

    private bool IsTextMeshProComponent(Component comp)
    {
        string typeName = comp.GetType().FullName;
        return typeName.StartsWith("TMPro.") || typeName.Contains("TextMeshPro");
    }
}
