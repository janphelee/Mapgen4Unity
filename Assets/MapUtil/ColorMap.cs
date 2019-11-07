using UnityEngine;

namespace Assets.MapGen
{
    class ColorMap
    {
        const int width = 64;
        const int height = 64;

        static Color32[] pixels { get; set; }

        static ColorMap()
        {
            pixels = new Color32[width * height];

            for (int y = 0, p = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float e = 2 * (float)x / width - 1,
                        m = (float)y / height;

                    byte r, g, b;

                    if (x == width / 2 - 1)
                    {
                        r = 48;
                        g = 120;
                        b = 160;
                    }
                    else if (x == width / 2 - 2)
                    {
                        r = 48;
                        g = 100;
                        b = 150;
                    }
                    else if (x == width / 2 - 3)
                    {
                        r = 48;
                        g = 80;
                        b = 140;
                    }
                    else if (e < 0)
                    {
                        r = (byte)(48 + 48 * e);
                        g = (byte)(64 + 64 * e);
                        b = (byte)(127 + 127 * e);
                    }
                    else
                    { // adapted from terrain-from-noise article
                        m = m * (1 - e); // higher elevation holds less moisture; TODO: should be based on slope, not elevation

                        r = (byte)(210 - 100 * m);
                        g = (byte)(185 - 45 * m);
                        b = (byte)(139 - 45 * m);
                        r = (byte)(255 * e + r * (1 - e));
                        g = (byte)(255 * e + g * (1 - e));
                        b = (byte)(255 * e + b * (1 - e));
                    }

                    pixels[p++] = new Color32(r, g, b, 255);
                }
            }

        }

        public static Texture texture()
        {
            var tex = new Texture2D(width, height);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }
    }
}
