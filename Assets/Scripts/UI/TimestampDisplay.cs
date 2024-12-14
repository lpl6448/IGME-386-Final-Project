using System;
using TMPro;
using UnityEngine;

public class TimestampDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    private void Update()
    {
        if (RasterImporter.Instance != null && RasterImporter.Instance.Timestamp > new DateTime())
        {
            DateTime localTimestamp = RasterImporter.Instance.Timestamp.ToLocalTime();
            text.text = localTimestamp.ToString("MM/dd/yyyy h:mmtt").ToLower();
        }
    }
}