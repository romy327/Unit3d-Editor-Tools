using UnityEngine;
using UnityEditor;

public class SlotCorrector : EditorWindow
{
    private static string suffix1 = "Suffix1_";
    private static string suffix2 = "Suffix2_";
    private static string customSuffix = "";

    // Add menu item to configure suffixes
    [MenuItem("Tools/Slot Corrector/Configure Suffixes")]
    public static void ShowWindow()
    {
        var window = GetWindow<SlotCorrector>("Slot Corrector");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Configure Suffixes", EditorStyles.boldLabel);

        suffix1 = EditorGUILayout.TextField("Suffix 1:", suffix1);
        suffix2 = EditorGUILayout.TextField("Suffix 2:", suffix2);
        customSuffix = EditorGUILayout.TextField("Custom Suffix:", customSuffix);
    }

    // Rename with Suffix 1 (Shortcut: Alt + Shift + X)
    [MenuItem("Tools/Slot Corrector/Rename With Suffix 1 _&x")]
    public static void RenameWithSuffix1()
    {
        RenameSelectedObjects(suffix1);
    }

    // Rename with Suffix 2 (Shortcut: Alt + Shift + Z)
    [MenuItem("Tools/Slot Corrector/Rename With Suffix 2 _&z")]
    public static void RenameWithSuffix2()
    {
        RenameSelectedObjects(suffix2);
    }

    // Rename with Custom Suffix (Shortcut: Alt + Shift + Q)
    [MenuItem("Tools/Slot Corrector/Rename With Custom Suffix _&q")]
    public static void RenameWithCustomSuffix()
    {
        RenameSelectedObjects(customSuffix);
    }

    private static void RenameSelectedObjects(string suffix)
    {
        if (Selection.objects.Length == 0)
        {
            Debug.LogWarning("No objects selected to rename.");
            return;
        }

        Undo.RecordObjects(Selection.objects, "Rename Objects with Suffix");

        foreach (var obj in Selection.objects)
        {
            if (obj != null && obj is GameObject)
            {
                GameObject gameObject = obj as GameObject;
                gameObject.name = gameObject.name + suffix;
                Debug.Log($"Renamed: {gameObject.name}");
            }
        }
    }
}
