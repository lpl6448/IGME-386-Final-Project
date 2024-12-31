using System;
using System.Collections.Generic;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// UI element that uses the ArcGIS geocoding REST API to let the user search for locations on the map by name
/// </summary>
public class PlaceSearch : MonoBehaviour
{
    // References
    [SerializeField] private MapSelector mapSelector;
    [SerializeField] private PlaceSearchItem itemPrefab;
    [SerializeField] private Transform itemContainer;
    [SerializeField] private TMP_InputField searchInput;

    [SerializeField] private string searchUrl;      // URL to send requests to, {0} is replaced by the search query, API key is appended to the end
    [SerializeField] private float updatePeriod;    // Minimum time (seconds) between requests

    private List<PlaceSearchItem> items = new List<PlaceSearchItem>(); // List of instantiated search result objects
    private float lastUpdate = -1;  // Game time of the last API response
    private UnityWebRequest req;    // Current API request
    private string lastSearch;      // Search query when the last request was sent

    /// <summary>
    /// Selects a location on the map by its coordinates
    /// </summary>
    /// <param name="coordinates">Location to select</param>
    public void SelectPlace(ArcGISPoint coordinates)
    {
        mapSelector.SelectLocation(GeoUtils.ProjectToSpatialReference(coordinates, ArcGISSpatialReference.WGS84()));
    }

    private void Start()
    {
        // Initialize state to avoid searching redundantly
        lastUpdate = Time.time;
        lastSearch = searchInput.text;
    }
    private void Update()
    {
        // If there is a current request, wait for a response
        if (req != null)
        {
            if (req.isDone)
            {
                // If the request did not return a 200 OK, fail
                if (req.responseCode != 200)
                {
                    Debug.LogWarning("Failed to search with code " + req.responseCode);
                    req = null;
                    return;
                }

                // Otherwise, assume success
                ClearItems();

                // Instantiate a new list of search result objects
                ResponseSearches response = JsonUtility.FromJson<ResponseSearches>(req.downloadHandler.text);
                ArcGISSpatialReference spatialReference = new ArcGISSpatialReference(response.spatialReference.wkid);
                foreach (ResponseCandidates candidate in response.candidates)
                {
                    PlaceSearchItem item = Instantiate(itemPrefab, itemContainer, false);
                    item.Initialize(this, candidate.attributes.LongLabel, new ArcGISPoint(candidate.location.x, candidate.location.y, spatialReference));
                    items.Add(item);
                }

                // Allow a new request to be sent after updatePeriod
                lastUpdate = Time.time;
                req = null;
            }
        }
        // Otherwise, consider sending a new request
        else if (Time.time - lastUpdate >= updatePeriod && lastSearch != searchInput.text)
        {
            lastSearch = searchInput.text;

            // If there is a new search query, send a new request
            if (!string.IsNullOrWhiteSpace(lastSearch))
            {
                req = UnityWebRequest.Get(searchUrl.Replace("{0}", lastSearch) + ApiKeyInput.Instance.ApiKey);
                req.SendWebRequest();
            }
            else // If the search term is blank, also clear search results
                ClearItems();
        }
    }

    /// <summary>
    /// Destroys all instantiated search result objects so that more can later be instantiated
    /// </summary>
    private void ClearItems()
    {
        foreach (PlaceSearchItem item in items)
            Destroy(item.gameObject);
        items.Clear();
    }

    // Extract relevant fields from the JSON response
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
}