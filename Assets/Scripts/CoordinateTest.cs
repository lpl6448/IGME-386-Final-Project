using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.ArcGISMapsSDK.Utils.Math;
using Esri.GameEngine.Geometry;
using Esri.Unity;
using Unity.Mathematics;
using UnityEngine;

public class CoordinateTest : MonoBehaviour
{
    [SerializeField] private ArcGISMapComponent map;
    [SerializeField] private float extent;

    private void Update()
    {
        ArcGISPoint o = map.OriginPosition;
        ArcGISMutableArray<ArcGISPoint> arr = new ArcGISMutableArray<ArcGISPoint>();
        arr.Add(o);

        ArcGISPoint p0 = ArcGISGeometryEngine.MoveGeodetic(arr, extent,
            (ArcGISLinearUnit)ArcGISUnit.FromWKID((int)ArcGISLinearUnitId.Meters),
            0, (ArcGISAngularUnit)ArcGISUnit.FromWKID((int)ArcGISAngularUnitId.Degrees), ArcGISGeodeticCurveType.Geodesic).First();
        ArcGISPoint p1 = ArcGISGeometryEngine.MoveGeodetic(arr, extent,
            (ArcGISLinearUnit)ArcGISUnit.FromWKID((int)ArcGISLinearUnitId.Meters),
            90, (ArcGISAngularUnit)ArcGISUnit.FromWKID((int)ArcGISAngularUnitId.Degrees), ArcGISGeodeticCurveType.Geodesic).First();
        ArcGISPoint p2 = ArcGISGeometryEngine.MoveGeodetic(arr, extent,
            (ArcGISLinearUnit)ArcGISUnit.FromWKID((int)ArcGISLinearUnitId.Meters),
            180, (ArcGISAngularUnit)ArcGISUnit.FromWKID((int)ArcGISAngularUnitId.Degrees), ArcGISGeodeticCurveType.Geodesic).First();
        ArcGISPoint p3 = ArcGISGeometryEngine.MoveGeodetic(arr, extent,
            (ArcGISLinearUnit)ArcGISUnit.FromWKID((int)ArcGISLinearUnitId.Meters),
            270, (ArcGISAngularUnit)ArcGISUnit.FromWKID((int)ArcGISAngularUnitId.Degrees), ArcGISGeodeticCurveType.Geodesic).First();
        
        PrintPoint(p0);
        PrintPoint(p1);
        PrintPoint(p2);
        PrintPoint(p3);
    }
    private void PrintPoint(ArcGISPoint p) => print(p.X + ", " + p.Y);
}