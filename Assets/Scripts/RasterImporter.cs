using System.IO;
using UnityEngine;

public class RasterImporter : MonoBehaviour
{
    public static RasterImporter Instance { get; private set; }

    [SerializeField] private string reflectivityPath;
    public Texture2D ReflectivityTexture;

    private Texture2D ImportTexture(string path, TextureFormat format)
    {
        byte[] data = File.ReadAllBytes(path);

        Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false, false);
        tex.LoadImage(data);

        Color[] pixels = tex.GetPixels();
        tex.Reinitialize(tex.width, tex.height, format, false);
        tex.SetPixels(pixels);
        tex.Apply();

        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        return tex;
    }

    private void Awake()
    {
        Instance = this;
        if (File.Exists(reflectivityPath))
            ReflectivityTexture = ImportTexture(reflectivityPath, TextureFormat.R8);
    }
}
