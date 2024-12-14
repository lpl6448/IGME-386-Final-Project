using System;
using UnityEngine;

public class TimestampInput : ValidationInput
{
    [SerializeField] private string startTimestamp;
    [SerializeField] private float archiveDelayHours;

    public DateTime Timestamp { get; private set; }
    public bool UseTimestamp { get; private set; }

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
        SetStatus(StatusType.Valid, $"Retrieving archive data for {time.ToString()} ({TimeZoneInfo.Local.StandardName})");
    }
}