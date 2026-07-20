using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using MapGame.Core.Utils.Map;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace MapGame.Core.Utils.Graphic
{
    public class CountryTexturesGenerator : ITexturesGenerator
    {
        private const int SdfScale = ITexturesGenerator.SdfScale;

        public static void Initialize()
        {
            var (width, height, _) = MapUtils.GetBitmapParams();
            var (_, scaledHeight, scaledStride) = MapUtils.GetScaledBitmapParams();

            GraphicContext.CountryPixelData = new byte[scaledHeight * scaledStride];

            RefreshDirtyRect(new Int32Rect(0, 0, width, height));
        }

        public static void RefreshDirtyRect(Int32Rect dirtyRect)
        {
            var (width, height, _) = MapUtils.GetBitmapParams();
            var (_, _, scaledStride) = MapUtils.GetScaledBitmapParams();


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

                    int countryId = MapLogicContext.GlobalCountryMap[logicIdx];

                    if (countryColorsCache.TryGetValue(countryId, out byte[] bgra))
                    {
                        GraphicContext.CountryPixelData[byteIdx] = bgra[0];
                        GraphicContext.CountryPixelData[byteIdx + 1] = bgra[1];
                        GraphicContext.CountryPixelData[byteIdx + 2] = bgra[2];
                        GraphicContext.CountryPixelData[byteIdx + 3] = bgra[3];
                    }
                    else
                    {
                        GraphicContext.CountryPixelData[byteIdx] = 0;
                        GraphicContext.CountryPixelData[byteIdx + 1] = 0;
                        GraphicContext.CountryPixelData[byteIdx + 2] = 0;
                        GraphicContext.CountryPixelData[byteIdx + 3] = 0;
                    }
                }
            }

            float originalThickness = SDFAgent.BorderThickness;
            float originalRadius = SDFAgent.SmoothRadiusMultiplier;

            SDFAgent.BorderThickness = 0.2f;
            SDFAgent.SmoothRadiusMultiplier = 1f;

            var countrySdfPixels = SDFAgent.ComputeLocalSDF(MapLogicContext.GlobalCountryMap, width, height, dirtyRect);

            SDFAgent.BorderThickness = originalThickness;
            SDFAgent.SmoothRadiusMultiplier = originalRadius;

            foreach (var pixel in countrySdfPixels)
            {
                int idx = pixel.Index;
                byte borderAlpha = pixel.Alpha;

                if (idx >= 0 && idx + 3 < GraphicContext.CountryPixelData.Length)
                {
                    byte currentAlpha = GraphicContext.CountryPixelData[idx + 3];

                    GraphicContext.CountryPixelData[idx] = (byte)Math.Max(0, GraphicContext.CountryPixelData[idx] - borderAlpha);
                    GraphicContext.CountryPixelData[idx + 1] = (byte)Math.Max(0, GraphicContext.CountryPixelData[idx + 1] - borderAlpha);
                    GraphicContext.CountryPixelData[idx + 2] = (byte)Math.Max(0, GraphicContext.CountryPixelData[idx + 2] - borderAlpha);
                    GraphicContext.CountryPixelData[idx + 3] = Math.Max(currentAlpha, borderAlpha);
                }
            }
        }

        private static Dictionary<int, byte[]> BuildCountryColorsCache()
        {
            var cache = new Dictionary<int, byte[]>
            {
                [-1] = [0, 0, 0, 0],
                [-2] = [0, 0, 0, 0]
            };

            foreach (var region in MapLogicContext.Regions)
            {
                if (region.Owner != null)
                {
                    int countryId = region.Owner.Identifier.GetHashCode();

                    if (!cache.ContainsKey(countryId))
                    {
                        Color c = region.Owner.DisplayColor;
                        byte alpha = (byte)(c.A == 0 ? 0 : 80);
                        cache[countryId] = [c.B, c.G, c.R, alpha];
                    }
                }
            }
            return cache;
        }
    }
}