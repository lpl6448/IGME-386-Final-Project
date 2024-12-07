using UnityEngine;

public class PanelFlowManager : MonoBehaviour
{
    public enum State
    {
        Settings,
        Loading,
        Map,
    }

    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private LoadingPanel loadingPanel;
    [SerializeField] private GameObject mapPanel;

    public State CurrentState { get; private set; }

    private void Start()
    {
        CurrentState = State.Settings;
        settingsPanel.SetActive(true);
        mapPanel.SetActive(false);
        loadingPanel.gameObject.SetActive(false);
    }
    public void ProgressToLoading()
    {
        if (CurrentState != State.Settings)
            return;

        CurrentState = State.Loading;
        settingsPanel.SetActive(false);
        loadingPanel.gameObject.SetActive(true);
        loadingPanel.Load();
    }
    public void ProgressToMap()
    {
        if (CurrentState != State.Loading)
            return;

        CurrentState = State.Map;
        loadingPanel.gameObject.SetActive(false);
        mapPanel.SetActive(true);
    }
}