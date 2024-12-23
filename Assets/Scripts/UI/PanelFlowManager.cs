using UnityEngine;

public class PanelFlowManager : MonoBehaviour
{
    public enum State
    {
        Settings,
        Loading,
        Map,
        Simulation,
    }

    [SerializeField] private SettingsPanel settingsPanel;
    [SerializeField] private LoadingPanel loadingPanel;
    [SerializeField] private MapSelector mapPanel;
    [SerializeField] private SimulationPanel simulationPanel;

    public State CurrentState { get; private set; }

    private void Start()
    {
        CurrentState = State.Settings;
        settingsPanel.gameObject.SetActive(true);
        mapPanel.gameObject.SetActive(false);
        loadingPanel.gameObject.SetActive(false);
        simulationPanel.gameObject.SetActive(false);
    }
    public void ProgressToLoading()
    {
        if (CurrentState != State.Settings)
            return;

        CurrentState = State.Loading;
        settingsPanel.gameObject.SetActive(false);
        loadingPanel.gameObject.SetActive(true);
        if (settingsPanel.TimestampInput.UseTimestamp)
            loadingPanel.Load(settingsPanel.TimestampInput.Timestamp);
        else
            loadingPanel.Load();
    }
    public void BackToSettings()
    {
        if (CurrentState == State.Settings)
            return;

        CurrentState = State.Settings;
        settingsPanel.gameObject.SetActive(true);
        loadingPanel.gameObject.SetActive(false);
        simulationPanel.gameObject.SetActive(false);
        mapPanel.gameObject.SetActive(false);
    }
    public void BypassLoading()
    {
        if (CurrentState != State.Settings)
            return;

        settingsPanel.gameObject.SetActive(false);
        CurrentState = State.Loading;
        ProgressToMap();
    }
    public void BackToMap()
    {
        if (CurrentState != State.Simulation)
            return;

        CurrentState = State.Map;
        simulationPanel.gameObject.SetActive(false);
        mapPanel.gameObject.SetActive(true);
        mapPanel.InitializeMap();
    }
    public void ProgressToMap()
    {
        if (CurrentState != State.Loading)
            return;

        CurrentState = State.Map;
        RasterImporter.Instance.ImportTextures();
        loadingPanel.gameObject.SetActive(false);
        mapPanel.gameObject.SetActive(true);
        mapPanel.InitializeMap();
    }
    public void ProgressToSimulation()
    {
        if (CurrentState != State.Map)
            return;

        CurrentState = State.Simulation;
        mapPanel.gameObject.SetActive(false);
        simulationPanel.gameObject.SetActive(true);
    }
}