using System;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class LocationText : MonoBehaviour
{
    [SerializeField] private string templateUrl;
    [SerializeField] private float updatePeriod;
    [SerializeField] private float updateDistance;
    [SerializeField] private ArcGISLocationComponent trackLocation;
    [SerializeField] private TMP_Text locationText;

    private ArcGISPoint lastLocation = null;
    private UnityWebRequest req;
    private float updateTime;

    private void OnEnable()
    {
        lastLocation = null;
        updateTime = -updatePeriod - 10;
        locationText.text = "Retrieving location...";
    }

    private void Update()
    {
        if (req != null)
        {
            if (req.isDone)
            {
                if (req.responseCode != 200)
                {
                    Debug.LogWarning("Reverse geocoding request returned " + req.responseCode);
                    req = null;
                    return;
                }

                ResponseObject response = JsonUtility.FromJson<ResponseObject>(req.downloadHandler.text);
                if (response.error != null && response.error.code != 0)
                    locationText.text = "In the middle of nowhere";
                else
                    locationText.text = "Near " + response.address.LongLabel;

                req = null;
                updateTime = Time.time;
            }
        }
        else
        {
            ArcGISPoint camPoint = GeoUtils.ProjectToSpatialReference(trackLocation.Position, ArcGISSpatialReference.WGS84());
            double dis = lastLocation == null ? updateDistance + 100000 : ArcGISGeometryEngine.DistanceGeodetic(lastLocation, camPoint,
                ArcGISUnit.FromWKID((int)ArcGISLinearUnitId.Meters) as ArcGISLinearUnit,
                ArcGISUnit.FromWKID((int)ArcGISAngularUnitId.Degrees) as ArcGISAngularUnit,
                ArcGISGeodeticCurveType.Geodesic).Distance;
            if (dis >= updateDistance && Time.time - updateTime > updatePeriod)
            {
                lastLocation = camPoint;
                req = UnityWebRequest.Get(templateUrl.Replace("{0}", lastLocation.X + "," + lastLocation.Y) + ApiKeyInput.Instance.ApiKey);
                req.SendWebRequest();
            }
        }
    }

    [Serializable]
    private class ResponseObject
    {
        public ResponseAddress address;
        public ResponseError error;
    }
    [Serializable]
    private class ResponseAddress
    {
        public string LongLabel;
    }
    [Serializable]
    private class ResponseError
    {
        public int code;
    }
}