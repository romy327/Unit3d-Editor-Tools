using UnityEngine;
using UnityEditor;
using System.IO;

public class PrefabImageExporter : EditorWindow
{
    private string prefabsFolder = "Assets/Prefabs";
    private string exportPath = "Assets/PrefabImages";

    [MenuItem("Tools/Export Prefab Images")]
    public static void ShowWindow()
    {
        GetWindow<PrefabImageExporter>("Export Prefab Images");
    }

    private void OnGUI()
    {
        GUILayout.Label("Export Prefab Images (Multi-Angle, Zoomed 10%)", EditorStyles.boldLabel);
        prefabsFolder = EditorGUILayout.TextField("Prefabs Folder", prefabsFolder);
        exportPath = EditorGUILayout.TextField("Export Folder", exportPath);

        if (GUILayout.Button("Export"))
        {
            ExportPrefabImages();
        }
    }

    private void ExportPrefabImages()
    {
        Directory.CreateDirectory(exportPath);
        string[] prefabFiles = Directory.GetFiles(prefabsFolder, "*.prefab", SearchOption.AllDirectories);

        foreach (string prefabPath in prefabFiles)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                CaptureAndSavePrefabImages(prefab);
                Debug.Log($"Saved images for {prefab.name} in {exportPath}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("Prefab images exported successfully!");
    }

    private void CaptureAndSavePrefabImages(GameObject prefab)
    {
        GameObject instance = Instantiate(prefab);
        instance.transform.position = Vector3.zero;
        instance.SetActive(true);

        Bounds bounds = GetPrefabBounds(instance);
        float cameraDistance = bounds.extents.magnitude * 3.6f; // Zoomed in by 10%

        Vector3[] angles = {
            new Vector3(0, bounds.extents.y, -cameraDistance),  // Front
            new Vector3(-cameraDistance, bounds.extents.y, 0),  // Left
            new Vector3(cameraDistance, bounds.extents.y, 0),   // Right
            new Vector3(0, cameraDistance, 0)                   // Top
        };

        string[] angleNames = { "Front", "Left", "Right", "Top" };

        for (int i = 0; i < angles.Length; i++)
        {
            Texture2D prefabImage = CapturePrefabImage(instance, bounds.center, angles[i]);
            string imagePath = $"{exportPath}/{prefab.name}_{angleNames[i]}.png";
            File.WriteAllBytes(imagePath, prefabImage.EncodeToPNG());
        }

        DestroyImmediate(instance);
    }

    private Texture2D CapturePrefabImage(GameObject instance, Vector3 targetPosition, Vector3 cameraPosition)
    {
        Camera renderCamera = new GameObject("RenderCamera").AddComponent<Camera>();
        renderCamera.transform.position = cameraPosition;
        renderCamera.transform.LookAt(targetPosition);
        renderCamera.clearFlags = CameraClearFlags.Depth;
        renderCamera.backgroundColor = new Color(0, 0, 0, 0);
        renderCamera.orthographic = false;
        renderCamera.fieldOfView = 60;

        renderCamera.targetTexture = new RenderTexture(1024, 1024, 24);
        Texture2D image = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);

        renderCamera.Render();
        RenderTexture.active = renderCamera.targetTexture;
        image.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
        image.Apply();

        DestroyImmediate(renderCamera.gameObject);

        return image;
    }

    private Bounds GetPrefabBounds(GameObject prefabInstance)
    {
        Renderer[] renderers = prefabInstance.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(prefabInstance.transform.position, Vector3.zero);

        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        return bounds;
    }
}
