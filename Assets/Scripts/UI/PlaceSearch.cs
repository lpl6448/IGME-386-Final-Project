using System;
using System.Collections.Generic;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Networking;

public class PlaceSearch : MonoBehaviour
{
    [SerializeField] private MapSelector mapSelector;
    [SerializeField] private string searchUrl;
    [SerializeField] private PlaceSearchItem itemPrefab;
    [SerializeField] private Transform itemContainer;
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private float updatePeriod;

    private List<PlaceSearchItem> items = new List<PlaceSearchItem>();

    public void SelectPlace(ArcGISPoint coordinates)
    {
        mapSelector.SelectLocation(GeoUtils.ProjectToSpatialReference(coordinates, ArcGISSpatialReference.WGS84()));
    }

    private float lastUpdate = -1;
    private UnityWebRequest req;
    private string lastSearch;
    private void Start()
    {
        lastUpdate = Time.time;
        lastSearch = searchInput.text;
    }
    private void Update()
    {
        if (req != null)
        {
            if (req.isDone)
            {
                if (req.responseCode != 200)
                {
                    Debug.LogWarning("Failed to search with code " + req.responseCode);
                    req = null;
                    return;
                }

                ClearItems();

                ResponseSearches response = JsonUtility.FromJson<ResponseSearches>(req.downloadHandler.text);
                ArcGISSpatialReference spatialReference = new ArcGISSpatialReference(response.spatialReference.wkid);
                foreach (ResponseCandidates candidate in response.candidates)
                {
                    PlaceSearchItem item = Instantiate(itemPrefab, itemContainer, false);
                    item.Initialize(this, candidate.attributes.LongLabel, new ArcGISPoint(candidate.location.x, candidate.location.y, spatialReference));
                    items.Add(item);
                }

                lastUpdate = Time.time;
                req = null;
            }
        }
        else if (Time.time - lastUpdate >= updatePeriod && lastSearch != searchInput.text)
        {
            lastSearch = searchInput.text;

            if (!string.IsNullOrWhiteSpace(lastSearch))
            {
                req = UnityWebRequest.Get(searchUrl.Replace("{0}", lastSearch) + ApiKeyInput.Instance.ApiKey);
                req.SendWebRequest();
            }
            else
                ClearItems();
        }
    }
    private void ClearItems()
    {
        foreach (PlaceSearchItem item in items)
            Destroy(item.gameObject);
        items.Clear();
    }
}
[Serializable]
public class ResponseSearches
{
    public ResponseSpatialReference spatialReference;
    public ResponseCandidates[] candidates;
}
[Serializable]
public class ResponseSpatialReference
{
    public int wkid;
}
[Serializable]
public class ResponseCandidates
{
    public string address;
    public double2 location;
    public ResponseAttributes attributes;
}
[Serializable]
public class ResponseAttributes
{
    public string LongLabel;
}