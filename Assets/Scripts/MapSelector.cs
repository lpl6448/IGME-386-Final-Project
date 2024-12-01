using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class MapSelector : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform mapBounds;
    [SerializeField] private RectTransform selectionBox;
    [SerializeField] private float selectionExtent;
    [SerializeField] private float2 mapExtentMin;
    [SerializeField] private float2 mapExtentMax;

    [SerializeField] private RectTransform mapPanel;
    [SerializeField] private CanvasGroup alphaGroup;

    [SerializeField] private RawImage reflImage;

    private bool grow = false;
    private float growStartTime;

    private void Start()
    {
        reflImage.texture = RasterImporter.Instance.ReflectivityTexture;
    }
    private void LateUpdate()
    {
        if (!grow && RectTransformUtility.ScreenPointToLocalPointInRectangle(mapBounds, Input.mousePosition, canvas.worldCamera, out Vector2 point))
        {
            float2 t = Rect.PointToNormalized(mapBounds.rect, point);
            if (math.all(t > 0) && math.all(t < 1))
            {
                float2 projPoint = math.lerp(mapExtentMin, mapExtentMax, t);
                selectionBox.anchorMin = math.unlerp(mapExtentMin, mapExtentMax, projPoint - selectionExtent);
                selectionBox.anchorMax = math.unlerp(mapExtentMin, mapExtentMax, projPoint + selectionExtent);
                selectionBox.sizeDelta = Vector2.zero;
                selectionBox.gameObject.SetActive(true);

                if (Input.GetMouseButtonDown(0))
                {
                    grow = true;
                    growStartTime = Time.time;

                    ArcGISPoint originMercator = new ArcGISPoint(projPoint.x, projPoint.y, ArcGISSpatialReference.WebMercator());
                    ArcGISPoint originPoint = GeoUtils.ProjectToSpatialReference(originMercator, MapConfigure.Instance.MapReference);
                    MapConfigure.Instance.ReconfigureMap(originPoint);
                    
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(mapPanel, Input.mousePosition, canvas.worldCamera, out Vector2 panelPoint);
                    mapPanel.pivot = Rect.PointToNormalized(mapPanel.rect, panelPoint);
                }
            }
            else
                selectionBox.gameObject.SetActive(false);
        }
        else
            selectionBox.gameObject.SetActive(false);

        if (grow)
        {
            float elapsed = Time.time - growStartTime;
            mapPanel.localScale = Vector3.one * math.min(100000, math.exp(8 * elapsed * elapsed * elapsed));

            if (elapsed > 0.5f)
            {
                float t = math.saturate((elapsed - 0.5f) / 0.5f);
                alphaGroup.alpha = 1 - t;

                if (t == 1)
                    gameObject.SetActive(false);
            }
        }
    }
}