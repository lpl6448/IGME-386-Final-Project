using Esri.GameEngine.Geometry;
using TMPro;
using UnityEngine;

/// <summary>
/// UI element representing a search result object that the user can click to select on the map
/// </summary>
public class PlaceSearchItem : MonoBehaviour
{
    // References
    [SerializeField] private TMP_Text text;
    
    private PlaceSearch parent;         // PlaceSearch component that created this object
    private ArcGISPoint coordinates;    // Location to select when the user clicks this object

    /// <summary>
    /// Initializes all state and UI for this search result object
    /// </summary>
    /// <param name="parent">Component that created this object</param>
    /// <param name="label">Text to display to the user for this place</param>
    /// <param name="coordinates">Location to select when the user clicks this object</param>
    public void Initialize(PlaceSearch parent, string label, ArcGISPoint coordinates)
    {
        this.parent = parent;
        text.text = label;
        this.coordinates = coordinates;
    }

    /// <summary>
    /// On click, select the location that this search result references
    /// </summary>
    public void OnClick()
    {
        parent.SelectPlace(coordinates);
    }
}