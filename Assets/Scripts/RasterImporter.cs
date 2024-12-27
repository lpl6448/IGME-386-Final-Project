using System.IO;
using UnityEngine;
using BitMiracle.LibTiff.Classic;
using System;
using System.Collections.Generic;

/// <summary>
/// Contains functions for importing textures from their respective file paths
/// </summary>
public class RasterImporter : MonoBehaviour
{
    public static RasterImporter Instance { get; private set; }

    // Paths to all image files created by the Python scripts
    [SerializeField] private string reflectivityPath;
    [SerializeField] private string precipFlagPath;
    [SerializeField] private string lowCloudsPath;
    [SerializeField] private string midCloudsPath;
    [SerializeField] private string highCloudsPath;
    [SerializeField] private string totalCloudsPath;
    [SerializeField] private string cloudLevelPath;
    public string TimestampPath; // Path to the file containing the timestamp that the imported data references

    // References to all textures, created at runtime
    public Texture2D ReflectivityTexture;
    public Texture2D PrecipFlagTexture;
    public Texture2D LowCloudsTexture;
    public Texture2D MidCloudsTexture;
    public Texture2D HighCloudsTexture;
    public Texture2D TotalCloudsTexture;
    public Texture2D CloudLevelTexture;
    public DateTime Timestamp; // Timestamp that the imported data references

    // List of all created textures
    private List<Texture2D> texHandles = new List<Texture2D>();

    /// <summary>
    /// Imports an image file into a new texture with the specified format
    /// </summary>
    /// <param name="path">Path to the image file, parsed based on its file extension</param>
    /// <param name="format">Pixel format of the resulting texture</param>
    /// <returns>New texture with the imported image data, in the specified format</returns>
    /// <exception cref="InvalidDataException">TIFF file has invalid metadata</exception>
    /// <exception cref="NotSupportedException">The file type is not supported by this importer</exception>
    private Texture2D ImportTexture(string path, TextureFormat format)
    {
        Texture2D tex;
        string ext = Path.GetExtension(path).ToLower();

        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".exr")
        {
            byte[] data = File.ReadAllBytes(path);

            // Unity can natively load images from these file types at runtime
            tex = new Texture2D(1, 1, TextureFormat.ARGB32, false, false);
            tex.LoadImage(data);

            // Cast the texture to the desired pixel format
            Color[] pixels = tex.GetPixels();
            tex.Reinitialize(tex.width, tex.height, format, false);
            tex.SetPixels(pixels);
            tex.Apply();
        }
        else if (ext == ".tif") // Import TIFF files using an external library
            using (var tif = Tiff.Open(path, "r"))
            {
                // Get and verify the file's metadata
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

                // Import the image data
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

        // Set required attributes on the output texture
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        texHandles.Add(tex);
        return tex;
    }

    /// <summary>
    /// Checks if every required texture and metadata file exists
    /// </summary>
    /// <returns>Whether every required texture and metadata file exists</returns>
    public bool HasValidTextures()
    {
        string[] paths = { reflectivityPath, precipFlagPath, lowCloudsPath, midCloudsPath, highCloudsPath, totalCloudsPath, cloudLevelPath, TimestampPath };
        foreach (string path in paths)
            if (!File.Exists(path))
                return false;
        return true;
    }

    /// <summary>
    /// Imports all textures from their corresponding files, with the correct texture formats
    /// </summary>
    public void ImportTextures()
    {
        // Only run successfully if all required files exist
        if (!HasValidTextures())
            return;

        // Release the current textures to avoid memory leaks
        DestroyTextures();

        // Import all textures
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

        // Import the data timestamp from its file
        Timestamp = DateTime.FromFileTimeUtc(long.Parse(File.ReadAllText(TimestampPath)));
    }

    /// <summary>
    /// Disposes of all created textures to avoid memory leaks
    /// </summary>
    private void DestroyTextures()
    {
        foreach (Texture2D tex in texHandles)
            Destroy(tex);
    }

    private void Awake()
    {
        Instance = this;
    }
    private void OnDestroy()
    {
        DestroyTextures();
    }
}
