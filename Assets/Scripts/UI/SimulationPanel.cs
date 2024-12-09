using UnityEngine;

public class SimulationPanel : MonoBehaviour
{
    [SerializeField] private GameObject menu;

    public void RecenterMap()
    {
        MapConfigure.Instance.ReconfigureMapAtCameraPosition();
    }
    public void ToggleMenu()
    {
        menu.gameObject.SetActive(!menu.activeSelf);
    }

    private void OnEnable()
    {
        menu.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ToggleMenu();

        if (Input.GetKeyDown(KeyCode.R))
            RecenterMap();
    }
}