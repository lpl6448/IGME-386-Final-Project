using System;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using SunCalcNet;
using SunCalcNet.Model;
using UnityEngine;

/// <summary>
/// Contains references and functions called by other scripts to update the positions of the sun and moon
/// </summary>
public class CelestialController : MonoBehaviour
{
    // References to required lights to position
    [SerializeField] private Light sunLight;
    [SerializeField] private Light moonLight;

    /// <summary>
    /// Sets the positions of the sun and moon based on a world position and time
    /// </summary>
    /// <param name="origin">World position of the observer</param>
    /// <param name="dateTime">Time of observation</param>
    public void UpdateSunMoon(ArcGISPoint origin, DateTime dateTime)
    {
        // SunCalc takes latitude/longitude coordinates
        ArcGISPoint geographicOrigin = GeoUtils.ProjectToSpatialReference(origin, ArcGISSpatialReference.WGS84());
        double lat = geographicOrigin.Y;
        double lng = geographicOrigin.X;

        SunPosition sunPos = SunCalc.GetSunPosition(dateTime, lat, lng);
        if (sunLight != null)
            sunLight.transform.eulerAngles = new Vector3((float)sunPos.Altitude, (float)sunPos.Azimuth) * Mathf.Rad2Deg;

        MoonPosition moonPos = MoonCalc.GetMoonPosition(dateTime, lat, lng);
        if (moonLight != null)
            moonLight.transform.eulerAngles = new Vector3((float)moonPos.Altitude, (float)moonPos.Azimuth) * Mathf.Rad2Deg;

        // Since only one directional light can cast shadows at a time, only enable shadows for the one likely to be most visible
        if (sunPos.Altitude * Mathf.Rad2Deg < -3)
        {
            if (sunLight != null)
                sunLight.shadows = LightShadows.None;
            if (moonLight != null)
                moonLight.shadows = moonPos.Altitude > 0 ? LightShadows.Soft : LightShadows.None;
        }
        else
        {
            if (sunLight != null)
                sunLight.shadows = LightShadows.Soft;
            if (moonLight != null)
                moonLight.shadows = LightShadows.None;
        }
    }
}