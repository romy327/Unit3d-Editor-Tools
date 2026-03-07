using UnityEngine;//Auther    : RomyRMichael
using UnityEditor;//Portfolio : https://romyrmichael.c1.biz/
using System.IO;
using System.Collections.Generic;

public class AssignPBRTexturesV5 : EditorWindow
{
    private string textureFolderPath = "Assets";

    private enum NamingSource { ObjectName, MeshName, MaterialName }
    private NamingSource namingSource = NamingSource.ObjectName;

    private bool mergeAlbedoAOToEmission = false;
    private string mergedSuffix = "_AO_merged";
    private float aoOpacity = 1.0f;

    private int selectedBlendMode = 1;
    private readonly string[] blendModes = { "Soft Light", "Multiply", "Overlay" };

    private static Dictionary<string, Texture2D> textureCache;

    [MenuItem("Tools/Auto Assign PBR Textures PRO")]
    static void Init()
    {
        GetWindow<AssignPBRTexturesV5>("PBR Auto Assign PRO");
    }

    void OnGUI()
    {
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 18;
        titleStyle.normal.textColor = new Color(0.3f, 0.9f, 1f);
        titleStyle.alignment = TextAnchor.MiddleCenter;

        GUILayout.Space(10);
        GUILayout.Label("AUTO ASSIGN PBR TEXTURES PRO", titleStyle);
        GUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");

        GUILayout.Label("Texture Source", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        textureFolderPath = EditorGUILayout.TextField("Texture Folder", textureFolderPath);

        if (GUILayout.Button("Browse", GUILayout.Width(70)))
        {
            string selected = EditorUtility.OpenFolderPanel("Select Texture Folder", "Assets", "");

            if (selected.StartsWith(Application.dataPath))
            {
                textureFolderPath = "Assets" + selected.Substring(Application.dataPath.Length);
            }
            else
            {
                Debug.LogWarning("Folder must be inside Assets.");
            }
        }

        EditorGUILayout.EndHorizontal();

        namingSource = (NamingSource)EditorGUILayout.EnumPopup("Texture Match By", namingSource);

        EditorGUILayout.EndVertical();

        GUILayout.Space(8);

        EditorGUILayout.BeginVertical("box");

        GUILayout.Label("Emission Generator (AO + Albedo)", EditorStyles.boldLabel);

        mergeAlbedoAOToEmission = EditorGUILayout.Toggle("Merge Albedo + AO → Emission", mergeAlbedoAOToEmission, GUILayout.Width(100));

        if (mergeAlbedoAOToEmission)
        {
            mergedSuffix = EditorGUILayout.TextField("Merged Suffix", mergedSuffix);
            aoOpacity = EditorGUILayout.Slider("AO Opacity", aoOpacity, 0f, 1f);
            selectedBlendMode = EditorGUILayout.Popup("Blend Mode", selectedBlendMode, blendModes);
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(12);

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);

        if (GUILayout.Button("ASSIGN TEXTURES TO SELECTED OBJECTS", GUILayout.Height(40)))
        {
            BuildTextureCache(textureFolderPath);
            AssignTexturesToSelected();
        }

        GUI.backgroundColor = Color.white;

        GUILayout.Space(8);

        EditorGUILayout.HelpBox(
            "Select GameObjects or Materials in the hierarchy or project window.\n" +
            "Textures will be automatically assigned based on name matching.",
            MessageType.Info
        );

        GUILayout.Space(5);

        EditorGUILayout.LabelField("Supported Maps:",
            "AlbedoTransparency, AO, Normal, MetallicSmoothness, Emmision");
    }

    static void BuildTextureCache(string folder)
    {
        textureCache = new Dictionary<string, Texture2D>();

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });

