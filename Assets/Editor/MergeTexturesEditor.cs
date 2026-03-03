using UnityEngine;
using UnityEditor;
using System.IO;

public class MergeTexturesEditor : EditorWindow
{
    private Texture2D albedoTexture;
    private Texture2D aoTexture;
    private Texture2D previewTexture;
    private string customPrefix = "";
    private string customSuffix = "_shadow_merged";
    private float aoOpacity = 1.0f; // AO opacity slider
    private int selectedBlendMode = 0; // 0: Soft Light, 1: Multiply, 2: Overlay
    private Vector2 scrollPosition; // Scroll position for the scroll view

    private readonly string[] blendModes = { "Soft Light", "Multiply", "Overlay" };

    [MenuItem("Tools/Merge Textures (Albedo + AO)")]
    public static void ShowWindow()
    {
        GetWindow<MergeTexturesEditor>("Merge Textures");
    }

    void OnGUI()
    {
        GUILayout.Label("Merge Albedo and AO Textures", EditorStyles.boldLabel);

        albedoTexture = (Texture2D)EditorGUILayout.ObjectField("Albedo Texture", albedoTexture, typeof(Texture2D), false);
        aoTexture = (Texture2D)EditorGUILayout.ObjectField("AO Texture", aoTexture, typeof(Texture2D), false);

        customPrefix = EditorGUILayout.TextField("Custom Prefix", customPrefix);
        customSuffix = EditorGUILayout.TextField("Custom Suffix", customSuffix);

        aoOpacity = EditorGUILayout.Slider("AO Opacity", aoOpacity, 0, 1); // AO opacity slider
        selectedBlendMode = EditorGUILayout.Popup("Blend Mode", selectedBlendMode, blendModes); // Blend mode selection

        // Add scroll support
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Preview merged texture if available
        if (albedoTexture && aoTexture && previewTexture != null)
        {
            GUILayout.Label("Preview", EditorStyles.boldLabel);

            // Get the available width of the editor window
            float availableWidth = EditorGUIUtility.currentViewWidth - 8; // Subtract some padding
            float aspectRatio = (float)previewTexture.width / previewTexture.height;

            // Calculate the dynamic height based on the available width and aspect ratio
            float previewHeight = availableWidth / aspectRatio;

            // Display the texture with dynamically adjusted width and height
            GUILayout.Label(previewTexture, GUILayout.Width(availableWidth), GUILayout.Height(previewHeight));
        }

        EditorGUILayout.EndScrollView(); // End the scrollable section

        // Button to merge textures
        if (GUILayout.Button("Preview Merge"))
        {
            if (albedoTexture && aoTexture)
            {
                if (IsTextureReadable(albedoTexture) && IsTextureReadable(aoTexture))
                {
                    previewTexture = MergeTextures(albedoTexture, aoTexture, aoOpacity, selectedBlendMode);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please assign both Albedo and AO textures.", "OK");
            }
        }

        // Save merged texture button
        if (GUILayout.Button("Save Merged Texture"))
        {
            if (previewTexture)
            {
                SaveMergedTexture(previewTexture, albedoTexture, customPrefix, customSuffix);
            }
        }

        // Clear button to reset all fields
        if (GUILayout.Button("Clear"))
        {
            ClearInputs();
        }
    }

    void ClearInputs()
    {
        albedoTexture = null;
        aoTexture = null;
        previewTexture = null;
        customPrefix = "";
        customSuffix = "_shadow_merged";
        aoOpacity = 1.0f;
    }

    bool IsTextureReadable(Texture2D texture)
    {
        try
        {
            texture.GetPixels();
            return true;
        }
        catch (UnityException)
        {
            string texturePath = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            if (importer != null && !importer.isReadable)
            {
                EditorUtility.DisplayDialog("Texture Not Readable", $"The texture '{texture.name}' is not readable. Please enable 'Read/Write' in the texture import settings.", "OK");
                Selection.activeObject = texture;
            }
            return false;
        }
    }

    Texture2D MergeTextures(Texture2D albedo, Texture2D ao, float opacity, int blendMode)
    {
        int width = albedo.width;
        int height = albedo.height;

        if (ao.width != width || ao.height != height)
        {
            Debug.LogError("Albedo and AO textures must have the same dimensions.");
            return null;
        }

        Texture2D mergedTexture = new Texture2D(width, height);
        Color[] albedoPixels = albedo.GetPixels();
        Color[] aoPixels = ao.GetPixels();
        Color[] mergedPixels = new Color[albedoPixels.Length];

        for (int i = 0; i < albedoPixels.Length; i++)
        {
            Color albedoPixel = albedoPixels[i];
            Color aoPixel = aoPixels[i]; // Get the raw AO pixel

            // Apply blending mode first
            float blendedR = Blend(albedoPixel.r, aoPixel.r, blendMode);
            float blendedG = Blend(albedoPixel.g, aoPixel.g, blendMode);
            float blendedB = Blend(albedoPixel.b, aoPixel.b, blendMode);

            // Now, apply opacity as an interpolation between the original albedo and the blended result
            float finalR = Mathf.Lerp(albedoPixel.r, blendedR, opacity);
            float finalG = Mathf.Lerp(albedoPixel.g, blendedG, opacity);
            float finalB = Mathf.Lerp(albedoPixel.b, blendedB, opacity);

            mergedPixels[i] = new Color(finalR, finalG, finalB, albedoPixel.a); // Keep original alpha
        }

        mergedTexture.SetPixels(mergedPixels);
        mergedTexture.Apply();
        return mergedTexture;
    }

    float Blend(float baseValue, float blendValue, int blendMode)
    {
        switch (blendMode)
        {
            case 0: // Soft Light
                return (blendValue < 0.5f) ? (2 * baseValue * blendValue + baseValue * baseValue * (1 - 2 * blendValue)) : (2 * baseValue * (1 - blendValue) + Mathf.Sqrt(baseValue) * (2 * blendValue - 1));
            case 1: // Multiply
                return baseValue * blendValue;
            case 2: // Overlay
                return (baseValue < 0.5f) ? (2 * baseValue * blendValue) : (1 - 2 * (1 - baseValue) * (1 - blendValue));
            default:
                return baseValue;
        }
    }

    // Save the merged texture as a JPG file
    void SaveMergedTexture(Texture2D mergedTexture, Texture2D sourceTexture, string prefix, string suffix)
    {
        string path = AssetDatabase.GetAssetPath(sourceTexture);
        string directory = Path.GetDirectoryName(path);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);

        // Update file extension to .jpg
        string newFileName = $"{prefix}{fileNameWithoutExtension}{suffix}.jpg";
        string newFilePath = Path.Combine(directory, newFileName);

        // Encode texture to JPG instead of PNG
        byte[] bytes = mergedTexture.EncodeToJPG();
        File.WriteAllBytes(newFilePath, bytes);
        AssetDatabase.Refresh();

        Debug.Log($"Merged texture saved at: {newFilePath}");
    }
}