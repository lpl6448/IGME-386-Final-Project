using UnityEngine;

/// <summary>
/// Controls top-level UI screen visibility and user flows
/// </summary>
public class PanelFlowManager : MonoBehaviour
{
    /// <summary>
    /// Contains all possible states that the game UI can be in
    /// </summary>
    public enum State
    {
        Settings,   // Setup screen
        Loading,    // Loading operation
        Map,        // Map selection
        Simulation, // In simulation (only UI overlays)
    }

    private State currentState; // Currently active UI state

    // References
    [SerializeField] private SettingsPanel settingsPanel;
    [SerializeField] private LoadingPanel loadingPanel;
    [SerializeField] private MapSelector mapPanel;
    [SerializeField] private SimulationPanel simulationPanel;

    private void Start()
    {
        // Activate the settings panel
        currentState = State.Settings;
        settingsPanel.gameObject.SetActive(true);
        mapPanel.gameObject.SetActive(false);
        loadingPanel.gameObject.SetActive(false);
        simulationPanel.gameObject.SetActive(false);
    }

    /// <summary>
    /// Transitions from Settings to Loading and begins loading using the configured timestamp/archive settings
    /// </summary>
    public void ProgressToLoading()
    {
        if (currentState != State.Settings)
            return;

        currentState = State.Loading;
        settingsPanel.gameObject.SetActive(false);
        loadingPanel.gameObject.SetActive(true);
        if (settingsPanel.TimestampInput.UseTimestamp)
            loadingPanel.Load(settingsPanel.TimestampInput.Timestamp);
        else
            loadingPanel.Load();
    }

    /// <summary>
    /// Transitions from any state back to Settings
    /// </summary>
    public void BackToSettings()
    {
        if (currentState == State.Settings)
            return;

        currentState = State.Settings;
        settingsPanel.gameObject.SetActive(true);
        loadingPanel.gameObject.SetActive(false);
        simulationPanel.gameObject.SetActive(false);
        mapPanel.gameObject.SetActive(false);
    }

    /// <summary>
    /// Transitions from Settings to Map, instead using cached data to load the map
    /// </summary>
    public void BypassLoading()
    {
        if (currentState != State.Settings)
            return;

        settingsPanel.gameObject.SetActive(false);
        currentState = State.Loading;
        ProgressToMap();
    }

    /// <summary>
    /// Transitions from Simulation to Map, initializing the map selector
    /// </summary>
    public void BackToMap()
    {
        if (currentState != State.Simulation)
            return;

        currentState = State.Map;
        simulationPanel.gameObject.SetActive(false);
        mapPanel.gameObject.SetActive(true);
        mapPanel.InitializeMap();
    }

    /// <summary>
    /// Transitions from Loading to Map, importing the processed textures and initializing the map selector
    /// </summary>
    public void ProgressToMap()
    {
        if (currentState != State.Loading)
            return;

        currentState = State.Map;
        RasterImporter.Instance.ImportTextures();
        loadingPanel.gameObject.SetActive(false);
        mapPanel.gameObject.SetActive(true);
        mapPanel.InitializeMap();
    }

    /// <summary>
    /// Transitions from Map to Simulation
    /// </summary>
    public void ProgressToSimulation()
    {
        if (currentState != State.Map)
            return;

        currentState = State.Simulation;
        mapPanel.gameObject.SetActive(false);
        simulationPanel.gameObject.SetActive(true);
    }
}