        foreach (string g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            string name = Path.GetFileNameWithoutExtension(path).ToLower();

            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);

            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            textureCache[name] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        Debug.Log("Texture Cache Built : " + textureCache.Count);
    }

    void AssignTexturesToSelected()
    {
        Object[] selections = Selection.objects;

        foreach (Object obj in selections)
        {
            if (obj is GameObject go)
            {
                Renderer renderer = go.GetComponent<Renderer>();

                if (!renderer)
                    continue;

                foreach (Material material in renderer.sharedMaterials)
                {
                    if (!material) continue;

                    string baseName = GetBaseName(go, renderer, material);
                    AssignTexturesToMaterial(material, baseName);
                }
            }

            if (obj is Material mat)
            {
                AssignTexturesToMaterial(mat, mat.name);
            }
        }
    }

    string GetBaseName(GameObject go, Renderer r, Material mat)
    {
        switch (namingSource)
        {
            case NamingSource.MeshName:

                if (r is SkinnedMeshRenderer smr)
                    return smr.sharedMesh.name;

                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf) return mf.sharedMesh.name;

                return go.name;

            case NamingSource.MaterialName:
                return mat.name;

            default:
                return go.name;
        }
    }

    void AssignTexturesToMaterial(Material mat, string baseName)
    {
        baseName = baseName.ToLower();

        Texture2D albedo = FindTexture(baseName, "albedotransparency");
        Texture2D ao = FindTexture(baseName, "ao");
        Texture2D normal = FindTexture(baseName, "normal");
        Texture2D metallic = FindTexture(baseName, "metallicsmoothness");
        Texture2D emission = FindTexture(baseName, "emmision");

        string shader = mat.shader.name;

        bool isURP = shader.Contains("Universal");
        bool isHDRP = shader.Contains("HDRP");

        if (albedo)
        {
            if (isURP || isHDRP)
                mat.SetTexture("_BaseMap", albedo);
            else
                mat.SetTexture("_MainTex", albedo);
        }

        if (normal)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
        }

        if (metallic)
        {
            mat.SetTexture("_MetallicGlossMap", metallic);
            mat.EnableKeyword("_METALLICGLOSSMAP");
        }

        if (ao)
        {
            mat.SetTexture("_OcclusionMap", ao);
            mat.EnableKeyword("_OCCLUSIONMAP");
        }

        if (mergeAlbedoAOToEmission && albedo && ao)
        {
            Texture2D merged = MergeTextures(albedo, ao);

            string save = SaveMergedTexture(merged, albedo);
            emission = AssetDatabase.LoadAssetAtPath<Texture2D>(save);
        }

        // Updated Emission Logic
        if (emission)
        {
            mat.SetTexture("_EmissionMap", emission);
            mat.SetColor("_EmissionColor", Color.white);

            // This enables the emission keyword for the shader
            mat.EnableKeyword("_EMISSION");

            // This sets the Global Illumination flags so Unity knows the emission is active
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }
        else
        {
            mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }

        EditorUtility.SetDirty(mat);

        Debug.Log("Assigned textures → " + mat.name);
    }

    Texture2D FindTexture(string baseName, string type)
    {
        string key1 = $"{baseName}__{type}";
        string key2 = $"{baseName}_{type}";
        string key3 = $"{baseName}{type}";

        if (textureCache.TryGetValue(key1, out Texture2D t)) return t;
        if (textureCache.TryGetValue(key2, out t)) return t;
        if (textureCache.TryGetValue(key3, out t)) return t;

        return null;
    }

    Texture2D MergeTextures(Texture2D albedo, Texture2D ao)
    {
        int w = albedo.width;
        int h = albedo.height;

        Texture2D merged = new Texture2D(w, h);

        Color[] a = albedo.GetPixels();
        Color[] b = ao.GetPixels();
        Color[] r = new Color[a.Length];

        for (int i = 0; i < a.Length; i++)
        {
            // Blend logic - simplified to Lerp based on your request
            Color c = Color.Lerp(a[i], a[i] * b[i], aoOpacity);
            r[i] = new Color(c.r, c.g, c.b, a[i].a);
        }

        merged.SetPixels(r);
        merged.Apply();

        return merged;
    }

    string SaveMergedTexture(Texture2D tex, Texture2D source)
    {
        string path = AssetDatabase.GetAssetPath(source);
        string dir = Path.GetDirectoryName(path);
        string name = Path.GetFileNameWithoutExtension(path);

        string newFile = name + mergedSuffix + ".jpg";
        string newPath = Path.Combine(dir, newFile);

        File.WriteAllBytes(newPath, tex.EncodeToJPG());

        AssetDatabase.Refresh();

        return newPath;
    }
}