using UnityEngine;

/// <summary>
/// UI panel that controls basic flows for interactable overlay elements
/// </summary>
public class SimulationPanel : MonoBehaviour
{
    // References
    [SerializeField] private GameObject menu;

    /// <summary>
    /// Reconfigures the simulation to be centered at the current camera position
    /// </summary>
    public void RecenterMap()
    {
        MapConfigure.Instance.ReconfigureMapAtCameraPosition();
    }
    
    /// <summary>
    /// Toggles the active state of the pause menu
    /// </summary>
    public void ToggleMenu()
    {
        menu.gameObject.SetActive(!menu.activeSelf);
    }

    private void OnEnable()
    {
        // The pause menu starts deactivated
        menu.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Keyboard shortcuts
        if (Input.GetKeyDown(KeyCode.Escape))
            ToggleMenu();
        if (Input.GetKeyDown(KeyCode.R))
            RecenterMap();
    }
}