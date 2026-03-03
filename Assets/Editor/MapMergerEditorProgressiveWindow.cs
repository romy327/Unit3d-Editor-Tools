using UnityEditor;
using UnityEngine;
using System.IO;

public class MapMergerEditorWindow : EditorWindow
{
    private Texture2D baseColorTexture;
    private Texture2D aoTexture;
    private float opacity = 1.0f;

    private int blendModeIndex = 0;
    private readonly string[] blendModes = { "Multiply", "Overlay", "Screen", "Soft Light" };

    private int saveFormatIndex = 0;
    private readonly string[] saveFormats = { "PNG", "JPG" };

    private Texture2D mergedTexturePreview;

    [MenuItem("Tools/Map Merger")]
    public static void ShowWindow()
    {
        GetWindow<MapMergerEditorWindow>("Map Merger");
    }

    private void OnGUI()
    {
        GUILayout.Label("Map Merger Tool", EditorStyles.boldLabel);
        baseColorTexture = (Texture2D)EditorGUILayout.ObjectField("Base Color Texture", baseColorTexture, typeof(Texture2D), false);
        aoTexture = (Texture2D)EditorGUILayout.ObjectField("AO Texture", aoTexture, typeof(Texture2D), false);

        opacity = EditorGUILayout.Slider("Opacity", opacity, 0f, 1f);
        blendModeIndex = EditorGUILayout.Popup("Blend Mode", blendModeIndex, blendModes);
        saveFormatIndex = EditorGUILayout.Popup("Save Format", saveFormatIndex, saveFormats);

        if (GUILayout.Button("Preview Merged Map"))
        {
            if (baseColorTexture == null || aoTexture == null)
            {
                Debug.LogError("Please assign both Base Color and AO textures.");
                return;
            }
            CreateMergedTexture();
        }

        if (mergedTexturePreview != null)
        {
            GUILayout.Space(10);
            GUILayout.Label("Merged Texture Preview:");
            Rect previewRect = GUILayoutUtility.GetRect(256, 256, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            GUI.DrawTexture(previewRect, mergedTexturePreview, ScaleMode.ScaleToFit);
        }

        if (mergedTexturePreview != null && GUILayout.Button("Save Merged Texture"))
        {
            SaveMergedTexture();
        }
    }

    private void CreateMergedTexture()
    {
        if (baseColorTexture.width != aoTexture.width || baseColorTexture.height != aoTexture.height)
        {
            Debug.LogError("Textures must have the same dimensions.");
            return;
        }

        int width = baseColorTexture.width;
        int height = baseColorTexture.height;
        Texture2D mergedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color baseColor = baseColorTexture.GetPixel(x, y);
                Color aoColor = aoTexture.GetPixel(x, y);
                Color blended = BlendPixels(baseColor, aoColor, blendModes[blendModeIndex]);
                Color finalColor = Color.Lerp(baseColor, blended, opacity);
                mergedTexture.SetPixel(x, y, finalColor);
            }
        }

        mergedTexture.Apply();
        mergedTexturePreview = mergedTexture;
    }

    private Color BlendPixels(Color baseColor, Color blendColor, string mode)
    {
        switch (mode)
        {
            case "Multiply":
                return baseColor * blendColor;
            case "Overlay":
                return new Color(
                    baseColor.r < 0.5f ? 2 * baseColor.r * blendColor.r : 1 - 2 * (1 - baseColor.r) * (1 - blendColor.r),
                    baseColor.g < 0.5f ? 2 * baseColor.g * blendColor.g : 1 - 2 * (1 - baseColor.g) * (1 - blendColor.g),
                    baseColor.b < 0.5f ? 2 * baseColor.b * blendColor.b : 1 - 2 * (1 - baseColor.b) * (1 - blendColor.b),
                    baseColor.a
                );
            case "Screen":
                return new Color(
                    1 - (1 - baseColor.r) * (1 - blendColor.r),
                    1 - (1 - baseColor.g) * (1 - blendColor.g),
                    1 - (1 - baseColor.b) * (1 - blendColor.b),
                    baseColor.a
                );
            case "Soft Light":
                return new Color(
                    (1 - 2 * blendColor.r) * baseColor.r * baseColor.r + 2 * blendColor.r * baseColor.r,
                    (1 - 2 * blendColor.g) * baseColor.g * baseColor.g + 2 * blendColor.g * baseColor.g,
                    (1 - 2 * blendColor.b) * baseColor.b * baseColor.b + 2 * blendColor.b * baseColor.b,
                    baseColor.a
                );
            default:
                return baseColor;
        }
    }

    private void SaveMergedTexture()
    {
        if (baseColorTexture == null || mergedTexturePreview == null) return;

        string basePath = AssetDatabase.GetAssetPath(baseColorTexture);
        string folder = Path.GetDirectoryName(basePath);
        string baseName = Path.GetFileNameWithoutExtension(basePath);
        string extension = saveFormatIndex == 0 ? ".png" : ".jpg";

        string mergedName = baseName.Replace("__AlbedoTransparency", "") + "__AO_Merged" + extension;
        string fullPath = Path.Combine(folder, mergedName);

        byte[] imageData = saveFormatIndex == 0 ? mergedTexturePreview.EncodeToPNG() : mergedTexturePreview.EncodeToJPG();
        File.WriteAllBytes(fullPath, imageData);

        Debug.Log($"Merged texture saved: {fullPath}");
        AssetDatabase.ImportAsset(fullPath);
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
    }
}
