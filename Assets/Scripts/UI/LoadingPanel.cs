using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class LoadingPanel : MonoBehaviour
{
    public enum LoadState
    {
        InProgress,
        Success,
        Failure,
    }

    [SerializeField] private UnityEvent onSuccess;
    [SerializeField] private UnityEvent onCancel;

    public LoadState CurrentState { get; private set; }

    [SerializeField] private LoadingBar radar;
    [SerializeField] private LoadingBar clouds;
    [SerializeField] private TMP_Text loadingText;

    private bool[] attemptsSuccesses = new bool[2];
    private LoadState radarState;
    private LoadState cloudsState;
    private List<PythonScriptStatus> activeScripts = new List<PythonScriptStatus>();
    private DateTime startTimestamp;

    public void Load()
    {
        StartCoroutine(LoadCrt());
    }
    public void Cancel()
    {
        radar.Initialize();
        clouds.Initialize();
        foreach (PythonScriptStatus status in activeScripts)
            status.Exit();
        StopAllCoroutines();

        onCancel.Invoke();
    }

    private IEnumerator LoadCrt()
    {
        loadingText.text = "Loading weather data...";
        CurrentState = LoadState.InProgress;
        if (File.Exists(RasterImporter.Instance.TimestampPath))
            File.Delete(RasterImporter.Instance.TimestampPath);
        radar.Initialize();
        clouds.Initialize();
        startTimestamp = DateTime.UtcNow;

        StartCoroutine(RadarCrt());
        StartCoroutine(CloudsCrt());

        while (CurrentState == LoadState.InProgress)
        {
            if (radarState == LoadState.Failure || cloudsState == LoadState.Failure)
                CurrentState = LoadState.Failure;
            else if (radarState == LoadState.Success && cloudsState == LoadState.Success)
                CurrentState = LoadState.Success;
            yield return null;
        }

        if (CurrentState == LoadState.Success)
        {
            loadingText.text = "Finishing up...";
            File.WriteAllText(RasterImporter.Instance.TimestampPath, startTimestamp.ToString());
            yield return new WaitForSeconds(1);
            onSuccess.Invoke();
        }
        else if (CurrentState == LoadState.Failure)
        {
            loadingText.text = "Failed!";
        }
    }
    private IEnumerator RadarCrt()
    {
        radarState = LoadState.InProgress;

        yield return RunScriptMultipleAttempts(radar, 0, 1, 0, "Python/DataProcessing.py");
        if (!attemptsSuccesses[0])
        {
            radar.OverrideStatus("Failed to download and process data!", LoadingBar.ColorType.Failure);
            radarState = LoadState.Failure;
            yield break;
        }

        radarState = LoadState.Success;
    }

    private IEnumerator CloudsCrt()
    {
        cloudsState = LoadState.InProgress;

        yield return RunScriptMultipleAttempts(clouds, 0, 0.3f, 1, "Python/CloudDataDownload.py");
        if (!attemptsSuccesses[1])
        {
            clouds.OverrideStatus("Failed to download data!", LoadingBar.ColorType.Failure);
            cloudsState = LoadState.Failure;
            yield break;
        }

        PythonScriptStatus status = PythonManager.Instance.RunScript("Python/CloudDataVariablesProcessing.py");
        activeScripts.Add(status);
        clouds.StartStage(0.3f, 1, status);
        while (!status.HasFinished)
            yield return null;
        activeScripts.Remove(status);

        if (status.ExitCode > 0)
        {
            clouds.OverrideStatus("Failed to process data!", LoadingBar.ColorType.Failure);
            cloudsState = LoadState.Failure;
            yield break;
        }

        cloudsState = LoadState.Success;
    }

    private IEnumerator RunScriptMultipleAttempts(LoadingBar bar, float start, float end, int key, string path, string args = "")
    {
        attemptsSuccesses[key] = false;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            PythonScriptStatus status = PythonManager.Instance.RunScript(path, args);
            activeScripts.Add(status);
            bar.StartStage(start, end, status);

            while (!status.HasFinished)
                yield return null;
            activeScripts.Remove(status);

            if (status.ExitCode > 0)
            {
                bar.OverrideStatus("Retrying...", LoadingBar.ColorType.Failure);
                yield return new WaitForSeconds(1.5f);
            }
            else
            {
                attemptsSuccesses[key] = true;
                break;
            }
        }
    }
}