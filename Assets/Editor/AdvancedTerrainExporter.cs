using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Text;
using System.Collections.Generic;

// Advanced Terrain Exporter
// Features:
// - OBJ Export (optimized with LOD sampling)
// - JLB Binary Export
// - Chunked Export for large terrains
// - Progress bar
// - Optional FBX export (requires Unity FBX Exporter package)

public class AdvancedTerrainExporter : EditorWindow
{
    private Terrain terrain;
    private string fileName = "TerrainExport";

    private enum ExportFormat { OBJ, JLB, FBX }
    private ExportFormat format = ExportFormat.OBJ;

    private int lodStep = 1; // sampling step
    private bool chunkExport = false;
    private int chunkSize = 256;

    [MenuItem("Tools/Advanced Terrain Exporter")]
    public static void ShowWindow()
    {
        GetWindow<AdvancedTerrainExporter>("Terrain Exporter Pro");
    }

    private void OnGUI()
    {
        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);
        fileName = EditorGUILayout.TextField("File Name", fileName);
        format = (ExportFormat)EditorGUILayout.EnumPopup("Format", format);

        GUILayout.Space(10);
        GUILayout.Label("Optimization", EditorStyles.boldLabel);
        lodStep = EditorGUILayout.IntSlider("LOD Step", lodStep, 1, 16);

        chunkExport = EditorGUILayout.Toggle("Chunk Export", chunkExport);
        if (chunkExport)
            chunkSize = EditorGUILayout.IntField("Chunk Size", chunkSize);

        GUILayout.Space(10);

        if (GUILayout.Button("Export Terrain"))
        {
            if (terrain == null)
            {
                Debug.LogError("No terrain selected");
                return;
            }

            string path = EditorUtility.SaveFolderPanel("Export Terrain", "", fileName);
            if (!string.IsNullOrEmpty(path))
            {
                Export(path);
            }
        }
    }

    void Export(string folder)
    {
        TerrainData td = terrain.terrainData;

        if (chunkExport)
        {
            ExportChunks(td, folder);
        }
        else
        {
            ExportSingle(td, folder + "/" + fileName);
        }

        EditorUtility.ClearProgressBar();
        Debug.Log("Export Complete");
    }

    void ExportSingle(TerrainData td, string path)
    {
        if (format == ExportFormat.OBJ)
            ExportOBJ(td, path + ".obj");
        else if (format == ExportFormat.JLB)
            ExportJLB(td, path + ".jlb");
#if UNITY_EDITOR
        else if (format == ExportFormat.FBX)
            ExportFBX(td, path + ".fbx");
#endif
    }

    void ExportChunks(TerrainData td, string folder)
    {
        int res = td.heightmapResolution;
        int chunks = Mathf.CeilToInt(res / (float)chunkSize);

        for (int cy = 0; cy < chunks; cy++)
        {
            for (int cx = 0; cx < chunks; cx++)
            {
                EditorUtility.DisplayProgressBar("Exporting Chunks",
                    $"Chunk {cx},{cy}", (cy * chunks + cx) / (float)(chunks * chunks));

                string path = $"{folder}/{fileName}_{cx}_{cy}";
                ExportOBJChunk(td, path + ".obj", cx, cy);
            }
        }
    }

    void ExportOBJ(TerrainData td, string path)
    {
        ExportOBJChunk(td, path, 0, 0, true);
    }

    void ExportOBJChunk(TerrainData td, string path, int cx, int cy, bool full = false)
    {
        int res = td.heightmapResolution;
        float[,] heights = td.GetHeights(0, 0, res, res);
        Vector3 size = td.size;

        int startX = full ? 0 : cx * chunkSize;
        int startY = full ? 0 : cy * chunkSize;

        int endX = full ? res : Mathf.Min(startX + chunkSize, res);
        int endY = full ? res : Mathf.Min(startY + chunkSize, res);

        StringBuilder sb = new StringBuilder();

        int vertIndex = 1;
        Dictionary<Vector2Int, int> indexMap = new Dictionary<Vector2Int, int>();

        // Vertices
        for (int y = startY; y < endY; y += lodStep)
        {
            for (int x = startX; x < endX; x += lodStep)
            {
                float h = heights[y, x];
                Vector3 v = new Vector3(
                    x / (float)(res - 1) * size.x,
                    h * size.y,
                    y / (float)(res - 1) * size.z
                );

                sb.AppendLine($"v {v.x} {v.y} {v.z}");
                sb.AppendLine($"vt {x / (float)(res - 1)} {y / (float)(res - 1)}");

                indexMap[new Vector2Int(x, y)] = vertIndex++;
            }
        }

        // Faces
        for (int y = startY; y < endY - lodStep; y += lodStep)
        {
            for (int x = startX; x < endX - lodStep; x += lodStep)
            {
                var a = new Vector2Int(x, y);
                var b = new Vector2Int(x + lodStep, y);
                var c = new Vector2Int(x, y + lodStep);
                var d = new Vector2Int(x + lodStep, y + lodStep);

                if (!indexMap.ContainsKey(a) || !indexMap.ContainsKey(b) ||
                    !indexMap.ContainsKey(c) || !indexMap.ContainsKey(d)) continue;

                int i1 = indexMap[a];
                int i2 = indexMap[b];
                int i3 = indexMap[c];
                int i4 = indexMap[d];

                sb.AppendLine($"f {i1}/{i1} {i3}/{i3} {i2}/{i2}");
                sb.AppendLine($"f {i2}/{i2} {i3}/{i3} {i4}/{i4}");
            }
        }

        File.WriteAllText(path, sb.ToString());
    }

    void ExportJLB(TerrainData td, string path)
    {
        int res = td.heightmapResolution;
        float[,] heights = td.GetHeights(0, 0, res, res);
        Vector3 size = td.size;

        using (BinaryWriter bw = new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            bw.Write(res);
            bw.Write(size.x);
            bw.Write(size.y);
            bw.Write(size.z);

            for (int y = 0; y < res; y += lodStep)
                for (int x = 0; x < res; x += lodStep)
                    bw.Write(heights[y, x]);
        }
    }

