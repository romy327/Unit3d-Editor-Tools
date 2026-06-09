using UnityEngine;

public class VehicleSpacing : MonoBehaviour
{
    public float safeDistance = 8f;

    public float normalSpeed = 12f;

    SplineVehicle vehicle;

    void Start()
    {
        vehicle = GetComponent<SplineVehicle>();
    }

    void Update()
    {
        RaycastHit hit;

        if (Physics.Raycast(
            transform.position + Vector3.up,
            transform.forward,
            out hit,
            safeDistance))
        {
            if (hit.collider.CompareTag("Traffic"))
            {
                vehicle.speed =
                    Mathf.Lerp(vehicle.speed,
                    0f,
                    Time.deltaTime * 3f);
            }
        }
        else
        {
            vehicle.speed =
                Mathf.Lerp(vehicle.speed,
                normalSpeed,
                Time.deltaTime * 2f);
        }
    }
}