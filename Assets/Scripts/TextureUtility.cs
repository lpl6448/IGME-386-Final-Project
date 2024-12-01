using System;
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
}
