using System.Collections.Generic;
using System.IO;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using Esri.Unity;
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
    [SerializeField][ColorUsage(false, true)] private Color rainFogColorMultiplier;
    [SerializeField][ColorUsage(false, true)] private Color snowFogColorMultiplier;
    [SerializeField] private float snowFogDensityMultiplier;
    [SerializeField] private float reflMulFactor;

    [SerializeField] private float2 extentMin = new float2(-14600000, 2600000);
    [SerializeField] private float2 extentMax = new float2(-6800000, 6500000);

    private List<Texture2D> textureHandles = new List<Texture2D>();
    private Texture2D rpLowClouds;
    private Texture2D rpMidClouds;
    private Texture2D rpCumulonimbusMap;
    private Texture2D rpReflFog;
    private Texture2D rpRainMap;
    private Texture2D rpSnowMap;

    public ArcGISSpatialReference MapReference => map.OriginPosition.SpatialReference;

    public void ReconfigureMapAtCameraPosition()
    {
        ArcGISPoint originPos = cameraLocation.Position;
        originPos = new ArcGISPoint(originPos.X, originPos.Y, originPos.SpatialReference);
        ReconfigureMap(originPos, false);
    }
    public void ReconfigureMap(ArcGISPoint originPoint, bool resetCamera = true)
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
        float fogBufferUp = 500;
        float fogBufferDown = 1000;
        rainFog.parameters.size.z = height + fogBufferUp + fogBufferDown;
        rainFog.transform.position = Vector3.up * (-fogBufferDown + height + fogBufferUp) / 2;

        if (resetCamera)
        {
            cameraLocation.Position = new ArcGISPoint(originPoint.X, originPoint.Y, height - 300);
            cameraLocation.Rotation = new ArcGISRotation(0, 90, 0);
        }

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
        TextureUtility.MaxConvolution(rpCumulonimbusMap, 3);
        TextureUtility.Convolution(rpCumulonimbusMap, TextureUtility.GenerateGaussianKernel(5));
        // TextureUtility.PixelOperator(rpLowClouds, (x, y, c) => new Color(math.max(c.r,
        //     math.saturate(math.unlerp(0.03f, 0.06f, rpCumulonimbusMap.GetPixel(x, y).r))), 1, 1, 1));
        TextureUtility.PixelOperator(rpCumulonimbusMap, (x, y, c) =>
        {
            float low = rpLowClouds.GetPixel(x, y).r;
            rpLowClouds.SetPixel(x, y, new Color(math.max(low,
                math.saturate(math.unlerp(0.04f, 0.07f, c.r))), 1, 1, 1));
            float mid = rpMidClouds.GetPixel(x, y).r;
            float max = math.max(low, mid);
            float t = math.saturate(math.unlerp(0.04f, 0.16f, c.r));
            if (t > 0)
                t = math.lerp(0.25f, 1, t * t);
            // Only use cumulonimbus clouds if they won't interfere with other cloud layers
            return new Color(t, 1, 1, 1);
            // return new Color(math.remap(0.3f, 0.4f, t, 0, max), 1, 1, 1);
            // if (t > 0)
            //     return new Color(math.lerp(0.33f, 1, t), 1, 1, 1);
            // return Color.clear;
        });

        ReprojectTexture(RasterImporter.Instance.ReflectivityTexture, rpRainMap, originMercator, cloudsExtent);
        TextureUtility.PixelOperator(rpRainMap, (x, y, c) => new Color(math.saturate(math.unlerp(0.03f, 0.09f, c.r)), 1, 1, 1));

        ReprojectTexture(RasterImporter.Instance.PrecipFlagTexture, rpSnowMap, originMercator, rainExtent, false);
        TextureUtility.PixelOperator(rpSnowMap, (x, y, c) =>
        {
            return new Color(math.abs(c.r - 3 / 255f) < 0.5f / 255f ? 1 : 0, 1, 1, 1);
        });
        TextureUtility.MaxConvolution(rpSnowMap, 3);
        TextureUtility.Convolution(rpSnowMap, TextureUtility.GenerateGaussianKernel(5));

        ReprojectTexture(RasterImporter.Instance.ReflectivityTexture, rpReflFog, originMercator, rainExtent);
        TextureUtility.PixelOperator(rpReflFog, (x, y, c) =>
        {
            Color baseColor = new Color(1, 1, 1, math.saturate(math.unlerp(0.03f, 0.23f, c.r)));
            float snowAmount = rpSnowMap.GetPixelBilinear((x + 0.5f) / rpReflFog.width, (y + 0.5f) / rpReflFog.height).r;
            Color snowMul = Color.Lerp(new Color(rainFogColorMultiplier.r, rainFogColorMultiplier.g, rainFogColorMultiplier.b, 1),
                new Color(snowFogColorMultiplier.r, snowFogColorMultiplier.g, snowFogColorMultiplier.b, snowFogDensityMultiplier), snowAmount);
            float reflMul = (c.r - reflMulFactor) * (c.r - reflMulFactor) / reflMulFactor / reflMulFactor;
            return new Color(baseColor.r * reflMul, baseColor.g * reflMul, baseColor.b * reflMul, baseColor.a) * snowMul;
        });

        rpLowClouds.Apply();
        rpMidClouds.Apply();
        rpCumulonimbusMap.Apply();
        rpReflFog.Apply();
        rpRainMap.Apply();
        rpSnowMap.Apply();
    }

    private Texture2D CreateTexture(int resolution, TextureFormat format)
    {
        Texture2D tex = new Texture2D(resolution, resolution, format, false, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        textureHandles.Add(tex);
        return tex;
    }
    private void ReprojectTexture(Texture2D source, Texture2D target, float2 originMercator, float extent, bool bilinear = true)
    {
        ArcGISPoint o = new ArcGISPoint(originMercator.x, originMercator.y, ArcGISSpatialReference.WebMercator());
        ArcGISPoint nx = o.Clone() as ArcGISPoint;
        ArcGISPoint ny = o.Clone() as ArcGISPoint;
        ArcGISPoint px = o.Clone() as ArcGISPoint;
        ArcGISPoint py = o.Clone() as ArcGISPoint;
        nx = MovePoint(nx, extent, 270);
        ny = MovePoint(ny, extent, 180);
        px = MovePoint(px, extent, 90);
        py = MovePoint(py, extent, 0);
        if (bilinear)
            TextureReprojector.ReprojectTexture(source, extentMin, extentMax,
                target, new float2((float)nx.X, (float)ny.Y), new float2((float)px.X, (float)py.Y));
        else
            TextureReprojector.ReprojectTextureNearestNeighbor(source, extentMin, extentMax,
                target, new float2((float)nx.X, (float)ny.Y), new float2((float)px.X, (float)py.Y));
    }
    private ArcGISPoint MovePoint(ArcGISPoint p, float meters, float deg)
    {
        ArcGISMutableArray<ArcGISPoint> a = new ArcGISMutableArray<ArcGISPoint>();
        a.Add(p);
        return ArcGISGeometryEngine.MoveGeodetic(a, meters, ArcGISUnit.FromWKID((int)ArcGISLinearUnitId.Meters) as ArcGISLinearUnit,
            deg, ArcGISUnit.FromWKID((int)ArcGISAngularUnitId.Degrees) as ArcGISAngularUnit, ArcGISGeodeticCurveType.Geodesic).First();
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

        rpReflFog = CreateTexture(512, TextureFormat.RGBAHalf);
        rainFog.parameters.materialMask.SetTexture("_Rain_Texture", rpReflFog);

        rpRainMap = CreateTexture(256, TextureFormat.R8);
        volumetricClouds.rainMap.value = rpRainMap;

        rpSnowMap = CreateTexture(Mathf.RoundToInt(rainExtent * 2 / 1000), TextureFormat.R8); // radar resolution ~1km
    }

    private void OnDestroy()
    {
        foreach (Texture2D tex in textureHandles)
            Destroy(tex);
        textureHandles.Clear();
    }
}
