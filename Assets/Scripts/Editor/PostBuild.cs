using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

/// <summary>
/// Post-build step where the Python scripts required for data processing are copied to the output directory
/// </summary>
public static class PostBuild
{
    [PostProcessBuild(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        string outDir = Path.Combine(Path.GetDirectoryName(pathToBuiltProject), "Python");
        if (Directory.Exists(outDir))
            Directory.Delete(outDir, true);

        Directory.CreateDirectory(outDir);
        foreach (string file in Directory.GetFiles("Python"))
            File.Copy(file, Path.Combine(outDir, Path.GetFileName(file)));
    }
}
