using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX;

public class PrecipEffectController : MonoBehaviour
{
    [SerializeField] private LocalVolumetricFog precipFog;
    [SerializeField] private VisualEffect rainEffect; // Spawn Rate, Fall Velocity
    [SerializeField] private VisualEffect snowEffect; // Spawn Rate, Fall Velocity, Turbulence Strength
    [SerializeField] private Transform samplePoint;

    private void Update()
    {
        Texture2D fogTex = precipFog.parameters.materialMask.GetTexture("_Refl_Snow_Map") as Texture2D;
        float3 localSample = precipFog.transform.InverseTransformPoint(samplePoint.position);
        float2 fogUV = math.unlerp(-((float3)precipFog.parameters.size).xy / 2, ((float3)precipFog.parameters.size).xy / 2, localSample.xy);
        Color encodedData = fogTex.GetPixelBilinear(fogUV.x, fogUV.y).gamma;
        UpdateEffects(encodedData.r, encodedData.g);
    }
    private void UpdateEffects(float reflectivity, float snowAmount)
    {
        if (reflectivity < 0.08f)
            reflectivity = 0;
        float precipIntensity = math.pow(reflectivity, 3.175f);
        float rainPrecipIntensity = precipIntensity * (1 - snowAmount);
        float snowPrecipIntensity = precipIntensity * snowAmount;

        rainEffect.SetFloat("Spawn Rate", math.min(100000, rainPrecipIntensity * 160000 * math.saturate(rainPrecipIntensity * 50)));
        rainEffect.SetVector3("Fall Velocity", new Vector3(2 + rainPrecipIntensity * 40, -12 - rainPrecipIntensity * 70, 0));
        snowEffect.SetFloat("Spawn Rate", math.min(33333, snowPrecipIntensity * 600000));
        snowEffect.SetVector3("Fall Velocity", new Vector3(0.5f + snowPrecipIntensity * 120, -2, 0));
        snowEffect.SetFloat("Turbulence Strength", math.lerp(0.75f, 50, snowPrecipIntensity));
        snowEffect.SetFloat("Min Size", math.lerp(0.004f, 0.07f, snowPrecipIntensity));
        snowEffect.SetFloat("Max Size", math.lerp(0.012f, 0.21f, snowPrecipIntensity));
    }
}