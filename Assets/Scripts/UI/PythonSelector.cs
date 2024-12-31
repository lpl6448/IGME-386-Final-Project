using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Checks user input for a valid ArcPy installation by testing scripts
/// </summary>
public class PythonSelector : ValidationInput
{
    [SerializeField] private string defaultPath; // Default Python path on first launch

    private PythonScriptStatus status; // Information on the currently running test script

    private void OnEnable()
    {
        // Set the path to the most recently saved path and validate it
        input.SetTextWithoutNotify(PlayerPrefs.GetString("386-python-path", defaultPath));
        Validate();
    }

    /// <summary>
    /// Begins a validation check by running a basic Python script using the specified ArcPy path and checking its status
    /// </summary>
    public override void Validate()
    {
        // Stop the current ArcPy test and kill any associated Python process
        StopAllCoroutines();
        if (status != null)
        {
            status.Kill();
            status = null;
        }

        // Begin the new ArcPy test
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

        // Run a test script that attempts to import ArcPy and return its version
        PythonManager.Instance.PythonPath = path;
        status = PythonManager.Instance.RunScript("Python/ValidatePythonExe.py", "");
        while (!status.HasExited && string.IsNullOrEmpty(status.LastProgressMessage))
            yield return null;
        if (status.ExitCode > 0 || string.IsNullOrEmpty(status.LastProgressMessage))
        {
            SetStatus(StatusType.Invalid, "arcpy is not installed in this Python executable (or you are not signed in!)");
            yield break;
        }

        // If the script runs successfully, the Python path is validated
        SetStatus(StatusType.Valid, "Using " + status.LastProgressMessage);
        PlayerPrefs.SetString("386-python-path", path);
        PlayerPrefs.Save();
    }
}