using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Starts and keeps track of all Python scripts
/// </summary>
public class PythonManager : MonoBehaviour
{
    public static PythonManager Instance { get; private set; }

    /// <summary>
    /// Path to the ArcPy executable, defaults to the below but can be changed using the UI controls in-game
    /// </summary>
    public string PythonPath = "C://Program Files/ArcGIS/Pro/bin/Python/envs/arcgispro-py3/python.exe";

    // All active Python processes
    private ConcurrentBag<PythonScriptStatus> processes = new ConcurrentBag<PythonScriptStatus>();

    /// <summary>
    /// Begins a new Python script, with an accompanying thread to keep track of its status and messages
    /// </summary>
    /// <param name="path">Path to the Python script</param>
    /// <param name="args">Command-line arguments for the script</param>
    /// <param name="status">Optional status object with preconfigured event handlers</param>
    /// <returns>Status object configured with the started process and messages</returns>
    public PythonScriptStatus RunScript(string path, string args = "", PythonScriptStatus status = null)
    {
        if (status == null)
            status = new PythonScriptStatus();

        // Start a thread to keep track of this process's events
        Task.Run(() =>
        {
            // Create and attach the Python process to this thread
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = PythonPath,
                Arguments = $"-u {path} {args}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using (Process process = new Process { StartInfo = start })
            {
                lock (status)
                {
                    status.Process = process;
                    process.OutputDataReceived += (sender, args) =>
                    {
                        lock (status)
                        {
                            string line = args.Data;
                            if (line != null)
                            {
                                // Some output messages are actually progress messages, which report more specific data about the script's execution
                                Match progressMatch = Regex.Match(line.Trim(), @"^Progress(\:|\s)+((\d+(\.\d*)?)\%?(\:|\s)*)?(.+)?$");
                                if (progressMatch.Success)
                                {
                                    // Update the status object with this progress message
                                    if (progressMatch.Groups[3].Success)
                                        status.Progress = float.Parse(progressMatch.Groups[3].Value);
                                    if (progressMatch.Groups[6].Success)
                                        status.LastProgressMessage = progressMatch.Groups[6].Value;

                                    if (status.OnProgress != null)
                                        status.OnProgress(status.Progress, status.LastProgressMessage);
                                }
                                else
                                {
                                    // Update the status object with this output message
                                    status.LastOutputMessage = line;
                                    if (status.OnOutput != null)
                                        status.OnOutput(line);
                                }
                            }
                        }
                    };
                    process.ErrorDataReceived += (sender, args) =>
                    {
                        lock (status)
                        {
                            string line = args.Data;
                            if (line != null)
                            {
                                // Update the status object with this error message
                                status.LastErrorMessage = line;
                                if (status.OnError != null)
                                    status.OnError(line);
                            }
                        }
                    };

                    // Begin the process
                    process.Start();
                    processes.Add(status);
                }
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                lock (status)
                {
                    // After the process has exited, update the status object
                    status.HasExited = true;
                    status.ExitCode = process.ExitCode;
                    if (status.OnExit != null)
                        status.OnExit(status.ExitCode);
                }
            }
        });

        return status;
    }

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// On destroy, close all active Python scripts
    /// </summary>
    private void OnDestroy()
    {
        foreach (PythonScriptStatus status in processes)
            lock (status)
            {
                status.OnProgress = null;
                status.OnOutput = null;
                status.OnError = null;
                status.OnExit = null;
                status.Exit();
            }
    }
}

/// <summary>
/// Contains useful status information about a currently active Python script
/// </summary>
public class PythonScriptStatus
{
    /// <summary>
    /// The system process that this status object references
    /// </summary>
    public Process Process;

    /// <summary>
    /// Last reported progress of the script (read from "Progress XX" messages)
    /// </summary>
    public float Progress = 0;

    /// <summary>
    /// Last reported progress message (read from "Progress MESSAGE" messages)
    /// </summary>
    public string LastProgressMessage = null;

    /// <summary>
    /// Last line read from the process's standard output stream
    /// </summary>
    public string LastOutputMessage = null;

    /// <summary>
    /// Last line read from the process's standard error stream
    /// </summary>
    public string LastErrorMessage = null;

    /// <summary>
    /// Whether the Python script has exited
    /// </summary>
    public bool HasExited = false;

    /// <summary>
    /// If the Python script has exited, the exit code of the process (0 should be mean success)
    /// </summary>
    public int ExitCode = -1;

    /// <summary>
    /// Whether the Python script is determined to have finished (if it has exited or reported 100% progress)
    /// </summary>
    public bool HasFinished => HasExited || Progress >= 100;

    /// <summary>
    /// Called whenever a new progress number or message is reported
    /// </summary>
    public Action<float, string> OnProgress;

    /// <summary>
    /// Called whenever a new non-progress message is read from standard output
    /// </summary>
    public Action<string> OnOutput;

    /// <summary>
    /// Called whenever an error message is read from standard error
    /// </summary>
    public Action<string> OnError;

    /// <summary>
    /// Called whenever the script exits with its exit code
    /// </summary>
    public Action<int> OnExit;

    /// <summary>
    /// Creates a new blank status object, preconfigured with event handlers
    /// </summary>
    /// <param name="onProgress">Called whenever a new progress number or message is reported</param>
    /// <param name="onOutput">Called whenever a new non-progress message is read from standard output</param>
    /// <param name="onError">Called whenever an error message is read from standard error</param>
    /// <param name="onExit">Called whenever the script exits with its exit code</param>
    public PythonScriptStatus(
        Action<float, string> onProgress = null,
        Action<string> onOutput = null,
        Action<string> onError = null,
        Action<int> onExit = null)
    {
        OnProgress = onProgress;
        OnOutput = onOutput;
        OnError = onError;
        OnExit = onExit;
    }

    /// <summary>
    /// Attempts to gracefully close the Python script
    /// </summary>
    public void Exit()
    {
        lock (this)
            try
            {
                Process.Close();
            }
            catch { }
    }

    /// <summary>
    /// Forcibly instantly closes the Python script
    /// </summary>
    public void Kill()
    {
        lock (this)
            try
            {
                Process.Kill();
            }
            catch { }
    }
}