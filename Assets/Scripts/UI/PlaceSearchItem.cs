using Esri.GameEngine.Geometry;
using TMPro;
using UnityEngine;

public class PlaceSearchItem : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    
    private PlaceSearch parent;
    private ArcGISPoint coordinates;

    public void Initialize(PlaceSearch parent, string label, ArcGISPoint coordinates)
    {
        this.parent = parent;
        text.text = label;
        this.coordinates = coordinates;
    }
    public void OnClick()
    {
        parent.SelectPlace(coordinates);
    }
}