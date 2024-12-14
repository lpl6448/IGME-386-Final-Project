using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class PythonSelector : ValidationInput
{
    [SerializeField] private string defaultPath;

    private PythonScriptStatus status;

    private void OnEnable()
    {
        input.SetTextWithoutNotify(PlayerPrefs.GetString("386-python-path", defaultPath));
        Validate();
    }

    public override void Validate()
    {
        StopAllCoroutines();
        if (status != null)
        {
            status.Kill();
            status = null;
        }

        StartCoroutine(ValidatePathCrt(input.text));
    }
    private IEnumerator ValidatePathCrt(string path)
    {
        if (!File.Exists(path))
        {
            SetStatus(StatusType.Invalid, "File does not exist");
            yield break;
        }

        if (Path.GetFileName(path) != "python.exe")
        {
            SetStatus(StatusType.Invalid, "Path must point to a valid python.exe");
            yield break;
        }

        SetStatus(StatusType.Loading, "Loading...");

        PythonManager.Instance.PythonPath = path;
        status = PythonManager.Instance.RunScript("Python/ValidatePythonExe.py", "");
        while (!status.HasExited && string.IsNullOrEmpty(status.LastProgressMessage))
            yield return null;
        if (status.ExitCode > 0 || string.IsNullOrEmpty(status.LastProgressMessage))
        {
            SetStatus(StatusType.Invalid, "arcpy is not installed in this Python executable");
            yield break;
        }

        SetStatus(StatusType.Valid, "Using " + status.LastProgressMessage);

        PlayerPrefs.SetString("386-python-path", path);
        PlayerPrefs.Save();
    }
}