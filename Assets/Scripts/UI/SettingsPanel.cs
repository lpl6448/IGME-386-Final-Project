using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    public TimestampInput TimestampInput;
    [SerializeField] private ApiKeyInput apiKeyInput;
    [SerializeField] private PythonSelector pythonInput;
    [SerializeField] private Button launchButton;
    [SerializeField] private Button bypassButton;

    private bool hasCheckedCache = false;
    private bool validCache = false;

    private void OnEnable()
    {
        hasCheckedCache = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();

        if (!hasCheckedCache && RasterImporter.Instance != null)
        {
            validCache = RasterImporter.Instance.HasValidTextures();
            hasCheckedCache = true;
        }

        launchButton.interactable = pythonInput.ValidStatus == ValidationInput.StatusType.Valid
            && TimestampInput.ValidStatus == ValidationInput.StatusType.Valid
            && apiKeyInput.ValidStatus == ValidationInput.StatusType.Valid;
        bypassButton.interactable = hasCheckedCache && validCache && apiKeyInput.ValidStatus == ValidationInput.StatusType.Valid;
    }
}