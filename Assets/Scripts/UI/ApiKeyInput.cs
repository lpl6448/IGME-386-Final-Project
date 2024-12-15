using System;
using UnityEngine;
using UnityEngine.Networking;

public class ApiKeyInput : ValidationInput
{
    public static ApiKeyInput Instance { get; private set; }

    [SerializeField] private string defaultApiKey;
    [SerializeField] private string testUrl;
    [SerializeField] private float testInterval;

    public string ApiKey { get; private set; }
    private UnityWebRequest req;
    private bool shouldTest;
    private float lastTestFinishTime;

    public override void Validate()
    {
        StopAllCoroutines();
        shouldTest = true;
    }
    private void Update()
    {
        if (shouldTest && req == null && Time.time - lastTestFinishTime > testInterval)
        {
            shouldTest = false;
            req = UnityWebRequest.Get(testUrl + input.text);
            req.SendWebRequest();

            SetStatus(StatusType.Loading, "Validating...");
        }
        else if (req != null && req.isDone)
        {
            lastTestFinishTime = Time.time;

            if (req.responseCode != 200)
            {
                SetStatus(StatusType.Invalid, "Error while contacting the ArcGIS API");
                req = null;
                return;
            }

            try
            {
                ResponseObject res = JsonUtility.FromJson<ResponseObject>(req.downloadHandler.text);
                if (res.error != null && res.error.code != 0)
                {
                    SetStatus(StatusType.Invalid, "Invalid API key");
                    req = null;
                    return;
                }

                ApiKey = input.text;
                SetStatus(StatusType.Valid, "API key validated");
                PlayerPrefs.SetString("386-api-key", ApiKey);
                PlayerPrefs.Save();
                req = null;
            }
            catch
            {
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
        input.SetTextWithoutNotify(PlayerPrefs.GetString("386-api-key", defaultApiKey));
        lastTestFinishTime = -lastTestFinishTime - 1;
        Validate();
    }

    private void Awake()
    {
        Instance = this;
    }
}
