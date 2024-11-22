using System.IO;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using Unity.Mathematics;
using UnityEngine;

public class TextureReprojector : MonoBehaviour
{
    public static void ReprojectTexture(Texture2D baseTex, float2 baseMin, float2 baseMax,
        Texture2D newTex, float2 newMin, float2 newMax)
    {
        for (int x = 0; x < newTex.width; x++)
            for (int y = 0; y < newTex.height; y++)
            {
                float2 newTexUV = (new float2(x, y) + 0.5f) / new float2(newTex.width, newTex.height);
                float2 baseTexUV = math.unlerp(baseMin, baseMax, math.lerp(newMin, newMax, newTexUV));
                newTex.SetPixel(x, y, baseTex.GetPixelBilinear(baseTexUV.x, baseTexUV.y));
            }
    }

    [SerializeField] private ArcGISMapComponent map;
    [SerializeField] private Texture2D baseTex;
    [SerializeField] private float extent;
    [SerializeField] private string output;
    private void Start()
    {
        ArcGISPoint originPoint = GeoUtils.ProjectToSpatialReference(map.OriginPosition, ArcGISSpatialReference.WebMercator());
        float2 origin = (float2)new double2(originPoint.X, originPoint.Y);

        Texture2D tex = new Texture2D(256, 256, TextureFormat.R8, false, false);
        ReprojectTexture(baseTex, new float2(-14600000, 2600000), new float2(-6800000, 6500000),
            tex, origin - extent, origin + extent);
        tex.Apply();
        File.WriteAllBytes($"Assets/{output}.png", tex.EncodeToPNG());
    }
}