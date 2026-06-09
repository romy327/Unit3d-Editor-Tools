using UnityEngine;
using UnityEngine.Splines;

public class SplineVehicle : MonoBehaviour
{
    public SplineContainer spline;

    [Range(0f, 1f)]
    public float distanceOnSpline;

    public float speed = 12f;

    public float maxSpeed = 12f;

    public float safeDistance = 0.04f;

    public SplineVehicle frontVehicle;

    float splineLength;

    void Start()
    {
        if (spline != null)
            splineLength = spline.CalculateLength();

        speed = maxSpeed;
    }

    void Update()
    {
        if (spline == null || splineLength <= 0f) return;

        float targetSpeed = maxSpeed;

        float step = (targetSpeed / splineLength) * Time.deltaTime;
        float targetDistance = distanceOnSpline + step;

        // 🚨 SAFE FOLLOW SYSTEM (STABLE VERSION)
        if (frontVehicle != null)
        {
            float gap = frontVehicle.distanceOnSpline - distanceOnSpline;

            if (gap < safeDistance)
            {
                targetSpeed = 0f;
            }
        }

        // 🔥 smooth acceleration / braking
        speed = Mathf.Lerp(speed, targetSpeed, Time.deltaTime * 3f);

        float finalStep = (speed / splineLength) * Time.deltaTime;

        distanceOnSpline += finalStep;

        // 🔥 clean loop (no clamp jitter)
        if (distanceOnSpline > 1f)
            distanceOnSpline -= 1f;

        if (distanceOnSpline < 0f)
            distanceOnSpline += 1f;

        Vector3 pos = spline.EvaluatePosition(distanceOnSpline);
        Vector3 forward = spline.EvaluateTangent(distanceOnSpline);

        transform.position = pos;
        transform.rotation = Quaternion.LookRotation(forward);
    }
}