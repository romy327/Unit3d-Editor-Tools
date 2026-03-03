using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AlignAndDistribute : EditorWindow
{
    private enum Axis { X, Y, Z }
    private enum LayoutMode { Line, Circle }
    private enum CirclePlane { XY, XZ, YZ }

    private Axis selectedAxis = Axis.X;
    private LayoutMode layoutMode = LayoutMode.Line;
    private CirclePlane circlePlane = CirclePlane.XZ;

    private float spaceBetween = 1f;
    private float circleRadius = 5f;

    private bool duplicateObjects = false;
    private int duplicateCount = 8;

    private bool preview = true;
    private List<Vector3> previewPositions = new List<Vector3>();

    [MenuItem("Tools/Align and Distribute Objects")]
    public static void ShowWindow() => GetWindow<AlignAndDistribute>("Align & Distribute");

    private void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
    private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnGUI()
    {
        GUILayout.Label("Align & Distribute Tool", EditorStyles.boldLabel);

        layoutMode = (LayoutMode)EditorGUILayout.EnumPopup("Layout Mode", layoutMode);
        if (layoutMode == LayoutMode.Line)
        {
            selectedAxis = (Axis)EditorGUILayout.EnumPopup("Axis", selectedAxis);
            spaceBetween = EditorGUILayout.FloatField("Space Between", spaceBetween);
        }
        else
        {
            circlePlane = (CirclePlane)EditorGUILayout.EnumPopup("Circle Plane", circlePlane);
            circleRadius = EditorGUILayout.FloatField("Radius", circleRadius);
        }

        duplicateObjects = EditorGUILayout.Toggle("Duplicate Objects", duplicateObjects);
        if (duplicateObjects)
            duplicateCount = EditorGUILayout.IntSlider("Count", duplicateCount, 1, 100);

        preview = EditorGUILayout.Toggle("Preview", preview);

        GUILayout.Space(10);
        if (GUILayout.Button("Apply"))
            ApplyLayout();
    }

    private void ApplyLayout()
    {
        Object[] selection = Selection.objects;
        if (selection.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "Select at least one scene object or prefab asset.", "OK");
            return;
        }

        List<Transform> targets = new List<Transform>();

        foreach (Object obj in selection)
        {
            if (obj == null) continue;
            GameObject go = obj as GameObject;
            if (go == null) continue;

            // Prefab asset in Project tab
            if (PrefabUtility.IsPartOfPrefabAsset(go))
            {
                for (int i = 0; i < (duplicateObjects ? duplicateCount : 1); i++)
                {
                    GameObject dup = (GameObject)PrefabUtility.InstantiatePrefab(go); // Linked prefab instance
                    if (dup != null)
                    {
                        dup.name = go.name + (duplicateObjects ? "_" + i : "");
                        Undo.RegisterCreatedObjectUndo(dup, "Duplicate Prefab");

                        // Assign parent after instantiation to preserve prefab link
                        if (Selection.activeTransform != null)
                            dup.transform.SetParent(Selection.activeTransform.parent, true);

                        targets.Add(dup.transform);
                    }
                }
            }
            // Scene object in Hierarchy
            else if (go.scene.IsValid())
            {
                for (int i = 0; i < (duplicateObjects ? duplicateCount : 1); i++)
                {
                    GameObject dup = Object.Instantiate(go, go.transform.position, go.transform.rotation, go.transform.parent);
                    dup.name = go.name + (duplicateObjects ? "_" + i : "");
                    Undo.RegisterCreatedObjectUndo(dup, "Duplicate Scene Object");
                    targets.Add(dup.transform);
                }
            }
        }

        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No valid objects or prefabs to duplicate.", "OK");
            return;
        }

        // Apply layout
        if (layoutMode == LayoutMode.Line)
            ApplyLine(targets);
        else
            ApplyCircle(targets);
    }

    private void ApplyLine(List<Transform> targets)
    {
        if (targets.Count == 0) return;
        Vector3 start = targets[0].position;

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null) continue;
            Undo.RecordObject(targets[i], "Align Line");

            Vector3 pos = start;
            if (selectedAxis == Axis.X) pos.x += i * spaceBetween;
            if (selectedAxis == Axis.Y) pos.y += i * spaceBetween;
            if (selectedAxis == Axis.Z) pos.z += i * spaceBetween;

            targets[i].position = pos;
        }
    }

    private void ApplyCircle(List<Transform> targets)
    {
        if (targets.Count == 0) return;
        Vector3 center = targets[0].position;
        float angleStep = 360f / targets.Count;

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null) continue;
            Undo.RecordObject(targets[i], "Arrange Circle");

            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 pos = center;

            switch (circlePlane)
            {
                case CirclePlane.XZ: pos.x += Mathf.Cos(angle) * circleRadius; pos.z += Mathf.Sin(angle) * circleRadius; break;
                case CirclePlane.XY: pos.x += Mathf.Cos(angle) * circleRadius; pos.y += Mathf.Sin(angle) * circleRadius; break;
                case CirclePlane.YZ: pos.y += Mathf.Cos(angle) * circleRadius; pos.z += Mathf.Sin(angle) * circleRadius; break;
            }

            targets[i].position = pos;
            targets[i].LookAt(center);
        }
    }

    private void OnSceneGUI(SceneView view)
    {
        if (!preview) return;
        GameObject[] selected = Selection.gameObjects;
        if (selected.Length == 0) return;

        previewPositions.Clear();
        int count = duplicateObjects ? duplicateCount : selected.Length;
        if (count <= 0) return;

        Vector3 center = selected[0]?.transform.position ?? Vector3.zero;

        if (layoutMode == LayoutMode.Line)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = center;
                if (selectedAxis == Axis.X) pos.x += i * spaceBetween;
                if (selectedAxis == Axis.Y) pos.y += i * spaceBetween;
                if (selectedAxis == Axis.Z) pos.z += i * spaceBetween;
                previewPositions.Add(pos);
            }
        }
        else
        {
            float step = 360f / count;
            for (int i = 0; i < count; i++)
            {
                float angle = step * i * Mathf.Deg2Rad;
                Vector3 pos = center;

                switch (circlePlane)
                {
                    case CirclePlane.XZ: pos.x += Mathf.Cos(angle) * circleRadius; pos.z += Mathf.Sin(angle) * circleRadius; break;
                    case CirclePlane.XY: pos.x += Mathf.Cos(angle) * circleRadius; pos.y += Mathf.Sin(angle) * circleRadius; break;
                    case CirclePlane.YZ: pos.y += Mathf.Cos(angle) * circleRadius; pos.z += Mathf.Sin(angle) * circleRadius; break;
                }

                previewPositions.Add(pos);
            }
        }

        Handles.color = Color.cyan;
        foreach (var p in previewPositions)
        {
            Handles.DrawWireDisc(p, Vector3.up, 0.15f);
            Handles.DrawLine(center, p);
        }

        SceneView.RepaintAll();
    }
}
