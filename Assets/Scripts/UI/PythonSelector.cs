using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PythonSelector : MonoBehaviour
{
    [SerializeField] private TMP_InputField pathInput;
    [SerializeField] private string defaultPath;

    [SerializeField] private Image validationOutline;
    [SerializeField] private TMP_Text validationText;
    [SerializeField] private Button launchButton;
    [SerializeField] private Button bypassButton;

    [SerializeField] private Color validColor;
    [SerializeField] private Color testingColor;
    [SerializeField] private Color invalidColor;

    private PythonScriptStatus status;

    private void Start()
    {
        pathInput.text = PlayerPrefs.GetString("386-python-path", defaultPath);
        bypassButton.interactable = RasterImporter.Instance.HasValidTextures();
    }

    public void ValidatePath()
    {
        StopAllCoroutines();
        if (status != null)
        {
            status.Kill();
            status = null;
        }

        StartCoroutine(ValidatePathCrt(pathInput.text));
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

    enum StatusType
    {
        Invalid,
        Loading,
        Valid,
    }

    private void SetStatus(StatusType type, string message)
    {
        Color validationColor = type == StatusType.Invalid ? invalidColor : type == StatusType.Loading ? testingColor : validColor;
        validationText.text = message;
        validationText.color = validationColor;
        validationOutline.color = validationColor;

        launchButton.interactable = type == StatusType.Valid;
    }
}