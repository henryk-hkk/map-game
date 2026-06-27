using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MapGame.Core.Constants;

namespace MapGame.Core.Utils
{
    public static class MapDataLoader
    {
        public static BitmapImage LoadTextureMap(string relativePath)
        {
            Uri fileUri = new Uri("Assets/Map/Colored.png", UriKind.Relative);
            return new BitmapImage(fileUri);
        }
        public static byte[] LoadGrayscaleMap(string relativePath, bool validateSize = false)
        {
            Uri fileUri = new Uri(relativePath, UriKind.Relative);

            BitmapImage bitmap = new BitmapImage(fileUri);
            if(validateSize)
            {
                if (Map.Width != bitmap.PixelWidth || Map.Height != bitmap.PixelHeight)
                {
                    throw new Exception("Wymiary mapy w silniku nie pokrywają się z wymiarem assetów");
                }
            }
            else
            {
                Map.Width = bitmap.PixelWidth;
                Map.Height = bitmap.PixelHeight;
            }

            FormatConvertedBitmap grayBitmap = new FormatConvertedBitmap(bitmap, PixelFormats.Gray8, null, 0);

            byte[] pixelData = new byte[Map.Width * Map.Height];

            int stride = Map.Width;

            grayBitmap.CopyPixels(pixelData, stride, 0);

            return pixelData;
        }

        public static bool[] LoadHeightbasedLandMask(byte[] grayscaleHeightmap)
        {
            bool[] pixelData = new bool[grayscaleHeightmap.Length];
            
            for(int i= 0; i < grayscaleHeightmap.Length; i++)
            {
                if (grayscaleHeightmap[i] < 143) pixelData[i] = true;
                else pixelData[i] = false;
            }

            return pixelData;
        }
        public static bool[] LoadLandMask(string relativePath)
        {
            Uri fileUri = new Uri(relativePath, UriKind.Relative);

            BitmapImage bitmap = new BitmapImage(fileUri);

            if (Map.Width != bitmap.PixelWidth || Map.Height != bitmap.PixelHeight)
            {
                throw new Exception("Wymiary mapy w silniku nie pokrywają się z wymiarem assetów");
            }

            FormatConvertedBitmap grayBitmap = new FormatConvertedBitmap(bitmap, PixelFormats.Gray8, null, 0);

            byte[] pixelData = new byte[Map.Width * Map.Height];

            int stride = Map.Width;

            grayBitmap.CopyPixels(pixelData, stride, 0);

            return LoadHeightbasedLandMask(pixelData);
        }

        public static (Dictionary<Color, PixelArea> Areas, byte[] Pixels) LoadAreasFromColorMap(string imagePath)
        {
            BitmapImage colorMap = new BitmapImage(new Uri(imagePath, UriKind.Relative));

            int stride = Map.Width * 4;
            byte[] pixels = new byte[Map.Width * stride];
            colorMap.CopyPixels(pixels, stride, 0);

            Dictionary<Color, PixelArea> areasDict = new Dictionary<Color, PixelArea>();

            for (int y = 0; y < Map.Height; y++)
            {
                for (int x = 0; x < Map.Width; x++)
                {
                    int index = (y * stride) + (x * 4);

                    byte b = pixels[index];
                    byte g = pixels[index + 1];
                    byte r = pixels[index + 2];

                    if (r == 0 && g == 0 && b == 0) continue;

                    Color pixelColor = Color.FromRgb(r, g, b);

                    if (!areasDict.ContainsKey(pixelColor))
                    {
                        areasDict[pixelColor] = new PixelArea();
                    }

                    areasDict[pixelColor].AddPixel(x, y);
                }
            }
            System.Diagnostics.Debug.WriteLine($"Wczytano {areasDict.Count} unikalnych Areas.");
            return (areasDict, pixels);
        }
    }
}
