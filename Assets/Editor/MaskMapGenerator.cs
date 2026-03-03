using UnityEngine;
using UnityEditor;
using System.IO;

public class MaskMapGenerator : EditorWindow
{
    private Texture2D metallic;
    private Texture2D ao;
    private Texture2D detailMask;
    private Texture2D smoothness;
    private Texture2D previewTexture;
    private string savePath;
    private string[] formats = { "PNG", "TGA", "TIFF" };
    private int selectedFormat = 0;

    [MenuItem("Tools/Mask Map Generator")]  
    public static void ShowWindow()
    {
        GetWindow<MaskMapGenerator>("Mask Map Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Select Textures", EditorStyles.boldLabel);
        metallic = (Texture2D)EditorGUILayout.ObjectField("Metallic (R)", metallic, typeof(Texture2D), false);
        ao = (Texture2D)EditorGUILayout.ObjectField("AO (G)", ao, typeof(Texture2D), false);
        detailMask = (Texture2D)EditorGUILayout.ObjectField("Detail Mask (B)", detailMask, typeof(Texture2D), false);
        smoothness = (Texture2D)EditorGUILayout.ObjectField("Smoothness (A)", smoothness, typeof(Texture2D), false);
        
        selectedFormat = EditorGUILayout.Popup("Save Format", selectedFormat, formats);
        
        if (GUILayout.Button("Preview Mask Map"))
        {
            GeneratePreview();
        }
        
        if (previewTexture != null)
        {
            GUILayout.Label("Mask Map Preview", EditorStyles.boldLabel);
            GUILayout.Space(10); // Add padding
            Rect previewRect = GUILayoutUtility.GetAspectRect(1);
            previewRect.x += 10; // Left padding
            previewRect.width -= 20; // Reduce width for padding
            previewRect.y += 10; // Top padding
            previewRect.height -= 20; // Reduce height for padding
            EditorGUI.DrawPreviewTexture(previewRect, previewTexture);
        }
        
        if (GUILayout.Button("Generate Mask Map"))
        {
            GenerateMaskMap();
        }
        
        if (GUILayout.Button("Clear All"))
        {
            ClearAllTextures();
        }
    }

    private void GeneratePreview()
    {
        if (metallic == null || ao == null || detailMask == null || smoothness == null)
        {
            Debug.LogError("Please assign all texture inputs.");
            return;
        }

        int width = metallic.width;
        int height = metallic.height;
        previewTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color mColor = metallic.GetPixel(x, y);
                Color aoColor = ao.GetPixel(x, y);
                Color dColor = detailMask.GetPixel(x, y);
                Color sColor = smoothness.GetPixel(x, y);

                Color maskColor = new Color(mColor.r, aoColor.g, dColor.b, sColor.a);
                previewTexture.SetPixel(x, y, maskColor);
            }
        }
        previewTexture.Apply();
    }

    private void GenerateMaskMap()
    {
        if (previewTexture == null)
        {
            Debug.LogError("Please generate a preview first.");
            return;
        }

        string metallicPath = AssetDatabase.GetAssetPath(metallic);
        if (string.IsNullOrEmpty(metallicPath))
        {
            Debug.LogError("Invalid metallic texture path.");
            return;
        }

        string directory = Path.GetDirectoryName(metallicPath);
        string metallicName = Path.GetFileNameWithoutExtension(metallicPath).Replace("Metallic", "", System.StringComparison.OrdinalIgnoreCase).Replace("metalic", "", System.StringComparison.OrdinalIgnoreCase).Trim();
        string extension = formats[selectedFormat].ToLower();
        savePath = Path.Combine(directory, metallicName + "_MaskMap." + extension);

        SaveTexture(previewTexture, savePath, extension);
    }

    private void SaveTexture(Texture2D texture, string path, string format)
    {
        byte[] data = null;
        switch (format)
        {
            case "png":
                data = texture.EncodeToPNG();
                break;
            case "tga":
                data = texture.EncodeToTGA();
                break;
            case "tiff":
                data = texture.EncodeToEXR(); // TIFF export is not directly supported, EXR as an alternative
                break;
        }

        if (data != null)
        {
            File.WriteAllBytes(path, data);
            AssetDatabase.ImportAsset(path);
            Debug.Log("Mask Map saved to " + path);
        }
        else
        {
            Debug.LogError("Failed to encode texture.");
        }
    }

    private void ClearAllTextures()
    {
        metallic = null;
        ao = null;
        detailMask = null;
        smoothness = null;
        previewTexture = null;
        Debug.Log("Cleared all texture selections.");
    }
}
