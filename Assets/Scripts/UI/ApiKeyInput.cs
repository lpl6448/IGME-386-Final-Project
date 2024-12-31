using System;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Checks user input for a valid API key and sends test requests to the ArcGIS REST API to fully validate it
/// </summary>
public class ApiKeyInput : ValidationInput
{
    public static ApiKeyInput Instance { get; private set; }

    [SerializeField] private string defaultApiKey; // API key used by default
    [SerializeField] private string testUrl; // URL to query, API key is appended to the end
    [SerializeField] private float testInterval; // Minimum time (seconds) between each URL query

    /// <summary>
    /// Most recently validated API key, only well-defined if the input is valid
    /// </summary>
    public string ApiKey { get; private set; }

    private UnityWebRequest req; // Current request that is waiting for a response
    private bool shouldTest; // Whether a new input has been entered that needs to be checked
    private float lastTestFinishTime; // Game time of the last response, used to enforce testInterval

    /// <summary>
    /// Begins a validation check by sending a request to the test URL
    /// </summary>
    public override void Validate()
    {
        shouldTest = true;
    }

    private void Update()
    {
        // If a new input needs to be checked and enough time has passed since the last check, begin a new check
        if (shouldTest && req == null && Time.time - lastTestFinishTime > testInterval)
        {
            shouldTest = false;
            req = UnityWebRequest.Get(testUrl + input.text);
            req.SendWebRequest();

            SetStatus(StatusType.Loading, "Validating...");
        }
        // Otherwise, check the current request's status
        else if (req != null && req.isDone)
        {
            lastTestFinishTime = Time.time;

            // If anything other than a 200 OK is returned, there was an error
            if (req.responseCode != 200)
            {
                SetStatus(StatusType.Invalid, "Error while contacting the ArcGIS API");
                req = null;
                return;
            }

            try
            {
                ResponseObject res = JsonUtility.FromJson<ResponseObject>(req.downloadHandler.text);
                
                // If the response has an error code, it is likely due to an invalid API key
                if (res.error != null && res.error.code != 0)
                {
                    SetStatus(StatusType.Invalid, "Invalid API key");
                    req = null;
                    return;
                }

                // Otherwise, a valid response was returned and the API key works
                ApiKey = input.text;
                SetStatus(StatusType.Valid, "API key validated");
                PlayerPrefs.SetString("386-api-key", ApiKey);
                PlayerPrefs.Save();
                req = null;
            }
            catch
            {
                // Generic catch-all for any unexpected errors
                SetStatus(StatusType.Invalid, "Error while contacting the ArcGIS API");
                req = null;
                return;
            }
        }
    }
    [Serializable]
    private class ResponseObject
    {
        public ResponseError error;
    }
    [Serializable]
    private class ResponseError
    {
        public int code;
    }

    private void OnEnable()
    {
        // Initialize the input field using the last saved API key and begin validation
        input.SetTextWithoutNotify(PlayerPrefs.GetString("386-api-key", defaultApiKey));
        lastTestFinishTime = -lastTestFinishTime - 1;
        Validate();
    }

    private void Awake()
    {
        Instance = this;
    }
}
