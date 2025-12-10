using UnityEngine;
using System;

[System.Serializable]
public struct RayTracingMaterial
{
    public Color color;
    public Color emissionColor;
    public float lightIntensity;
    
    public void SetDefaultValues()
    {
        color = Color.white;
        emissionColor = Color.white;
        lightIntensity = 0;
    }
}
