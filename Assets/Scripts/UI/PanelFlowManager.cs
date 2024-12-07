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
    [SerializeField] private GameObject mapPanel;

    public State CurrentState { get; private set; }

    private void Start()
    {
        CurrentState = State.Settings;
        settingsPanel.SetActive(true);
        mapPanel.SetActive(false);
    }
    public void ProgressToMap()
    {
        if (CurrentState != State.Settings)
            return;

        CurrentState = State.Map;
        settingsPanel.SetActive(false);
        mapPanel.SetActive(true);
    }
}