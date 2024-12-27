using System.Collections.Generic;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using Esri.Unity;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Master script that prepares the scene for the selected world location,
/// by reprojecting textures, positioning the camera, updating sun/moon, etc.
/// </summary>
public class MapConfigure : MonoBehaviour
{
    public static MapConfigure Instance { get; private set; }

    // References to other scripts that must be called to fully prepare the scene
    [SerializeField] private ArcGISMapComponent map;
    [SerializeField] private ArcGISLocationComponent cameraLocation;
    [SerializeField] private CelestialController celestialController;

    [SerializeField] private Volume cloudsVolume; // Cloud volume settings
    [SerializeField] private float cloudsExtent; // Distance from the origin that clouds render within (based on Unity-controlled values)
    [SerializeField] private LocalVolumetricFog rainFog; // Fog shaft reference
    [SerializeField] private float rainExtent; // Distance from the origin that rain/snow shafts should render within (based on rainFog values)

    // Extent of the entire simulation map
    [SerializeField] private float2 extentMin = new float2(-14600000, 2600000);
    [SerializeField] private float2 extentMax = new float2(-6800000, 6500000);

    // References to textures used in cloudsVolume and rainFog
    private List<Texture2D> textureHandles = new List<Texture2D>();
    private Texture2D rpLowClouds;
    private Texture2D rpMidClouds;
    private Texture2D rpCumulonimbusMap;
    private Texture2D rpReflFog;
    private Texture2D rpRainMap;
    private Texture2D rpSnowMap;

    /// <summary>
    /// Shorthand property to easily access the spatial reference of the ArcGIS map
    /// </summary>
    public ArcGISSpatialReference MapReference => map.OriginPosition.SpatialReference;

    /// <summary>
    /// Configures the map component, sun/moon positions, and textures for the simulation, using the current camera position as the origin
    /// </summary>
    public void ReconfigureMapAtCameraPosition()
    {
        ArcGISPoint originPos = cameraLocation.Position;
        originPos = new ArcGISPoint(originPos.X, originPos.Y, originPos.SpatialReference);
        ReconfigureMap(originPos, false);
    }

