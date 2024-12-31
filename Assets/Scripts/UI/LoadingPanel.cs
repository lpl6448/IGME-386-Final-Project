using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// UI element that runs the required Python scripts and updates loading bars
/// </summary>
public class LoadingPanel : MonoBehaviour
{
    /// <summary>
    /// Contains different status types used for the overall loading progress
    /// </summary>
    public enum LoadState
    {
        InProgress,     // Waiting for all loading processes to complete
        Success,        // All loading processes have completed
        Failure,        // One or more loading processes failed
    }

    /// <summary>
    /// Current state of the overall loading operation
    /// </summary>
    public LoadState CurrentState { get; private set; }

    // References
    [SerializeField] private LoadingBar radar;
    [SerializeField] private LoadingBar clouds;
    [SerializeField] private TMP_Text loadingText;

    [SerializeField] private UnityEvent onSuccess;  // Called when all scripts succeed
    [SerializeField] private UnityEvent onCancel;   // Called if the user cancels the loading operation

    private bool[] attemptsSuccesses = new bool[2]; // Contains whether each multiple-attempt loading process has succeeded or not
    private LoadState radarState;       // Current state of the radar loading scripts
    private LoadState cloudsState;      // Current state of the clouds loading scripts
    private List<PythonScriptStatus> activeScripts = new List<PythonScriptStatus>(); // Handles to all running Python scripts
    private DateTime startTimestamp;    // Timestamp of either the current time or the archive time
    private string archiveTimestamp;    // If not null, the archive timestamp date, formatted to be used as Python arguments

    /// <summary>
    /// Begins loading data for a given archive timestamp
    /// </summary>
    /// <param name="timestamp">Archive timestamp</param>
    public void Load(DateTime timestamp)
    {
        startTimestamp = timestamp;
        archiveTimestamp = timestamp.ToString("yyyyMMdd-HH") + "00";
        StartCoroutine(LoadCrt());
    }

    /// <summary>
    /// Begins loading data for the current timestamp (most recent data)
    /// </summary>
    public void Load()
    {
        startTimestamp = DateTime.UtcNow;
        archiveTimestamp = null;
        StartCoroutine(LoadCrt());
    }

    /// <summary>
    /// Cancels all scripts for the current loading operation
    /// </summary>
    public void Cancel()
    {
        // Reset all state
        radar.Initialize();
        clouds.Initialize();
        foreach (PythonScriptStatus status in activeScripts)
            status.Exit();
        StopAllCoroutines();

        onCancel.Invoke();
    }

    private IEnumerator LoadCrt()
    {
        // Initialize all state
        loadingText.text = "Loading weather data...";
        CurrentState = LoadState.InProgress;
        if (File.Exists(RasterImporter.Instance.TimestampPath))
            File.Delete(RasterImporter.Instance.TimestampPath);
        radar.Initialize();
        clouds.Initialize();

        // Begin both loading processes
        StartCoroutine(RadarCrt());
        StartCoroutine(CloudsCrt());

        // Wait for either both scripts to complete or one to fail
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
            File.WriteAllText(RasterImporter.Instance.TimestampPath, startTimestamp.ToFileTimeUtc().ToString());
            yield return new WaitForSeconds(1); // Wait one second for loading bars to fill completely, since the screen will freeze after onSuccess
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

        // Any scripts involving downloading should be given multiple tries
        yield return RunScriptMultipleAttempts(radar, 0, 1, 0, "Python/DataProcessing.py", archiveTimestamp);
        if (!attemptsSuccesses[0])
        {
            radar.OverrideStatus("Failed to download and process data!", LoadingBar.ColorType.Failure);
            radarState = LoadState.Failure;
            yield break;
        }

        // Once the script has finished, all radar data is prepared
        radarState = LoadState.Success;
    }

    private IEnumerator CloudsCrt()
    {
        cloudsState = LoadState.InProgress;

        // Any scripts involving downloading should be given multiple tries
        yield return RunScriptMultipleAttempts(clouds, 0, 0.3f, 1, "Python/CloudDataDownload.py", archiveTimestamp);
        if (!attemptsSuccesses[1])
        {
            clouds.OverrideStatus("Failed to download data!", LoadingBar.ColorType.Failure);
            cloudsState = LoadState.Failure;
            yield break;
        }

        // After downloading has finished, begin processing
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

    /// <summary>
    /// Runs the given Python script, retrying up to three times upon failure
    /// </summary>
    /// <param name="bar">Loading bar to update</param>
    /// <param name="start">Loading bar's fill amount at the beginning of the stage</param>
    /// <param name="end">Loading bar's fill amount at the end of the stage</param>
    /// <param name="key">Index of attempsSuccesses, so that that entry can be updated with this script's overall success value</param>
    /// <param name="path">Python script to run</param>
    /// <param name="args">Command-line arguments to pass to the Python script</param>
    /// <returns></returns>
    private IEnumerator RunScriptMultipleAttempts(LoadingBar bar, float start, float end, int key, string path, string args = "")
    {
        attemptsSuccesses[key] = false;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            // Begin the script
            PythonScriptStatus status = PythonManager.Instance.RunScript(path, args);
            activeScripts.Add(status);
            bar.StartStage(start, end, status);

            while (!status.HasFinished)
                yield return null;
            activeScripts.Remove(status);

            // If the script fails, retry
            if (status.ExitCode > 0)
            {
                if (attempt < 2)
                    bar.OverrideStatus("Retrying...", LoadingBar.ColorType.Failure);
                yield return new WaitForSeconds(1.5f);
            }
            // If the script succeeds, return
            else
            {
                attemptsSuccesses[key] = true;
                break;
            }
        }
    }
}