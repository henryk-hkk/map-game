using MapGame.Core.Constants;
using MapGame.Core.Utils.Geographic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MapGame.Core.Utils
{
    public static class MapDataLoader
    {



        public static BitmapImage LoadTexture(string relativePath)
        {
            Uri fileUri = new Uri(relativePath, UriKind.Relative);
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

            return GetGrayscaleMaskPixels(pixelData);
        }

        public static (Dictionary<Color, PixelArea> Areas, byte[] Pixels) LoadAreasFromColorMap(string imagePath)
        {
            BitmapImage colorMap = new BitmapImage();
            colorMap.BeginInit();
            colorMap.UriSource = new Uri(imagePath, UriKind.Relative);
            colorMap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            colorMap.EndInit();

            FormatConvertedBitmap convertedBitmap = new FormatConvertedBitmap(colorMap, PixelFormats.Bgra32, null, 0);

            int stride = Map.Width * 4;

            byte[] pixels = new byte[Map.Height * stride];
            convertedBitmap.CopyPixels(pixels, stride, 0);

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

        public static (BidirectionalMap<int, string>? RegionDict, List<Region>? Regions) ReadJSONRegionData(string relativePath)
        {

            Uri fileUri = new Uri(relativePath, UriKind.Relative);
            string jsonPath = fileUri.ToString();
            if (!File.Exists(jsonPath))
            {
                System.Diagnostics.Debug.WriteLine($"BŁĄD: Nie znaleziono pliku konfiguracyjnego w {jsonPath}");
                return (null, null);
            }

            string jsonContent = File.ReadAllText(jsonPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            MapJSONData config = JsonSerializer.Deserialize<MapJSONData>(jsonContent, options);

            if (config?.Regions == null) return (null, null);

            BidirectionalMap<int, string> regionsDict = new();
            List<Region> regions = [];

            foreach (var region in config.Regions)
            {
                if (region.Areas == null) continue;

                Region mapRegion = new Region(region.RegionId, region.Identifier);
                regionsDict.Add(region.RegionId, region.Identifier);
                regions.Add(mapRegion);

                foreach (var areaDef in region.Areas)
                {
                    Color targetColor = areaDef.GetColor();

                    if (Map.Areas.TryGetValue(targetColor, out PixelArea? actualArea))
                    {
                        actualArea.Name = areaDef.Name;
                        actualArea.parentRegionId = region.RegionId;
                        mapRegion.Add(actualArea);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"OSTRZEŻENIE: Kolor {targetColor} zdefiniowany w JSON nie istnieje na mapie rastrowej!");
                    }
                }
            }
            return (regionsDict , regions);
        }
    }
}