    /// <summary>
    /// Configures the map component, camera location, sun/moon positions, and textures for the simulation
    /// </summary>
    /// <param name="originPoint">Observation center</param>
    /// <param name="resetCamera">If true, set the camera position to the originPoint</param>
    public void ReconfigureMap(ArcGISPoint originPoint, bool resetCamera = true)
    {
        // Update map component
        map.APIKey = ApiKeyInput.Instance.ApiKey;
        map.OriginPosition = originPoint;

        // Update sun/moon positions based on the timestamp of imported data
        celestialController.UpdateSunMoon(originPoint, RasterImporter.Instance.Timestamp);

        // Determine where to sample the base textures
        ArcGISPoint originPointMercator = GeoUtils.ProjectToSpatialReference(originPoint, ArcGISSpatialReference.WebMercator());
        float2 originMercator = (float2)new double2(originPointMercator.X, originPointMercator.Y);
        float2 originUV = math.unlerp(extentMin, extentMax, originMercator);

        // Sample cloud heights
        float height = RasterImporter.Instance.CloudLevelTexture.GetPixelBilinear(originUV.x, originUV.y).r;
        if (height < 1)
            height = 1500; // Default if there is no data
        cloudsVolume.profile.TryGet(out VolumetricClouds volumetricClouds);
        volumetricClouds.bottomAltitude.value = height; // Set cloud base height
        
        // Update fog shaft heights to be fit approximate between the ground and the cloud height
        float fogBufferUp = 500;
        float fogBufferDown = 1000;
        rainFog.parameters.size.z = height + fogBufferUp + fogBufferDown;
        rainFog.transform.position = Vector3.up * (-fogBufferDown + height + fogBufferUp) / 2;

        // Optionally set the camera to be 300 meters below the cloud height and facing north
        if (resetCamera)
        {
            cameraLocation.Position = new ArcGISPoint(originPoint.X, originPoint.Y, height - 300);
            cameraLocation.Rotation = new ArcGISRotation(0, 90, 0);
        }

        // Sample mid- and upper-level clouds and configure the static CloudLayer
        float midCloudsAtOrigin = RasterImporter.Instance.MidCloudsTexture.GetPixelBilinear(originUV.x, originUV.y).r;
        float highCloudsAtOrigin = RasterImporter.Instance.HighCloudsTexture.GetPixelBilinear(originUV.x, originUV.y).r;
        cloudsVolume.profile.TryGet(out CloudLayer cloudLayer);
        cloudLayer.layerA.opacityR.value = math.remap(0, 0.67f, 0, 1, midCloudsAtOrigin);
        cloudLayer.layerA.opacityG.value = math.remap(0.4f, 1, 0, 0.5f, midCloudsAtOrigin);
        cloudLayer.layerB.opacityB.value = math.remap(0, 0.33f, 0, 0.75f, highCloudsAtOrigin);
        cloudLayer.layerB.opacityA.value = math.remap(0.1f, 1, 0, 1, math.pow(highCloudsAtOrigin, 2));

        // Low cloud cover is placed in the red channel
        ReprojectTexture(RasterImporter.Instance.LowCloudsTexture, rpLowClouds, originMercator, cloudsExtent);
        TextureUtility.PixelOperator(rpLowClouds, (x, y, c) => new Color(c.r, 1, 1, 1));

        // Mid cloud cover is placed in the red channel if above a certain threshold (currently unused)
        ReprojectTexture(RasterImporter.Instance.MidCloudsTexture, rpMidClouds, originMercator, cloudsExtent);
        TextureUtility.PixelOperator(rpMidClouds, (x, y, c) => new Color(c.r >= 0.2f ? c.r : 0, 1, 1, 1));

        // Derive a likely cumulonimbus cloud map based on blurring and expanding reflectivity data
        ReprojectTexture(RasterImporter.Instance.ReflectivityTexture, rpCumulonimbusMap, originMercator, cloudsExtent);
        TextureUtility.MaxConvolution(rpCumulonimbusMap, 3);
        TextureUtility.Convolution(rpCumulonimbusMap, TextureUtility.GenerateGaussianKernel(5));
        TextureUtility.PixelOperator(rpCumulonimbusMap, (x, y, c) =>
        {
            // This code aims to limit the interference between cumulonimbus clouds and surrounding clouds,
            // since Unity only allows one type of cloud for each "column" of the atmosphere
            float low = rpLowClouds.GetPixel(x, y).r;
            rpLowClouds.SetPixel(x, y, new Color(math.max(low,
                math.saturate(math.unlerp(0.04f, 0.07f, c.r))), 1, 1, 1));
            float mid = rpMidClouds.GetPixel(x, y).r;
            float max = math.max(low, mid);
            float t = math.saturate(math.unlerp(0.04f, 0.16f, c.r));
            if (t > 0)
                t = math.lerp(0.25f, 1, t * t);
            return new Color(t, 1, 1, 1);
        });

        // Derive a rain map to darken clouds that are producing rain
        ReprojectTexture(RasterImporter.Instance.ReflectivityTexture, rpRainMap, originMercator, cloudsExtent);
        TextureUtility.PixelOperator(rpRainMap, (x, y, c) => new Color(math.saturate(math.unlerp(0.03f, 0.09f, c.r)), 1, 1, 1));

        // The red channel contains the precipitation type (0 - rain, 1 - snow)
        // This texture is also blurred to make the transition between rain and snow more gradual
        ReprojectTexture(RasterImporter.Instance.PrecipFlagTexture, rpSnowMap, originMercator, rainExtent, false);
        TextureUtility.PixelOperator(rpSnowMap, (x, y, c) =>
        {
            return new Color(math.abs(c.r - 3 / 255f) < 0.5f / 255f ? 1 : 0, 1, 1, 1);
        });
        TextureUtility.MaxConvolution(rpSnowMap, 3);
        TextureUtility.Convolution(rpSnowMap, TextureUtility.GenerateGaussianKernel(5));

        // Fill the fog input texture with precipitation intensity (red) and type (blue)
        ReprojectTexture(RasterImporter.Instance.ReflectivityTexture, rpReflFog, originMercator, rainExtent);
        TextureUtility.PixelOperator(rpReflFog, (x, y, c) =>
        {
            float snowAmount = rpSnowMap.GetPixelBilinear((x + 0.5f) / rpReflFog.width, (y + 0.5f) / rpReflFog.height).r;
            return new Color(c.r, snowAmount, 1, 1);
        });

        // Save all texture changes to the GPU
        rpLowClouds.Apply();
        rpMidClouds.Apply();
        rpCumulonimbusMap.Apply();
        rpReflFog.Apply();
        rpRainMap.Apply();
        rpSnowMap.Apply();
    }

