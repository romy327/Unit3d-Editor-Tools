using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class HotspotLoader : EditorWindow
{
    int toatalAssetCount = 0;
    int toatalprefabSpawned = 0;
    int toatalprefabCleared = 0;
    GameObject customPrefab;
    string message = null;
    bool slotsExits = false;

    float prefabScale;

    [MenuItem("Tools/HotSpot Loader")]

    static void Init()
    {
        HotspotLoader window = (HotspotLoader)EditorWindow.GetWindow(typeof(HotspotLoader));
        window.Show();
    }

    void OnGUI()
    {
        InitializeGUI();
    }

    void InitializeGUI()
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Add Custom Hotspot prefab(Ignore this and it will load default)");
        customPrefab = (GameObject)EditorGUILayout.ObjectField(customPrefab, typeof(GameObject), true);
        EditorGUILayout.Space();
        prefabScale = float.Parse(EditorGUILayout.TextField("Hotspot Scale: ", prefabScale.ToString()));
        EditorGUILayout.Space();

        if (GUILayout.Button("Load HotSpot"))
        {
            SpawnHotSpots(customPrefab);
        }
        EditorGUILayout.Space();


        //EditorGUILayout.LabelField("Total Hotspot Assets :  ", toatalAssetCount != 0 ? toatalAssetCount.ToString() : null);
        EditorGUILayout.LabelField("Total Hotspot Assets :  ", toatalAssetCount.ToString());
        if (slotsExits)
        {
            EditorGUILayout.LabelField("Total prefab Spawned : ", toatalprefabSpawned.ToString());
        }
        else
        {
            EditorGUILayout.LabelField("Total prefab Cleared : ", toatalprefabCleared.ToString());
        }
        if (GUILayout.Button("Clear All HotSpots"))
        {
            ClearAllSpawnedHotSpots(customPrefab);
        }

        EditorGUILayout.Space();

        EditorGUILayout.EndVertical();
        if (message != null)
        {
            EditorGUILayout.LabelField(message);
        }
    }

    void SpawnHotSpots(GameObject _hotspotPrefab)
    {
        float prefScale= Mathf.Clamp(prefabScale, 0.01f, 1.5f);
        GameObject hotSpotprefab = _hotspotPrefab;

        GameObject targetprefab  = Selection.activeGameObject;
        if(targetprefab==null)
            targetprefab = GameObject.Find($"Hotspot");

        if (hotSpotprefab == null)
        {
            hotSpotprefab = Resources.Load("HotSpot") as GameObject;
            if (hotSpotprefab == null || targetprefab == null) return;
        }
        int i = 0;
        foreach (Transform ts in targetprefab.transform)
        {
            if (ts.name.StartsWith("Hotspot_sep_"))
            {
                if (ts.childCount <= 0)
                {
                    GameObject hotspotviewGob = PrefabUtility.InstantiatePrefab(hotSpotprefab) as GameObject;
                    hotspotviewGob.transform.parent = ts.transform;
                    hotspotviewGob.transform.localPosition = Vector3.zero;
                    hotspotviewGob.transform.localScale = Vector3.one * prefScale;
                    hotspotviewGob.transform.localRotation = Quaternion.Euler(0, 0, 0);
                    i++;
                }
            }
        }
        slotsExits = true;
        toatalprefabSpawned = i;
        toatalAssetCount = targetprefab.transform.childCount;
        if (i == toatalAssetCount)
            message = "All hotspot Loaded Successfully..!";
        else
        {
            if (i == 0) message = "Hotspots Already Loaded..!";
            else message = "Loading ignored for " + (toatalAssetCount - toatalprefabSpawned) + " , Plaease Check";
        }

    }
    void ClearAllSpawnedHotSpots(GameObject _hotspotPrefab)
    {
        GameObject hotSpotprefab = _hotspotPrefab;

        GameObject targetprefab = Selection.activeGameObject;
        if (targetprefab == null)
            targetprefab = GameObject.Find($"Hotspot");

        if (targetprefab.transform.childCount <= 0)
            return;

        if (hotSpotprefab == null)
        {
            hotSpotprefab = Resources.Load("HotSpot") as GameObject;
            if (hotSpotprefab == null || targetprefab == null) return;
        }

        int i = 0;
        foreach (Transform ts in targetprefab.transform)
        {
            if (ts.name.StartsWith("Hotspot_sep_"))
            {
                if (ts.childCount <= 0) break;
                GameObject hotspotviewGob = ts.GetChild(0).transform.gameObject;
                if (hotspotviewGob != null && ts.name.StartsWith(targetprefab.name))
                {
                    DestroyImmediate(hotspotviewGob);
                    i++;
                }
            }
        }
        toatalprefabCleared = i;
        slotsExits = false;

        if (i == toatalAssetCount)
            message = "All hotspot Cleared Successfully..!";
        else
        {
            if (i == 0) message = "Hotspots Already Cleared..!";
            else message = "Clearing ignored.. Plaease Check";
        }
    }
}
