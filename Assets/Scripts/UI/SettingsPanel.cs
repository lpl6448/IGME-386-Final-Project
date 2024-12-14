using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    public TimestampInput TimestampInput;
    [SerializeField] private PythonSelector pythonInput;
    [SerializeField] private Button launchButton;
    [SerializeField] private Button bypassButton;

    private bool hasCheckedCache = false;

    private void OnEnable()
    {
        hasCheckedCache = false;
    }

    private void Update()
    {
        if (!hasCheckedCache && RasterImporter.Instance != null)
        {
            bypassButton.interactable = RasterImporter.Instance.HasValidTextures();
            hasCheckedCache = true;
        }

        launchButton.interactable = pythonInput.ValidStatus == ValidationInput.StatusType.Valid
            && TimestampInput.ValidStatus == ValidationInput.StatusType.Valid;
    }
}