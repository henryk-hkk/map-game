using HelixToolkit.SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MapGame.Core.Utils.Graphic
{
    public static class GraphicUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32Rect GetDirtyRect(int minX, int maxX, int minY, int maxY, int margin)
        {
            minX = Math.Max((int)MapContext.MinX, minX - margin);
            minY = Math.Max((int)MapContext.MinY, minY - margin);
            maxX = Math.Min((int)Math.Ceiling(MapContext.MaxX) - 1, maxX + margin);
            maxY = Math.Min((int)Math.Ceiling(MapContext.MaxY) - 1, maxY + margin);

            return new(minX, minY, maxX - minX + 1, maxY - minY + 1);
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
        public static TextureModel ToFastDynamicTextureModel(this byte[] bgraPixels, int width, int height)
        {
            var fakeStream = new FastDdsStream(bgraPixels, width, height);

            return TextureModel.Create(fakeStream);
        }
    }
}
