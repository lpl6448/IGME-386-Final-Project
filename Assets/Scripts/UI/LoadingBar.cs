using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI element that checks the status of one or more Python scripts and updates a loading bar with
/// the current progress and messages in the scripts
/// </summary>
public class LoadingBar : MonoBehaviour
{
    /// <summary>
    /// Contains different status types used to color the loading bar
    /// </summary>
    public enum ColorType
    {
        InProgress,     // Waiting for script(s) to complete
        Success,        // Script(s) have all completed successfully
        Failure,        // One or more scripts have failed
    }

    // References
    [SerializeField] private Image negativeFillImage;
    [SerializeField] private Image barColorImage;
    [SerializeField] private TMP_Text statusText;

    // Colors corresponding to ColorType
    [SerializeField] private Color inProgressColor;
    [SerializeField] private Color successColor;
    [SerializeField] private Color failureColor;

    // Exponential smoothing values (higher is more snappy)
    [SerializeField] private float progressSmoothing;
    [SerializeField] private float colorSmoothing;

    private float stageStart = 0;                           // Progress amount for the beginning of this stage
    private float stageEnd = 0;                             // Progress amount for the end of this stage
    private List<PythonScriptStatus> stageLinkedScripts;    // List of all scripts that must complete for this stage to succeed
    private string lastTextBeforeOverride;                  // Status message before a text override was initiated
    private string textOverride;                            // If not null, the text to display instead of a status message
    private ColorType colorOverride;                        // If textOverride is not null, the color to display instead of the current status color

    /// <summary>
    /// Begins a stage, where one or more scripts must complete successfully
    /// </summary>
    /// <param name="start">Loading bar's fill amount at the beginning of the stage</param>
    /// <param name="end">Loading bar's fill amount at the end of the stage</param>
    /// <param name="linkedScripts">Script(s) that must complete successfully before this stage is finished</param>
    public void StartStage(float start, float end, params PythonScriptStatus[] linkedScripts)
    {
        stageStart = start;
        stageEnd = end;
        stageLinkedScripts = new List<PythonScriptStatus>(linkedScripts);
    }

    /// <summary>
    /// Initiates a status override with the given text and color
    /// </summary>
    /// <param name="text">Message text to display instead of the current status text</param>
    /// <param name="colorType">Color to display instead of the current</param>
    public void OverrideStatus(string text, ColorType colorType)
    {
        lastTextBeforeOverride = GetProgressText();
        textOverride = text;
        colorOverride = colorType;
    }

    /// <summary>
    /// Initializes all values for this loading bar
    /// </summary>
    public void Initialize()
    {
        negativeFillImage.fillAmount = 1;
        barColorImage.color = GetColorFromType(ColorType.InProgress);
        statusText.text = "";
        stageStart = 0;
        stageEnd = 0;
        stageLinkedScripts = null;
    }

    private void Update()
    {
        // Do not update if there if no stage has been initiated yet
        if (stageLinkedScripts == null)
            return;

        // Get all status values, considering any overrides
        float goalProgress = GetProgress();
        string text = GetProgressText();
        ColorType colorType = GetColorType(goalProgress);
        if (textOverride != null && text == lastTextBeforeOverride)
        {
            text = textOverride;
            colorType = colorOverride;
        }
        else
            textOverride = null;

        // Update UI based on the status values, smoothing out numeric values
        negativeFillImage.fillAmount = Mathf.Lerp(negativeFillImage.fillAmount, 1 - goalProgress, 1 - Mathf.Exp(-Time.deltaTime * progressSmoothing));
        barColorImage.color = Color.Lerp(barColorImage.color, GetColorFromType(colorType), 1 - Mathf.Exp(-Time.deltaTime * colorSmoothing));
        statusText.text = text;
    }

    /// <summary>
    /// Gets the loading bar's current fill, based on the average progress of all linked scripts and the stage start/end
    /// </summary>
    /// <returns>Loading bar's current fill amount</returns>
    private float GetProgress()
    {
        float stageProgress = 0;
        foreach (PythonScriptStatus status in stageLinkedScripts)
            stageProgress += status.Progress;
        stageProgress /= stageLinkedScripts.Count;

        return Mathf.Lerp(stageStart, stageEnd, stageProgress / 100);
    }

    /// <summary>
    /// Gets the most recent status text from the linked scripts, not considering overrides.
    /// If there are multiple scripts, the status of the script with the lowest progress is returned
    /// </summary>
    /// <returns>Most recent status text to display</returns>
    private string GetProgressText()
    {
        string text = null;
        float minProgress = Mathf.Infinity;
        foreach (PythonScriptStatus status in stageLinkedScripts)
            if (!string.IsNullOrEmpty(status.LastProgressMessage) && status.Progress < minProgress)
            {
                text = status.LastProgressMessage;
                minProgress = status.Progress;
            }
        return text;
    }

    /// <summary>
    /// Gets the color type to use based on a progress value, not considering overrides
    /// </summary>
    /// <param name="progress">Overall progress amount (0-1)</param>
    /// <returns>ColorType corresponding to the current overall progress</returns>
    private ColorType GetColorType(float progress)
    {
        if (progress >= 1)
            return ColorType.Success;
        return ColorType.InProgress;
    }

    /// <summary>
    /// Gets the configured color for a given ColorType
    /// </summary>
    /// <param name="type">ColorType to get the color of</param>
    /// <returns>Color for that type, based on configured color values</returns>
    private Color GetColorFromType(ColorType type)
        => type == ColorType.InProgress ? inProgressColor : type == ColorType.Success ? successColor : failureColor;
}
