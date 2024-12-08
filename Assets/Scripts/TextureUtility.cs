using System;
using Unity.Mathematics;
using UnityEngine;

public static class TextureUtility
{
    public static void PixelOperator(Texture2D tex, Func<int, int, Color, Color> op)
    {
        for (int x = 0; x < tex.width; x++)
            for (int y = 0; y < tex.height; y++)
            {
                Color c = tex.GetPixel(x, y);
                tex.SetPixel(x, y, op(x, y, c));
            }
    }
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

    // Thanks ChatGPT
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
