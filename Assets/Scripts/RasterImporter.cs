using System.IO;
using UnityEngine;
using BitMiracle.LibTiff.Classic;
using System;
using System.Text;

public class RasterImporter : MonoBehaviour
{
    public static RasterImporter Instance { get; private set; }

    [SerializeField] private string reflectivityPath;
    [SerializeField] private string precipFlagPath;
    [SerializeField] private string lowCloudsPath;
    [SerializeField] private string midCloudsPath;
    [SerializeField] private string highCloudsPath;
    [SerializeField] private string totalCloudsPath;
    [SerializeField] private string cloudLevelPath;
    public string TimestampPath;
    public Texture2D ReflectivityTexture;
    public Texture2D PrecipFlagTexture;
    public Texture2D LowCloudsTexture;
    public Texture2D MidCloudsTexture;
    public Texture2D HighCloudsTexture;
    public Texture2D TotalCloudsTexture;
    public Texture2D CloudLevelTexture;
    public DateTime Timestamp;

    private Texture2D ImportTexture(string path, TextureFormat format)
    {
        Texture2D tex;
        string ext = Path.GetExtension(path).ToLower();

        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".exr")
        {
            byte[] data = File.ReadAllBytes(path);

            tex = new Texture2D(1, 1, TextureFormat.ARGB32, false, false);
            tex.LoadImage(data);

            Color[] pixels = tex.GetPixels();
            tex.Reinitialize(tex.width, tex.height, format, false);
            tex.SetPixels(pixels);
            tex.Apply();
        }
        else if (ext == ".tif")
            using (var tif = Tiff.Open(path, "r"))
            {
                Tiff.SetErrorHandler(new ThrowTiffErrorHandler());
                int width = tif.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                int height = tif.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                short bitsPerSample = tif.GetField(TiffTag.BITSPERSAMPLE)[0].ToShort();
                short samplesPerPixel = tif.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToShort();
                SampleFormat sampleFormat = (SampleFormat)tif.GetField(TiffTag.SAMPLEFORMAT)[0].Value;
                if (bitsPerSample != 32)
                    throw new InvalidDataException($"TIFF must have 32 bits per sample, but it has {bitsPerSample}");
                if (samplesPerPixel != 1)
                    throw new InvalidDataException($"TIFF must have 1 sample per pixel, but it has {samplesPerPixel}");
                if (sampleFormat != SampleFormat.IEEEFP)
                    throw new InvalidDataException($"TIFF must be in floating-point format, but it is in {sampleFormat} format");

                tex = new Texture2D(width, height, format, false, false);
                Color[] pixels = new Color[width * height];
                if (!tif.IsTiled())
                {
                    byte[] buffer = new byte[tif.ScanlineSize()];
                    for (int y = 0; y < height; y++)
                    {
                        tif.ReadScanline(buffer, y);
                        for (int x = 0; x < width; x++)
                            tex.SetPixel(x, y, new Color(BitConverter.ToSingle(buffer, x * sizeof(float)), 0, 0));
                    }
                }
                else
                {
                    int tileWidth = tif.GetField(TiffTag.TILEWIDTH)[0].ToInt();
                    int tileHeight = tif.GetField(TiffTag.TILELENGTH)[0].ToInt();
                    byte[] buffer = new byte[tif.TileSize()];
                    for (int ty = 0; ty < height; ty += tileHeight)
                        for (int tx = 0; tx < width; tx += tileWidth)
                        {
                            tif.ReadTile(buffer, 0, tx, ty, 0, 0);
                            for (int x = 0; x < tileWidth && x + tx < width; x++)
                                for (int y = 0; y < tileHeight && y + ty < height; y++)
                                {
                                    int start = (y * tileWidth + x) * sizeof(float);
                                    pixels[(height - 1 - y - ty) * width + x + tx] = new Color(BitConverter.ToSingle(buffer, start), 0, 0);
                                }
                        }

                }
                tex.SetPixels(pixels);
                tex.Apply();
            }
        else
            throw new NotSupportedException($"File type {ext} is not supported");

        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        return tex;
    }

    public bool HasValidTextures()
    {
        string[] paths = { reflectivityPath, precipFlagPath, lowCloudsPath, midCloudsPath, highCloudsPath, totalCloudsPath, cloudLevelPath, TimestampPath };
        foreach (string path in paths)
            if (!File.Exists(path))
                return false;
        return true;
    }

    public void ImportTextures()
    {
        if (!HasValidTextures())
            return;

        ReflectivityTexture = ImportTexture(reflectivityPath, TextureFormat.R8);

        PrecipFlagTexture = ImportTexture(precipFlagPath, TextureFormat.R8);

        LowCloudsTexture = ImportTexture(lowCloudsPath, TextureFormat.RFloat);
        TextureUtility.PixelOperator(LowCloudsTexture, (x, y, c) => c / 100);
        LowCloudsTexture.Apply();

        MidCloudsTexture = ImportTexture(midCloudsPath, TextureFormat.RFloat);
        TextureUtility.PixelOperator(MidCloudsTexture, (x, y, c) => c / 100);
        MidCloudsTexture.Apply();

        HighCloudsTexture = ImportTexture(highCloudsPath, TextureFormat.RFloat);
        TextureUtility.PixelOperator(HighCloudsTexture, (x, y, c) => c / 100);
        HighCloudsTexture.Apply();

        TotalCloudsTexture = ImportTexture(totalCloudsPath, TextureFormat.RFloat);
        TextureUtility.PixelOperator(TotalCloudsTexture, (x, y, c) => c / 100);
        TotalCloudsTexture.Apply();

        CloudLevelTexture = ImportTexture(cloudLevelPath, TextureFormat.RFloat);

        Timestamp = DateTime.FromFileTimeUtc(long.Parse(File.ReadAllText(TimestampPath)));
    }

    private void Awake()
    {
        Instance = this;
    }
}

public class ThrowTiffErrorHandler : TiffErrorHandler
{
    public override void ErrorHandler(Tiff tif, string method, string format, params object[] args)
    {
        StringBuilder s = new StringBuilder();
        if (method != null)
        {
            s.Append(string.Format("{0}: ", method));
        }

        s.Append(string.Format(format, args));
        s.Append("\n");

        throw new Exception(s.ToString());
    }

    public override void ErrorHandlerExt(Tiff tif, object clientData, string method, string format, params object[] args)
    {
        StringBuilder s = new StringBuilder();
        s.Append(clientData);
        if (method != null)
        {
            s.Append(string.Format("{0}: ", method));
        }

        s.Append(string.Format(format, args));
        s.Append("\n");

        throw new Exception(s.ToString());
    }

    public override void WarningHandler(Tiff tif, string method, string format, params object[] args)
    {
        StringBuilder s = new StringBuilder();
        if (method != null)
        {
            s.Append(string.Format("{0}: ", method));
        }

        s.Append(string.Format(format, args));
        s.Append("\n");

        Debug.LogWarning(s.ToString());
    }

    public override void WarningHandlerExt(Tiff tif, object clientData, string method, string format, params object[] args)
    {
        StringBuilder s = new StringBuilder();
        s.Append(clientData);
        if (method != null)
        {
            s.Append(string.Format("{0}: ", method));
        }

        s.Append(string.Format(format, args));
        s.Append("\n");

        Debug.LogWarning(s.ToString());
    }
}