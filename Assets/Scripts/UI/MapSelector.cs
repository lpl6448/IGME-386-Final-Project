using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MapSelector : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private UnityEvent onExit;
    [SerializeField] private RectTransform mapBounds;
    [SerializeField] private RectTransform selectionBox;
    [SerializeField] private float selectionExtent;
    [SerializeField] private float2 mapExtentMin;
    [SerializeField] private float2 mapExtentMax;

    [SerializeField] private RectTransform mapPanel;
    [SerializeField] private CanvasGroup alphaGroup;
    [SerializeField] private RawImage reflImage;
    [SerializeField] private RawImage cloudsImage;

    [SerializeField] private RectTransform[] blackoutBoxes;

    private Material reflMat;
    private bool grow = false;
    private float growStartTime;
    private float mapStartTime;

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
    public void Reset()
    {
        grow = false;
        growStartTime = -1;
    }

    public void SelectLocation(ArcGISPoint point)
    {
        if (grow || Time.time - mapStartTime < 0.5f)
            return;

        grow = true;
        growStartTime = -1;

        MapConfigure.Instance.ReconfigureMap(point);

        ArcGISPoint pointMercator = GeoUtils.ProjectToSpatialReference(point, ArcGISSpatialReference.WebMercator());
        float2 pointUV = math.unlerp(mapExtentMin, mapExtentMax, (float2)new double2(pointMercator.X, pointMercator.Y));
        float2 pointR = math.lerp(mapBounds.rect.min, mapBounds.rect.max, pointUV);
        float2 pointSS = ((float3)canvas.worldCamera.WorldToScreenPoint(mapBounds.TransformPoint((Vector2)pointR))).xy;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(mapPanel, pointSS, canvas.worldCamera, out Vector2 panelPoint);
        mapPanel.pivot = Rect.PointToNormalized(mapPanel.rect, panelPoint);
    }

    private void LateUpdate()
    {
        if (!grow && RectTransformUtility.ScreenPointToLocalPointInRectangle(mapBounds, Input.mousePosition, canvas.worldCamera, out Vector2 point))
        {
            float2 t = Rect.PointToNormalized(mapBounds.rect, point);
            bool inBlackout = false;
            foreach (RectTransform blackout in blackoutBoxes)
                if (RectTransformUtility.RectangleContainsScreenPoint(blackout, Input.mousePosition, canvas.worldCamera))
                {
                    inBlackout = true;
                    break;
                }

            if (!inBlackout && math.all(t > 0) && math.all(t < 1))
            {
                float2 projPoint = math.lerp(mapExtentMin, mapExtentMax, t);
                selectionBox.anchorMin = math.unlerp(mapExtentMin, mapExtentMax, projPoint - selectionExtent);
                selectionBox.anchorMax = math.unlerp(mapExtentMin, mapExtentMax, projPoint + selectionExtent);
                selectionBox.sizeDelta = Vector2.zero;
                selectionBox.gameObject.SetActive(true);

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

        if (grow)
        {
            if (growStartTime == -1)
                growStartTime = Time.time; // Prevents the initial frame freeze from making grow time jump
            float elapsed = Time.time - growStartTime;
            mapPanel.localScale = Vector3.one * math.min(100000, math.exp(8 * elapsed * elapsed * elapsed));

            if (elapsed > 0.5f)
            {
                float t = math.saturate((elapsed - 0.5f) / 0.5f);
                alphaGroup.alpha = 1 - t;

                if (t == 1)
                {
                    grow = false;
                    onExit.Invoke();
                }
            }
        }
    }
}