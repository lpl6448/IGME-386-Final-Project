using System.Collections;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// UI element that displays an interactive map and lets the user click a location to begin the loading operation
/// </summary>
public class MapSelector : MonoBehaviour
{
    // References
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform mapBounds;
    [SerializeField] private RectTransform selectionBox;
    [SerializeField] private RectTransform mapPanel;
    [SerializeField] private CanvasGroup alphaGroup;
    [SerializeField] private RawImage reflImage;
    [SerializeField] private RawImage cloudsImage;
    [SerializeField] private RectTransform[] blackoutBoxes; // Bounds that cannot be clicked on the map (for other interactable UI elements)

    [SerializeField] private UnityEvent onExit;     // Called after a location is selected
    [SerializeField] private UnityEvent onQuit;     // Called when the user cancels map selection
    [SerializeField] private float selectionExtent; // Extent used to visualize the expected viewable area for the cursor location
    [SerializeField] private float2 mapExtentMin;   // Minimum corner of the map, using WebMercator
    [SerializeField] private float2 mapExtentMax;   // Maximum corner of the map, using WebMercator

    private Material reflMat;       // Material to update with reflectivity and precipitation type data
    private bool grow = false;      // Whether a location has been selected and the map is currently zooming in to it
    private float growStartTime;    // Game time when the map started zooming in to the selected location
    private float mapStartTime;     // Game time when the map was initialized

    /// <summary>
    /// Initializes all textures and state for the map display
    /// </summary>
    public void InitializeMap()
    {
        reflMat = new Material(reflImage.material);
        reflImage.material = reflMat;

        reflImage.texture = RasterImporter.Instance.ReflectivityTexture;
        reflMat.SetTexture("_PrecipFlagTex", RasterImporter.Instance.PrecipFlagTexture);
        cloudsImage.texture = RasterImporter.Instance.TotalCloudsTexture;

        mapPanel.localScale = Vector3.one;
        alphaGroup.alpha = 1;

        mapStartTime = Time.time;
    }

    /// <summary>
    /// Resets all animation for the map
    /// </summary>
    public void Reset()
    {
        grow = false;
        growStartTime = -1;
    }

    /// <summary>
    /// Selects the given location on the map, beginning an animation to zoom into it and using MapConfigure to initialize the simulation
    /// </summary>
    /// <param name="point"></param>
    public void SelectLocation(ArcGISPoint point)
    {
        if (grow || Time.time - mapStartTime < 0.5f)
            return;

        // Begin the animation to zoom into the location
        grow = true;
        growStartTime = -1;

        // Set the mapPanel pivot so that, when scaled, it appears to zoom into the selected location
        ArcGISPoint pointMercator = GeoUtils.ProjectToSpatialReference(point, ArcGISSpatialReference.WebMercator());
        float2 pointUV = math.unlerp(mapExtentMin, mapExtentMax, (float2)new double2(pointMercator.X, pointMercator.Y));
        float2 pointR = math.lerp(mapBounds.rect.min, mapBounds.rect.max, pointUV);
        float2 pointSS = ((float3)canvas.worldCamera.WorldToScreenPoint(mapBounds.TransformPoint((Vector2)pointR))).xy;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(mapPanel, pointSS, canvas.worldCamera, out Vector2 panelPoint);
        mapPanel.pivot = Rect.PointToNormalized(mapPanel.rect, panelPoint);
        
        // Prepare the simulation
        MapConfigure.Instance.ReconfigureMap(point);
    }

    private void Update()
    {
        // Shortcut for quitting the map selection screen
        if (Input.GetKeyDown(KeyCode.Escape))
            StartCoroutine(QuitCrt());
    }
    private IEnumerator QuitCrt()
    {
        // Wait one frame before quitting, so that the escape press is not processed again on the next screen
        yield return null;
        onQuit.Invoke();
    }

    private void LateUpdate()
    {
        // If not animating, display a cursor approximating the view extent based on the user's pointer
        if (!grow && RectTransformUtility.ScreenPointToLocalPointInRectangle(mapBounds, Input.mousePosition, canvas.worldCamera, out Vector2 point))
        {
            float2 t = Rect.PointToNormalized(mapBounds.rect, point);

            // Prevent any cursor display or interaction when over another interactable UI element
            bool inBlackout = false;
            foreach (RectTransform blackout in blackoutBoxes)
                if (RectTransformUtility.RectangleContainsScreenPoint(blackout, Input.mousePosition, canvas.worldCamera))
                {
                    inBlackout = true;
                    break;
                }

            // If the cursor is over a valid portion of the map, display the view extent cursor
            if (!inBlackout && math.all(t > 0) && math.all(t < 1))
            {
                float2 projPoint = math.lerp(mapExtentMin, mapExtentMax, t);
                selectionBox.anchorMin = math.unlerp(mapExtentMin, mapExtentMax, projPoint - selectionExtent);
                selectionBox.anchorMax = math.unlerp(mapExtentMin, mapExtentMax, projPoint + selectionExtent);
                selectionBox.sizeDelta = Vector2.zero;
                selectionBox.gameObject.SetActive(true);

                // Select the location if the user clicks
                if (Input.GetMouseButtonDown(0))
                {
                    ArcGISPoint originMercator = new ArcGISPoint(projPoint.x, projPoint.y, ArcGISSpatialReference.WebMercator());
                    ArcGISPoint originPoint = GeoUtils.ProjectToSpatialReference(originMercator, MapConfigure.Instance.MapReference);
                    SelectLocation(originPoint);
                }
            }
            else
                selectionBox.gameObject.SetActive(false);
        }
        else
            selectionBox.gameObject.SetActive(false);

        // Zoom animation
        if (grow)
        {
            if (growStartTime == -1)
                growStartTime = Time.time; // Prevents the initial frame freeze from making grow time jump

            float elapsed = Time.time - growStartTime;

            // Zoom in
            mapPanel.localScale = Vector3.one * math.min(100000, math.exp(8 * elapsed * elapsed * elapsed));

            // Fade out
            if (elapsed > 0.5f)
            {
                float t = math.saturate((elapsed - 0.5f) / 0.5f);
                alphaGroup.alpha = 1 - t;

                // Conclude animation
                if (t >= 1)
                {
                    grow = false;
                    onExit.Invoke();
                }
            }
        }
    }
}