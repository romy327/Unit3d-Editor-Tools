using UnityEngine;//Auther    : RomyRMichael
using UnityEditor;//Portfolio : https://romyrmichael.c1.biz/
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class AutoSaveAndCrosshair : MonoBehaviour
{
    private static bool showCrosshair = false; // Default to off
    private static bool enableAutoSave = false; // Default to off
    private static float saveInterval = 60f; // Default save interval in seconds
    private static float nextSaveTime = 0f;

    static AutoSaveAndCrosshair()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.update += Update;
    }

    static void OnSceneGUI(SceneView sceneView)
    {
        Handles.BeginGUI();

        // Position the UI at the bottom left of the Scene view
        GUILayout.BeginArea(new Rect(10, sceneView.position.height - 105, 150, 70));

        // Toggle for showing the crosshair
        showCrosshair = GUILayout.Toggle(showCrosshair, "Show Crosshair");

        // Toggle for enabling/disabling autosave
        enableAutoSave = GUILayout.Toggle(enableAutoSave, "Enable Autosave");

        // Input field for autosave interval
        GUILayout.Label("Save Interval (s):");
        saveInterval = EditorGUILayout.FloatField(saveInterval);

        GUILayout.EndArea();

        if (showCrosshair)
        {
            // Draw crosshair at the center of the Scene view
            Vector2 center = new Vector2(sceneView.position.width / 2, sceneView.position.height / 2);
            float size = 12; // Adjust the size of the crosshair here
            Handles.DrawAAPolyLine(2.0f, center + new Vector2(0, -size), center + new Vector2(0, size));
            Handles.DrawAAPolyLine(2.0f, center + new Vector2(-size, 0), center + new Vector2(size, 0));
        }

        Handles.EndGUI();
    }

    static void Update()
    {
        if (enableAutoSave)
        {
            float timeRemaining = nextSaveTime - (float)EditorApplication.timeSinceStartup;

            if (timeRemaining <= 5f && timeRemaining > 0f && EditorSceneManager.GetActiveScene().isDirty)
            {
                // Show a notification 5 seconds before autosave
                ShowNotification("Auto-saving in " + Mathf.CeilToInt(timeRemaining) + " seconds...");
            }

            if (timeRemaining <= 0f)
            {
                // Save the scene only if it's dirty to avoid unnecessary saves
                if (EditorSceneManager.GetActiveScene().isDirty)
                {
                    SaveScene();
                }
                nextSaveTime = (float)EditorApplication.timeSinceStartup + saveInterval;
            }
        }
    }

    static void SaveScene()
    {
        if (!EditorApplication.isPlaying && EditorSceneManager.GetActiveScene().isDirty)
        {
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Scene auto-saved at: " + System.DateTime.Now);
            ClearNotification();
        }
    }

    static void ShowNotification(string message)
    {
        SceneView.lastActiveSceneView.ShowNotification(new GUIContent(message));
    }

    static void ClearNotification()
    {
        SceneView.lastActiveSceneView.RemoveNotification();
    }
}
