using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

public class TrafficSpawner : MonoBehaviour
{
    public SplineContainer spline;

    public GameObject[] vehiclePrefabs;

    public int vehicleCount = 30;

    List<SplineVehicle> spawned = new List<SplineVehicle>();

    void Start()
    {
        SpawnVehicles();
        AssignFrontVehicles();
    }

    void SpawnVehicles()
    {
        for (int i = 0; i < vehicleCount; i++)
        {
            GameObject prefab =
                vehiclePrefabs[Random.Range(0, vehiclePrefabs.Length)];

            GameObject obj = Instantiate(prefab);

            SplineVehicle v = obj.GetComponent<SplineVehicle>();
            if (v == null)
                v = obj.AddComponent<SplineVehicle>();

            v.spline = spline;

            v.distanceOnSpline = (float)i / vehicleCount;

            v.maxSpeed = Random.Range(8f, 15f);

            spawned.Add(v);
        }
    }

    void AssignFrontVehicles()
    {
        for (int i = 0; i < spawned.Count - 1; i++)
        {
            spawned[i].frontVehicle = spawned[i + 1];
        }

        spawned[spawned.Count - 1].frontVehicle = null;
    }
}