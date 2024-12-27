using System;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Contains utility functions for projecting and convoluting textures
/// </summary>
public static class TextureUtility
{
    /// <summary>
    /// Reprojects the input texture into an output texture with a new extent, using bilinear filtering
    /// </summary>
    /// <param name="baseTex">Input texture</param>
    /// <param name="baseMin">Minimum of the current extent</param>
    /// <param name="baseMax">Maximum of the current extent</param>
    /// <param name="newTex">Output texture, overwritten to contain the projected data</param>
    /// <param name="newMin">Minimum of the new extent</param>
    /// <param name="newMax">Maximum of the new extent</param>
    public static void ReprojectTexture(Texture2D baseTex, float2 baseMin, float2 baseMax,
        Texture2D newTex, float2 newMin, float2 newMax)
    {
        for (int x = 0; x < newTex.width; x++)
            for (int y = 0; y < newTex.height; y++)
            {
                float2 newTexUV = (new float2(x, y) + 0.5f) / new float2(newTex.width, newTex.height);
                float2 baseTexUV = math.unlerp(baseMin, baseMax, math.lerp(newMin, newMax, newTexUV));
                newTex.SetPixel(x, y, baseTex.GetPixelBilinear(baseTexUV.x, baseTexUV.y));
            }
    }

    /// <summary>
    /// Reprojects the input texture into an output texture with a new extent, using nearest neighbor filtering
    /// </summary>
    /// <param name="baseTex">Input texture</param>
    /// <param name="baseMin">Minimum of the current extent</param>
    /// <param name="baseMax">Maximum of the current extent</param>
    /// <param name="newTex">Output texture, overwritten to contain the projected data</param>
    /// <param name="newMin">Minimum of the new extent</param>
    /// <param name="newMax">Maximum of the new extent</param>
    public static void ReprojectTextureNearestNeighbor(Texture2D baseTex, float2 baseMin, float2 baseMax,
        Texture2D newTex, float2 newMin, float2 newMax)
    {
        for (int x = 0; x < newTex.width; x++)
            for (int y = 0; y < newTex.height; y++)
            {
                float2 newTexUV = (new float2(x, y) + 0.5f) / new float2(newTex.width, newTex.height);
                float2 baseTexUV = math.unlerp(baseMin, baseMax, math.lerp(newMin, newMax, newTexUV));
                newTex.SetPixel(x, y, baseTex.GetPixel(Mathf.RoundToInt(baseTexUV.x * baseTex.width), Mathf.RoundToInt(baseTexUV.y * baseTex.height)));
            }
    }

    /// <summary>
    /// Performs the specified operation on every pixel of the input texture
    /// </summary>
    /// <param name="tex">Texture to modify</param>
    /// <param name="op">Function taking in the x and y coordinates and current color of each pixel and returning a new color</param>
    public static void PixelOperator(Texture2D tex, Func<int, int, Color, Color> op)
    {
        for (int x = 0; x < tex.width; x++)
            for (int y = 0; y < tex.height; y++)
            {
                Color c = tex.GetPixel(x, y);
                tex.SetPixel(x, y, op(x, y, c));
            }
    }

    /// <summary>
    /// Sets each pixel of the texture to the maximum value of a region of surrounding pixels defined by kernelSize
    /// </summary>
    /// <param name="tex">Texture to modify</param>
    /// <param name="kernelSize">Size (odd number) of the region to select maximum values from</param>
    public static void MaxConvolution(Texture2D tex, int kernelSize)
    {
        int kernelExtent = kernelSize / 2;
        Color[] oldPixels = tex.GetPixels();
        for (int x = 0; x < tex.width; x++)
            for (int y = 0; y < tex.height; y++)
            {
                Color c = new Color();
                for (int dx = -kernelExtent; dx <= kernelExtent; dx++)
                    for (int dy = -kernelExtent; dy <= kernelExtent; dy++)
                        {
                            int kx = Mathf.Clamp(x + dx, 0, tex.width - 1);
                            int ky = Mathf.Clamp(y + dy, 0, tex.height - 1);
                            Color o = oldPixels[ky * tex.width + kx];
                            c.r = math.max(o.r, c.r);
                            c.g = math.max(o.g, c.g);
                            c.b = math.max(o.b, c.b);
                            c.a = math.max(o.a, c.a);
                        }
                tex.SetPixel(x, y, c);
            }
    }

    /// <summary>
    /// Performs a convolution on the texture using the specified kernel
    /// </summary>
    /// <param name="tex">Texture to modify</param>
    /// <param name="kernel">Kernel (odd-length 2D array) of weights used to sum up surrounding pixels into a final result</param>
    public static void Convolution(Texture2D tex, float[,] kernel)
    {
        int kernelExtent = kernel.GetLength(0) / 2;
        Color[] oldPixels = tex.GetPixels();
        for (int x = 0; x < tex.width; x++)
            for (int y = 0; y < tex.height; y++)
            {
                Color c = new Color();
                for (int dx = -kernelExtent; dx <= kernelExtent; dx++)
                    for (int dy = -kernelExtent; dy <= kernelExtent; dy++)
                        {
                            int kx = Mathf.Clamp(x + dx, 0, tex.width - 1);
                            int ky = Mathf.Clamp(y + dy, 0, tex.height - 1);
                            c += oldPixels[ky * tex.width + kx] * kernel[dx + kernelExtent, dy + kernelExtent];
                        }
                tex.SetPixel(x, y, c);
            }
    }

    /// <summary>
    /// Creates a kernel that will perform a Gaussian blur on a texture.
    /// Assisted by ChatGPT
    /// </summary>
    /// <param name="kernelSize">Kernel size (odd number) of the region of pixels to consider, or the blur size</param>
    /// <returns>2D array containing the kernel weights</returns>
    /// <exception cref="ArgumentException">The kernel size is not a positive odd number</exception>
    public static float[,] GenerateGaussianKernel(int kernelSize)
    {
        if (kernelSize % 2 == 0 || kernelSize <= 0)
        {
            throw new ArgumentException("Kernel size must be a positive odd number.");
        }

        float sigma = 0.3f * ((kernelSize - 1) * 0.5f - 1) + 0.8f;

        float[,] kernel = new float[kernelSize, kernelSize];
        int radius = kernelSize / 2;
        float sigmaSquared = sigma * sigma;
        float sum = 0;

        // Generate the kernel values
        for (int x = -radius; x <= radius; x++)
            for (int y = -radius; y <= radius; y++)
            {
                float exponent = -(x * x + y * y) / (2 * sigmaSquared);
                kernel[x + radius, y + radius] = Mathf.Exp(exponent);
                sum += kernel[x + radius, y + radius];
            }

        // Normalize the kernel
        for (int i = 0; i < kernelSize; i++)
            for (int j = 0; j < kernelSize; j++)
            {
                kernel[i, j] /= sum;
            }

        return kernel;
    }
}
