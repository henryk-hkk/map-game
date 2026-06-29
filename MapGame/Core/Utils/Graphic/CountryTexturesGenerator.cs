using MapGame.Core.Constants;
using MapGame.Core.Utils.Geographic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace MapGame.Core.Utils.Graphic
{
    public static class CountryTexturesGenerator
    {
        private const int SdfScale = 2;

        public static void InitializeCountryRendering()
        {
            var (width, height, stride) = MapUtils.GetBitmapParams();
            int scaledWidth = width * SdfScale;
            int scaledHeight = height * SdfScale;
            int scaledStride = scaledWidth * 4;

            Map.CountryPixelData = new byte[scaledHeight * scaledStride];
            Map.CountryBitmap = new WriteableBitmap(scaledWidth, scaledHeight, 96, 96, PixelFormats.Bgra32, null);

            // Budujemy mapę logiczną państw na start
            Map.GlobalCountryMap = new int[width * height];
            BuildGlobalCountryMap(width, height);

            // Pierwsze pełne rysowanie całej mapy świata
            RefreshCountryDirtyRect(new Int32Rect(0, 0, width, height));

            ImageBrush brush = new ImageBrush(Map.CountryBitmap);
            RenderOptions.SetBitmapScalingMode(brush, BitmapScalingMode.NearestNeighbor);

            // Pędzel dynamiczny - nie zamrażamy!
            Map.CountryMaterial = new DiffuseMaterial(brush);
        }

        private static void BuildGlobalCountryMap(int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = (y * width) + x;
                    int stride = width * 4;
                    int byteIdx = (y * stride) + (x * 4);

                    // Ignorujemy wodę
                    if (Map.AreaPixels[byteIdx] == 0 && Map.AreaPixels[byteIdx + 1] == 0 && Map.AreaPixels[byteIdx + 2] == 0)
                    {
                        Map.GlobalCountryMap[idx] = -1;
                        continue;
                    }

                    Color c = Color.FromRgb(Map.AreaPixels[byteIdx + 2], Map.AreaPixels[byteIdx + 1], Map.AreaPixels[byteIdx]);
                    if (Map.Areas.TryGetValue(c, out PixelArea area))
                    {
                        var region = Map.Regions.Find(r => r.Id == area.parentRegionId);
                        if (region?.Owner != null)
                        {
                            // Jako unikalne ID państwa wykorzystujemy hash z jego string Identifier
                            Map.GlobalCountryMap[idx] = region.Owner.Identifier.GetHashCode();
                        }
                        else
                        {
                            Map.GlobalCountryMap[idx] = -2; // Brak właściciela
                        }
                    }
                }
            }
        }

        public static void RefreshCountryDirtyRect(Int32Rect dirtyRect)
        {
            var (width, height, _) = MapUtils.GetBitmapParams();
            int scaledWidth = width * SdfScale;
            int scaledStride = scaledWidth * 4;

            int startX_scaled = dirtyRect.X * SdfScale;
            int startY_scaled = dirtyRect.Y * SdfScale;
            int endX_scaled = (dirtyRect.X + dirtyRect.Width) * SdfScale;
            int endY_scaled = (dirtyRect.Y + dirtyRect.Height) * SdfScale;

            for (int y = startY_scaled; y < endY_scaled; y++)
            {
                for (int x = startX_scaled; x < endX_scaled; x++)
                {
                    int globalX = x / SdfScale;
                    int globalY = y / SdfScale;
                    int logicIdx = (globalY * width) + globalX;
                    int byteIdx = (y * scaledStride) + (x * 4);

                    int countryId = Map.GlobalCountryMap[logicIdx];

                    if (countryId == -1)
                    {
                        Map.CountryPixelData[byteIdx] = 0;
                        Map.CountryPixelData[byteIdx + 1] = 0;
                        Map.CountryPixelData[byteIdx + 2] = 0;
                        Map.CountryPixelData[byteIdx + 3] = 0;
                        continue;
                    }

                    Color fillColor = Color.FromArgb(0, 0, 0, 0);
                    int origByteIdx = (globalY * (width * 4)) + (globalX * 4);
                    Color areaColor = Color.FromRgb(Map.AreaPixels[origByteIdx + 2], Map.AreaPixels[origByteIdx + 1], Map.AreaPixels[origByteIdx]);

                    if (Map.Areas.TryGetValue(areaColor, out PixelArea area))
                    {
                        var region = Map.Regions.Find(r => r.Id == area.parentRegionId);
                        if (region?.Owner != null && region.Owner.DisplayColor.HasValue)
                        {
                            fillColor = region.Owner.DisplayColor.Value;
                        }
                    }

                    Map.CountryPixelData[byteIdx] = fillColor.B;
                    Map.CountryPixelData[byteIdx + 1] = fillColor.G;
                    Map.CountryPixelData[byteIdx + 2] = fillColor.R;
                    Map.CountryPixelData[byteIdx + 3] = (byte)(fillColor.A == 0 ? 0 : 70);
                }
            }

            float originalThickness = SDFAgent.BorderThickness;
            float originalRadius = SDFAgent.SmoothRadiusMultiplier;

            SDFAgent.BorderThickness = 0.5f;
            SDFAgent.SmoothRadiusMultiplier = 2.0f;

            var countrySdfPixels = SDFAgent.ComputeLocalSDF(Map.GlobalCountryMap, width, height, SdfScale, dirtyRect);

            SDFAgent.BorderThickness = originalThickness;
            SDFAgent.SmoothRadiusMultiplier = originalRadius;

            foreach (var pixel in countrySdfPixels)
            {
                int idx = pixel.Index;
                byte borderAlpha = pixel.Alpha;

                if (idx >= 0 && idx + 3 < Map.CountryPixelData.Length)
                {
                    byte currentAlpha = Map.CountryPixelData[idx + 3];

                    Map.CountryPixelData[idx] = (byte)Math.Max(0, Map.CountryPixelData[idx] - borderAlpha);     // B
                    Map.CountryPixelData[idx + 1] = (byte)Math.Max(0, Map.CountryPixelData[idx + 1] - borderAlpha); // G
                    Map.CountryPixelData[idx + 2] = (byte)Math.Max(0, Map.CountryPixelData[idx + 2] - borderAlpha); // R
                    Map.CountryPixelData[idx + 3] = Math.Max(currentAlpha, borderAlpha);
                }
            }

            Int32Rect scaledDirtyRect = new Int32Rect(startX_scaled, startY_scaled, endX_scaled - startX_scaled, endY_scaled - startY_scaled);
            int offset = (startY_scaled * scaledStride) + (startX_scaled * 4);
            Map.CountryBitmap.WritePixels(scaledDirtyRect, Map.CountryPixelData, scaledStride, offset);
        }
    }
}
