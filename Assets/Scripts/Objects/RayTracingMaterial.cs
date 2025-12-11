using UnityEngine;
using System;

[System.Serializable]
public struct RayTracingMaterial
{
    public Color color;
    public Color emissionColor;
    public float lightIntensity;
    [Range(0, 1)] public float reflectance;

    public void SetDefaultValues()
    {
        color = Color.white;
        emissionColor = Color.white;
        lightIntensity = 0;
        reflectance = 1;
    }
}
