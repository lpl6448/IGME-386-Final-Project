using System.Collections;
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

    public LoadState CurrentState { get; private set; }

    public LoadingBar Radar;
    public LoadingBar Clouds;

    private bool[] attemptsSuccesses = new bool[2];
    private LoadState radarState;
    private LoadState cloudsState;

    public void Load()
    {
        StartCoroutine(LoadCrt());
    }

    private IEnumerator LoadCrt()
    {
        CurrentState = LoadState.InProgress;
        Radar.Initialize();
        Clouds.Initialize();

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
            yield return new WaitForSeconds(1);
            onSuccess.Invoke();
        }
    }
    private IEnumerator RadarCrt()
    {
        radarState = LoadState.InProgress;

        yield return RunScriptMultipleAttempts(Radar, 0, 1, 0, "Python/DataProcessing.py");
        if (!attemptsSuccesses[0])
        {
            Radar.OverrideStatus("Failed to download and process data!", LoadingBar.ColorType.Failure);
            radarState = LoadState.Failure;
            yield break;
        }

        radarState = LoadState.Success;
    }

    private IEnumerator CloudsCrt()
    {
        cloudsState = LoadState.InProgress;

        yield return RunScriptMultipleAttempts(Clouds, 0, 0.3f, 1, "Python/CloudDataDownload.py");
        if (!attemptsSuccesses[1])
        {
            Clouds.OverrideStatus("Failed to download data!", LoadingBar.ColorType.Failure);
            cloudsState = LoadState.Failure;
            yield break;
        }

        PythonScriptStatus status = PythonManager.Instance.RunScript("Python/CloudDataVariablesProcessing.py");
        Clouds.StartStage(0.3f, 1, status);
        while (!status.HasFinished)
            yield return null;

        if (status.ExitCode > 0)
        {
            Clouds.OverrideStatus("Failed to process data!", LoadingBar.ColorType.Failure);
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
            bar.StartStage(start, end, status);

            while (!status.HasFinished)
                yield return null;

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