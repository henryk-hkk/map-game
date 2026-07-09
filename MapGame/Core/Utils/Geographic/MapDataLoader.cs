using MapGame.Core.Utils.Geographic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MapGame.Core.Utils.Geographic
{
    public static class MapDataLoader
    {

        private static bool IsCorrectSize(BitmapImage bitmap)
        {
            if (MapContext.Width != bitmap.PixelWidth || MapContext.Height != bitmap.PixelHeight)
            {
                return false;
            }
            return true;
        }

        public static BitmapImage LoadTexture(string relativePath)
        {
            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(relativePath, UriKind.Relative);

            bitmap.CacheOption = BitmapCacheOption.OnLoad;

            bitmap.EndInit();

            bitmap.Freeze();

            return bitmap;
        }

        
        public static byte[] LoadGrayscaleMap(string relativePath, bool validateSize = false)
        {
            Uri fileUri = new(relativePath, UriKind.Relative);

            BitmapImage bitmap = new(fileUri);
            if(validateSize)
            {
                if (!IsCorrectSize(bitmap)) {
                    throw new Exception("Wymiary mapy w silniku nie pokrywają się z wymiarem assetów");
                }
            }
            else
            {
                MapContext.Width = bitmap.PixelWidth;
                MapContext.Height = bitmap.PixelHeight;
            }

            FormatConvertedBitmap grayBitmap = new(bitmap, PixelFormats.Gray8, null, 0);

            byte[] pixelData = new byte[MapContext.Width * MapContext.Height];

            int stride = MapContext.Width;

            grayBitmap.CopyPixels(pixelData, stride, 0);

            return pixelData;
        }

        public static bool[] GetGrayscaleMaskPixels(byte[] grayscaleMask)
        {
            bool[] pixelData = new bool[grayscaleMask.Length];
            
            for(int i= 0; i < grayscaleMask.Length; i++)
            {
                if (grayscaleMask[i] < 128) pixelData[i] = true;
                else pixelData[i] = false;
            }

            return pixelData;
        }
        public static bool[] LoadMask(string relativePath)
        {
            Uri fileUri = new(relativePath, UriKind.Relative);

            BitmapImage bitmap = new(fileUri);

            if (!IsCorrectSize(bitmap))
            {
                throw new Exception("Wymiary mapy w silniku nie pokrywają się z wymiarem assetów");
            }

            FormatConvertedBitmap grayBitmap = new(bitmap, PixelFormats.Gray8, null, 0);

            byte[] pixelData = new byte[MapContext.Width * MapContext.Height];

            int stride = MapContext.Width;

            grayBitmap.CopyPixels(pixelData, stride, 0);

            return GetGrayscaleMaskPixels(pixelData);
        }

        public static (Dictionary<Color, PixelArea> AreaColors, List<PixelArea> Areas, byte[] Pixels) LoadAreasFromColorMap(string imagePath)
        {
            BitmapImage colorMap = new();
            colorMap.BeginInit();
            colorMap.UriSource = new Uri(imagePath, UriKind.Relative);
            colorMap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            colorMap.EndInit();

            FormatConvertedBitmap convertedBitmap = new(colorMap, PixelFormats.Bgra32, null, 0);

            int stride = MapContext.Width * 4;

            byte[] pixels = new byte[MapContext.Height * stride];
            convertedBitmap.CopyPixels(pixels, stride, 0);

            Dictionary<Color, PixelArea> areasDict = [];
            List<PixelArea> areas = [];

            for (int y = 0; y < MapContext.Height; y++)
            {
                for (int x = 0; x < MapContext.Width; x++)
                {
                    int index = (y * stride) + (x * 4);

                    byte b = pixels[index];
                    byte g = pixels[index + 1];
                    byte r = pixels[index + 2];

                    if (r == 0 && g == 0 && b == 0) continue;

                    Color pixelColor = Color.FromRgb(r, g, b);

                    if (!areasDict.TryGetValue(pixelColor, out PixelArea? value))
                    {
                        PixelArea a = new();
                        areas.Add(a);
                        value = a;
                        areasDict[pixelColor] = value;
                    }

                    value.AddPixel(x, y);
                    
                }
            }

            System.Diagnostics.Debug.WriteLine($"Wczytano {areasDict.Count} unikalnych Areas.");
            return (areasDict, areas, pixels);
        }
    }
}
