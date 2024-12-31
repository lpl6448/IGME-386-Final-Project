using System;
using TMPro;
using UnityEngine;

/// <summary>
/// UI element that displays the timestamp of the currently loaded map data
/// </summary>
public class TimestampDisplay : MonoBehaviour
{
    // References
    [SerializeField] private TMP_Text text;

    private void Update()
    {
        // If there is valid map data, display its timestamp in a user-readable format
        if (RasterImporter.Instance != null && RasterImporter.Instance.Timestamp > new DateTime())
        {
            DateTime localTimestamp = RasterImporter.Instance.Timestamp.ToLocalTime();
            text.text = localTimestamp.ToString("MM/dd/yyyy h:mmtt").ToLower();
        }
    }
}