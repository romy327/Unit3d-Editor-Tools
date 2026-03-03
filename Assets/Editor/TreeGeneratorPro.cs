using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class TreeGeneratorPro : EditorWindow
{
    // Main parameters
    float trunkHeight = 8f;
    float trunkBaseRadius = 0.5f;
    int trunkSegments = 24;          // length subdivisions along trunk
    int radialSegments = 10;         // circle resolution
    float trunkCurveStrength = 0.6f; // how curvy the trunk is
    int branchLevels = 3;            // recursive branch depth
    int branchesPerLevel = 4;        // per node average
    float branchLengthFactor = 0.55f;
    float branchRadiusFactor = 0.45f;
    float branchAngleMin = 20f;
    float branchAngleMax = 60f;
    float branchNoise = 0.8f;

    // leaves
    int leafCountPerCluster = 30;
    float leafSize = 0.25f;
    float leafClusterRadius = 0.6f;

    // misc
    int seed = 12345;
    bool combineMeshes = true;
    bool saveAsPrefab = true;
    string saveFolder = "Assets/GeneratedTrees";
    Material barkMaterial;
    Material leafMaterial;

    [MenuItem("Tools/TreeGeneratorPro")]
    public static void ShowWindow() => GetWindow<TreeGeneratorPro>("Tree Generator Pro");

    void OnGUI()
    {
        GUILayout.Label("Realistic Procedural Tree Generator", EditorStyles.boldLabel);

        EditorGUILayout.Space();
        GUILayout.Label("Trunk", EditorStyles.miniBoldLabel);
        trunkHeight = EditorGUILayout.Slider("Height", trunkHeight, 1f, 40f);
        trunkBaseRadius = EditorGUILayout.Slider("Base Radius", trunkBaseRadius, 0.05f, 2f);
        trunkSegments = EditorGUILayout.IntSlider("Trunk Segments", trunkSegments, 6, 64);
        radialSegments = EditorGUILayout.IntSlider("Radial Segments", radialSegments, 6, 24);
        trunkCurveStrength = EditorGUILayout.Slider("Curve Strength", trunkCurveStrength, 0f, 2f);

        EditorGUILayout.Space();
        GUILayout.Label("Branching", EditorStyles.miniBoldLabel);
        branchLevels = EditorGUILayout.IntSlider("Branch Levels", branchLevels, 0, 5);
        branchesPerLevel = EditorGUILayout.IntSlider("Branches Per Level (avg)", branchesPerLevel, 0, 10);
        branchLengthFactor = EditorGUILayout.Slider("Branch Length Factor", branchLengthFactor, 0.1f, 0.95f);
        branchRadiusFactor = EditorGUILayout.Slider("Branch Radius Factor", branchRadiusFactor, 0.1f, 0.9f);
        branchAngleMin = EditorGUILayout.Slider("Min Branch Angle", branchAngleMin, 0f, 90f);
        branchAngleMax = EditorGUILayout.Slider("Max Branch Angle", branchAngleMax, branchAngleMin, 90f);
        branchNoise = EditorGUILayout.Slider("Branch Noise", branchNoise, 0f, 2f);

        EditorGUILayout.Space();
        GUILayout.Label("Leaves", EditorStyles.miniBoldLabel);
        leafCountPerCluster = EditorGUILayout.IntSlider("Leaves per cluster", leafCountPerCluster, 1, 200);
        leafSize = EditorGUILayout.Slider("Leaf Size", leafSize, 0.01f, 1f);
        leafClusterRadius = EditorGUILayout.Slider("Leaf Cluster Radius", leafClusterRadius, 0.05f, 2f);

        EditorGUILayout.Space();
        seed = EditorGUILayout.IntField("Random Seed", seed);
        combineMeshes = EditorGUILayout.Toggle("Combine Meshes", combineMeshes);
        saveAsPrefab = EditorGUILayout.Toggle("Save As Prefab", saveAsPrefab);
        saveFolder = EditorGUILayout.TextField("Prefab Save Folder", saveFolder);

        EditorGUILayout.Space();
        barkMaterial = (Material)EditorGUILayout.ObjectField("Bark Material", barkMaterial, typeof(Material), false);
        leafMaterial = (Material)EditorGUILayout.ObjectField("Leaf Material", leafMaterial, typeof(Material), false);

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Realistic Tree", GUILayout.Height(40)))
        {
            Generate();
        }

        if (GUILayout.Button("Help / Notes"))
        {
            ShowHelp();
        }
    }

    void ShowHelp()
    {
        EditorUtility.DisplayDialog("Notes",
            "This tool creates a realistic procedural tree mesh using swept tubes for trunk/branches\n" +
            "- You can tune branchLevels and branchesPerLevel for complexity.\n" +
            "- Assign materials for better visuals.\n" +
            "- The tool combines generated meshes into one mesh when 'Combine Meshes' is ON.\n" +
            "- Prefabs are saved to the specified folder.\n\n" +
            "Tip: Use a tiling bark texture and a cutout leaf texture (alpha) for best results.", "OK");
    }

    // -------------------------
    // Generation pipeline
    // -------------------------
    void Generate()
    {
        Random.InitState(seed);

        // Root object
        GameObject root = new GameObject("ProceduralTree");
        Undo.RegisterCreatedObjectUndo(root, "Create ProceduralTree");

        // Create main trunk spline
        List<Vector3> trunkPoints = GenerateCurvedSpline(Vector3.zero, Vector3.up * trunkHeight, trunkSegments, trunkCurveStrength);
        List<float> trunkRadii = new List<float>();
        for (int i = 0; i < trunkPoints.Count; i++)
        {
            float t = (float)i / (trunkPoints.Count - 1);
            float radius = Mathf.Lerp(trunkBaseRadius, trunkBaseRadius * 0.05f, t * t); // taper
            trunkRadii.Add(radius);
        }

        Mesh trunkMesh = BuildTubeMesh(trunkPoints, trunkRadii, radialSegments);
        GameObject trunkGO = new GameObject("Trunk");
        trunkGO.transform.SetParent(root.transform, false);
        MeshFilter tmf = trunkGO.AddComponent<MeshFilter>();
        MeshRenderer tmr = trunkGO.AddComponent<MeshRenderer>();
        tmf.sharedMesh = trunkMesh;
        if (barkMaterial) tmr.sharedMaterial = barkMaterial;
        else tmr.sharedMaterial = new Material(Shader.Find("Standard"));

        // Collect generated parts for later combination
        List<MeshFilter> generatedFilters = new List<MeshFilter> { tmf };

        // Generate branches recursively from trunk points
        // pick candidate branch attach points along trunk (excluding bottom and top extremes)
        List<int> attachIndices = Enumerable.Range(1, trunkPoints.Count - 2).ToList();
        int approxBranches = Mathf.Max(1, branchesPerLevel);
        float attachSpacing = Mathf.Max(1, attachIndices.Count / (float)approxBranches);

        GenerateBranchesRecursive(root.transform, trunkPoints, trunkRadii, 1, branchLevels, attachIndices, generatedFilters, trunkPoints, trunkRadii);

        // After branches and leaves are created, optionally combine meshes
        if (combineMeshes)
            CombineIntoSingle(root, generatedFilters.ToArray());

        // Save prefab
        if (saveAsPrefab)
        {
            if (!System.IO.Directory.Exists(saveFolder))
                System.IO.Directory.CreateDirectory(saveFolder);

            string prefabPath = AssetDatabase.GenerateUniqueAssetPath(saveFolder + "/ProceduralTree.prefab");
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"Saved prefab: {prefabPath}");
        }

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        Debug.Log("Realistic tree generated.");
    }

    // Recursive branching that traverses levels and spawns branches at various points
    void GenerateBranchesRecursive(Transform root, List<Vector3> baseSplinePoints, List<float> baseRadii, int currentLevel, int maxLevel, List<int> attachIndices, List<MeshFilter> generatedFilters, List<Vector3> trunkPoints, List<float> trunkRadii)
    {
        if (currentLevel > maxLevel) return;

        // For each attach index, probabilistically spawn some branches
        int count = attachIndices.Count;
        int avgBranches = Mathf.Max(1, branchesPerLevel);
        float spawnProb = Mathf.Clamp01(avgBranches / (float)count);

        for (int i = 0; i < attachIndices.Count; i++)
        {
            if (Random.value > spawnProb && currentLevel != 1) // ensure level 1 gets some branches
                continue;

            int idx = attachIndices[i];
            Vector3 attachPoint = trunkPoints[idx];

            // Direction: roughly outwards from trunk + upward tilt
            Vector3 tangent = Vector3.zero;
            if (idx < trunkPoints.Count - 1) tangent = (trunkPoints[idx + 1] - trunkPoints[idx]).normalized;
            else tangent = (trunkPoints[idx] - trunkPoints[idx - 1]).normalized;

            // choose an outward direction
            Vector3 radial = Vector3.Cross(tangent, Vector3.right);
            if (radial.magnitude < 0.001f) radial = Vector3.Cross(tangent, Vector3.forward);
            radial.Normalize();

            // Random rotation around tangent
            float yaw = Random.Range(0f, 360f);
            Quaternion rot = Quaternion.AngleAxis(yaw, tangent);
            radial = rot * radial;

            float angle = Random.Range(branchAngleMin, branchAngleMax);
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.Cross(radial, tangent)) * radial;
            dir = (dir + tangent * 0.5f).normalized;

            float parentRadius = trunkRadii[Mathf.Clamp(idx, 0, trunkRadii.Count - 1)];
            float branchLength = trunkHeight * Mathf.Pow(branchLengthFactor, currentLevel) * Random.Range(0.8f, 1.2f);
            float branchRadius = parentRadius * Mathf.Pow(branchRadiusFactor, currentLevel);

            // create branch spline
            List<Vector3> branchSpline = GenerateBranchSpline(attachPoint, dir, branchLength, Mathf.Max(3, trunkSegments / 4), branchNoise);
            List<float> branchRadii = new List<float>();
            for (int r = 0; r < branchSpline.Count; r++)
            {
                float t = (float)r / (branchSpline.Count - 1);
                branchRadii.Add(Mathf.Lerp(branchRadius, branchRadius * 0.03f, t * t));
            }

            Mesh branchMesh = BuildTubeMesh(branchSpline, branchRadii, Mathf.Max(6, radialSegments - 2));
            GameObject bgo = new GameObject("Branch_L" + currentLevel);
            bgo.transform.SetParent(root, true);
            MeshFilter bmf = bgo.AddComponent<MeshFilter>();
            MeshRenderer bmr = bgo.AddComponent<MeshRenderer>();
            bmf.sharedMesh = branchMesh;
            bmr.sharedMaterial = (barkMaterial ? barkMaterial : new Material(Shader.Find("Standard")));
            generatedFilters.Add(bmf);

            // Create leaf cluster at end of branch
            Vector3 clusterCenter = branchSpline.Last();
            CreateLeafCluster(clusterCenter, bgo.transform, leafCountPerCluster, leafSize * Random.Range(0.8f, 1.2f), leafClusterRadius * Random.Range(0.7f, 1.3f), generatedFilters);

            // Recurse: create child attach indices along this branch spline for next level
            if (currentLevel < maxLevel)
            {
                List<int> childAttachIndices = new List<int>();
                int available = branchSpline.Count;
                // exclude the base and tip; choose a few positions
                for (int p = 1; p < available - 1; p++)
                    childAttachIndices.Add(p);
                GenerateBranchesRecursive(root, branchSpline, branchRadii, currentLevel + 1, maxLevel, childAttachIndices, generatedFilters, branchSpline, branchRadii);
            }
        }
    }

    // -------------------------
    // Spline and mesh builders
    // -------------------------
    List<Vector3> GenerateCurvedSpline(Vector3 start, Vector3 end, int segments, float curveStrength)
    {
        List<Vector3> pts = new List<Vector3>();
        Vector3 dir = (end - start);
        float len = dir.magnitude;
        dir.Normalize();

        // Simple cubic Bezier control points
        Vector3 p0 = start;
        Vector3 p3 = end;

        Vector3 up = Vector3.up;
        Vector3 p1 = p0 + up * len * 0.25f + Random.onUnitSphere * (curveStrength * len * 0.05f);
        Vector3 p2 = p3 - up * len * 0.25f + Random.onUnitSphere * (curveStrength * len * 0.05f);

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 p = BezierCubic(p0, p1, p2, p3, t);
            // add subtle noise for realism
            p += Random.onUnitSphere * (curveStrength * len * 0.02f * (1f - Mathf.Abs(0.5f - t) * 2f));
            pts.Add(p);
        }

        return pts;
    }

    List<Vector3> GenerateBranchSpline(Vector3 start, Vector3 direction, float length, int segments, float noise)
    {
        List<Vector3> pts = new List<Vector3>();
        Vector3 p0 = start;
        Vector3 p3 = start + direction.normalized * length;

        Vector3 p1 = p0 + direction.normalized * length * 0.25f + Random.onUnitSphere * (noise * length * 0.05f);
        Vector3 p2 = p3 - direction.normalized * length * 0.15f + Random.onUnitSphere * (noise * length * 0.05f);

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 p = BezierCubic(p0, p1, p2, p3, t);
            p += Random.onUnitSphere * (noise * length * 0.01f * Mathf.Sin(t * Mathf.PI)); // taper noise
            pts.Add(p);
        }
        return pts;
    }

    static Vector3 BezierCubic(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        float u = 1 - t;
        return u * u * u * a + 3 * u * u * t * b + 3 * u * t * t * c + t * t * t * d;
    }

    Mesh BuildTubeMesh(List<Vector3> path, List<float> radii, int radialSeg)
    {
        Mesh mesh = new Mesh();
        int rings = path.Count;
        int vertsPerRing = radialSeg + 1; // close ring

        Vector3[] vertices = new Vector3[rings * vertsPerRing];
        Vector3[] normals = new Vector3[vertices.Length];
        Vector2[] uvs = new Vector2[vertices.Length];
        List<int> triangles = new List<int>();

        for (int i = 0; i < rings; i++)
        {
            Vector3 center = path[i];
            // compute tangent
            Vector3 tangent;
            if (i < rings - 1) tangent = (path[i + 1] - path[i]).normalized;
            else tangent = (path[i] - path[i - 1]).normalized;

            // Orientation basis (normal & binormal)
            Vector3 n = Vector3.Cross(tangent, Vector3.up);
            if (n.magnitude < 0.001f) n = Vector3.Cross(tangent, Vector3.right);
            n.Normalize();
            Vector3 b = Vector3.Cross(tangent, n).normalized;

            float radius = radii[i];

            for (int r = 0; r < vertsPerRing; r++)
            {
                float ang = (r / (float)(radialSeg)) * Mathf.PI * 2f;
                Vector3 circ = Mathf.Cos(ang) * n + Mathf.Sin(ang) * b;
                int idx = i * vertsPerRing + r;
                vertices[idx] = center + circ * radius;
                normals[idx] = circ.normalized;
                uvs[idx] = new Vector2(r / (float)radialSeg, i / (float)(rings - 1));
            }
        }

        // triangles between rings
        for (int i = 0; i < rings - 1; i++)
        {
            for (int r = 0; r < radialSeg; r++)
            {
                int i0 = i * vertsPerRing + r;
                int i1 = i * vertsPerRing + r + 1;
                int i2 = (i + 1) * vertsPerRing + r;
                int i3 = (i + 1) * vertsPerRing + r + 1;

                triangles.Add(i0);
                triangles.Add(i2);
                triangles.Add(i1);

                triangles.Add(i1);
                triangles.Add(i2);
                triangles.Add(i3);
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }

    // Creates a bunch of leaf quads as child meshes; adds MeshFilters to generatedFilters
    void CreateLeafCluster(Vector3 center, Transform parent, int leafCount, float size, float radius, List<MeshFilter> generatedFilters)
    {
        // Build a single quad mesh to reuse
        Mesh quad = BuildQuadMesh();

        for (int i = 0; i < leafCount; i++)
        {
            Vector3 offset = Random.onUnitSphere * (radius * Random.Range(0.2f, 1f));
            // bias toward top hemisphere
            if (offset.y < -radius * 0.1f) offset.y = Mathf.Abs(offset.y);

            Vector3 pos = center + offset;
            Quaternion rot = Quaternion.LookRotation((Random.onUnitSphere + Vector3.up * 0.3f).normalized);
            float s = size * Random.Range(0.7f, 1.3f);

            GameObject leaf = new GameObject("Leaf");
            leaf.transform.SetParent(parent, true);
            leaf.transform.position = pos;
            leaf.transform.rotation = rot;
            leaf.transform.localScale = Vector3.one * s;

            MeshFilter mf = leaf.AddComponent<MeshFilter>();
            MeshRenderer mr = leaf.AddComponent<MeshRenderer>();
            mf.sharedMesh = quad;
            mr.sharedMaterial = (leafMaterial ? leafMaterial : CreateDefaultLeafMaterial());
            generatedFilters.Add(mf);
        }
    }

    Mesh BuildQuadMesh()
    {
        Mesh m = new Mesh();
        Vector3[] v = new Vector3[4] {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0)
        };
        Vector2[] uv = new Vector2[4] {
            new Vector2(0,0), new Vector2(1,0), new Vector2(0,1), new Vector2(1,1)
        };
        int[] t = new int[6] { 0, 2, 1, 1, 2, 3 };
        Vector3[] n = new Vector3[4] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
        m.vertices = v; m.uv = uv; m.triangles = t; m.normals = n;
        return m;
    }

    Material CreateDefaultLeafMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.SetFloat("_Mode", 1); // cutout
        return mat;
    }

    // Combine provided MeshFilters into one mesh, assign main material
    void CombineIntoSingle(GameObject root, MeshFilter[] parts)
    {
        var combineList = new List<UnityEngine.CombineInstance>();
        List<Material> materials = new List<Material>();

        foreach (var mf in parts)
        {
            if (mf == null || mf.sharedMesh == null) continue;
            var mr = mf.GetComponent<MeshRenderer>();
            var ci = new UnityEngine.CombineInstance { mesh = mf.sharedMesh, transform = mf.transform.localToWorldMatrix };
            combineList.Add(ci);
            materials.Add(mr ? mr.sharedMaterial : null);
        }

        if (combineList.Count == 0) return;

        Mesh combined = new Mesh();
        combined.CombineMeshes(combineList.ToArray(), true, true);

        GameObject combinedGO = new GameObject("CombinedTree");
        combinedGO.transform.SetParent(root.transform, false);
        var mfCombined = combinedGO.AddComponent<MeshFilter>();
        var mrCombined = combinedGO.AddComponent<MeshRenderer>();
        mfCombined.sharedMesh = combined;
        mrCombined.sharedMaterial = (barkMaterial ? barkMaterial : new Material(Shader.Find("Standard")));

        // Cleanup old generated children (except keep combined)
        List<Transform> toDestroy = new List<Transform>();
        foreach (Transform t in root.transform)
        {
            if (t.name == "CombinedTree") continue;
            toDestroy.Add(t);
        }
        foreach (var t in toDestroy) DestroyImmediate(t.gameObject);
    }
}
