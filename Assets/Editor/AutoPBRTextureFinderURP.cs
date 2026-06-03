using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class AutoPBRTextureFinderURP : EditorWindow
{
    [MenuItem("Tools/Auto PBR Texture Finder (URP)")]
    public static void ShowWindow()
    {
        GetWindow<AutoPBRTextureFinderURP>("Auto PBR Finder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Auto PBR Texture Finder (URP)", EditorStyles.boldLabel);

        if (GUILayout.Button("Apply Textures To Selected Objects"))
        {
            ApplyToSelection();
        }

        GUILayout.Space(10);
        GUILayout.Label("Select GameObjects in Hierarchy before running.", EditorStyles.helpBox);
    }

    private static void ApplyToSelection()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("No GameObjects selected.");
            return;
        }

        foreach (GameObject obj in selectedObjects)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                if (renderer.sharedMaterials == null) continue;

                Material[] mats = renderer.sharedMaterials;

                for (int i = 0; i < mats.Length; i++)
                {
                    Material mat = mats[i];
                    if (mat == null) continue;

                    ApplyTexturesToMaterial(mat, obj.name, renderer.name);
                }
            }
        }

        Debug.Log("PBR Texture assignment completed.");
    }

    private static void ApplyTexturesToMaterial(Material mat, string objectName, string meshName)
    {
        string[] searchKeys = BuildSearchKeys(mat.name, objectName, meshName);

        Texture2D albedo = FindBestTexture(searchKeys, TextureType.Albedo);
        Texture2D normal = FindBestTexture(searchKeys, TextureType.Normal);
        Texture2D metallic = FindBestTexture(searchKeys, TextureType.Metallic);
        Texture2D ao = FindBestTexture(searchKeys, TextureType.AO);
        Texture2D emission = FindBestTexture(searchKeys, TextureType.Emission);
        Texture2D height = FindBestTexture(searchKeys, TextureType.Height);

        Undo.RecordObject(mat, "Auto Assign PBR Textures");

        if (albedo) mat.SetTexture("_BaseMap", albedo);
        if (normal)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
        }

        if (metallic)
            mat.SetTexture("_MetallicGlossMap", metallic);

        if (ao)
            mat.SetTexture("_OcclusionMap", ao);

        if (emission)
        {
            mat.SetTexture("_EmissionMap", emission);
            mat.EnableKeyword("_EMISSION");
        }

        if (height)
            mat.SetTexture("_ParallaxMap", height);

        EditorUtility.SetDirty(mat);
    }

    private static string[] BuildSearchKeys(string materialName, string objectName, string meshName)
    {
        string cleanMat = Clean(materialName);
        string cleanObj = Clean(objectName);
        string cleanMesh = Clean(meshName);

        List<string> keys = new List<string>
        {
            cleanMat,
            cleanObj,
            cleanMesh,
            cleanMat + "_",
            cleanObj + "_",
            cleanMesh + "_"
        };

        return keys.Where(k => !string.IsNullOrEmpty(k)).Distinct().ToArray();
    }

    private static Texture2D FindBestTexture(string[] keys, TextureType type)
    {
        List<string> typeKeywords = GetTypeKeywords(type);

        foreach (string key in keys)
        {
            foreach (string t in typeKeywords)
            {
                string search = $"{key} {t} t:Texture2D";
                string[] guids = AssetDatabase.FindAssets(search);

                if (guids.Length > 0)
                {
                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        if (tex != null)
                            return tex;
                    }
                }
            }
        }

        return null;
    }

    private static List<string> GetTypeKeywords(TextureType type)
    {
        switch (type)
        {
            case TextureType.Albedo:
                return new List<string> { "albedo", "basecolor", "base_color", "diffuse", "col", "color", "alb" };

            case TextureType.Normal:
                return new List<string> { "normal", "nrm", "norm", "normaldx", "normalgl", "bump" };

            case TextureType.Metallic:
                return new List<string> { "metallic", "metal", "metalness", "mtl" };

            case TextureType.AO:
                return new List<string> { "ao", "occlusion", "ambientocclusion", "occ" };

            case TextureType.Emission:
                return new List<string> { "emission", "emissive", "emit", "glow" };

            case TextureType.Height:
                return new List<string> { "height", "displacement", "parallax", "disp" };
        }

        return new List<string>();
    }

    private static string Clean(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";

        input = input.ToLower();
        input = input.Replace(" ", "_");
        input = input.Replace("-", "_");

        // remove common unity suffixes
        input = input.Replace("(clone)", "");
        input = input.Replace(".fbx", "");

        return input.Trim();
    }

    private enum TextureType
    {
        Albedo,
        Normal,
        Metallic,
        AO,
        Emission,
        Height
    }
}