#if UNITY_EDITOR
    void ExportFBX(TerrainData td, string path)
    {
        Debug.LogWarning("FBX Export requires Unity FBX Exporter package. Converting to mesh...");

        GameObject temp = new GameObject("TempTerrainMesh");
        MeshFilter mf = temp.AddComponent<MeshFilter>();
        MeshRenderer mr = temp.AddComponent<MeshRenderer>();

        mf.sharedMesh = GenerateMesh(td);

        UnityEditor.Formats.Fbx.Exporter.ModelExporter.ExportObject(path, temp);
        DestroyImmediate(temp);
    }
#endif

    Mesh GenerateMesh(TerrainData td)
    {
        int res = td.heightmapResolution;
        float[,] heights = td.GetHeights(0, 0, res, res);
        Vector3 size = td.size;

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        for (int y = 0; y < res; y += lodStep)
        {
            for (int x = 0; x < res; x += lodStep)
            {
                verts.Add(new Vector3(
                    x / (float)(res - 1) * size.x,
                    heights[y, x] * size.y,
                    y / (float)(res - 1) * size.z
                ));
            }
        }

        int w = res / lodStep;

        for (int y = 0; y < w - 1; y++)
        {
            for (int x = 0; x < w - 1; x++)
            {
                int i = y * w + x;

                tris.Add(i);
                tris.Add(i + w);
                tris.Add(i + 1);

                tris.Add(i + 1);
                tris.Add(i + w);
                tris.Add(i + w + 1);
            }
        }

        Mesh m = new Mesh();
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.SetVertices(verts);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();

        return m;
    }
}
