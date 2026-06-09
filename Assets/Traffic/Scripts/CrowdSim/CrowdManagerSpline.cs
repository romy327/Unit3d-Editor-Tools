using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Splines;

public enum TrafficMode { OneWay, TwoWay }

public class CrowdManagerSpline : MonoBehaviour
{
    public SplineContainer splineContainer;
    public List<GameObject> agentPrefabs;
    public int poolSize = 50;
    public float roadWidth = 2.0f;
    public TrafficMode mode = TrafficMode.OneWay;

    void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject prefab = agentPrefabs[Random.Range(0, agentPrefabs.Count)];
            GameObject agent = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            
            var controller = agent.GetComponent<CrowdAgentSpline>();
            bool isReversed = (mode == TrafficMode.TwoWay) && (i % 2 != 0);
            float startProgress = (float)i / (float)poolSize;
            
            controller.Initialize(splineContainer, startProgress, roadWidth, i, poolSize, isReversed);
            
            agent.tag = "Agent";
            agent.name = "PooledAgent_" + i;
        }
    }
}