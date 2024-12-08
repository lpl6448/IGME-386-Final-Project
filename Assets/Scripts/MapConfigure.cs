using System.Collections.Generic;
using System.IO;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class MapConfigure : MonoBehaviour
{
    public static MapConfigure Instance { get; private set; }

    [SerializeField] private ArcGISMapComponent map;
    [SerializeField] private ArcGISLocationComponent cameraLocation;

    [SerializeField] private Volume cloudsVolume;
    [SerializeField] private float cloudsExtent;
    [SerializeField] private LocalVolumetricFog rainFog;
    [SerializeField] private float rainExtent;

    [SerializeField] private float2 extentMin = new float2(-14600000, 2600000);
    [SerializeField] private float2 extentMax = new float2(-6800000, 6500000);

    private List<Texture2D> textureHandles = new List<Texture2D>();
    private Texture2D rpLowClouds;
    private Texture2D rpMidClouds;
    private Texture2D rpCumulonimbusMap;
    private Texture2D rpRainFog;
    private Texture2D rpRainMap;

    public ArcGISSpatialReference MapReference => map.OriginPosition.SpatialReference;

    public void ReconfigureMap(ArcGISPoint originPoint)
    {
        map.OriginPosition = originPoint;

        ArcGISPoint originPointMercator = GeoUtils.ProjectToSpatialReference(originPoint, ArcGISSpatialReference.WebMercator());
        float2 originMercator = (float2)new double2(originPointMercator.X, originPointMercator.Y);

        float2 originUV = math.unlerp(extentMin, extentMax, originMercator);
        float height = RasterImporter.Instance.CloudLevelTexture.GetPixelBilinear(originUV.x, originUV.y).r;
        if (height < 1)
            height = 1500; // Default if there is no data
        cloudsVolume.profile.TryGet(out VolumetricClouds volumetricClouds);
        volumetricClouds.bottomAltitude.value = height;
        float fogBuffer = 500;
        rainFog.parameters.size.z = height + fogBuffer;
        rainFog.transform.position = Vector3.up * height / 2;

        cameraLocation.Position = new ArcGISPoint(originPoint.X, originPoint.Y, height - 300);
        cameraLocation.Rotation = new ArcGISRotation(0, 90, 0);

        float midCloudsAtOrigin = RasterImporter.Instance.MidCloudsTexture.GetPixelBilinear(originUV.x, originUV.y).r;
        float highCloudsAtOrigin = RasterImporter.Instance.HighCloudsTexture.GetPixelBilinear(originUV.x, originUV.y).r;
        cloudsVolume.profile.TryGet(out CloudLayer cloudLayer);
        cloudLayer.layerA.opacityR.value = math.remap(0, 0.67f, 0, 1, midCloudsAtOrigin);
        cloudLayer.layerA.opacityG.value = math.remap(0.4f, 1, 0, 0.5f, midCloudsAtOrigin);
        cloudLayer.layerB.opacityB.value = math.remap(0, 0.33f, 0, 0.75f, highCloudsAtOrigin);
        cloudLayer.layerB.opacityA.value = math.remap(0.1f, 1, 0, 1, math.pow(highCloudsAtOrigin, 2));

        ReprojectTexture(RasterImporter.Instance.LowCloudsTexture, rpLowClouds, originMercator, cloudsExtent);
        TextureUtility.PixelOperator(rpLowClouds, (x, y, c) => new Color(c.r, 1, 1, 1));

        ReprojectTexture(RasterImporter.Instance.MidCloudsTexture, rpMidClouds, originMercator, cloudsExtent);
        TextureUtility.PixelOperator(rpMidClouds, (x, y, c) => new Color(c.r >= 0.2f ? c.r : 0, 1, 1, 1));

        ReprojectTexture(RasterImporter.Instance.ReflectivityTexture, rpCumulonimbusMap, originMercator, cloudsExtent);
        TextureUtility.MaxConvolution(rpCumulonimbusMap, 5);
        TextureUtility.Convolution(rpCumulonimbusMap, TextureUtility.GenerateGaussianKernel(5));
        TextureUtility.PixelOperator(rpLowClouds, (x, y, c) => new Color(math.max(c.r,
            math.saturate(math.unlerp(0.03f, 0.06f, rpCumulonimbusMap.GetPixel(x, y).r))), 1, 1, 1));
        TextureUtility.PixelOperator(rpCumulonimbusMap, (x, y, c) =>
        {
            float t = math.saturate(math.unlerp(0.06f, 0.13f, c.r));
            if (t > 0)
                return new Color(math.lerp(0.33f, 1, t), 1, 1, 1);
            return Color.clear;
        });

        ReprojectTexture(RasterImporter.Instance.ReflectivityTexture, rpRainFog, originMercator, rainExtent);
        TextureUtility.PixelOperator(rpRainFog, (x, y, c) => new Color(1, 1, 1, math.saturate(math.unlerp(0.03f, 0.23f, c.r))));

        ReprojectTexture(RasterImporter.Instance.ReflectivityTexture, rpRainMap, originMercator, cloudsExtent);
        TextureUtility.PixelOperator(rpRainMap, (x, y, c) => new Color(math.saturate(math.unlerp(0.03f, 0.09f, c.r)), 1, 1, 1));

        rpLowClouds.Apply();
        rpMidClouds.Apply();
        rpCumulonimbusMap.Apply();
        rpRainFog.Apply();
        rpRainMap.Apply();
    }

    private Texture2D CreateTexture(int resolution, TextureFormat format)
    {
        Texture2D tex = new Texture2D(resolution, resolution, format, false, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        textureHandles.Add(tex);
        return tex;
    }
    private void ReprojectTexture(Texture2D source, Texture2D target, float2 originMercator, float extent)
    {
        TextureReprojector.ReprojectTexture(source, extentMin, extentMax,
            target, originMercator - extent, originMercator + extent);
    }

    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        cloudsVolume.profile.TryGet(out VolumetricClouds volumetricClouds);

        rpLowClouds = CreateTexture(256, TextureFormat.R8);
        volumetricClouds.cumulusMap.value = rpLowClouds;

        rpMidClouds = CreateTexture(256, TextureFormat.R8);
        volumetricClouds.altoStratusMap.value = rpMidClouds;

        rpCumulonimbusMap = CreateTexture(256, TextureFormat.R8);
        volumetricClouds.cumulonimbusMap.value = rpCumulonimbusMap;

        rpRainFog = CreateTexture(512, TextureFormat.ARGB32);
        rainFog.parameters.materialMask.SetTexture("_Rain_Texture", rpRainFog);

        rpRainMap = CreateTexture(256, TextureFormat.R8);
        volumetricClouds.rainMap.value = rpRainMap;
    }

    private void OnDestroy()
    {
        foreach (Texture2D tex in textureHandles)
            Destroy(tex);
        textureHandles.Clear();
    }
}
