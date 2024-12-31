using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI panel that displays setup options and ensures that everything is configured before the user can move on
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    // References
    public TimestampInput TimestampInput;
    [SerializeField] private ApiKeyInput apiKeyInput;
    [SerializeField] private PythonSelector pythonInput;
    [SerializeField] private Button launchButton;
    [SerializeField] private Button bypassButton;

    private bool hasCheckedCache = false;   // Whether the user's system has been checked for previously loaded map data
    private bool validCache = false;        // Whether the map data cache is valid, assuming it has been checked

    private void OnEnable()
    {
        hasCheckedCache = false;
    }

    private void Update()
    {
        // Shortcut to quit the application
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();

        // If needed, check for previously loaded map data
        if (!hasCheckedCache && RasterImporter.Instance != null)
        {
            validCache = RasterImporter.Instance.HasValidTextures();
            hasCheckedCache = true;
        }

        // The launch button is only clickable if all three inputs are validated
        launchButton.interactable = pythonInput.ValidStatus == ValidationInput.StatusType.Valid
            && TimestampInput.ValidStatus == ValidationInput.StatusType.Valid
            && apiKeyInput.ValidStatus == ValidationInput.StatusType.Valid;
        
        // The bypass button is clickable as long as there is a valid map data cache
        bypassButton.interactable = hasCheckedCache && validCache && apiKeyInput.ValidStatus == ValidationInput.StatusType.Valid;
    }
}