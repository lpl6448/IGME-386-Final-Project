using System;
using UnityEngine;

/// <summary>
/// Checks user input for valid DateTime timestamps within the valid range of archive data
/// </summary>
public class TimestampInput : ValidationInput
{
    [SerializeField] private string startTimestamp; // Earliest timestamp that archive data can be downloaded for
    [SerializeField] private float archiveDelayHours; // A safe buffer (hours) before the current time to ensure that archive data has been uploaded

    /// <summary>
    /// Last valid timestamp, only well-defined if the input is a valid timestamp
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Whether the user intends to use an archive timstamp (that is, whether they have entered a valid timestamp),
    /// only well-defined if the input is a valid timestamp (true) or empty (false)
    /// </summary>
    public bool UseTimestamp { get; private set; }

    /// <summary>
    /// Checks the current input and updates validation status and feedback accordingly.
    /// If the input is empty, then it is valid and the user intends to use the most recent data, rather than the archive.
    /// If the input is a valid timestamp within the archive range, then it is valid and the user intends to use the archive.
    /// </summary>
    public override void Validate()
    {
        if (string.IsNullOrWhiteSpace(input.text))
        {
            UseTimestamp = false;
            SetStatus(StatusType.Valid, "");
            return;
        }

        if (!DateTime.TryParse(input.text, out DateTime time))
        {
            SetStatus(StatusType.Invalid, $"Invalid timestamp");
            return;
        }

        if (time < DateTime.Parse(startTimestamp))
        {
            SetStatus(StatusType.Invalid, $"Archive data is only available after {DateTime.Parse(startTimestamp).ToShortDateString()}");
            return;
        }

        if (time > DateTime.UtcNow - TimeSpan.FromHours(archiveDelayHours))
        {
            SetStatus(StatusType.Invalid, $"Archive data is only available {archiveDelayHours} hours before the current time");
            return;
        }

        Timestamp = time.ToUniversalTime();
        UseTimestamp = true;
        SetStatus(StatusType.Valid, $"Retrieving archive data for {time} ({TimeZoneInfo.Local.StandardName})");
    }
}