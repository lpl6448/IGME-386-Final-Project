using System;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// UI element that repeatedly queries for and displays the camera's most recent location,
/// using the ArcGIS reverse geocoding REST API
/// </summary>
public class LocationText : MonoBehaviour
{
    // References
    [SerializeField] private ArcGISLocationComponent trackLocation;
    [SerializeField] private TMP_Text locationText;

    [SerializeField] private string templateUrl;    // URL to query, {0} is replaced with coordinates, API key is appended to the end
    [SerializeField] private float updatePeriod;    // Time (seconds) after the last response, before the next request can be sent
    [SerializeField] private float updateDistance;  // Distance (meters) from the last request, before a new one can be sent

    private ArcGISPoint lastLocation = null;        // Camera's location when the most recent request was sent
    private UnityWebRequest req;                    // Current request
    private float updateTime;                       // Game time when a response was last received

    private void OnEnable()
    {
        // Initialize all state
        lastLocation = null;
        updateTime = -updatePeriod - 10;
        locationText.text = "Retrieving location...";
    }

    private void Update()
    {
        // If there is a current request, wait for it to complete
        if (req != null)
        {
            if (req.isDone)
            {
                // If the request did not return 200 OK, fail
                if (req.responseCode != 200)
                {
                    Debug.LogWarning("Reverse geocoding request returned " + req.responseCode);
                    req = null;
                    return;
                }

                ResponseObject response = JsonUtility.FromJson<ResponseObject>(req.downloadHandler.text);

                // If the response has an error, it is likely because there are no nearby locations
                if (response.error != null && response.error.code != 0)
                    locationText.text = "In the middle of nowhere";
                else
                    locationText.text = "Near " + response.address.LongLabel;

                // Allow a new request to be sent after the given updatePeriod
                req = null;
                updateTime = Time.time;
            }
        }
        // Otherwise, consider sending a new request based on updatePeriod and updateDistance
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

    // Extract relevant fields from the JSON response
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