    /// <summary>
    /// Creates a new texture with the correct settings to be used in cloudsVolume and rainFog
    /// </summary>
    /// <param name="resolution">Pixel size (width and height) of the texture</param>
    /// <param name="format">Texture format</param>
    /// <returns>New texture with the specified settings and some required defaults</returns>
    private Texture2D CreateTexture(int resolution, TextureFormat format)
    {
        Texture2D tex = new Texture2D(resolution, resolution, format, false, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        textureHandles.Add(tex);
        return tex;
    }

    /// <summary>
    /// Reprojects the source textures into the target texture by approximating the new texture bounds and optionally linearly interpolating.
    /// Web Mercator still produces subtle artifacts due to its curvature, but these are mitigated somewhat by using ArcGIS's MoveGeodetic
    /// function to approximate the true bounds of the texture.
    /// </summary>
    /// <param name="source">Base texture, unchanged</param>
    /// <param name="target">Destination texture, changed to contain the reprojected data</param>
    /// <param name="originMercator">Web Mercator coordinates of the projection origin</param>
    /// <param name="extent">Extent in Web Mercator meters of the projection</param>
    /// <param name="bilinear">Whether to reproject the entire texture</param>
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
            TextureUtility.ReprojectTexture(source, extentMin, extentMax,
                target, new float2((float)nx.X, (float)ny.Y), new float2((float)px.X, (float)py.Y));
        else
            TextureUtility.ReprojectTextureNearestNeighbor(source, extentMin, extentMax,
                target, new float2((float)nx.X, (float)ny.Y), new float2((float)px.X, (float)py.Y));
    }

    /// <summary>
    /// Move a point a given number of meters in a particular direction
    /// </summary>
    /// <param name="p">Base point to move</param>
    /// <param name="meters">Number of meters to move the point</param>
    /// <param name="deg">Azimuth degrees defining the direction to move the point</param>
    /// <returns>The base point, shifted based on meters and deg</returns>
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

    /// <summary>
    /// To initialize, create all required textures for cloudsVolume and rainFog
    /// </summary>
    private void Start()
    {
        cloudsVolume.profile.TryGet(out VolumetricClouds volumetricClouds);

        rpLowClouds = CreateTexture(256, TextureFormat.R8);
        volumetricClouds.cumulusMap.value = rpLowClouds;

        rpMidClouds = CreateTexture(256, TextureFormat.R8);
        volumetricClouds.altoStratusMap.value = rpMidClouds;

        rpCumulonimbusMap = CreateTexture(256, TextureFormat.R8);
        volumetricClouds.cumulonimbusMap.value = rpCumulonimbusMap;

        rpReflFog = CreateTexture(512, TextureFormat.RGHalf);
        rainFog.parameters.materialMask.SetTexture("_Refl_Snow_Map", rpReflFog);

        rpRainMap = CreateTexture(256, TextureFormat.R8);
        volumetricClouds.rainMap.value = rpRainMap;

        rpSnowMap = CreateTexture(Mathf.RoundToInt(rainExtent * 2 / 1000), TextureFormat.R8); // radar resolution ~1km
    }

    /// <summary>
    /// On destroy, also dispose of all created textures to prevent memory leaks
    /// </summary>
    private void OnDestroy()
    {
        foreach (Texture2D tex in textureHandles)
            Destroy(tex);
        textureHandles.Clear();
    }
}
