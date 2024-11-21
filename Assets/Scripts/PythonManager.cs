using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

public class PythonManager : MonoBehaviour
{
    public string PythonPath = "C://Program Files/ArcGIS/Pro/bin/Python/envs/arcgispro-py3/python.exe";

    public async void Test()
    {
        ProcessStartInfo start = new ProcessStartInfo
        {
            FileName = PythonPath,
            Arguments = "Python/test.py hi",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using (Process process = new Process { StartInfo = start })
        {
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            print(output);
        }

    }

    private void Start()
    {
        Task.Run(() => Test());
    }
}