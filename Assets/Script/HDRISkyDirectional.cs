using UnityEngine;

[ExecuteAlways]
public class HDRISkyDirectional : MonoBehaviour
{
    public Material skyMaterial;      // Your Skybox Material
    public Light directionalLight;    // Scene directional light

    void Update()
    {
        if (skyMaterial && directionalLight)
        {
            // Pass light direction and color to shader
            skyMaterial.SetVector("_SunDirection", -directionalLight.transform.forward);
            skyMaterial.SetColor("_SunColor", directionalLight.color * directionalLight.intensity);
        }
    }
}