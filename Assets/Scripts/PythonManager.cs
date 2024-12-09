using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

public class PythonManager : MonoBehaviour
{
    public static PythonManager Instance { get; private set; }

    public string PythonPath = "C://Program Files/ArcGIS/Pro/bin/Python/envs/arcgispro-py3/python.exe";

    private ConcurrentBag<PythonScriptStatus> processes = new ConcurrentBag<PythonScriptStatus>();

    public PythonScriptStatus RunScript(string path, string args = "", PythonScriptStatus status = null)
    {
        if (status == null)
            status = new PythonScriptStatus();

        Task.Run(() =>
        {
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
                                Match progressMatch = Regex.Match(line.Trim(), @"^Progress(\:|\s)+((\d+(\.\d*)?)\%?(\:|\s)*)?(.+)?$");
                                if (progressMatch.Success)
                                {
                                    if (progressMatch.Groups[3].Success)
                                        status.Progress = float.Parse(progressMatch.Groups[3].Value);
                                    if (progressMatch.Groups[6].Success)
                                        status.LastProgressMessage = progressMatch.Groups[6].Value;

                                    if (status.OnProgress != null)
                                        status.OnProgress(status.Progress, status.LastProgressMessage);
                                }
                                else
                                {
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
                                status.LastErrorMessage = line;
                                if (status.OnError != null)
                                    status.OnError(line);
                            }
                        }
                    };

                    process.Start();
                    processes.Add(status);
                }
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                lock (status)
                {
                    status.HasExited = true;
                    status.ExitCode = process.ExitCode;
                    if (status.OnExit != null)
                        status.OnExit(status.ExitCode);
                }
            }
        });

        return status;
    }
    public IEnumerator WaitForScriptToExit(PythonScriptStatus status)
    {
        while (!status.HasExited)
            yield return null;
    }

    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        // PythonScriptStatus status = new PythonScriptStatus(
        //     (f, s) => print("Progress: " + f + " - " + s),
        //     (s) => print("Output: " + s),
        //     (s) => UnityEngine.Debug.LogError(s),
        //     (c) => print("Process exited with code " + c)
        // );
        // RunScript("Python/test.py", "hi", status);
    }
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
public class PythonScriptStatus
{
    public Process Process;
    public float Progress = 0;
    public string LastProgressMessage = null;
    public string LastOutputMessage = null;
    public string LastErrorMessage = null;
    public bool HasExited = false;
    public int ExitCode = -1;

    public bool HasFinished => HasExited || Progress >= 100;

    public Action<float, string> OnProgress;
    public Action<string> OnOutput;
    public Action<string> OnError;
    public Action<int> OnExit;

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

    public void Exit()
    {
        lock (this)
            if (Process != null)
                Process.Close();
    }
    public void Kill()
    {
        lock (this)
            if (Process != null)
                Process.Kill();
    }
}