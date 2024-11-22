using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class MapConfigure : MonoBehaviour
{
    [SerializeField] private ArcGISMapComponent map;
    [SerializeField] private ArcGISLocationComponent cameraLocation;

    [SerializeField] private Volume cloudsVolume;
    [SerializeField] private float cloudsExtent;
    [SerializeField] private Material rainFog;
    [SerializeField] private float rainExtent;

    [SerializeField] private Texture2D cloudsTex;
    [SerializeField] private Texture2D reflTex;

    private void Start()
    {
        ArcGISPoint originPoint = map.OriginPosition;
        cameraLocation.Position = new ArcGISPoint(originPoint.X, originPoint.Y, 1200);

        ArcGISPoint originPointMercator = GeoUtils.ProjectToSpatialReference(originPoint, ArcGISSpatialReference.WebMercator());
        float2 originMercator = (float2)new double2(originPointMercator.X, originPointMercator.Y);
        
        Texture2D rclouds = new Texture2D(256, 256, TextureFormat.R8, false, false);
        TextureReprojector.ReprojectTexture(cloudsTex, new float2(-14600000, 2600000), new float2(-6800000, 6500000),
            rclouds, originMercator - cloudsExtent, originMercator + cloudsExtent);
        rclouds.Apply();
        cloudsVolume.profile.TryGet(out VolumetricClouds volumetricClouds);
        volumetricClouds.cumulusMap.value = rclouds;
        
        Texture2D rrain = new Texture2D(256, 256, TextureFormat.RGBA32, false, false);
        TextureReprojector.ReprojectTexture(reflTex, new float2(-14600000, 2600000), new float2(-6800000, 6500000),
            rrain, originMercator - rainExtent, originMercator + rainExtent);
        for (int x = 0; x < rrain.width; x++)
            for (int y = 0; y < rrain.height; y++)
            {
                Color c = rrain.GetPixel(x, y);
                rrain.SetPixel(x, y, new Color(1, 1, 1, c.r));
            }
        rrain.Apply();
        rainFog.SetTexture("_Rain_Texture", rrain);
        
        Texture2D rrain2 = new Texture2D(256, 256, TextureFormat.R8, false, false);
        TextureReprojector.ReprojectTexture(reflTex, new float2(-14600000, 2600000), new float2(-6800000, 6500000),
            rrain2, originMercator - cloudsExtent, originMercator + cloudsExtent);
        for (int x = 0; x < rrain2.width; x++)
            for (int y = 0; y < rrain2.height; y++)
            {
                Color c = rrain2.GetPixel(x, y);
                rrain2.SetPixel(x, y, new Color(math.saturate(math.unlerp(0.01f, 0.05f, c.r)), 1, 1, 1));
            }
        rrain2.Apply();
        volumetricClouds.rainMap.value = rrain2;
    }
}