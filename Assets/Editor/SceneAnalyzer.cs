using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class SceneAnalyzer : EditorWindow
{
    private int totalTriangles = 0;
    private int totalVertices = 0;
    private int totalMeshes = 0;
    private int totalRenderers = 0;
    private int totalMaterials = 0;
    private int skinnedMeshes = 0;
    private float estimatedTextureMemoryMB = 0f;

    private string performanceRating = "NOT ANALYZED";
    private bool analyzed = false;

    // 🎯 Targets
    private const int TRI_60 = 800000;
    private const int TRI_90 = 500000;

    private const int DC_60 = 500;
    private const int DC_90 = 300;

    private const int TEX_60 = 700;
    private const int TEX_90 = 512;

    [MenuItem("Tools/Scene Analyzer")]
    public static void ShowWindow()
    {
        SceneAnalyzer window = GetWindow<SceneAnalyzer>();
        window.titleContent = new GUIContent("Scene Analyzer");
        window.minSize = new Vector2(380, 500);
    }

    private void OnGUI()
    {
        GUILayout.Space(5);
        GUILayout.Label("Scene Optimization Analyzer", EditorStyles.boldLabel);

        var scene = SceneManager.GetActiveScene();
        string sceneName = scene.IsValid() ? scene.name : "Unknown";

        GUILayout.Label($"Scene: {sceneName}", EditorStyles.helpBox);

        GUILayout.Space(10);

        if (GUILayout.Button("Analyze Scene", GUILayout.Height(30)))
        {
            AnalyzeScene();
            analyzed = true;
        }

        GUILayout.Space(10);

        GUILayout.Label($"Triangles: {totalTriangles}");
        GUILayout.Label($"Renderers (≈ Draw Calls): {totalRenderers}");
        GUILayout.Label($"Meshes: {totalMeshes}");
        GUILayout.Label($"Materials: {totalMaterials}");
        GUILayout.Label($"Skinned Meshes: {skinnedMeshes}");
        GUILayout.Label($"Texture Memory: {estimatedTextureMemoryMB:F2} MB");

        GUILayout.Space(10);
        GUILayout.Label($"Performance Rating: {performanceRating}", EditorStyles.boldLabel);

        GUILayout.Space(15);

        if (GUILayout.Button("Export Report (.txt)", GUILayout.Height(25)))
        {
            ExportReport();
        }

        // ✅ NEW SECTION: TARGET OPTIMIZATION GUIDE
        if (analyzed)
        {
            GUILayout.Space(20);
            GUILayout.Label("🎯 Target Optimization Guide", EditorStyles.boldLabel);

            DrawTarget("Triangles", totalTriangles, TRI_60, TRI_90);
            DrawTarget("Draw Calls", totalRenderers, DC_60, DC_90);
            DrawTarget("Texture Memory (MB)", (int)estimatedTextureMemoryMB, TEX_60, TEX_90);
        }
    }

    void DrawTarget(string label, int current, int target60, int target90)
    {
        int reduce60 = Mathf.Max(0, current - target60);
        int reduce90 = Mathf.Max(0, current - target90);

        GUILayout.Space(5);
        GUILayout.Label($"• {label}", EditorStyles.boldLabel);

        GUILayout.Label($"   Current: {current}");

        GUILayout.Label($"   60 FPS Target: ≤ {target60}  | Reduce: {reduce60}");
        GUILayout.Label($"   90 FPS Target: ≤ {target90}  | Reduce: {reduce90}");
    }

    void AnalyzeScene()
    {
        totalTriangles = 0;
        totalVertices = 0;
        totalMeshes = 0;
        totalRenderers = 0;
        totalMaterials = 0;
        skinnedMeshes = 0;
        estimatedTextureMemoryMB = 0f;

        HashSet<Material> uniqueMaterials = new HashSet<Material>();
        HashSet<Texture> uniqueTextures = new HashSet<Texture>();

        MeshFilter[] meshFilters = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
        foreach (var mf in meshFilters)
        {
            if (mf.sharedMesh == null) continue;

            totalMeshes++;
            totalTriangles += mf.sharedMesh.triangles.Length / 3;
            totalVertices += mf.sharedMesh.vertexCount;
        }

        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        totalRenderers = renderers.Length;

        foreach (var r in renderers)
        {
            foreach (var mat in r.sharedMaterials)
            {
                if (mat == null) continue;
                uniqueMaterials.Add(mat);

                Shader shader = mat.shader;
                int count = ShaderUtil.GetPropertyCount(shader);

                for (int i = 0; i < count; i++)
                {
                    if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        string propName = ShaderUtil.GetPropertyName(shader, i);
                        Texture tex = mat.GetTexture(propName);

                        if (tex != null)
                            uniqueTextures.Add(tex);
                    }
                }
            }
        }

        totalMaterials = uniqueMaterials.Count;

        skinnedMeshes = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None).Length;

        foreach (var tex in uniqueTextures)
        {
            estimatedTextureMemoryMB += EstimateTextureSize(tex);
        }

        EvaluatePerformance();
    }

    float EstimateTextureSize(Texture tex)
    {
        if (tex is Texture2D t2d)
        {
            return (t2d.width * t2d.height * 4f) / (1024f * 1024f);
        }
        return 0f;
    }

    void EvaluatePerformance()
    {
        if (totalTriangles < 500000 && totalRenderers < 300)
        {
            performanceRating = "GOOD";
        }
        else if (totalTriangles < 1000000 && totalRenderers < 600)
        {
            performanceRating = "MODERATE";
        }
        else
        {
            performanceRating = "HEAVY";
        }
    }

    void ExportReport()
    {
        string path = EditorUtility.SaveFilePanel(
            "Save Optimization Report",
            "",
            "Scene_Optimization_Report.txt",
            "txt"
        );

        if (string.IsNullOrEmpty(path)) return;

        var scene = SceneManager.GetActiveScene();
        string sceneName = scene.IsValid() ? scene.name : "Unknown";

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("=== SCENE OPTIMIZATION REPORT ===");
        sb.AppendLine($"Scene: {sceneName}");

        sb.AppendLine($"Triangles: {totalTriangles}");
        sb.AppendLine($"Renderers: {totalRenderers}");
        sb.AppendLine($"Texture Memory: {estimatedTextureMemoryMB:F2} MB");

        sb.AppendLine("\n--- TARGETS ---");
        sb.AppendLine($"60 FPS → Tri ≤ {TRI_60}, DC ≤ {DC_60}, Tex ≤ {TEX_60}");
        sb.AppendLine($"90 FPS → Tri ≤ {TRI_90}, DC ≤ {DC_90}, Tex ≤ {TEX_90}");

        File.WriteAllText(path, sb.ToString());

        Debug.Log("✅ Report Saved: " + path);
    }
}