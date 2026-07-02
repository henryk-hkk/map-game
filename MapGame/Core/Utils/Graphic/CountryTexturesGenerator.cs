using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using MapGame.Core.Constants;
using MapGame.Core.Utils.Geographic;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

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

            Map.GlobalCountryMap = new int[width * height];
            BuildGlobalCountryMap(width, height);

            RefreshCountryDirtyRect(new Int32Rect(0, 0, width, height));
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

                    if (Map.AreaPixels[byteIdx] == 0 && Map.AreaPixels[byteIdx + 1] == 0 && Map.AreaPixels[byteIdx + 2] == 0)
                    {
                        Map.GlobalCountryMap[idx] = -1;
                        continue;
                    }

                    Color c = Color.FromRgb(Map.AreaPixels[byteIdx + 2], Map.AreaPixels[byteIdx + 1], Map.AreaPixels[byteIdx]);
                    if (Map.Areas.TryGetValue(c, out PixelArea area))
                    {
                        var region = Map.Regions.Find(r => r.Id == area.ParentRegionId);
                        if (region?.Owner != null)
                        {
                            Map.GlobalCountryMap[idx] = region.Owner.Identifier.GetHashCode();
                        }
                        else
                        {
                            Map.GlobalCountryMap[idx] = -2;
                        }
                    }
                }
            }
        }

        public static void RefreshCountryDirtyRect(Int32Rect dirtyRect)
        {
            var (width, height, _) = MapUtils.GetBitmapParams();
            int scaledWidth = width * SdfScale;
            int scaledHeight = height * SdfScale;
            int scaledStride = scaledWidth * 4;

            int startX_scaled = dirtyRect.X * SdfScale;
            int startY_scaled = dirtyRect.Y * SdfScale;
            int endX_scaled = (dirtyRect.X + dirtyRect.Width) * SdfScale;
            int endY_scaled = (dirtyRect.Y + dirtyRect.Height) * SdfScale;

            var countryColorsCache = BuildCountryColorsCache();

            for (int y = startY_scaled; y < endY_scaled; y++)
            {
                for (int x = startX_scaled; x < endX_scaled; x++)
                {
                    int globalX = x / SdfScale;
                    int globalY = y / SdfScale;
                    int logicIdx = (globalY * width) + globalX;
                    int byteIdx = (y * scaledStride) + (x * 4);

                    int countryId = Map.GlobalCountryMap[logicIdx];

                    if (countryColorsCache.TryGetValue(countryId, out byte[] bgra))
                    {
                        Map.CountryPixelData[byteIdx] = bgra[0];
                        Map.CountryPixelData[byteIdx + 1] = bgra[1];
                        Map.CountryPixelData[byteIdx + 2] = bgra[2];
                        Map.CountryPixelData[byteIdx + 3] = bgra[3];
                    }
                    else
                    {
                        Map.CountryPixelData[byteIdx] = 0;
                        Map.CountryPixelData[byteIdx + 1] = 0;
                        Map.CountryPixelData[byteIdx + 2] = 0;
                        Map.CountryPixelData[byteIdx + 3] = 0;
                    }
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

                    Map.CountryPixelData[idx] = (byte)Math.Max(0, Map.CountryPixelData[idx] - borderAlpha);
                    Map.CountryPixelData[idx + 1] = (byte)Math.Max(0, Map.CountryPixelData[idx + 1] - borderAlpha);
                    Map.CountryPixelData[idx + 2] = (byte)Math.Max(0, Map.CountryPixelData[idx + 2] - borderAlpha);
                    Map.CountryPixelData[idx + 3] = Math.Max(currentAlpha, borderAlpha);
                }
            }
        }

        private static Dictionary<int, byte[]> BuildCountryColorsCache()
        {
            var cache = new Dictionary<int, byte[]>();

            cache[-1] = new byte[] { 0, 0, 0, 0 };
            cache[-2] = new byte[] { 0, 0, 0, 0 };

            foreach (var region in Map.Regions)
            {
                if (region.Owner != null)
                {
                    int countryId = region.Owner.Identifier.GetHashCode();

                    if (!cache.ContainsKey(countryId))
                    {
                        Color c = region.Owner.DisplayColor ?? Color.FromArgb(0, 0, 0, 0);
                        byte alpha = (byte)(c.A == 0 ? 0 : 70);
                        cache[countryId] = new byte[] { c.B, c.G, c.R, alpha };
                    }
                }
            }

            return cache;
        }
    }
}