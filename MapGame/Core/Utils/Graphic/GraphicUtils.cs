using HelixToolkit.SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MapGame.Core.Utils.Graphic
{
    public static class GraphicUtils
    {
        public static void ColorPixel(byte[] pixels, int pixelIndex, Color color)
        {
            byte blue = color.B, green = color.G, red = color.R, alpha = color.A;
            ColorPixel(pixels, pixelIndex, blue, green, red, alpha);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ColorPixel(byte[] pixels, int index, byte b, byte g, byte r, byte a)
        {
            pixels[index] = b;
            pixels[index + 1] = g;
            pixels[index + 2] = r;
            pixels[index + 3] = a;
        }

        public static TextureModel ToTextureModel(this BitmapSource bitmap)
        {
            if (bitmap == null) return null;

            var stream = new MemoryStream();

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(stream);

            stream.Position = 0;

            return new TextureModel(stream);
        }

        public static TextureModel ToDynamicTextureModel(this WriteableBitmap bitmap)
        {
            if (bitmap == null) return null;
            var stream = new MemoryStream();

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(stream);
            stream.Position = 0;

            return new TextureModel(stream);
        }
    }
}
