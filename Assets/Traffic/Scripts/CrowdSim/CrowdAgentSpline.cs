using UnityEngine;
using System.Collections;
using UnityEngine.Splines;

[RequireComponent(typeof(Animator), typeof(BoxCollider), typeof(Rigidbody))]
public class CrowdAgentSpline : MonoBehaviour
{
    [HideInInspector] public SplineContainer splineContainer;
    public float travelSpeed = 2f;
    public float rotationSpeed = 5f;
    
    [HideInInspector] public float progress;
    [HideInInspector] public bool isReversed; 
    private float currentSpeedMultiplier = 1f;
    private Vector3 lateralOffset;
    private float offsetRange;
    
    private Animator animator;
    private float splineLength;

    void Start()
    {
        animator = GetComponent<Animator>();
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; 
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
    }

    public void Initialize(SplineContainer container, float startProgress, float offsetRange, int agentIndex, int totalAgents, bool reversed)
    {
        splineContainer = container;
        splineLength = splineContainer.Spline.GetLength();
        progress = startProgress;
        this.offsetRange = offsetRange;
        this.isReversed = reversed;
        currentSpeedMultiplier = Random.Range(0.8f, 1.2f);
        
        float laneOffset = Mathf.Lerp(-offsetRange, offsetRange, (float)agentIndex / (float)(totalAgents - 1));
        Vector3 tangent = (Vector3)splineContainer.EvaluateTangent(startProgress);
        Vector3 normal = Vector3.Cross(tangent, Vector3.up).normalized;
        lateralOffset = normal * laneOffset;

        StopAllCoroutines();
        StartCoroutine(MoveRoutine());
        StartCoroutine(SwitchLaneRoutine());
    }

    IEnumerator MoveRoutine()
    {
        while (true)
        {
            if (splineContainer != null)
            {
                float step = (travelSpeed * currentSpeedMultiplier) / splineLength;
                progress += isReversed ? -step * Time.deltaTime : step * Time.deltaTime;
                
                if (progress > 1f) progress = 0f;
                if (progress < 0f) progress = 1f;

                // Casting float3 to Vector3 to fix CS0034 error
                transform.position = (Vector3)splineContainer.EvaluatePosition(progress) + lateralOffset;
                
                Vector3 forward = (Vector3)splineContainer.EvaluateTangent(progress);
                if (isReversed) forward = -forward; 
                
                if (forward != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(forward);
                    Vector3 euler = targetRot.eulerAngles;
                    euler.x = 0; euler.z = 0; // Strictly lock to Y-axis
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(euler), rotationSpeed * Time.deltaTime);
                }

                if (animator != null) animator.SetFloat("Speed", (travelSpeed * currentSpeedMultiplier) / 3f);
            }
            yield return null;
        }
    }

    IEnumerator SwitchLaneRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(5f, 10f));
            float newLaneOffset = Random.Range(-offsetRange, offsetRange);
            Vector3 tangent = (Vector3)splineContainer.EvaluateTangent(progress);
            Vector3 normal = Vector3.Cross(tangent, Vector3.up).normalized;
            Vector3 targetOffset = normal * newLaneOffset;

            float elapsed = 0f;
            Vector3 startOffset = lateralOffset;
            while (elapsed < 2f)
            {
                lateralOffset = Vector3.Lerp(startOffset, targetOffset, elapsed / 2f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            lateralOffset = targetOffset;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Agent")) currentSpeedMultiplier = Mathf.Lerp(currentSpeedMultiplier, 0.2f, Time.deltaTime * 3f);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Agent")) currentSpeedMultiplier = Mathf.Lerp(currentSpeedMultiplier, 1f, Time.deltaTime * 2f);
    }
}