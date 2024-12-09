using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingBar : MonoBehaviour
{
    public enum ColorType
    {
        InProgress,
        Success,
        Failure,
    }

    [SerializeField] private Image negativeFillImage;
    [SerializeField] private Image barColorImage;
    [SerializeField] private TMP_Text statusText;

    [SerializeField] private Color inProgressColor;
    [SerializeField] private Color successColor;
    [SerializeField] private Color failureColor;

    [SerializeField] private float progressSmoothing;
    [SerializeField] private float colorSmoothing;

    private float stageStart = 0;
    private float stageEnd = 0;
    private List<PythonScriptStatus> stageLinkedScripts;
    public void StartStage(float start, float end, params PythonScriptStatus[] linkedScripts)
    {
        stageStart = start;
        stageEnd = end;
        stageLinkedScripts = new List<PythonScriptStatus>(linkedScripts);
    }

    private string lastTextBeforeOverride;
    private string textOverride;
    private ColorType colorOverride;
    public void OverrideStatus(string text, ColorType colorType)
    {
        lastTextBeforeOverride = GetProgressText();
        textOverride = text;
        colorOverride = colorType;
    }

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
        if (stageLinkedScripts == null)
            return;

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

        negativeFillImage.fillAmount = Mathf.Lerp(negativeFillImage.fillAmount, 1 - goalProgress, 1 - Mathf.Exp(-Time.deltaTime * progressSmoothing));
        barColorImage.color = Color.Lerp(barColorImage.color, GetColorFromType(colorType), 1 - Mathf.Exp(-Time.deltaTime * colorSmoothing));
        statusText.text = text;
    }

    private float GetProgress()
    {
        float stageProgress = 0;
        foreach (PythonScriptStatus status in stageLinkedScripts)
            stageProgress += status.Progress;
        stageProgress /= stageLinkedScripts.Count;

        return Mathf.Lerp(stageStart, stageEnd, stageProgress / 100);
    }
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
    private ColorType GetColorType(float progress)
    {
        if (progress >= 1)
            return ColorType.Success;
        return ColorType.InProgress;
    }
    private Color GetColorFromType(ColorType type)
        => type == ColorType.InProgress ? inProgressColor : type == ColorType.Success ? successColor : failureColor;
}
