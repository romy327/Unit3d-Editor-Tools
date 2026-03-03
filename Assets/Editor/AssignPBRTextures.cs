using UnityEngine;
using UnityEditor;
using System.IO;

public class AssignPBRTextures : EditorWindow
{
    private string textureFolderPath = "Assets";
    private enum NamingSource { ObjectName, MeshName, MaterialName }
    private NamingSource namingSource = NamingSource.ObjectName;

    private bool mergeAlbedoAOToEmission = true; // ✅ New option toggle
    private string mergedSuffix = "_shadow_merged";
    private float aoOpacity = 1.0f;
    private int selectedBlendMode = 1; // Default Multiply
    private readonly string[] blendModes = { "Soft Light", "Multiply", "Overlay" };

    [MenuItem("Tools/Assign PBR Textures")]
    public static void ShowWindow()
    {
        GetWindow<AssignPBRTextures>("Assign PBR Textures");
    }

    private void OnGUI()
    {
        GUIStyle customLabelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = Color.cyan },
            fontSize = 18
        };

        GUILayout.Space(10);
        GUILayout.Label("Auto Assign PBR Textures V4", customLabelStyle);
        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Texture Folder", GUILayout.Width(100));
        textureFolderPath = EditorGUILayout.TextField(textureFolderPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string selected = EditorUtility.OpenFolderPanel("Select Texture Folder", "Assets", "");
            if (!string.IsNullOrEmpty(selected) && selected.StartsWith(Application.dataPath))
            {
                textureFolderPath = "Assets" + selected.Substring(Application.dataPath.Length);
            }
            else
            {
                Debug.LogWarning("Please select a folder inside the Assets directory.");
            }
        }
        EditorGUILayout.EndHorizontal();

        namingSource = (NamingSource)EditorGUILayout.EnumPopup("Texture Match By", namingSource);

        GUILayout.Space(10);
        mergeAlbedoAOToEmission = EditorGUILayout.Toggle("Merge Albedo+AO → Emission", mergeAlbedoAOToEmission);

        if (mergeAlbedoAOToEmission)
        {
            mergedSuffix = EditorGUILayout.TextField("Merged Suffix", mergedSuffix);
            aoOpacity = EditorGUILayout.Slider("AO Opacity", aoOpacity, 0, 1);
            selectedBlendMode = EditorGUILayout.Popup("Blend Mode", selectedBlendMode, blendModes);
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Assign Textures to Selected"))
        {
            AssignTexturesToSelected(textureFolderPath, namingSource);
        }
    }

    private static void AssignTexturesToSelected(string folderPath, NamingSource source)
    {
        Object[] selections = Selection.objects;

        if (selections.Length == 0)
        {
            Debug.LogWarning("Please select one or more GameObjects or Materials.");
            return;
        }

        foreach (Object obj in selections)
        {
            Material targetMaterial = null;
            string baseName = "";

            if (obj is GameObject go)
            {
                Renderer renderer = go.GetComponent<Renderer>();
                if (!renderer)
                {
                    Debug.LogWarning($"GameObject '{go.name}' has no Renderer component.");
                    continue;
                }

                switch (source)
                {
                    case NamingSource.ObjectName:
                        baseName = go.name;
                        break;
                    case NamingSource.MeshName:
                        baseName = renderer is SkinnedMeshRenderer smr && smr.sharedMesh != null
                            ? smr.sharedMesh.name
                            : (renderer.GetComponent<MeshFilter>()?.sharedMesh?.name ?? go.name);
                        break;
                    case NamingSource.MaterialName:
                        baseName = renderer.sharedMaterial != null ? renderer.sharedMaterial.name : go.name;
                        break;
                }

                targetMaterial = renderer.sharedMaterial;
                if (!targetMaterial)
                {
                    targetMaterial = new Material(Shader.Find("Standard"));
                    renderer.sharedMaterial = targetMaterial;
                    Debug.Log($"Created new standard material for {go.name}");
                }
            }
            else if (obj is Material mat)
            {
                baseName = mat.name;
                targetMaterial = mat;
            }

            if (targetMaterial != null && !string.IsNullOrEmpty(baseName))
            {
                AssignTexturesToMaterial(targetMaterial, baseName, folderPath);
            }
        }
    }

    private static void AssignTexturesToMaterial(Material material, string baseName, string folderPath)
    {
        Texture2D albedoTex = null;
        Texture2D aoTex = null;
        Texture2D emissionTex = null;

        string[] textureTypes = new string[] { "AlbedoTransparency", "AO", "MetallicSmoothness", "Normal", "Emmision" };

        foreach (string type in textureTypes)
        {
            string[] possibleNames = new string[] { $"{baseName}__{type}", $"{baseName}_{type}", $"{baseName}{type}" };
            Texture2D tex = FindTexture(possibleNames, folderPath);

            switch (type)
            {
                case "AlbedoTransparency":
                    if (tex != null)
                    {
                        albedoTex = tex;
                        material.SetTexture("_MainTex", tex);
                    }
                    break;
                case "AO":
                    if (tex != null)
                    {
                        aoTex = tex;
                        material.SetTexture("_OcclusionMap", tex);
                        material.EnableKeyword("_OCCLUSIONMAP");
                    }
                    break;
                case "MetallicSmoothness":
                    if (tex != null)
                    {
                        material.SetTexture("_MetallicGlossMap", tex);
                        material.EnableKeyword("_METALLICGLOSSMAP");
                    }
                    break;
                case "Normal":
                    if (tex != null)
                    {
                        material.SetTexture("_BumpMap", tex);
                        material.EnableKeyword("_NORMALMAP");
                    }
                    break;
                case "Emmision":
                    emissionTex = tex; // assign later, merging may override
                    break;
            }
        }

        // ✅ Merge Albedo+AO → Emission if option enabled
        if (window.mergeAlbedoAOToEmission && albedoTex != null && aoTex != null)
        {
            Texture2D merged = MergeTextures(albedoTex, aoTex, window.aoOpacity, window.selectedBlendMode);
            string savePath = SaveMergedTexture(merged, albedoTex, "", window.mergedSuffix);
            emissionTex = AssetDatabase.LoadAssetAtPath<Texture2D>(savePath);
        }

        if (emissionTex != null)
        {
            material.SetTexture("_EmissionMap", emissionTex);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.SetColor("_EmissionColor", Color.white);
        }

        EditorUtility.SetDirty(material);
        Debug.Log($"✅ Finished assigning textures to: {material.name}");
    }

    private static Texture2D FindTexture(string[] names, string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            foreach (string name in names)
            {
                if (fileName.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
            }
        }
        return null;
    }

    // --- Merge logic from MergeTexturesEditor ---
    private static Texture2D MergeTextures(Texture2D albedo, Texture2D ao, float opacity, int blendMode)
    {
        int width = albedo.width;
        int height = albedo.height;
        if (ao.width != width || ao.height != height)
        {
            Debug.LogError("Albedo and AO textures must have the same dimensions.");
            return null;
        }

        Texture2D merged = new Texture2D(width, height);
        Color[] albedoPixels = albedo.GetPixels();
        Color[] aoPixels = ao.GetPixels();
        Color[] mergedPixels = new Color[albedoPixels.Length];

        for (int i = 0; i < albedoPixels.Length; i++)
        {
            Color a = albedoPixels[i];
            Color b = aoPixels[i];
            float r = Blend(a.r, b.r, blendMode, opacity);
            float g = Blend(a.g, b.g, blendMode, opacity);
            float bl = Blend(a.b, b.b, blendMode, opacity);
            mergedPixels[i] = new Color(r, g, bl, a.a);
        }
        merged.SetPixels(mergedPixels);
        merged.Apply();
        return merged;
    }

    private static float Blend(float baseValue, float blendValue, int mode, float opacity)
    {
        float blended = mode switch
        {
            0 => (blendValue < 0.5f) ? (2 * baseValue * blendValue + baseValue * baseValue * (1 - 2 * blendValue)) : (2 * baseValue * (1 - blendValue) + Mathf.Sqrt(baseValue) * (2 * blendValue - 1)),
            1 => baseValue * blendValue,
            2 => (baseValue < 0.5f) ? (2 * baseValue * blendValue) : (1 - 2 * (1 - baseValue) * (1 - blendValue)),
            _ => baseValue
        };
        return Mathf.Lerp(baseValue, blended, opacity);
    }

    private static string SaveMergedTexture(Texture2D tex, Texture2D source, string prefix, string suffix)
    {
        string path = AssetDatabase.GetAssetPath(source);
        string dir = Path.GetDirectoryName(path);
        string name = Path.GetFileNameWithoutExtension(path);
        string newFile = $"{prefix}{name}{suffix}.jpg";
        string newPath = Path.Combine(dir, newFile);
        File.WriteAllBytes(newPath, tex.EncodeToJPG());
        AssetDatabase.Refresh();
        Debug.Log($"Merged texture saved at: {newPath}");
        return newPath;
    }

    // ✅ Store settings inside EditorWindow (so static functions can access)
    private static AssignPBRTextures window => (AssignPBRTextures)GetWindow(typeof(AssignPBRTextures));
}
