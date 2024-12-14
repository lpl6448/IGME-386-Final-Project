using System;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using SunCalcNet;
using SunCalcNet.Model;
using UnityEngine;

public class CelestialController : MonoBehaviour
{
    [SerializeField] private Light sunLight;
    [SerializeField] private Light moonLight;

    public void UpdateSunMoon(ArcGISPoint origin, DateTime dateTime)
    {
        ArcGISPoint geographicOrigin = GeoUtils.ProjectToSpatialReference(origin, ArcGISSpatialReference.WGS84());

        double lat = geographicOrigin.Y;
        double lng = geographicOrigin.X;

        SunPosition sunPos = SunCalc.GetSunPosition(dateTime, lat, lng);
        if (sunLight != null)
            sunLight.transform.eulerAngles = new Vector3((float)sunPos.Altitude, (float)sunPos.Azimuth) * Mathf.Rad2Deg;

        MoonPosition moonPos = MoonCalc.GetMoonPosition(dateTime, lat, lng);
        if (moonLight != null)
            moonLight.transform.eulerAngles = new Vector3((float)moonPos.Altitude, (float)moonPos.Azimuth) * Mathf.Rad2Deg;

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